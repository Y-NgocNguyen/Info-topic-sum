using sharedservice.Models;


namespace CourseService.Interface
{
    public interface ICourseService
    {
        Course CreateCourse(Course course);
        bool AddCourseRange(Course[] courses);
        void DeleteCourse(int id);
        void DeleteCourseRange(int[] ids);
        IQueryable<Course> GetAll();
        dynamic GetDetailCourse(int id);
       Course getCourstByCode(string code);
        Course? UpdateCourse(int id, Course course);

        bool UpdateRange(Course[] course);
        string UpdateRangeAny(Course[] courses);

        dynamic test();
    }
}
