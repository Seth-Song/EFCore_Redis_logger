namespace EFCore_Redis_logger.Utility.ConfigurationHelper
{
    public class CacheConfigInfo
    {
        public string CacheName { get; set; }
        public string Password { get; set; }
        public int RetryCount { get; set; } = 5;
        public int ReconnectInterval { get; set; } = 60;
        public CacheServerType ServerType { get; set; }
        public string ConnectionString { get; set; }
        public int DefaultExpirationTime { get; set; } = 120;
    }

    internal enum RuningMode
    {
        Normal,
        FallBack
    }

    public enum CacheServerType
    {
        Redis = 0, //CSRedisCore used by default
        Memory = 2,
    }
}
