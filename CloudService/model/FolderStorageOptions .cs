using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudService.model
{
    public class FolderStorageOptions
    {
        public string NewFolder { get; set; }
        public string FailedFolder { get; set; }
        public string CompletedFolder { get; set; }
        public string ExportFolderCSV { get; set; }
        public string ExportFolderExcel { get; set; }
    }
}
