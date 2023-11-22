using Newtonsoft.Json;

namespace UPOSControl.Http
{
    class ResponseModel
    {
        [JsonProperty("api")]
        public string API { get; set; }
        [JsonProperty("response")]
        public string Response { get; set; }
    }
}
