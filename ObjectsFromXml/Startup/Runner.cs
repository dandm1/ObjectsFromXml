using System.Collections.Generic;
using System.IO;
using JobManager;
using ObjectsFromXml;
using System;

namespace HostConsoleApp
{
    public class Runner
    {
        public void Run()
        {
            var manager = JobEngine.Instance;
            var fileStream = new FileStream("Startup.xml",FileMode.Open,FileAccess.Read);
            var processBuilder = new ObjectBuilder(fileStream,typeof(IJob));
            var logger = new StringLogger();
            processBuilder.Logger = logger;
            IEnumerable<IJob> jobs = processBuilder.Build<IJob>();
            Console.Write(logger.ToString());
            foreach (IJob job in jobs)
                manager.Add(job);

            //manager.Run();
        }
    }
}
