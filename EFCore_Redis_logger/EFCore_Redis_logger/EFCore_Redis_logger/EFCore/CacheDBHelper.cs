using EFCore_Redis_logger.Utility.Cache;
using EFCore_Redis_logger.Utility.Cache.CacheService;
using EFCore_Redis_logger.Utility.log;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore_Redis_logger.EFCore
{
    public class CacheDBHelper
    {
        private static DemoLogger Logger = DemoLogger.GetInstance(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDBRepository _dbRepository;
        private static ICacheService _cacheService = new MemoryCacheService();
        private IServiceScopeFactory _serviceScopeFactory;
        private static readonly double DefaultExpireTime = 1;
        private static readonly string CacheKeyPrefix = "DemoCacheKey_";

        public CacheDBHelper(IDBRepository coreDbRepository,
            IServiceScopeFactory serviceScopeFactory)
        {
            _dbRepository = coreDbRepository;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public T FirstOrDefault<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, string key = null, TimeSpan expiredAfter = default, bool isTracking = false) where T : class
        {
            var items = _dbRepository.FirstOrDefaultPure(expression, navProps, isTracking);
            return items;
        }

        public IQueryable<T> Filter<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, string key = null, TimeSpan expiredAfter = default, bool isTracking = false) where T : class
        {
            var items = _dbRepository.FilterPure(expression, navProps, isTracking);
            return items;
        }

        public IQueryable<T> FilterFromCache<T>(Expression<Func<T, bool>> expression) where T : class
        {
            var cacheKey = CacheKeyPrefix + typeof(T).Name;
            var entities = new List<T>();
            if (_cacheService.KeyExists(cacheKey))
            {
                entities = _cacheService.FetchItem<List<T>>(cacheKey);
            }

            if (entities is not { Count: > 0 })
            {
                entities = Filter<T>(expression).ToList();
                var status = _cacheService.CacheItem(cacheKey, entities, TimeSpan.FromMinutes(DefaultExpireTime));
                Logger.Info($"Added {entities.Count} items into cache key: {cacheKey}, status: {status}");
            }
            return entities.AsQueryable().Where(expression);
        }

        public T FirstOrDefaultFromCache<T>(Expression<Func<T, bool>> expression) where T : class
        {
            var cacheKey = CacheKeyPrefix + typeof(T).Name;
            var entities = new List<T>();
            if (_cacheService.KeyExists(cacheKey))
            {
                entities = _cacheService.FetchItem<List<T>>(cacheKey);
            }

            if (entities is not { Count: > 0 })
            {
                entities = Filter<T>(expression).ToList();
                var status = _cacheService.CacheItem(cacheKey, entities, TimeSpan.FromMinutes(DefaultExpireTime));
                Logger.Info($"Added {entities.Count} items into cache key: {cacheKey}, status: {status}");
            }
            return entities.AsQueryable().FirstOrDefault(expression);
        }

       
    }
}
