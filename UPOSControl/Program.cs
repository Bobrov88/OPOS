using System;
using System.Collections.Generic;
using System.Threading;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.Tasks;
using WindowsInput;

namespace UPOSControl
{
    internal class Program
    {

        private static UPOSConfiguration _configuration;
        private static ScannerManager _scannerManager;
        private static FileManager _fileManager;
        private static TaskManager _taskManager;

        //Имитация режима KBW для UBS-HID
        public delegate void KeyboardTextInputDelegate(string text);
        public static KeyboardTextInputDelegate _keyboardTextInputDelegate;
        private static InputSimulator _inputSimulator;
        private static bool InputKeyboardState = false;
        private static Thread _cmdThread;
        public delegate void ExitProg();
        public static ExitProg _myDelegateExitProg;
        private static bool Exit = false;

        public static async System.Threading.Tasks.Task Main(string[] args) 
        { 
            try
            {
                _keyboardTextInputDelegate = new KeyboardTextInputDelegate(InputTextBehindKeyboard);
                _inputSimulator = new InputSimulator();

                //Инициализация
                ConsoleManager.Init();
                _fileManager = new FileManager();

                //Чтение эталонов
                _fileManager.ReadEthalons();

                //Чтение конфигурации из файла
                _configuration = _fileManager.ReadConfig();
                if (_configuration == null)
                {
                    _configuration = new UPOSConfiguration();
                    _configuration.Create();
                }

                if (_configuration == null)
                    throw new Exception("Конфигурация отсутствует.");

                _configuration.StartTime = DateTime.Now;
                _configuration.StopCorrectly = false;

                _fileManager.Init(_configuration.LogsPeriod);

                InitConfigure(); 

                //Получение аргументов
                if (args?.Length > 0)
                {
                    List<ArgCommand> CommandsToDo = GetCommandFromString(args);
                    int counter = 1;
                    if(CommandsToDo?.Count > 0)
                    {
                        foreach(ArgCommand commandItem in CommandsToDo)
                        {
                            ConsoleManager.Add(LogType.Error, "Program", "Main", $"{counter}. Команда {ToOneString(commandItem.commands)}. Устройство {commandItem.device}.");
                            await _scannerManager.Control(commandItem.commands, commandItem.device, commandItem.values);
                            counter++;
                        }
                    }
                }
                else
                {
                    StartCMD();
                }
            }
            catch(Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "Program", "Main", "Вызвано исключение: " + ex.Message);
            }
        }

        public static void StartCMD()
        {
            _myDelegateExitProg = new ExitProg(ExitEnv);
            _cmdThread = new Thread(CommandCMD);
            Exit = false;
            _cmdThread.Start(new object[] { _scannerManager, _myDelegateExitProg });
        }

        public static void StopCMD()
        {
            Exit = true;
        }

        private static void ExitEnv()
        {
            _scannerManager.Stop();

            if (_taskManager != null)
                if (_taskManager.IsRunning())
                    _taskManager.Stop();

            _fileManager.SaveEthalons(); 
            
            Environment.Exit(0);
        }

        private static async void CommandCMD(object paramms)
        {
            ScannerManager scannerManager = (ScannerManager)(paramms as object[])[0];
            ExitProg myDelegateExitProg = (ExitProg)(paramms as object[])[1];

            string commandOrig = "";

            while (true)
            {
                while (!Console.KeyAvailable) { Thread.Sleep(100); }

                if (Exit)
                    break;

                commandOrig = ConsoleManager.Read();

                //Если ввод от имитатора ввода, то не выполнять комманду
                if (InputKeyboardState)
                {
                    InputKeyboardState = false;
                    continue;
                }

                if (commandOrig.Equals("exit"))
                    break;
                else
                {
                    string[] argsCmd = commandOrig.Split(' ');
                    List<ArgCommand> CommandsToDo = GetCommandFromString(argsCmd);
                    int counter = 1;
                    if (CommandsToDo?.Count > 0)
                    {
                        foreach (ArgCommand commandItem in CommandsToDo)
                        {
                            ConsoleManager.Add(LogType.Error, "Program", "Main", $"{counter}. Команда {ToOneString(commandItem.commands)}. { (commandItem.device != -1 ? "Устройство " + commandItem.device + "." : "") }");
                            await scannerManager.Control(commandItem.commands, commandItem.device, commandItem.values, commandItem.number, commandItem.hex);
                            counter++;
                        }
                    }
                }
            }

            if (commandOrig.Equals("exit"))
            {
                myDelegateExitProg.Invoke();
            }
        }

