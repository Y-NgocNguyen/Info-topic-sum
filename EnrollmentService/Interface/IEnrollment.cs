using EnrollmentService.Unit;

using sharedservice.Models;


namespace EnrollmentService.Interface
{
    public interface IEnrollment
    {
        IEnumerable<Enrollment> GetAllErollments();
        Task<Enrollment> AddEnrollment(Request request);
        Task removeEnrollment(Request request);
    }
}
