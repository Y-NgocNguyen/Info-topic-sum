using EnrollmentService.Unit;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;


namespace EnrollmentService.Interface
{
    public interface IEnrollment
    {
        ActionResult<IEnumerable<Enrollment>> GetAllErollments();
        Task<ActionResult> AddEnrollment(Request request);
        Task<ActionResult> removeEnrollment(Request request);
    }
}
