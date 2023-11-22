using Newtonsoft.Json;
using System;
using System.Net;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.API
{
    public class Api
    {
        [JsonProperty("on")]
        public bool On { get; set; } = false;
        [JsonProperty("port")]
        public int Port { get; set; } = 4455;
        [JsonProperty("ip")]
        public string Ip { get; set; } = "localhost";
        [JsonProperty("bearerKey")]
        public string BearerKey { get; set; } = "";
        [JsonProperty("basicKeys")]
        public string[] BasicKeys { get; set; }

        public void Init()
        {
            ConsoleManager.Add(LogType.Information, "Api", "Api", "Хотите настроить WEB API (y - да, n - нет)?");

            while (true) {
                string way = ConsoleManager.Read();
                if (way.Equals("n"))
                    break;
                else if (way.Equals("y"))
                {
                    SetAddress();
                    break;
                }
            }
        }

        private void SetAddress()
        {
            // Получение имени компьютера.
            string host = System.Net.Dns.GetHostName();
            // Получение ip-адреса.
            IPAddress[] ips = System.Net.Dns.GetHostEntry(host).AddressList;

            ConsoleManager.Add(LogType.Information, "Api", "SetAddress", "Доступные IP:");
            for (int i = 0; i < ips.Length; i++)
            {
                ConsoleManager.Add(LogType.Information, "Program", "SetAddress", String.Format("{0} - {1}", (i + 1).ToString(), ips[i].MapToIPv4().ToString()));
            }

            ConsoleManager.Add(LogType.Information, "Api", "SetAddress", "Выберите номер IP (по - умолчанию - localhost):");
            string ipAddrNum = Console.ReadLine();
            if (!String.IsNullOrEmpty(ipAddrNum) && int.TryParse(ipAddrNum, out int n))
            {
                Ip = ips[int.Parse(ipAddrNum) - 1].MapToIPv4().ToString();
            }

            ConsoleManager.Add(LogType.Information, "Api", "SetAddress", "Введите номер порта (по - умолчанию - 4455):");
            string portNum = Console.ReadLine();
            if (!String.IsNullOrEmpty(portNum) && int.TryParse(portNum, out int n1))
            {
                Port = int.Parse(portNum);
            }
        }

        /// <summary>
        /// Получить информацию об устройстве
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            string apiStr = "API Вкл -  [" + On + "]" +
                "\nАдрес [" + Ip + ":" + Port + "]" +
                "\nАвторизация, Bearer \nКлюч - ";

            if (String.IsNullOrEmpty(BearerKey))
                apiStr += "[не установлен]";
            else apiStr += "[" + BearerKey + "]";

            apiStr += "\nАвторизация, Basic - ";

            if(BasicKeys?.Length > 0)
            {
                apiStr += "установлено " + BasicKeys.Length + " ключей";

                for (int i = 0; i < BasicKeys.Length; i++)
                {
                    apiStr += "\n" + (i + 1) + " ключ - [" + BasicKeys[i] + "]";
                }
            }
            else
                apiStr += "[не установлен]";

            return apiStr;
        }
    }


}
