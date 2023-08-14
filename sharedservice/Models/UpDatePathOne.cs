using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharedservice.Models
{
    public class UpDatePathOne
    {
        public int[] entityIds { get; set; }
        public Dictionary<string, object> columnValues { get; set; }
    }
}
