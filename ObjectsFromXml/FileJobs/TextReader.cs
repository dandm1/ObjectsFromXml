using System;
using ObjectsFromXml;
using JobManager;
using System.Collections.Generic;

//<TextReader Name="Read Text" Format="HTML">
namespace FileJobs
{
    [ObjectBuilder]
    public class TextReader : IJob
    {
        public string Name { get; set; }
        public string Format { get; set; }
        public double[] Numbers { get; set; }
        public TextReader()
        {
        }

        private IFileHandle _parameters;
        public IJobParameters Parameters
        {
            get { return _parameters; }
            set { _parameters = (IFileHandle)value; }
        }

        private IEnumerable<IJob> _prereqs;
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
            if (_parameters == null)
            {
                throw new Exception("Parameters are required.");
                return false;
            }

            var textReader = new System.IO.StreamReader(_parameters.Content);
            string theString = textReader.ReadToEnd();
            return true;
        }
    }
}
