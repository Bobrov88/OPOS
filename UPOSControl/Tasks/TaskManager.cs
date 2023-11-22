using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.Tasks
{
    public class TaskManager
    {
        private bool bIsRunning { get; set; } = false;
        private static Thread HttpServerThread;

        private static ScannerManager _scannerManager;


        // Делегат на поток
        public delegate Task<AnswerFromDevice> RunCommandFromClient(string request);
        public static RunCommandFromClient myDelegateRunCommandFromClient;


        public delegate void StopTaskRobot();
        public static StopTaskRobot myDelegateStopTaskRobot;

        public void Stop()
        {
            try
            {
                if (bIsRunning)
                {
                    myDelegateStopTaskRobot?.Invoke();
                }
                else
                    throw new Exception("Task Robot уже остановлен.");
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "TaskManager", "Stop", "Вызвано исключение: " + ex.Message);
            }
        }

        internal bool IsRunning()
        {
            return bIsRunning;
        }

        public bool Start(WebPortalSetting settings, ScannerManager scannerManager)
        {
            _scannerManager = scannerManager;

            try
            {
                if (bIsRunning)
                    throw new Exception("Task Robot уже запущен.");

                //Объявляем функцию при вызове делегата
                myDelegateRunCommandFromClient = new RunCommandFromClient(SubmitCmdToDevice);

                HttpServerThread = new Thread(CreateServer);
                HttpServerThread.Start(settings);

                while (!bIsRunning) {


                    System.Threading.Tasks.Task.Delay(500);

                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "TaskManager", "Start", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }


        public void CreateServer(object settings)
        {
            TaskRobot taskRobot = new TaskRobot();
            taskRobot.Start((WebPortalSetting)settings);
            bIsRunning = true;
        }

        public async Task<AnswerFromDevice> SubmitCmdToDevice(string request)
        {
            return null;
        }

        /// <summary>
        /// Получить текущие устройства
        /// </summary>
        /// <returns></returns>
        public static List<CashDevice> GetDevices()
        {
            if(_scannerManager != null)
                return _scannerManager.GetDevices();

            return new List<CashDevice>();
        }

        /// <summary>
        /// Получить переменные для устройства
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        public static async Task<List<EthalonVariable>> GetVariables(string deviceKey)
        {
            if (_scannerManager != null)
                return await _scannerManager.GetVariables(deviceKey);

            return null;
        }

        /// <summary>
        /// Установить объекты устройств с сервера
        /// </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        public static bool SetDevices(List<CashDevice> devices)
        {
            if (_scannerManager != null)
                return _scannerManager.SetDevices(devices);

            return false;
        }

        /// <summary>
        /// Обновить прошивку устройства
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async static System.Threading.Tasks.Task<bool> UpdateFirmware(string deviceKey, string filePath)
        {
            if (_scannerManager != null)
                return await _scannerManager.UpdateFirmware(deviceKey, filePath);

            return false;
        }

        /// <summary>
        /// Установить переменные для устройства
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public async static System.Threading.Tasks.Task<bool> SetVariables(string deviceKey, List<EthalonVariable> variables)
        {
            if (_scannerManager != null)
                return await _scannerManager.SetVariables(deviceKey, variables);

            return false;
        }
    }
}
