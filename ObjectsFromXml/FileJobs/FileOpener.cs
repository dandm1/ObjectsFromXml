using System;
using System.Collections.Generic;
using ObjectsFromXml;
using JobManager;

namespace FileJobs
{
    [ObjectBuilder]
    public class FileOpener : IJob
    {
        IFileHandle _parameters;
        public FileOpener()
        {
        }

        public IFileHandle Parameters
        {
            get { return _parameters; }
        
            set { _parameters = value; }
        }

        public string Name { get; set; }
        public string Filename { get; set; }
        public string FileMode { get; set; }

        IEnumerable<IJob> _prereqs;
        public IEnumerable<IJob> PreReqs
        {
            get
            {
                if (_prereqs == null)
                    _prereqs = new List<IJob>();

                return _prereqs;
            }

            set
            {
                _prereqs = value;
            }
        }

        public bool Run()
        {
            try
            {
                if (FileMode == "Read")
                    _parameters.Content = System.IO.File.OpenRead(Filename);
                else
                    _parameters.Content = System.IO.File.Open(Filename, System.IO.FileMode.Open);
            }
            catch(Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
