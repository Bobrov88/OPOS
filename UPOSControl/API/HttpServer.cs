using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.API
{
    class HttpServer : ApiController, IDisposable
    {
        private readonly TcpListener _listener;
        private readonly List<HttpServerClient> _clients;

        public HttpServer(IPAddress address, int port = 4455)
        {
            if (port == 80 || port < 0 || port == 8080)
                throw new Exception("Неправильно указан порт.");

            _listener = new TcpListener(address, port);
            _clients = new List<HttpServerClient>();

            myDelegateStopHttpServer = new StopHttpServer(Stop);

            ListenAsync();
        }

        public async void ListenAsync()
        {
            try
            {
                _listener.Start();
                ConsoleManager.Add(LogType.Error, "HttpServer", "ListenAsync", String.Format("Сервер стартовал на {0}", _listener.LocalEndpoint));
                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();

                    ConsoleManager.Add(LogType.Error, "HttpServer", "ListenAsync", String.Format("Подключение: {0} -> {1}", client.Client.RemoteEndPoint, client.Client.LocalEndPoint));
                    lock (_clients)
                    {
                        _clients.Add(new HttpServerClient(client, c => { lock (_clients) { _clients.Remove(c); } c.Dispose(); }));
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (ex.ObjectName.EndsWith("Socket"))
                    ConsoleManager.Add(LogType.Alert, "HttpServer", "ListenAsync", "Web-сервер остановлен.");
                else
                    ConsoleManager.Add(LogType.Error, "HttpServer", "ListenAsync", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        public new void Stop()
        {
            _listener.Stop();
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
            _listener.Stop();
            if (disposing)
            {
                ConsoleManager.Add(LogType.Error, "HttpServer", "ListenAsync", "Отключаю подключенных клиентов...");
                lock (_clients)
                {
                    foreach (HttpServerClient client in _clients)
                    {
                        client.Dispose();
                    }
                }
                Console.WriteLine("Клиенты отключены.");
            }
        }

        ~HttpServer() => Dispose(false);
    }
}
