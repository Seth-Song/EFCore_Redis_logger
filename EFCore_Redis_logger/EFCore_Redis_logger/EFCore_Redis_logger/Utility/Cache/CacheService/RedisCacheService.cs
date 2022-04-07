using EFCore_Redis_logger.Utility.log;
using EFCore_Redis_logger.Utility.SystemTextJson;
using StackExchange.Redis;

namespace EFCore_Redis_logger.Utility.Cache
{
    public class RedisCacheService : ICacheService
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(RedisCacheService));
        private ConnectionMultiplexer connection;
        private readonly SystemTextJsonSerializer _serializer;
        private string connectionString;

        public RedisCacheService(string connectionString)
        {
            this.connectionString = connectionString;
            _serializer = new SystemTextJsonSerializer();
        }

        #region  Common used
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
            GetServer().FlushDatabase();
        }
        public bool Remove(string key)
        {
            return GetConnection().KeyDelete(key);
        }
        public void RemoveAll(IEnumerable<string> keys)
        {
            keys.ToList().ForEach(k => Remove(k));
        }

        public bool SetExpire(string key, TimeSpan expiresIn)
        {
            return GetConnection().KeyExpire(key, expiresIn);
        }
        public bool SetExpire(string key, DateTimeOffset expiresAt)
        {
            return GetConnection().KeyExpire(key, expiresAt.DateTime);
        }
        public bool KeyExpired(string key)
        {
            return GetConnection().KeyTimeToLive(key) == null;
        }
        public TimeSpan? KeyTimeToLive(string key)
        {
            return GetConnection().KeyTimeToLive(key);
        }

        public bool KeyExists(string key)
        {
            return GetConnection().KeyExists(key);
        }
        public IEnumerable<string> SearchKeys(string pattern)
        {
            List<string> keys = new List<string>();
            var dbkeys = GetServer().Keys(GetConnection().Database, pattern);
            foreach (var key in dbkeys)
            {
                if (!keys.Contains(key.ToString()))
                {
                    keys.Add((string)key.ToString());
                }
            }
            return keys;
        }

        public IEnumerable<string> ScanKeys(string pattern)
        {
            return SearchKeys(pattern);
        }
        #endregion

        #region obj
        public T Get<T>(string key)
        {
            var value = GetString(key);
            if (value != null)
            {
                return _serializer.Deserialize<T>(value);
            }
            throw new KeyNotFoundException(key);
        }

        public bool Set<T>(string key, T value)
        {
            return SetString(key, _serializer.Serialize(value));
        }
        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return SetString(key, _serializer.Serialize(value), expiresIn);
        }
        public bool Set<T>(string key, T value, DateTimeOffset expiresAt)
        {
            return SetString(key, _serializer.Serialize(value), expiresAt);
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
                SetString(key, _serializer.Serialize(value));
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
                SetString(key, _serializer.Serialize(value), expiresIn);
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
                SetString(key, _serializer.Serialize(value), expiresAt);
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
            return GetConnection().StringSet(key, value);
        }
        public bool SetString(string key, string value, TimeSpan expiresIn)
        {
            return GetConnection().StringSet(key, value, expiresIn);
        }
        public bool SetString(string key, string value, DateTimeOffset expiresAt)
        {
            return GetConnection().StringSet(key, value, expiresAt.Subtract(DateTimeOffset.Now));
        }
        public void SetString(Dictionary<string, string> items)
        {
            var values = items
                .Select(i => new KeyValuePair<RedisKey, RedisValue>(i.Key, i.Value))
                .ToArray();
            GetConnection().StringSet(values);
        }
        public decimal StringIncrement(string key, decimal value = 1)
        {
            return (decimal)GetConnection().StringIncrement(key, (double)value);
        }
        public decimal StringDecrement(string key, decimal value = 1)
        {
            return (decimal)GetConnection().StringDecrement(key, (double)value);
        }

        public string GetString(string key)
        {
            var value = GetConnection().StringGet(key);
            if (value.HasValue)
            {
                return (string)value;
            }
            throw new KeyNotFoundException(key);
        }
        public IDictionary<string, string> GetString(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(i => (RedisKey)i).ToArray();
            var result = GetConnection().StringGet(redisKeys);

            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < redisKeys.Length; index++)
            {
                var value = result[index];
                dict.Add((string)redisKeys[index], (string)value);
            }

            return dict;
        }
        #endregion

        #region HashTable
        public bool SetHash(string key, string fieldKey, string value)
        {
            return GetConnection().HashSet(key, fieldKey, value);
        }
        public void SetHash(string key, Dictionary<string, string> values)
        {
            var entries = values.Select(kv => new HashEntry(kv.Key, kv.Value));
            GetConnection().HashSet(key, entries.ToArray());
        }
        public string GetHash(string key, string fieldKey)
        {
            var value = GetConnection().HashGet(key, fieldKey);
            return (string)value;
        }
        public Dictionary<string, string> GetHash(string key, IEnumerable<string> fieldKeys)
        {
            return fieldKeys.Select(x => new { key = x, value = GetHash(key, x) })
                       .ToDictionary(kv => (string)kv.key, kv => (string)kv.value, StringComparer.Ordinal);
        }
        public Dictionary<string, string> GetAllHash(string key)
        {
            return GetConnection().HashGetAll(key).ToDictionary(x => x.Name.ToString(),
                             x => (string)x.Value,
                             StringComparer.Ordinal);
        }
        public bool RemoveHash(string key, string fieldKey)
        {
            return GetConnection().HashDelete(key, fieldKey);
        }
        public long RemoveHash(string key, IEnumerable<string> fieldKeys)
        {
            return GetConnection().HashDelete(key, fieldKeys.Select(x => (RedisValue)x).ToArray());
        }
        public decimal HashIncrement(string key, string fieldKey, decimal value = 1)
        {
            return (decimal)GetConnection().HashIncrement(key, fieldKey, (double)value);
        }
        public decimal HashDecrement(string key, string fieldKey, decimal value = 1)
        {
            return (decimal)GetConnection().HashDecrement(key, fieldKey, (double)value);
        }
        public IEnumerable<string> HashKeys(string key)
        {
            List<string> keys = new List<string>();
            var dbkeys = GetConnection().HashKeys(key);
            foreach (var dbkey in dbkeys)
            {
                if (!keys.Contains(dbkey.ToString()))
                {
                    keys.Add((string)dbkey.ToString());
                }
            }
            return keys;
        }
        #endregion

        #region List
        public long SetList(string key, string value, bool addToLeft = false)
        {
            if (addToLeft)
            {
                return GetConnection().ListLeftPush(key, value);
            }
            else
            {
                return GetConnection().ListRightPush(key, value);
            }
        }
        public long SetList(string key, IEnumerable<string> values, bool addToLeft = false)
        {
            var redisValues = values.Select(i => (RedisValue)i).ToArray();
            if (addToLeft)
            {
                return GetConnection().ListLeftPush(key, redisValues);
            }
            else
            {
                return GetConnection().ListRightPush(key, redisValues);
            }
        }
        public IEnumerable<string> GetListRange(string key, long start, long stop = -1)
        {
            return GetConnection().ListRange(key, start, stop).Select(i => (string)i);
        }
        public string PopList(string key, bool fromLeft = false)
        {
            if (fromLeft)
            {
                return GetConnection().ListLeftPop(key);
            }
            else
            {
                return GetConnection().ListRightPop(key);
            }
        }
        public void RemoveListByRange(string key, long start, long stop)
        {
            GetConnection().ListTrim(key, start, stop);
        }
        #endregion

        #region Sorted Set
        public bool SetSortSet(string key, string member, double score)
        {
            return GetConnection().SortedSetAdd(key, member, score);
        }
        public long SetSortSet(string key, Dictionary<string, double> values)
        {
            var entries = values.Select(kv => new SortedSetEntry(kv.Key, kv.Value)).ToArray();
            return GetConnection().SortedSetAdd(key, entries);
        }
        public decimal? GetSortSetScore(string key, string member)
        {
            return (decimal?)GetConnection().SortedSetScore(key, member);
        }
        public decimal SortedSetIncrement(string key, string member, decimal value)
        {
            return (decimal)GetConnection().SortedSetIncrement(key, member, (double)value);
        }
        public decimal SortedSetDecrement(string key, string member, decimal value)
        {
            return (decimal)GetConnection().SortedSetDecrement(key, member, (double)value);
        }
        public IEnumerable<string> GetSortedSetRange(string key, long start, long stop = -1)
        {
            return GetConnection().SortedSetRangeByRank(key, start, stop).Select(i => (string)i);
        }
        public Dictionary<string, double> GetSortedSetRangeWithScore(string key, long start, long stop = -1)
        {
            return GetConnection().SortedSetRangeByRankWithScores(key, start, stop).ToDictionary(x => x.Element.ToString(),
                             x => (double)x.Score,
                             StringComparer.Ordinal);
        }
        public IEnumerable<string> GetSortedSetRangeByScore(string key, double start, double stop)
        {
            return GetConnection().SortedSetRangeByScore(key, start, stop).Select(i => (string)i);
        }
        public Dictionary<string, double> GetSortedSetRangeByScoreWithScore(string key, double start, double stop)
        {
            return GetConnection().SortedSetRangeByScoreWithScores(key, start, stop).ToDictionary(x => x.Element.ToString(),
                             x => (double)x.Score,
                             StringComparer.Ordinal);
        }
        public long RemoveSortedSetByRange(string key, long start, long stop)
        {
            return GetConnection().SortedSetRemoveRangeByRank(key, start, stop);
        }
        public long RemoveSortedSetByScore(string key, double start, double stop)
        {
            return GetConnection().SortedSetRemoveRangeByScore(key, start, stop);
        }
        #endregion



        private IDatabase GetConnection()
        {
            EnsureConnected();
            return connection.GetDatabase();
        }
        private IServer GetServer()
        {
            EnsureConnected();
            return connection.GetServer(connection.GetEndPoints()[0]);
        }
        private void EnsureConnected()
        {
            if (connection == null || !connection.IsConnected || connection.GetDatabase() == null)
            {
                connection = ConnectionMultiplexer.Connect(connectionString);
            }
        }

        public Task<long> RemoveAsync(string key)
        {
            throw new NotImplementedException();
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

        public long RemoveSet(string setId, List<string> items)
        {
            throw new NotImplementedException();
        }

        public List<string> GetAllSet(string setId)
        {
            throw new NotImplementedException();
        }

        public long AddSetItem(string setId, string value)
        {
            throw new NotImplementedException();
        }

        public long AddSet(string setId, List<string> items)
        {
            throw new NotImplementedException();
        }

        public bool SetContains(string key, string value)
        {
            throw new NotImplementedException();
        }

        public bool SetMove(string sourceKey, string destinationKey, string value)
        {
            throw new NotImplementedException();
        }

        public string[] GetItemFromSetByCount(string key, int count)
        {
            throw new NotImplementedException();
        }

        public bool Lock(string key, TimeSpan expiresIn, Action action)
        {
            throw new NotImplementedException();
        }
    }
}
