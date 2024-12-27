namespace TimeWatcher.Models;

public record WorkOptions
{
    public int ChatId { get; set; }
    public string TimeZone { get; init; } = TimeZoneInfo.Utc.Id;
    public TimeSpan MessageTimeUtc { get; init; } = default;
}