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
        //create IactionResult upload to GCS
        public async Task<IActionResult> UpLoadToGCS([FromForm]MyFile imageFile)
        {
           MyFile resualtFile = await _cloudService.UploadFileToGCS(imageFile.Files);
            return Ok(resualtFile);

        }
        [HttpGet("fileName")]
        public async Task<IActionResult> ImportFile(string fileName)
        {
            return Ok(await _cloudService.ImportFile(fileName));
        }


        [HttpDelete("fileNameForStorage")]
        //create IactionResult delete file
        public async Task<IActionResult> DeleteFile(string fileNameForStorage)
        {
           
            return await _cloudService.DeleteFile(fileNameForStorage) ? Ok() : NotFound();
        }
        [HttpGet("ExportFile")]
        public async Task<IActionResult> ExportFile()
        {
            return Ok( await _cloudService.ExportEnRollMentToCSV());
        }
    }
}
