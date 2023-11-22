using System.ComponentModel.DataAnnotations;

namespace UPOSControl.Enums
{
    public enum Status
    {
        [Display(Name = "В ОЖИДАНИИ")]
        isWait,
        [Display(Name = "В РАБОТЕ")]
        isWork,
        [Display(Name = "ВЫПОЛНЕНО")]
        isDone,
        [Display(Name = "ОТМЕНЕНО")]
        isCancelled,
        [Display(Name = "НЕ ВЫПОЛНЕНО")]
        isNotDone,
        [Display(Name = "ОТСУТСТВУЕТ")]
        nothing
    }
}
