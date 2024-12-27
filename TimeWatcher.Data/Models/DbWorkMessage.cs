using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TimeWatcher.Data.Models;

public class DbWorkMessage : IEntityTypeConfiguration<DbWorkMessage>, IEquatable<DbWorkMessage>
{
    public int Id { get; init; }
    public required DateOnly Date { get; init; }
    public IList<DbWorkRecord> Records { get; init; } = [];

    public void Configure(EntityTypeBuilder<DbWorkMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => x.Date);

        builder.Property(x => x.Date).HasConversion(x => x.DayNumber, x => DateOnly.FromDayNumber(x));

        builder.HasMany(x => x.Records).WithOne(x => x.Message).HasForeignKey(x => x.Date).HasPrincipalKey(x => x.Date)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
    }

    public bool Equals(DbWorkMessage? other) => other is not null && (ReferenceEquals(this, other) || Id == other.Id);

    public override bool Equals(object? obj) =>
        obj is not null
        && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((DbWorkMessage)obj)));

    public override int GetHashCode() => Id;

    public override string ToString() => Id.ToString("D", CultureInfo.InvariantCulture);
}