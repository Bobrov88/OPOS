using System;
using UPOSControl.Enums;
using UPOSControl.Classes;
using System.Timers;
using System.Threading.Tasks;
using System.IO;
using UPOSControl.Utils;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using LibUsbDotNet.Main;
using System.Linq;
using LibUsbDotNet;
using System.Reflection;
using UPOSControl.API;
using UPOSControl.USB;
using UPOSControl.Tasks;
using System.Text;

namespace UPOSControl.Managers
{
    public class ScannerManager
    {
        private static bool _configured { get; set; } = false;

        private static PosCommands _posCommands;
        private static UPOSConfiguration _configuration { get; set; }

        private static Thread _searchDevicesThread;
        private static System.Timers.Timer _searchDevicesTimer;

        private static List<string> _checkedComPorts { get; set; }
        private static List<UsbRegistry> _checkedUsb { get; set; }

        private static FileManager _fileManager;

        public delegate void ReturnAnswerFromDevice(AnswerFromDevice answer);
        public delegate UsbRegDeviceList GetUsbDevicesDelegate();

        public static GetUsbDevicesDelegate _getUsbDevicesDelegate;

        private static ApiController _apiController;

        private static bool bUpdateComplete = false;

        private static CancellationTokenSource _cts;

        private static AnswerFromDevice _answerFromDevice;

        private static bool StopSearchDevicesTimer = false;


        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="configuration"></param>
        public void Init(UPOSConfiguration configuration)
        {
            if (_configured)
                return;

            _posCommands = new PosCommands();
            _posCommands.Init();
            _configuration = configuration;

            //Добавляем новые команды
            if (configuration.Commands?.Count > 0)
                _posCommands.AddCommand(configuration.Commands);

            _fileManager = new FileManager();
            _getUsbDevicesDelegate = new GetUsbDevicesDelegate(GetUsbRegDeviceList);

            //Запускаем настроенные устройства
            int countDevices = _configuration.Devices.Count;
            if (countDevices > 0)
            {
                ConsoleManager.Add(LogType.Information, "ScannerManager", "ScannerManager", String.Format("Сконфигурированных устройств - {0}.", countDevices));

                foreach (CashDevice device in _configuration.Devices)
                {
                    switch (device.Interface)
                    {
                        case InterfaceType.USBVIRTUALCOM:
                        case InterfaceType.RS232:
                            {
                                device.Com.Configure(false, device.Com.PortName);
                                device.Start();
                            }
                            break;
                        case InterfaceType.USBHIDPOS:
                        case InterfaceType.USBKBW:
                            {
                                device.Usb.Configure(false);
                                device.Start();
                            }
                            break;
                    }
                }
            }
            else
                ConsoleManager.Add(LogType.Information, "ScannerManager", "ScannerManager", "Сконфигурированные устройства отсутствуют.");

            if (_configuration.SearchPeriod > 0)
            {
                //Запускаем поток для поиска новых устройств
                _searchDevicesThread = new Thread(StartFindingDevices);
                _searchDevicesThread.Start(_configuration.SearchPeriod);
            }

            if (_configuration.Api != null)
            {
                if (_configuration.Api.On)
                {
                    _apiController = new ApiController();
                    _apiController.Start(_configuration.Api, this);
                }
            }

            _configured = true;
        }

        public static bool IsConfigured()
        {
            return _configured;
        }

        /// <summary>
        /// Создать новое устройство
        /// </summary>
        /// <param name="newDevice"></param>
        private void CreateDevice(CashDevice newDevice = null)
        {
            ConsoleManager.Add(LogType.Information, "ScannerManager", "CreateDevice", "Создание нового устройства");

            if (newDevice == null)
            {
                newDevice = new CashDevice();
                newDevice.Create();
            }

            bool result = newDevice.Start();
            _configuration.AddDevice(newDevice);
        }

        /// <summary>
        /// Возвращает список только активных устройств с серийным номером
        /// </summary>
        /// <returns></returns>
        public List<CashDevice> GetDevices()
        {
            return _configuration.Devices.Where(p => p.Interface != InterfaceType.UNKNOWN && !String.IsNullOrEmpty(p.SerialNumber)).ToList();
        }


