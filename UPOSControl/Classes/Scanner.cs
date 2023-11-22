using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Controls
{
    internal class Scanner
    {
        public bool AutoDisable { get; set; }
        public bool CapCompareFirmwareVersion { get; set; }
        public int CapPowerReporting { get; set; }
        public bool CapStatisticsReporting { get; set; }
        public bool CapUpdateFirmware { get; set; }
        public bool CapUpdateStatistics { get; set; }
        public string CheckHealthText { get; set; }
        public bool Claimed { get; set; }
        public int DataCount { get; set; }
        public bool DataEventEnabled { get; set; }
        public bool DeviceEnabled { get; set; }
        public bool FreezeEvents { get; set; }
        public int OutputID { get; set; }
        public int PowerNotify { get; set; }
        public int PowerState { get; set; }
        public int State { get; set; }

        public string DeviceControlDescription { get; set; }
        public int DeviceControlVersion { get; set; }
        public string DeviceServiceDescription { get; set; }
        public int DeviceServiceVersion { get; set; }
        public string PhysicalDeviceDescription { get; set; }
        public string PhysicalDeviceName { get; set; }


        public bool DecodeData { get; set; }
        public byte[] ScanData { get; set; }
        public byte[] ScanDataLabel { get; set; }
        public int ScanDataType { get; set; }
    }
}
