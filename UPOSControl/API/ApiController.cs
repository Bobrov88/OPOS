using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.API
{

    public class ApiController
    {
        private bool bIsRunning { get; set; } = false;
        private static Thread HttpServerThread;


        private static Api ServerApi { get; set; }
        private static IPAddress ServerIpAddress { get; set; }
        private static ScannerManager _scannerManager;
        private static int ServerPort { get; set; }


        // Делегат на поток
        public delegate Task<AnswerFromDevice> RunCommandFromClient(string request);
        public static RunCommandFromClient myDelegateRunCommandFromClient;

        public delegate void StopHttpServer();
        public static StopHttpServer myDelegateStopHttpServer;

        public ApiController() {

        }

        public static Api GetApiData()
        {
            return ServerApi;
        }


        public async Task<AnswerFromDevice> SubmitCmdToDevice(string request)
        {
            AnswerFromDevice answer = new AnswerFromDevice();

            try
            {
                string[] requestArray = request.Split(new string[] { "/", "api", "&", "?" }, StringSplitOptions.RemoveEmptyEntries);

                //{controller}
                switch (requestArray[0].ToLower())
                {
                    default: 
                        {
                            ArgCommand command = new ArgCommand();

                            for(int i = 0; i < requestArray.Length; i++)
                            {
                                string requestItem = requestArray[i];

                                if (String.IsNullOrEmpty(requestItem))
                                    throw new Exception("Пустое значение.");

                                if (requestItem.Contains("device=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] devArr = requestItem.Split("device=", StringSplitOptions.RemoveEmptyEntries);

                                    if (!int.TryParse(devArr[0], out command.device))
                                        throw new Exception("Неправильно задано устройство.");
                                }
                                else if (requestItem.Contains("number=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] devArr = requestItem.Split("number=", StringSplitOptions.RemoveEmptyEntries);

                                    if (!int.TryParse(devArr[0], out int numberVal))
                                        throw new Exception("Неправильно задано число.");

                                    if (numberVal >= 0 && numberVal <= 255)
                                        command.number = (char)numberVal;
                                    else
                                        throw new Exception("Неправильно задано число.");
                                }
                                else if (requestItem.Contains("hex=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] devArr = requestItem.Split("hex=", StringSplitOptions.RemoveEmptyEntries);

                                    if (String.IsNullOrEmpty(devArr[0]))
                                        throw new Exception("Неправильно задана строка.");

                                    command.hex = devArr[0];
                                }
                                else if(requestItem.Contains("value=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] valArr = requestItem.Split("value=", StringSplitOptions.RemoveEmptyEntries);

                                    if (String.IsNullOrEmpty(valArr[0]))
                                        throw new Exception("Неправильно задано значение.");

                                    if (command.values == null)
                                        command.values = new string[1];
                                    else
                                        Array.Resize(ref command.values, command.values.Length + 1);
                                    command.values[command.values.GetUpperBound(0)] = valArr[0];
                                }
                                else
                                {
                                    if (command.commands == null)
                                        command.commands = new string[1];
                                    else
                                        Array.Resize(ref command.commands, command.commands.Length + 1);

                                    command.commands[command.commands.GetUpperBound(0)] = requestItem;
                                }
                            }

                            answer = await _scannerManager.Control(command.commands, command.device, command.values, command.number, command.hex, null, true);

                        } break;
                    case "exit":
                        {
                            _scannerManager.Stop();
                            Environment.Exit(0);

                        } break;
                }
            }
            catch (Exception ex) {

                if (answer != null)
                {
                    answer.Error = Errors.ErrorOther;
                    answer.Message = ex.Message;
                }
                ConsoleManager.Add(LogType.Error, "ApiController", "SubmitCmdToDevice", "Вызвано исключение: " + ex.Message);
            
            }

            if (answer != null)
                answer.Request = Encoding.UTF8.GetBytes(request);

            return answer;
        }

        

        public void Stop()
        {
            try
            {
                if (bIsRunning)
                {
                    myDelegateStopHttpServer?.Invoke();
                }
                else
                    throw new Exception("Server уже остановлен.");
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ApiController", "Stop", "Вызвано исключение: " + ex.Message);
            }
        }

        internal bool IsRunning()
        {
            return bIsRunning;
        }

        public bool Start(Api api, ScannerManager scannerManager)
        {
            _scannerManager = scannerManager;

            try
            {
                ServerApi = api;

                if (bIsRunning)
                        throw new Exception("Server уже запущен.");
                
                if (String.IsNullOrEmpty(api.Ip))
                    throw new Exception("Ip не может быть пустым.");

                if (api.Ip.Equals("localhost"))
                    ServerIpAddress = IPAddress.Any;
                else
                {
                    IPAddress address = IPAddress.Any;
                    bool result = IPAddress.TryParse(api.Ip, out address);
                    if (!result)
                        throw new Exception("Неправильно указан Ip адрес.");

                    ServerIpAddress = address;
                }

                ServerPort = api.Port;

                //Объявляем функцию при вызове делегата
                myDelegateRunCommandFromClient = new RunCommandFromClient(SubmitCmdToDevice);

                HttpServerThread = new Thread(CreateServer);
                HttpServerThread.Start();

                while(!bIsRunning) { }

                return true;
            }
            catch(Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "ApiController", "Start", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }

        public void CreateServer()
        {
            HttpServer server = new HttpServer(ServerIpAddress, ServerPort);
            bIsRunning = true;
        }

        public string GetHelp()
        {
            return _scannerManager.GetHtmlHelp();
        }
    }

    
}
