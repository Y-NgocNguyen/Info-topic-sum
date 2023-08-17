using sharedservice.Models;
using CourseService.Interface;
using sharedservice.Repository;
using sharedservice.UnitofWork;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Reflection;

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
        public Course CreateCourse(Course course)
        {
            course.Price ??=  0.0f;
            _db.Add(course);
            _unitOfWork.SaveChanges();
            return course;
        }
        public bool AddCourseRange(Course[] courses )
        {
            
            courses = courses.Select(c =>
            {
                c.Price ??= 0.0f;
                return c;
            }).ToArray();

            _db.AddRange(courses);
            _unitOfWork.SaveChanges();

            return true;
        }
        /// <summary>
        /// Deletes a course from the database based on its ID.
        /// </summary>
        /// <param name="id">The ID of the course to delete.</param>
        /// <returns>An ActionResult</returns>
        public void DeleteCourse(int id)
        {
            var courses = _db.Find(u => u.Id == id).FirstOrDefault();
   
            _db.Remove(courses);

            _unitOfWork.SaveChanges();
            
        }
        
        public void DeleteCourseRange(int[] ids)
        {
            var coursesDelete = _db.Find(c => ids.Contains(c.Id) );
           
            _db.RemoveRange(coursesDelete);
            _unitOfWork.SaveChanges();
             

        }

        /// <summary>
        /// Retrieves all courses from the database.
        /// </summary>
        /// <returns>An ActionResult containing a collection of courses.</returns>
        public IEnumerable<Course> GetAll()
        {
            return _db.GetAll();
        }

        /// <summary>
        /// Retrieves the details of a specific course from the database based on its ID.
        /// </summary>
        /// <param name="id">The ID of the course to retrieve.</param>
        /// <returns>An ActionResult containing the details of the course.</returns>
        public dynamic GetDetailCourse(int id)
        {
           
            var course = _db.Find(u=>u.Id == id).Select(c => new
            {
                c.Id,
                c.Decription,
                c.Price,
                c.Code
            }).FirstOrDefault();
           
            return course;
        }
        /// <summary>
        /// Updates the details of a specific course in the database based on its ID.
        /// </summary>
        /// <param name="id"> ID of the course to update.</param>
        /// <param name="course"> updated course object.</param>
        /// <returns>An IActionResult</returns>
        public Course? UpdateCourse(int id, Course course)
        {
            var coursedb = _db.Find(u => u.Id == id).FirstOrDefault();

            try
            {
                coursedb.Price = course.Price;
                coursedb.Decription = course.Decription;
                coursedb.Code = course.Code;
                _unitOfWork.SaveChanges();
                return coursedb;
            }
            catch (Exception)
            {

                return null;
            }
          
        }

        /// <summary>
        /// Updates multiple courses in the database.
        /// </summary>
        /// <param name="courses">
        /// The array of Course objects to update.
        /// </param>
        /// <returns>Generates and returns a SQL query to update the specified courses.</returns>
        public string UpdateRangeAny(Course[] courses)
        {

            List<(int id, Dictionary<string, dynamic> data)> items = new List<(int id, Dictionary<string, dynamic> data)>();

            for (int i = 0; i < courses.Count(); i++)
            {
                Course e = courses[i];

                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();

                foreach (var property in typeof(Course).GetProperties())
                {
                    data[property.Name] = property.GetValue(e);
                }

                items.Add((e.Id, data));
            }

           
           

            string[] nameProperties = typeof(Course).GetProperties().Select(e=> e.Name).ToArray();

            string sqlFinnal = "";

            string sqlHeader = "UPDATE courses SET ";
            string sqlbody = "";
            string sqlfooter = " WHERE courses.Id IN ( ";

            for (int k = 0; k < nameProperties.Count();k++)
            {
                if (nameProperties[k].ToLower() == "id") continue;

                sqlbody += nameProperties[k] + " = ";

                string sql = "CASE ";
                for (int i = 0; i < items.Count; i++)
                {

                    var item = items[i];

                    if (item.data.TryGetValue(nameProperties[k], out dynamic value))
                    {
                        
                            sql += $"WHEN id = {item.id} THEN '{value}'  ";
                    }
                    if (i == items.Count - 1)
                    {
                        sql += $"ELSE {nameProperties[k]} ";
                    }

                }
                sql += "END,";
                sqlbody += sql;
            }

            sqlbody = sqlbody.Substring(0, sqlbody.Length - 1);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                sqlfooter += $" {item.id} ,";
            }
            sqlfooter = sqlfooter.Substring(0, sqlfooter.Length - 1);
            sqlfooter += " );";
            sqlFinnal = sqlHeader + sqlbody + sqlfooter;
            return _db.RunSqlRaw(sqlFinnal).ToString();
           
        }
     
      /*  public bool UpdateRangeAny(Course[] courses)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                foreach (var course in courses)
                {
                    _db.UpdateSQLRaw(course);
                }
                _unitOfWork.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return false;
            }

        }*/
        public dynamic test()
        {
            var listCourstId =  _db2.GetAll().Select(c => c.CouresId);

            var jointb = from c in _db.GetAll()
                         where listCourstId.Contains(c.Id)
                         select c;
                        
            
            return jointb;
        }

        public bool UpdateRange(Course[] course)
        {
            return true;
        }

       
    }
}
