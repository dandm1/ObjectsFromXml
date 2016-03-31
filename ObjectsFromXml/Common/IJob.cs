using ObjectsFromXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobManager
{
    [ObjectBuilder]
    public interface IJob
    {
        IEnumerable<IJob> PreReqs { get; set; }
        bool Run();
    }
}
