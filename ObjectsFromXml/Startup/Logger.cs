using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace HostConsoleApp
{
    public class Logger : ObjectsFromXml.ILog
    {
        private log4net.ILog _logger;

        public Logger()
        {
            _logger = log4net.LogManager.GetLogger("Default");
        }

        public Logger(log4net.ILog inLog)
        {
            _logger = inLog;
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message,Exception ex)
        {
            _logger.Error(message,ex);
        }

        public void ErrorFormat(string v, params object[] values)
        {
            _logger.ErrorFormat(v, values);
        }

        public void Warn(string v)
        {
            _logger.Warn(v);
        }

        public void WarnFormat(string v, params object[] values)
        {
            _logger.WarnFormat(v, values);
        }

        public void InfoFormat(string v, params object[] values)
        {
            _logger.InfoFormat(v, values);
        }

        public void Critical(string v)
        {
            _logger.Fatal(v);
        }

        public void Critical(string v,Exception ex)
        {
            _logger.Fatal(v, ex);
        }

        public void CriticalFormat(string v, params object[] values)
        {
            _logger.FatalFormat(v, values);
        }
    }
}
