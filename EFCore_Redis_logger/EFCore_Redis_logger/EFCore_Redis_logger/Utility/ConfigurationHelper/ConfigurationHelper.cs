using EFCore_Redis_logger.Utility.log;

namespace EFCore_Redis_logger.Utility.ConfigurationHelper
{
    public class ConfigurationHelper1
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(ConfigurationHelper1));
        private readonly IConfiguration _configuration;

        public ConfigurationHelper1(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region Basic
        public string Resolve(string node)
        {
            string nodeValue = _configuration[node];
            return nodeValue;
        }

        public T Resolve<T>(string node)
        {
            var targetType = typeof(T);
            var instance = (T)targetType.Assembly.CreateInstance(targetType.FullName);
            var targetSection = _configuration.GetSection(node);
            if (targetSection.Exists())
            {
                targetSection.Bind(instance);
            }
            else
            {
                throw new Exception($"The node {node} is not exist in the configuration file.");
            }
            return instance;
        }

        public IConfigurationSection GetSection(string sectionName)
        {
            return _configuration.GetSection(sectionName);
        }
        #endregion

        #region Redis
        public string RedisConnection()
        {
            var cacheServerInfo = Resolve<CacheConfigInfo>(ConfigurationKeys.CacheServer);
            var connectionString = cacheServerInfo.ConnectionString;
            if (!string.IsNullOrEmpty(cacheServerInfo.Password))
            {
                connectionString = cacheServerInfo.ConnectionString.TrimEnd(new char[] { ' ', ',' }) + ",password=" + cacheServerInfo.Password;
            }
            return connectionString;
        }

        public int RedisExpire()
        {
            var cacheServerInfo = Resolve<CacheConfigInfo>(ConfigurationKeys.CacheServer);
            logger.Info($"ConfigurationHelper - RedisExpire : {cacheServerInfo.DefaultExpirationTime}");
            return cacheServerInfo.DefaultExpirationTime;
        }
        #endregion

        #region Database Connection String

        public string DBConnection()
        {
            var connectionValue = Resolve(ConfigurationKeys.ConnectionString);

            string pwdSting = GetNodeString(connectionValue, ";", "Password", true);
            string unprotectPwdString = UnprotectFromString(pwdSting.Substring(("Password" + "=").Length));
            var unprotectString = connectionValue.Replace(pwdSting.Substring(("Password" + "=").Length), unprotectPwdString);

            return unprotectString;
        }

        private static string GetNodeString(string originalString, string endSplitFlag, string startNodeName, bool needTrimSpace = false)
        {
            try
            {
                if (string.IsNullOrEmpty(originalString))
                {
                    return originalString;
                }
                string nodeString = string.Empty;
                string[] nodeStrings = originalString.Split(new[] { endSplitFlag }, StringSplitOptions.None);
                foreach (var tmpString in nodeStrings)
                {
                    if (tmpString.IndexOf(startNodeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        nodeString = tmpString;
                        if (needTrimSpace)
                        {
                            while (nodeString.Contains(" "))
                            {
                                nodeString = nodeString.Replace(" ", "");
                            }
                        }
                        return nodeString;
                    }
                }
                return nodeString;
            }
            catch (Exception ex)
            {
                return originalString;
            }
        }
        public static string UnprotectFromString(string protectedMsg)
        {
            try
            {
                if (string.IsNullOrEmpty(protectedMsg))
                {
                    return protectedMsg;
                }
                string originalMsg = protectedMsg;

                originalMsg = protectedMsg.Replace("pt{", "").Replace("}", "");

                return originalMsg;
            }
            catch (Exception ex)
            {
                return protectedMsg;
            }
        }
        #endregion
    }
}
