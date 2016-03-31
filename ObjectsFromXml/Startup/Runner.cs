using System.Collections.Generic;
using System.IO;
using JobManager;
using ObjectsFromXml;

namespace HostConsoleApp
{
    public class Runner
    {
        public void Run()
        {
            var manager = JobEngine.Instance;
            var fileStream = new FileStream("Startup.xml",FileMode.Open,FileAccess.Read);
            var processBuilder = new ObjectBuilder(fileStream,typeof(IJob));
            IEnumerable<IJob> jobs = processBuilder.Build<IJob>();
            foreach (IJob job in jobs)
                manager.Add(job);

            manager.Run();
        }
    }
}
