using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.Utils;

namespace UPOSControl.COM
{
    public class UPOSComDevice
    {
        [JsonProperty("portName")]
        public string PortName { get; set; }
        [JsonProperty("baudRate")]
        public int BaudRate { get; set; }
        [JsonProperty("parity")]
        public Parity Parity { get; set; }
        [JsonProperty("dataBits")]
        public int DataBits { get; set; }
        [JsonProperty("stopBits")]
        public StopBits StopBits { get; set; }
        [JsonProperty("handshake")]
        public Handshake Handshake { get; set; }
        [JsonProperty("readTimeout")]
        public int ReadTimeout { get; set; }
        [JsonProperty("writeTimeout")]
        public int WriteTimeout { get; set; }

        [JsonProperty("configured")]
        private bool Configured { get; set; }
        private bool TryToOpen { get; set; } = false;

        private SerialPort _serialPort;
        private Thread _readThread;

        private delegate void SerialListeningThreadAnswerDelegate(AnswerFromDevice answer);
        private SerialListeningThreadAnswerDelegate _serialListeningThreadAnswerDelegate;

        private delegate void SerialListeningThreadRequestDelegate();
        private SerialListeningThreadRequestDelegate _serialListeningThreadRequestDelegate;

        private delegate void SerialListeningThreadPauseDelegate(bool state);
        private SerialListeningThreadPauseDelegate _serialListeningThreadPauseDelegate;

        

        private AnswerFromDevice _answerFromDevice;
        private bool bRComplete { get; set; } = false;
        private bool bWComplete { get; set; } = false;
        private bool bPause { get; set; } = false;


        private CashDevice myDevice;

        /// <summary>
        /// Работает или нет
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        /// <summary>
        /// Настроен или нет
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return Configured;
        }

        public UPOSComDevice(CashDevice parent)
        {
            _serialPort = new SerialPort();

            myDevice = parent;
        }

        /// <summary>
        /// Ввести данные
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="portName"></param>
        public void Configure(bool inputData = true, string portName = "") {

            if (inputData)
            {
                PortName = SetPortName("");
                BaudRate = SetPortBaudRate(9600);
                Parity = SetPortParity(Parity.None);
                DataBits = SetPortDataBits(8);
                StopBits = SetPortStopBits(StopBits.One);
                Handshake = SetPortHandshake(Handshake.None);
                ReadTimeout = 1000;
                WriteTimeout = 1000;
            }
            else
            {
                PortName = portName;
                BaudRate = 9600;
                Parity = Parity.None;
                DataBits = 8;
                StopBits = StopBits.One;
                Handshake = Handshake.None;
                ReadTimeout = 2000;
                WriteTimeout = 2000;
            }

            Configured = true;

        }

        /// <summary>
        /// Установить порт
        /// </summary>
        /// <param name="defaultPortName"></param>
        /// <returns></returns>
        private string SetPortName(string defaultPortName)
        {
            string portName;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortName", "Доступные порты:");
            foreach (string s in SerialPort.GetPortNames())
            {
                ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortName", String.Format("{0}", s));
            }

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortName", String.Format("Введите номер COM порта (по-умолчанию - 1): ", defaultPortName));
            portName = ConsoleManager.Read();

            if (string.IsNullOrEmpty(portName) || !int.TryParse(portName, out int n))
            {
                portName = defaultPortName;
            }
            else portName = "COM" + portName;

            return portName;
        }

        /// <summary>
        /// Пропускная способность
        /// </summary>
        /// <param name="defaultPortBaudRate"></param>
        /// <returns></returns>
        private int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortBaudRate", String.Format("BaudRate (по-умолчанию - {0}): ", defaultPortBaudRate));
            baudRate = ConsoleManager.Read();