        /// <summary>
        /// Обновить данные устройства
        /// </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        public bool SetDevices(List<CashDevice> devices)
        {
            if (_configuration != null)
            {
                _configuration.SetDevices(devices);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Удалить устройство
        /// </summary>
        /// <param name="deviceNumber"></param>
        private void RemoveDevice(int deviceNumber)
        {
            if(_configuration.Devices?.Count == 0)
                ConsoleManager.Add(LogType.Information, "ScannerManager", "RemoveDevice", "Список пустой.");
            
            CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
            if(device == null)
                ConsoleManager.Add(LogType.Information, "ScannerManager", "RemoveDevice", "Устройство не найдено.");

            device.Stop();

            _configuration.Devices.Remove(device);
        }

        /// <summary>
        /// Начать прослушку порта
        /// </summary>
        /// <param name="searchPeriod"></param>
        private void StartFindingDevices(object searchPeriod)
        {
            ConsoleManager.Add(LogType.Information, "ScannerManager", "StartFindingDevices", "Запуск поиска устройств.");

            _checkedComPorts = new List<string>();
            _checkedUsb = new List<UsbRegistry>();
            UsbDevice.ForceLibUsbWinBack = true;
            Console.WriteLine("200");
            //Запуск таймера.
            _searchDevicesTimer = new System.Timers.Timer();
            _searchDevicesTimer.Interval = (int)searchPeriod;
            _searchDevicesTimer.Elapsed += new ElapsedEventHandler(CheckDevices);
            _searchDevicesTimer.Enabled = true;
            Console.WriteLine("206");
        }

        /// <summary>
        /// Проверка устройств
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CheckDevices(object sender, EventArgs e)
        {
            /*
            _searchDevicesTimer.Enabled = false;

            if (StopSearchDevicesTimer)
                return;

            //Без эталонов система не работает
            if (EthalonManager.Ethalons?.Count > 0)
            {
                //Пробуем включить уже сконфигурированные устройства
                if (_configuration.Devices.Count > 0)
                {
                    foreach (CashDevice device in _configuration.Devices)
                    {
                        switch (device.Interface)
                        {
                            case InterfaceType.UNKNOWN:
                                {
                                    bool result = device.Start();
                                    if (result)
                                        device.NeedToReload = false;
                                }
                                break;
                        }
                    }
                }

                //Поиск новых устройств
                await CheckComDevices();
                await CheckUsbDevices();
            }
            else
            {
                //Пробуем настроить web-портал
                if (!_configuration.WebPortal.On && !_configuration.WebPortal.Initialized)
                {
                    Program.StopCMD();

                    if (_configuration.WebPortal.Init())
                    {
                        TaskManager _taskManager = new TaskManager();
                        _taskManager.Start(_configuration.WebPortal, this);
                        _configuration.WebPortal.Initialized = true;
                    }

                    Program.StartCMD();
                }
            }
            */
            //  _searchDevicesTimer.Enabled = true;
            //   Console.WriteLine("266");
            string str = Console.ReadLine();
            string[] serials = { str };
            await CheckComDevices(serials);
            await CheckUsbDevices();
        }

        /// <summary>
        /// Проверка COM устройств
        /// Проверяет только новые порты
        /// </summary>
        private async System.Threading.Tasks.Task CheckComDevices(string[] serials)
        {
            //  string[] serials = SerialPort.GetPortNames();
           // Console.WriteLine("Enter comport >>>");
           // string str = Console.ReadLine();
          //  string[] serials = { str };
       //     Console.WriteLine("278 " + serials[0]);
       //     Console.WriteLine("279 " + serials[1]);
         //   Console.WriteLine("280 " + serials[2]);

            //Удаляем порты, которые отключили
            if (_checkedComPorts.Count > 0)
                _checkedComPorts = _checkedComPorts.AsEnumerable().Where(pred => serials.Contains(pred)).ToList();

            //Получаем все порты из конфигурации
            List<string> serialsInConfigure = _configuration.Devices.AsEnumerable().Where(pred => pred.Com.IsConfigured()).Select(pred => { return pred.Com.PortName; }).ToList();

       //     Console.WriteLine("289 " + serialsInConfigure.Count);
            //  Console.WriteLine("289 " + serialsInConfigure[0]);
            //  Console.WriteLine("290 " + serialsInConfigure[1]);

            foreach (string comName in serials)
            {
                //Проверяем новый порт, если его нет в конфигурации
                if (!_checkedComPorts.Contains(comName) && !serialsInConfigure.Contains(comName))
                {
                    Console.WriteLine("298 "  + comName);
                    _checkedComPorts.Add(comName);
                    CashDevice newDevice = new CashDevice();
                    newDevice.Create(InterfaceType.USBVIRTUALCOM);

                    //Предполагаемый эталон
                    string ethalonKeyId = newDevice.Com.TryCheckDevice(comName);
                    Console.WriteLine("ethalon " + ethalonKeyId);
                    if (!String.IsNullOrEmpty(ethalonKeyId))
                    {
                        Console.WriteLine("306 " + comName);
                        newDevice.EthalonKeyId = ethalonKeyId;
                        await SetDeviceInfo(newDevice);
                        //
                        newDevice.SerialNumber = "22600149";
                        //
                        if(String.IsNullOrEmpty(newDevice.SerialNumber))
                        {
                            Program.StopCMD();

                            ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Для работы с данным устройством необходимо ввести его СЕРИЙНЫЙ НОМЕР:");
                            string sn = ConsoleManager.Read();
                            if (!String.IsNullOrEmpty(sn))
                            {
                                newDevice.SerialNumber = sn;
                            }

                            Program.StartCMD();
                        }
                        Console.WriteLine("323 " + comName);
                        _configuration.AddDevice(newDevice);
                        //Теперь устройство работает в режиме COM
                        newDevice.SetDeviceMode(InterfaceType.USBVIRTUALCOM);
                    }
                }
            }
        }

        public static UsbRegDeviceList GetUsbDevices()
        {
            if (_getUsbDevicesDelegate != null)
                return _getUsbDevicesDelegate.Invoke();

            return null;
        }

        private UsbRegDeviceList GetUsbRegDeviceList()
        {
            try 
            {
                UsbDevice.ForceLibUsbWinBack = true;
                
                if (UsbDevice.AllDevices.Count() > 0)
                {
                    return UsbDevice.AllDevices;
                }
            }
            catch(Exception) { }

            return null;
        }

        /// <summary>
        /// Проверка USB устройств
        /// </summary>
        private async System.Threading.Tasks.Task CheckUsbDevices()
        {
            try
            {
                UsbRegDeviceList allDevices = GetUsbDevices();

                if (allDevices?.Count > 0)
                {
                    
                    
                    foreach (UsbRegistry usbDevice in allDevices)
                    {
                        if (!_checkedUsb.Exists(pred => usbDevice.Vid == pred.Vid && usbDevice.Pid == pred.Pid && usbDevice.DevicePath == pred.DevicePath))
                        {
                            //Проверяем отсутствие устройства в списке сконфигурированных устройств
                            bool deviceIsConfigure = false;
                            if (_configuration.Devices?.Count > 0)
                            {
                                foreach(CashDevice device in _configuration.Devices)
                                {
                                    if (device.Usb.IsConfigured())
                                        if(device.Usb.DeviceRegistry.Pid == usbDevice.Pid && device.Usb.DeviceRegistry.Vid == usbDevice.Vid && device.Usb.DeviceRegistry.DevicePath == usbDevice.DevicePath)
                                        {
                                            deviceIsConfigure = true;
                                            break;
                                        }
                                }
                            }

                            if (!deviceIsConfigure)
                            {
                                _checkedUsb.Add(usbDevice);

                                if (ScannerInitializer.Initialize(usbDevice))
                                {
                                    CashDevice newDevice = new CashDevice();
                                    newDevice.Create(InterfaceType.USBHIDPOS);
                                    newDevice.Usb.Configure(false, usbDevice);
                                    bool result = newDevice.Usb.Start(1000);
                                    if (result)
                                    {
                                        await SetDeviceInfo(newDevice);

                                        if (String.IsNullOrEmpty(newDevice.SerialNumber))
                                        {
                                            Program.StopCMD();

                                            ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Для работы с данным устройством необходимо ввести его СЕРИЙНЫЙ НОМЕР:");
                                            string sn = ConsoleManager.Read();
                                            if (!String.IsNullOrEmpty(sn))
                                            {
                                                newDevice.SerialNumber = sn;
                                            }

                                            Program.StartCMD();
                                        }

                                        _configuration.AddDevice(newDevice);
                                        //Теперь устройство работает в режиме USB
                                        newDevice.SetDeviceMode(InterfaceType.USBHIDPOS);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return;
        }

        /// <summary>
        /// Выполнить команду
        /// </summary>
        /// <param name="path"></param>
        /// <param name="device"></param>
        /// <param name="values"></param>
        /// <param name="number"></param>
        /// <param name="hex"></param>
        /// <param name="deviceObj"></param>
        /// <param name="fromApi"></param>
        /// <returns></returns>
        public async Task<AnswerFromDevice> Control(string[] path, int device, string[] values = null, int number = -1, string hex = "", CashDevice deviceObj = null, bool fromApi = false) {

            AnswerFromDevice answer = new AnswerFromDevice() { HasAnswer = false, Message = "Нет изменений" };

            try
            {   
                switch (path[0].ToLower())
                {
                    case "configure":
                        {
                            switch (path[1].ToLower())
                            {
                                case "add":
                                    {
                                        switch (path[2].ToLower())
                                        {
                                            case "device":
                                                {
                                                    if (deviceObj == null)
                                                    {
                                                        CreateDevice();
                                                    }
                                                    else
                                                    {
                                                        CreateDevice(deviceObj);
                                                    }

                                                    return new AnswerFromDevice() { HasAnswer = true, Message = $"Устройство создано." };
                                                }
                                            case "logs":
                                                {
                                                    if (values?.Length > 0)
                                                    {
                                                        List<LogType> logTypeList = new List<LogType>();

                                                        for (int i = 0; i < values.Length; i++)
                                                        {
                                                            if (Enum.TryParse(typeof(LogType), values[i], true, out object logType))
                                                                logTypeList.Add((LogType)logType);
                                                        }

                                                        _configuration.AddLogTypes(logTypeList);
                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", $"Добавлено - {logTypeList.Count}");

                                                        return new AnswerFromDevice() { HasAnswer = true, Message = $"Добавлено - {logTypeList.Count}" };
                                                    }

                                                    ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Нет изменений");
                                                } break;
                                            default: break;
                                        }
                                    }
                                    break;
                                case "remove":
                                    {
                                        switch (path[2].ToLower())
                                        {
                                            case "device":
                                                {
                                                        RemoveDevice(device);
                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", $"Удалено устройство - {device}");

                                                        return new AnswerFromDevice() { HasAnswer = true, Message = $"Удалено устройство - {device}" };
                                                }
                                            case "logs":
                                                {
                                                    if (values.Length > 0)
                                                    {
                                                        List<LogType> logTypeList = new List<LogType>();

                                                        for (int i = 3; i < values.Length; i++)
                                                        {
                                                            if (Enum.TryParse(typeof(LogType), values[i], true, out object logType))
                                                                logTypeList.Add((LogType)logType);
                                                        }

                                                        _configuration.RemoveLogTypes(logTypeList);
                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", $"Удалено - {logTypeList.Count}");

                                                        return new AnswerFromDevice() { HasAnswer = true, Message = $"Удалено - {logTypeList.Count}" };
                                                    }

                                                    ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Нет изменений");
                                                }
                                                break;
                                            default: break;
                                        }
                                    }
                                    break;
                                case "get":
                                    {
                                        if (path.Length > 2)
                                        {
                                            switch (path[2].ToLower())
                                            {
                                                case "devices":
                                                    {

                                                        int devNum = _configuration.Devices.Count;
                                                        string devStr = "Количество устройств [" + devNum + "]";

                                                        if (devNum > 0)
                                                        {
                                                            foreach (CashDevice uposdevice in _configuration.Devices)
                                                            {
                                                                devStr += "\n" + uposdevice.GetInfo();
                                                            }
                                                        }

                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", devStr);
                                                        return new AnswerFromDevice() { HasAnswer = true, Message = devStr };

                                                    }
                                                case "api":
                                                    {
                                                        if (fromApi)
                                                            return answer;

                                                        string apiStr = _configuration.Api.GetInfo(); 
                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", apiStr);
                                                        return new AnswerFromDevice() { HasAnswer = true, Message = apiStr };

                                                    }
                                                case "webportal":
                                                    {
                                                        if (fromApi)
                                                            return answer;

                                                        string webPortalStr = _configuration.WebPortal.GetInfo();
                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", webPortalStr);
                                                        return new AnswerFromDevice() { HasAnswer = true, Message = webPortalStr };

                                                    }
                                                case "logs":
                                                    {

                                                        string logsToPrint = "Список типов лога для записи в файл:";
                                                        logsToPrint += "\n" + LogType.Request + ", " + LogType.Response;

                                                        if (_configuration.LogsToWrite?.Count > 0)
                                                        {
                                                            foreach (LogType logType in _configuration.LogsToWrite)
                                                            {
                                                                logsToPrint += ", " + logType;
                                                            }
                                                        }

                                                        ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", logsToPrint);
                                                        return new AnswerFromDevice() { HasAnswer = true, Message = logsToPrint };

                                                    }
                                            }
                                        }
                                        else
                                        {

                                            string confStr = _configuration.ToJsonString();
                                            ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", confStr);
                                            return new AnswerFromDevice() { HasAnswer = true, Message = confStr };

                                        }
                                    }
                                    break;
                                case "api":
                                    {

                                        if (fromApi)
                                            return answer;

                                        _configuration.Api.Init();
                                    }
                                    break;
                                case "webportal":
                                    {
                                        if (!_configuration.WebPortal.Initialized)
                                        {
                                            if (_configuration.WebPortal.Init())
                                            {
                                                ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Хотите запустить web-портал (y - да, n - нет)?");
                                                string way = ConsoleManager.Read();
                                                if (String.IsNullOrEmpty(way))
                                                {
                                                    TaskManager _taskManager = new TaskManager();
                                                    _taskManager.Start(_configuration.WebPortal, this);
                                                    _configuration.WebPortal.Initialized = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case "auto-search-devices":
                                    {
                                        if (path.Length > 2)
                                        {
                                            switch (path[2].ToLower())
                                            {
                                                case "start":
                                                    {
                                                        if (_searchDevicesTimer != null)
                                                        {
                                                            StopSearchDevicesTimer = false;
                                                            _searchDevicesTimer.Enabled = true;
                                                            ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Поиск запущен.");
                                                        }
                                                        else
                                                            ConsoleManager.Add(LogType.Alert, "ScannerManager", "Control", "Невозможно запустить поиск. Перезапустите программу.");
                                                    } break;
                                                case "stop":
                                                    {

                                                        if (_searchDevicesTimer != null)
                                                        {
                                                            StopSearchDevicesTimer = true;
                                                            ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Поиск остановлен.");
                                                        }
                                                        else
                                                            ConsoleManager.Add(LogType.Alert, "ScannerManager", "Control", "Невозможно остановить поиск. Перезапустите программу.");
                                                    } break;
                                            }
                                        }
                                    }
                                    break;
                                default: { } break;
                            }
                        }
                        break;
                    case "saveconfigure":
                        {
                            bool result = _fileManager.SaveConfig(_configuration);
                            if (result)
                            {
                                ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Конфигурация успешно сохранена.");
                                return new AnswerFromDevice() { HasAnswer = true, Message = "Конфигурация успешно сохранена." };
                            }
                            else
                                ConsoleManager.Add(LogType.Information, "ScannerManager", "Control", "Что-то не так.");
                        }
                        break;
                    case "scanner": 
                        {
                            switch (path[1].ToLower())
                            {
                                case "hex":
                                    {
                                        return await ControlHex(device, path[2]);
                                    }
                                case "ascii":
                                    {
                                        return await ControlHex(device, path[2].ASCIIToHexString());
                                    }
                                case "updatefirmware":
                                    {
                                        _cts = new CancellationTokenSource();
                                        bUpdateComplete = false;

                                        if (values?.Length > 0)
                                        {
                                            //Поток на перепрошивку
                                            _ = System.Threading.Tasks.Task.Run(() =>
                                            {
                                                UpdateFirmware(device, values[0], _cts.Token);
                                            });

                                            //Поток на отмену прошивки через клавишу ESCAPE
                                            _ = System.Threading.Tasks.Task.Run(() => {

                                                ConsoleKeyInfo key = new ConsoleKeyInfo();
                                                while (!bUpdateComplete)
                                                {
                                                    key = Console.ReadKey();
                                                    if (key.Key == ConsoleKey.Escape)
                                                    {
                                                        _cts.Cancel();
                                                        break;
                                                    }
                                                }

                                            });

                                            //Поток ожидания завершения перепрошивки 
                                            await System.Threading.Tasks.Task.Run(() => {

                                                while (!bUpdateComplete) { }
                                                return bUpdateComplete;

                                            });


                                        }
                                        else
                                        {
                                            _answerFromDevice = new AnswerFromDevice();
                                            _answerFromDevice.HasAnswer = false;
                                            _answerFromDevice.Message = "Неправильно задана команда.";
                                        }
                                        return _answerFromDevice;
                                    }
                                case "keyboardstate":
                                    {
                                        if (values?.Length > 0)
                                        {
                                            switch (values[0])
                                            {
                                                case "true":
                                                    {
                                                        KeyboardState(device, true);
                                                        return new AnswerFromDevice() { Error = Errors.Success, Message = "Значение установлено.", HasAnswer = true };
                                                    }
                                                case "false":
                                                    {
                                                        KeyboardState(device, false);
                                                        return new AnswerFromDevice() { Error = Errors.Success, Message = "Значение установлено.", HasAnswer = true };
                                                    }
                                            }
                                        }
                                        else
                                        {
                                            _answerFromDevice = new AnswerFromDevice();
                                            _answerFromDevice.HasAnswer = false;
                                            _answerFromDevice.Message = "Неправильно задана команда.";
                                        }
                                        return _answerFromDevice;
                                    }
                                default:
                                    {
                                        CommandState state = CommandState.NONE;
                                        string val = "";

                                        if (values?.Length > 0)
                                        {
                                            switch (values[0])
                                            {
                                                case "on": state = CommandState.ON; break;
                                                case "off": state = CommandState.OFF; break;
                                                case "?": state = CommandState.CURRENT; break;
                                                case "^": state = CommandState.DEFAULT; break;
                                                case "*": state = CommandState.RANGE; break;
                                                default: val = values[0]; break;
                                            }
                                        }
                                        else if (number != -1)
                                            val = number.ToString("X2");
                                        else if (!String.IsNullOrEmpty(hex))
                                            val = hex;

                                        answer = await CreateCommand(device, path[1], val, state);
                                    }
                                    break;
                            }
                        }
                        break;

                }
            }
            catch { }

            return answer;
        }

        /// <summary>
        /// Установить значение параметра
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="command"></param>
        /// <param name="value">HEX Значение для установки</param>
        /// <param name="state">состояние параметра (вкл/выкл)</param>
        private async Task<AnswerFromDevice> CreateCommand(int deviceNumber, string command, string value = "", CommandState state = CommandState.NONE)
        {

            AnswerFromDevice response = new AnswerFromDevice();
            AnswerFromDevice answer = new AnswerFromDevice();

            try
            {
                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
                if (device == null) 
                    return answer;

                PosCommand posCommand = _posCommands.CreateCommand(command, device.EthalonKeyId);
                if (posCommand == null)
                    return answer;

                PosCommand prefix = _posCommands.CreateCommand("Prefix", device.EthalonKeyId);
                PosCommand suffix = _posCommands.CreateCommand("Suffix", device.EthalonKeyId);
                PosCommand end = _posCommands.CreateCommand("End", device.EthalonKeyId);

                switch (posCommand.Type)
                {
                    case CommandType.Cmd:
                        {
                            response = await device.SendRequestToDevice(posCommand.Command);
                            return response;
                        }
                    case CommandType.Pre_Cmd_Suf:
                        {
                            if (prefix != null)
                                answer = await device.SendRequestToDevice(prefix?.Command);

                            response = await device.SendRequestToDevice(posCommand.Command);

                            if (suffix != null)
                                if (!String.IsNullOrEmpty(suffix.Command))
                                    answer = await device.SendRequestToDevice(suffix.Command);

                            return response;
                        }
                    case CommandType.Pre_Cmd1_CmdN_Suf:
                        {
                            if (prefix != null)
                                answer = await device.SendRequestToDevice(prefix?.Command);

                            foreach (string commandItem in posCommand.Commands)
                            {
                                response = await device.SendRequestToDevice(commandItem + end.Command);
                            }

                            if (suffix != null)
                                if (!String.IsNullOrEmpty(suffix.Command))
                                    answer = await device.SendRequestToDevice(suffix.Command);

                            return response;
                        }
                    case CommandType.Pre_CmdValEnd_Suf:
                        {
                            if (prefix != null)
                                answer = await device.SendRequestToDevice(prefix?.Command);

                            if (!String.IsNullOrEmpty(value))
                            {
                                if(int.TryParse(value, out int number))
                                {
                                    if(number >= 0 && number < 256)
                                        response = await device.SendRequestToDevice(posCommand.Command + ((byte)number).ToString("X2") + end.Command);
                                }
                                else
                                    response = await device.SendRequestToDevice(posCommand.Command + value + end.Command);
                            }
                            else if (state == CommandState.ON)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("On", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.OFF)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Off", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.CURRENT)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Current", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.DEFAULT)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Default", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.RANGE)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Range", device.EthalonKeyId).Command + end.Command);

                            if (suffix != null)
                                if (!String.IsNullOrEmpty(suffix.Command))
                                    answer = await device.SendRequestToDevice(suffix.Command);

                            return response;
                        }
                    case CommandType.Pre_CmdCurEnd_Suf:
                        {
                            if (prefix != null)
                                answer = await device.SendRequestToDevice(prefix?.Command);

                            if (state == CommandState.NONE)
                                response = await device.SendRequestToDevice(posCommand.Command + end.Command);
                            else if (state == CommandState.CURRENT)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Current", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.DEFAULT)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Default", device.EthalonKeyId).Command + end.Command);
                            else if (state == CommandState.RANGE)
                                response = await device.SendRequestToDevice(posCommand.Command + _posCommands.CreateCommand("Range", device.EthalonKeyId).Command + end.Command);

                            if (suffix != null)
                                if (!String.IsNullOrEmpty(suffix.Command))
                                    answer = await device.SendRequestToDevice(suffix.Command);

                            return response;
                        }
                }
            }
            catch(Exception ex)
            {

                ConsoleManager.Add(LogType.Error, "ScannerManager", "Control", String.Format("Вызвано исключение: {0}", ex.Message));
                return answer;
            }

            return response;
        }

        /// <summary>
        /// Отправить команду на сервер
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="state"></param>
        private void KeyboardState(int deviceNumber, bool state)
        {
            try
            {
                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
                if (device == null)
                    return;

                device.SendKeyboardState(state);

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ScannerManager", "KeyboardState", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Отправить команду на сервер
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="command"></param>
        private async Task<AnswerFromDevice> ControlHex(int deviceNumber, string command)
        {
            AnswerFromDevice answer = new AnswerFromDevice();
            try
            {

                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
                if (device == null) 
                    return answer;

                return await device.SendRequestToDevice(command);
                
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ScannerManager", "ControlHex", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return answer;
        }

        /// <summary>
        /// Получить данные об устройстве
        /// </summary>
        /// <param name="device"></param>
        private async Task<CashDevice> SetDeviceInfo(CashDevice device)
        {
            try
            {
                PosCommand prefix = _posCommands.CreateCommand("Prefix", device.EthalonKeyId);
                PosCommand suffix = _posCommands.CreateCommand("Suffix", device.EthalonKeyId);
                PosCommand end = _posCommands.CreateCommand("End", device.EthalonKeyId);

                AnswerFromDevice answer = null;

                if (prefix != null)
                    answer = await device.SendRequestToDevice(prefix?.Command);

                //Пробуем получить серийный номер напрямую
                PosCommand serialCmd = _posCommands.CreateCommand("SerialNumber", device.EthalonKeyId);
                if (serialCmd != null)
                {
                    answer = await device.SendRequestToDevice(serialCmd.Command);
                    if (answer.HasAnswer)
                    {
                        device.SerialNumber = answer.Message;
                    }
                }
                else {

                    ConsoleManager.Add(LogType.Information, "ScannerManager", "SetDeviceInfo", "Пробую получить информацию об устройстве через комманду FirmwareVersion.");
                //    answer = await device.SendRequestToDevice(_posCommands.CreateCommand("FirmwareVersion", device.EthalonKeyId)?.Command + end.Command);
                    //if (answer.HasAnswer)
                        if (false)
                        {
                        int indexOfStr = -1;
                        string str = "";

                        indexOfStr = answer.Message.IndexOf("manufacture", StringComparison.CurrentCultureIgnoreCase);
                        if (indexOfStr != -1)
                        {
                            str = answer.Message.Substring(indexOfStr);
                            indexOfStr = str.IndexOf(":", StringComparison.CurrentCultureIgnoreCase);
                            str = str.Substring(indexOfStr + 1);
                            str = str.Split("\n")[0];
                            str = str.Split(";")[0];
                            str = str.Replace(" ", string.Empty);
                            device.CompanyName = str;
                        }

                        indexOfStr = answer.Message.IndexOf("serial", StringComparison.CurrentCultureIgnoreCase);
                        if (indexOfStr != -1)
                        {
                            str = answer.Message.Substring(indexOfStr);
                            indexOfStr = str.IndexOf(":", StringComparison.CurrentCultureIgnoreCase);
                            str = str.Substring(indexOfStr + 1);
                            str = str.Split("\n")[0];
                            str = str.Split(";")[0];
                            str = str.Replace(" ", string.Empty);
                            device.SerialNumber = str;
                        }

                        indexOfStr = answer.Message.IndexOf("product", StringComparison.CurrentCultureIgnoreCase);
                        if (indexOfStr != -1)
                        {
                            str = answer.Message.Substring(indexOfStr);
                            indexOfStr = str.IndexOf(":", StringComparison.CurrentCultureIgnoreCase);
                            str = str.Substring(indexOfStr + 1);
                            str = str.Split("\n")[0];
                            str = str.Split(";")[0];
                            str = str.Replace(" ", string.Empty);
                            device.ModelName = str;
                        }
                    }

                    switch (device.Interface)
                    {
                        case InterfaceType.USBHIDPOS:
                        case InterfaceType.USBKBW:
                            {
                                if (device.Usb.IsConfigured())
                                {
                                    foreach (KeyValuePair<string, object> property in device.Usb.DeviceRegistry.DeviceProperties)
                                    {
                                        if(String.IsNullOrEmpty(device.SerialNumber))
                                            if (property.Key.Contains("serial", StringComparison.CurrentCultureIgnoreCase))
                                                device.SerialNumber = (string)((KeyValuePair<string, object>)property).Value;

                                        if (String.IsNullOrEmpty(device.ModelName))
                                            if (property.Key.Contains("mfg", StringComparison.CurrentCultureIgnoreCase))
                                            device.ModelName = (string)((KeyValuePair<string, object>)property).Value;
                                    }
                                }
                            } break;
                    }
                }

                if (suffix != null)
                    if (!String.IsNullOrEmpty(suffix.Command))
                        answer = await device.SendRequestToDevice(suffix.Command);

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ScannerManager", "SetDeviceInfo", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return device;
        }

        /// <summary>
        /// Обновить прошивку
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="filePath"></param>
        public async Task<bool> UpdateFirmware(string deviceKey, string filePath)
        {
            AnswerFromDevice answer = new AnswerFromDevice();

            try
            {

                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.SerialNumber == deviceKey || pred.KeyId == deviceKey || pred.SecondKeyId == deviceKey);
                if (device == null)
                    throw new Exception("Устройство не найдено.");

                if (String.IsNullOrEmpty(filePath))
                    throw new Exception("Имя файла пустое.");

                _cts = new CancellationTokenSource();
                bUpdateComplete = false;

                //Поток на перепрошивку
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    UpdateFirmware(device.Number, filePath, _cts.Token);
                });

                //Поток на отмену прошивки через клавишу ESCAPE
                _ = System.Threading.Tasks.Task.Run(() => {

                    ConsoleKeyInfo key = new ConsoleKeyInfo();
                    while (!bUpdateComplete)
                    {
                        key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Escape)
                        {
                            _cts.Cancel();
                            break;
                        }
                    }

                });

                //Поток ожидания завершения перепрошивки 
                return await System.Threading.Tasks.Task.Run(() => {
                    while (!bUpdateComplete) { }
                    return bUpdateComplete;
                });
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Обновить прошивку
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="filePath"></param>
        /// <param name="cancellation"></param>
        private async void UpdateFirmware(int deviceNumber, string filePath, CancellationToken cancellation)
        {
            AnswerFromDevice answer = new AnswerFromDevice();
            try
            {
               
                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
                if (device == null)
                    throw new Exception("Устройство не найдено.");
                
                if (!System.IO.File.Exists(filePath))
                {
                    filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Firmwares\" + filePath;
                    if (!System.IO.File.Exists(filePath))
                        throw new Exception("Файл не найден.");
                }

                FileInfo fileInfo = new FileInfo(filePath);

                byte[] FirmwareData = await FileManager.ReadFile(filePath);
                if (FirmwareData.Length == 0 || cancellation.IsCancellationRequested)
                    throw new Exception("Файл не удалось прочитать. Или отмена.");

                PosCommand prefix = _posCommands.CreateCommand("Prefix", device.EthalonKeyId);
                PosCommand init1 = _posCommands.CreateCommand("Init1", device.EthalonKeyId);
                PosCommand end = _posCommands.CreateCommand("End", device.EthalonKeyId);

                _answerFromDevice = await device.SendRequestToDevice(prefix.Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice(_posCommands.CreateCommand("FirmwareVersion", device.EthalonKeyId)?.Command + end.Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice(prefix.Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice(init1.Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice(_posCommands.CreateCommand("Init2", device.EthalonKeyId).Command);
                if ((_answerFromDevice.Error != Errors.Success && _answerFromDevice.Error != Errors.ErrorTimeout) || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice(init1.Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice("0D");
                if (_answerFromDevice.Error != Errors.Success && _answerFromDevice.Error != Errors.ErrorTimeout || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendRequestToDevice("6E657761707020" + fileInfo.Name.ToHexString() + "20" + fileInfo.Length.ToString().ToHexString() + "0A");
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                _answerFromDevice = await device.SendBufferToDevice(FirmwareData, cancellation);
                if (!_answerFromDevice.HasAnswer)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                ConsoleManager.Add(LogType.Error, "ScannerManager", "UpdateFirmware", String.Format("Файл успешно загружен. Устанавливаю... Не выключайте!!!"));

                _answerFromDevice = await device.SendRequestToDevice(_posCommands.CreateCommand("SetFirmware", device.EthalonKeyId).Command);
                if (!_answerFromDevice.HasAnswer || cancellation.IsCancellationRequested)
                    throw new Exception("Нет ответа от устройства. Или отмена.");

                ConsoleManager.Add(LogType.Error, "ScannerManager", "UpdateFirmware", String.Format("Операция будет выполнена после перезагрузки устройства."));
                ConsoleManager.Add(LogType.Error, "ScannerManager", "UpdateFirmware", String.Format("Внимание!!! Не отключайте устройство. Это может привести к порче системы."));

                device.NeedToReload = true;

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ScannerManager", "UpdateFirmware", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            bUpdateComplete = true;
        }

        /// <summary>
        /// Установить значения переменных
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="variables"></param>
        /// <exception cref="Exception"></exception>
        public async Task<bool> SetVariables(string deviceKey, List<EthalonVariable> variables)
        {
            if (_configuration == null)
                return false;

            CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.SerialNumber == deviceKey || pred.KeyId == deviceKey || pred.SecondKeyId == deviceKey);
            if (device == null)
                throw new Exception("Устройство не найдено.");
            
            foreach (EthalonVariable variable in variables)
            {
                if (!variable.Write)
                    continue;

                AnswerFromDevice response = new AnswerFromDevice();
                AnswerFromDevice answer = new AnswerFromDevice();

                PosCommand posCommand = _posCommands.CreateCommand(variable.Name, device.EthalonKeyId);
                if (posCommand == null)
                    return false;

                PosCommand prefix = _posCommands.CreateCommand("Prefix", device.EthalonKeyId);
                PosCommand suffix = _posCommands.CreateCommand("Suffix", device.EthalonKeyId);
                PosCommand end = _posCommands.CreateCommand("End", device.EthalonKeyId);

                string value = "";

                switch (variable.CommandType)
                {
                    case CommandType.Cmd:
                        {

                            value = posCommand.Command;
                            response = await device.SendRequestToDevice(value);

                        }
                        break;
                    case CommandType.Pre_Cmd_Suf: 
                        {

                            answer = await device.SendRequestToDevice(prefix?.Command);

                            value = posCommand.Command; 
                            response = await device.SendRequestToDevice(value);

                            answer = await device.SendRequestToDevice(suffix.Command);

                        } break;
                    case CommandType.Pre_CmdValEnd_Suf:
                        {

                            switch (variable.ValueType)
                            {
                                case Enums.ValueType.String:
                                    value = posCommand.Command + variable.CurrentValue.ASCIIToHexString() + end.Command; break;
                                case Enums.ValueType.Boolean:
                                    {
                                        if (variable.CurrentValue == "true")
                                            value = posCommand.Command + _posCommands.CreateCommand("On", device.EthalonKeyId).Command + end.Command;
                                        else if (variable.CurrentValue == "false")
                                            value = posCommand.Command + _posCommands.CreateCommand("Off", device.EthalonKeyId).Command + end.Command;
                                    }
                                    break;
                                case Enums.ValueType.Int:
                                    {
                                        int number = Convert.ToInt32(variable.CurrentValue);
                                        value = posCommand.Command + ((byte)number).ToString("X2") + end.Command;
                                    }
                                    break;
                            }

                            if (String.IsNullOrEmpty(value))
                                return false;
                            
                            answer = await device.SendRequestToDevice(prefix?.Command);

                            response = await device.SendRequestToDevice(value);

                            answer = await device.SendRequestToDevice(suffix.Command);


                        } break;
                    case CommandType.Pre_Cmd1_CmdN_Suf:
                        {

                            answer = await device.SendRequestToDevice(prefix?.Command);

                            foreach (string commandItem in posCommand.Commands)
                            {
                                response = await device.SendRequestToDevice(commandItem + end.Command);
                            }

                            answer = await device.SendRequestToDevice(suffix.Command);
                        }
                        break;
                }

                //Устанавливаем значение в списке
                _configuration.SetVariable(deviceKey, variable);
            }

            return true;
        }

        /// <summary>
        /// Получить переменные типа GET
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <exception cref="Exception"></exception>
        public async Task<List<EthalonVariable>> GetVariables(string deviceKey)
        {
            try
            {

                if (_configuration == null)
                return null;

                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.SerialNumber == deviceKey || pred.KeyId == deviceKey || pred.SecondKeyId == deviceKey);
                if (device == null)
                    throw new Exception("Устройство не найдено.");

                if (device.Variables?.Count > 0)
                {
                    foreach (EthalonVariable variable in device.Variables)
                    {
                        if (!variable.Read)
                            continue;

                        AnswerFromDevice response = new AnswerFromDevice();
                        AnswerFromDevice answer = new AnswerFromDevice();

                        PosCommand posCommand = _posCommands.CreateCommand(variable.Name, device.EthalonKeyId, false);
                        if (posCommand == null)
                            continue;

                        PosCommand prefix = _posCommands.CreateCommand("Prefix", device.EthalonKeyId);
                        PosCommand suffix = _posCommands.CreateCommand("Suffix", device.EthalonKeyId);
                        PosCommand end = _posCommands.CreateCommand("End", device.EthalonKeyId);
                        PosCommand current = _posCommands.CreateCommand("End", device.EthalonKeyId);

                        switch (variable.CommandType)
                        {
                            case CommandType.Cmd:
                                {

                                    response = await device.SendRequestToDevice(posCommand.Command);

                                    if (response.HasAnswer)
                                    {
                                        variable.CurrentValue = response.Message;
                                    }

                                }
                                break;
                            case CommandType.Pre_CmdCurEnd_Suf:
                                {

                                    answer = await device.SendRequestToDevice(prefix?.Command);

                                    response = await device.SendRequestToDevice(posCommand.Command + current.Command + end.Command);

                                    if (response.HasAnswer)
                                    {
                                        variable.CurrentValue = response.Message;
                                    }

                                    answer = await device.SendRequestToDevice(suffix.Command);

                                }
                                break;
                            case CommandType.Pre_Cmd_Suf:
                                {

                                    answer = await device.SendRequestToDevice(prefix?.Command);

                                    response = await device.SendRequestToDevice(posCommand.Command);

                                    if (response.HasAnswer)
                                    {
                                        variable.CurrentValue = response.Message;
                                    }

                                    answer = await device.SendRequestToDevice(suffix.Command);

                                }
                                break;
                        }
                    }
                }

                return device.Variables;
            }
            catch (Exception ex)
            {

                ConsoleManager.Add(LogType.Error, "ScannerManager", "GetVariables", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return new List<EthalonVariable>();
        }

        /// <summary>
        /// Остановить работу устройв(а)
        /// </summary>
        /// <param name="deviceNumber">0 - остановить все</param>
        public void Stop(int deviceNumber = 0)
        {
            if(deviceNumber == 0)
            {
                if(_configuration.Devices?.Count > 0)
                {
                    foreach(CashDevice device in _configuration.Devices)
                    {
                        device.Stop();
                    }
                }

                if (_apiController != null)
                    if (_apiController.IsRunning())
                        _apiController.Stop();

                _configuration.StopTime = DateTime.Now;
                _configuration.StopCorrectly = true;
                _fileManager.SaveConfig(_configuration);
            }
            else
            {
                CashDevice device = _configuration.Devices.FirstOrDefault(pred => pred.Number == deviceNumber);
                if (device == null) 
                    return;

                device.Stop();
            }
        }

        public static string GetHelp()
        {
            string helpStr = "UPOSControl v1.5 от MINAKOVprog:\n";
            helpStr += _posCommands.GetHelp();
            helpStr +=
                "Возможный формат ввода команд: \n" +
                "<exit> - завершить программу. \n" +
                "<-command configure add device> – создать конфигурацию нового устройства. \n" +
                "<-command configure remove device -device {0...10}> – удалить устройство из списка. \n" +
                "<-command configure get devices> – получить список устройств. \n" +
                "<-command configure auto-search-devices stop> – остановить автоматический поиск новых устройств. \n" +
                "<-command configure auto-search-devices start> – запустить автоматический поиск новых устройств. \n" +
                "<-command configure api> – настроить API приложения. \n" +
                "<-command configure webportal> – настроить WEB-портал. \n" +
                "<-command configure get api> – получить конфигурацию API. \n" +
                "<-command configure get webportal> – получить конфигурацию Web-портала. \n" +
                "<-command configure add logs -value {log-type-1} -value {log-type-2}> \n" +
                "Добавить типы логов для записи в файл. \n" +
                "Где log-type может быть <Information, Alert, Error, Start>. \n" +
                "<-command configure remove logs -value {log-type-1} -value {log-type-2}> \n" +
                "Удалить типы логов для записи в файл. (Information, Alert, Error, Start). \n" +
                "<-command configure get logs> – получить конфигурацию logs. \n" +
                "<-command configure get> – получить всю конфигурацию. \n" +
                "<-command saveconfigure> - сохранить конфигурацию. \n\n" +
                "SET команда: <-command scanner {command name} -device {0...10} -value {on (или off, ?, *, ^)} (или -number {0...255}, -hex {HEX}> \n" +
                "Можно писать только один тип значения <-value> - определённое значение; или <-number> - число, или <-hex> HEX строка. \n" +
                "DO, GET, SEQUENCE, NONE команда: <-command scanner {command name} -device {0...10}>. \n" +
                "NONE команда передаётся устройству “чистой” (без префикса и суффикса). \n" +
                "HEX команда: <-command scanner hex {HEX} -device {0...10}>. \n" +
                "ASCII команда: <-command scanner ascii {ASCII} -device {0...10}>. \n" +
                "UTF8 команда: <-command scanner utf8 {UTF8} -device {0...10}>. \n" +
                "<help> - открыть помощь. \n" +
                "Примеры команд. \n" +
                "<-command scanner updatefirmware -device {0...10} -value {file name (или file path)}> \n" +
                "Обновить прошивку из файла (можно указать полный путь или имя, тогда файл должен находиться в дирректории программы в папке /Firmware. \n";

            return helpStr;
        }

        public string GetHtmlHelp()
        {
            string helpStr = "Список Api запросов, UPOSControl v1.5 от MINAKOVprog:<br />";
            helpStr +=
                "<br />1.  GET /api/exit - завершить программу." +
                "<hr>" +
                "<br />2.  POST /api/configure/add/device> – создать и запустить конфигурацию нового устройства." +
                "<br /><span style='padding-left:30px;'></span>-H \"Content-Type: application/json\"" +
                "<br /><span style='padding-left:30px;'></span>-d \"" +
                "<br /><span style='padding-left:30px;'></span>{" +
                "<br /><span style='padding-left:45px;'></span>\"number\": 0," +
                "<br /><span style='padding-left:45px;'></span>\"keyId\": string," +
                "<br /><span style='padding-left:45px;'></span>\"secondKeyId\": string," +
                "<br /><span style='padding-left:45px;'></span>\"interface\": number," +
                "<br /><span style='padding-left:45px;'></span>\"name\": string," +
                "<br /><span style='padding-left:45px;'></span>\"companyName\": string," +
                "<br /><span style='padding-left:45px;'></span>\"productName\": string," +
                "<br /><span style='padding-left:45px;'></span>\"firmwareVersion\": string," +
                "<br /><span style='padding-left:45px;'></span>\"firmwareCreatedDate\": string," +
                "<br /><span style='padding-left:45px;'></span>\"serialNumber\": string," +
                "<br /><span style='padding-left:45px;'></span>\"usb\": {" +
                "<br /><span style='padding-left:60px;'></span></span>\"vid\": HEX string," +
                "<br /><span style='padding-left:60px;'></span></span>\"pid\": HEX string" +
                "<br /><span style='padding-left:45px;'></span>}," +
                "<br /><span style='padding-left:45px;'></span>\"com\": {" +
                "<br /><span style='padding-left:60px;'></span></span>\"portName\": string," +
                "<br /><span style='padding-left:60px;'></span></span>\"baudRate\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"parity\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"dataBits\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"stopBits\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"handshake\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"readTimeout\": number," +
                "<br /><span style='padding-left:60px;'></span></span>\"writeTimeout\": number" +
                "<br /><span style='padding-left:45px;'></span>}" +
                "<br /><span style='padding-left:30px;'></span>}\"" +
                "<hr>" +
                "<br />3.  GET /api/configure/remove/device?device={0...10} – удалить устройство из списка." +
                "<hr>" +
                "<br />4.  GET /api/configure/get/devices – получить список устройств." +
                "<hr>" +
                "<br />5.  GET /api/configure/add/logs?value={log-type-1}&value={log-type-2} – добавить типы логов для записи в файл." +
                "<hr>" +
                "<br />6.  GET /api/configure/remove/logs?value={log-type-1}&value={log-type-2} – удалить типы логов для записи в файл." +
                "<br />где log-type может быть <Information, Alert, Error, Start>" +
                "<hr>" +
                "<br />7.  GET /api/configure/get/logs – получить конфигурацию logs." +
                "<hr>" +
                "<br />8.  GET /api/configure/get – получить всю конфигурацию." +
                "<hr>" +
                "<br />9.  GET /api/saveconfigure - сохранить конфигурацию." +
                "<hr>" +
                "<br />10. GET /api/scanner/updatefirmware?value={file name (или file path)}&device={0...10} - обновить прошивку из файла." +
                "<br />Можно указать полный путь или имя, тогда файл должен находиться в дирректории программы /Firmware/." +
                "<hr>" +
                "<br />11. GET /api/scanner/hex/{HEX}?device={0...10} - передать \"чистую\" HEX команду." +
                "<hr>" +
                "<br />12. GET /api/scanner/utf8/{UTF8}?device={0...10} - передать \"чистую\" UTF8 команду." +
                "<hr>" +
                "<br />13. GET /api/scanner/ascii/{ASCII}?device={0...10} - передать \"чистую\" ASCII команду." +
                "<hr>";

            return helpStr + _posCommands.GetHtmlHelp();
        }
    }
}
