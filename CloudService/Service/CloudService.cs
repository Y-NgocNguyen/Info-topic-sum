using CloudService.Interface;
using CloudService.model;
using CourseService.Interface;
using CsvHelper;
using CsvHelper.Configuration;
using EnrollmentService.Interface;
using EnrollmentService.Unit;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using sharedservice.Models;
using sharedservice.Repository;
using sharedservice.UnitofWork;
using System.Globalization;
using ClosedXML.Excel;

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

        private readonly string newFolder;
        private readonly string failedFolder;
        private readonly string compelteFolder;
        private readonly string exportFolderCSV;
        private readonly string exportFolderEx;
        public CloudS(IUnitOfWork unitOfWork, IConfiguration configuration, ICourseService courseService, IEnrollment enrollment)
        {
            _unitOfWork = unitOfWork;
            _genericFile = _unitOfWork.GetRepository<MyFile>();

            googleCredential = GoogleCredential.GetApplicationDefault();
            storageClient = StorageClient.Create(googleCredential);
            bucketName = configuration["GoogleCloudStorageBucket"];

            _enrollmentService = enrollment;
            _courseService = courseService;

            newFolder = configuration["GCS:folderStorage:newFolder"];
            failedFolder = configuration["GCS:folderStorage:failedFolder"];
            compelteFolder = configuration["GCS:folderStorage:compelteFolder"];
            exportFolderCSV = "course/export/csv/";
            exportFolderEx = "course/export/excel/";

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
            string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Datadownload");
            string localFilePath = Path.Combine(downloadDirectory, nameFile);
            string fileNameForStorage = $"{newFolder}{nameFile}";

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            using (var fileStream = File.Create(localFilePath))
            {
                await storageClient.DownloadObjectAsync(bucketName, fileNameForStorage, fileStream);
            }

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
           
            
            string? code = IsValidFileName(fileName);

            if (code == null)
            {
                return "file name invalid";
            }

            //check file duplicate in GCS

            if (await CheckDuplicate($"Y/course/new/{fileName}"))
                return "file duplicate";

            

            //get file from gcs
             string localFilePath = await DownLoadFileFromGCS(fileName);

            try
            {
                if (await ImportCsvToDataBaseUseCsvHelper(localFilePath, fileName, code) == null)
                {
                    return null;
                }
            }
            catch (Exception)
            {

                File.Delete(localFilePath);
                await movieFileInGCS($"{newFolder}{fileName}", $"{failedFolder}{fileName}");
                return null;
               
            }


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
            var duplicate = new List<string>();
            var listobject = storageClient.ListObjects(bucketName, "Y/Course/");
            bool filetocheck = false;
            foreach (var storageObject in listobject)
            {
                if (duplicate.Contains(fileName))
                {
                    return true;
                }
                if (storageObject.Name == $"Viet/Course/new/{fileName}" && filetocheck == false)
                {
                    filetocheck = true;
                    continue;
                }
                if (Path.GetFileName(storageObject.Name) != "")
                    duplicate.Add(Path.GetFileName(storageObject.Name));
            }
            return false;
        }
        public bool IsHeaderValid(string[] record)
        {
            string[] expectedFields = { "CourseCode", "UserId", "IsEnroll", "EnrollDate" };

            return expectedFields.SequenceEqual(record);
        }

        public bool IsRecordValid(CsvRecord record, string code)
        {
            return record.CourseCode == code;
        }



        public async Task<dynamic> ImportCsvToDataBase(string localFilePath, string fileName, string code)
        {
            

            using (var reader = new StreamReader(localFilePath))
            {
                var list = new List<string>();
                var httpClient = new HttpClient();
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
                            await movieFileInGCS($"{newFolder}{fileName}", $"{failedFolder}{fileName}");
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
                    var request = new Request() { uId = values[1], cId = idCourst };
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
                await movieFileInGCS($"{newFolder}{fileName}", $"{compelteFolder}{fileName}");
                return list;
            }
        }
        public async Task<dynamic> ImportCsvToDataBaseUseCsvHelper(string localFilePath, string fileName, string code)
        {
            var list = new List<string>();
            var httpClient = new HttpClient();

            using (var reader = new StreamReader(localFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, Delimiter = ";" }))
            {
                csv.Context.RegisterClassMap<CsvRecordMap>();
                await csv.ReadAsync();


                if (csv.Configuration.Delimiter != ";" || !csv.ReadHeader() || !IsHeaderValid(csv.HeaderRecord))
                {
                    await movieFileInGCS($"{newFolder}{fileName}", $"{failedFolder}{fileName}");
                    return null;
                }
                
                while (await csv.ReadAsync())
                {
                    var record = csv.GetRecord<CsvRecord>();
                   
                    if (record.GetType().GetProperties().Length != 4)
                    {
                        await movieFileInGCS($"{newFolder}{fileName}", $"{failedFolder}{fileName}");
                        return null;
                    }
                    if (!IsRecordValid(record, code))
                    {
                        continue;
                    }


                    var response = await httpClient.GetAsync($"https://localhost:7286/api/Authenticate/checkUser/{record.UserId}");

                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }
                    string userId = record.UserId;

                    if (list.Contains(userId))
                    {
                        continue;
                    }

                    list.Add(userId);

                    if (record.IsEnroll.HasValue && !string.IsNullOrEmpty(record.IsEnroll.ToString()))
                    {
                        int courseId = _courseService.getCourstByCode(code).Id;
                        var request = new Request() { uId = userId, cId = courseId };

                        if (!record.IsEnroll.Value)
                        {
                            await _enrollmentService.removeEnrollment(request);
                        }
                        else
                        {
                            DateTime enrollDate = record.EnrollDate ?? DateTime.Now;

                            await _enrollmentService.AddEnrollment(request, enrollDate);
                        }
                    }
                }
            }

            await movieFileInGCS($"{newFolder}{fileName}", $"{compelteFolder}{fileName}");
            return list;
        }

        //export csv and excel
        public  async Task<dynamic> ExportEnRollMentToCSV()
        {
              
            IQueryable<Enrollment> listEnroll =  _enrollmentService.GetAllErollments();
            IQueryable<Course> listCourse =  _courseService.GetAll();

           
            var list = from enroll in listEnroll
                       join course in listCourse on enroll.CouresId equals course.Id 
                       where enroll.EnrolledDate.AddMonths(3) >= DateTime.Now
                       select new
                       {
                           CourseCode = course.Code,
                           UserId = enroll.UserId??"",
                           IsEnroll = true,
                           EnrollDate = enroll.EnrolledDate,
                       };

            DateTime date = DateTime.Now;
            string nameCSV = $"Export_Backup_Course_{date.Year}_{date.Month}_{date.Day}.csv";
            string nameExcel = $"Export_Backup_Course_{date.Year}_{date.Month}_{date.Day}.xlsx";


            Task exportCsvTask = ExportCSV(list, nameCSV);
            Task exportExcelTask = ExportExcel(list, nameExcel);
            await Task.WhenAll(exportCsvTask, exportExcelTask);
            return list;
           
        }
        //export csv
        public async Task  ExportCSV(dynamic list, string nameCSV)
        {
           

            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(nameCSV))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(list);
                writer.Flush();
                memoryStream.Position = 0;

                var file = new FormFile(memoryStream, 0, memoryStream.Length, "file", nameCSV);

                await UploadFileAsync(file, $"{exportFolderCSV}{nameCSV}");

            }
            File.Delete(nameCSV);
        }
        //export excel
        public async Task ExportExcel(dynamic list,string nameExcel)
        {
              using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Course");
                //add filter
                worksheet.Range("A1:D1").SetAutoFilter();
                //add header name
                worksheet.Cell("A1").Value = "CourseCode";
                worksheet.Cell("B1").Value = "UserId";
                worksheet.Cell("C1").Value = "IsEnroll";
                worksheet.Cell("D1").Value = "EnrollDate";

                //add data
                int i = 2;
                foreach (var item in list)
                {
                    worksheet.Cell($"A{i}").Value = item.CourseCode;
                    worksheet.Cell($"B{i}").Value = item.UserId;
                    worksheet.Cell($"C{i}").Value = item.IsEnroll;
                    worksheet.Cell($"D{i}").Value = item.EnrollDate;
                    i++;
                }

                
                workbook.SaveAs(nameExcel);
                var file = new FormFile(new MemoryStream(), 0, 0, "file", nameExcel);
                await UploadFileAsync(file, $"{exportFolderEx}{nameExcel}");
            }
            File.Delete(nameExcel);
        }



    }
}
