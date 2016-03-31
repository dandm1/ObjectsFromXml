using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.LogManager.GetLogger("Default").Info("Startup.");
            var runner = new Runner();
            runner.Run();
        }
    }
}
