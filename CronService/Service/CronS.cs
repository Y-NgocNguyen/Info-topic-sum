using CloudService.Interface;
using CronService.Interface;
using Hangfire;
using Hangfire.Storage;

namespace CronService.Service
{
    public class CronS:ICronService
    {
        private readonly ICloud  _cloud;
        public CronS(ICloud cloud)
        {
           _cloud = cloud;
        }
        //create job in 30s
        public async void CreateJobIn30s()
        {
            //create job in 30s
            RecurringJob.AddOrUpdate<ICloud>(x => x.ExportEnRollMentToCSV(), "*/30 * * * * *");

           /* BackgroundJob.Schedule<ICloud>(x => x.ExportEnRollMentToCSV(), TimeSpan.FromMinutes(5));*/
        }
        //get all job
        public List<dynamic> GetAllJob()
        {
            //get all job
            var list = JobStorage.Current.GetConnection().GetRecurringJobs();

            var jobList = list.Select(job => new
            {
                Id = job.Id,
                Method = job.Job.Method.Name,
                CronExpression = job.Cron

            }).ToList<dynamic>();

            return jobList;
        }
        //delete job by id
        public void DeleteJobById(string id)
        {
            //delete job by id
            RecurringJob.RemoveIfExists(id);
            
        }
    }
}
