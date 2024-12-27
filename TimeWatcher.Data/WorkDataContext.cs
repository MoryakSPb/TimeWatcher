using Microsoft.EntityFrameworkCore;
using TgBotFrame.Commands.Authorization.Interfaces;
using TgBotFrame.Commands.Authorization.Models;
using TimeWatcher.Data.Models;

namespace TimeWatcher.Data;

public class WorkDataContext(DbContextOptions<WorkDataContext> options) : DbContext(options), IAuthorizationData
{
    public DbSet<DbWorkMessage> WorkMessages { get; init; } = null!;
    public DbSet<DbWorkRecord> WorkRecords { get; init; } = null!;

    Task IAuthorizationData.SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesAsync(cancellationToken);

    public DbSet<DbRole> Roles { get; init; } = null!;
    public DbSet<DbRoleMember> RoleMembers { get; init; } = null!;
    public DbSet<DbBan> Bans { get; init; } = null!;
    public DbSet<DbUser> Users { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        IAuthorizationData.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkDataContext).Assembly);
    }
}