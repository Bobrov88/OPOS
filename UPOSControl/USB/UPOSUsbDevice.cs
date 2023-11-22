using LibUsbDotNet.Main;
using MonoLibUsb.Profile;
using Newtonsoft.Json;
using System;
using System.Linq;
using UPOSControl.Enums;
using UPOSControl.Managers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Timers;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using UPOSControl.Classes;
using UPOSControl.Utils;
using System.Threading.Tasks;
using static UPOSControl.Program;

namespace UPOSControl.USB
{
    /// <summary>
    /// Определение USB-устройства
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Legacy Code")]
    public class UPOSUsbDevice : ScannerTransport
    {
        [JsonIgnore]
        public int VID { get; set; }
        [JsonIgnore]
        public int PID { get; set; }

        [JsonProperty("vid")]
        public string VIDs { get; set; }
        [JsonProperty("pid")]
        public string PIDs { get; set; }
        [JsonProperty("busNumber")]
        public int BusNumber { get; set; } = -1;
        [JsonProperty("deviceAddress")]
        public int DeviceAddress { get; set; } = -1;

        [JsonIgnore]
        public UsbRegistry DeviceRegistry;

        [JsonIgnore]
        private bool Configured { get; set; } = false;


        private UsbDevice MyUsbDevice;
        private System.Timers.Timer _timer = new System.Timers.Timer(); //Таймер событий, чтобы устройство не заснуло
        private System.Timers.Timer _timerTimeout = new System.Timers.Timer(); 
        // Defined by AOA
        private static readonly int MAX_PACKET_BYTES = 1024;
        private static readonly short PACKET_HEADER_SIZE = 2;
        private static readonly int REMOTE_STRING_LENGTH_MAX = 4 * 1024 * 1024;

        private static int EMPTY_STRING = 1;

        private UsbEndpointReader EPReader;
        private UsbEndpointWriter EPWriter;

        private bool Shutdown = false;

        private BackgroundWorker ReceiveMessagesThread;
        private BackgroundWorker SendMessagesThread;
        private DoWorkEventHandler ReceiveMessagesDoWorkHandler;
        private DoWorkEventHandler SendMessagesDoWorkHandler;

        private readonly object DeviceAccessorySyncLock = new object();

        BlockingQueue<string> MessageQueue = new BlockingQueue<string>();
        public override string ShortTitle() => "USB";
        [JsonIgnore]
        public override string Title => "USB PD";
        [JsonIgnore]
        public override string Summary => "USB";

        private CashDevice myDevice;

        private AnswerFromDevice _answerFromDevice;

        private delegate void UsbListeningThreadAnswerDelegate(AnswerFromDevice answer);
        private UsbListeningThreadAnswerDelegate _usbListeningThreadAnswerDelegate;

        /// <summary>
        /// Состояние чтения пакета (получен ответ)
        /// </summary>
        /// <returns></returns>
        private delegate bool ReadingPacketDelegate();
        private ReadingPacketDelegate _readingPacketCompleteDelegate;

        private delegate int IsReadingPacketDelegate();
        private IsReadingPacketDelegate _isReadingPacketDelegate;

        private ManagementEventWatcher watchInserted;
        private ManagementEventWatcher watchRemoved;

        /// <summary>
        /// Состояние чтения пакета
        /// 0 - не началось
        /// 1 - чтение началось
        /// 2 - чтение закончено, получен ответ
        /// </summary>
        private int _readingPacketState = 0;
        private bool _readingPacketComplete = false;

        public UPOSUsbDevice(CashDevice parent)
        {
            myDevice = parent;
        }

        /// <summary>
        /// Установить адрес устройства
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SetAddress()
        {
            MonoLibUsbShowInfo showInfo = new MonoLibUsbShowInfo();
            MonoUsbProfileList profileList = showInfo.GetProfiles();
            MonoUsbProfile profile = null;

            while (true)
            {
                ConsoleManager.Add(LogType.Information, "UPOSUsbDevice", "SetAddress", "Введите АДРЕС устройства:");
                string addressStr = ConsoleManager.Read();
                string[] address = new string[2];
                if (!String.IsNullOrEmpty(addressStr))
                    address = addressStr.Split('.');

                if (address.Length == 2)
                {
                    profile = profileList.FirstOrDefault(p => p.BusNumber == Convert.ToInt32(address[0]) && p.DeviceAddress == Convert.ToInt32(address[1]));
                    if (profile == null)
                        ConsoleManager.Add(LogType.Information, "UPOSUsbDevice", "SetAddress", "Профиль устройства не найден.");
                    else
                        break;
                }
            }

            VID = profile.DeviceDescriptor.VendorID;
            PID = profile.DeviceDescriptor.ProductID;
            DeviceAddress = profile.DeviceAddress;
            BusNumber = profile.BusNumber;

            VIDs = PID.ToString("X2");
            if (VIDs.Length == 2)
                VIDs = "00" + VIDs;

            PIDs = PID.ToString("X2");
            if (PIDs.Length == 2)
                PIDs = "00" + PIDs;

            MonoUsbShowConfig _showConfig = new MonoUsbShowConfig();
            _showConfig.ShowConfig(profile.DeviceDescriptor.ProductID, profile.DeviceDescriptor.VendorID);
        }