            if (string.IsNullOrEmpty(baudRate) || !int.TryParse(baudRate, out int n))
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        /// <summary>
        /// Установить параметр для порта
        /// </summary>
        /// <param name="defaultPortParity"></param>
        /// <returns></returns>
        private Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortParity", "PortParity:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortParity", String.Format("{0}", s));
            }

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortParity", String.Format("Введите значение PortParity (по-умолчанию - {0}): ", defaultPortParity.ToString()));
            parity = ConsoleManager.Read();

            if (string.IsNullOrEmpty(parity))
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }

        /// <summary>
        /// Установить количество битов данных
        /// </summary>
        /// <param name="defaultPortDataBits"></param>
        /// <returns></returns>
        private int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortDataBits", String.Format("Введите значение DataBits (по-умолчанию - {0}): ", defaultPortDataBits));
            dataBits = ConsoleManager.Read();

            if (string.IsNullOrEmpty(dataBits) || !int.TryParse(dataBits, out int n))
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        /// <summary>
        /// Установить Стоп бит
        /// </summary>
        /// <param name="defaultPortStopBits"></param>
        /// <returns></returns>
        private StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortStopBits", "nStopBits:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortStopBits", String.Format("{0}", s));
            }

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortStopBits", String.Format("Введите значение StopBits (по-умолчанию - {0}): ", defaultPortStopBits.ToString()));
            stopBits = ConsoleManager.Read();

            if (string.IsNullOrEmpty(stopBits))
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }

        /// <summary>
        /// Установить параметр порта
        /// </summary>
        /// <param name="defaultPortHandshake"></param>
        /// <returns></returns>
        private Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortHandshake", "HandShake:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortHandshake", String.Format("{0}", s));
            }

            ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SetPortHandshake", String.Format("Введите значение HandShake (по-умолчанию - {0}): ", defaultPortHandshake.ToString()));
            handshake = ConsoleManager.Read();

            if (string.IsNullOrEmpty(handshake))
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }
        
        /// <summary>
        /// Запустить работу порта
        /// </summary>
        public bool Start(bool printException = true)
        {
            try
            {
                if (!IsConnected() && Configured)
                {
                    TryToOpen = true;

                    _serialPort.PortName = PortName;
                    _serialPort.BaudRate = BaudRate;
                    _serialPort.Parity = Parity;
                    _serialPort.DataBits = DataBits;
                    _serialPort.StopBits = StopBits;
                    _serialPort.Handshake = Handshake;

                    _serialPort.ReadTimeout = ReadTimeout;
                    _serialPort.WriteTimeout = WriteTimeout;
                    _serialPort.Open();

                    StartListening();

                    //Теперь устройство работает в режиме COM
                    myDevice.SetDeviceMode(InterfaceType.USBVIRTUALCOM);
                }
            }
            catch (Exception ex)
            {
                if(printException)
                    ConsoleManager.Add(LogType.Error, "UPOSComDevice", "Start", String.Format("Вызвано исключение: {0}", ex.Message));
                TryToOpen = false;
            }

            return IsConnected();
        }

        /// <summary>
        /// Запустить прослушивание
        /// </summary>
        public void StartListening()
        {
            if (IsConnected())
            {
                Console.WriteLine("311 StartListening");
                _answerFromDevice = new AnswerFromDevice();
                _serialListeningThreadAnswerDelegate = new SerialListeningThreadAnswerDelegate(SetAnswer);
                Console.WriteLine("314 StartListening");
                _readThread = new Thread(Listening);
                _readThread.Start(_serialPort);
            }
        }

        /// <summary>
        /// Проверка COM устройства
        /// </summary>
        /// <param name="comName"></param>
        /// <returns>Предполагаемый эталон устрйоства</returns>
        public string TryCheckDevice(string comName)
        {
            try
            {
                if (!IsConnected())
                { 
                    Console.WriteLine("330 in UPOS");
                    Configure(false, comName);

                    TryToOpen = true;

                    _serialPort.PortName = PortName;
                    Console.WriteLine("PortName="+ PortName);
                    _serialPort.BaudRate = BaudRate;
                    Console.WriteLine("BaudRate="+ BaudRate);
                    _serialPort.Parity = Parity;
                    Console.WriteLine("Parity="+ Parity);
                    _serialPort.DataBits = DataBits;
                    Console.WriteLine("DataBits="+DataBits);
                    _serialPort.StopBits = StopBits;
                    Console.WriteLine("StopBits="+StopBits);
                    _serialPort.Handshake = Handshake;


                    _serialPort.ReadTimeout = ReadTimeout;
                    _serialPort.WriteTimeout = WriteTimeout;
                    _serialPort.Open();

                    var stream = _serialPort.BaseStream;
                    stream.ReadTimeout = 1000;
                    stream.Flush();

                    string init1Cmd = "";
                    string init2Cmd = "";

                    byte[] datacmd;
                    string hexStr;
                    string answerStr;

                    // if (EthalonManager.Ethalons?.Count > 0)
                    if (true)
                    {
                        //       foreach(CashDevice ethalon in EthalonManager.Ethalons)
                        //       {
                        //      init1Cmd = ethalon.Variables?.FirstOrDefault(p => p.Name.ToLower() == "Init1".ToLower())?.HexValue;
                        //      init2Cmd = ethalon.Variables?.FirstOrDefault(p => p.Name.ToLower() == "Init2".ToLower())?.HexValue;
                        //      answerStr = ethalon.Variables?.FirstOrDefault(p => p.Name.ToLower() == "AnswerInit".ToLower())?.HexValue;

                        //  if (String.IsNullOrEmpty(init1Cmd))
                        //        continue;
                        init1Cmd = "";
                        Console.WriteLine("before hex");
                        datacmd = init1Cmd.HexStringToByteArray();
                        Console.WriteLine("after hex");
                        _serialPort.Write(datacmd, 0, datacmd.Length);
                        Console.WriteLine("after com write");
                        hexStr = "";
                        Console.WriteLine("379 in UPOS");
                    /*    while (true)
                            {
                            Console.WriteLine("382 in UPOS");
                            try
                                {
                                    byte[] buf = new byte[256];
                                    int actuallyRead = stream.Read(buf, 0, buf.Length);
                                    hexStr += BitConverter.ToString(buf, 0, actuallyRead).Replace("-", String.Empty);
                                Console.WriteLine("388 in UPOS");
                            }
                                catch (IOException) { break; }
                                catch (TimeoutException) { break; }
                                catch (Exception) { break; }
                            }*/

                            //if (hexStr.Contains(answerStr))
                            if (true)
                            {
                                //Найдено новое устройство
                                ConsoleManager.Add(LogType.Information, "UPOSComDevice", "TryCheckDevice", String.Format("Найдено " + init1Cmd/*ethalon.CompanyName*/ + " новое устройство: {0}", comName));

                           //     if (!String.IsNullOrEmpty(init2Cmd))
                         //       {
                            //        datacmd = init2Cmd.HexStringToByteArray();
                            //        _serialPort.Write(datacmd, 0, datacmd.Length);
                          //      }

                                StartListening();

                            //return ethalon.keyId;
                                return "COM4";
                            }

                  //      }
                    }

            //        stream.Close();
            //        _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "TryCheckDevice", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return "";
        }

        /// <summary>
        /// Отправить команду
        /// </summary>
        /// <param name="cmdLine"></param>
        /// <param name="encodingType"></param>
        public async Task<AnswerFromDevice> SubmitCmd(string cmdLine, string encodingType = "HEX")
        {
            try
            {
                if (IsConnected())
                {

                    byte[] datacmd = null;

                    switch (encodingType)
                    {
                        case "ASCII":
                            {
                                datacmd = Encoding.ASCII.GetBytes(cmdLine);
                            }
                            break;
                        case "HEX":
                            {
                                datacmd = cmdLine.HexStringToByteArray();
                            }
                            break;
                        case "UTF8":
                            {
                                datacmd = Encoding.UTF8.GetBytes(cmdLine);
                            }
                            break;
                        case "UTF16":
                            {
                                datacmd = Encoding.Unicode.GetBytes(cmdLine);
                            }
                            break;
                    }

                    ConsoleManager.Add(LogType.Request, "UPOSComDevice", "SubmitCmd", String.Format("\nУстройство N[{0}]\n[{1}][{2}]", myDevice.Number, encodingType, cmdLine));

                    _answerFromDevice = new AnswerFromDevice();
                    _answerFromDevice.Request = datacmd;

                    _serialPort.Write(datacmd, 0, datacmd.Length);
                    _serialListeningThreadRequestDelegate.Invoke();

                    await Task<AnswerFromDevice>.Run(() => {

                        while (!bRComplete) { };
                        bRComplete = false;

                        return _answerFromDevice;

                    });
                }
                else throw new Exception("Порт не открыт.");
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "SubmitCmd", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return _answerFromDevice;
        }

        /// <summary>
        /// Отправить буффер
        /// </summary>
        /// <param name="buffer"></param>
        public AnswerFromDevice SubmitCmd(byte[] buffer, CancellationToken cancellation)
        {
            _answerFromDevice = new AnswerFromDevice();
            _answerFromDevice.Request = buffer;

            try
            {
                if (IsConnected())
                {
                    ConsoleManager.Add(LogType.Information, "UPOSComDevice", "SubmitCmd", String.Format("Устройство N[{0}]\nПередача файла начата.", myDevice.Number));

                    _answerFromDevice = new AnswerFromDevice();
                    _answerFromDevice.Request = buffer;
                    _serialListeningThreadPauseDelegate.Invoke(true);

                    int actualWrite = 0;
                    int bufferLength = buffer.Length;
                    var stream = _serialPort.BaseStream;
                    stream.ReadTimeout = 1000;
                    stream.Flush();

                    int packetLength = 1024; //Размер пакета
                    int packets = (int)Math.Ceiling((double)bufferLength / packetLength); //Всего пакетов

                    int lastPacketLength = bufferLength - (packetLength * (packets - 1));
                    int packetsCounter = 1;

                    while (packetsCounter <= packets && !cancellation.IsCancellationRequested)
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
                        else if(packetsCounter == packets)
                            writeBuff[0] = 0x01;

                        writeBuff[1] = (byte)packetsCounter;
                        writeBuff[2] = (byte)(0xFF - writeBuff[1]);
                        Array.Copy(buffer, actualWrite, writeBuff, 3, actualLength);
                        short crc = calcCrc(buffer.Skip(actualWrite).Take(writeBuff.Length - 5).ToArray());
                        writeBuff[writeBuff.Length - 2] = (byte)((crc >> 8) & 0xFF);
                        writeBuff[writeBuff.Length - 1] = (byte)((crc) & 0xFF);

                        try
                        {
                            _serialPort.Write(writeBuff, 0, writeBuff.Length);
                        }
                        catch (IOException io)
                        {
                            if (!cancellation.IsCancellationRequested)
                                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "SubmitCmd", String.Format("Устройство N[{0}]\n Ошибка ввода/вывода: {0}", myDevice.Number, io.Message));
                            
                            break;
                        }
                        catch (TimeoutException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        actualWrite += actualLength;

                        while (_serialPort.BytesToRead == 0) { }

                        try
                        {
                            byte[] buf = new byte[1];
                            int actuallyRead = stream.Read(buf, 0, buf.Length);

                            if (actuallyRead > 0)
                            {
                                if (buf[0] != 0x06)
                                    return _answerFromDevice;
                            }
                            else
                                return _answerFromDevice;

                        }
                        catch (IOException io)
                        {
                            if (!cancellation.IsCancellationRequested)
                                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "SubmitCmd", String.Format("Устройство N[{0}]\n Ошибка ввода/вывода: {0}", myDevice.Number, io.Message));
                            break;
                        }
                        catch (TimeoutException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        int procent = (int)((double)(actualWrite * 100) / bufferLength);

                        if(actualWrite >= bufferLength) 
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

                    _serialListeningThreadPauseDelegate.Invoke(false);

                    if(cancellation.IsCancellationRequested)
                    { 
                        _answerFromDevice.HasAnswer = false;
                        _answerFromDevice.Message = "Отмена передачи.";
                        _answerFromDevice.Error = Errors.ErrorIOCancelled;
                    }
                    else
                        _answerFromDevice.HasAnswer = true;

                    return _answerFromDevice;
                }
                else throw new Exception("Порт не открыт.");
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "SubmitCmd", String.Format("Вызвано исключение: {0}", ex.Message));
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


        /// <summary>
        /// Остановить порт
        /// </summary>
        public void Stop()
        {
            try
            {
                _serialListeningThreadPauseDelegate?.Invoke(false);

                if (_serialPort.IsOpen)
                    _serialPort.Close();

                if (TryToOpen)
                {
                    TryToOpen = false;
                    myDevice.SetDeviceMode(InterfaceType.UNKNOWN);
                }
            }
            catch (NullReferenceException ex1)
            {
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "Stop", String.Format("Вызвано исключение: {0}", ex.Message));
            }
        }


        /// <summary>
        /// Вернуть ответ от устройства
        /// </summary>
        /// <param name="answer"></param>
        private void SetAnswer(AnswerFromDevice answer)
        {
            _answerFromDevice.Length = answer.Length;
            _answerFromDevice.Message = answer.Message;
            _answerFromDevice.Response = answer.Response;
            _answerFromDevice.HexStr = answer.HexStr;
            _answerFromDevice.Error = answer.Error;
            _answerFromDevice.HasAnswer = answer.HasAnswer;
            bRComplete = true;
        }

        /// <summary>
        /// Установка параметра
        /// </summary>
        private void SetRequest()
        {
            bWComplete = true;
        }

        /// <summary>
        /// Установка параметра
        /// </summary>
        private void Pause(bool state)
        {
            bPause = state;
        }

        /// <summary>
        /// Начать прослушку порта
        /// </summary>
        /// <param name="obj"></param>
        private void Listening(object obj)
        {
            _serialPort = (SerialPort)obj;
            _serialListeningThreadRequestDelegate = new SerialListeningThreadRequestDelegate(SetRequest);
            _serialListeningThreadPauseDelegate = new SerialListeningThreadPauseDelegate(Pause);

            try
            {
                using (var stream = _serialPort.BaseStream) { 

                    stream.Flush();
                    Errors err = Errors.Success;
                    List<byte> buffList = new List<byte>();
                    string answerStr = "";
                    string hexStr = "";
                    bool hasAnswer = false;
                    byte[] fullbuf = null;

                    while (_serialPort.IsOpen)
                    {
                        while (bPause) { };

                        int bufSize = _serialPort.BytesToRead;
                        if (bufSize > 0 || bWComplete)
                        {
                            while (err == Errors.Success)
                            {
                                byte[] buf = new byte[1024];
                                int actuallyRead = 0;

                                try
                                {
                                    actuallyRead = stream.Read(buf, 0, buf.Length);
                                    buffList.AddRange(buf.Take(actuallyRead).ToArray());
                                }
                                catch (IOException io)
                                {
                                    err = Errors.ErrorIO;
                                    ConsoleManager.Add(LogType.Error, "UPOSComDevice", "Listening", String.Format("Устройство N[{0}]\n Ошибка ввода/вывода: {0}", myDevice.Number, io.Message));
                                    break;
                                }
                                catch (TimeoutException)
                                {
                                    err = Errors.ErrorTimeout;
                                    break;
                                }
                                catch (Exception)
                                {
                                    err = Errors.ErrorOther;
                                    break;
                                }
                            }

                            if (buffList.Count > 0)
                            {
                                if (err == Errors.ErrorTimeout)
                                    err = Errors.Success;

                                hasAnswer = true;
                                fullbuf = buffList.ToArray();
                                answerStr = Encoding.UTF8.GetString(fullbuf, 0, fullbuf.Length).Replace("\r", String.Empty);
                                if (answerStr[answerStr.Length - 1] == '\n') answerStr = answerStr.Substring(0, answerStr.Length - 1);

                                hexStr = BitConverter.ToString(fullbuf, 0, fullbuf.Length).Replace("-", String.Empty);
                                ConsoleManager.Add(LogType.Response, "UPOSComDevice", "Listening", String.Format("\nУстройство N[{0}]\nUTF8 [{1}]\nHEX [{2}]", myDevice.Number, answerStr, hexStr));

                            }

                            _serialListeningThreadAnswerDelegate.Invoke(new AnswerFromDevice { Error = err, HasAnswer = hasAnswer, Response = fullbuf, Length = fullbuf == null ? 0 : fullbuf.Length, Message = answerStr, HexStr = hexStr });

                            err = Errors.Success;
                            buffList = new List<byte>();
                            answerStr = "";
                            hexStr = "";
                            hasAnswer = false;
                            fullbuf = null;
                            bWComplete = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "UPOSComDevice", "Listening", String.Format("Вызвано исключение: {0}", ex.Message));
            }
            Stop();
        }
    }
}
