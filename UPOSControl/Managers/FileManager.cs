using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Logging;
using UPOSControl.Utils;
using System.Security.Cryptography;

namespace UPOSControl.Managers
{
    internal class FileManager
    {

        // Имя файла логов
        private static string LogFileName { get; set; }
        // Имя файла конфиг
        private static string ConfigFileName { get; set; }
        // Имя файла конфиг
        private static string EthalonsFileName { get; set; }
        // Дата файла логов
        private static string LogFileDate { get; set; }

        // Путь к папке логов
        private static string PathToLogsDirrectory { get; set; }
        private static string PathToConfigDirrectory { get; set; }

        // Дата активной сессии
        private static DateTime CurrentSessionDateTime { get; set; }

        // Удалять файлы с логами датой ранее
        private static double UnixDateToSaveLogFiles { get; set; }
        // Количество дней сохранять логи
        private static int DaysNumToSaveLogFiles { get; set; }

        static byte[] fileKey =
                    {
                        0x44, 0x33, 0x21, 0x04, 0x07, 0x0D, 0xFF, 0xAD,
                        0x09, 0x01, 0x12, 0x17, 0x34, 0x11, 0x0D, 0x7E
                    };


        /// <summary>
        /// Конструктор
        /// </summary>
        public FileManager()
        {
            string dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            LogFileName = "logfile-";
            PathToLogsDirrectory = dirPath + @"\Logs\";

            PathToConfigDirrectory = dirPath + @"\Configure\";
            ConfigFileName = "configure.json";
            EthalonsFileName = "devices.conf";

        }

        /// <summary>
        /// Функция запуска менеджера
        /// </summary>
        public void Init(int daysCountToSaveLogFiles)
        {
            DaysNumToSaveLogFiles = -daysCountToSaveLogFiles;
            CurrentSessionDateTime = DateTime.Now;
            UnixDateToSaveLogFiles = CurrentSessionDateTime.AddDays(DaysNumToSaveLogFiles).ToEpochTime();
            LogFileDate = CurrentSessionDateTime.ToString("yyyy-MM-dd");

            try
            {
                //Проверяем наличие дирректории
                if (!Directory.Exists(PathToLogsDirrectory))
                {
                    Directory.CreateDirectory(PathToLogsDirrectory);
                }
                //Проверяем наличие дирректории
                if (!Directory.Exists(PathToConfigDirrectory))
                {
                    Directory.CreateDirectory(PathToConfigDirrectory);
                }

                //Очищаем старые файлы
                string[] FilesInDirectory = Directory.GetFiles(PathToLogsDirrectory);
                if (FilesInDirectory?.Count() > 0)
                {
                    int FilesCount = FilesInDirectory.Count();

                    for (int i = 0; i < FilesCount; i++)
                    {
                        FileInfo fileInfo = new FileInfo(FilesInDirectory[i]);
                        string fileDate = fileInfo.Name.Substring(LogFileName.Length, 10);
                        double fileUnixDate = DateTime.Parse(fileDate).ToEpochTime();
                        if (fileUnixDate < UnixDateToSaveLogFiles)
                        {
                            fileInfo.Delete();
                            ConsoleManager.Add(LogType.NotWrite, "FileManager", "Start", "Удалён старый логфайл - " + fileInfo.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "Start", "Вызвано исключение: " + ex.Message);
            }
        }

        public static void AddLog(Log log) {
            try
            {

                if (CurrentSessionDateTime.Day != DateTime.Now.Day)
                {
                    CurrentSessionDateTime = DateTime.Now;
                    UnixDateToSaveLogFiles = CurrentSessionDateTime.AddDays(DaysNumToSaveLogFiles).ToEpochTime();
                    LogFileDate = CurrentSessionDateTime.ToString("yyyy-MM-dd");
                    ConsoleManager.Add(LogType.NotWrite, "FileManager", "AddLog", "Текущий логфайл - " + LogFileName + LogFileDate + ".json");

                    //Очищаем старые файлы
                    string[] FilesInDirectory = Directory.GetFiles(PathToLogsDirrectory);
                    if (FilesInDirectory != null)
                        foreach (string filePath in FilesInDirectory)
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            string fileDate = fileInfo.Name.Substring(LogFileName.Length, 10);
                            double fileUnixDate = DateTime.Parse(fileDate).ToEpochTime();
                            if (fileUnixDate < UnixDateToSaveLogFiles)
                            {
                                fileInfo.Delete();
                                ConsoleManager.Add(LogType.NotWrite, "FileManager", "AddLog", "Удалён старый логфайл - " + fileInfo.Name);
                            }
                        }
                }

                List<Log> logs = ReadLogs();
                if (logs == null)
                    logs = new List<Log>();
                logs.Add(log);

                WriteToJsonFile(PathToLogsDirrectory + LogFileName + LogFileDate + ".json", logs);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "AddLog", "Вызвано исключение: " + ex.Message);
            }
        }

        /// <summary>
        /// Добавить лог в файл
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="objectToWrite"></param>
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, Newtonsoft.Json.Formatting.Indented);

