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
            var docName = "";
            if (pro_img != null)
            {
                var folderName = Path.Combine("data", @"pulse", year);
                var filePath = HttpContext.Current.Server.MapPath("~");

                string extension = System.IO.Path.GetExtension(pro_img.FileName);
                docName = FileName + extension;
                var fileName = docName;
                var fullPath = Path.Combine(filePath, folderName, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    pro_img.InputStream.CopyTo(stream);
                    stream.Dispose();
                }
                if (fileno == 2)
                {
                    ModifyIndexFile(filePath + folderName, docName);
                }
            }
            return "Image Uploaded";
        }

        internal static void ModifyIndexFile(string filepath, string additionalFileName)
        {
            var indexPath = Path.Combine(filepath, "index.txt");

            try
            {
                string content = File.ReadAllText(indexPath);
                if (!content.Contains(additionalFileName))
                {
                    content += "," + additionalFileName;
                }
                File.WriteAllText(indexPath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}