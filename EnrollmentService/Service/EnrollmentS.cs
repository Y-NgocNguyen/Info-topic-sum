using EnrollmentService.Interface;
using EnrollmentService.Unit;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;
using sharedservice.Repository;
using sharedservice.UnitofWork;
using System.Net.Http.Json;
using System.Text;

namespace EnrollmentService.Service
{
    public class EnrollmentS : IEnrollment
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Enrollment> _dbEn;
        private readonly IGenericRepository<Course> _dbCou;

        public EnrollmentS(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _dbEn = _unitOfWork.GetRepository<Enrollment>();
            _dbCou = _unitOfWork.GetRepository<Course>();
        }
        /// <summary>
        /// Adds an enrollment for a course based on the provided request.
        /// </summary>
        /// <param name="request">The enrollment request containing the course and user IDs.</param>
        /// <returns>An ActionResult </returns>
        public async Task<ActionResult> AddEnrollment(Request request)
        {
            // check User exit
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://localhost:7286/api/Authenticate/checkUser/{request.uId}");

            if (!response.IsSuccessStatusCode)
            {
                return new NotFoundResult();
            }

            // check unique
            if(_dbEn.Find(e => e.CouresId == request.cId && e.UserId == request.uId).FirstOrDefault() != null)
            {
                return new ConflictResult();
            }

            //check course exit
            var responseContent = await response.Content.ReadFromJsonAsync<User>();
            Course course = _dbCou.Find(e=>e.Id == request.cId).FirstOrDefault();

            if(course == null)
            {
                return new NotFoundObjectResult("Course exit");
            }
            //check user's money
            if (responseContent.balanceAccount < course.Price)
            {
                return new BadRequestObjectResult("not enough money");
            }

            //call Api sub money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");

            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=t";

            var response2 = await httpClient.PatchAsync(url, content);
            if (!response2.IsSuccessStatusCode)
            {
                return new BadRequestResult();
            }

            Enrollment enrollment = new Enrollment()
            {
                EnrolledDate = DateTime.Now,
                CouresId = request.cId,
                UserId = request.uId,
                Coures = course
            };
           
            _dbEn.Add(enrollment);
            _unitOfWork.SaveChangesAsync();
            return new OkResult();
        }

        public ActionResult<IEnumerable<Enrollment>> GetAllErollments()
        {
            return new OkObjectResult(_dbEn.GetAll());
        }
        /// <summary>
        /// Removes an enrollment for a course based on the provided request.
        /// </summary>
        /// <param name="request">The enrollment request containing the course and user IDs.</param>
        /// <returns>An ActionResult </returns>
        public async Task<ActionResult> removeEnrollment(Request request)
        {
            HttpClient httpClient = new HttpClient();

            //check enrollment exit
            Enrollment enrollment = _dbEn.Find(e => e.UserId == request.uId && e.CouresId == request.cId).FirstOrDefault();
            if (enrollment == null)
            {
                return new NotFoundResult();
            }
            // call api add money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");
            
            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=c";
            var response2 = await httpClient.PatchAsync(url, content);

            if (!response2.IsSuccessStatusCode)
            {
                return new BadRequestResult();
            }

            _dbEn.Remove(enrollment);
            
            _unitOfWork.SaveChangesAsync();

            return new OkResult();
        }
    }
}
