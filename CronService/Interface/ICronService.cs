namespace CronService.Interface
{
    public interface ICronService
    {
        void CreateJobIn30s();

        void DeleteJobById(string id);

        List<(string, string, string)> GetAllJob();
    }
}