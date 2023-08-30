using EnrollmentService.Unit;

using sharedservice.Models;

namespace EnrollmentService.Interface
{
    public interface IEnrollment
    {
        IQueryable<Enrollment> GetAllErollments();

        Task<Enrollment> AddEnrollment(Request request, DateTime dateTime);

        Task<bool> removeEnrollment(Request request);
    }
}