using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using MyHub.Controllers;
using MySql.Data.MySqlClient;
/*
 HealthwatchTeleDiagnostics@gmail.com
 healthwatch@123
 Jan 1 1950/Male
  
 */
namespace MyHub.Hubs
{
    public partial class ChatHub : Hub
    {
        public static void SendEmail_MeterBox(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(mailDoc.m_To);
                mail.From = new MailAddress("Healthwatch Tele Diagnostics <meterbox@chcgroup.in>");
                mail.Subject = mailDoc.m_Subject;
                string Body = mailDoc.m_Body;
                //mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.chcgroup.in";
                smtp.Port = 587;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("meterbox@chcgroup.in", "hwmeter2019$");
                smtp.EnableSsl = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //________________________________________________________
                mail.Body = Body;
                //________________________________________________________
                smtp.Send(mail);
                MyGlobal.Log("Mail Succesully Sent to " + mailDoc.m_To);
            }
            catch (Exception ex)
            {
                MyGlobal.Log("Failed to send Mail to " + mailDoc.m_To);
                //MessageToDebugger("MAIL FAILED-" + ex.Message);
            }
        }
        public static void SendEmail_MeterBox_withgmail(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(mailDoc.m_To);
                mail.From = new MailAddress("Healthwatch Tele Diagnostics <HealthwatchTeleDiagnostics@gmail.com>");
                mail.Subject = mailDoc.m_Subject;
                string Body = mailDoc.m_Body;
                //mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port =  587; // or  587
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("HealthwatchTeleDiagnostics@gmail.com", "healthwatch@123");
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //________________________________________________________
                /*
                var contentID = "Image";
                var inlineLogo = new Attachment(@"c:\temp\icon256.png");
                inlineLogo.ContentId = contentID;
                inlineLogo.ContentDisposition.Inline = true;
                inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                mail.Attachments.Add(inlineLogo);
                mail.Body = "<htm><body><img src=\"cid:" + contentID + "\">hi</body></html>";
                */
                mail.Body = Body;
                //________________________________________________________
                smtp.Send(mail);
                MyGlobal.Log("Mail Succesully Sent to " + mailDoc.m_To);
            }
            catch (Exception ex)
            {
                MyGlobal.Log("Failed to send Mail to " + mailDoc.m_To);
                //MessageToDebugger("MAIL FAILED-" + ex.Message);
            }
        }
        //---------------------------------------------------------------
        private void SendEmail(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(mailDoc.m_To);
                mail.From = new MailAddress("Cartrac trip alert <cartrac.mobisat@gmail.com>");
                mail.Subject = mailDoc.m_Subject;
                string Body = mailDoc.m_Body;
                //mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587; // or  587
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("cartrac.mobisat@gmail.com", "cartracmobisat123$");
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //________________________________________________________
                var contentID = "Image";
                var inlineLogo = new Attachment(@"c:\temp\img.png");
                inlineLogo.ContentId = contentID;
                inlineLogo.ContentDisposition.Inline = true;
                inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                mail.Attachments.Add(inlineLogo);
                mail.Body = "<htm><body><img src=\"cid:" + contentID + "\"></body></html>";
                //________________________________________________________
                smtp.Send(mail);
                MessageToDebugger("MAIL SENT");
            }
            catch (Exception ex)
            {
                MessageToDebugger("MAIL FAILED-" + ex.Message);
            }
        }
        /*
        public static string FixBase64ForImage(string Image)
        {
            System.Text.StringBuilder sbText = new System.Text.StringBuilder(Image, Image.Length);
            sbText.Replace("\r\n", string.Empty); sbText.Replace(" ", string.Empty);
            return sbText.ToString();
        }
        */
        public static void SendEmail_Doom(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(mailDoc.m_To.Trim());
                mail.From = new MailAddress(mailDoc.Domain + " Alert <cartrac.mobisat@gmail.com>");
                mail.Subject = mailDoc.m_Subject;
                string Body = mailDoc.m_Body;
                mail.Body = Body;
                mail.IsBodyHtml = true;
                //_____________________________SMTP Settings and sent
                SmtpClient smtp = new SmtpClient();
                
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587; // or  587
                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Credentials = new System.Net.NetworkCredential("cartrac.mobisat@gmail.com", "cartracmobisat123$");
                smtp.EnableSsl = true;
                smtp.Send(mail);
                
                //smtp.Host = "smtp.rediffmail.com";
                //smtp.Host = "mail.chcgroup.in";
                /*
                smtp.Host = "smtp.chcgroup.in";
                smtp.Port = 587;
                //smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Credentials = new System.Net.NetworkCredential("meterbox@chcgroup.in", "hwmeter2019$");
                smtp.EnableSsl = false;
                smtp.Send(mail);
                */
            }
            /*
            catch (SmtpException e)
            {
                MyGlobal.Error("SendEmail_Doom -> "  + e.Message);
            }
            */
            catch (Exception ex)
            {
                MyGlobal.Error("SendEmail_Doom -> " + "()"+
                    ex.Message +
                    "__" + mailDoc.m_To + "__" + mailDoc.m_Subject + "__" + mailDoc.m_Body);
            }
        }
        public static void SendEmail_DoomReceipt(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            var path = (System.Web.HttpContext.Current == null)
                ? System.Web.Hosting.HostingEnvironment.MapPath("~/")
                : System.Web.HttpContext.Current.Server.MapPath("~/");

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    using (Attachment inlineLogo = new Attachment(path + (@"data/dvrphotos/" + mailDoc.m_Param_DvrPhoto)))
                    {
                        using (Attachment inlineLogo_RouteMap = new Attachment(path + @"/temp/map_" + mailDoc.m_Param_IMEI + ".png"))
                        {
                            mail.To.Add(mailDoc.m_To);
                            mail.From = new MailAddress("Cartrac trip alert <cartrac.mobisat@gmail.com>");
                            mail.Subject = mailDoc.m_Subject;
                            string Body = mailDoc.m_Body;
                            mail.Body = Body;
                            mail.IsBodyHtml = true;
                            //_____________________________Add CID attachments


                            var contentID = "DvrPhoto";
                            //var inlineLogo = new Attachment(path + (@"data/dvrphotos/" + mailDoc.m_Param_DvrPhoto));  //c:\temp\eugene.jpg
                            inlineLogo.ContentId = contentID;
                            inlineLogo.ContentDisposition.Inline = true;
                            inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                            mail.Attachments.Add(inlineLogo);

                            var contentID_RouteMap = "RouteMap";
                            //var inlineLogo_RouteMap = new Attachment(path + @"/temp/map_" + mailDoc.m_Param_IMEI + ".png");
                            inlineLogo_RouteMap.ContentId = contentID_RouteMap;
                            inlineLogo_RouteMap.ContentDisposition.Inline = true;
                            inlineLogo_RouteMap.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                            mail.Attachments.Add(inlineLogo_RouteMap);
                            //_____________________________SMTP Settings and sent
                            SmtpClient smtp = new SmtpClient();
                            smtp.Host = "smtp.gmail.com";
                            smtp.Port = 587; // or  587
                            smtp.UseDefaultCredentials = false;
                            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtp.Credentials = new System.Net.NetworkCredential("cartrac.mobisat@gmail.com", "cartracmobisat123$");
                            smtp.EnableSsl = true;
                            smtp.Send(mail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyGlobal.Error("SendEmail_DoomReceipt -> " + ex.Message);
            }
        }
        //____________________________
        /*
        https://stackoverflow.com/questions/11594702/how-to-get-image-from-google-static-map-api
        "&zoom= 19" +
        */
        private static void SaveRouteImage(String sIMEI, String sTripNo)
        {
            String url = "https://maps.googleapis.com/maps/api/staticmap?" +
                "size=500x400" +
                "&style=visibility:on" +
                "&scale= 1";
            //"&path=40.737102,-73.990318|40.749825,-73.987963|40.752946,-73.987384|40.755823,-73.986397";
            String sPath = "";
            String sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "select m_Lat,m_Lng from dispatch_log.log_" + sIMEI + " " +
                    "where m_id_trip='" + sTripNo + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                double dblLatPrev = 0, dblLngPrev = 0;
                                while (reader.Read())
                                {
                                    double dblLatDiff = dblLatPrev - ChatHub.GetDouble(GetPure(reader, 0));
                                    double dblLngDiff = dblLngPrev - ChatHub.GetDouble(GetPure(reader, 1));
                                    if (dblLatDiff > 0.01 || dblLatDiff < -0.01 || dblLngDiff > 0.01 || dblLngDiff < -0.01)
                                    {
                                        dblLatPrev = ChatHub.GetDouble(GetPure(reader, 0));
                                        dblLngPrev = ChatHub.GetDouble(GetPure(reader, 1));
                                        if (sPath.Length != 0) sPath += "|";
                                        sPath += GetPure(reader, 0) + "," + GetPure(reader, 1);
                                    }
                                }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("SaveRouteImage -> " + sIMEI+" -> "+ sTripNo+" -> "+ ex.Message);
            }
            url = url + "&path=" + sPath;
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(url, GetRootPath() + "temp/map_" + sIMEI + ".png");
                    //wc.DownloadFile(url, @"c:\temp\img.png");
                }
            }
            catch (ArgumentNullException ex)
            {
                //code specifically for a ArgumentNullException
                MyGlobal.Error("SaveRouteImage ArgumentNullException -> " + ex.Message);
            }
            catch (WebException ex)
            {
                //code specifically for a WebException
                MyGlobal.Error("SaveRouteImage WebException -> " + ex.Message+"____"+ url);
            }
            catch (Exception ex)
            {
                //code for any other type of exception
                MyGlobal.Error("SaveRouteImage Exception -> " + ex.Message);
            }
            finally
            {
                //call this if exception occurs or not
                //in this example, dispose the WebClient
                //wc?.Dispose();
            }
        }
        private static string GetRootPath()
        {
            var path = (System.Web.HttpContext.Current == null)
    ? System.Web.Hosting.HostingEnvironment.MapPath("~/")
    : System.Web.HttpContext.Current.Server.MapPath("~/");
            return path;
        }

        public static void SendReceiptForThisTripNo(Object obj)
        {
            ReceiptRequestObj TripReqObj = (ReceiptRequestObj)obj;
            String m_DriverID = "", m_AmountTotal = "", m_DistanceTotal = "",
                m_TripStartTime = "", m_TripEndTime = "", m_WaitingTime = "",
                trip_m_DeviceIMEI = "", trip_m_RegNo = "", m_JobID = "", m_DriverName = "",
                job_m_PickAddress = "", job_m_DropAddress = "", job_m_VehicleType = "",
                job_m_AssignedTo = "", job_m_AssignedToStaffID = "", 
                driver_m_FName = "", driver_m_Mobile = "",
                client_m_Email = "", client_m_Name = "";

            String sSQL = "SELECT m_DriverID,m_AmountTotal,m_DistanceTotal,m_TripStartTime,m_TripEndTime,m_WaitingTime,trip.m_DeviceIMEI as trip_m_DeviceIMEI,trip.m_RegNo as trip_m_RegNo,m_JobID,m_DriverName," +
"job.m_PickAddress as job_m_PickAddress,job.m_DropAddress as job_m_DropAddress,job.m_VehicleType as job_m_VehicleType,job.m_AssignedTo as job_m_AssignedTo,job.m_AssignedToStaffID as job_m_AssignedToStaffID," +
"driver.m_FName as driver_m_FName,driver.m_Mobile as driver_m_Mobile," +
"client.m_Email as client_m_Email,client.m_Name as client_m_Name " +
"FROM " + MyGlobal.activeDB + ".tbl_trips trip " +
"left join " + MyGlobal.activeDB + ".tbl_jobs_doom job on job.m_id=trip.m_JobID " +
"left join " + MyGlobal.activeDB + ".tbl_clients client on client.m_IMEI=job.m_IMEI " +
"left join " + MyGlobal.activeDB + ".tbl_drivers driver on driver.m_DeviceIMEI=trip.m_DeviceIMEI " +
"where m_TripSequentialNumber='" + TripReqObj.tripno + "' and trip.m_DeviceIMEI='" + TripReqObj.imei + "' " +
"and trip.m_Profile='" + TripReqObj.profile + "'";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_DriverID"] != null) m_DriverID = reader["m_DriverID"].ToString();
                                    if (reader["m_AmountTotal"] != null) m_AmountTotal = reader["m_AmountTotal"].ToString();
                                    if (reader["m_DistanceTotal"] != null) m_DistanceTotal = reader["m_DistanceTotal"].ToString();
                                    if (reader["m_TripStartTime"] != null) m_TripStartTime = reader["m_TripStartTime"].ToString();
                                    if (reader["m_TripEndTime"] != null) m_TripEndTime = reader["m_TripEndTime"].ToString();
                                    if (reader["m_WaitingTime"] != null) m_WaitingTime = reader["m_WaitingTime"].ToString();
                                    if (reader["trip_m_DeviceIMEI"] != null) trip_m_DeviceIMEI = reader["trip_m_DeviceIMEI"].ToString();
                                    if (reader["trip_m_RegNo"] != null) trip_m_RegNo = reader["trip_m_RegNo"].ToString();
                                    if (reader["m_JobID"] != null) m_JobID = reader["m_JobID"].ToString();
                                    if (reader["m_DriverName"] != null) m_DriverName = reader["m_DriverName"].ToString();
                                    if (reader["job_m_PickAddress"] != null) job_m_PickAddress = reader["job_m_PickAddress"].ToString();
                                    if (reader["job_m_DropAddress"] != null) job_m_DropAddress = reader["job_m_DropAddress"].ToString();
                                    if (reader["job_m_VehicleType"] != null) job_m_VehicleType = reader["job_m_VehicleType"].ToString();
                                    if (reader["job_m_AssignedTo"] != null) job_m_AssignedTo = reader["job_m_AssignedTo"].ToString();
                                    if (reader["job_m_AssignedToStaffID"] != null) job_m_AssignedToStaffID = reader["job_m_AssignedToStaffID"].ToString();
                                    if (reader["driver_m_FName"] != null) driver_m_FName = reader["driver_m_FName"].ToString();
                                    if (reader["driver_m_Mobile"] != null) driver_m_Mobile = reader["driver_m_Mobile"].ToString();
                                    if (reader["client_m_Email"] != null) client_m_Email = reader["client_m_Email"].ToString();
                                    if (reader["client_m_Name"] != null) client_m_Name = reader["client_m_Name"].ToString();
                                    //________________________Convert
                                    TimeSpan time;
                                    if (IsNumeric(m_DistanceTotal))
                                    {
                                        m_DistanceTotal = (GetInt32(m_DistanceTotal) / 1000.0).ToString("0.00");
                                    }
                                    else
                                    {
                                        m_DistanceTotal = "0";
                                    }
                                    if (IsNumeric(m_WaitingTime))
                                    {
                                        time = TimeSpan.FromSeconds(GetInt32(m_WaitingTime));
                                        m_WaitingTime = time.ToString(@"hh\:mm\:ss");
                                    }
                                    else
                                    {
                                        m_WaitingTime = "0";
                                    }
                                    if (IsNumeric(m_AmountTotal))
                                    {
                                        m_AmountTotal = (GetInt32(m_AmountTotal) / 100.0).ToString("0.00");
                                    }
                                    else
                                    {
                                        m_AmountTotal = "0";
                                    }
                                    if (IsNumeric(m_TripStartTime))
                                    {
                                        m_TripStartTime = UnixTimeToDateTime(m_TripStartTime);
                                    }
                                    else
                                    {
                                        m_TripStartTime = "";
                                    }
                                    if (IsNumeric(m_TripEndTime))
                                    {
                                        m_TripEndTime = UnixTimeToDateTime(m_TripEndTime);
                                    }
                                    else
                                    {
                                        m_TripEndTime = "";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("SendReceiptForThisTripNo -> "+ex.Message);
                //MessageToDebugger("FromDoom-4->"+ex.Message);
            }
            if (client_m_Email.Length == 0)
            {
                //MessageToDebugger("No email to sent");
                //return "No email associated";
            }
            //_______________________________________________________Get Map image file ready
            SaveRouteImage(trip_m_DeviceIMEI, TripReqObj.tripno);
            //________________________________________________________Read template
            string sBody = "None";
            String path = GetRootPath() + "views/Templates/TripMail.cshtml";
            if (System.IO.File.Exists(path))
            {
                sBody = System.IO.File.ReadAllText(path);
            }
            //________________________________________________________Replace
            sBody = sBody.Replace("{{m_DriverID}}", m_DriverID);
            sBody = sBody.Replace("{{m_AmountTotal}}", m_AmountTotal);
            sBody = sBody.Replace("{{m_DistanceTotal}}", m_DistanceTotal);
            sBody = sBody.Replace("{{m_TripStartTime}}", m_TripStartTime);
            sBody = sBody.Replace("{{m_TripEndTime}}", m_TripEndTime);
            sBody = sBody.Replace("{{m_WaitingTime}}", m_WaitingTime);
            sBody = sBody.Replace("{{trip_m_DeviceIMEI}}", trip_m_DeviceIMEI);
            sBody = sBody.Replace("{{trip_m_RegNo}}", trip_m_RegNo);
            sBody = sBody.Replace("{{m_JobID}}", m_JobID);
            sBody = sBody.Replace("{{m_DriverName}}", m_DriverName);
            sBody = sBody.Replace("{{job_m_PickAddress}}", job_m_PickAddress);
            sBody = sBody.Replace("{{job_m_DropAddress}}", job_m_DropAddress);
            sBody = sBody.Replace("{{job_m_VehicleType}}", job_m_VehicleType);
            sBody = sBody.Replace("{{job_m_AssignedTo}}", job_m_AssignedTo);
            sBody = sBody.Replace("{{job_m_AssignedToStaffID}}", job_m_AssignedToStaffID);
            sBody = sBody.Replace("{{driver_m_FName}}", driver_m_FName);
            sBody = sBody.Replace("{{m_DriverMobile}}", driver_m_Mobile);
            sBody = sBody.Replace("{{m_ClientName}}", client_m_Name);

            sBody = sBody.Replace("{{m_TripNo}}", TripReqObj.tripno);

            //________________________________________________________
            //var tripResponse = new TripResponse();
            //tripResponse.status = false;
            //tripResponse.result = "None";
            //_____________________________________________
            MailDoc mailDoc = new MailDoc();
            mailDoc.m_To = client_m_Email;// "support@sharewaredreams.com";
            mailDoc.m_Subject = "Cartrac trip alert " + TripReqObj.tripno;
            mailDoc.m_Body = sBody;
            mailDoc.m_Param_DvrPhoto = m_DriverID + ".jpg";
            mailDoc.m_Param_IMEI = trip_m_DeviceIMEI;
            Thread newThread = new Thread(ChatHub.SendEmail_DoomReceipt);
            newThread.Start(mailDoc);
            //tripResponse.result = "Mail sent";
            //_____________________________________________
            //return Json(tripResponse, JsonRequestBehavior.AllowGet);
            //return "Mail Sent";
        }
        public static bool IsNumeric(string text)
        {
            Int32 test = 0;
            return Int32.TryParse(text, out test);
        }
    }
    public class MailDoc
    {
        public string m_To { get; set; }
        public string m_Subject { get; set; }
        public string m_Body { get; set; }
        public string m_Param_DvrPhoto { get; set; }
        public string m_Param_IMEI { get; set; }
        public string Domain { get; set; }
        public MailDoc()
        {
            m_To = "";
            m_Subject = "";
            m_Body = "";
            m_Param_DvrPhoto = "";
            m_Param_IMEI = "";
            Domain = "";
        }
    }

}