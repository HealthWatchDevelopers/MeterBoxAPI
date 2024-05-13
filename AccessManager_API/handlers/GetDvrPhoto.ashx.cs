using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyHub
{
    /// <summary>
    /// Summary description for GetDvrPhoto
    /// </summary>
    public class GetDvrPhoto : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string staffid = context.Request.QueryString.Get("staffid");
            string imei = context.Request.QueryString.Get("imei");

            string sFileName = staffid + ".jpg";// GetProfileImageName(sProfile, sUsername);
            //string sFileName =  GetProfileImageName(staffid, imei);
            var mimeType = MimeMapping.GetMimeMapping(sFileName);
            context.Response.ContentType = mimeType;

            String sFilePath = context.Server.MapPath("~") + "data\\dvrphotos\\" + sFileName;//Actual working path

            //String sFilePath = context.Server.MapPath("C:/inetpub/wwwroot/meterboxAPITest/data/dvrphotos/") + sFileName;

            if (System.IO.File.Exists(sFilePath))
            {
                context.Response.WriteFile(sFilePath);
            }
            else
            {
                context.Response.WriteFile(context.Server.MapPath("~") + "data\\dvrphotos\\dummy.jpg");
            }
        }
        /*
        private string GetProfileImageName(string staffid,string imei)
        {
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                }
            }
            catch (MySqlException ex)
            {
                onLoadResponse.result = "Error-" + ex.Message;
            }
        }
        */
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}