﻿using EnrollmentService.Interface;
using EnrollmentService.Unit;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;
using System.Text;

namespace Course_service.Controllers
{
    [Route("enrollments")]
    public class EnrollmentsController : Controller
    {
        private readonly IEnrollment _db;
        public EnrollmentsController(IEnrollment enrollment)
        {
            _db = enrollment;
        }
        [HttpGet]
        public ActionResult<IEnumerable<Enrollment>> GetAllErollments()
        {
            return _db.GetAllErollments();
        }
        [HttpPost]
        public async Task<ActionResult> AddEnrollment([FromBody] Request request )
        {
            return await _db.AddEnrollment(request);
        }

        [HttpPut]
        public async Task<ActionResult> removeEnrollment([FromBody] Request request)
        {
            return await _db.removeEnrollment(request);
        }

    }
    
}
