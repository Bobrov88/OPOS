using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UPOSControl.API;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.Tasks;
using UPOSControl.Utils;

namespace UPOSControl.Classes
{
    /// <summary>
    /// Конфигурация
    /// </summary>
    public class UPOSConfiguration
    {
        [JsonProperty("devices")]
        public List<CashDevice> Devices { get; set; }


        [JsonProperty("logsToWrite")]
        public List<LogType> LogsToWrite { get; set; }
        [JsonProperty("logsPeriod")]
        public int LogsPeriod { get; set; } = 10;


        [JsonProperty("searchPeriod")]
        public int SearchPeriod { get; set; } = 2000;


        [JsonProperty("api")]
        public Api Api { get; set; }


        [JsonProperty("webPortal")]
        public WebPortalSetting WebPortal { get; set; }


        [JsonProperty("commands")]
        public List<PosCommand> Commands { get; set; }



        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("stopTime")]
        public DateTime StopTime { get; set; }
        [JsonProperty("stopCorrectly")]
        public bool StopCorrectly { get; set; } = false;


        public void Create()
        {
            ConsoleManager.Add(LogType.Information, "UPOSConfiguration", "Create", "Создание новой конфигурации.");

            Devices = new List<CashDevice>();
            Api = new Api();
            WebPortal = new WebPortalSetting();

        }


