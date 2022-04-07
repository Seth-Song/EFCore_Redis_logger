using EFCore_Redis_logger.EFCore;
using EFCore_Redis_logger.Utility.Cache;
using EFCore_Redis_logger.Utility.Cache1;
using EFCore_Redis_logger.Utility.ConfigurationHelper;
using EFCore_Redis_logger.Utility.log;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

namespace EFCore_Redis_logger.Extension
{
    public static class ServiceCollectionExtensions
    {
        private static DemoLogger logger = DemoLogger.GetInstance(typeof(ServiceCollectionExtensions));


        public static IServiceCollection AddCacheService(this IServiceCollection services, ConfigurationHelper1 configurationHelper)
        {
            services.AddSingleton<ICacheService, CacheService>();
            return services;
        }

        public static IServiceCollection AddDBContext(this IServiceCollection services, ConfigurationHelper1 configurationHelper)
        {         
            string DBConn = configurationHelper.DBConnection();
            services.AddDbContextPool<DBContext>(options =>
            {
                options.UseSqlServer(DBConn).UseLoggerFactory(DemoLogger.DemoLoggerFactory);
            });

            services.AddScoped<IDBRepository, DBRepository>();
            services.AddScoped<DBContextFactory>();
            services.AddTransient<CacheDBHelper>();

            return services;
        }

        public static void AddLogger(this WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            builder.Host.UseNLog();
     
        }
    }
}
