using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.Tasks;

namespace UPOSControl.Http
{
    public class HttpManager
    {

        private string APP_PATH = "https://casheq.ru:80";
        private static string _token;
        private static List<CookieWithOptions> _cookiesWO;
        private static IEnumerable<string> _cookies;

        public HttpManager(string domain)
        {
            APP_PATH = domain;
        }

        /// <summary>
        /// Ответ от удалённого сервера
        /// </summary>
        public class HttpAnswer
        {
            public string Message { get; set; } = "";
            public HttpStatusCode Code { get; set; }
        }


        /// <summary>
        /// Авторизация
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Login(string userName, string password)
        {
            var registerModel = new
            {
                UserName = userName,
                Password = password,
                ConfirmPassword = password
            };
            using (var client = new HttpClient())
            {
                var response = client.PostAsJsonAsync(APP_PATH + "/api/Account/Login", registerModel).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (response.Headers.TryGetValues("Set-Cookie", out _cookies))
                    {
                        _cookiesWO = new List<CookieWithOptions>();

                        foreach(string item in _cookies)
                        {
                            CookieWithOptions cookie = ExtractCookie(item);
                            _cookiesWO.Add(cookie);
                            return true;
                        }

                    }
                }
            }

            return false;
        }

        public class CookieWithOptions
        {
            public CookieOptions Options { get; set; } = new CookieOptions();
            public string Key { get; set; } = default!;
            public string Value { get; set; } = default!;
        }

        public static class CookieProperties
        {
            public const string Expires = "expires";
            public const string Expired = "expired";
            public const string Path = "path";
            public const string Domain = "domain";
            public const string Secure = "secure";
            public const string SameSite = "samesite";
            public const string HttpOnly = "httponly";
            public const string MaxAge = "maxage";
            public const string IsEssential = "isessential";
        }

        private CookieWithOptions ExtractCookie(string cookieString)
        {
            CookieWithOptions cookie = new();

            var parts = cookieString!.Split("; ");
            var claims = new Dictionary<string, string>();

            foreach (var part in parts)
            {
                var keyValuePair = part.Split('=');
                claims.Add(keyValuePair.First(), keyValuePair.Last());
            }

            SetCookieOptions(claims, cookie);

            return cookie;
        }

        public void SetCookieOptions(
            Dictionary<string, string> claims,
            CookieWithOptions cookie)
        {
            foreach (var claim in claims)
            {
                switch (claim.Key.ToLower())
                {
                    case CookieProperties.Expires:
                        cookie.Options.Expires = DateTime.ParseExact(
                            claim.Value,
                            "ddd, d MMM yyyy HH:mm:ss GMT",
                            CultureInfo.GetCultureInfoByIetfLanguageTag("en-us"),
                            DateTimeStyles.AdjustToUniversal);
                        break;
                    case CookieProperties.Secure:
                        cookie.Options.Secure = bool.Parse(claim.Value);
                        break;
                    case CookieProperties.IsEssential:
                        cookie.Options.IsEssential = bool.Parse(claim.Value);
                        break;
                    case CookieProperties.SameSite:
                        {
                            SameSiteMode res = SameSiteMode.Lax;
                            Enum.TryParse<SameSiteMode>(claim.Value, true, out res);
                            cookie.Options.SameSite = res;
                        }
                        break;
                    case CookieProperties.Path:
                        cookie.Options.Path = claim.Value;
                        break;
                    case CookieProperties.Domain:
                        cookie.Options.MaxAge = TimeSpan.Parse(claim.Value);
                        break;
                    case CookieProperties.HttpOnly:
                        {
                            bool httponly = false;
                            if (!bool.TryParse(claim.Value, out httponly))
                                cookie.Options.HttpOnly = true;
                            else
                                cookie.Options.HttpOnly = httponly;
                        }
                        break;
                    default:
                        cookie.Key = claim.Key;
                        cookie.Value = claim.Value;
                        break;
                }
            }
        }

        /// <summary>
        /// Получение токена
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Dictionary<string, string> GetTokenDictionary(string userName, string password)
        {
            var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>( "grant_type", "password" ),
                    new KeyValuePair<string, string>( "username", userName ),
                    new KeyValuePair<string, string> ( "password", password )
                };
            var content = new FormUrlEncodedContent(pairs);

            using (var client = new HttpClient())
            {
                var response =
                    client.PostAsync(APP_PATH + "/Token", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                // Десериализация полученного JSON-объекта
                Dictionary<string, string> tokenDictionary =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                return tokenDictionary;
            }
        }

        /// <summary>
        /// Cоздаем http-клиента с токеном
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>        
        HttpClient CreateClient()
        {
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            }
            if(_cookies?.Count() > 0)
            {
                foreach (string cookie in _cookies)
                {
                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                }
                
            }

