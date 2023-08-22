using CloudService.Interface;
using Course_service.Filter;
using Microsoft.AspNetCore.Mvc;
using sharedservice.Models;

namespace Course_service.Controllers
{
    [Route("Cloud")]
    public class CloudController : Controller
    {
        private readonly ICloud _cloudService;
        public CloudController(ICloud cloud)
        {
            _cloudService = cloud;
        }
        [HttpGet]
        public IActionResult GetAllFile()
        {
            
            return Ok(_cloudService.getAllFile());
        }
        [HttpPost]
        [CheckFileFilter]
        //create IactionResult uload to GCS
        public async Task<IActionResult> UploadtoGCS([FromForm]MyFile imageFile)
        {
           
            return Ok (await _cloudService.UploadFileToGCS(imageFile.Files));
        }
        [HttpDelete("fileNameForStorage")]
        //create IactionResult delete file
        public async Task<IActionResult> DeleteFile(string fileNameForStorage)
        {
           
            return await _cloudService.DeleteFile(fileNameForStorage) ? Ok() : NotFound();
        }

        [HttpGet("nameFile")]
        public async Task<IActionResult> DownloadFile(string nameFile)
        {
            await _cloudService.DownLoadFileFromGCS(nameFile);
            return Ok();
        }
    }
}
