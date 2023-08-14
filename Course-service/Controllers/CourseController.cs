using CourseService.Interface;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;



namespace Course_service.Controllers
{
    [Route("courses")]
    public class CourseController : Controller
    {
        private readonly courseContext _course;
        private readonly ICourseService _courseService;
        public CourseController(ICourseService courseService, courseContext course)
        {
            _courseService = courseService;
            _course = course;
        }
        [HttpGet("test")]
        public IActionResult test()
        {
            var query = from enrollment in _course.Enrollments
                        join course in _course.Courses on enrollment.CouresId equals course.Id
                        select new
                        {
                            enrollment.CouresId,
                            enrollment.EnrolledDate,
                            Course = course
                        };
           
            var query3 = from course in _course.Courses
                          select new
                          {
                             course.Id,
                             a = _course.Enrollments.Where(e=>e.CouresId == course.Id).ToList()
                          };

            var query4 = from course in _course.Courses
                         select new
                         {
                             course.Id,
                             counte = _course.Enrollments.Where(e => e.CouresId == course.Id).Count()
                         };
            return Ok(query4);
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

        [HttpDelete("deleteRange")]
        public ActionResult DeletaRange([FromBody] int[] ids)
        {
            return _courseService.DeleteCourseRange(ids);
        }
        [HttpPost("addRange")]
        public ActionResult addRange([FromBody] Course[] courses)
        {
            return _courseService.AddCourseRange(courses);
        }
        [HttpPatch("updateRangeOne")]
        public ActionResult updateRangeOne([FromBody]UpDatePathOne upDatePathOne)
        {
           
            return _courseService.UpdateRangeOne(upDatePathOne.entityIds, upDatePathOne.columnValues);
        }

        [HttpPatch("updateRangeAny")]
        public ActionResult updateRangeAny([FromBody]Course[] courses)
        {
            return _courseService.UpdateRangeAny(courses);
        }
    }
}
