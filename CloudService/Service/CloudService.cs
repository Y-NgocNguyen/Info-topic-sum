using CloudService.Interface;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using sharedservice.Models;
using sharedservice.Repository;
using sharedservice.UnitofWork;


namespace CloudService.Service
{
    public class CloudS : ICloud
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<MyFile> _genericFile;

        private readonly GoogleCredential googleCredential;
        private readonly StorageClient storageClient;
        private readonly string bucketName;

        public ILogger<CloudS> Logger { get; set; }

        public CloudS(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _genericFile = _unitOfWork.GetRepository<MyFile>();

            googleCredential = GoogleCredential.GetApplicationDefault();
            storageClient = StorageClient.Create(googleCredential);
            bucketName = configuration["GoogleCloudStorageBucket"];

            Logger = NullLogger<CloudS>.Instance;
        }
        /// <summary>
        /// Upload File to GCS
        /// </summary>
        /// <param name="imageFile">File Upload</param>
        /// <param name="fileNameForStorage">File name in google Cloud</param>
        /// <returns></returns>
      
        public async Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage)
        {
            string folderName = "Y";
            string fullFilePath = $"{folderName}/{fileNameForStorage}";

            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                var dataObject = await storageClient.UploadObjectAsync(bucketName, fullFilePath, null, memoryStream);
                return dataObject.MediaLink;
            }
            
        }

        /// <summary>
        /// generate file name for storage
        /// </summary>
        /// <param name="fileName">file name gennerate</param>
        /// <returns></returns>
        private static string FormFileName(string fileName)
        {
            var fileExtension = Path.GetExtension(fileName);
            var fileNameForStorage = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}{fileExtension}";
            return fileNameForStorage;
        }


        /// <summary>
        /// Deletes a file from GCS.
        /// </summary>
        /// <param name="fileNameForStorage">The name of the file in the storage.</param>

        public async Task DeleteFileAsync(string fileNameForStorage)
        {
            await storageClient.DeleteObjectAsync(bucketName, fileNameForStorage);
        }


        /// <summary>
        /// get all file in database
        /// </summary>
        /// <returns></returns>

        public IEnumerable<MyFile> getAllFile()
        {
           
            return _genericFile.GetAll().ToList();
        }
        /// <summary>
        /// Upload file to GCS
        /// </summary>
        /// <param name="imageFile">Image file upload </param>
        /// <returns></returns>

        //function upload file to gcs
        public async Task<MyFile> UploadFileToGCS(IFormFile imageFile)
        {

            string fileNameForStorage = FormFileName(imageFile.FileName);

            var ImageUrl = await UploadFileAsync(imageFile, fileNameForStorage);

            var temp = new MyFile() { Name = fileNameForStorage, Image = ImageUrl };
            _genericFile.Add(temp);
            _unitOfWork.SaveChanges();
            return temp;
        }
        /// <summary>
        /// DownLoad file from GCS
        /// </summary>
        /// <param name="nameFile">the name of file in the storage</param>
        /// <returns></returns>
        public async Task DownLoadFileFromGCS(string nameFile)
        {
            string localFilePath = $"D:\\code\\c#\\Info-topic\\CloudService\\Datadownload\\{nameFile}";

            var fileNameForStorage = $"Y/{nameFile}";
            var fileStream = File.Create(localFilePath);
            await storageClient.DownloadObjectAsync(bucketName, fileNameForStorage, fileStream);
            fileStream.Close();
        }
        

        //create function delete file to gcs
        public async Task<bool> DeleteFile(string fileNameForStorage)
        {
            try
            {
                string folderName = "Y";
                string fullFilePath = $"{folderName}/{fileNameForStorage}";
                await storageClient.DeleteObjectAsync(bucketName, fullFilePath);
                _genericFile.Remove(_genericFile.Find(x => x.Name == fileNameForStorage).FirstOrDefault());
                _unitOfWork.SaveChanges();
                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {

                return false;
            }

        }
    }
}
