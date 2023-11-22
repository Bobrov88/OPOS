using Newtonsoft.Json;
using System;
using UPOSControl.Enums;
using UPOSControl.Managers;
using WindowsInput;

namespace UPOSControl.Tasks
{
    public class WebPortalSetting
    {
        [JsonProperty("on")]
        public bool On { get; set; } = false;
        [JsonProperty("domain")]
        public string Domain { get; set; } = @"https://casheq.ru:80";
        [JsonProperty("login")]
        public string Login { get; set; } = "Test";
        [JsonProperty("password")]
        public string Password { get; set; } = "Test1234";

        [JsonIgnore]
        public bool Initialized { get; set; } = false; 

        /// <summary>
        /// Инициализация
        /// </summary>
        public bool Init()
        {
            ConsoleManager.Add(LogType.Information, "WebPortalSetting", "WebPortalSetting", "Хотите настроить web-портал (Enter - да)?");
            
            string way = ConsoleManager.Read();
            if (String.IsNullOrEmpty(way)) { 
                SetAddress();
                SetLogin();
                SetPassword();
                On = true;
                ConsoleManager.Add(LogType.Information, "WebPortalSetting", "Init", "Web-портал настроен.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Установить адрес портала
        /// </summary>
        private void SetAddress()
        {
            ConsoleManager.Add(LogType.Information, "WebPortalSetting", "SetAddress", @"Введите адрес домена и порт web-портала (по - умолчанию - https://casheq.ru:80):");
            string domain = ConsoleManager.Read();
            if (!String.IsNullOrEmpty(domain))
            {
                Domain = domain;
            }
        }

        private void SetLogin()
        {
            ConsoleManager.Add(LogType.Information, "WebPortalSetting", "SetLogin", @"Введите Логин (по - умолчанию - Test):");
            string login = ConsoleManager.Read();
            if (!String.IsNullOrEmpty(login))
            {
                Login = login;
            }
        }

        private void SetPassword()
        {
            ConsoleManager.Add(LogType.Information, "WebPortalSetting", "SetLogin", @"Введите Пароль (по - умолчанию - Test1234):");
            string password = ConsoleManager.Read();
            if (!String.IsNullOrEmpty(password))
            {
                Password = password;
            }
        }

        /// <summary>
        /// Получить информацию об устройстве
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            string apiStr = "Web-портал Вкл -  [" + On + "]" +
                "\nАдрес [" + Domain + "]" +
                "\nАвторизация: \nLogin - ";

            if (String.IsNullOrEmpty(Login))
                apiStr += "[не установлен]";
            else apiStr += "[" + Login + "]";

            apiStr += "\nPassword - ";

            if (String.IsNullOrEmpty(Password))
                apiStr += "[не установлен]";
            else apiStr += "[" + Password + "]";

            return apiStr;
        }
    }
}
