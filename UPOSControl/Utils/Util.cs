using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.Utils
{
    internal class Util
    {
        

        /// <summary>
        /// Отсортировать список по убыванию
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static List<T> SortListByIntToDownWay<T>(List<T> list, string propertyName = "Number")
        {
            try
            {
                if (list?.Count > 0)
                {
                    List<T> sortedList = new List<T>();
                    sortedList.Add(list.First());
                    for (int i = 1; i < list.Count; i++)
                    {
                        bool addToEnd = true;
                        int intElementI = (int)typeof(T).GetProperty(propertyName).GetValue(list.ElementAt(i), null);

                        for (int s = 0; s < sortedList.Count; s++)
                        {
                            int intElementS = (int)typeof(T).GetProperty(propertyName).GetValue(sortedList.ElementAt(s), null);

                            if (intElementS < intElementI)
                            {
                                sortedList.Insert(s, list.ElementAt(i));
                                addToEnd = false;
                                break;
                            }

                        }

                        if (addToEnd)
                            sortedList.Add(list.ElementAt(i));
                    }

                    return sortedList;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "Util", "SortListByIntToDownWay", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return list;
        }

        /// <summary>
        /// Отсортировать список по возрастанию
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static List<T> SortListByIntToUpWay<T>(List<T> list, string propertyName = "Number")
        {
            try
            {
                if (list?.Count > 0)
                {
                    List<T> sortedList = new List<T>();
                    sortedList.Add(list.First());
                    for (int i = 1; i < list.Count; i++)
                    {
                        bool addToEnd = true;
                        int intElementI = (int)typeof(T).GetProperty(propertyName).GetValue(list.ElementAt(i), null);

                        for (int s = 0; s < sortedList.Count; s++)
                        {
                            int intElementS = (int)typeof(T).GetProperty(propertyName).GetValue(sortedList.ElementAt(s), null);

                            if (intElementS > intElementI)
                            {
                                sortedList.Insert(s, list.ElementAt(i));
                                addToEnd = false;
                                break;
                            }

                        }

                        if (addToEnd)
                            sortedList.Add(list.ElementAt(i));
                    }

                    return sortedList;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "Util", "SortListByIntToUpWay", String.Format("Вызвано исключение: {0}", ex.Message));
            }

            return list;
        }

        /// <summary>
        /// Отсортировать список по убыванию времени (сначала новые)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static List<T> SortListByDateTimeToDownWay<T>(List<T> list, string propertyName = "Update")
        {
            try
            {
                if (list?.Count > 0)
                {
                    List<T> sortedList = new List<T>();
                    sortedList.Add(list.First());
                    for (int i = 1; i < list.Count; i++)
                    {
                        bool addToEnd = true;
                        DateTime dateElementI = (DateTime)typeof(T).GetProperty(propertyName).GetValue(list.ElementAt(i), null);

                        for (int s = 0; s < sortedList.Count; s++)
                        {
                            DateTime dateElementS = (DateTime)typeof(T).GetProperty(propertyName).GetValue(sortedList.ElementAt(s), null);

                            if (dateElementS < dateElementI)
                            {
                                sortedList.Insert(s, list.ElementAt(i));
                                addToEnd = false;
                                break;
                            }

                        }

                        if (addToEnd)
                            sortedList.Add(list.ElementAt(i));
                    }

                    return sortedList;
                }
            }
            catch (Exception e)
            {
                ConsoleManager.Add(LogType.Error, "Util", "SortListByDateTimeToDownWay", String.Format("Вызвано исключение: {0}", e.Message));
            }

            return list;
        }

        /// <summary>
        /// Отсортировать список по возрастанию времени (сначала старые)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static List<T> SortListByDateTimeToUpWay<T>(List<T> list, string propertyName = "Update")
        {
            try
            {
                if (list?.Count > 0)
                {
                    List<T> sortedList = new List<T>();
                    sortedList.Add(list.First());
                    for (int i = 1; i < list.Count; i++)
                    {
                        bool addToEnd = true;
                        DateTime dateElementI = (DateTime)typeof(T).GetProperty(propertyName).GetValue(list.ElementAt(i), null);

                        for (int s = 0; s < sortedList.Count; s++)
                        {
                            DateTime dateElementS = (DateTime)typeof(T).GetProperty(propertyName).GetValue(sortedList.ElementAt(s), null);

                            if (dateElementS > dateElementI)
                            {
                                sortedList.Insert(s, list.ElementAt(i));
                                addToEnd = false;
                                break;
                            }

                        }

                        if (addToEnd)
                            sortedList.Add(list.ElementAt(i));
                    }

                    return sortedList;
                }
            }
            catch (Exception e)
            {
                ConsoleManager.Add(LogType.Error, "Util", "SortListByDateTimeToUpWay", String.Format("Вызвано исключение: {0}", e.Message));
            }

            return list;
        }

    }

    public static class StringHelper
    {
        public static string ASCIIToHexString(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
                sb.AppendFormat("{0:X2}", (int)c);
            return sb.ToString().Trim();
        }

        public static string ToHexString(this string str)
        {
            byte[] bytes = str.IsUnicode() ? Encoding.UTF8.GetBytes(str) : Encoding.Default.GetBytes(str);

            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        public static byte[] HexStringToByteArray(this string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        public static bool IsUnicode(this string input)
        {
            const int maxAnsiCode = 255;

            return input.Any(c => c > maxAnsiCode);
        }
    }
}
