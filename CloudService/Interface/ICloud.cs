using Microsoft.AspNetCore.Http;
using sharedservice.Models;

namespace CloudService.Interface
{
    public interface ICloud
    {

      
        Task<MyFile> UploadFileToGCS(IFormFile imageFile);
        Task<bool> DeleteFile(string fileNameForStorage);
       
        IEnumerable<MyFile> getAllFile();
      
        Task<dynamic> ImportFile(string urlFile);
       
        Task<dynamic> ExportEnRollMentToCSV();
    }
}
