using LibUsbDotNet.Main;
using MonoLibUsb;
using MonoLibUsb.Descriptors;
using MonoLibUsb.Profile;
using System;
using System.Collections.Generic;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.USB
{
    internal class MonoUsbShowConfig
    {
        private static MonoUsbSessionHandle sessionHandle;

        // Predicate functions for finding only devices with the specified VendorID & ProductID.
        private static bool MyVidPidPredicate(MonoUsbProfile profile, int pid, int vid)
        {
            if (profile.DeviceDescriptor.VendorID == vid && profile.DeviceDescriptor.ProductID == pid)
                return true;
            return false;
        }

        public void ShowConfig(int pid = 0, int vid = 0)
        {
            // Initialize the context.
            sessionHandle = new MonoUsbSessionHandle();
            if (sessionHandle.IsInvalid)
                throw new Exception(String.Format("Ошибка инициализации libusb. {0}:{1}",
                                                  MonoUsbSessionHandle.LastErrorCode,
                                                  MonoUsbSessionHandle.LastErrorString));

            MonoUsbProfileList profileList = new MonoUsbProfileList();

            // The list is initially empty.
            // Each time refresh is called the list contents are updated. 
            int ret = profileList.Refresh(sessionHandle);
            if (ret < 0) 
                throw new Exception("Не удалось получить лист устройств.");
            ConsoleManager.Add(LogType.Information, "MonoUsbShowConfig", "ShowConfig", String.Format("{0} устройств найдено.", ret));
            
            List<MonoUsbProfile> myVidPidList;
            // Use the GetList() method to get a generic List of MonoUsbProfiles
            // Find all profiles that match in the MyVidPidPredicate.
            if (vid != 0 && pid != 0)
                myVidPidList = profileList.GetList().FindAll(p => MyVidPidPredicate(p, pid, vid));
            else
                myVidPidList = profileList.GetList();

            // myVidPidList reresents a list of connected USB devices that matched
            // in MyVidPidPredicate.
            foreach (MonoUsbProfile profile in myVidPidList)
            {
                // Write the VendorID and ProductID to console output.
                ConsoleManager.Add(LogType.Information, "MonoUsbShowConfig", "ShowConfig", String.Format("[Устройство] Vid:{0:X4} Pid:{1:X4}", profile.DeviceDescriptor.VendorID, profile.DeviceDescriptor.ProductID));

                // Loop through all of the devices configurations.
                for (byte i = 0; i < profile.DeviceDescriptor.ConfigurationCount; i++)
                {
                    // Get a handle to the configuration.
                    MonoUsbConfigHandle configHandle;
                    if (MonoUsbApi.GetConfigDescriptor(profile.ProfileHandle, i, out configHandle) < 0) continue;
                    if (configHandle.IsInvalid) continue;

                    // Create a MonoUsbConfigDescriptor instance for this config handle.
                    MonoUsbConfigDescriptor configDescriptor = new MonoUsbConfigDescriptor(configHandle);

                    // Write the bConfigurationValue to console output.
                    ConsoleManager.Add(LogType.Information, "MonoUsbShowConfig", "ShowConfig", String.Format("  [Config] bConfigurationValue:{0}", configDescriptor.bConfigurationValue));
                    
                    // Interate through the InterfaceList
                    foreach (MonoUsbInterface usbInterface in configDescriptor.InterfaceList)
                    {
                        // Interate through the AltInterfaceList
                        foreach (MonoUsbAltInterfaceDescriptor usbAltInterface in usbInterface.AltInterfaceList)
                        {
                            // Write the bInterfaceNumber and bAlternateSetting to console output.
                            ConsoleManager.Add(LogType.Information, "MonoUsbShowConfig", "ShowConfig", String.Format("    [Interface] bInterfaceNumber:{0} bAlternateSetting:{1}",
                                              usbAltInterface.bInterfaceNumber,
                                              usbAltInterface.bAlternateSetting));
                            
                            // Interate through the EndpointList
                            foreach (MonoUsbEndpointDescriptor endpoint in usbAltInterface.EndpointList)
                            {
                                // Write the bEndpointAddress, EndpointType, and wMaxPacketSize to console output.
                                ConsoleManager.Add(LogType.Information, "MonoUsbShowConfig", "ShowConfig", String.Format("      [Endpoint] bEndpointAddress:{0:X2} EndpointType:{1} wMaxPacketSize:{2}",
                                                  endpoint.bEndpointAddress,
                                                  (EndpointType)(endpoint.bmAttributes & 0x3),
                                                  endpoint.wMaxPacketSize));
                            }
                        }
                    }
                    // Not neccessary, but good programming practice.
                    configHandle.Close();
                }
            }
            // Not neccessary, but good programming practice.
            profileList.Close();
            // Not neccessary, but good programming practice.
            sessionHandle.Close();
        }

    }

    
}
