using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace HostConsoleApp
{
    public class StringLogger : ObjectsFromXml.ILog
    {
        private StringBuilder _logger;

        public override string ToString()
        {
            return _logger.ToString();
        }

        public StringLogger()
        {
            _logger = new StringBuilder();
        }


        public void Info(string message)
        {
            _logger.AppendFormat("INFO : {0} : {1}\r\n", DateTime.Now, message);
        }

        public void Error(string message)
        {
            _logger.AppendFormat("ERROR : {0} : {1}\r\n", DateTime.Now, message);
        }

        public void Error(string message,Exception ex)
        {
            _logger.AppendFormat("INFO : {0} : {1}\r\nError is:{2}\r\n", DateTime.Now, message,ex);
        }

        public void ErrorFormat(string v, params object[] values)
        {
            _logger.AppendFormat("ERROR : {0} : ",DateTime.Now);
            _logger.AppendFormat(v, values);
            _logger.Append("\r\n");
        }

        public void Warn(string v)
        {
            _logger.AppendFormat("WARN : {0} : {1}\r\n", DateTime.Now, v);
        }

        public void WarnFormat(string v, params object[] values)
        {
            _logger.AppendFormat("WARN : {0} : ", DateTime.Now);
            _logger.AppendFormat(v, values);
            _logger.Append("\r\n");
        }

        public void InfoFormat(string v, params object[] values)
        {
            _logger.AppendFormat("INFO : {0} : ", DateTime.Now);
            _logger.AppendFormat(v, values);
            _logger.Append("\r\n");
        }

        public void Critical(string v)
        {
            _logger.AppendFormat("CRIT : {0} : {1}\r\n", DateTime.Now, v);
        }

        public void Critical(string v,Exception ex)
        {
            _logger.AppendFormat("CRIT : {0} : {1}\r\nError is:{2}\r\n", DateTime.Now, v, ex);
        }

        public void CriticalFormat(string v, params object[] values)
        {
            _logger.AppendFormat("CRIT : {0} : ", DateTime.Now);
            _logger.AppendFormat(v, values);
            _logger.Append("\r\n");
        }
    }
}
