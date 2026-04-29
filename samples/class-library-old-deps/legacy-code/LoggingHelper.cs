using System;
using log4net;

namespace Contoso.Mapping
{
    public static class LoggingHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LoggingHelper));

        public static void Configure()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public static void Info(string message) => _log.Info(message);
        public static void Warn(string message) => _log.Warn(message);
        public static void Error(string message, Exception ex = null) => _log.Error(message, ex);
    }
}
