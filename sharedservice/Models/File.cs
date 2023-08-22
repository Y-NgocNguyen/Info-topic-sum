using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharedservice.Models
{
    public partial class MyFile
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string? Image { get; set; }
        [NotMapped]
        public  IFormFile Files { get; set; }
    }
}
