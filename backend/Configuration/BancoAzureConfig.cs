using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Configuration
{
    public class AzureSqlSettings
    {
        public string ConnectionString { get; set; } = null!;
    }

    public static class BancoAzureConfig
    {
        public static IServiceCollection AddBancoAzure(this IServiceCollection services, IConfiguration config)
        {
            // Vincula as configurações do appsettings.json
            services.Configure<AzureSqlSettings>(config.GetSection("AzureSqlSettings"));

            // Registra o DbContext usando EF Core e SQL Server, com retry on failure
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var settings = sp.GetRequiredService<IOptions<AzureSqlSettings>>().Value;
                options.UseSqlServer(
                    settings.ConnectionString,
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    )
                );
            });

            return services;
        }
    }
}
