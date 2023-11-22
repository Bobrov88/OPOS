using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using UPOSControl.Enums;
using Newtonsoft.Json;

namespace UPOSControl.Logging
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string KeyId { get; set; }

        [JsonProperty("type")]
        public LogType Type { get; set; } = LogType.Information;

        [JsonProperty("objectName")]
        public string ObjectName { get; set; } = "";

        [JsonProperty("className")]
        public string ClassName { get; set; } = "";
        [JsonProperty("timeRequest")]
        public DateTime TimeRequest { get; set; } = DateTime.Now;
        [JsonProperty("userName")]
        public string UserName { get; set; } = "";

        [JsonProperty("text")]
        public string Text { get; set; } = "";
        [JsonIgnore]
        public bool NewLine { get; set; } = true;
    }

    /// <summary>
    /// Класс запроса логов с Фронта
    /// </summary>
    public class LogsRequest
    {
        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }
        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }
    }
}