using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyHub.Service
{
    public class FileUpload
    {
        internal static object InsertImage(HttpPostedFileBase pro_img, string year, string FileName, int fileno)
        {
            
            if (pro_img != null)
            {
                var folderName = Path.Combine("data", @"pulse", year);
                var filePath = HttpContext.Current.Server.MapPath("~");
                var fullPath = Path.Combine(filePath, folderName);

                UploadFile(fullPath, FileName, pro_img);

                // If fileno is 2, call ModifyIndexFile
                if (fileno == 2)
                {
                    ModifyIndexFile(Path.Combine(filePath, folderName), FileName);
                }
            }
            return "Image Uploaded";
        }


        internal static void UploadFile(string filePath,string FileName,HttpPostedFileBase file)
        {
            //var fullPath = Path.Combine(filePath, FileName);
            FileName = FileName.Replace("/", "")
                            .Replace(":", "");
            string extension = System.IO.Path.GetExtension(file.FileName);
            string docName = FileName + extension;
            var fileName = docName;

            // Check if the directory exists, if not, create it
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            // Combine the directory path with the file name
            filePath = Path.Combine(filePath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.InputStream.CopyTo(stream);
            }

            // Dispose the stream after copying
            file.InputStream.Dispose();
        }


        internal static void ModifyIndexFile(string filepath, string additionalFileName)
        {
            var indexPath = Path.Combine(filepath, "index.txt");

            try
            {
                // Check if the file exists, if not, create it
                if (!File.Exists(indexPath))
                {
                    File.Create(indexPath).Close();
                }
                string content = File.ReadAllText(indexPath);
                if (!content.Contains(additionalFileName))
                {
                    content += (string.IsNullOrEmpty(content) ? "" : ",") + additionalFileName;
                    File.WriteAllText(indexPath, content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}