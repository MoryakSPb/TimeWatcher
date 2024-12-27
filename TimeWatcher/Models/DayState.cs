namespace TimeWatcher.Models;

public enum DayState : byte
{
    Working,
    Holiday,
    PreHoliday,
    HolidayWithPay = 4,
    Playday = 8,
}