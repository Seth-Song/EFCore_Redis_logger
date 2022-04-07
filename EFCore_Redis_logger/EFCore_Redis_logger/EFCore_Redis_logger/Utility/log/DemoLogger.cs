using NLog;
using NLog.Extensions.Logging;
using System.Globalization;
using LogLevel = NLog.LogLevel;

namespace EFCore_Redis_logger.Utility.log
{
    public class DemoLogger
    {
        private Logger _logger;
        private static EventWaitHandle WaitHandle = new ManualResetEvent(true);
        private static readonly TimeSpan FlushTimeout = new TimeSpan(0, 1, 0);
        private static readonly TimeSpan ThreadWait = new TimeSpan(0, 0, 1);
        private DemoLogger(Type loggerType)
        {
            _logger = LogManager.GetLogger(loggerType.FullName, loggerType);
        }

        private DemoLogger(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        public static DemoLogger GetInstance(Type type)
        {
            return new DemoLogger(type);
        }

        public static DemoLogger GetInstance(string name)
        {
            return new DemoLogger(name);
        }

        public static ILoggerFactory DemoLoggerFactory = new LoggerFactory(new List<ILoggerProvider>() { new NLogLoggerProvider() });

        static DemoLogger()
        {
        }

        public void Debug(string msg, params object[] args)
        {
            WriteLog(msg, LogLevel.Debug, null, args);
        }

        public void Debug(string msg)
        {
            WriteLog(msg, LogLevel.Debug);
        }

        public void Info(string msg, params object[] args)
        {
            WriteLog(msg, LogLevel.Info, null, args);
        }

        public void Info(string msg)
        {
            WriteLog(msg, LogLevel.Info);
        }

        public void Warn(string msg, params object[] args)
        {
            WriteLog(msg, LogLevel.Warn, null, args);
        }

        public void Warn(string msg, Exception err)
        {
            WriteLog(msg, LogLevel.Warn, err);
        }

        public void Error(string msg, params object[] args)
        {
            WriteLog(msg, LogLevel.Error, null, args);
        }

        public void Error(string msg, Exception err)
        {
            WriteLog(msg, LogLevel.Error, err);
        }

        public void Fatal(string msg, params object[] args)
        {
            WriteLog(msg, LogLevel.Fatal, null, args);
        }

        public void Fatal(string msg, Exception err)
        {
            WriteLog(msg, LogLevel.Fatal, err);
        }

        private void WriteLog(string msg, LogLevel level, Exception e = null, params object[] args)
        {
            var ei = new LogEventInfo(level, _logger.Name, CultureInfo.CurrentCulture, msg, args, e)
            {
                TimeStamp = DateTime.Now,
                Level = level
            };
            _logger.Log(ei);
            WaitHandle.Set();
        }
    }
}
