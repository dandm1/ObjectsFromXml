using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobManager
{
    public class JobEngine
    {
        public static JobEngine Instance { get { return _manager; } }

        static JobEngine _manager;

        static JobEngine()
        {
            _manager = new JobEngine();
        }

        Dictionary<IJob, List<IJob>> _jobBuffer;

        public JobEngine()
        {
            _jobBuffer = new Dictionary<IJob, List<IJob>>();
        }

        private Dictionary<IJob,JobStatus> _statusObject;

        internal Dictionary<IJob,JobStatus> StatusObject
        {
            get
            {
                if (_statusObject == null)
                    _statusObject = new Dictionary<IJob, JobStatus>();

                return _statusObject;
            }
        }

        public void Add(IJob theJob)
        {
            var prereqs = theJob.PreReqs ?? new List<IJob>();
            _jobBuffer.Add(theJob, prereqs.ToList());
            var statusObj = new JobStatus(theJob, theJob.PreReqs);
            StatusObject[theJob] = statusObj;
        }

        public void Run()
        {
            while (StatusObject.Any(x => x.Value.ReadyToRun))
            {
                var readyJobs = StatusObject.Where(x => x.Value.ReadyToRun).ToList();
                foreach (var job in readyJobs)
                    job.Value.Status = StatusEnum.Ready;

                foreach (var job in readyJobs)
                {
                    try
                    {

                        if (job.Key.Run())
                            job.Value.Status = StatusEnum.Completed;
                    }
                    catch (Exception ex)
                    {
                        job.Value.Status = StatusEnum.Error;
                    }
                }
            };
        }
    }
}
