using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsFromXml
{
    public interface IExtraParameters
    {
        void AddParameter(string key, dynamic parameter);
        IDictionary<string, dynamic> Parameters { get; }
    }
}
