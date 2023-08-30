using CronService.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Course_service.Controllers
{
    [Route("Job")]
    public class JobController : Controller
    {
        private readonly ICronService _jobService;
        public JobController(ICronService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet("Create")]
        public IActionResult CreateJobIn30s()
        {
            _jobService.CreateJobIn30s();
              return Ok();
        }
        [HttpGet]
        public IActionResult GetAllJob()
        {
           
            return Ok(_jobService.GetAllJob());
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteJobById(string id)
        {
          _jobService.DeleteJobById(id);
            return Ok();
        }
    }
}
