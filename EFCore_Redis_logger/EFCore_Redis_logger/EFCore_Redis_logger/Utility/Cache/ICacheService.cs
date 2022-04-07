using StackExchange.Redis;

namespace EFCore_Redis_logger.Utility.Cache
{
    public interface ICacheService
    {
        #region Common used
        bool CacheItem(string key, object item, TimeSpan expiredAfter = default(TimeSpan), string region = null);
        bool FetchItem<T>(string key, out T returnValue);
        T FetchItem<T>(string key);
        bool CacheItemExist(string key);
        void RemoveCacheByRegion(string region);
        void RemoveCache(string key);
        void RemoveWithPattern(string pattern);
        #endregion

        #region Common
        /// <summary>
        /// Delete all the keys on the cache service.
        /// </summary>
        void Clear();
        /// <summary>
        /// Removes the specified key. A key is ignored if it does not exist.
        /// </summary>
        bool Remove(string key);

        Task<long> RemoveAsync(string key);
        /// <summary>
        /// Removes the specified keys. A key is ignored if it does not exist.
        /// </summary>
        void RemoveAll(IEnumerable<string> keys);

        /// <summary>
        /// Set a timeout on key. After the timeout has expired, the key will automatically
        /// be deleted. A key with an associated timeout is said to be volatile in Redis
        /// terminology.
        /// </summary>
        /// <returns>1 if the timeout was set. 0 if key does not exist or the timeout could not be set.</returns>
        bool SetExpire(string key, TimeSpan expiresIn);
        bool SetExpire(string key, DateTimeOffset expiresAt);
        /// <summary>
        /// Check if key is expired.
        /// </summary>
        bool KeyExpired(string key);
        /// <summary>
        /// Get the remaining time to live of a key that has a timeout. Return null when key does not exist or does not have a timeout.
        /// </summary>
        TimeSpan? KeyTimeToLive(string key);

        /// <summary>
        /// Returns if key exists.
        /// </summary>
        /// <param name="key"></param>
        bool KeyExists(string key);

        Task<bool> KeyExistsAsync(string key);
        /// <summary>
        /// Returns all keys matching pattern;
        /// </summary>
        IEnumerable<string> SearchKeys(string pattern);

        /// <summary>
        /// Scan all keys matching pattern; use cursor to improve the performance and not blocking redis too long.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IEnumerable<string> ScanKeys(string pattern);

        #endregion

