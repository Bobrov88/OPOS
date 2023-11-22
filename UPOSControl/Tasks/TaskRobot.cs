using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Http;
using UPOSControl.Managers;

namespace UPOSControl.Tasks
{
    public class TaskRobot
    {

        // Таймер
        private Timer _taskTimer;
        private Timer _taskTimer2;
        private static bool Busy = false;
        private static bool Busy2 = false;

        private HttpManager _httpManager;
        private static WebPortalSetting _webPortalSetting { get; set; }
        private static ServerState _serverState { get; set; }
        private static List<CashDevice> _devices { get; set; }
        private static List<Tasks.Task> _tasksToSet { get; set; }

        /// <summary>
        /// Запустить робот
        /// </summary>
        /// <param name="webPortalSetting"></param>
        public bool Start(WebPortalSetting webPortalSetting) 
        {
            _webPortalSetting = webPortalSetting;
            _httpManager = new HttpManager(webPortalSetting.Domain);

            _tasksToSet = new List<Task>();

            ConsoleManager.Add(LogType.Information, "TaskManager", "Start", "Запускаю web-портал.");

            //Получаем состояние сервера
            _serverState = _httpManager.GetServerState();

            if (_serverState == null)
            {
                ConsoleManager.Add(LogType.Error, "TaskManager", "Start", "Не удалось получить состояние сервера.");
                return false;
            }

            _taskTimer = new Timer();
            _taskTimer.Interval = _serverState.UpdateInterval * 1000;
            _taskTimer.Elapsed += new ElapsedEventHandler(CheckTasks); //Вызов функции по таймеру
            _taskTimer.Enabled = true;

            _taskTimer2 = new Timer();
            _taskTimer2.Interval = _serverState.UpdateInterval * 1000 + 1000;
            _taskTimer2.Elapsed += new ElapsedEventHandler(SetTasks); //Вызов функции по таймеру
            _taskTimer2.Enabled = true;

            return true;
        }

