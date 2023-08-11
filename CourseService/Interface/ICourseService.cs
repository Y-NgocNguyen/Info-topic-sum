using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;


namespace CourseService.Interface
{
    public interface ICourseService
    {
        ActionResult<IEnumerable<Course>> GetAll();
        ActionResult<Course> GetDetailCourse(int id);
        ActionResult<Course> CreateCourse(Course course);
        ActionResult<Course> DeleteCourse(int id);
        IActionResult UpdateCourse(int id, Course course);
    }
}
