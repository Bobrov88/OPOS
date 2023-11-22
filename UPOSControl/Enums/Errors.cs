using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Enums
{
    public enum Errors
    {
        Success = 0,
        ErrorIO = -1,
        ErrorInvalidParam = -2,
        ErrorAccess = -3,
        ErrorNoDevice = -4,
        ErrorNotFound = -5,
        ErrorBusy = -6,
        ErrorTimeout = -7,
        ErrorOverflow = -8,
        ErrorPipe = -9,
        ErrorInterrupted = -10,
        ErrorNoMem = -11,
        ErrorNotSupported = -12,
        ErrorIOCancelled = -13,
        ErrorOther = -99
    }
}
