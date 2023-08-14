using sharedservice.Models;
using CourseService.Interface;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Repository;
using sharedservice.UnitofWork;
using Microsoft.Extensions.Logging;

using System.Collections.Immutable;

namespace CourseService.Service
{
    public class CourseServiceB : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Course> _db;
        private readonly IGenericRepository<Enrollment> _db2;
        private readonly ILogger<CourseServiceB> _logger;
        public CourseServiceB(IUnitOfWork unitOfWork, ILogger<CourseServiceB> logger)
        {
            _unitOfWork = unitOfWork;
            _db = _unitOfWork.GetRepository<Course>();
            _db2 = _unitOfWork.GetRepository<Enrollment>();
            _logger = logger;
        }
        /// <summary>
        /// Creates a new course and saves it to the database.
        /// </summary>
        /// <param name="course"> The course object to create. </param>
        /// <returns>An ActionResult</returns>
        public ActionResult<Course> CreateCourse(Course course)
        {
            if (course == null)
            {
                return new BadRequestResult();
            }
            course.Price ??=  0.0f;

            _db.Add(course);
            _unitOfWork.SaveChangesAsync();

            return new OkResult();
        }
        public ActionResult AddCourseRange(Course[] courses )
        {
            if(!courses.Any())
            {
                return new BadRequestResult();
            }
            courses = courses.Select(c =>
            {
                c.Price ??= 0.0f;
                return c;
            }).ToArray();

            _db.AddRange(courses);
            _unitOfWork.SaveChangesAsync();
            return new OkResult();
        }
        /// <summary>
        /// Deletes a course from the database based on its ID.
        /// </summary>
        /// <param name="id">The ID of the course to delete.</param>
        /// <returns>An ActionResult</returns>
        public ActionResult<Course> DeleteCourse(int id)
        {
            var courses = _db.Find(u => u.Id == id).FirstOrDefault();

            if (courses == null)
            {
                return new NotFoundResult();
            }
            
            _db.Remove(courses);

            _unitOfWork.SaveChangesAsync();
            return new NoContentResult();
        }
        
        public  ActionResult DeleteCourseRange(int[] ids)
        {
            var coursesDelete = _db.Find(c => ids.Contains(c.Id) );
           
            if (coursesDelete == null || coursesDelete.Count() != ids.Length)
            {
              return new NotFoundResult();
            }
            _db.RemoveRange(coursesDelete);
            _unitOfWork.SaveChangesAsync();
            return new OkResult();

        }

        /// <summary>
        /// Retrieves all courses from the database.
        /// </summary>
        /// <returns>An ActionResult containing a collection of courses.</returns>
        public ActionResult<IEnumerable<Course>> GetAll()
        { 
            return new OkObjectResult(
                _db.GetAll().Select(c => new
            {
                c.Id,
                c.Code,
                c.Price,
                c.Decription
            }));
        }
        /// <summary>
        /// Retrieves the details of a specific course from the database based on its ID.
        /// </summary>
        /// <param name="id">The ID of the course to retrieve.</param>
        /// <returns>An ActionResult containing the details of the course.</returns>
        public ActionResult<Course> GetDetailCourse(int id)
        {
           
            var course = _db.Find(u=>u.Id == id).Select(c => new
            {
                c.Id,
                c.Decription,
                c.Price,
                c.Code
            }).FirstOrDefault();
            if (course == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(course);
        }
        /// <summary>
        /// Updates the details of a specific course in the database based on its ID.
        /// </summary>
        /// <param name="id"> ID of the course to update.</param>
        /// <param name="course"> updated course object.</param>
        /// <returns>An IActionResult</returns>
        public IActionResult UpdateCourse(int id, Course course)
        {
            var coursedb = _db.Find(u => u.Id == id).FirstOrDefault();

            if (coursedb == null)
            {
                return new NotFoundResult();
            }

            coursedb.Price = course.Price;
            coursedb.Decription = course.Decription;
            coursedb.Code = course.Code;
            _unitOfWork.SaveChangesAsync();
            return new NoContentResult();
        }
        /// <summary>
        /// funciton test. has bug. not interested
        /// </summary>
        /// <param name="entityIds"></param>
        /// <param name="columnValues"></param>
        /// <returns></returns>
        public ActionResult UpdateRangeOne(int[] entityIds, Dictionary<string, object> columnValues)
        {
            List<Course>  courses = _db.Find(c=> entityIds.Contains(c.Id)).ToList();
         
            _db.UpdateRangeOne(courses,columnValues);
            _unitOfWork.SaveChangesAsync();
            return new OkResult();
        }
        /// <summary>
        /// Update courses with some property 
        /// </summary>
        /// <param name="courses"></param>
        /// <returns></returns>
        public ActionResult UpdateRangeAny(Course[] courses)
        {

            List<Course> updateCou = new List<Course>();
            foreach (var course in courses)
            {
                var temp = _db.Find(e => e.Id == course.Id ).FirstOrDefault();
                _db.entry(temp);
                if (temp == null) new BadRequestResult();

                updateCou.Add(_db.update2Oject(course, temp));
            }
            _db.UpdateRangeAny(updateCou);
            _unitOfWork.SaveChangesAsync();
            return new OkResult();
        }


    }
}
