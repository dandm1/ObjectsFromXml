using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsFromXml
{
    class ExceptionLogger : ILog
    {
        public void Critical(string v)
        {
            throw new Exception(v);
        }

        public void CriticalFormat(string v, params object[] values)
        {
            throw new Exception(string.Format(v, values));
        }

        public void Error(string v)
        {
            throw new Exception(v);
        }

        public void ErrorFormat(string v, params object[] values)
        {
            throw new Exception(string.Format(v, values));
        }

        public void Info(string v)
        {
            //Ignore info
        }

        public void InfoFormat(string v, params object[] values)
        {
            //Ignore info
        }

        public void Warn(string v)
        {
            //Ignore warnings
        }

        public void WarnFormat(string v, params object[] values)
        {
            //Ingore warnings
        }
    }
}