            return client;
        }

        /// <summary>
        /// Получить состояние сервера
        /// </summary>
        /// <returns></returns>
        public ServerState GetServerState()
        {
            ServerState _serverState = null;

            try
            {
                using (var client = CreateClient())
                {
                    ResponseModel responseModel = null;

                    var response = client.GetAsync(APP_PATH + "/api/server/serverstate").Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (!String.IsNullOrEmpty(result))
                        responseModel =
                            JsonConvert.DeserializeObject<ResponseModel>(result);

                    if (!String.IsNullOrEmpty(responseModel?.Response))
                    {
                        _serverState =
                            JsonConvert.DeserializeObject<ServerState>(responseModel.Response);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "GetServerState", "Вызвано исключение: " + ex.Message);
            }

            return _serverState;
        }

        /// <summary>
        /// Получить устройство
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public CashDevice PostDevice(CashDevice device)
        {
            try
            {
                using (var client = CreateClient())
                {
                    ResponseModel responseModel = null;

                    var response = client.PostAsJsonAsync(APP_PATH + "/api/devices/device", device).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (!String.IsNullOrEmpty(result))
                        responseModel =
                            JsonConvert.DeserializeObject<ResponseModel>(result);

                    if (!String.IsNullOrEmpty(responseModel?.Response))
                    {
                        device =
                            JsonConvert.DeserializeObject<CashDevice>(responseModel.Response);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "PostDevice", "Вызвано исключение: " + ex.Message);
            }

            return device;
        }

        /// <summary>
        /// Получить устройства пачкой
        /// </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        public List<CashDevice> PostDevices(List<CashDevice> devices)
        {
            try
            {
                if (devices?.Count > 0)
                {
                    using (var client = CreateClient())
                    {
                        ResponseModel responseModel = null;

                        var response = client.PostAsJsonAsync(APP_PATH + "/api/devices/devices", devices).Result;
                        string result = response.Content.ReadAsStringAsync().Result;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {

                            if (!String.IsNullOrEmpty(result))
                                responseModel =
                                    JsonConvert.DeserializeObject<ResponseModel>(result);

                            if (!String.IsNullOrEmpty(responseModel?.Response))
                            {
                                devices =
                                    JsonConvert.DeserializeObject<List<CashDevice>>(responseModel.Response);
                            }
                        }
                        else
                            ConsoleManager.Add(LogType.Alert, "HttpManager", "PostDevices", "Не удалось получить объект устройства: " + result);

                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "PostDevices", "Вызвано исключение: " + ex.Message);
            }

            return devices;
        }

        /// <summary>
        /// Получить эталоны
        /// </summary>
        /// <returns></returns>
        public void GetEthalons()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = client.GetAsync(APP_PATH + "/api/devices/ethalons").Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (!String.IsNullOrEmpty(result))
                        {
                            EthalonManager.Ethalons = JsonConvert.DeserializeObject<List<CashDevice>>(result);
                            EthalonManager.Update = true;
                        }
                    }
                    else
                        ConsoleManager.Add(LogType.Alert, "HttpManager", "GetEthalons", "Не удалось получить эталоны: " + result);

                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "GetEthalons", "Вызвано исключение: " + ex.Message);
            }
        }

        /// <summary>
        /// Получить новые задания для устройства
        /// </summary>
        /// <param name="devicekeyid"></param>
        /// <returns></returns>
        public List<Tasks.Task> GetNewTasks(string devicekeyid)
        {
            List<Tasks.Task> tasks = new List<Tasks.Task>();

            try
            {
                using (var client = CreateClient())
                {
                    ResponseModel responseModel = null;

                    var response = client.GetAsync(APP_PATH + "/api/devices/newtasks/" + devicekeyid).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (!String.IsNullOrEmpty(result))
                        responseModel =
                            JsonConvert.DeserializeObject<ResponseModel>(result);

                    if (!String.IsNullOrEmpty(responseModel?.Response))
                    {
                        tasks = JsonConvert.DeserializeObject<List<Tasks.Task>>(responseModel.Response);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "GetNewTasks", "Вызвано исключение: " + ex.Message);
            }

            return tasks;
        }

        /// <summary>
        /// Подтвердить задание
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool PostSetTask(Tasks.Task task)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = client.PostAsJsonAsync(APP_PATH + "/api/devices/settask", task).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                        return true;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "PostSetTask", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }



        /// <summary>
        /// Подтвердить задание
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool PostSetTasks(List<Tasks.Task> tasks)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = client.PostAsJsonAsync(APP_PATH + "/api/devices/settasks", tasks).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                        return true;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "PostSetTasks", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }


        /// <summary>
        /// Подтвердить устройство
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool PostSetDevice(Tasks.Task task)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = client.PostAsJsonAsync(APP_PATH + "/api/devices/setdevice", task).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                        return true;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "PostSetDevice", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }

        private static bool DownloadIsCompleted = false;

        /// <summary>
        /// Скачать файл прошивки
        /// </summary>
        /// <param name="fileKeyId"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<bool> DownloadFirmware(string fileKeyId, string fileName)
        {
            try
            {
                if (String.IsNullOrEmpty(fileKeyId))
                {
                    ConsoleManager.Add(LogType.Error, "HttpManager", "DownloadFirmware", "Невозможно скачать файл прошивки, ключ не может быть пустым.");
                    return false;
                }
                if (String.IsNullOrEmpty(fileName))
                {
                    ConsoleManager.Add(LogType.Error, "HttpManager", "DownloadFirmware", "Невозможно скачать файл прошивки, не задано имя файла.");
                    return false;
                }

                Uri uri = new Uri(APP_PATH + "/api/devices/downloadfirmware/" + fileKeyId);

                using (WebClient client = new WebClient())
                {
                    if (_cookies?.Count() > 0)
                    {
                        foreach (string cookie in _cookies)
                        {
                            client.Headers.Add("Cookie", cookie);
                        }
                    }

                    client.DownloadProgressChanged += WebClientDownloadProgressChanged;

                    if (!Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Firmwares\"))
                        Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Firmwares\");

                    fileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Firmwares\" + fileName;

                    await client.DownloadFileTaskAsync(uri, fileName);

                    ConsoleManager.Add(" ");
                    ConsoleManager.Add(LogType.Information, "HttpManager", "WebClientDownloadProgressChanged", "Скачивание прошивки завершено успешно.");

                    return true;
                }
            }
            catch(WebException we)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "DownloadFirmware", "Вызвано исключение: " + we.Message);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "HttpManager", "DownloadFirmware", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }

        void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ConsoleManager.AddProcent(e.ProgressPercentage);
        }
    }
}
