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
        public async Task<Enrollment> AddEnrollment(Request request, DateTime dateTime)
        {
            // check User exit
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://localhost:7286/api/Authenticate/checkUser/{request.uId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
                /* return new HttpResponseMessage(HttpStatusCode.NotFound)
                 {
                     ReasonPhrase = "User not found"
                 };*/
            }

            // check unique
            var temp = _dbEn.Find(e => e.CouresId == request.cId && e.UserId == request.uId).FirstOrDefault();
            if (temp != null)
            {
                var content2 = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");

                string url2 = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=t";

                var response3 = await httpClient.PatchAsync(url2, content2);
                if (!response3.IsSuccessStatusCode)
                {
                    return null;
                }

                temp.EnrolledDate = DateTime.Now;
                _unitOfWork.SaveChanges();
                return temp;
            }

            //check course exit
            var responseContent = await response.Content.ReadFromJsonAsync<User>();
            Course? course = _dbCou.Find(e => e.Id == request.cId).FirstOrDefault();

            if (course == null)
            {
                return null;
                /* return new HttpResponseMessage(HttpStatusCode.NotFound)
                 {
                     ReasonPhrase = "exting courst"
                 };*/
            }
            //check user's money
            if (responseContent.balanceAccount < course.Price)
            {
                return null;
                /* return new HttpResponseMessage(HttpStatusCode.NotFound)
                 {
                     ReasonPhrase = "user not money"
                 };*/
            }

            //call Api sub money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");

            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=t";

            var response2 = await httpClient.PatchAsync(url, content);
            if (!response2.IsSuccessStatusCode)
            {
                return null;
            }

            var enrollment = new Enrollment()
            {
                EnrolledDate = dateTime,
                CouresId = request.cId,
                UserId = request.uId
            };

            _dbEn.Add(enrollment);
            _unitOfWork.SaveChanges();
            return enrollment;
        }

        public IQueryable<Enrollment> GetAllErollments()
        {
            return _dbEn.GetAll();
        }

        /// <summary>
        /// Removes an enrollment for a course based on the provided request.
        /// </summary>
        /// <param name="request">The enrollment request containing the course and user IDs.</param>
        /// <returns>An ActionResult </returns>
        public async Task<bool> removeEnrollment(Request request)
        {
            var httpClient = new HttpClient();

            //check enrollment exit
            Enrollment? enrollment = _dbEn.Find(e => e.UserId == request.uId && e.CouresId == request.cId).FirstOrDefault();
            if (enrollment == null)
            {
                return false;
            }
            // call api add money
            var content = new StringContent(request.cId.ToString(), Encoding.UTF8, "application/json");

            string url = $"https://localhost:7286/api/Authenticate/UserBalance/{request.uId}?d=c";
            var response2 = await httpClient.PatchAsync(url, content);

            if (!response2.IsSuccessStatusCode)
            {
                return false;
            }

            _dbEn.Remove(enrollment);

            _unitOfWork.SaveChanges();
            return true;
        }
    }
}