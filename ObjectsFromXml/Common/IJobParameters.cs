using ObjectsFromXml;

namespace JobManager
{
    [ObjectBuilder]
    public interface IJobParameters
    {
        string Name { get; set; }
    }
}
