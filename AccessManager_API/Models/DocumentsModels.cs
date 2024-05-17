using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace MyHub.Models
{
    public class PulseFilesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<string> fileNames { get; set; }
        public string[] folderNames { get; set; }
        public PulseFilesResponse()
        {
            status = false;
            result = "";
            fileNames = new List<string>();
            folderNames =null;   
        }
    }

    public class PulseFileUpload
    {
        public string Date { get; set; }
    }

}