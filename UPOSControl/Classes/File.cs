using UPOSControl.Enums;
using Newtonsoft.Json;
using System;

namespace UPOSControl.Classes
{
    public class File
    {
        [JsonProperty("keyId")]
        public string KeyId { get; set; }


        [JsonProperty("name")]
        public string Name { get; set; } //Оригинальное имя
        [JsonProperty("extension")]
        public string Extension { get; set; } //Разрешение без точки
        [JsonProperty("path")]
        public string Path { get; set; } //Полный путь к файлу в хранилище
        [JsonProperty("size")]
        public double Size { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("type")]
        public FileType Type { get; set; }
        [JsonProperty("keyOwner")]
        public string KeyOwner { get; set; }
        [JsonProperty("create")]
        public DateTime Create { get; set; } //Время создания файла
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; } //Иконка файла, нужна для отображения в чате
        [JsonProperty("verify")]
        public bool Verify { get; set; } //Подтвердить
    }
}
