using System.Collections.Generic;

namespace JobManager
{
    internal class JobStatus
    {
        private IJob _job;

        public IJob Job
        {
            get { return _job; }
            set { _job = value; }
        }

        private IEnumerable<IJob> _prerequisites;

        public IEnumerable<IJob> Prerequisites
        {
            get { return _prerequisites; }
            set { _prerequisites = value; }
        }

        private StatusEnum _status;

        public StatusEnum Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public JobStatus(IJob job, IEnumerable<IJob> prereqs)
        {
            _job = job;
            _prerequisites = prereqs;
            _status = StatusEnum.New;
        }

        public bool ReadyToRun
        {
            get
            {
                var engine = JobEngine.Instance;
                bool isReady = true;
                foreach (var job in _prerequisites)
                    isReady |= (engine.StatusObject[job].Status == StatusEnum.Completed);
                return isReady;
            }
        }
    }
}
