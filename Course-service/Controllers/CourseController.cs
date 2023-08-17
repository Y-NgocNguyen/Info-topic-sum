using CourseService.Interface;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;



namespace Course_service.Controllers
{
    [Route("courses")]
    public class CourseController : Controller
    {
        private readonly dbContext _course;
        private readonly ICourseService _courseService;
        public CourseController(ICourseService courseService, dbContext course)
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
            return _courseService.test();
          
        }
        [HttpGet]
        public ActionResult<IEnumerable<Course>> GetAllCourse()
        {
            return Ok(_courseService.GetAll());
         
        }


        [HttpGet("{id:int:min(1)}")]
        public ActionResult<Course> GetDetailCourse(int id)
        {
            return Ok(_courseService.GetDetailCourse(id));
        }

        [HttpPost]
        public ActionResult<Course> CreateCourse([FromBody] Course course)
        {
            return Ok(_courseService.CreateCourse(course));
        }


        [HttpDelete("{id:int:min(1)}")]
        public ActionResult DeleteCourse(int id)
        {
            _courseService.DeleteCourse(id);
            return Ok();
        }
        
        [HttpPut("{id:int:min(1)}")]
        public IActionResult UpdateCourse(int id, [FromBody] Course course)
        {
            return Ok(_courseService.UpdateCourse(id, course));
        }

        [HttpDelete("Range")]
        public ActionResult DeletaRange([FromBody] int[] ids)
        {
             _courseService.DeleteCourseRange(ids);
            return Ok();
        }
        [HttpPost("Range")]
        public ActionResult addRange([FromBody] Course[] courses)
        {
            return _courseService.AddCourseRange(courses) == true ? Ok() : BadRequest() ;
        }
      /*  [HttpPatch("RangeOne")]
        public ActionResult updateRangeOne([FromBody]UpDatePathOne upDatePathOne)
        {
           
            return _courseService.UpdateRangeOne(upDatePathOne.entityIds, upDatePathOne.columnValues);
        }*/

        [HttpPatch("RangeAny")]
        public ActionResult updateRangeAny([FromBody]Course[] courses)
        {
           
            return Ok(_courseService.UpdateRangeAny(courses));
        }
    }
}
