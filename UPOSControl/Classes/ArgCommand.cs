using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Classes
{
    /// <summary>
    /// Команда
    /// </summary>
    public class ArgCommand
    {
        public string[] commands;
        public string[] values;
        public int device;
        public int number;
        public string hex;

        public ArgCommand()
        {
            commands = null;
            values = null;
            device = -1;
            hex = "";
            number = -1;
        }
    }
}
