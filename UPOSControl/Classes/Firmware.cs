using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace UPOSControl.Classes
{
    public class Firmware
    {
        [JsonProperty("keyId")]
        public string KeyId { get; set; }
        [JsonProperty("secondKeyId")]
        public string SecondKeyId { get; set; }



        [JsonProperty("name")]
        public string Name { get; set; } = ""; //Имя прошивки
        [JsonProperty("text")]
        public string Text { get; set; } = ""; //Описание прошивки
        [JsonProperty("file")]
        public File File { get; set; } //Файл прошивки
    }
}
