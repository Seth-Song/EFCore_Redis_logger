using EFCore_Redis_logger.Utility.log;
using System.Collections;
using System.Text.RegularExpressions;

namespace EFCore_Redis_logger.Utility.Cache.CacheService
{
    public class MemoryCacheService : ICacheService
    {
        private CacheRepository cache = new CacheRepository();
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(RedisCacheService));

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
            cache.Clear();
        }
        public bool Remove(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null)
            {
                cache.Remove(targetKey);
                return true;
            }
            return false;
        }
        public void RemoveAll(IEnumerable<string> keys)
        {
            keys.ToList().ForEach(k => Remove(k));
        }

        public bool SetExpire(string key, TimeSpan expiresIn)
        {
            var cacheKey = cache.Keys.SingleOrDefault(k => k.Key == key);
            if (cacheKey != null)
            {
                object cacheValue = cache[cacheKey];
                cache.Remove(cacheKey);

                cacheKey.ExpiredOn = DateTimeOffset.Now + expiresIn;
                cache.Add(cacheKey, cacheValue);
                return true;
            }
            return false;
        }
        public bool SetExpire(string key, DateTimeOffset expiresAt)
        {
            var cacheKey = cache.Keys.SingleOrDefault(k => k.Key == key);
            if (cacheKey != null)
            {
                object cacheValue = cache[cacheKey];
                cache.Remove(cacheKey);

                cacheKey.ExpiredOn = expiresAt;
                cache.Add(cacheKey, cacheValue);
                return true;
            }
            return false;
        }
        public bool KeyExpired(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null && targetKey.ExpiredOn < DateTimeOffset.Now)
            {
                cache.Remove(targetKey);
                return true;
            }
            return false;
        }
        public TimeSpan? KeyTimeToLive(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null && targetKey.ExpiredOn != DateTimeOffset.MaxValue)
            {
                if (targetKey.ExpiredOn < DateTimeOffset.Now)
                {
                    return targetKey.ExpiredOn - DateTimeOffset.Now;
                }
                else
                {
                    cache.Remove(targetKey);
                }
            }
            return null;
        }

        public bool KeyExists(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null && targetKey.ExpiredOn < DateTimeOffset.Now)
            {
                cache.Remove(targetKey);
                targetKey = null;
            }
            return targetKey != null;
        }
        public IEnumerable<string> SearchKeys(string pattern)
        {
            var result = new List<string>();
            var regexPattern = $"^{ pattern.Replace("*", ".*").Replace("?", ".")}$";


            foreach (var cacheKey in cache.Keys)
            {
                if (Regex.IsMatch(cacheKey.Key, regexPattern))
                {
                    result.Add(cacheKey.Key);
                }
            }
            return result;
        }
        public IEnumerable<string> ScanKeys(string pattern)
        {
            return SearchKeys(pattern);
        }

        #endregion

        #region obj
        public T Get<T>(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null && targetKey.ExpiredOn < DateTimeOffset.Now)
            {
                cache.Remove(targetKey);
                targetKey = null;
            }
            if (targetKey != null)
            {
                return (T)cache[targetKey];
            }

            throw new KeyNotFoundException(key);
        }
        public bool Set<T>(string key, T value)
        {
            var cacheKey = cache.Keys.SingleOrDefault(k => k.Key == key);
            if (cacheKey == null)
            {
                cacheKey = new CacheKey(key);
            }

            cache[cacheKey] = value;
            return true;
        }
        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            var cacheKey = cache.Keys.SingleOrDefault(k => k.Key == key);
            if (cacheKey == null)
            {
                cacheKey = new CacheKey(key);
            }

            cacheKey.ExpiredOn = DateTimeOffset.Now + expiresIn;
            cache[cacheKey] = value;
            return true;
        }
        public bool Set<T>(string key, T value, DateTimeOffset expiresAt)
        {
            var cacheKey = cache.Keys.SingleOrDefault(k => k.Key == key);
            if (cacheKey == null)
            {
                cacheKey = new CacheKey(key);
            }

            cacheKey.ExpiredOn = expiresAt;
            cache[cacheKey] = value;
            return true;
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc) where T : class
        {
            if (KeyExists(key))
            {
                return Get<T>(key);
            }

            T value = AddCacheFunc();
            Set<T>(key, value);
            return value;
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, TimeSpan expiresIn) where T : class
        {
            if (KeyExists(key))
            {
                return Get<T>(key);
            }

            T value = AddCacheFunc();
            Set<T>(key, value, expiresIn);
            return value;
        }
        public T GetWithAdd<T>(string key, Func<T> AddCacheFunc, DateTimeOffset expiresAt) where T : class
        {
            if (KeyExists(key))
            {
                return Get<T>(key);
            }

            T value = AddCacheFunc();
            Set<T>(key, value, expiresAt);
            return value;
        }
        #endregion


        #region String
        public bool SetString(string key, string value)
        {
            return Set<string>(key, value);
        }
        public bool SetString(string key, string value, TimeSpan expiresIn)
        {
            return Set<string>(key, value, expiresIn);
        }
        public bool SetString(string key, string value, DateTimeOffset expiresAt)
        {
            return Set<string>(key, value, expiresAt);
        }
        public void SetString(Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                Set(item.Key, item.Value);
            }
        }
        public decimal StringIncrement(string key, decimal value = 1)
        {
            decimal currentValue = 0;
            string stringValue = GetString(key);
            if (stringValue != null)
            {
                currentValue = Convert.ToDecimal(stringValue);
            }
            currentValue += value;
            SetString(key, currentValue.ToString());

            return currentValue;
        }

        public decimal StringDecrement(string key, decimal value = 1)
        {
            decimal currentValue = 0;
            string stringValue = GetString(key);
            if (stringValue != null)
            {
                currentValue = Convert.ToDecimal(stringValue);
            }
            currentValue -= value;
            SetString(key, currentValue.ToString());

            return currentValue;
        }

        public string GetString(string key)
        {
            var targetKey = cache.Keys.SingleOrDefault(ck => ck.Key == key);
            if (targetKey != null && targetKey.ExpiredOn < DateTimeOffset.Now)
            {
                cache.Remove(targetKey);
                targetKey = null;
            }
            if (targetKey != null)
            {
                return (string)cache[targetKey];
            }
            return null;
        }
        public IDictionary<string, string> GetString(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                result[key] = GetString(key);
            }
            return result;
        }
        #endregion


        #region HashTable
        public bool SetHash(string key, string fieldKey, string value)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new Dictionary<string, string>();
            }
            var dictionary = cache[cacheKey] as Dictionary<string, string>;
            if (dictionary.ContainsKey(fieldKey))
            {
                dictionary[fieldKey] = value;
                return false;
            }
            else
            {
                dictionary.Add(fieldKey, value);
                return true;
            }
        }
        public void SetHash(string key, Dictionary<string, string> values)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new Dictionary<string, string>();
            }
            var dictionary = cache[cacheKey] as Dictionary<string, string>;
            if (dictionary != null)
            {
                values.Keys.ToList().ForEach(field =>
                {
                    dictionary[field] = values[field];
                });
            }
        }
        public string GetHash(string key, string fieldKey)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                if (dictionary != null && dictionary.ContainsKey(fieldKey))
                {
                    return dictionary[fieldKey];
                }
            }

            return null;
        }
        public Dictionary<string, string> GetHash(string key, IEnumerable<string> fieldKeys)
        {
            var result = new Dictionary<string, string>();
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                if (dictionary != null)
                {
                    foreach (var fieldKey in fieldKeys)
                    {
                        if (dictionary.ContainsKey(fieldKey))
                        {
                            result[fieldKey] = dictionary[fieldKey];
                        }
                        else
                        {
                            result[fieldKey] = string.Empty;
                        }
                    }
                }
            }

            return result;
        }
        public Dictionary<string, string> GetAllHash(string key)
        {
            var result = new Dictionary<string, string>();
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                if (dictionary != null)
                {
                    result = dictionary.ToDictionary(i => i.Key, i => i.Value);
                }
            }
            return result;
        }
        public bool RemoveHash(string key, string fieldKey)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                if (dictionary.ContainsKey(fieldKey))
                {
                    dictionary.Remove(fieldKey);
                    if (dictionary.Count == 0)
                    {
                        cache.Remove(cacheKey);
                    }
                    return true;
                }
            }
            return false;
        }
        public long RemoveHash(string key, IEnumerable<string> fieldKeys)
        {
            long removedCount = 0;
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                foreach (var fieldKey in fieldKeys)
                {
                    if (dictionary.ContainsKey(fieldKey))
                    {
                        dictionary.Remove(fieldKey);
                        removedCount++;
                    }
                }
                if (dictionary.Count == 0)
                {
                    cache.Remove(cacheKey);
                }
            }
            return removedCount;
        }
        public decimal HashIncrement(string key, string fieldKey, decimal value = 1)
        {
            decimal currentValue = 0;
            string stringValue = GetHash(key, fieldKey);
            if (stringValue != null)
            {
                currentValue = Convert.ToDecimal(stringValue);
            }
            currentValue += value;
            SetHash(key, fieldKey, currentValue.ToString());

            return currentValue;
        }
        public decimal HashDecrement(string key, string fieldKey, decimal value = 1)
        {
            decimal currentValue = 0;
            string stringValue = GetHash(key, fieldKey);
            if (stringValue != null)
            {
                currentValue = Convert.ToDecimal(stringValue);
            }
            currentValue -= value;
            SetHash(key, fieldKey, currentValue.ToString());

            return currentValue;
        }
        public IEnumerable<string> HashKeys(string key)
        {
            var result = new List<string>();
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (cache.ContainsKey(cacheKey))
            {
                var dictionary = cache[cacheKey] as Dictionary<string, string>;
                if (dictionary != null)
                {
                    result = dictionary.Keys.ToList();
                }
            }
            return result;
        }
        #endregion


        #region List
        public long SetList(string key, string value, bool addToLeft = false)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new List<string>();
            }
            var list = cache[cacheKey] as List<string>;
            if (list != null)
            {
                if (addToLeft)
                {
                    list.Insert(0, value);
                }
                else
                {
                    list.Add(value);
                }
            }
            return list.Count;
        }
        public long SetList(string key, IEnumerable<string> values, bool addToLeft = false)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new List<string>();
            }
            var list = cache[cacheKey] as List<string>;
            if (list != null)
            {
                if (addToLeft)
                {
                    values.ToList().ForEach(x =>
                    {
                        list.Insert(0, x);
                    });
                }
                else
                {
                    values.ToList().ForEach(x =>
                    {
                        list.Add(x);
                    });
                }
            }
            return list.Count;
        }
        public IEnumerable<string> GetListRange(string key, long start, long stop = -1)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new List<string>();
            }
            var list = cache[cacheKey] as List<string>;
            if (list != null)
            {
                stop = stop == -1 ? list.Count : stop + 1;
                return list.GetRange((int)start, (int)(stop - start));
            }

            return null;
        }
        public string PopList(string key, bool fromLeft = false)
        {
            string result = null;
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new List<string>();
            }
            var list = cache[cacheKey] as List<string>;
            if (list != null)
            {
                int index = 0;
                if (!fromLeft)
                {
                    index = list.Count - 1;
                }
                result = list[index];
                list.RemoveAt(index);
            }

            return result;
        }
        public void RemoveListByRange(string key, long start, long stop)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new List<string>();
            }
            var list = cache[cacheKey] as List<string>;
            if (list != null)
            {
                stop = stop == -1 ? list.Count : stop + 1;
                list.RemoveRange((int)start, (int)(stop - start));
            }
            if (list.Count == 0)
            {
                cache.Remove(cacheKey);
            }
        }
        #endregion


        #region Sorted Set
        public bool SetSortSet(string key, string member, double score)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new CacheSortedListValue();
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.Set(member, score);
        }
        public long SetSortSet(string key, Dictionary<string, double> values)
        {
            long result = 0;
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new CacheSortedListValue();
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            values.Keys.ToList().ForEach(value =>
            {
                if (list.Set(value, values[value]))
                {
                    result++;
                }
            });

            return result;
        }
        public decimal? GetSortSetScore(string key, string member)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new CacheSortedListValue();
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.GetScore(member);
        }
        public decimal SortedSetIncrement(string key, string member, decimal value)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new CacheSortedListValue();
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.SortedSetScoreIncrement(member, value);
        }
        public decimal SortedSetDecrement(string key, string member, decimal value)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                cache[cacheKey] = new CacheSortedListValue();
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.SortedSetScoreIncrement(member, 0 - value);
        }
        public IEnumerable<string> GetSortedSetRange(string key, long start, long stop = -1)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return null;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.GetSortedSetRangeWithScore(start, stop).Keys;
        }
        public Dictionary<string, double> GetSortedSetRangeWithScore(string key, long start, long stop = -1)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return null;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.GetSortedSetRangeWithScore(start, stop);
        }
        public IEnumerable<string> GetSortedSetRangeByScore(string key, double start, double stop)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return null;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.GetSortedSetRangeByScoreWithScore(start, stop).Keys;
        }
        public Dictionary<string, double> GetSortedSetRangeByScoreWithScore(string key, double start, double stop)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return null;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.GetSortedSetRangeByScoreWithScore(start, stop);
        }
        public long RemoveSortedSetByRange(string key, long start, long stop)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return 0;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.RemoveSortedSetByRange(start, stop);
        }
        public long RemoveSortedSetByScore(string key, double start, double stop)
        {
            var cacheKey = cache.Keys.FirstOrDefault(i => i.Key == key) ?? new CacheKey(key);
            if (!cache.ContainsKey(cacheKey))
            {
                return 0;
            }
            var list = cache[cacheKey] as CacheSortedListValue;
            return list.RemoveSortedSetByScore(start, stop);
        }

        #endregion

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


        #region Set

        #endregion
        public bool Lock(string key, TimeSpan expiresIn, Action action)
        {
            action?.Invoke();
            return true;
        }
    }

    internal class CacheRepository : IDictionary<CacheKey, object>
    {
        private Dictionary<CacheKey, object> internalRepo = new Dictionary<CacheKey, object>();

        public object this[CacheKey key]
        {
            get
            {
                return internalRepo[key];
            }
            set
            {
                lock (internalRepo)
                {
                    internalRepo[key] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                return internalRepo.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<CacheKey> Keys
        {
            get
            {
                return internalRepo.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return internalRepo.Values;
            }
        }

        public void Add(KeyValuePair<CacheKey, object> item)
        {
            internalRepo.Add(item.Key, item.Value);
        }

        public void Add(CacheKey key, object value)
        {
            internalRepo.Add(key, value);
        }

        public void Clear()
        {
            internalRepo.Clear();
        }

        public bool Contains(KeyValuePair<CacheKey, object> item)
        {
            return internalRepo.Contains(item);
        }

        public bool ContainsKey(CacheKey key)
        {
            return internalRepo.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<CacheKey, object>[] array, int arrayIndex)
        {
        }

        public IEnumerator<KeyValuePair<CacheKey, object>> GetEnumerator()
        {
            return internalRepo.GetEnumerator();
        }

        public bool Remove(KeyValuePair<CacheKey, object> item)
        {
            return internalRepo.Remove(item.Key);
        }

        public bool Remove(CacheKey key)
        {
            lock (internalRepo)
            {
                return internalRepo.Remove(key);
            }
        }

        public bool TryGetValue(CacheKey key, out object value)
        {
            return internalRepo.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return internalRepo.GetEnumerator();
        }
    }


    internal class CacheKey
    {
        public CacheKey(string key) : this(key, DateTimeOffset.MaxValue)
        {

        }

        public CacheKey(string key, DateTimeOffset expiredOn)
        {
            Key = key;
            ExpiredOn = expiredOn;
        }

        public string Key { get; set; }

        public DateTimeOffset ExpiredOn { get; set; }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }
    }

    internal class CacheSortedListValue
    {
        private List<CacheSortedListItemValue> list;
        public int Count { get { return list.Count; } }

        internal CacheSortedListValue()
        {
            list = new List<CacheSortedListItemValue>();
        }

        internal bool Set(string value, double score)
        {
            bool existing = false;
            CacheSortedListItemValue currentValue = null;
            var currentIndex = list.FindIndex(x => { return x.Name.Equals(value); });
            if (currentIndex == -1)
            {
                currentValue = new CacheSortedListItemValue() { Name = value, Score = (decimal)score };
            }
            else
            {
                existing = true;
                currentValue = list[currentIndex];
                currentValue.Score = (decimal)score;
                list.RemoveAt(currentIndex);
            }

            var newIndex = list.FindIndex(x => { return currentValue.CompareTo(x) < 0; });
            if (newIndex == -1)
            {
                list.Add(currentValue);
            }
            else
            {
                list.Insert(newIndex, currentValue);
            }

            return existing;
        }

        public decimal? GetScore(string member)
        {
            var currentIndex = list.FindIndex(x => x.Name.Equals(member));
            if (currentIndex == -1)
            {
                return null;
            }
            else
            {
                return list[currentIndex].Score;
            }
        }

        public decimal SortedSetScoreIncrement(string member, decimal value)
        {
            CacheSortedListItemValue currentValue = null;
            var currentIndex = list.FindIndex(x => x.Name.Equals(member));
            if (currentIndex != -1)
            {
                currentValue = list[currentIndex];
                list.RemoveAt(currentIndex);
            }
            else
            {
                currentValue = new CacheSortedListItemValue() { Name = member, Score = 0 };
            }
            currentValue.Score += value;
            var newIndex = list.FindIndex(x => { return currentValue.CompareTo(x) < 0; });
            if (newIndex == -1)
            {
                list.Add(currentValue);
            }
            else
            {
                list.Insert(newIndex, currentValue);
            }

            return currentValue.Score;
        }

        public Dictionary<string, double> GetSortedSetRangeWithScore(long start, long stop)
        {
            stop = stop == -1 ? list.Count : stop + 1;
            return list.GetRange((int)start, (int)(stop - start)).ToDictionary(key => key.Name, key => (double)key.Score);
        }

        public Dictionary<string, double> GetSortedSetRangeByScoreWithScore(double start, double stop)
        {
            return list.FindAll(x => { return x.Score >= (decimal)start && x.Score <= (decimal)stop; }).ToDictionary(key => key.Name, key => (double)key.Score);
        }


        public long RemoveSortedSetByRange(long start, long stop)
        {
            return stop - start;
        }
        public long RemoveSortedSetByScore(double start, double stop)
        {
            return list.RemoveAll(x => { return x.Score >= (decimal)start && x.Score <= (decimal)stop; });
        }
    }
    internal class CacheSortedListItemValue : IComparable
    {
        public string Name { get; set; }
        public decimal Score { get; set; }

        public int CompareTo(object obj)
        {
            CacheSortedListItemValue compare = obj as CacheSortedListItemValue;
            if (this.Score != compare.Score)
            {
                return this.Score - compare.Score > 0 ? 1 : -1;
            }
            else
            {
                return this.Name.CompareTo(compare.Name);
            }
        }
    }
}
