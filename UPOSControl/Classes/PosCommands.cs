using System;
using System.Collections.Generic;
using System.Linq;
using UPOSControl.Enums;
using UPOSControl.Managers;

namespace UPOSControl.Classes
{
    public class PosCommands
    {
        public List<PosCommand> Commands { get; set; }

        public void Init()
        {
        }

        /// <summary>
        /// Добавить команды в список
        /// </summary>
        /// <param name="commandsList"></param>
        public void AddCommand(List<PosCommand> commandsList)
        {
            if (Commands != null)
                Commands.AddRange(commandsList);
        }

        /// <summary>
        /// Создать комманду
        /// </summary>
        /// <param name="command"></param>
        /// <param name="ethalonKeyId"></param>
        /// <param name="withSystemCmds">С системной группой</param>
        /// <returns></returns>
        internal PosCommand CreateCommand(string command, string ethalonKeyId = "", bool withSystemCmds = true)
        {
            try
            {
                //Поиск команды по эталону
                if(EthalonManager.Ethalons?.Count > 0 && !String.IsNullOrEmpty(ethalonKeyId))
                {
                    EthalonVariable ethalonVar = EthalonManager.GetEthalonVariable(ethalonKeyId, command);
                    if(ethalonVar != null)
                    {
                        if (!withSystemCmds)
                            if (ethalonVar.Group == "System")
                                return null;

                        PosCommand posCommand = new PosCommand {
                            Name = ethalonVar.Name,
                            Type = ethalonVar.CommandType,
                            Description = ethalonVar.Text
                        };

                        if(ethalonVar.CommandType == CommandType.Pre_Cmd1_CmdN_Suf)
                        {
                            posCommand.Commands = ethalonVar.HexValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        }
                        else
                        {
                            posCommand.Command = ethalonVar.HexValue;
                        }

                        return posCommand;
                    }
                }

                if (Commands?.Count > 0)
                {
                    PosCommand posCommand = Commands.FirstOrDefault(pred => pred.Name.ToLower() == command.ToLower());
                    if (posCommand != null)
                        return posCommand;
                }

                throw new Exception($"Команда {command} не найдена.");
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "PosCommands", "CreateCommand", "Вызвано исключение: " + ex.Message);
            }

            return null;
        }

        internal string GetHelp()
        {
            string help = "Список основных команд:\n";

            try
            {
                if (Commands?.Count > 0)
                {
                    int counter = 1;
                    foreach (PosCommand posCommand in Commands)
                    { 
                        try
                        {
                            help += String.Format("{0}: <{1}> \nтип [{2}],\nописание [{3}]\n\n", counter, posCommand.Name, posCommand.Type, posCommand.Description);
                            counter++;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "PosCommands", "GetHelp", "Вызвано исключение: " + ex.Message);
            }

            return help;
        }

        internal string GetHtmlHelp()
        {
            string help = "<br />Список команд для Сканер штрих-кода:";

            try
            {
                if (Commands?.Count > 0)
                {
                    int counter = 1;
                    foreach (PosCommand posCommand in Commands)
                    {
                        try
                        {
                            switch (posCommand.Type)
                            {
                                case CommandType.Pre_CmdCurEnd_Suf: 
                                    {
                                        help += $"<br />{counter}. GET /api/scanner/{posCommand.Name}?device={{device number}} - {posCommand.Description}";
                                    }
                                    break;
                                case CommandType.Pre_CmdValEnd_Suf:
                                    {
                                        help += $"<br />{counter}. GET /api/scanner/{posCommand.Name}?value={{value}}&device={{device number}} - {posCommand.Description}";
                                    }
                                    break;
                                case CommandType.Pre_Cmd_Suf:
                                    {
                                        help += $"<br />{counter}. GET /api/scanner/{posCommand.Name}?device={{device number}} - {posCommand.Description}";
                                    }
                                    break;
                                case CommandType.Pre_Cmd1_CmdN_Suf:
                                    {
                                        help += $"<br />{counter}. GET /api/scanner/{posCommand.Name}?device={{device number}} - {posCommand.Description}";
                                    }
                                    break;
                                case CommandType.Cmd:
                                    {
                                        help += $"<br />{counter}. GET /api/scanner/{posCommand.Name}?device={{device number}} - {posCommand.Description}";
                                    }
                                    break;
                            }
                            
                            counter++;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Add(LogType.Error, "PosCommands", "GetHtmlHelp", "Вызвано исключение: " + ex.Message);
            }

            return help;
        }
    }
}
