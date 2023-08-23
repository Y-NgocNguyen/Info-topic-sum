
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudService.model
{
   
    public class CsvRecord
    {
        public string CourseCode { get; set; }
        public string UserId { get; set; }
        public bool? IsEnroll { get; set; }
        public DateTime? EnrollDate { get; set; }
    }

}
