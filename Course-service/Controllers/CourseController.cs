using CourseService.Interface;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;



namespace Course_service.Controllers
{
    [Route("courses")]
    public class CourseController : Controller
    {
        
        private readonly ICourseService _courseService;
        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;

        }
        [HttpGet]
        public ActionResult<IEnumerable<Course>> GetAllCourse()
        {
           return _courseService.GetAll();
         
        }


        [HttpGet("{id:int:min(1)}")]
        public ActionResult<Course> GetDetailCourse(int id)
        {
            return _courseService.GetDetailCourse(id);
        }

        [HttpPost]
        public ActionResult<Course> CreateCourse([FromBody] Course course)
        {
            return _courseService.CreateCourse(course);
        }


        [HttpDelete("{id:int:min(1)}")]
        public ActionResult<Course> DeleteCourse(int id)
        {
           return _courseService.DeleteCourse(id);
        }
        [HttpPut("{id:int:min(1)}")]
        public IActionResult UpdateCourse(int id, [FromBody] Course course)
        {
            return _courseService.UpdateCourse(id, course);
        }
    }
}
