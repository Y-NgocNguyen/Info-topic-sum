using EnrollmentService.Interface;
using EnrollmentService.Unit;
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
        public async Task<Enrollment> AddEnrollment(Request request)
        {
            // check User exit
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://localhost:7286/api/Authenticate/checkUser/{request.uId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("not found");
            }

            // check unique
            if(_dbEn.Find(e => e.CouresId == request.cId && e.UserId == request.uId).FirstOrDefault() != null)
            {
                throw new Exception("Unique");
            }

            //check course exit
            var responseContent = await response.Content.ReadFromJsonAsync<User>();
            Course course = _dbCou.Find(e=>e.Id == request.cId).FirstOrDefault();

            if(course == null)
            {
                throw new Exception("exting courst");
            }
            //check user's money
            if (responseContent.balanceAccount < course.Price)
            {
                throw new Exception("user not money");
            }

            //call Api sub money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");

            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=t";

            var response2 = await httpClient.PatchAsync(url, content);
            if (!response2.IsSuccessStatusCode)
            {
                throw new Exception(response2.RequestMessage.ToString());
            }

            Enrollment enrollment = new Enrollment()
            {
                EnrolledDate = DateTime.Now,
                CouresId = request.cId,
                UserId = request.uId
              
            };
           
            _dbEn.Add(enrollment);
            _unitOfWork.SaveChanges();
            return enrollment;
        }

        public IEnumerable<Enrollment> GetAllErollments()
        {
            return _dbEn.GetAll();
        }
        /// <summary>
        /// Removes an enrollment for a course based on the provided request.
        /// </summary>
        /// <param name="request">The enrollment request containing the course and user IDs.</param>
        /// <returns>An ActionResult </returns>
        public async Task removeEnrollment(Request request)
        {
            HttpClient httpClient = new HttpClient();

            //check enrollment exit
            Enrollment enrollment = _dbEn.Find(e => e.UserId == request.uId && e.CouresId == request.cId).FirstOrDefault();
            if (enrollment == null)
            {
                throw new Exception("Exing Enrollment");
            }
            // call api add money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");
            
            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=c";
            var response2 = await httpClient.PatchAsync(url, content);

            if (!response2.IsSuccessStatusCode)
            {
               throw new Exception(response2.RequestMessage.ToString());
            }

            _dbEn.Remove(enrollment);
            
            _unitOfWork.SaveChanges();

           
        }
    }
}
