
using EFCore_Redis_logger.Utility.ConfigurationHelper;
using EFCore_Redis_logger.Utility.log;

namespace EFCore_Redis_logger.Utility.Cache
{
    public class CacheServiceFactory
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(CacheServiceFactory));
        public static ICacheService Create(string connectionString, int redisexpire, out CacheServerType serverType)
        {         
            ICacheService cacheService;
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception("The cache server configuration is invalid. Could not connect to the Redis server.");
                }
                else
                {
                    //default redis 
                    serverType = CacheServerType.Redis;


                    cacheService = new CSRedisCacheService(connectionString, redisexpire);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"An error occured while connecting to the cahce server. Message:{ex}.");
                throw ex;
            }

            return cacheService;
        }

        public static ICacheService CreateFallback()
        {
            return new Cache.CacheService.MemoryCacheService();
        }
    }
}
