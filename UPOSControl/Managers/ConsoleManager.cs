using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UPOSControl.Classes;
using UPOSControl.Enums;
using UPOSControl.Logging;

namespace UPOSControl.Managers
{
    internal class ConsoleManager
    {

        private static bool StatusSetCursor { get; set; } = false;
        private static bool WritePath { get; set; } = false;
        private static Log LastLog { get; set; }

        private static List<Log> Tasks;

        private static string Cursor { get; } = ">>> ";
        private static string In { get; } = "Ответ: ";
        private static string Out { get; } = "Запрос: ";


        private static Thread WriteThread;
        private static bool bWrite { get; set; } = true;

        public static bool WriteSystemMessagesToFile { get; set; } = false;

        public static LogType[] LogsWriteToFile { get; set; } = { LogType.Request, LogType.Response };


        public static void Init()
        {
            Tasks = new List<Log>();

            WriteThread = new Thread(Write);
            WriteThread.Start();
        }

        private static void Write()
        {
            while (bWrite)
            {
                try
                {
                    if (Tasks.Count > 0)
                    {
                        do
                        {
                            Log Log = Tasks.First();
                            if (Log != null)
                            {
                                //Удаляем указатель курсора
                                if (StatusSetCursor || Log.Type == LogType.Procent)
                                {
                                    int currentLineCursor = Console.CursorTop;
                                    Console.SetCursorPosition(0, Console.CursorTop);
                                    Console.Write(new string(' ', Console.WindowWidth));
                                    Console.SetCursorPosition(0, currentLineCursor);
                                }

                                if (Log.NewLine)
                                {
                                    if (Log.Type == LogType.Request)
                                        Console.Write(Out);
                                    else if (Log.Type == LogType.Response)
                                        Console.Write(In);

                                    if (WritePath)
                                        Console.WriteLine(String.Format("[{0}][{1}.{2}] {3}", Log.Type, Log.ClassName, Log.ObjectName, Log.Text));
                                    else
                                        Console.WriteLine(Log.Text);

                                    if (Log.Type == LogType.Response)
                                        Console.Write("\n");

                                    Console.Write(Cursor);
                                }
                                else
                                {
                                    if (WritePath)
                                        Console.Write(String.Format("[{0}][{1}.{2}] {3}", Log.Type, Log.ClassName, Log.ObjectName, Log.Text));
                                    else
                                        Console.Write(Log.Text);
                                }

                                StatusSetCursor = Log.NewLine;
                                if (LogsWriteToFile.Contains(Log.Type) && Log.Type != LogType.NotWrite)
                                    FileManager.AddLog(Log);

                                Tasks.Remove(Log);
                            }
                        }
                        while (Tasks.Count > 0);
                    }
                }
                catch { }
            }
        }

        public static void Add(LogType logType, string className = "", string objectName = "", string logStr = "", bool cursorToNewString = true)
        {
            Log log = new Log
            {
                Type = logType,
                ObjectName = objectName,
                ClassName = className,
                TimeRequest = DateTime.Now,
                UserName = "ConsoleManager",
                Text = logStr,
                NewLine = cursorToNewString
            };

            Tasks.Add(log);
        }

        public static void Add(string str = "")
        {
            Log log = new Log
            {
                Type = LogType.Information,
                ObjectName = "",
                ClassName = "",
                TimeRequest = DateTime.Now,
                UserName = "ConsoleManager",
                Text = str,
                NewLine = true
            };

            Tasks.Add(log);
        }

        public static void AddProcent(int procent = 0)
        {
            string str = "[■";
            int squaresCount = procent / 5; 

            for(int i = 0; i < squaresCount; i++)
            {
                str += '■';
            }
            for (int i = squaresCount; i <= 20; i++)
            {
                str += ' ';
            }
            str += String.Format("] - {0}%", procent);

            Log log = new Log
            {
                Type = LogType.Procent,
                ObjectName = "",
                ClassName = "",
                TimeRequest = DateTime.Now,
                UserName = "ConsoleManager",
                Text = str,
                NewLine = false
            };

            Tasks.Add(log);
        }

        public static string Read()
        {
            string readStr = Console.ReadLine();
            Console.Write(Cursor);
            return readStr;
        }
    }
}
