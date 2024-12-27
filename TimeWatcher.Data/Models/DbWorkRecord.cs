using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgBotFrame.Commands.Authorization.Models;

namespace TimeWatcher.Data.Models;

public class DbWorkRecord : IEntityTypeConfiguration<DbWorkRecord>, IEquatable<DbWorkRecord>
{
    public required long UserId { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly Arrived { get; set; }
    public TimeOnly? Left { get; set; }
    public DbUser User { get; init; } = null!;
    public DbWorkMessage Message { get; init; } = null!;

    public void Configure(EntityTypeBuilder<DbWorkRecord> builder)
    {
        builder.HasKey(x => new { x.UserId, x.Date });

        builder.Property(x => x.Date).HasConversion(x => x.DayNumber, x => DateOnly.FromDayNumber(x));
        builder.Property(x => x.Arrived).HasConversion(x => x.Ticks, x => new(x));
        builder.Property(x => x.Left).HasConversion(x => x.HasValue ? x.Value.Ticks : default(long?),
            x => x.HasValue ? new TimeOnly(x.Value) : null);

        builder.HasIndex(x => x.Date).IsDescending(true);
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Message).WithMany(x => x.Records).HasForeignKey(x => x.Date).HasPrincipalKey(x => x.Date)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
    }

    public bool Equals(DbWorkRecord? other) => other is not null
                                               && (ReferenceEquals(this, other)
                                                   || (UserId == other.UserId && Date.Equals(other.Date)));

    public override bool Equals(object? obj) =>
        obj is not null
        && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((DbWorkRecord)obj)));

    public override int GetHashCode() => HashCode.Combine(UserId, Date);

    public override string ToString() =>
        $"{User?.GetMention() ?? UserId.ToString("D")}: {Arrived.ToString("t", CultureInfo.InvariantCulture)} — {Left?.ToString("t", CultureInfo.InvariantCulture) ?? "--:--"}";
}