using Newtonsoft.Json;
using System;
using System.Reflection;
using UPOSControl.Enums;

namespace UPOSControl.Classes
{
    public class PosCommand
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("command")]
        public string Command { get; set; }
        [JsonProperty("commands")]
        public string[] Commands { get; set; }
        [JsonProperty("type")]
        public CommandType Type { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
