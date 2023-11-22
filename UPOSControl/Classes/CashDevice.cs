using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPOSControl.COM;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.USB;

namespace UPOSControl.Classes
{
    public class CashDevice : ICashDevice
    {
        [JsonProperty("number")]
        public int Number { get; set; } = 0;
        [JsonProperty("keyId")]
        public string KeyId { get; set; } = "";
        [JsonProperty("secondKeyId")]
        public string SecondKeyId { get; set; } = "";



        [JsonProperty("ethalonKeyId")]
        public string EthalonKeyId { get; set; }



        [JsonProperty("name")]
        public string Name { get; set; } = ""; //Имя
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; } = ""; //Картинка
        [JsonProperty("text")]
        public string Text { get; set; } = ""; //Описание



        [JsonProperty("authorId")]
        public string AuthorId { get; set; } = ""; //Id автора



        [JsonProperty("group")]
        public string Group { get; set; } = ""; //Группа устройства
        [JsonProperty("type")]
        public string Type { get; set; } = ""; //Тип устройства
        [JsonProperty("interface")]
        public InterfaceType Interface { get; set; } = InterfaceType.UNKNOWN; //Интерфейс устройства
        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = ""; //Имя компании устройства
        [JsonProperty("modelName")]
        public string ModelName { get; set; } = ""; //Имя модели устройства
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; } = ""; //Серийный номер устройства



        [JsonProperty("address")]
        public string Address { get; set; } = ""; //Адрес местонахождения



        [JsonProperty("ip")]
        public string Ip { get; set; } = ""; //Ip адрес устройства
        [JsonProperty("port")]
        public int Port { get; set; } = 4455; //Port адрес устройства



        [JsonProperty("variables")]
        public List<EthalonVariable> Variables { get; set; } //Переменные для устройства



        [JsonProperty("firmware")]
        public Firmware Firmware { get; set; } //Текущая прошивка устройства



        [JsonProperty("tasks")]
        public List<Task> Tasks { get; set; } //Задания для данного устройства



        [JsonProperty("intervalViolation")]
        public bool IntervalViolation { get; set; } = true; //Нарушение интервала
        [JsonProperty("deviceStateOffline")]
        public bool DeviceStateOffline { get; set; } = true; //Состояние устройства - не доступен
        [JsonProperty("firmwareVersionViolation")]
        public bool FirmwareVersionViolation { get; set; } = true; //Нарушение версии дупустимых прошивок



        [JsonProperty("usb")]
        public UPOSUsbDevice Usb { get; set; }
        [JsonProperty("com")]
        public UPOSComDevice Com { get; set; }



        [JsonProperty("keyboardState")]
        public bool KeyboardState { get; set; } = false;


        [JsonIgnore]
        public bool NeedToReload { get; set; } = false;


        /// <summary>
        /// Конструктор
        /// </summary>
        public CashDevice()
        {
            Com = new UPOSComDevice(this);
            Usb = new UPOSUsbDevice(this);
        }

        /// <summary>
        /// Создать устройство
        /// </summary>
        public void Create()
        {
            Name = SetName("CashDevice");
            CompanyName = SetCompanyName("");
            ModelName = SetProductName("");
            SerialNumber = SetSerialNumber();
            Interface = SetInterface();

            switch (Interface)
            {
                case InterfaceType.USBVIRTUALCOM: Com.Configure(); break;
                case InterfaceType.RS232: Com.Configure(); break;
                case InterfaceType.USBHIDPOS: Usb.Configure(); break;
                case InterfaceType.USBKBW: Usb.Configure(); break;
            }
        }

        /// <summary>
        /// Создать устройство
        /// </summary>
        public void Create(InterfaceType type)
        {
            Interface = type;
        }

        /// <summary>
        /// Установить интерфейс
        /// </summary>
        /// <returns></returns>
        private InterfaceType SetInterface()
        {
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetInterface", "1 - USBVIRTUALCOM");
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetInterface", "2 - USBHIDPOS");
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetInterface", "3 - USBKBW");
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetInterface", "4 - RS232");
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetInterface", String.Format("Введите интерфейс (по-умолчанию - {0}): ", "USBVIRTUALCOM"));
            string myInterfaceStr = Console.ReadLine();
            switch (myInterfaceStr)
            {
                case "1": return InterfaceType.USBVIRTUALCOM;
                case "2": return InterfaceType.USBHIDPOS;
                case "3": return InterfaceType.USBKBW;
                case "4": return InterfaceType.RS232;
                default: return InterfaceType.USBVIRTUALCOM; 
            }
        }

        /// <summary>
        /// Установить имя устройства
        /// </summary>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        private string SetName(string defaultName)
        {
            string name;
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetName", String.Format("Введите имя устройства (по-умолчанию - {0}): ", defaultName));
            name = ConsoleManager.Read();

            if (string.IsNullOrEmpty(name))
            {
                name = defaultName;
            }

            return name;
        }

        /// <summary>
        /// Установить имя устройства
        /// </summary>
        /// <returns></returns>
        private string SetSerialNumber()
        {
            string name;
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetSerialNumber", "Введите серийный номер устройства: ");
            name = ConsoleManager.Read();

            if (string.IsNullOrEmpty(name))
            {
                name = "";
            }

            return name;
        }

        /// <summary>
        /// Установить имя компании
        /// </summary>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        private string SetCompanyName(string defaultName)
        {
            string name;
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetCompanyName", String.Format("Введите название компании (по-умолчанию - {0}): ", defaultName));
            name = ConsoleManager.Read();

            if (string.IsNullOrEmpty(name))
            {
                name = defaultName;
            }

            return name;
        }

        /// <summary>
        /// Установить имя продукта
        /// </summary>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        private string SetProductName(string defaultName)
        {
            string name;
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetProductName", String.Format("Введите название модели устройства (по-умолчанию - {0}): ", defaultName));
            name = ConsoleManager.Read();

            if (string.IsNullOrEmpty(name))
            {
                name = defaultName;
            }

            return name;
        }

        /// <summary>
        /// Отправить команду на устройство
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<AnswerFromDevice> SendRequestToDevice(string command)
        {
            switch (Interface)
            {
                case InterfaceType.USBVIRTUALCOM: return await Com.SubmitCmd(command);
                case InterfaceType.USBHIDPOS: return await Usb.SendMessage(command);
                case InterfaceType.USBKBW: return await Usb.SendMessage(command);
                case InterfaceType.RS232: return await Com.SubmitCmd(command);
            }

            return null;
        }

        /// <summary>
        /// Поменять состояние эмуляции клавиатуры
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public void SendKeyboardState(bool state)
        {
            KeyboardState = state;
        }

        /// <summary>
        /// Передать данные на устройтсво
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public async Task<AnswerFromDevice> SendBufferToDevice(byte[] buffer, CancellationToken cancellation)
        {
            switch (Interface)
            {
                case InterfaceType.USBVIRTUALCOM: return Com.SubmitCmd(buffer, cancellation);
                case InterfaceType.USBHIDPOS: return await Usb.SendBuffer(buffer);
                case InterfaceType.USBKBW: return await Usb.SendBuffer(buffer);
                case InterfaceType.RS232: return Com.SubmitCmd(buffer, cancellation);
            }

            return null;
        }

        public void Stop()
        {
            switch (Interface)
            {
                case InterfaceType.USBVIRTUALCOM: Com.Stop(); break;
                case InterfaceType.USBHIDPOS: Usb.Disconnect(true); break;
                case InterfaceType.USBKBW: Usb.Disconnect(true); break;
                case InterfaceType.RS232: Com.Stop(); break;
            }
        }

        public bool Start()
        {
            switch (Interface)
            {
                case InterfaceType.USBVIRTUALCOM:
                case InterfaceType.RS232:
                    {
                        if (!Com.IsConnected() && Com.IsConfigured())
                            return Com.Start();
                    }
                    break;
                case InterfaceType.USBHIDPOS:
                case InterfaceType.USBKBW:
                    {
                        if (!Usb.IsConnected() && Usb.IsConfigured())
                            return Usb.Start(1000);
                    }
                    break;
                case InterfaceType.UNKNOWN:
                    {
                        if (Com.IsConfigured())
                            return Com.Start(false);
                        if (Usb.IsConfigured())
                            return Usb.Start(1000);
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Устанавливает режим для устройства
        /// </summary>
        /// <param name="interfaceType"></param>
        public void SetDeviceMode(InterfaceType interfaceType)
        {
            Interface = interfaceType;
            ConsoleManager.Add(LogType.Information, "CashDevice", "SetDeviceMode", String.Format("Устройство N[{0}]: переход в режим {1}", Number, Interface));
        }

        /// <summary>
        /// Получить информацию об устройстве
        /// </summary>
        /// <returns></returns>
        public string GetInfo() {

            string deviceStr = "Устройство N[" + Number + "]" +
                "\nИнтерфейс [" + Interface.ToString() + "]" +
                "\nИмя [" + Name + "]" +
                "\nПроизводитель [" + CompanyName + "]" +
                "\nМодель [" + ModelName + "]" +
                "\nСерийный номер [" + SerialNumber + "]" +
                "\nВерсия прошивки [" + Firmware?.Name + "]";

            if (Com.IsConfigured())
            {
                deviceStr +=
                "\nПорт [" + Com.PortName + "]";
            }

            if (Usb.IsConfigured())
            {
                deviceStr +=
                "\nVendorId [" + Usb.VIDs + "]" +
                "\nProductId [" + Usb.PIDs + "]";
            }

            return deviceStr;
        }

    }
}
