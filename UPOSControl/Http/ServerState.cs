using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UPOSControl.Http
{
    public class ServerState
    {
        [JsonProperty("applicationUserId")]
        public string ApplicationUserId { get; set; } //Id ключ пользователя


        [JsonProperty("serviceMode")]
        public bool ServiceMode { get; set; } //Сервисный режим, вкл/выкл
        [JsonProperty("demoMode")]
        public bool DemoMode { get; set; } //Демо режим, вкл/выкл
        [JsonProperty("responseCacheTime")]
        public int ResponseCacheTime { get; set; } //Время кэширования ответа клиенту
        [JsonProperty("cachingChangeState")]
        public bool СachingChangeState { get; set; } = false; //Состояние изменения времени кэширования
        [JsonProperty("updateInterval")]
        public int UpdateInterval { get; set; } = 300; //Интервал обновления данных в секундах


        [JsonProperty("authorized")]
        public bool Authorized { get; set; } //Проверка на авторизацию пользователя (авторизован или нет)



        [JsonProperty("emailService")]
        public bool EmailService { get; set; } = false; //Почтовый сервис


        [JsonProperty("smsService")]
        public bool SmsService { get; set; } = false; //Sms сервис


        [JsonProperty("confirm")]
        public string Confirm { get; set; } = ""; //Пользовательское соглашение
    }
}
