using Newtonsoft.Json;

namespace UPOSControl.Classes
{
    /// <summary>
    /// Переменная
    /// </summary>
    public class EthalonVariable
    {
        [JsonProperty("keyId")]
        public string KeyId { get; set; }
        [JsonProperty("secondKeyId")]
        public string SecondKeyId { get; set; }



        [JsonProperty("valueType")]
        public Enums.ValueType ValueType { get; set; } //Тип переменной
        [JsonProperty("commandType")]
        public Enums.CommandType CommandType { get; set; } //Тип комманды
        [JsonProperty("read")]
        public bool Read { get; set; } //Использование для чтения
        [JsonProperty("write")]
        public bool Write { get; set; } //Использование для записи 


        [JsonProperty("valueIsSetOnDevice")]
        public bool ValueIsSetOnDevice { get; set; } = true; //Значение установлено на устройстве


        [JsonProperty("group")]
        public string Group { get; set; } = ""; //Группа переменной
        [JsonProperty("name")]
        public string Name { get; set; } = ""; //Имя переменной
        [JsonProperty("text")]
        public string Text { get; set; } = ""; //Описание переменной



        [JsonProperty("hexValue")]
        public string HexValue { get; set; } = ""; //Значение комманды
        [JsonProperty("currentValue")]
        public string CurrentValue { get; set; } = ""; //Значение переменной устройства

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; } = ""; //Исходное значение переменной устройства



        [JsonProperty("minValue")]
        public double MinValue { get; set; } = 0; //Минимальное значение переменной
        [JsonProperty("maxValue")]
        public double MaxValue { get; set; } = 0; //Максимальное значение переменной
    }
}
