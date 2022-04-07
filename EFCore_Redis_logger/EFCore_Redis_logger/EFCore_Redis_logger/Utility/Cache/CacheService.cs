using EFCore_Redis_logger.Utility.Cache;
using EFCore_Redis_logger.Utility.ConfigurationHelper;
using EFCore_Redis_logger.Utility.log;


namespace EFCore_Redis_logger.Utility.Cache1
{
    public class CacheService : ICacheService
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(CacheService));
        private IConfiguration _configuration;
        private ICacheService internalService;
        private CacheConfigInfo cacheInfo;
        private CacheServerType cacheServerType;
        private RuningMode mode = RuningMode.Normal;
        private int errorCount = 0;

        private DateTime lastChangeTime = DateTime.UtcNow;
        public CacheService(IConfiguration configuration)
        {
            try
            {
                _configuration = configuration;
                var configrationHelper = new ConfigurationHelper1(_configuration);
                cacheInfo = configrationHelper.Resolve<CacheConfigInfo>(ConfigurationKeys.CacheServer);
                var connectionString = configrationHelper.RedisConnection();
                var cacheexpire = configrationHelper.RedisExpire();
                internalService = CacheServiceFactory.Create(connectionString, cacheexpire, out cacheServerType);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to init cache server information. Missing cache server configuration.");
                throw ex;
            }
        }

        #region used common
        public bool CacheItem(string key, object item, TimeSpan expiredAfter = default(TimeSpan), string region = null)
        {
            return Invoke(() => internalService.CacheItem(key, item, expiredAfter, region));
        }

        public bool FetchItem<T>(string key, out T returnValue)
        {
            returnValue = default(T);
            return Invoke(() => internalService.FetchItem<T>(key, out T returnValue));
        }

        public T FetchItem<T>(string key)
        {
            return Invoke(() => internalService.FetchItem<T>(key));
        }

        public bool CacheItemExist(string key)
        {
            return Invoke(() => internalService.CacheItemExist(key));
        }

        public void RemoveCacheByRegion(string region)
        {
            Invoke(() => internalService.RemoveCacheByRegion(region));
        }

        public void RemoveCache(string key)
        {
            Invoke(() => internalService.Remove(key));
        }

        public void RemoveWithPattern(string pattern)
        {
            Invoke(() => internalService.RemoveWithPattern(pattern));
        }

        #endregion

        #region Common
        public void Clear()
        {
            logger.Info($"Clearing cache storage items. Server:{cacheServerType}.");
            Invoke(() => internalService.Clear());
        }
        public bool Remove(string key)
        {
            //logger.Debug($"Removing cache item. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.Remove(key));
        }
        public void RemoveAll(IEnumerable<string> keys)
        {
            //logger.Debug($"Removing cache items from storage. Key count:{keys.Count()}, server:{cacheServerType}.");
            Invoke(() => internalService.RemoveAll(keys));
        }

        public bool SetExpire(string key, TimeSpan expiresIn)
        {
            //logger.Debug($"Setting cache expiration time. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.SetExpire(key, expiresIn));
        }
        public bool SetExpire(string key, DateTimeOffset expiresAt)
        {
            //logger.Debug($"Setting cache expiration time. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.SetExpire(key, expiresAt));
        }
        public bool KeyExpired(string key)
        {
            //logger.Debug($"Checking cache expiration status. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.KeyExpired(key));
        }
        public TimeSpan? KeyTimeToLive(string key)
        {
            //logger.Debug($"Retrieving cache expiration time. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.KeyTimeToLive(key));
        }

        public bool KeyExists(string key)
        {
            //logger.Debug($"Checking cache exist status. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.KeyExists(key));
        }
        public IEnumerable<string> SearchKeys(string pattern)
        {
            //logger.Debug($"Search cache keys from cache server. Pattern:{pattern}, server:{cacheServerType}.");
            return Invoke(() => internalService.SearchKeys(pattern));
        }

        public IEnumerable<string> ScanKeys(string pattern)
        {
            //logger.Debug($"scan cache keys from cache server. Key pattern:{pattern}, server:{cacheServerType}.");
            var results = Invoke(() => internalService.ScanKeys(pattern));
            return results.Distinct();
        }
        #endregion

        #region obj
        public T Get<T>(string key)
        {
            //logger.Debug($"Retrieving cache information for type {typeof(T).FullName}. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.Get<T>(key));
        }
        public bool Set<T>(string key, T value)
        {
            //logger.Debug($"Adding or updating cache information for type {typeof(T).FullName}. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.Set(key, value, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime)));
        }
        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            //logger.Debug($"Adding or updating cache information for type {typeof(T).FullName}. Key:{key}, expire time:{expiresIn}, server:{cacheServerType}.");
            return Invoke(() => internalService.Set(key, value, expiresIn));
        }
        public bool Set<T>(string key, T value, DateTimeOffset expiresAt)
        {
            //logger.Debug($"Adding or updating cache information for type {typeof(T).FullName}. Key:{key}, expire at:{expiresAt}, server:{cacheServerType}.");
            return Invoke(() => internalService.Set(key, value, expiresAt));
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc) where T : class
        {
            //logger.Debug($"Retrieving or adding cache information for type {typeof(T).FullName}. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetWithAdd(key, AddCacheFunc, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime)));
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, TimeSpan expiresIn) where T : class
        {
            //logger.Debug($"Retrieving or adding cache information for type {typeof(T).FullName}. Key:{key}, expire time:{expiresIn}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetWithAdd(key, AddCacheFunc, expiresIn));
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, DateTimeOffset expiresAt) where T : class
        {
            //logger.Debug($"Retrieving or adding cache information for type {typeof(T).FullName}. Key:{key}, expire at:{expiresAt}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetWithAdd(key, AddCacheFunc, expiresAt));
        }

        #endregion

        #region String
        public bool SetString(string key, string value)
        {
            //logger.Debug($"Adding or updating cache information. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.SetString(key, value, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime)));
        }
        public bool SetString(string key, string value, TimeSpan expiresIn)
        {
            //logger.Debug($"Adding or updating cache information. Key:{key}, expire time:{expiresIn}, server:{cacheServerType}.");
            return Invoke(() => internalService.SetString(key, value));
        }
        public bool SetString(string key, string value, DateTimeOffset expiresAt)
        {
            //logger.Debug($"Adding or updating cache information. Key:{key}, expire at:{expiresAt}, server:{cacheServerType}.");
            return Invoke(() => internalService.SetString(key, value));
        }
        public void SetString(Dictionary<string, string> items)
        {
            //logger.Debug($"Adding or updating cache items information. Item Count:{items.Count}, , server:{cacheServerType}.");
            Invoke(() =>
            {
                foreach (var tuple in items)
                {
                    internalService.SetString(tuple.Key, tuple.Value, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
            });
        }
        public decimal StringIncrement(string key, decimal value = 1)
        {
            //logger.Debug($"Increase cache value. Key:{key}, increment value:{value}, server:{cacheServerType}.");
            return Invoke(() => internalService.StringIncrement(key, value));
        }
        public decimal StringDecrement(string key, decimal value = 1)
        {
            //logger.Debug($"Decrease cache value. Key:{key}, increment value:{value}, server:{cacheServerType}.");
            return Invoke(() => internalService.StringIncrement(key, value));
        }

        public string GetString(string key)
        {
            //logger.Debug($"Retrieving cache information. Key:{key}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                if (internalService.KeyExists(key))
                {
                    return internalService.GetString(key);
                }
                return null;
            });
        }
        public IDictionary<string, string> GetString(IEnumerable<string> keys)
        {
            //logger.Debug($"Retrieving cache items information. Key count:{keys.Count()}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetString(keys));
        }

        #endregion

        #region HashTable
        public bool SetHash(string key, string fieldKey, string value)
        {
            //logger.Debug($"Adding or updating hash table to cache storage. Key:{key}, item key:{fieldKey}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                bool result = internalService.SetHash(key, fieldKey, value);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return result;
            });
        }
        public void SetHash(string key, Dictionary<string, string> values)
        {
            //logger.Debug($"Adding or updating hash table to cache storage. Key:{key}, value count:{values.Count}, server:{cacheServerType}.");
            Invoke(() =>
            {
                internalService.SetHash(key, values);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
            });
        }
        public string GetHash(string key, string fieldKey)
        {
            //logger.Debug($"Retrieving hash item from hash table. Key:{key}, item key:{fieldKey}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetHash(key, fieldKey));
        }
        public Dictionary<string, string> GetHash(string key, IEnumerable<string> fieldKeys)
        {
            //logger.Debug($"Retrieving hash item from hash table. Key:{key}, item key count:{fieldKeys.Count()}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetHash(key, fieldKeys));
        }
        public Dictionary<string, string> GetAllHash(string key)
        {
            //logger.Debug($"Retrieving hash table values. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetAllHash(key));
        }
        public bool RemoveHash(string key, string fieldKey)
        {
            //logger.Debug($"Deleting item from hash table. Key:{key},item key:{fieldKey}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                bool result = internalService.RemoveHash(key, fieldKey);
                if (internalService.KeyExists(key))
                {
                    internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
                return result;
            });
        }
        public long RemoveHash(string key, IEnumerable<string> fieldKeys)
        {
            //logger.Debug($"Deleting hash items from hash table. Key:{key}, item key count:{fieldKeys.Count()}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.RemoveHash(key, fieldKeys);
                if (internalService.KeyExists(key))
                {
                    internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
                return result;
            });
        }
        public decimal HashIncrement(string key, string fieldKey, decimal value = 1)
        {
            //logger.Debug($"Increase hash item from cache table. Key:{key}, increment value:{value}, server:{cacheServerType}.");
            return Invoke(() => internalService.HashIncrement(key, fieldKey, value));
        }
        public decimal HashDecrement(string key, string fieldKey, decimal value = 1)
        {
            //logger.Debug($"Decrease hash item from cache table. Key:{key}, increment value:{value}, server:{cacheServerType}.");
            return Invoke(() => internalService.HashDecrement(key, fieldKey, value));
        }
        public IEnumerable<string> HashKeys(string key)
        {
            //logger.Debug($"Retrieving all property keys of hash item from cache table. Key:{key}, server:{cacheServerType}.");
            return Invoke(() => internalService.HashKeys(key));
        }

        #endregion

        #region List
        public long SetList(string key, string value, bool addToLeft = false)
        {
            //logger.Debug($"Adding or updating list item to cache storage. Key:{key}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.SetList(key, value, addToLeft);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return result;
            });
        }
        public long SetList(string key, IEnumerable<string> values, bool addToLeft = false)
        {
            //logger.Debug($"Adding or updating list item to cache storage. Key:{key}, value count:{values.Count()}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.SetList(key, values, addToLeft);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return result;
            });
        }
        public IEnumerable<string> GetListRange(string key, long start, long stop = -1)
        {
            //logger.Debug($"Retrieving list item from cache table. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetListRange(key, start, stop));
        }
        public string PopList(string key, bool fromLeft = false)
        {
            //logger.Debug($"Popout one item of list item from cache table. Key:{key}, fromLeft: {fromLeft}, server:{cacheServerType}.");
            return Invoke(() => internalService.PopList(key, fromLeft));
        }
        public void RemoveListByRange(string key, long start, long stop)
        {
            //logger.Debug($"Removing items of list item from cache table. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            Invoke(() =>
            {
                internalService.RemoveListByRange(key, start, stop);
                if (internalService.KeyExists(key))
                {
                    internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
            });
        }
        #endregion

        #region Sorted Set
        public bool SetSortSet(string key, string member, double score)
        {
            //logger.Debug($"Adding or updating sorted item to cache storage. Key:{key}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                bool result = internalService.SetSortSet(key, member, score);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return result;
            });
        }
        public long SetSortSet(string key, Dictionary<string, double> values)
        {
            //logger.Debug($"Adding or updating sorted item to cache storage. Key:{key}, value count: {values.Count}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.SetSortSet(key, values);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return result;
            });
        }
        public decimal? GetSortSetScore(string key, string member)
        {
            //logger.Debug($"Retrieving sorted set score from cache table. Key:{key}, member: {member}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetSortSetScore(key, member));
        }
        public decimal SortedSetIncrement(string key, string member, decimal value)
        {
            //logger.Debug($"Increase sorted set item from cache table. Key:{key}, member: {member}, server:{cacheServerType}.");
            return Invoke(() => internalService.SortedSetIncrement(key, member, value));
        }
        public decimal SortedSetDecrement(string key, string member, decimal value)
        {
            //logger.Debug($"Decrease sorted set item from cache table. Key:{key}, member: {member}, server:{cacheServerType}.");
            return Invoke(() => internalService.SortedSetDecrement(key, member, value));
        }
        public IEnumerable<string> GetSortedSetRange(string key, long start, long stop = -1)
        {
            //logger.Debug($"Rertieving sorted item to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetSortedSetRange(key, start, stop));
        }
        public Dictionary<string, double> GetSortedSetRangeWithScore(string key, long start, long stop = -1)
        {
            //logger.Debug($"Rertieving sorted item with score to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetSortedSetRangeWithScore(key, start, stop));
        }
        public IEnumerable<string> GetSortedSetRangeByScore(string key, double start, double stop)
        {
            //logger.Debug($"Rertieving sorted item by score to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetSortedSetRangeByScore(key, start, stop));
        }
        public Dictionary<string, double> GetSortedSetRangeByScoreWithScore(string key, double start, double stop)
        {
            //logger.Debug($"Rertieving sorted item by score with score to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() => internalService.GetSortedSetRangeByScoreWithScore(key, start, stop));
        }
        public long RemoveSortedSetByRange(string key, long start, long stop)
        {
            //logger.Debug($"Removing sorted item to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.RemoveSortedSetByRange(key, start, stop);
                if (internalService.KeyExists(key))
                {
                    internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
                return result;
            });
        }
        public long RemoveSortedSetByScore(string key, double start, double stop)
        {
            //logger.Debug($"Removing sorted item by score to cache storage. Key:{key}, start: {start}, stop: {stop}, server:{cacheServerType}.");
            return Invoke(() =>
            {
                long result = internalService.RemoveSortedSetByScore(key, start, stop);
                if (internalService.KeyExists(key))
                {
                    internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
                return result;
            });
        }
        #endregion

      

        #region Helper Functions
        private T Invoke<T>(Func<T> func, int retryTimes = 0)
        {
            T result = default(T);
            ProcessBack();
            var isCompleted = false;
            while (!isCompleted && retryTimes < cacheInfo.RetryCount)
            {
                try
                {
                    result = func();
                    isCompleted = true;
                }
        
                catch (Exception ex)
                {
                    logger.Error($"Failed to invoke cache function for specified type. Current retry number:{retryTimes++}.", ex);
                }
            }
            if (!isCompleted)
            {
                ProcessError();
            }
            return result;
        }

        private void Invoke(Action func, int retryTimes = 0)
        {
            ProcessBack();
            var isCompleted = false;
            while (!isCompleted && retryTimes < cacheInfo.RetryCount)
            {
                try
                {
                    func();
                    isCompleted = true;
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to invoke cache function.Current retry number:{retryTimes++}.", ex);
                }
            }
            if (!isCompleted)
            {
                ProcessError();
            }
        }

        private void ProcessBack()
        {
            if (mode == RuningMode.FallBack && ((DateTime.UtcNow - lastChangeTime).TotalMinutes >= cacheInfo.ReconnectInterval))
            {
                try
                {
                    logger.Info($"Begin to re-connect to cache server. Server:{cacheServerType}.");
                    var configrationHelper = new ConfigurationHelper1(_configuration);
                    var connectionString = configrationHelper.RedisConnection();
                    var redisexpire = configrationHelper.RedisExpire();
                    internalService = CacheServiceFactory.Create(connectionString, redisexpire, out cacheServerType);
                    mode = RuningMode.Normal;
                    lastChangeTime = DateTime.UtcNow;
                    errorCount = 0;
                }
                catch (Exception ex)
                {
                    logger.Warn($"An error occured while re-connecting to cache server. Server:{cacheServerType}, message:{ex}.");
                }
                finally
                {
                    logger.Info($"Re-connect to cache server completed. Server:{cacheServerType}.");
                }
            }
        }

        private void ProcessError()
        {
            errorCount++;
            logger.Error($"Processing error operations. Current error count:{errorCount}.");
            if (mode == RuningMode.Normal && errorCount >= 5)
            {
                //internalService = CacheServiceFactory.CreateFallback();
                //mode = RuningMode.FallBack;
                lastChangeTime = DateTime.UtcNow;
                errorCount = 0;
                logger.Info($"Failed to connect to cache server after several retries.");
            }
        }


        #endregion

        #region Set
        public long RemoveSet(string setId, List<string> items)
        {
            return Invoke(() =>
            {
                long result = internalService.RemoveSet(setId, items);
                if (internalService.KeyExists(setId))
                {
                    internalService.SetExpire(setId, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                }
                return result;
            });
        }

        public List<string> GetAllSet(string setId)
        {
            return Invoke(() => internalService.GetAllSet(setId));
        }

        public long AddSetItem(string setId, string value)
        {
            return Invoke(() =>
            {
                long res = internalService.AddSetItem(setId, value);
                internalService.SetExpire(setId, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return res;
            });
        }

        public long AddSet(string setId, List<string> items)
        {
            return Invoke(() =>
            {
                long res = internalService.AddSet(setId, items);
                internalService.SetExpire(setId, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return res;
            });
        }

        public bool SetContains(string key, string value)
        {
            return Invoke(() =>
            {
                bool res = internalService.SetContains(key, value);
                internalService.SetExpire(key, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return res;
            });
        }

        public bool SetMove(string sourceKey, string destinationKey, string value)
        {
            return Invoke(() =>
            {
                bool res = internalService.SetMove(sourceKey, destinationKey, value);
                internalService.SetExpire(destinationKey, TimeSpan.FromSeconds(cacheInfo.DefaultExpirationTime));
                return res;
            });
        }

        public string[] GetItemFromSetByCount(string key, int count)
        {
            return Invoke(() => internalService.GetItemFromSetByCount(key, count));
        }

        public async Task<long> RemoveAsync(string key)
        {
            return await Invoke(() => internalService.RemoveAsync(key));
        }

        public Task<bool> KeyExistsAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            throw new NotImplementedException();
        }
        #endregion
        public bool Lock(string key, TimeSpan expiresIn, Action action)
        {
            return internalService.Lock(key, expiresIn, action);
        }
    }
}
