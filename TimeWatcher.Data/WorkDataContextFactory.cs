using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeWatcher.Data;

public class WorkDataContextFactory : IDesignTimeDbContextFactory<WorkDataContext>
{
    public WorkDataContext CreateDbContext(string[] args)
    {
        Directory.CreateDirectory("./data");
        DbContextOptionsBuilder<WorkDataContext> builder = new();
        string connectionString = args.Length != 0
            ? string.Join(' ', args)
            : "Data Source=./data/data.sqlite;";
        Console.WriteLine("connectionString = " + connectionString);
        return new(builder
            .UseSqlite(connectionString)
            .Options);
    }
}