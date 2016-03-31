using ObjectsFromXml;
using System.IO;

namespace FileJobs
{
    [ObjectBuilder]
    public class FileHandle : IFileHandle
    {
        public FileHandle()
        { }

        public string Name { get; set; }
        
        public Stream Content { get; set; }
        
    }
}