        public async void CheckTasks(object sender, EventArgs e)
        {

            _taskTimer.Enabled = false;

            try
            {
                if (!Busy2)
                {
                    Busy = true;

                    //Получаем устройства в системе
                    _devices = TaskManager.GetDevices();

                    //Проверяем авторизацию
                    if (!_serverState.Authorized)
                    {
                        //Авторизуемся
                        _serverState.Authorized = _httpManager.Login(_webPortalSetting.Login, _webPortalSetting.Password);

                        if (_serverState.Authorized)
                        {
                            ConsoleManager.Add(LogType.Information, "TaskManager", "CheckTasks", "Получаю эталоны из web-портал.");
                            //Обновляем эталоны
                            _httpManager.GetEthalons();

                            //Получаем устройства из портала                        
                            _devices = _httpManager.PostDevices(_devices);
                            TaskManager.SetDevices(_devices);
                        }

                    }
                    else
                    {
                        List<CashDevice> devicesToSet = _devices.Where(p => String.IsNullOrEmpty(p.KeyId)).ToList();
                        if (devicesToSet?.Count > 0)
                        {
                            //Получаем устройства из портала
                            devicesToSet = _httpManager.PostDevices(devicesToSet);
                            TaskManager.SetDevices(devicesToSet);

                            if (devicesToSet?.Count > 0)
                            {
                                _devices = _devices.Where(p => !String.IsNullOrEmpty(p.KeyId)).ToList();
                                _devices.AddRange(devicesToSet);
                            }
                        }
                    }

                    if (_serverState.Authorized)
                    {
                        foreach (CashDevice device in _devices)
                        {

                            if (!String.IsNullOrEmpty(device.KeyId))
                            {
                                List<Task> tasks = _httpManager.GetNewTasks(device.KeyId);

                                //Выполняем задания
                                if (tasks?.Count > 0)
                                {
                                    ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "По устройству №" + device.Number + " поступило заданий - " + tasks.Count);

                                    int counter = 0;

                                    foreach (Task task in tasks)
                                    {
                                        //Задание уже выполнялось и не отправлено на подтверждение
                                        if (_tasksToSet?.Count > 0)
                                            if (_tasksToSet.Exists(p => p.KeyId == task.KeyId))
                                                continue;

                                        counter++;
                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Задание " + counter);

                                        if (device.NeedToReload)
                                        {
                                            tasks.Select(p =>
                                            {
                                                p.Status = Status.isNotDone;
                                                p.Reason = "Задание не может быть выполнено сейчас.";
                                                return p;
                                            }).ToList();

                                            ConsoleManager.Add(LogType.Information, "TaskManager", "CheckTasks", "Устройство необходимо перезагрузить.");
                                            _tasksToSet.AddRange(tasks);

                                            break;
                                        }

                                        //Прошивка
                                        switch (task.TaskTypeFirmware)
                                        {
                                            //Перепрошивка устройства
                                            case TaskType.SET:
                                                {
                                                    ConsoleManager.Add(LogType.Information, "TaskManager", "CheckTasks", "Задание - перепрошивка устройства.");

                                                    //Скачиваем файл прошивки
                                                    if (task.Firmware == null)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Пустой объект Firmware!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Пустой объект Firmware!");
                                                        break;
                                                    }

                                                    if (task.Firmware.File == null)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Пустой объект File!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Пустой объект File!");
                                                        break;
                                                    }

                                                    if (await _httpManager.DownloadFirmware(task.Firmware.File.KeyId, task.Firmware.File.Name + "." + task.Firmware.File.Extension))
                                                    {
                                                        if (await TaskManager.UpdateFirmware(device.KeyId, task.Firmware.File.Name + "." + task.Firmware.File.Extension))
                                                        {
                                                            task.Status = Status.isDone;
                                                            device.NeedToReload = true;
                                                        }
                                                        else
                                                        {
                                                            task.Status = Status.isNotDone;
                                                            task.Reason = "Не удалось установить прошивку!";
                                                            ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Не удалось установить прошивку!");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Не удалось скачать прошивку!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Не удалось скачать прошивку!");
                                                    }
                                                }
                                                break;
                                            //Получить текущую прошивку устройства
                                            case TaskType.GET:
                                                {
                                                    if (device.Firmware == null)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Прошивка не установлена!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Прошивка не установлена!");
                                                    }
                                                    else
                                                    {
                                                        task.FirmwareKeyId = device.Firmware.KeyId;
                                                        task.Status = Status.isDone;
                                                    }
                                                }
                                                break;

                                        }

                                        //Установка переменных
                                        switch (task.TaskTypeVariables)
                                        {
                                            //Установить переменные
                                            case TaskType.SET:
                                                {
                                                    ConsoleManager.Add(LogType.Information, "TaskManager", "CheckTasks", "Задание - установка переменных.");

                                                    if (task.Variables == null)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Пустой объект Variables!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Пустой объект Variables!");
                                                        break;
                                                    }
                                                    if (task.Variables.Count == 0)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Отсутствуют Variables!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Отсутствуют Variables!");
                                                        break;
                                                    }

                                                    if (await TaskManager.SetVariables(device.KeyId, task.Variables))
                                                    {
                                                        task.Status = Status.isDone;
                                                    }
                                                    else
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Не удалось установить переменные!"; 
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Не удалось установить переменные!");
                                                    }
                                                }
                                                break;
                                            //Получить текущие значения переменных устройства
                                            case TaskType.GET:
                                                {
                                                    List<EthalonVariable> deviceVariables = await TaskManager.GetVariables(device.KeyId);
                                                    if (deviceVariables == null)
                                                    {
                                                        task.Status = Status.isNotDone;
                                                        task.Reason = "Отсутствуют Variables!";
                                                        ConsoleManager.Add(LogType.Alert, "TaskManager", "CheckTasks", "Отсутствуют Variables!");
                                                    }
                                                    else
                                                    {
                                                        task.Variables = deviceVariables;
                                                        task.Status = Status.isDone;
                                                    }
                                                }
                                                break;
                                        }

                                        _tasksToSet.Add(task);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "TaskManager", "CheckTasks", "Вызвано исключение: " + ex.Message);
            }

            Busy = false;
            _taskTimer.Enabled = true;

        }

        public async void SetTasks(object sender, EventArgs e)
        {

            _taskTimer2.Enabled = false;

            try
            {
                if (_tasksToSet?.Count > 0 && !Busy)
                {
                    Busy2 = true;
                    if (_httpManager.PostSetTasks(_tasksToSet))
                    {
                        ConsoleManager.Add(LogType.Information, "TaskManager", "SetTasks", "Задания подтверждены.");
                        _tasksToSet.Clear();
                    }
                    else
                        ConsoleManager.Add(LogType.Information, "TaskManager", "SetTasks", "Не удалось подтвердить задания.");
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "TaskManager", "SetTasks", "Вызвано исключение: " + ex.Message);
            }

            Busy2 = false;
            _taskTimer2.Enabled = true;

        }
    }
}
