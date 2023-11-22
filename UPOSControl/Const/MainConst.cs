using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Const
{
    internal class MainConst
    {
        public static int S_CLOSED { get; } = 1;
        public static int S_IDLE { get; } = 2;
        public static int S_BUSY { get; } = 3;
        public static int S_ERROR { get; } = 4;


        public static int ERR { get; } = 100;
        public static int ERREXT { get; } = 200;

        public static int SUCCESS { get; } = 0;
        public static int E_CLOSED { get; } = 1 + ERR;
        public static int E_CLAIMED { get; } = 2 + ERR;
        public static int E_NOTCLAIMED { get; } = 3 + ERR;
        public static int E_NOSERVICE { get; } = 4 + ERR;
        public static int E_DISABLED { get; } = 5 + ERR;
        public static int E_ILLEGAL { get; } = 6 + ERR;
        public static int E_NOHARDWARE { get; } = 7 + ERR;
        public static int E_OFFLINE { get; } = 8 + ERR;
        public static int E_NOEXIST { get; } = 9 + ERR;
        public static int E_EXISTS { get; } = 10 + ERR;
        public static int E_FAILURE { get; } = 11 + ERR;
        public static int E_TIMEOUT { get; } = 12 + ERR;
        public static int E_BUSY { get; } = 13 + ERR;
        public static int E_EXTENDED { get; } = 14 + ERR;
        public static int E_DEPRECATED { get; } = 15 + ERR; 

        public static int ESTATS_ERROR { get; } = 80 + ERREXT;
        public static int EFIRMWARE_BAD_FILE { get; } = 81 + ERREXT;
        public static int ESTATS_DEPENDENCY { get; } = 82 + ERREXT;

        public static int BC_NONE { get; } = 0;
        public static int BC_NIBBLE { get; } = 1;
        public static int BC_DECIMAL { get; } = 2;

        public static int CH_INTERNAL { get; } = 1;
        public static int CH_EXTERNAL { get; } = 2;
        public static int CH_INTERACTIVE { get; } = 3;

        public static int PR_NONE { get; } = 0;
        public static int PR_STANDARD { get; } = 1;
        public static int PR_ADVANCED { get; } = 2;

        public static int PN_DISABLED { get; } = 0;
        public static int PN_ENABLED { get; } = 1;

        public static int PS_UNKNOWN { get; } = 2000;
        public static int PS_ONLINE { get; } = 2001;
        public static int PS_OFF { get; } = 2002;
        public static int PS_OFFLINE { get; } = 2003;
        public static int PS_OFF_OFFLINE { get; } = 2004;

        public static int CFV_FIRMWARE_OLDER { get; } = 1;
        public static int CFV_FIRMWARE_SAME { get; } = 2;
        public static int CFV_FIRMWARE_NEWER { get; } = 3;
        public static int CFV_FIRMWARE_DIFFERENT { get; } = 4;
        public static int CFV_FIRMWARE_UNKNOWN { get; } = 5;

        public static int EL_OUTPUT { get; } = 1;
        public static int EL_INPUT { get; } = 2;
        public static int EL_INPUT_DATA { get; } = 3;

        public static int ER_RETRY { get; } = 11;
        public static int ER_CLEAR { get; } = 12;
        public static int ER_CONTINUEINPUT { get; } = 13;

        public static int SUE_POWER_ONLINE { get; } = 2001;
        public static int SUE_POWER_OFF { get; } = 2002;
        public static int SUE_POWER_OFFLINE { get; } = 2003;
        public static int SUE_POWER_OFF_OFFLINE { get; } = 2004;

        public static int SUE_UF_PROGRESS { get; } = 2100;
        public static int SUE_UF_COMPLETE { get; } = 2200; 
        public static int SUE_UF_FAILED_DEV_OK { get; } = 2201;
        public static int SUE_UF_FAILED_DEV_UNRECOVERABLE { get; } = 2202;
        public static int SUE_UF_FAILED_DEV_NEEDS_FIRMWARE { get; } = 2203;
        public static int SUE_UF_FAILED_DEV_UNKNOWN { get; } = 2204;
        public static int SUE_UF_COMPLETE_DEV_NOT_RESTORED { get; } = 2205;

        public static int FOREVER { get; } = -1;

    }
}
