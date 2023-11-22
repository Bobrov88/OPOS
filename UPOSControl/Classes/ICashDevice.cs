using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOSControl.Enums;

namespace UPOSControl.Classes
{
    interface ICashDevice
    {
        void SetDeviceMode(InterfaceType interfaceType);
    }
}
