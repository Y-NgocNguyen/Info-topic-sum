using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace sharedservice.Models
{
    public partial class Course
    {
        public Course()
        {
            Enrollments = new HashSet<Enrollment>();
        }
        
        public int Id { get; set; }
     
        public string Code { get; set; } = null!;
        public float? Price { get; set; }
        public string? Decription { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
