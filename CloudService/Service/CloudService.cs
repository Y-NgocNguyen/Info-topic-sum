using CloudService.Interface;
using CloudService.model;
using CourseService.Interface;
using EnrollmentService.Interface;
using EnrollmentService.Unit;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using sharedservice.Models;
using sharedservice.Repository;
using sharedservice.UnitofWork;
using System.Globalization;

namespace CloudService.Service
{
    public class CloudS : ICloud
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<MyFile> _genericFile;

        private readonly GoogleCredential googleCredential;
        private readonly StorageClient storageClient;
        private readonly string bucketName;

        private readonly IEnrollment _enrollmentService;
        private readonly ICourseService _courseService;



        public CloudS(IUnitOfWork unitOfWork, IConfiguration configuration, ICourseService courseService, IEnrollment enrollment)
        {
            _unitOfWork = unitOfWork;
            _genericFile = _unitOfWork.GetRepository<MyFile>();

            googleCredential = GoogleCredential.GetApplicationDefault();
            storageClient = StorageClient.Create(googleCredential);
            bucketName = configuration["GoogleCloudStorageBucket"];

            _enrollmentService = enrollment;
            _courseService = courseService;


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

            return _genericFile.GetAll();
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
        public async Task<string> DownLoadFileFromGCS(string nameFile)
        {
            string localFilePath = $"D:\\code\\c#\\Info-topic\\CloudService\\Datadownload\\{nameFile}";

            var fileNameForStorage = $"Y/course/new/{nameFile}";
            var fileStream = File.Create(localFilePath);
            await storageClient.DownloadObjectAsync(bucketName, fileNameForStorage, fileStream);
            fileStream.Close();
            return localFilePath;
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


        public async Task<dynamic> ImportFile(string urlFile)
        {
         
            string fileName = Path.GetFileName(urlFile);

            //check file fomat RegisterCourse_<Course_code>_yyyy_mm_dd.csv
           
            
            string code = IsValidFileName(fileName);

            if (code == null)
            {
                return "file name invalid";
            }

            //check file duplicate in GCS

            if (await CheckDuplicate($"Y/course/new/{fileName}"))
                return "file duplicate";

            

            //get file from gcs
             string localFilePath = await DownLoadFileFromGCS(fileName);

            if (await ImportCsvToDataBase(localFilePath, fileName, code) == null)
            {
                return null;
            }

            File.Delete(localFilePath);


            /* await movieFileInGCS($"Y/course/new/{fileName}", $"Y/course/failed/{fileName}");*/

            return "";

    
        }
        //move file to orther folder 
        public async Task movieFileInGCS(string source,string dest)
        {
            storageClient.CopyObject(bucketName, source, bucketName, dest);
            storageClient.DeleteObject(bucketName, source);
        }
        //is valid file name
        public string? IsValidFileName(string fileName)
        {
            //check file name start with RegisterCourse_ and end with .csv
            if (!fileName.StartsWith("RegisterCourse_") || !fileName.EndsWith(".csv"))
            {
                return null;
            }

            
            //split file name to get course code and date
            string[] parts = fileName.Split('_');
            if (parts.Length != 5)
            {
                return null;
            }
           
            string courseCode = parts[1];
            
            string dateString = $"{parts[2]}_{parts[4].Split('.')[0]}_{parts[3]}";
            DateTime date;

            //check date format yyyy_mm_dd
            if (!DateTime.TryParseExact(dateString, "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return null;
            }
           
           //check course code exist in database
            if(_courseService.getCourstByCode(courseCode) ==  null)
            {
                return null;
            }


            return courseCode;
        }

        //creater function check file duplicate in GCS
        public async Task<bool> CheckDuplicate(string fileName)
        {
            List<string> duplicate = new List<string>();
            var listobject = storageClient.ListObjects(bucketName, "Y/course/");
            bool filetocheck = false;
            foreach (var storageObject in listobject)
            {
                if (duplicate.Contains(fileName))
                {
                    return true;
                }
                if (storageObject.Name == $"Y/course/new/{fileName}" && filetocheck == false)
                {
                    filetocheck = true;
                    continue;
                }
                if (Path.GetFileName(storageObject.Name) != "")
                    duplicate.Add(Path.GetFileName(storageObject.Name));
            }
            return false;
        }

        public async Task<dynamic> ImportCsvToDataBase(string localFilePath,string fileName,string code)
        {
            using (var reader = new StreamReader(localFilePath))
            {
                List<string> list = new List<string>();
                HttpClient httpClient = new HttpClient();
                bool isFirstLine = true;

                while (reader.Peek() >= 0)
                {
                   

                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    //check header file
                    if (isFirstLine)
                    {
                        isFirstLine = false;


                        if (values[0] != "CourseCode" || values[1] != "UserId" || values[2] != "IsEnroll" || values[3] != "EnrollDate")
                        {
                            //if header file invalid then move file to folder failed
                            await movieFileInGCS($"Y/course/new/{fileName}", $"Y/course/failed/{fileName}");
                            return null;
                        }

                        continue; // Skip the header row
                    }
                    
                    if (values.Length != 4)
                    {
                        continue;
                    }

                    if (values[0] != code)
                    {
                        continue;
                    }   

                    //check user exist in user service

                    var response = await httpClient.GetAsync($"https://localhost:7286/api/Authenticate/checkUser/{values[1]}");

                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }


                    //if  CourseCode + userId duplicate in file then continue

                    if (list.Contains(values[1]))
                    {
                        continue;
                    }

                    //add userId to list
                    string temp = values[1];

                    list.Add(temp);

                    //if values[2] == null => continue
                    if (values[2] == "")
                    {
                        continue;
                    }

                    int idCourst = _courseService.getCourstByCode(code).Id;
                    Request request = new Request() { uId = values[1], cId = idCourst };
                    if (values[2] == "false")
                    {
                        //call function delete Enrollment in Enrollment Service
                        await _enrollmentService.removeEnrollment(request);
                    }
                    else
                    {
                        //call function add Enrollment in Enrollment Service
                        DateTime dateTime = values[3] == "" ? DateTime.Now : DateTime.Parse(values[3]);
                        await _enrollmentService.AddEnrollment(request, dateTime);
                    }

                }
                //if compelted => move file to folder completed
                await movieFileInGCS($"Y/course/new/{fileName}", $"Y/course/completed/{fileName}");
                return list;
            }
        }




    }
}
