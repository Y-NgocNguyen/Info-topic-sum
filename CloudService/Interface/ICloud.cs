

using Microsoft.AspNetCore.Http;
using sharedservice.Models;

namespace CloudService.Interface
{
    public interface ICloud
    {

        Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage);
        Task<MyFile> UploadFileToGCS(IFormFile imageFile);
        Task<bool> DeleteFile(string fileNameForStorage);
        Task DeleteFileAsync(string fileNameForStorage);
        IEnumerable<MyFile> getAllFile();
       Task DownLoadFileFromGCS(string nameFile);
    }
}
