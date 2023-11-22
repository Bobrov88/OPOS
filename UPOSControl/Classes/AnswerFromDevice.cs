using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOSControl.Enums;

namespace UPOSControl.Classes
{
    public class AnswerFromDevice
    {
        public string Message { get; set; } = "";
        public string HexStr { get; set; } = "";
        public byte[] Response { get; set; }
        public int Length { get; set; } = 0;
        public bool HasAnswer { get; set; } = false;
        public Errors Error { get; set; } = 0;
        public byte[] Request { get; set; }
    }
}
