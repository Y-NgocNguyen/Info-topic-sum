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
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Google.Apis.Logging;

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

        private readonly FolderStorageOptions _folderStorageOptions;
        private readonly GCSConfi _gCSConfi;

        private readonly ILogger<CloudS> _logger;
       
        private List<string> headers;
        public CloudS(IUnitOfWork unitOfWork, IOptions<GCSConfi> _gCSConfi, IOptions<FolderStorageOptions> options, ICourseService courseService, IEnrollment enrollment,ILogger<CloudS> logger)
        {
            _unitOfWork = unitOfWork;
            _genericFile = _unitOfWork.GetRepository<MyFile>();

            googleCredential = GoogleCredential.GetApplicationDefault();
            storageClient = StorageClient.Create(googleCredential);
            bucketName = _gCSConfi.Value.BucketName;

            _enrollmentService = enrollment;
            _courseService = courseService;

           
            _folderStorageOptions = options.Value;
            headers = new List<string> { "CourseCode", "UserId", "IsEnroll", "EnrollDate" };

            _logger = logger;

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
        /// <param name="fileName">file name generate</param>
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
            _logger.LogInformation("Y dang in Log");
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
            string fileNameForStorage = $"{_folderStorageOptions.NewFolder}{nameFile}";

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


        public async Task<dynamic?> ImportFile(string urlFile)
        {
         
            string fileName = Path.GetFileName(urlFile);

            //check file format RegisterCourse_<Course_code>_yyyy_mm_dd.csv
           
            
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
                await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.FailedFolder}{fileName}");
                return null;
               
            }


            return "";

    
        }
        //move file to other folder 
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

        //create function check file duplicate in GCS
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
                            await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.FailedFolder}{fileName}");
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
                //if completed => move file to folder completed
                
                await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.CompletedFolder}{fileName}");
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
                    await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.FailedFolder}{fileName}");
                    return null;
                }
                
                while (await csv.ReadAsync())
                {
                    var record = csv.GetRecord<CsvRecord>();
                   
                    if (record.GetType().GetProperties().Length != 4)
                    {
                        await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.FailedFolder}{fileName}");
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

            await movieFileInGCS($"{_folderStorageOptions.NewFolder}{fileName}", $"{_folderStorageOptions.CompletedFolder}{fileName}");
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

                await UploadFileAsync(file, $"{_folderStorageOptions.ExportFolderCSV}{nameCSV}");

            }
            File.Delete(nameCSV);
        }
        //get column name from number column
        string GetColumnName(int columnNumber)
        {
            StringBuilder columnName = new StringBuilder();
            int dividend = columnNumber;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName.Insert(0, (char)('A' + modulo));
                dividend = (dividend - modulo) / 26;
            }

            return columnName.ToString();
        }
        //get value by property name
        private object GetValueByPropertyName(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj);
        }

        //export excel
        public async Task ExportExcel(dynamic list,string nameExcel)
        {
              using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Course");
                //add filter
                var range = worksheet.Range($"A1:{GetColumnName(headers.Count)}1");
                range.SetAutoFilter();

                //add header name
                for (int i = 0; i < headers.Count; i++)
                {
                    var columnName = GetColumnName(i + 1);
                    worksheet.Cell($"{columnName}1").Value = headers[i];
                }

                //add data
                int row = 2;
                foreach (var item in list)
                {
                    for (int col = 0; col < headers.Count; col++)
                    {
                        var columnName = GetColumnName(col + 1);
                        var value = GetValueByPropertyName(item, headers[col]);
                        worksheet.Cell($"{columnName}{row}").Value = value;
                    }
                    row++;
                }



                workbook.SaveAs(nameExcel);
                var file = new FormFile(new MemoryStream(), 0, 0, "file", nameExcel);
                await UploadFileAsync(file, $"{_folderStorageOptions.ExportFolderCSV}{nameExcel}");
            }
            File.Delete(nameExcel);
        }



    }
}
