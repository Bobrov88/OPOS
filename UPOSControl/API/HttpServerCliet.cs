using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.API
{
    class HttpServerClient: ApiController, IDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly EndPoint _remoteEndPoint;
        private readonly Task _clientTask;
        private readonly Action<HttpServerClient> _disposeCallback;
        // Таймер
        private Timer TimerTimeout;

        private bool _close = false;

        private HttpRequestMessage _request;
        private HttpStatusCode _status;
        private HttpResponseMessage _response;
        private Api _api;

        public HttpServerClient(TcpClient client, Action<HttpServerClient> disposeCallback)
        {
            _client = client;
            _stream = client.GetStream();
            _remoteEndPoint = client.Client.RemoteEndPoint;
            _disposeCallback = disposeCallback;
            _clientTask = RunReadingLoop();
            TimerTimeout = new System.Timers.Timer();
            TimerTimeout.Interval = 3000;
            TimerTimeout.Elapsed += new ElapsedEventHandler(AnswerByTimeout); //Вызов функции по таймеру
            _api = GetApiData();

        }

        const string errorTemplate = "<html><head><title>{0}</title></head><body><center><h1>{0}</h1></center><hr><center>TcpListener server</center></body></html>";

        private async Task RunReadingLoop()
        {
            try
            {
                while (!_close)
                {
                    string apiRequest = "";
                    (_request, _status) = await ReceivePacket().ConfigureAwait(false);
                    if (_request != null)
                    {
                        if (!String.IsNullOrEmpty(_api.BearerKey) || _api.BasicKeys?.Length > 0)
                        {
                            AuthenticationHeaderValue authentication = _request?.Headers.Authorization;
                            if(authentication == null)
                            {
                                CreateAnswerToClient(new AnswerFromDevice { Message = "Ошибка авторизации.", Error = Errors.ErrorAccess, HasAnswer = false, Request = _request != null ? Encoding.UTF8.GetBytes(_request.RequestUri.ToString()) : null });
                                _close = true;
                                break;
                            }

                            switch (authentication.Scheme)
                            {
                                case "Bearer":
                                    {
                                        if (!String.IsNullOrEmpty(_api.BearerKey))
                                            if (!authentication.Parameter.Equals(_api.BearerKey))
                                            {
                                                CreateAnswerToClient(new AnswerFromDevice { Message = "Ошибка авторизации.", Error = Errors.ErrorAccess, HasAnswer = false, Request = _request != null ? Encoding.UTF8.GetBytes(_request.RequestUri.ToString()) : null });
                                                _close = true;
                                                break;
                                            }
                                    }
                                    break;
                                case "Basic":
                                    {

                                        bool userExist = false;

                                        if (_api.BasicKeys?.Length > 0)
                                        {
                                            foreach (string basic in _api.BasicKeys)
                                            {
                                                if (authentication.Parameter.Equals(basic))
                                                {
                                                    userExist = true;
                                                    break;
                                                }
                                            }

                                            if (!userExist)
                                            {
                                                CreateAnswerToClient(new AnswerFromDevice { Message = "Ошибка авторизации.", Error = Errors.ErrorAccess, HasAnswer = false, Request = _request != null ? Encoding.UTF8.GetBytes(_request.RequestUri.ToString()) : null });
                                                _close = true;
                                                break;
                                            }
                                        }


                                    }
                                    break;
                            }
                        }

                        apiRequest = _request.RequestUri.ToString();
                        ConsoleManager.Add(LogType.Information, "HttpServerClient", "RunReadingLoop", $"Web запрос: {_request.Method.Method} {apiRequest}");
                    }
                    else
                        ConsoleManager.Add(LogType.Information, "HttpServerClient", "RunReadingLoop", $"Web запрос: не распознан.");

                    if (_status == HttpStatusCode.OK)
                    {
                        if (apiRequest == "/")
                        {
                            CreateAnswerToClient(new AnswerFromDevice { Message = GetHelp(), Error = 0, HasAnswer = true, Request = Encoding.UTF8.GetBytes(apiRequest) });
                        }
                        else {

                            //Запуск таймера
                            TimerTimeout.Enabled = true;
                            AnswerFromDevice answer = await myDelegateRunCommandFromClient.Invoke(apiRequest);
                            if (answer != null)
                                CreateAnswerToClient(answer);

                        }
                    }
                    else
                    {
                        CreateAnswerToClient(new AnswerFromDevice { Message = $"{_status}", Error = Errors.ErrorOther, HasAnswer = false, Request = Encoding.UTF8.GetBytes(apiRequest) });
                    }
                }

                ConsoleManager.Add(LogType.Information, "HttpServerClient", "RunReadingLoop", String.Format("Подключение к {0} закрыто клиентом.", _remoteEndPoint));
                _stream.Close();
            }
            catch (HttpRequestException)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "RunReadingLoop", String.Format("Подключение к {0} закрыто клиентом.", _remoteEndPoint));
            }
            catch (IOException)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "RunReadingLoop", String.Format("Подключение к {0} закрыто сервером.", _remoteEndPoint));
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "RunReadingLoop", ex.GetType().Name + ": " + ex.Message);
            }
            if (!disposed)
                _disposeCallback(this);
        }

        private void AnswerByTimeout(object sender, EventArgs e)
        {
            CreateAnswerToClient(new AnswerFromDevice { Message = "Вышло время ожидания ответа от устройства. Либо запрос без ответа.", Error = 0, HasAnswer = true, Request = _request != null ? Encoding.UTF8.GetBytes(_request.RequestUri.ToString()) : null });
        }


        private async void CreateAnswerToClient(AnswerFromDevice answer)
        {
            try
            {
                TimerTimeout.Enabled = false;

                using (_response = new HttpResponseMessage(_status))
                {
                    if (_request != null)
                        foreach (var c in _request?.Headers.Connection)
                            _response.Headers.Connection.Add(c);
                    else
                        _response.Headers.Connection.Add("close");

                    if(answer.Error == 0 && answer.HasAnswer)
                        _response.Content = CreateHtmlContent($"<html><head><title>{answer.Request}</title></head><body><p>{answer.Message}</p></body></html>");
                    else 
                        _response.Content = CreateHtmlContent(string.Format(errorTemplate, $"{(int)_response.StatusCode} {_response.ReasonPhrase}"));

                    await SendResponse(_response).ConfigureAwait(false);

                    if (_response.Headers.Connection.Contains("close"))
                        _close = true;
                }
            }
            catch (HttpRequestException)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "CreateAnswerToClient", String.Format("Подключение к {0} закрыто клиентом.", _remoteEndPoint));
            }
            catch (IOException)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "CreateAnswerToClient", String.Format("Подключение к {0} закрыто сервером.", _remoteEndPoint));
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "CreateAnswerToClient", "Вызвано исключение " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private HttpContent CreateHtmlContent(string text)
        {
            StringContent content = new StringContent(text, Encoding.UTF8, "text/html");
            content.Headers.ContentLength = content.Headers.ContentLength;
            return content;
        }

        private async Task SendResponse(HttpResponseMessage response)
        {
            using (StreamWriter sw = new StreamWriter(_stream, leaveOpen: true))
            {
                sw.WriteLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
                sw.Write(response.Headers);
                sw.WriteLine(response.Content?.Headers.ToString() ?? "");
            }
            if (response.Content != null)
                await response.Content.CopyToAsync(_stream);
        }

        private async Task<(HttpRequestMessage, HttpStatusCode)> ReceivePacket()
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage();
                string requestHeader = await ReadLineAsync().ConfigureAwait(false);
                string[] headerTokens = requestHeader.Split(" ");
                if (headerTokens.Length != 3)
                    return (null, HttpStatusCode.BadRequest);
                request.Method = new HttpMethod(headerTokens[0]);
                request.RequestUri = new Uri(headerTokens[1], UriKind.Relative);
                string[] protocolTokens = headerTokens[2].Split('/');
                if (protocolTokens.Length != 2 || protocolTokens[0] != "HTTP")
                    return (null, HttpStatusCode.BadRequest);
                request.Version = Version.Parse(protocolTokens[1]);
                MemoryStream ms = new MemoryStream();
                HttpContent content = new StreamContent(ms);
                request.Content = content;
                while (true)
                {
                    string headerLine = await ReadLineAsync().ConfigureAwait(false);
                    if (headerLine.Length == 0)
                        break;
                    string[] tokens = headerLine.Split(":", 2);
                    if (tokens.Length == 2)
                    {
                        foreach (HttpRequestHeader h in Enum.GetValues(typeof(HttpRequestHeader)))
                        {
                            if (tokens[0].ToLower() == Enum.GetName(typeof(HttpRequestHeader), h).ToLower())
                            {
                                if ((int)h >= 10 && (int)h <= 19) // if Entity Header
                                    request.Content.Headers.Add(tokens[0], tokens[1]);
                                else
                                    request.Headers.Add(tokens[0], tokens[1]);
                                break;
                            }
                        }
                    }
                }
                long length = request.Content.Headers?.ContentLength ?? 0;

                if (length > 0)
                {
                    await CopyBytesAsync(_stream, ms, (int)length);
                    ms.Position = 0;
                }
                return (request, HttpStatusCode.OK);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
            catch
            {
                return (null, HttpStatusCode.InternalServerError);
            }
        }

        private async Task CopyBytesAsync(Stream source, Stream target, int count)
        {
            const int bufferSize = 65536;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (count > 0)
                {
                    int bytesReceived = await source.ReadAsync(buffer.AsMemory(0, Math.Min(count, bufferSize)));
                    if (bytesReceived == 0)
                        break;
                    await target.WriteAsync(buffer.AsMemory(0, bytesReceived));
                    count -= bytesReceived;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task<string> ReadLineAsync() => await Task.Run(ReadLine);

        private string ReadLine()
        {
            LineState lineState = LineState.None;
            StringBuilder sb = new StringBuilder(128);

            try
            {
                while (true)
                {
                    int b = _stream.ReadByte();
                    switch (b)
                    {
                        case -1:
                            {
                                _close = true;
                                throw new HttpRequestException("Подключение разорвано.");
                            }
                        case '\r':
                            if (lineState == LineState.None)
                                lineState = LineState.CR;
                            else
                                throw new ProtocolViolationException("Неожиданный CR в заголовке запроса.");
                            break;
                        case '\n':
                            if (lineState == LineState.CR)
                                lineState = LineState.LF;
                            else
                                throw new ProtocolViolationException("Неожиданный LF в заголовке запроса.");
                            break;
                        default:
                            lineState = LineState.None;
                            sb.Append((char)b);
                            break;
                    }
                    if (lineState == LineState.LF)
                        break;
                }
            }
            catch(Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpServerClient", "CreateAnswerToClient", "Вызвано исключение " + ex.GetType().Name + ": " + ex.Message);
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                throw new ObjectDisposedException(typeof(HttpServer).FullName);
            disposed = true;
            if (_client.Connected)
            {
                _stream.Close();
                _clientTask.Wait();
            }
            if (disposing)
            {
                _client.Dispose();
            }
        }

        ~HttpServerClient() => Dispose(false);
    }

    internal static class HttpRequestHeaderExtensions
    {
        private static readonly string[] s_names = {
        "Cache-Control",
        "Connection",
        "Date",
        "Keep-Alive",
        "Pragma",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Via",
        "Warning",
        "Allow",
        "Content-Length",
        "Content-Type",
        "Content-Encoding",
        "Content-Language",
        "Content-Location",
        "Content-MD5",
        "Content-Range",
        "Expires",
        "Last-Modified",
        "Accept",
        "Accept-Charset",
        "Accept-Encoding",
        "Accept-Language",
        "Authorization",
        "Cookie",
        "Expect",
        "From",
        "Host",
        "If-Match",
        "If-Modified-Since",
        "If-None-Match",
        "If-Range",
        "If-Unmodified-Since",
        "Max-Forwards",
        "Proxy-Authorization",
        "Referer",
        "Range",
        "Te",
        "Translate",
        "User-Agent",
    };

        public static string GetName(this HttpRequestHeader header)
        {
            return s_names[(int)header];
        }
    }

    enum LineState
    {
        None,
        LF,
        CR
    }
}
