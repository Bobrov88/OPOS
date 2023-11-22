using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UPOSControl.Classes;

namespace UPOSControl.Managers
{
    /// <summary>
    /// Конфигурация
    /// </summary>
    public class EthalonManager
    {
        [JsonProperty("ethalons")]
        public static List<CashDevice> Ethalons { get; set; }

        public static bool Update { get; set; } = false;

        public static EthalonVariable GetEthalonVariable(string ethalonKeyId, string variableName)
        {
            if(Ethalons?.Count > 0)
            {
                CashDevice ethalon = Ethalons.FirstOrDefault(p => p.KeyId == ethalonKeyId);
                if (ethalon != null)
                    return ethalon.Variables?.FirstOrDefault(p => p.Name.ToLower() == variableName.ToLower());
            }

            return null;
        }

    }
}