                writer = new StreamWriter(filePath);
                    writer.Write(contentsToWriteToFile);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "WriteToJsonFile", "Вызвано исключение: " + ex.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Записать зашифрованный файл
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="objectToWrite"></param>
        public static void WriteToFile<T>(string filePath, T objectToWrite) where T : new()
        {
            try
            {
                ConsoleManager.Add(LogType.Information, "FileManager", "WriteToFile", "Сохраняю файл: " + filePath);

                using (FileStream fileStream = new(filePath, FileMode.OpenOrCreate))
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = fileKey;

                        byte[] iv = aes.IV;
                        fileStream.Write(iv, 0, iv.Length);

                        var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, Newtonsoft.Json.Formatting.Indented);

                        using (CryptoStream cryptoStream = new(
                            fileStream,
                            aes.CreateEncryptor(),
                            CryptoStreamMode.Write))
                        {
                            using (StreamWriter encryptWriter = new(cryptoStream))
                            {
                                encryptWriter.Write(contentsToWriteToFile);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "WriteToFile", "Вызвано исключение: " + ex.Message);
            }
        }

        /// <summary>
        /// Прочитать JSON файл
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "ReadFromJsonFile", "Вызвано исключение: " + ex.Message);
                return default;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        /// <summary>
        /// Прочитать зашифрованный JSON файл
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T ReadFromFile<T>(string filePath) where T : new()
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    ConsoleManager.Add(LogType.Alert, "FileManager", "ReadFromFile", "Отсутствует файл: " + filePath);
                    return default;
                }

                using (FileStream fileStream = new(filePath, FileMode.Open))
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] iv = new byte[aes.IV.Length];
                        int numBytesToRead = aes.IV.Length;
                        int numBytesRead = 0;
                        while (numBytesToRead > 0)
                        {
                            int n = fileStream.Read(iv, numBytesRead, numBytesToRead);
                            if (n == 0) break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }

                        using (CryptoStream cryptoStream = new(
                           fileStream,
                           aes.CreateDecryptor(fileKey, iv),
                           CryptoStreamMode.Read))
                        {
                            using (StreamReader decryptReader = new(cryptoStream))
                            {
                                string fileContents = decryptReader.ReadToEnd();
                                return JsonConvert.DeserializeObject<T>(fileContents);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.NotWrite, "FileManager", "ReadFromFile", "Вызвано исключение: " + ex.Message);
                return default;
            }
        }

        /// <summary>
        /// Загрузить базу из файлов
        /// </summary>
        /// <returns></returns>
        public static List<Log> ReadLogs(bool allFiles = false)
        {
            try
            {
                if (allFiles)
                {
                    string[] FilesInDirectory = Directory.GetFiles(PathToLogsDirrectory);
                    if (FilesInDirectory?.Count() > 0)
                    {
                        List<string> FilesList = FilesInDirectory.ToList();
                        List<Log> LogsList = new List<Log>();
                        foreach (string filePath in FilesList)
                        {
                            List<Log> _logsL = ReadFromJsonFile<List<Log>>(filePath);

                            if (_logsL?.Count > 0)
                                LogsList.AddRange(_logsL);
                        }

                        if (LogsList.Count > 0)
                            LogsList = Util.SortListByDateTimeToDownWay(LogsList, "TimeRequest");

                        return LogsList;
                    }
                }
                else
                {
                    if (System.IO.File.Exists(PathToLogsDirrectory + LogFileName + LogFileDate + ".json"))
                    {
                        List<Log> _logsL = ReadFromJsonFile<List<Log>>(PathToLogsDirrectory + LogFileName + LogFileDate + ".json");
                        return _logsL;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "FileManager", "ReadLogs", "Вызвано исключение: " + ex.Message);
            }

            return new List<Log>();
        }


        /// <summary>
        /// Загрузить конфиг из файла
        /// </summary>
        /// <returns></returns>
        public UPOSConfiguration ReadConfig()
        {
            try
            {
                string configPath = PathToConfigDirrectory + ConfigFileName;
                ConsoleManager.Add(LogType.Error, "FileManager", "ReadConfig", "Загружаю конфигурацию.");
                if (System.IO.File.Exists(configPath))
                {
                    UPOSConfiguration config = ReadFromJsonFile<UPOSConfiguration>(configPath);
                    if (config != null)
                        return config;
                }
                else
                    ConsoleManager.Add(LogType.Error, "FileManager", "ReadConfig", "Внимание! Файл конфигурации отсутствует.");

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "FileManager", "ReadConfig", "Вызвано исключение: " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Загрузить эталоны из файла
        /// </summary>
        /// <returns></returns>
        public void ReadEthalons()
        {
            try
            {
                string configPath = PathToConfigDirrectory + EthalonsFileName;
                ConsoleManager.Add(LogType.Error, "FileManager", "ReadEthalons", "Загружаю эталоны.");
                if (System.IO.File.Exists(configPath))
                {
                    List<CashDevice> ethalons = ReadFromFile<List<CashDevice>>(configPath);

                    if (ethalons != null)
                        EthalonManager.Ethalons = ethalons;
                }
                else
                    ConsoleManager.Add(LogType.Error, "FileManager", "ReadEthalons", "Внимание! Файл эталонов устройств отсутствует.");

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "FileManager", "ReadEthalons", "Вызвано исключение: " + ex.Message);
            }
        }

        /// <summary>
        /// Записать конфиг в файл
        /// </summary>
        /// <returns></returns>
        public bool SaveConfig(UPOSConfiguration config)
        {
            try
            {
                if (config != null)
                {
                    string configPath = PathToConfigDirrectory + ConfigFileName;
                    if (System.IO.File.Exists(configPath))
                    {
                        System.IO.File.Delete(configPath);
                    }

                    WriteToJsonFile(configPath, config);

                    return true;
                }

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "FileManager", "SaveConfig", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Записать эталоны в файл
        /// </summary>
        /// <returns></returns>
        public bool SaveEthalons()
        {
            try
            {
                if (EthalonManager.Ethalons?.Count > 0 && EthalonManager.Update)
                {
                    string configPath = PathToConfigDirrectory + EthalonsFileName;
                    
                    if (System.IO.File.Exists(configPath))
                    {
                        System.IO.File.Delete(configPath);
                    }

                    WriteToFile(configPath, EthalonManager.Ethalons);

                    return true;
                }

            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "FileManager", "SaveEthalons", "Вызвано исключение: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Прочитать файл
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadFile(string filePath)
        {
            // чтение из файла
            using (FileStream fstream = System.IO.File.OpenRead(filePath))
            {
                // выделяем массив для считывания данных из файла
                byte[] buffer = new byte[fstream.Length];
                // считываем данные
                await fstream.ReadAsync(buffer, 0, buffer.Length);

                return buffer;
            }
        }
    }
}