        /// <summary>
        /// Вернуть ответ от устройства
        /// </summary>
        /// <param name="answer"></param>
        private void SetAnswer(AnswerFromDevice answer)
        {
            if (answer != null && _answerFromDevice != null)
            {
                _answerFromDevice.Length = answer.Length;
                _answerFromDevice.Message = answer.Message;
                _answerFromDevice.Response = answer.Response;
                _answerFromDevice.HexStr = answer.HexStr;
                _answerFromDevice.Error = answer.Error;
                _answerFromDevice.HasAnswer = answer.HasAnswer;
            }

            _readingPacketComplete = true;
        }

        private bool GetReadingPacketCompleteState()
        {
            return _readingPacketComplete;
        }


        /// <summary>
        /// Настроен или нет
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return Configured;
        }

        /// <summary>
        /// Ввести данные
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="deviceRegistry"></param>
        public void Configure(bool inputData = true, UsbRegistry deviceRegistry = null)
        {

            if (inputData)
            {
                SetAddress();
            }
            else
            {
                if (deviceRegistry == null)
                {
                    if (!String.IsNullOrEmpty(PIDs) && !String.IsNullOrEmpty(VIDs))
                    {
                        byte[] arr = VIDs.HexStringToByteArray();
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        VID = BitConverter.ToInt16(arr, 0);

                        arr = PIDs.HexStringToByteArray();
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        PID = BitConverter.ToInt16(arr, 0);
                    }
                    else 
                        return;
                }
                else {
                    DeviceRegistry = deviceRegistry;
                    VID = deviceRegistry.Vid;
                    PID = deviceRegistry.Pid;
                    string[] devAddress = deviceRegistry.DevicePath.Split(new String[2] { "usbdev", "." }, StringSplitOptions.RemoveEmptyEntries);
                    if (devAddress?.Length > 1)
                    {
                        if (int.TryParse(devAddress[0], out int busNum))
                            BusNumber = busNum;
                        if (int.TryParse(devAddress[1], out int address))
                            DeviceAddress = address;
                    }

                    VIDs = VID.ToString("X2");
                    if (VIDs.Length == 2)
                        VIDs = "00" + VIDs;

                    PIDs = PID.ToString("X2");
                    if (PIDs.Length == 2)
                        PIDs = "00" + PIDs;
                }
            }

            UsbDevice.UsbErrorEvent += ExceptionHandler;

            Configured = true;

        }

        private string GetVIDPIDString()
        {
            return "VID_" + VIDs + " PID_" + PIDs;
        }

        /// <summary>
        /// Запустить работу usb устройства
        /// </summary>
        /// <param name="pingSleepSeconds"></param>
        public bool Start(int pingSleepSeconds = 0)
        {
            if (!IsConfigured())
                return false;

            if (pingSleepSeconds > 0)
            {
                EnablePinging(pingSleepSeconds);
            }

            //Прослушивание событий подключения
            ListenForUSB();
            InitializeBGWDoWorkHandlers();
            bool result = ConnectDevice();

            // Таймер, который будет проверять подключение,
            // что устройство работает и исправно.
            // Windows может перевести USB порт 
            // в спящий режим и не разбудить его, поэтому этот код
            // предварительно проверяет соединение и восстанавливает
            // при необходимости.
            if (GetPingSleepSeconds() > 0)
            {
                if (_timer != null)
                {
                    _timer.Close();
                }
                else
                {
                    _timer = new System.Timers.Timer();
                }
                _timer.AutoReset = false;
                _timer.Interval = GetPingSleepSeconds() * 1000;
                _timer.Elapsed += OnTimerEvent;
                _timer.Start();
            }

            _usbListeningThreadAnswerDelegate = new UsbListeningThreadAnswerDelegate(SetAnswer);
            _readingPacketCompleteDelegate = new ReadingPacketDelegate(GetReadingPacketCompleteState);

            return result;
        }

        private bool ConnectDevice()
        {

            // Открыть устройство, как если бы оно уже находилось в режиме клиента
            bool result = DeviceSetToAccessoryMode();

            return result;
        }

