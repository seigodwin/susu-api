using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Susu.Application.Interfaces;
using Susu.Infrastructure.Data;
using Susu.Infrastructure.Repositories;

namespace Susu.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("SusuDatabase") ?? configuration["ConnectionStrings:SusuDatabase"];
            if (string.IsNullOrWhiteSpace(conn))
            {
                // Do not throw here; allow caller to configure later. But log or warning in real app.
            }

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conn));

            // Use EF-backed repository when DbContext is available
            services.AddScoped(typeof(IBaseRepository<>), typeof(EfRepository<>));
            // Unit of Work for transactional operations
            services.AddScoped<Susu.Application.Interfaces.IUnitOfWork, Susu.Infrastructure.UnitOfWork>();
            // Register data seeder and run it immediately to seed demo data if DB empty
            services.AddScoped<Susu.Infrastructure.Data.DataSeeder>();
            // Build a temporary provider to run seeding synchronously during startup
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetService<Susu.Infrastructure.Data.DataSeeder>();
                try
                {
                    seeder?.SeedDemoAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // swallow seeding errors here; in real app log the failure
                }
            }

            return services;
        }
    }
}
