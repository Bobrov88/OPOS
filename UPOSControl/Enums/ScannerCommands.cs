using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Enums
{
    internal enum ScannerCommands
    {
        GetCapPowerReporting,
        GetCapStatisticsReporting,
        GetCapUpdateStatistics,
        GetCapCompareFirmwareVersion,
        GetCapUpdateFirmware,
        GetAutoDisable,
        SetAutoDisable,
        GetDataCount,
        GetDataEventEnabled,
        SetDataEventEnabled,
        GetDecodeData,
        SetDecodeData,
        GetScanData,
        GetScanDataLabel,
        GetScanDataType,
        GetPowerNotify,
        SetPowerNotify,
        GetPowerState,
        ClearInput,
        ReSetStatistics,
        RetrieveStatistics,
        UpdateStatistics,
        CompareFirmwareVersion,
        UpdateFirmware,
        ClearInputProperties
    }
}
