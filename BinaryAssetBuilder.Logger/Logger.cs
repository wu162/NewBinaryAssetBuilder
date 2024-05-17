using System.IO;
using NLog;

namespace BinaryAssetBuilder
{
    public class Logger
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        
        public static void init()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = $"{Directory.GetCurrentDirectory()}/logs/BuildLog_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH") + ".log" };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            NLog.LogManager.Configuration = config;
            _logger.Info("Logger initialized");
            _logger.Info($"rootDir: {Directory.GetCurrentDirectory()}");
        }

        public static void info(string msg)
        {
            _logger.Info(msg);
        }
        
        public static void error(string msg)
        {
            _logger.Error(msg);
        }
        
        public static void error<T>(T value)
        {
            _logger.Error(value);
        }
    }
}