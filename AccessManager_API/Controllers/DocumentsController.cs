using Microsoft.AspNetCore.Http;
using MyHub.Models;
using MyHub.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Cors;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DocumentsController : Controller
    {
        // GET: Templates
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetPulseList(string profile, string user, string staffid, string year, string month, string days)
        {
            PulseFilesResponse response = new PulseFilesResponse();
            //response.fileNames.Add("eugene");
            string path = Path.Combine(Server.MapPath("~/data/pulse/" + year + "/")) + "index.txt";
            if (System.IO.File.Exists(path))
            {
                string readText = System.IO.File.ReadAllText(path);
                char[] delimiterChars = { ',' };

                string[] fileNamesArray = readText.Split(delimiterChars);

                response.fileNames = fileNamesArray
               .Select(fileName => new { Name = fileName, Month = GetMonth(fileName) })
               .OrderByDescending(item => item.Month)
               .Select(item => item.Name)
               .ToList();

                path = Path.Combine(Server.MapPath("~/data/pulse/"));
                //string[] folders = Directory.GetDirectories(path);

                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                // Get the names of all folders in the directory
                string[] folderNames = directoryInfo.GetDirectories()
                    .Select(dir => dir.Name).OrderByDescending(name => name)
                    .ToArray();


                response.folderNames = folderNames;
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }


        private static int GetMonth(string fileName)
        {
            // Example: Assuming file names are in the format "filename_MM.txt"
            string monthPart = fileName.Split(' ').FirstOrDefault(); // Extract month part
            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            int monthIndex = Array.IndexOf(months, monthPart);

            // Print the month index
            if (monthIndex != -1)
            {
                return monthIndex;
            }
            else
            {
                return -1;
            }
        }

        [HttpGet]
        public FileResult GetPulsePDF(string filename, string user, string staffid, string year, string month, string days)
        {

            return File("../data/pulse/"+year+"/"+filename, "application/pdf");
            //return Json(response, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public FileResult GetDocumentPDF(string filename, string user, string staffid, string year, string month, string days)
        {

            return File("../data/documents/" + filename, "application/pdf");
            //return Json(response, JsonRequestBehavior.AllowGet);
        }

        //Testing Starts by Sivaguru M CHC1704 on 10-04-2024
        [HttpGet]
        public ActionResult GetPulseListMain(string profile, string user, string staffid, string year, string month, string days)
        {
            PulseFilesResponse response = new PulseFilesResponse();
            //response.fileNames.Add("eugene");
            string path = Path.Combine(Server.MapPath("~/data/pulse/")) + "index.txt";
            if (System.IO.File.Exists(path))
            {
                string readText = System.IO.File.ReadAllText(path);
                char[] delimiterChars = { ',' };
                response.fileNames = readText.Split(delimiterChars).ToList();
            }
            /* from files
            DirectoryInfo d = new DirectoryInfo(Path.Combine(Server.MapPath("~/data/pulse/")));
            FileInfo[] Files = d.GetFiles("*.png");

            foreach (FileInfo file in Files)
            {
                response.fileNames.Add(file.Name);
            }
            */
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public FileResult GetPulsePDFMain(string filename, string user, string staffid, string year, string month, string days)
        {

            return File("../data/pulse/" + filename, "application/pdf");
            //return Json(response, JsonRequestBehavior.AllowGet);
        }
        //Testing Ends

        #region Created by Periya Samy P CHC1761 on 29-04-2024
        [HttpPost]
        public ActionResult PulseUploadFile()
        {
            try
            {
                var postResponse = new PostResponse();
                

                HttpPostedFileBase file = Request.Files["pdf"];
                HttpPostedFileBase img = Request.Files["image"];
                string dateString = Request.Form["date"];

                DateTime date = DateTime.ParseExact(dateString, "yyyy-MM", null);
                string formattedDate = date.ToString("yyyy-MMM");
                string Year = formattedDate.Split('-')[0];

                string FileName = formattedDate.Split('-')[1]+" " +
                                Year.Substring(Year.Length - 2); 


                var ab = FileUpload.InsertImage(file, Year, FileName, 1);
                var bc = FileUpload.InsertImage(img, Year, FileName, 2);

                postResponse.status = true;
                postResponse.result = "Uploaded";
                return Json(postResponse, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {
                var Message = e.Message;
            }

            return null;
        }
        #endregion


    }
}