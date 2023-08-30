
using Hangfire;
using Hangfire.Common;

namespace Course_service.Job
{
    public class MyJob
    {
        private readonly ILogger<MyJob> logger;
       public MyJob(ILogger<MyJob> logger)
        {
            this.logger = logger;
        }

        //create function cron Run Every Minute with hangfire
        public void cronRunEvery30Seconds()
        {
            logger.LogInformation("RunEveryMinute");
        }
  
        
    }
}