        /// <summary>
        /// Добавить устройство в конец списка
        /// </summary>
        /// <param name="device"></param>
        internal void AddDevice(CashDevice device)
        {
            try
            {
                if (device == null)
                    throw new Exception("Экземпляр не может быть null.");

                if (Devices == null)
                    Devices = new List<CashDevice>();

                if (Devices.Count > 0)
                {
                    //Проверка на копию
                    foreach (CashDevice deviceItem in Devices)
                    {
                        if (device.Usb != null && deviceItem.Usb != null)
                            if(device.Usb.IsConfigured())
                                if (deviceItem.Usb.VIDs == device.Usb.VIDs && deviceItem.Usb.PIDs == device.Usb.PIDs && deviceItem.Usb.BusNumber == device.Usb.BusNumber && deviceItem.Usb.DeviceAddress == device.Usb.DeviceAddress)
                                    throw new Exception(String.Format("Устройство с идентичными VIDs {0} и PIDs {1} и {2} уже есть в списке.", device.Usb.VIDs, device.Usb.PIDs, device.Usb.BusNumber + "." + device.Usb.DeviceAddress));

                        if(device.Com != null && deviceItem.Com != null)
                            if(!String.IsNullOrEmpty(device.Com.PortName))
                                if (deviceItem.Com.PortName == device.Com.PortName)
                                    throw new Exception(String.Format("Устройство {0} уже есть в списке.", device.Com?.PortName));
                    }

                    if (device.Number == 0)
                    {
                        Devices = Util.SortListByIntToUpWay(Devices);
                        device.Number = Devices.Last().Number + 1;
                    }
                }

                Devices.Add(device);
            }
            catch(Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSConfiguration", "AddDevice", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Удалить устройство из списка
        /// </summary>
        /// <param name="deviceNumber"></param>
        internal void RemoveDevice(int deviceNumber = 0)
        {
            try
            {
                if (deviceNumber < 1)
                    throw new Exception("Неправильно задан номер.");

                if (Devices == null || Devices?.Count == 0)
                    throw new Exception("Список устройств пустой.");

                Devices.RemoveAll(pred => pred.Number == deviceNumber);

                //Увеличиваем номер на один
                if (Devices.Count > 0)
                {
                    Devices = Devices.AsEnumerable().Where(pred => pred.Number > deviceNumber).Select(pred2 => { pred2.Number--; return pred2; }).ToList();
                }

                Devices = Util.SortListByIntToUpWay(Devices);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSConfiguration", "RemoveDevice", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Получить список устройств
        /// </summary>
        /// <returns></returns>
        internal string GetJsonDevices()
        {
            return JsonConvert.SerializeObject(Devices, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Получить настройки API
        /// </summary>
        /// <returns></returns>
        internal string GetJsonAPI()
        {
            return JsonConvert.SerializeObject(Api, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Получить настройки web-портала
        /// </summary>
        /// <returns></returns>
        internal string GetJsonWebPortal()
        {
            return JsonConvert.SerializeObject(WebPortal, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Добавить типы логов
        /// </summary>
        /// <param name="logTypes"></param>
        internal void AddLogTypes(List<LogType> logTypes)
        {
            try
            {
                if (logTypes == null)
                    throw new Exception("Список не может быть null.");

                if (logTypes.Count == 0)
                    throw new Exception("Список не может быть пустым.");

                if (LogsToWrite == null)
                    LogsToWrite = new List<LogType>();

                foreach(LogType logType in logTypes)
                {
                    if (!LogsToWrite.Contains(logType))
                        LogsToWrite.Add(logType);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSConfiguration", "AddLogTypes", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Удалить типы логов из списка
        /// </summary>
        /// <returns></returns>
        internal void RemoveLogTypes(List<LogType> logTypes)
        {
            try
            {
                if (logTypes == null)
                    throw new Exception("Список не может быть null.");

                if (logTypes.Count == 0)
                    throw new Exception("Список не может быть пустым.");

                if (LogsToWrite?.Count > 0)
                {
                    foreach(LogType logType in logTypes)
                    {
                        LogsToWrite.Remove(logType);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSConfiguration", "RemoveLogTypes", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Получить настройки 
        /// </summary>
        /// <returns></returns>
        internal string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Установить значения устройств из данных web-портала
        /// </summary>
        /// <param name="devices"></param>
        internal void SetDevices(List<CashDevice> devices)
        {
            if(Devices?.Count > 0 && devices?.Count > 0)
            {
                foreach(CashDevice device in devices)
                {
                    Devices.Where(p =>
                    {

                        if (!String.IsNullOrEmpty(device.SerialNumber))
                            if (p.SerialNumber == device.SerialNumber)
                                return true;

                        if (!String.IsNullOrEmpty(device.KeyId))
                            if (p.KeyId == device.KeyId)
                                return true;

                        if (!String.IsNullOrEmpty(device.SecondKeyId))
                            if (p.SecondKeyId == device.SecondKeyId)
                                return true;

                        return false;

                    }).Select(pred =>
                    {

                        if (!String.IsNullOrEmpty(device.KeyId))
                            if (pred.KeyId != device.KeyId)
                                pred.KeyId = device.KeyId;
                        if (!String.IsNullOrEmpty(device.SecondKeyId))
                            if (pred.SecondKeyId != device.SecondKeyId)
                                pred.SecondKeyId = device.SecondKeyId;
                        if (!String.IsNullOrEmpty(device.EthalonKeyId))
                            if (pred.EthalonKeyId != device.EthalonKeyId)
                            {
                                pred.EthalonKeyId = device.EthalonKeyId;
                            }

                        if (!String.IsNullOrEmpty(device.Name))
                            if (pred.Name != device.Name)
                                pred.Name = device.Name;
                        if (!String.IsNullOrEmpty(device.Thumbnail))
                            if (pred.Thumbnail != device.Thumbnail)
                                pred.Thumbnail = device.Thumbnail;
                        if (!String.IsNullOrEmpty(device.Text))
                            if (pred.Text != device.Text)
                                pred.Text = device.Text;

                        if (!String.IsNullOrEmpty(device.AuthorId))
                            if (pred.AuthorId != device.AuthorId)
                                pred.AuthorId = device.AuthorId;

                        if (!String.IsNullOrEmpty(device.Group))
                            if (pred.Group != device.Group)
                                pred.Group = device.Group;
                        if (!String.IsNullOrEmpty(device.Type))
                            if (pred.Type != device.Type)
                                pred.Type = device.Type;
                        
                        if (!String.IsNullOrEmpty(device.CompanyName))
                            if (pred.CompanyName != device.CompanyName)
                                pred.CompanyName = device.CompanyName;
                        if (!String.IsNullOrEmpty(device.ModelName))
                            if (pred.ModelName != device.ModelName)
                                pred.ModelName = device.ModelName;

                        if (!String.IsNullOrEmpty(device.Address))
                            if (pred.Address != device.Address)
                                pred.Address = device.Address;

                        //Переменные
                        if(device.Variables?.Count > 0 )
                        {
                            foreach(EthalonVariable variable in device.Variables)
                            {
                                //Создаём список, если он отсутствует
                                if (pred.Variables == null)
                                    pred.Variables = new List<EthalonVariable>();

                                //Если переменная отсутствует, то создать новую
                                if (pred.Variables.FirstOrDefault(predV => predV.KeyId == variable.KeyId) == null)
                                    pred.Variables.Add(variable);
                                else
                                {
                                    //Не меняем значения переменных
                                    pred.Variables.Where(predV => predV.KeyId == variable.KeyId).Select(predV2 =>
                                    {
                                        if (!String.IsNullOrEmpty(variable.Group))
                                            if (predV2.Group != variable.Group)
                                                predV2.Group = variable.Group;

                                        if (!String.IsNullOrEmpty(variable.Name))
                                            if (predV2.Name != variable.Name)
                                                predV2.Name = variable.Name;

                                        if (!String.IsNullOrEmpty(variable.Text))
                                            if (predV2.Text != variable.Text)
                                                predV2.Text = variable.Text;

                                        if (predV2.ValueType != variable.ValueType)
                                            predV2.ValueType = variable.ValueType;

                                        return predV2;
                                    }).ToList();
                                }
                            }
                        }

                        //Присваиваем объект прошивки, если он отсутствует
                        if(pred.Firmware == null)
                            if(device.Firmware != null)
                            {
                                pred.Firmware = device.Firmware;
                            }

                        return pred;
                    }).ToList();
                }
            }
        }

        /// <summary>
        /// Установить значения устройств из данных web-портала
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="variable"></param>
        internal void SetVariable(string deviceKey, EthalonVariable variable)
        {
            if (Devices?.Count > 0)
            {
                Devices.Where(p => p.SerialNumber == deviceKey || p.KeyId == deviceKey || p.SecondKeyId == deviceKey).Select(pred =>
                {
                    //Создаём список, если он отсутствует
                    if (pred.Variables == null)
                        pred.Variables = new List<EthalonVariable>();

                    //Если переменная отсутствует, то создать новую
                    if (pred.Variables.FirstOrDefault(predV => predV.KeyId == variable.KeyId) == null)
                        pred.Variables.Add(variable);
                    else
                    {
                        //Не меняем значения переменных
                        pred.Variables.Where(predV1 => predV1.KeyId == variable.KeyId).Select(predV2 =>
                        {
                            if (predV2.CurrentValue != variable.CurrentValue)
                                predV2.CurrentValue = variable.CurrentValue;

                            return predV2;
                        }).ToList();

                    }

                    return pred;

                }).ToList();
            }
        }
    }
}
