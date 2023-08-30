using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace sharedservice.Models
{
    public partial class MyFile
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string? Image { get; set; }

        [NotMapped]
        public IFormFile Files { get; set; }
    }
}