        /// <summary>
        /// Ввести текст через сканер
        /// </summary>
        /// <param name="text"></param>
        private static void InputTextBehindKeyboard(string text)
        {
            InputKeyboardState = true;
            _inputSimulator.Keyboard.TextEntry(text);
        }

        /// <summary>
        /// Собрать массив в одну строку
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToOneString(string[] array)
        {
            string response = "";
            foreach (string item in array)
            {
                if (response.Length == 0)
                    response = item;
                else
                    response += " " + item;
            }

            return response;
        }

        /// <summary>
        /// Получить список команд из строки
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static List<ArgCommand> GetCommandFromString(string[] args)
        {
            List<ArgCommand> commandsToDo = new List<ArgCommand>();

            try
            {
                if (args?.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        switch (args[i])
                        {
                            case "-command":
                                {
                                    bool commandEnd = false;
                                    ArgCommand command = new ArgCommand();

                                    if (args.Length <= i + 1)
                                        throw new Exception("Неправильно задана команда.");

                                    for (int j = i + 1; j < args.Length; j++)
                                    {
                                        switch (args[j])
                                        {
                                            default:
                                                {
                                                    if (command.commands == null)
                                                        command.commands = new string[1];
                                                    else
                                                        Array.Resize(ref command.commands, command.commands.Length + 1);

                                                    command.commands[command.commands.GetUpperBound(0)] = args[j];
                                                }
                                                break;
                                            case "-device":
                                                {
                                                    if (args.Length <= j + 1)
                                                        throw new Exception("Неправильно задано устройство.");

                                                    j++;
                                                    if (!int.TryParse(args[j], out command.device))
                                                        throw new Exception("Неправильно задано устройство.");
                                                }
                                                break;
                                            case "-value":
                                                {
                                                    if (args.Length <= j + 1)
                                                        throw new Exception("Неправильно задано значение.");

                                                    j++;
                                                    if (command.values == null)
                                                        command.values = new string[1];
                                                    else
                                                        Array.Resize(ref command.values, command.values.Length + 1);

                                                    command.values[command.values.GetUpperBound(0)] = args[j];
                                                }
                                                break;
                                            case "-number":
                                                {
                                                    if (args.Length <= j + 1)
                                                        throw new Exception("Неправильно задано число.");

                                                    j++;
                                                    if (!int.TryParse(args[j], out int numberVal))
                                                        throw new Exception("Неправильно задано число.");

                                                    if(numberVal >= 0 && numberVal <= 255)
                                                        command.number = (char)numberVal;
                                                    else
                                                        throw new Exception("Неправильно задано число.");

                                                }
                                                break;
                                            case "-hex":
                                                {
                                                    if (args.Length <= j + 1)
                                                        throw new Exception("Неправильно задана строка.");

                                                    j++;
                                                    command.hex = args[j];
                                                }
                                                break;
                                            case "-command":
                                                {
                                                    commandEnd = true;
                                                }
                                                break;
                                        }

                                        if (commandEnd)
                                        {
                                            i = j - 1;
                                            break;
                                        }
                                    }

                                    commandsToDo.Add(command);

                                    if (!commandEnd)
                                        break;
                                } break;
                            case "help":
                                {
                                    string helpStr = ScannerManager.GetHelp();
                                    ConsoleManager.Add(LogType.Information, "Program", "GetCommandFromString", helpStr);
                                } break;
                        }
                    }
                }
            }
            catch { }

            return commandsToDo;
        }

        private static void InitConfigure()
        {
            ConsoleManager.Add(LogType.Information, "Program", "InitConfigure", "Инициализация сервисов.");

            _scannerManager = new ScannerManager();
            _scannerManager.Init(_configuration);
            
            if (_configuration.WebPortal != null)
            {
                if (_configuration.WebPortal.On)
                {
                    _taskManager = new TaskManager();
                    _taskManager.Start(_configuration.WebPortal, _scannerManager);
                    _configuration.WebPortal.Initialized = true;
                }
            }

            ConsoleManager.Add(" ");
        }
    }
}
