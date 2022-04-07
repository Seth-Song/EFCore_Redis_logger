using CSRedis;
using EFCore_Redis_logger.Utility.log;

namespace EFCore_Redis_logger.Utility.Cache
{
    public class CSRedisCacheService : ICacheService
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(CSRedisCacheService));
        private static CSRedisClient rds;
        private TimeSpan AbsoluteExpirationSpan = new TimeSpan(0, 2, 0);
        public CSRedisCacheService(string connectionString, int redisexpire)
        {
            AbsoluteExpirationSpan = TimeSpan.FromSeconds(redisexpire);
            if (rds == null)
            {
                rds = new CSRedisClient(connectionString);
            }
        }

        #region Used Common
        public bool CacheItem(string key, object item, TimeSpan expiredAfter = default(TimeSpan), string region = null)
        {
            ProcessRegion(key, expiredAfter, region);
            return Set(key, item, expiredAfter);
        }

        public bool FetchItem<T>(string key, out T returnValue)
        {
            returnValue = default(T);
            bool fetched = false;
            try
            {
                var result = Get<T>(key);
                if (result != null)
                {
                    returnValue = result;
                    fetched = true;
                }
            }
            catch (Exception e)
            {
                logger.Warn("error occurred when fetch item" + e.ToString());
            }

            return fetched;
        }

        public T FetchItem<T>(string key)
        {
            return Get<T>(key);
        }

        public bool CacheItemExist(string key)
        {
            bool exist = false;
            try
            {
                var result = Get<object>(key);
                if (result != null)
                {
                    exist = true;
                }
            }
            catch (Exception e)
            {
                logger.Warn("error occurred when fetch item" + e.ToString());
            }
            return exist;
        }

        public void RemoveCacheByRegion(string region)
        {
            var set = Get<HashSet<string>>("RG_" + region);
            if (set != null)
            {
                RemoveAll(set.ToList<string>());
                Remove("RG_" + region);
            }
        }

        public void RemoveCache(string key)
        {
            Remove(key);
        }

        public void RemoveWithPattern(string pattern)
        {
            IEnumerable<string> keys = SearchKeys(pattern);
            RemoveAll(keys);
        }

        #region Private
        private void ProcessRegion(string Key, TimeSpan expiredAfter, string region)
        {
            //warn: here we cache the regionId with never expire, this region must be empty manually
            if (!string.IsNullOrEmpty(region))
            {
                var set = Get<HashSet<string>>("RG_" + region);
                if (set == null)
                {
                    set = new HashSet<string>();
                }
                //just ensure we add the key,we don't care whether this key exist or not before
                set.Add(Key);
                CacheItem("RG_" + region, set, expiredAfter);
            }
        }

        #endregion

        #endregion

        #region Common
        public void Clear()
        {
            var keys = rds.Keys("*");
            rds.Del(keys);
        }
        public bool Remove(string key)
        {
            return rds.Del(key) > 0 ? true : false;
        }
        public void RemoveAll(IEnumerable<string> keys)
        {
            if (keys != null)
            {
                rds.Del(keys.ToArray());
            }
        }

        public bool SetExpire(string key, TimeSpan expiresIn)
        {
            return rds.Expire(key, expiresIn);
        }
        public bool SetExpire(string key, DateTimeOffset expiresAt)
        {
            return rds.ExpireAt(key, expiresAt.DateTime);
        }
        public bool KeyExpired(string key)
        {
            return rds.PTtl(key) > 0;
        }
        public TimeSpan? KeyTimeToLive(string key)
        {
            if (rds.PTtl(key) > 0)
            {
                return TimeSpan.FromMilliseconds(rds.PTtl(key));
            }
            else
            {
                return null;
            }
        }

        public bool KeyExists(string key)
        {
            return rds.Exists(key);
        }
        public IEnumerable<string> SearchKeys(string pattern)
        {
            return rds.Keys(pattern).ToList();
        }
        public IEnumerable<string> ScanKeys(string pattern)
        {
            long cursor = 0;
            var results = new List<string>();
            do
            {
                var scanner = rds.Scan(cursor, pattern, 100);
                results.AddRange(scanner.Items);
                cursor = scanner.Cursor;
            }
            while (cursor != 0);
            return results;
        }
        #endregion


        #region obj
        public async Task<long> RemoveAsync(string key)
        {
            return await rds.DelAsync(key);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            return await rds.ExistsAsync(key);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return await rds.GetAsync<T>(key);
        }
        public T Get<T>(string key)
        {
            return rds.Get<T>(key);
        }
        public bool Set<T>(string key, T value)
        {
            return rds.Set(key, value);
        }
        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return rds.Set(key, value, (expiresIn == default(TimeSpan) ? AbsoluteExpirationSpan : expiresIn));
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await rds.SetAsync(key, value, (expiresIn == default(TimeSpan) ? AbsoluteExpirationSpan : expiresIn));
        }

        public bool Set<T>(string key, T value, DateTimeOffset expiresAt)
        {
            return rds.Set(key, value, expiresAt.Subtract(DateTimeOffset.Now));
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc) where T : class
        {
            try
            {
                if (KeyExists(key))
                {
                    return Get<T>(key);
                }

                T value = AddCacheFunc();
                rds.Set(key, value);
                return value;
            }
            catch (Exception ex)
            {
                logger.Warn("An error occurred in GetWithAdd.", ex);
                return AddCacheFunc();
            }
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, TimeSpan expiresIn) where T : class
        {
            try
            {
                if (KeyExists(key))
                {
                    return Get<T>(key);
                }

                T value = AddCacheFunc();
                rds.Set(key, value, expiresIn);
                return value;
            }
            catch (Exception ex)
            {
                logger.Warn("An error occurred in GetWithAdd.", ex);
                return AddCacheFunc();
            }
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, DateTimeOffset expiresAt) where T : class
        {
            try
            {
                if (KeyExists(key))
                {
                    return Get<T>(key);
                }

                T value = AddCacheFunc();
                rds.Set(key, value, expiresAt.Subtract(DateTimeOffset.Now));
                return value;
            }
            catch (Exception ex)
            {
                logger.Warn("An error occurred in GetWithAdd.", ex);
                return AddCacheFunc();
            }
        }
        #endregion

        #region String
        public bool SetString(string key, string value)
        {
            return rds.Set(key, value);
        }
        public bool SetString(string key, string value, TimeSpan expiresIn)
        {
            return rds.Set(key, value, expiresIn);
        }
        public bool SetString(string key, string value, DateTimeOffset expiresAt)
        {
            return rds.Set(key, value, expiresAt.Subtract(DateTimeOffset.Now));
        }
        public void SetString(Dictionary<string, string> items)
        {
            foreach (var tuple in items)
            {
                rds.Set(tuple.Key, tuple.Value);
            }
        }
        public decimal StringIncrement(string key, decimal value = 1)
        {
            return rds.IncrByFloat(key, value);
        }
        public decimal StringDecrement(string key, decimal value = 1)
        {
            return rds.IncrByFloat(key, 0 - value);
        }

        public string GetString(string key)
        {
            return rds.Get(key);
        }


        public IDictionary<string, string> GetString(IEnumerable<string> keys)
        {
            return keys.ToDictionary(key => key, key => rds.Get(key));
        }
        #endregion

        #region Set
        public long RemoveSet(string setId, List<string> items)
        {
            return rds.SRem(setId, items);
        }

        public List<string> GetAllSet(string setId)
        {
            return rds.SMembers(setId)?.ToList();
        }

        public long AddSetItem(string setId, string value)
        {
            return rds.SAdd(setId, value);
        }

        public long AddSet(string setId, List<string> items)
        {
            return rds.SAdd<string>(setId, items.ToArray());
        }

        public bool SetContains(string key, string value)
        {
            return rds.SIsMember(key, value);
        }

        public bool SetMove(string sourceKey, string destinationKey, string value)
        {
            return rds.SMove(sourceKey, destinationKey, value);
        }

        public string[] GetItemFromSetByCount(string key, int count)
        {
            return rds.SRandMembers(key, count);
        }
        #endregion


        #region HashTable
        public bool SetHash(string key, string fieldKey, string value)
        {
            return rds.HSet(key, fieldKey, value);
        }
        public void SetHash(string key, Dictionary<string, string> values)
        {
            if (values != null)
            {
                values.Keys.ToList().ForEach(field =>
                {
                    SetHash(key, field, values[field]);
                });
            }
        }
        public string GetHash(string key, string fieldKey)
        {
            return rds.HGet(key, fieldKey);
        }
        public Dictionary<string, string> GetHash(string key, IEnumerable<string> fieldKeys)
        {
            return fieldKeys.ToDictionary(fieldKey => fieldKey, fieldKey => rds.HGet(key, fieldKey));
        }
        public Dictionary<string, string> GetAllHash(string key)
        {
            return rds.HGetAll(key);
        }
        public bool RemoveHash(string key, string fieldKey)
        {
            var keys = new string[1] { fieldKey };
            return rds.HDel(key, keys) > 0 ? true : false;
        }
        public long RemoveHash(string key, IEnumerable<string> fieldKeys)
        {
            return rds.HDel(key, fieldKeys.ToArray());
        }
        public decimal HashIncrement(string key, string fieldKey, decimal value = 1)
        {
            return rds.HIncrByFloat(key, fieldKey, value);
        }
        public decimal HashDecrement(string key, string fieldKey, decimal value = 1)
        {
            return rds.HIncrByFloat(key, fieldKey, 0 - value);
        }
        public IEnumerable<string> HashKeys(string key)
        {
            return rds.HKeys(key).ToList();
        }
        #endregion


        #region List
        public long SetList(string key, string value, bool addToLeft = false)
        {
            if (addToLeft)
            {
                return rds.LPush(key, value);
            }
            else
            {
                return rds.RPush(key, value);
            }
        }
        public long SetList(string key, IEnumerable<string> values, bool addToLeft = false)
        {
            if (addToLeft)
            {
                return rds.LPush(key, values);
            }
            else
            {
                return rds.RPush(key, values);
            }
        }
        public IEnumerable<string> GetListRange(string key, long start, long stop = -1)
        {
            return rds.LRange(key, start, stop);
        }
        public string PopList(string key, bool fromLeft = false)
        {
            if (fromLeft)
            {
                return rds.LPop(key);
            }
            else
            {
                return rds.RPop(key);
            }
        }
        public void RemoveListByRange(string key, long start, long stop)
        {
            rds.LTrim(key, start, stop);
        }
        #endregion


        #region Sorted Set
        public bool SetSortSet(string key, string member, double score)
        {
            return rds.ZAdd(key, ((decimal)score, member)) > 0;
        }
        public long SetSortSet(string key, Dictionary<string, double> values)
        {
            long result = 0;
            if (values != null)
            {
                values.Keys.ToList().ForEach(field =>
                {
                    result = rds.ZAdd(key, ((decimal)values[field], field));
                });
            }
            return result;
        }
        public decimal? GetSortSetScore(string key, string member)
        {
            return rds.ZScore(key, member);
        }
        public decimal SortedSetIncrement(string key, string member, decimal value)
        {
            return rds.ZIncrBy(key, member, value);
        }
        public decimal SortedSetDecrement(string key, string member, decimal value)
        {
            return rds.ZIncrBy(key, member, 0 - value);
        }
        public IEnumerable<string> GetSortedSetRange(string key, long start, long stop = -1)
        {
            return rds.ZRange(key, start, stop);
        }
        public Dictionary<string, double> GetSortedSetRangeWithScore(string key, long start, long stop = -1)
        {
            return rds.ZRangeWithScores(key, start, stop).ToDictionary(key => key.member, key => (double)key.score);
        }
        public IEnumerable<string> GetSortedSetRangeByScore(string key, double start, double stop)
        {
            return rds.ZRangeByScore(key, (decimal)start, (decimal)stop);
        }
        public Dictionary<string, double> GetSortedSetRangeByScoreWithScore(string key, double start, double stop)
        {
            return rds.ZRangeByScoreWithScores(key, (decimal)start, (decimal)stop).ToDictionary(key => key.member, key => (double)key.score);
        }
        public long RemoveSortedSetByRange(string key, long start, long stop)
        {
            return rds.ZRemRangeByRank(key, start, stop);
        }
        public long RemoveSortedSetByScore(string key, double start, double stop)
        {
            return rds.ZRemRangeByScore(key, (decimal)start, (decimal)stop);
        }

        #endregion


        #region Blob
        public bool SetBlob(string key, byte[] value)
        {
            return rds.Set(key, value);
        }
        public bool SetBlob(string key, byte[] value, TimeSpan expiresIn)
        {
            return rds.Set(key, value, expiresIn);
        }
        public bool SetBlob(string key, byte[] value, DateTimeOffset expiresAt)
        {
            return rds.Set(key, value, expiresAt.Subtract(DateTimeOffset.Now));
        }
        public byte[] GetBlob(string key)
        {
            return rds.Dump(key);
        }

        public T GetBlob<T>(string key)
        {
            return rds.Get<T>(key);
        }
        #endregion

        #region Cuckoo Filter
        public bool CuckooSet<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public Dictionary<bool, int> CuckooSet<T>(string key, IEnumerable<T> values)
        {
            throw new NotImplementedException();
        }

        public bool CuckooExists<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public bool CuckooDelete<T>(string key, T value)
        {
            throw new NotImplementedException();
        }


        #endregion
        public bool Lock(string key, TimeSpan expiresIn, Action action)
        {
            if (action == null || string.IsNullOrWhiteSpace(key) || expiresIn == TimeSpan.Zero)
            {
                return false;
            }
            int exp = (int)expiresIn.TotalSeconds;
            var locker = rds.Lock(key, exp, false);
            if (locker == null)
            {
                return false;
            }
            try
            {
                action();
            }
            finally
            {
                locker.Unlock();
            }
            return true;
        }
    }
}
