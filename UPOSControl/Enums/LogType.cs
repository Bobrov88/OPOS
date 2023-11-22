using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Enums
{
    public enum LogType
    {
        [Display(Name = "Информация")]
        Information,
        [Display(Name = "Предупреждение")]
        Alert,
        [Display(Name = "Ошибка")]
        Error,
        [Display(Name = "Приложение запущено")]
        Start,
        [Display(Name = "Запрос")]
        Request,
        [Display(Name = "Ответ")]
        Response,
        [Display(Name = "Без записи в файл")]
        NotWrite,
        [Display(Name = "Проценты")]
        Procent
    }
}