        protected static int GetMaxDataTransferSize()
        {
            return MAX_PACKET_BYTES - PACKET_HEADER_SIZE;
        }

        private void UsbDevice_UsbErrorEvent(object sender, UsbError e)
        {
            try
            {
                throw new Exception(e.Description);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "UsbDevice_UsbErrorEvent", String.Format("Вызвано исключение UsbError: {0}", ex.Message));
            }
        }

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            // ConnectDevice() вызывается, если ссылка на устройство все еще действительна
            // и соединение исправно. Если нет, то он попытается восстановить
            // устройство и подключение.
            try
            {
                ConnectDevice();
                if (_timer != null)
                {
                    _timer.Close();
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Alert, "UPOSUsbDevice", "OnTimerEvent", String.Format("Вызвано исключение: {0}", ex.Message));

                // Ignore the exception if we are trying to reconnect
                // _timer?.Close();
            }
        }

        /// <summary>
        /// Открывает устройство в режиме аксессуаров. Настраивает потоки чтения и записи.
        /// </summary>
        private bool DeviceSetToAccessoryMode()
        {
            lock (DeviceAccessorySyncLock)
            {
                bool initialized = false;
                if (MyUsbDevice == null || !MyUsbDevice.IsOpen || !DeviceRegistry.IsAlive)
                {
                    // Если устройство уже открыто, и устройство долго не использовалось, закроем его, чтобы вернуть в рабочее состояние
                    if (MyUsbDevice != null && !DeviceRegistry.IsAlive)
                    {
                        if (MyUsbDevice.IsOpen)
                        {
                            MyUsbDevice.Close();
                        }
                    }

                    try
                    {

                        if (DeviceRegistry != null)
                        {
                            if (!DeviceRegistry.Open(out MyUsbDevice))
                            {
                                // устройство найдено, но не может быть открыто... Возможно, оно уже используется.
                                throw new Exception($"Устройство {GetVIDPIDString()} не может быть открыто.");
                            }
                        }

                    }
                    catch { }

                    try {

                        if (MyUsbDevice == null)
                        {
                            UsbRegDeviceList deviceList = ScannerManager.GetUsbDevices();
                            if (deviceList?.Count > 0)
                            {
                                foreach (UsbRegistry usbDevice in deviceList)
                                {
                                    if (usbDevice.Vid == VID && usbDevice.Pid == PID)
                                    {
                                        if (usbDevice.Open(out MyUsbDevice))
                                        {
                                            DeviceRegistry = usbDevice;
                                            string[] devAddress = usbDevice.DevicePath.Split(new String[2] { "usbdev", "." }, StringSplitOptions.RemoveEmptyEntries);
                                            if (devAddress?.Length > 1)
                                            {
                                                if (int.TryParse(devAddress[0], out int busNum))
                                                    BusNumber = busNum;
                                                if (int.TryParse(devAddress[1], out int address))
                                                    DeviceAddress = address;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            // устройство найдено, но не может быть открыто... Возможно, оно уже используется.
                                            throw new Exception($"Устройство {GetVIDPIDString()} не может быть открыто.");
                                        }

                                    }
                                }
                            }
                        }

                        if (MyUsbDevice is IUsbDevice wholeUsbDevice)
                        {
                            wholeUsbDevice.SetConfiguration(1);

                            wholeUsbDevice.ClaimInterface(0);
                        }

                        // Find the endpoint read/write pair from the device
                        if (ExtractEndpointPair(MyUsbDevice, out ReadEndpointID readId, out WriteEndpointID writeId))
                        {
                            EPReader = MyUsbDevice.OpenEndpointReader(readId);
                            EPWriter = MyUsbDevice.OpenEndpointWriter(writeId);

                            if (EPReader != null && EPWriter != null)
                            {
                                Shutdown = false;
                                StartListeningForMessages();
                                MessageSendLoop();
                                initialized = true;
                            }
                            else
                            {
                                // Сбой при открытии устройства
                                ConsoleManager.Add(LogType.Alert, "UPOSUsbDevice", "DeviceSetToAccessoryMode", String.Format($"Ошибка при открытии EPReader и EPWriter (r:{(EPReader != null ? "open" : "failed")}, w:{(EPWriter != null ? "open" : "failed")}) для устройства {DeviceRegistry.Vid:X}:{DeviceRegistry.Pid:X}. поток " + Thread.CurrentThread.GetHashCode()));

                                EPReader?.Dispose();
                                EPReader = null;

                                EPWriter?.Dispose();
                                EPWriter = null;

                                if (MyUsbDevice.IsOpen)
                                {
                                    MyUsbDevice.Close();
                                }
                                MyUsbDevice = null;
                            }
                        }
                        else
                        {
                            ConsoleManager.Add(LogType.Alert, "UPOSUsbDevice", "DeviceSetToAccessoryMode", String.Format($"Не удалось найти пару конечных точек чтения/записи для обнаруженного устройства {DeviceRegistry.Vid:X}:{DeviceRegistry.Pid:X}. поток " + Thread.CurrentThread.GetHashCode()));

                            if(MyUsbDevice != null)
                                if (MyUsbDevice.IsOpen)
                                    MyUsbDevice.Close();

                            MyUsbDevice = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "DeviceSetToAccessoryMode", "Вызвано исключение: " + ex.Message);

                        if (MyUsbDevice != null && MyUsbDevice.IsOpen)
                        {
                            MyUsbDevice.Close();
                        }
                        MyUsbDevice = null;
                    }

                    if (initialized)
                    {
                        OnDeviceReady();
                    }
                }
                else
                {
                    initialized = true;
                }
                return initialized;
            }
        }

        private bool ExtractEndpointPair(UsbDevice device, out ReadEndpointID readId, out WriteEndpointID writeId)
        {
            readId = 0;
            writeId = 0;

            if (device != null)
            {
                // Текущий сценарий для наших устройств сканирования - это базовая конфигурация 1 с 1 интерфейсом и 2 массовыми конечными точками
                foreach (UsbConfigInfo config in device.Configs)
                {
                    foreach (UsbInterfaceInfo info in config.InterfaceInfoList)
                    {
                        // Сброс к нулю, чтение / запись должны быть парой на одном интерфейсе
                        readId = 0;
                        writeId = 0;
                        foreach (UsbEndpointInfo endpoint in info.EndpointInfoList)
                        {
                            // Предположим, мы не будем ожидать более 1 чтения и 1 правильного, нет необходимости в быстром цикле выхода
                            // Считыватели имеют высокий бит, установленный
                            if ((endpoint.Descriptor.EndpointID & 0x80) > 0 && readId == 0)
                            {
                                readId = (ReadEndpointID)endpoint.Descriptor.EndpointID;
                            }
                            if ((endpoint.Descriptor.EndpointID & 0x80) == 0 && writeId == 0)
                            {
                                writeId = (WriteEndpointID)endpoint.Descriptor.EndpointID;
                            }
                        }
                        if (readId > 0 && writeId > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Отправить сообщение в очередь
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<AnswerFromDevice> SendMessage(string message)
        {
            _answerFromDevice = new AnswerFromDevice();
            _answerFromDevice.Request = message.HexStringToByteArray(); 

            if (IsConnected())
            {
                _readingPacketComplete = false;
                MessageQueue.Enqueue(message);

                await Task.Run(() =>
                {

                    if (_timerTimeout != null)
                        _timerTimeout.Close();
                    else
                        _timerTimeout = new System.Timers.Timer();

                    _timerTimeout.AutoReset = false;
                    _timerTimeout.Interval = 3000;
                    _timerTimeout.Elapsed += (object sender, ElapsedEventArgs e) => { _usbListeningThreadAnswerDelegate.Invoke(new AnswerFromDevice { Error = Errors.ErrorTimeout, HasAnswer = false, Message = "Сработал таймер Timeout." }); if (_timerTimeout != null) _timerTimeout.Close(); };
                    _timerTimeout.Start();

                    while (!_readingPacketCompleteDelegate.Invoke()) { 
                        Thread.Sleep(500);
                    };

                    return;

                });
            }

            return _answerFromDevice;
        }

        public async Task<AnswerFromDevice> SendBuffer(byte[] buffer)
        {
            _answerFromDevice = new AnswerFromDevice();
            _answerFromDevice.Request = buffer;

            if (IsConnected())
            {
                if (MessageQueue.Count == 0)
                {

                    int actualWrite = 0;
                    int bufferLength = buffer.Length;
                    int packetLength = 1024; //Размер пакета
                    int packets = (int)Math.Ceiling((double)bufferLength / packetLength); //Всего пакетов

                    int lastPacketLength = bufferLength - (packetLength * (packets - 1));
                    int packetsCounter = 1;

                    while (packetsCounter <= packets)
                    {
                        int actualLength = packetsCounter < packets ? packetLength : lastPacketLength;
                        byte[] writeBuff;

                        if (actualLength < 1024)
                        {
                            int bufLength = (int)Math.Ceiling((double)actualLength / 128);
                            bufLength = bufLength * 128 + 5;
                            writeBuff = new byte[bufLength];
                        }
                        else
                            writeBuff = new byte[1029];

                        if (packetsCounter < packets)
                            writeBuff[0] = 0x02;
                        else if (packetsCounter == packets)
                            writeBuff[0] = 0x01;

                        writeBuff[1] = (byte)packetsCounter;
                        writeBuff[2] = (byte)(0xFF - writeBuff[1]);
                        Array.Copy(buffer, actualWrite, writeBuff, 3, actualLength);
                        short crc = calcCrc(buffer.Skip(actualWrite).Take(writeBuff.Length - 5).ToArray());
                        writeBuff[writeBuff.Length - 2] = (byte)((crc >> 8) & 0xFF);
                        writeBuff[writeBuff.Length - 1] = (byte)((crc) & 0xFF);

                        _readingPacketComplete = false;
                        ErrorCode ecWrite = EPWriter.Write(writeBuff, 1000, out int bytesWritten);
                        actualWrite += actualLength;

                        await Task.Run(() =>
                        {

                            if (_timerTimeout != null)
                                _timerTimeout.Close();
                            else
                                _timerTimeout = new System.Timers.Timer();

                            _timerTimeout.AutoReset = false;
                            _timerTimeout.Interval = 3000;
                            _timerTimeout.Elapsed += (object sender, ElapsedEventArgs e) => { _usbListeningThreadAnswerDelegate.Invoke(new AnswerFromDevice { Error = Errors.ErrorTimeout, HasAnswer = false, Message = "Сработал таймер Timeout." }); if (_timerTimeout != null) _timerTimeout.Close(); };
                            _timerTimeout.Start();

                            while (!_readingPacketCompleteDelegate.Invoke()) {
                                Thread.Sleep(500);
                            };

                            return;

                        });

                        if (!_answerFromDevice.HasAnswer)
                            return _answerFromDevice;

                        int procent = (int)((double)(actualWrite * 100) / bufferLength);

                        if (actualWrite >= bufferLength)
                        {
                            ConsoleManager.AddProcent(100);
                            ConsoleManager.Add(" ");
                        }
                        else if (procent % 5 == 1)
                        {
                            ConsoleManager.AddProcent(procent);
                        }

                        packetsCounter++;
                    }
                }
            }

            return _answerFromDevice;
        }

        /// <summary>
        /// Рассчитать контрольную сумму
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static short calcCrc(byte[] data)
        {
            unchecked
            {
                short crc = 0;

                for (int a = 0; a < data.Length; a++)
                {
                    crc ^= (short)(data[a] << 8);
                    for (int i = 0; i < 8; i++)
                    {
                        if ((crc & 0x8000) != 0)
                            crc = (short)((crc << 1) ^ 0x1021);
                        else
                            crc = (short)(crc << 1);
                    }
                }

                return crc;
            }
        }

        private void InitializeBGWDoWorkHandlers()
        {
            ReceiveMessagesThread = new BackgroundWorker();
            SendMessagesThread = new BackgroundWorker();

            SendMessagesDoWorkHandler = delegate
            {
                do
                {
                    try
                    {
                        while (MessageQueue.Count > 0)
                        {
                            string message = MessageQueue.Dequeue();
                            if (message != null)
                            {
                                SendMessageSync(message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "InitializeBGWDoWorkHandlers", String.Format("Вызвано исключение: {0} {1}", e.Message, e.StackTrace));
                    }
                    lock (MessageQueue)
                    {
#if DEBUG
                        GC.Collect(); // use to test for memory leaks
#endif
                        Monitor.Wait(MessageQueue, 1000); // wake up every second and check if it is Shutdown...
                    }
                } while (!Shutdown);
            };

            ReceiveMessagesDoWorkHandler = delegate
            {
                _isReadingPacketDelegate = new IsReadingPacketDelegate(IsReading);
                GetMessages();
            };

            ReceiveMessagesThread.DoWork += ReceiveMessagesDoWorkHandler;
            SendMessagesThread.DoWork += SendMessagesDoWorkHandler;
        }

        public int SendMessageSync(string message)
        {
            BinaryWriter mOutPacketBuffer = new BinaryWriter(new MemoryStream(GetMaxDataTransferSize()));

            ConsoleManager.Add(LogType.Request, "UPOSUsbDevice", "SendMessageSync", message);
            int errorcode = 0;
            if (!string.IsNullOrEmpty(message))
            {
                byte[] stringBytes = message.HexStringToByteArray();

                int stringByteLength = stringBytes.Length;
                if (stringByteLength > REMOTE_STRING_LENGTH_MAX)
                {
                    throw new Exception("Размер строки " + stringByteLength + " байт превышает максимум " + REMOTE_STRING_LENGTH_MAX + " байт.");
                }

                mOutPacketBuffer.Seek(0, SeekOrigin.Begin);
                do
                {
                    // Размер, который мы можем отправить.
                    int sizeOfBuffer = ((MemoryStream)mOutPacketBuffer.BaseStream).Capacity;
                    int remainingSpace = (int)(sizeOfBuffer - mOutPacketBuffer.BaseStream.Position);

                    // Он меньше любого из оставшихся размеров сообщения,
                    // или оставшийся размер буфера.
                    int packetLength = Math.Min(stringByteLength, remainingSpace);

                    // Запись в буфер
                    try
                    {
                        mOutPacketBuffer.Write(stringBytes, 0, packetLength);
                    }
                    catch (Exception e)
                    {
                        ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "SendMessageSync", String.Format("Вызвано исключение: {0}", e.Message));
                    }

                    // Текущая позиция устанавливается на максимум, который может быть записан.
                    // Текущая позиция сбрасывается на ноль
                    WritePacket(mOutPacketBuffer);

                    // We have either nothing left to write, or we wrote the packetLength,
                    // and have more to write.
                    int remainingBytes = Math.Max(0, stringByteLength - packetLength);
                    if (remainingBytes == 0)
                    {
                        break;
                    }

                    // If we need to send additional packets use SetLength(0) to clear out the contents of the buffer
                    // so the remote side doesn't try to interpret old data as continuation info.
                    // // See Scanner UsbAccessoryManager.PacketReader.extract Packet() and handling of mTrailingReadBytes
                    mOutPacketBuffer.BaseStream.SetLength(0);

                } while (true);
            }
            else
            {
                errorcode = EMPTY_STRING;
                ConsoleManager.Add(LogType.Alert, "UPOSUsbDevice", "SendMessageSync", "Строка сообщения пустая.");
            }

            return errorcode;
        }

        /// <summary>
        /// Записывает пакет на подключенное USB-устройство.
        /// </summary>
        /// <param name="outDataBuffer"></param>
        public void WritePacket(BinaryWriter outDataBuffer)
        {
            if (IsConnected() && !EPWriter.IsDisposed)
            {
                try
                {
                    // Последний элемент, который может быть прочитан или записан.
                    int outDataSize = (int)outDataBuffer.BaseStream.Position;
                    if (outDataSize > GetMaxDataTransferSize())
                    {
                        throw new IOException("Размер сообщения слишком велик, " + outDataSize + " байт.");
                    }

                    //BinaryWriter writePacketBuffer = new BinaryWriter(new MemoryStream(outDataSize + 2));
                    BinaryWriter writePacketBuffer = new BinaryWriter(new MemoryStream(outDataSize));

                    // Запись 2-х байтов
                    try
                    {
                        //writePacketBuffer.Write(IPAddress.HostToNetworkOrder((short)outDataSize));
                        writePacketBuffer.Write(((MemoryStream)outDataBuffer.BaseStream).ToArray());
                    }
                    catch (Exception e)
                    {
                        ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "WritePacket", String.Format("Вызвано исключение: {0}", e.Message));
                    }

                    outDataBuffer.Seek(0, SeekOrigin.Begin);

                    ErrorCode ecWrite = ErrorCode.None;
                    int bytesWritten;

                    ecWrite = EPWriter.Write(((MemoryStream)writePacketBuffer.BaseStream).ToArray(), 1000, out bytesWritten);
                    if (ecWrite != ErrorCode.None)
                    {
                        OnDeviceError((int)ecWrite, null, "USB-устройство доступно, но при попытке отправить ему сообщение возникла ошибка. Попробуйте физически отключить/повторно подключить устройство сканера.");
                        ConsoleManager.Add(LogType.Alert, "UPOSUsbDevice", "WritePacket", "USB-устройство доступно, но при попытке отправить ему сообщение возникла ошибка. Попробуйте физически отключить/повторно подключить устройство сканера.");
                    }
                }
                catch (Exception e)
                {
                    ConsoleManager.Add(LogType.Error, "UPOSUsbDevice", "WritePacket", String.Format("Вызвано исключение: {0}", e.Message));
                }
            }
        }

        public override void Dispose()
        {
            Disconnect();
        }

        private void MessageSendLoop()
        {
            if (!SendMessagesThread.IsBusy)
            {
                SendMessagesThread.RunWorkerAsync();
            }
        }

        public void StartListeningForMessages()
        {
            if (!ReceiveMessagesThread.IsBusy)
            {
                ReceiveMessagesThread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Запуск через поток
        /// </summary>
        private void GetMessages()
        {
            while (!Shutdown) { 
                try
                {
                    string message = ReadPacket();
                    _readingPacketState = 2;
                    if (!String.IsNullOrEmpty(message) && IsConnected())
                        OnMessage(message);
                }
                catch { }
            }
        }

        private int IsReading()
        {
            return _readingPacketState;
        }

        static void ExceptionHandler(object sender, UsbError e)
        {
            //ConsoleManager.Add(LogType.Error, "USBScannerTransport", "ReadPacket", String.Format("Вызвано исключение: {0}", e.Description));
        }

        
        private string ReadPacket()
        {
            if (IsConnected() && !EPReader.IsDisposed && _readingPacketState != -1)
            {
                int numBytesRead = 0;
                ErrorCode ecRead = ErrorCode.None;
                List<byte> buffList = new List<byte>();

                try
                {
                    do
                    {
                        _readingPacketState = 1;
                        byte[] readPacket = new byte[EPReader.EndpointInfo.Descriptor.MaxPacketSize];
                        UsbTransfer usbReadTransfer;
                        int transferredIn = 0;

                        ecRead = EPReader.SubmitAsyncTransfer(readPacket, 0, readPacket.Length, 100, out usbReadTransfer);

                        if (usbReadTransfer != null)
                        {
                            WaitHandle.WaitAll(new WaitHandle[] { usbReadTransfer.AsyncWaitHandle }, 200, false);

                            if (!usbReadTransfer.IsCompleted) usbReadTransfer.Cancel();

                            ecRead = usbReadTransfer.Wait(out transferredIn);

                            usbReadTransfer.Dispose();
                        }
                        if (ecRead != ErrorCode.Success && ecRead != ErrorCode.IoTimedOut && ecRead != ErrorCode.IoCancelled)
                        {
                            return "";
                        }

                        if (transferredIn > 0)
                        {

                            if (readPacket[0] != 0x02)
                                buffList.AddRange(readPacket.Take(transferredIn).ToArray());
                            else
                            {
                                short inDataSize = 0;
                                int index = 0;
                                for (index = 0; index < readPacket.Length; index++)
                                {
                                    if (readPacket[index] == 0x02)
                                    {
                                        inDataSize = readPacket[index + 1];
                                        break;
                                    }
                                }

                                if (inDataSize > 0)
                                    buffList.AddRange(readPacket.Skip(index + 2).Take(inDataSize).ToArray());
                            }

                            numBytesRead += transferredIn;
                        }
                    }
                    while (numBytesRead <= 0 && !Shutdown || (ecRead == ErrorCode.Success && numBytesRead > 0));
                }
                catch (Exception e)
                {
                    ConsoleManager.Add(LogType.Error, "USBScannerTransport", "ReadPacket", String.Format("Вызвано исключение: {0}", e.Message));
                }

                if (!Shutdown && (ecRead == ErrorCode.Success || ecRead == ErrorCode.IoTimedOut))
                {
                    byte[] fullbuf = buffList.ToArray();
                    string answerStr = Encoding.UTF8.GetString(fullbuf, 0, fullbuf.Length);

                    if (myDevice.KeyboardState)
                        _keyboardTextInputDelegate.Invoke(answerStr);

                    answerStr = answerStr.Replace("\r", String.Empty);
                    if (answerStr[answerStr.Length - 1] == '\n') 
                        answerStr = answerStr.Substring(0, answerStr.Length - 1);

                    string hexStr = BitConverter.ToString(fullbuf, 0, fullbuf.Length).Replace("-", String.Empty);
                    ConsoleManager.Add(LogType.Response, "UPOSUsbDevice", "ReadPacket", String.Format("\nУстройство N[{0}]\nUTF8 [{1}]\nHEX [{2}]", myDevice.Number, answerStr, hexStr));

                    _usbListeningThreadAnswerDelegate.Invoke(new AnswerFromDevice { Error = (Errors)ecRead, HasAnswer = true, Response = fullbuf, Length = fullbuf.Length, Message = answerStr, HexStr = hexStr });
                    return answerStr;
                }
            }

            return "";
        }

        public bool IsConnected()
        {
            return MyUsbDevice != null && MyUsbDevice.IsOpen;
        }

        public void Disconnect(bool close = false)
        {
            lock (DeviceAccessorySyncLock)
            {
                Shutdown = true;

                OnDeviceDisconnected();

                lock (MessageQueue)
                {
                    Monitor.PulseAll(MessageQueue);
                }

                if (MyUsbDevice != null)
                {
                    try
                    {
                        if (MyUsbDevice.IsOpen)
                        {
                            // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                            // it exposes an IUsbDevice interface. If not (WinUSB) the
                            // 'wholeUsbDevice' variable will be null indicating this is
                            // an interface of a device; it does not require or support
                            // configuration and interface selection.
                            IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                            if (!ReferenceEquals(wholeUsbDevice, null))
                            {
                                // Release interface #0.
                                wholeUsbDevice.ReleaseInterface(0);
                            }

                        }
                        MyUsbDevice.Close();
                        MyUsbDevice = null;
                    }
                    finally
                    {
                        while (_isReadingPacketDelegate.Invoke() != 2) { }

                        // Free usb resources. UsbEndpointReader.Dispose is safe to call multiple times
                        EPReader?.Dispose();
                        EPReader = null;
                        EPWriter?.Dispose();
                        EPReader = null;

                        if (close)
                        {
                            EPReader = null;
                            EPReader = null;
                            _timer.Stop();
                            _timerTimeout.Stop();
                            watchRemoved.Stop();
                            watchInserted.Stop();
                        }
                        else
                            myDevice.SetDeviceMode(InterfaceType.UNKNOWN);
                    }
                }
            }
        }

        private void ListenForUSB()
        {
            ListenForUSBInserts();
            ListenForUSBRemovals();
        }

        private void ListenForUSBInserts()
        {
            //create a query to look for usb devices
            WqlEventQuery w = new WqlEventQuery();
            w.EventClassName = "__InstanceCreationEvent";
            w.Condition = "TargetInstance ISA 'Win32_USBControllerDevice'";
            w.WithinInterval = new TimeSpan(0, 0, 5);

            //use a "watcher", to run the query
            watchInserted = new ManagementEventWatcher(w);
            watchInserted.EventArrived += DeviceInsertedEvent;
            watchInserted.Start();
        }

        private void ListenForUSBRemovals()
        {
            //create a query to look for usb devices
            WqlEventQuery w = new WqlEventQuery();
            w.EventClassName = "__InstanceDeletionEvent";
            w.Condition = "TargetInstance ISA 'Win32_USBControllerDevice'";
            w.WithinInterval = new TimeSpan(0, 0, 5);

            //use a "watcher", to run the query
            watchRemoved = new ManagementEventWatcher(w);
            watchRemoved.EventArrived += DeviceRemovedEvent;
            watchRemoved.Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void DeviceEvent(object sender, EventArrivedEventArgs e, bool inserted)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string value = instance.GetPropertyValue("Dependent").ToString();
            string upperValue = value.ToUpper();

            string merchantPid = "PID_" + PIDs;
            string merchantVid = "VID_" + VIDs;

            if (upperValue.Contains(merchantVid) && upperValue.Contains(merchantPid))
            {
                bool thisDeviceExistInList = false;
                UsbRegDeviceList deviceList = ScannerManager.GetUsbDevices();
                if (deviceList?.Count > 0)
                {
                    foreach (UsbRegistry usbDevice in deviceList)
                    {
                        if (usbDevice.Vid == VID && usbDevice.Pid == PID)
                        {
                            string[] devAddress = usbDevice.DevicePath.Split(new String[2] { "usbdev", "." }, StringSplitOptions.RemoveEmptyEntries);
                            if (devAddress?.Length > 1)
                            {
                                if (int.TryParse(devAddress[0], out int busNum))
                                    if (int.TryParse(devAddress[1], out int address))
                                        if(busNum == BusNumber && address == DeviceAddress)
                                        {
                                            thisDeviceExistInList = true;
                                            break;
                                        }
                            }
                            break;
                        }
                    }
                }

                if (inserted)
                {
                    if (thisDeviceExistInList && !IsConnected()) {
                        ConsoleManager.Add(LogType.Information, "UPOSUsbDevice", "DeviceEvent", String.Format("Устройство {0}: {1}", GetVIDPIDString(), (inserted ? "подсоединено" : "отсоединено")));
                        DeviceSetToAccessoryMode();
                        myDevice.SetDeviceMode(InterfaceType.USBHIDPOS);
                    }
                }
                else
                {
                    if (!thisDeviceExistInList && IsConnected())
                    {
                        ConsoleManager.Add(LogType.Information, "UPOSUsbDevice", "DeviceEvent", String.Format("Устройство {0}: {1}", GetVIDPIDString(), (inserted ? "подсоединено" : "отсоединено")));
                        Disconnect();
                    }
                }
            }
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            DeviceEvent(sender, e, true);
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            DeviceEvent(sender, e, false);
        }

        protected override void OnDeviceDisconnected()
        {
            ConsoleManager.Add(LogType.Information, "UPOSUsbDevice", "OnDeviceDisconnected", $"Выключение USB устройства {GetVIDPIDString()}.");
            Shutdown = true;
            base.OnDeviceDisconnected();
        }
    }

}
