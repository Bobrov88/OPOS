using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UPOSControl.Const;

namespace UPOSControl.USB
{
    class ScannerInitializer
    {
        public static short ACCESSORY_STRING_MANUFACTURER = 0;
        public static short ACCESSORY_STRING_MODEL = 1;
        public static short ACCESSORY_STRING_DESCRIPTION = 2;
        public static byte ACCESSORY_STRING_VERSION = 3;
        public static byte ACCESSORY_STRING_URI = 4;
        public static byte ACCESSORY_STRING_SERIAL = 5;
        public static byte ACCESSORY_GET_PROTOCOL = 51;
        public static byte ACCESSORY_SEND_STRING = 52;
        public static byte ACCESSORY_START = 53;
        public delegate bool Step();

        public static bool Initialize(UsbRegistry usbDevice)
        {
            bool initialized = false;
            UsbDevice.ForceLibUsbWinBack = true;
            UsbDevice TempMyUsbDevice = null;
            try
            {
                if (!usbDevice.Open(out TempMyUsbDevice))
                {
                    return false;
                }

                // Если устройство открыто и готово
                if (TempMyUsbDevice != null && TempMyUsbDevice.IsOpen)
                {
                    initialized = InitializeDeviceConnectionAccessoryMode(TempMyUsbDevice);
                }
            }
            catch (Exception) { }

            if (TempMyUsbDevice != null && TempMyUsbDevice.IsOpen)
                TempMyUsbDevice.Close();

            return initialized;     
        }

        public static bool InitializeDeviceConnectionAccessoryMode(UsbDevice device)
        {
            List<Step> steps = new List<Step>
            {
                () => device != null,
                () => CheckProtocol(device),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_MANUFACTURER, "UPOSControl"),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_MODEL, "Adapter"),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_DESCRIPTION, "Windows POS Device"),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_VERSION, "1.0"),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_URI, "https://minakovprog.com"),
                () => SendControlMessage(device, ACCESSORY_SEND_STRING, ACCESSORY_STRING_SERIAL, "MPUPOSC0001"),
                () => SendControlMessage(device, ACCESSORY_START, 0, ""),
            };

            return steps.All(step => step());
        }

        private static bool SendControlMessage(UsbDevice device, byte requestCode, short index, string message)
        {
            short messageLength = 0;

            if (message != null)
            {
                messageLength = (short)message.Length;
            }

            UsbSetupPacket setupPacket = new UsbSetupPacket();
            setupPacket.RequestType = (byte)((byte)UsbConst.USB_DIR_OUT | (byte)UsbConst.USB_TYPE_VENDOR);

            setupPacket.Request = requestCode;
            setupPacket.Value = 0;
            setupPacket.Index = index;
            setupPacket.Length = messageLength;

            byte[] messageBytes = null;
            if (null != message)
            {
                messageBytes = Encoding.UTF8.GetBytes(message);
            }

            bool result = device.ControlTransfer(ref setupPacket, messageBytes, messageLength, out int resultTransferred);
            return result;
        }

        private static bool CheckProtocol(UsbDevice device)
        {
            byte[] message = new byte[2];
            short messageLength = 2;

            UsbSetupPacket setupPacket = new UsbSetupPacket();
            setupPacket.RequestType = (byte)(UsbConst.USB_DIR_IN | UsbConst.USB_TYPE_VENDOR);
            setupPacket.Request = ACCESSORY_GET_PROTOCOL;
            setupPacket.Value = 0;
            setupPacket.Index = 0;
            setupPacket.Length = 0;

            bool result = device.ControlTransfer(ref setupPacket, message, messageLength, out int resultTransferred);

            return result;
        }
    }
}