        #region obj
        /// <summary>
        /// Retrieving the cache information with custom type
        /// </summary>
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key);
        /// <summary>
        /// Adding to updating the cache information with custom type
        /// </summary>
        bool Set<T>(string key, T value);
        bool Set<T>(string key, T value, TimeSpan expiresIn);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn);
        bool Set<T>(string key, T value, DateTimeOffset expiresAt);

        /// <summary>
        /// Retrieving the cache information from cache service, if not exist, add the result of AddCacheFunc function to cache service
        /// </summary>
        /// <param name="AddCacheFunc">The function delegate to retrieve the cache value</param>
        T GetWithAdd<T>(string key, Func<T> AddCacheFunc) where T : class;
        T GetWithAdd<T>(string key, Func<T> AddCacheFunc, TimeSpan expiresIn) where T : class;
        T GetWithAdd<T>(string key, Func<T> AddCacheFunc, DateTimeOffset expiresAt) where T : class;
        #endregion

        #region String
        /// <summary>
        /// Adding to updating the cache information with string value
        /// </summary>
        bool SetString(string key, string value);
        bool SetString(string key, string value, TimeSpan expiresIn);
        bool SetString(string key, string value, DateTimeOffset expiresAt);
        /// <summary>
        /// Adding to updating multiple cache information with string value
        /// </summary>
        void SetString(Dictionary<string, string> items);
        /// <summary>
        /// Increments the string representing a floating point number stored at key by the 
        /// specified increment. If the key does not exist, it is set to 0 before performing
        /// the operation. The precision of the output is fixed at 17 digits after the decimal
        /// point regardless of the actual internal precision of the computation.
        /// </summary>
        /// <returns>the value of key after the increment</returns>
        decimal StringIncrement(string key, decimal value = 1);
        /// <summary>
        /// Decrements the string representing a floating point number stored at key by the
        /// specified decrement. If the key does not exist, it is set to 0 before performing
        /// the operation. The precision of the output is fixed at 17 digits after the decimal
        /// point regardless of the actual internal precision of the computation.
        /// </summary>
        /// <returns>the value of key after the decrement</returns>
        decimal StringDecrement(string key, decimal value = 1);

        /// <summary>
        /// Retrieving the cache information with string value
        /// </summary>
        string GetString(string key);
        /// <summary>
        /// Retrieving multiple cache information with string value
        /// </summary>
        IDictionary<string, string> GetString(IEnumerable<string> keys);
        #endregion

        #region HashTable
        /// <summary>
        /// Adding or updating field in the hash stored at key to value. If key does not exist, a new key
        /// holding a hash is created. If field already exists in the hash, it is overwritten.
        /// </summary>
        /// <returns>
        /// 1 if field is a new field in the hash and value was added. 
        /// 0 if field already exists in the hash and the value was updated.
        /// </returns>
        bool SetHash(string key, string fieldKey, string value);
        /// <summary>
        /// Sets the specified fields to their respective values in the hash stored at key.
        /// This command overwrites any existing fields in the hash. If key does not exist,
        /// a new key holding a hash is created.
        /// </summary>
        void SetHash(string key, Dictionary<string, string> values);
        /// <summary>
        /// Retrieving the value associated with field in the hash stored at key.
        /// </summary>
        string GetHash(string key, string fieldKey);
        /// <summary>
        /// Returns the values associated with the specified fields in the hash stored at
        /// key. For every field that does not exist in the hash, a nil value is returned.Because
        /// a non-existing keys are treated as empty hashes, running HMGET against a non-existing
        /// key will return a list of nil values.
        /// </summary>
        /// <returns>list of values associated with the given fields, in the same order as they are requested.</returns>
        Dictionary<string, string> GetHash(string key, IEnumerable<string> fieldKeys);
        /// <summary>
        /// Returns all fields and values of the hash stored at key.
        /// </summary>
        Dictionary<string, string> GetAllHash(string key);

        /// <summary>
        /// Removes the specified fields from the hash stored at key. Non-existing fields
        /// are ignored. Non-existing keys are treated as empty hashes and this command returns
        /// 0.
        /// </summary>
        /// <returns>The number of fields that were removed.</returns>
        bool RemoveHash(string key, string fieldKey);
        long RemoveHash(string key, IEnumerable<string> fieldKeys);
        /// <summary>
        /// Increment the specified field of an hash stored at key, and representing a floating
        /// point number, by the specified increment. If the field does not exist, it is
        /// set to 0 before performing the operation.
        /// </summary>
        /// <returns>the value at field after the increment operation.</returns>
        decimal HashIncrement(string key, string fieldKey, decimal value = 1);
        /// <summary>
        /// Decrement the specified field of an hash stored at key, and representing a floating
        /// point number, by the specified decrement. If the field does not exist, it is
        /// set to 0 before performing the operation.
        /// </summary>
        /// <returns>the value at field after the decrement operation.</returns>
        decimal HashDecrement(string key, string fieldKey, decimal value = 1);
        /// <summary>
        /// Returns all field names in the hash stored at key.
        /// </summary>
        /// <returns>list of fields in the hash, or an empty list when key does not exist.</returns>
        IEnumerable<string> HashKeys(string key);
        #endregion

        #region Set
        long RemoveSet(string setId, List<string> items);
        List<string> GetAllSet(string setId);
        long AddSetItem(string setId, string value);
        long AddSet(string setId, List<string> items);
        bool SetContains(string key, string value);
        bool SetMove(string sourceKey, string destinationKey, string value);
        string[] GetItemFromSetByCount(string key, int count);
        #endregion

        #region List
        /// <summary>
        /// Insert the specified value at the head or end of the list stored at key. If key does
        /// not exist, it is created as empty list before performing the push operations.
        /// </summary>
        /// <returns>the length of the list after the push operations.</returns>
        long SetList(string key, string value, bool addToLeft = false);
        /// <summary>
        /// Insert all the specified values at the head or end of the list stored at key. If key
        /// does not exist, it is created as empty list before performing the push operations.
        /// Elements are inserted one after the other to the head of the list, from the leftmost
        /// element to the rightmost element. So for instance the command LPUSH mylist a
        /// b c will result into a list containing c as first element, b as second element
        /// and a as third element.
        /// </summary>
        /// <returns>the length of the list after the push operations.</returns>
        long SetList(string key, IEnumerable<string> values, bool addToLeft = false);
        /// <summary>
        /// Returns the specified elements of the list stored at key. The offsets start and
        /// stop are zero-based indexes, with 0 being the first element of the list (the
        /// head of the list), 1 being the next element and so on. These offsets can also
        /// be negative numbers indicating offsets starting at the end of the list.For example,
        /// -1 is the last element of the list, -2 the penultimate, and so on. Note that
        /// if you have a list of numbers from 0 to 100, LRANGE list 0 10 will return 11
        /// elements, that is, the rightmost item is included.
        /// </summary>
        /// <returns>list of elements in the specified range.</returns>
        IEnumerable<string> GetListRange(string key, long start, long stop = -1);
        string PopList(string key, bool fromLeft = false);
        /// <summary>
        /// Trim an existing list so that it will contain only the specified range of elements
        /// specified. Both start and stop are zero-based indexes, where 0 is the first element
        /// of the list (the head), 1 the next element and so on. For example: LTRIM foobar
        /// 0 2 will modify the list stored at foobar so that only the first three elements
        /// of the list will remain. start and end can also be negative numbers indicating
        /// offsets from the end of the list, where -1 is the last element of the list, -2
        /// the penultimate element and so on.
        /// </summary>
        void RemoveListByRange(string key, long start, long stop);
        #endregion

        #region Sorted Set
        /// <summary>
        /// Adds the specified member with the specified score to the sorted set stored at
        /// key. If the specified member is already a member of the sorted set, the score
        /// is updated and the element reinserted at the right position to ensure the correct
        /// ordering.
        /// </summary>
        /// <returns>True if the value was added, False if it already existed (the score is still updated)</returns>
        bool SetSortSet(string key, string member, double score);
        /// <summary>
        /// Adds all the specified members with the specified scores to the sorted set stored
        /// at key. If a specified member is already a member of the sorted set, the score
        /// is updated and the element reinserted at the right position to ensure the correct
        /// ordering.
        /// </summary>
        /// <returns>The number of elements added to the sorted sets, not including elements already existing for which the score was updated.</returns>
        long SetSortSet(string key, Dictionary<string, double> values);
        /// <summary>
        /// Returns the score of member in the sorted set at key; If member does not exist
        /// in the sorted set, or key does not exist, nil is returned.
        /// </summary>
        /// <returns>the score of member</returns>
        decimal? GetSortSetScore(string key, string member);
        /// <summary>
        /// Increments the score of member in the sorted set stored at key by increment.
        /// If member does not exist in the sorted set, it is added with -decrement as its
        /// score (as if its previous score was 0.0).
        /// </summary>
        /// <returns>the new score of member</returns>
        decimal SortedSetIncrement(string key, string member, decimal value);
        /// <summary>
        /// Decrements the score of member in the sorted set stored at key by decrement.
        /// If member does not exist in the sorted set, it is added with increment as its
        /// score (as if its previous score was 0.0).
        /// </summary>
        /// <returns>the new score of member</returns>
        decimal SortedSetDecrement(string key, string member, decimal value);
        /// <summary>
        /// Returns the specified range of elements in the sorted set stored at key. By default
        /// the elements are considered to be ordered from the lowest to the highest score.
        /// Lexicographical order is used for elements with equal score. Both start and stop
        /// are zero-based indexes, where 0 is the first element, 1 is the next element and
        /// so on. They can also be negative numbers indicating offsets from the end of the
        /// sorted set, with -1 being the last element of the sorted set, -2 the penultimate
        /// element and so on.
        /// </summary>
        /// <returns>list of elements in the specified range</returns>
        IEnumerable<string> GetSortedSetRange(string key, long start, long stop = -1);
        Dictionary<string, double> GetSortedSetRangeWithScore(string key, long start, long stop = -1);
        /// <summary>
        /// Returns the specified range of elements in the sorted set stored at key. By default
        /// the elements are considered to be ordered from the lowest to the highest score.
        /// Lexicographical order is used for elements with equal score. Start and stop are
        /// used to specify the min and max range for score values. Similar to other range
        /// methods the values are inclusive.
        /// </summary>
        /// <returns>list of elements in the specified range</returns>
        IEnumerable<string> GetSortedSetRangeByScore(string key, double start, double stop);
        Dictionary<string, double> GetSortedSetRangeByScoreWithScore(string key, double start, double stop);
        /// <summary>
        /// Removes all elements in the sorted set stored at key with rank between start
        /// and stop. Both start and stop are 0 -based indexes with 0 being the element with
        /// the lowest score. These indexes can be negative numbers, where they indicate
        /// offsets starting at the element with the highest score. For example: -1 is the
        /// element with the highest score, -2 the element with the second highest score
        /// and so forth.
        /// </summary>
        /// <returns>the number of elements removed.</returns>
        long RemoveSortedSetByRange(string key, long start, long stop);
        long RemoveSortedSetByScore(string key, double start, double stop);
        #endregion

        //分布式锁
        bool Lock(string key, TimeSpan expiresIn, Action action);
    }
}
