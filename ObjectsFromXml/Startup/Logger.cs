using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ObjectsFromXml
{
    public class Logger
    {
        private static ILog _logger;

        static Logger()
        {
            _logger = log4net.LogManager.GetLogger("Default");
        }

        public static void Info(string message)
        {
            _logger.Info(message);
        }

        public static void Error(string message,Exception ex)
        {
            _logger.Error(message,ex);
        }

        internal static void Error(string v)
        {
            _logger.Error(v);
        }
    }
}
