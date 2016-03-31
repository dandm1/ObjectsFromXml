using ObjectsFromXml;
using JobManager;


namespace FileJobs
{

    public interface IFileHandle : IJobParameters
    {
        System.IO.Stream Content { get; set; }
    }
}
