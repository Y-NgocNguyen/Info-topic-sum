using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronService.Interface
{
    public interface ICronService
    {
        void CreateJobIn30s();
        void DeleteJobById(string id);
        List<dynamic> GetAllJob();
    }
}
