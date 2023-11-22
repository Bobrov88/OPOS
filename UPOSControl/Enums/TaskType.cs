using System.ComponentModel.DataAnnotations;

namespace UPOSControl.Enums
{
    public enum TaskType
    {
        [Display(Name = "Получить")]
        GET,
        [Display(Name = "Установить")]
        SET,
        [Display(Name = "Ничего не делать")]
        NOTHING = -1
    }
}
