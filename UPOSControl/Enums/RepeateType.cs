using System.ComponentModel.DataAnnotations;

namespace UPOSControl.Enums
{
    public enum RepeateType
    {
        [Display(Name = "В секундах")]
        Seconds,
        [Display(Name = "В минутах")]
        Minutes,
        [Display(Name = "В часах")]
        Hours,
        [Display(Name = "В днях")]
        Day,
        [Display(Name = "В неделях")]
        Week,
        [Display(Name = "В месяцах")]
        Mounth,
        [Display(Name = "В годах")]
        Year,
        [Display(Name = "Не повторять")]
        NOTREPEAT = -1
    }
}
