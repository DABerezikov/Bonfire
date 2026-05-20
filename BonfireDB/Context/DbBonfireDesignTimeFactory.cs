using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BonfireDB.Context;

/// <summary>
/// Фабрика для EF-инструментов (migrations, database update).
/// Позволяет dotnet-ef создавать DbBonfire без запуска хоста приложения.
/// </summary>
internal class DbBonfireDesignTimeFactory : IDesignTimeDbContextFactory<DbBonfire>
{
    public DbBonfire CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DbBonfire>()
            .UseSqlite("Data Source=BonfireDB.db")
            .Options;
        return new DbBonfire(options);
    }
}
