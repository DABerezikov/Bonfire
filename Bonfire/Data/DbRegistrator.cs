using System;
using BonfireDB;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bonfire.Data
{
    internal static class DbRegistrator
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration) => services
            .AddDbContext<DbBonfire>(opt =>
            {
                var type = configuration["Type"];
                switch (type)
                {
                    case null: throw new InvalidOperationException("Не определён тип БД");

                    default: throw new InvalidOperationException($"Тип подключения {type} не поддерживается");

                    case "SQLite":
                        opt.UseSqlite(configuration.GetConnectionString(type));
                        break;
                    

                }
            })
            .AddSingleton<DbInitializer>()
            .AddRepositoriesInDb()
        ;
    }
}
