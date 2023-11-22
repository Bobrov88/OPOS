using MonoLibUsb;
using MonoLibUsb.Profile;
using System;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.USB
{
    internal class MonoLibUsbShowInfo
    {
        private static MonoUsbSessionHandle __sessionHandle;

        public static MonoUsbSessionHandle Session
        {
            get
            {
                if (ReferenceEquals(__sessionHandle, null))
                    __sessionHandle = new MonoUsbSessionHandle();
                return __sessionHandle;
            }
        }

        /// <summary>
        /// Получить список устройств
        /// </summary>
        /// <param name="quietly"></param>
        /// <returns></returns>
        public MonoUsbProfileList GetProfiles(bool quietly = false)
        {
            int ret;
            MonoUsbProfileList profileList = null;

            // Initialize the context.
            if (Session.IsInvalid)
                throw new Exception(String.Format("Ошибка инициализации libusb. {0}:{1}",
                                                  MonoUsbSessionHandle.LastErrorCode,
                                                  MonoUsbSessionHandle.LastErrorString));

            MonoUsbApi.SetDebug(Session, 0);
            profileList = new MonoUsbProfileList();

            ret = profileList.Refresh(Session);
            if (ret < 0)
                throw new Exception("Не удалось получить лист устройств.");

            if (!quietly)
            {
                ConsoleManager.Add(LogType.Information, "MonoLibUsbShowInfo", "GetProfiles", String.Format("{0} устройств найдено.", ret));
                Console.WriteLine("{0} device(s) found.", ret);

                int counter = 0;
                foreach (MonoUsbProfile profile in profileList)
                {
                    counter++;
                    ConsoleManager.Add(LogType.Information, "MonoLibUsbShowInfo", "GetProfiles", String.Format("USB устройство - {0}", counter));
                    ConsoleManager.Add(LogType.Information, "MonoLibUsbShowInfo", "GetProfiles", String.Format("Адрес устройства: {0}.{1}", profile.BusNumber, profile.DeviceAddress));
                    ConsoleManager.Add(LogType.Information, "MonoLibUsbShowInfo", "GetProfiles", profile.DeviceDescriptor.ToString());
                }
            }

            Session.Close();

            return profileList;
        }

    }
}
