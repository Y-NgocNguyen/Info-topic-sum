
using CsvHelper.Configuration;
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
    public sealed class CsvRecordMap : ClassMap<CsvRecord>
    {
        public CsvRecordMap()
        {
            Map(m => m.CourseCode).Name("CourseCode");
            Map(m => m.UserId).Name("UserId");
            Map(m => m.IsEnroll).Name("IsEnroll");
            Map(m => m.EnrollDate).Name("EnrollDate");
        }
    }

}
