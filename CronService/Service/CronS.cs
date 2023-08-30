using CloudService.Interface;
using CronService.Interface;
using Hangfire;
using Hangfire.Storage;

namespace CronService.Service
{
    public class CronS : ICronService
    {
        //create job in 30s
        public void CreateJobIn30s()
        {
            //create job in 30s
            RecurringJob.AddOrUpdate<ICloud>("DailyExport", x => x.ExportEnrollmentToCSV(), Cron.Minutely());
        }

        //delete job by id
        public void DeleteJobById(string id)
        {
            //delete job by id
            RecurringJob.RemoveIfExists(id);
        }

        //get all job
        public List<(string, string, string)> GetAllJob()
        {
            //get all job
            var list = JobStorage.Current.GetConnection().GetRecurringJobs();

            var jobList = list.Select(job => (
                job.Id,
                Method: job.Job.Method.Name,
                CronExpression: job.Cron

            )).ToList();

            return jobList;
        }
    }
}