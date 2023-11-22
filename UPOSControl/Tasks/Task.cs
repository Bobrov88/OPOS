using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UPOSControl.Classes;
using UPOSControl.Enums;

namespace UPOSControl.Tasks
{
    public class Task
    {
        [JsonProperty("keyId")]
        public string KeyId { get; set; }
        [JsonProperty("secondKeyId")]
        public string SecondKeyId { get; set; }



        [JsonProperty("cashDeviceKeyId")]
        public string CashDeviceKeyId { get; set; } //Ключ родителя



        [JsonProperty("name")]
        public string Name { get; set; } = ""; //Имя переменной
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; } = ""; //Картинка
        [JsonProperty("text")]
        public string Text { get; set; } = ""; //Описание



        [JsonProperty("reason")]
        public string Reason { get; set; } = ""; //Причина



        [JsonProperty("status")]
        public Status Status { get; set; } = Status.isWait; //Статус задания



        [JsonProperty("start")]
        public DateTime Start { get; set; } //Дата выполнения заказа
        [JsonProperty("repeateType")]
        public RepeateType RepeateType { get; set; } = RepeateType.NOTREPEAT; //Тип для установки периода
        [JsonProperty("repeatePeriod")]
        public int RepeatePeriod { get; set; } = 0; //Период
        [JsonProperty("repeateCount")]
        public int RepeateCount { get; set; } = 0; //Повторять количество раз
        [JsonProperty("repeateWhileComplete")]
        public bool RepeateWhileComplete { get; set; } = true; //Повторять пока не будет успешно



        [JsonProperty("create")]
        public DateTime Create { get; set; } //Время создания заказа
        [JsonProperty("update")]
        public DateTime Update { get; set; } //Время изменения



        [JsonProperty("variables")]
        public List<EthalonVariable> Variables { get; set; } //Значения переменных для устройства
        [JsonProperty("taskTypeVariables")]
        public TaskType TaskTypeVariables { get; set; } = TaskType.NOTHING; //Установить значения или получить



        [JsonProperty("firmwareKeyId")]
        public string FirmwareKeyId { get; set; } //Ключ прошивки
        [JsonProperty("firmware")]
        public Firmware Firmware { get; set; } //Прошивка
        [JsonProperty("taskTypeFirmware")]
        public TaskType TaskTypeFirmware { get; set; } = TaskType.NOTHING; //Перепрошить
    }
}
