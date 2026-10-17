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

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            return services;
        }
    }
}
