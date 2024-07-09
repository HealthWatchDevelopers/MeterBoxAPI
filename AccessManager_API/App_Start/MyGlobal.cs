using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Extensions.Hosting;
using MyHub.Hubs;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;


namespace MyHub.Controllers
{
    public class MyGlobal
    {
        public const int const_LocalDebug = 1;//"chchealthcare";
        //public const int const_LocalDebug = 2;//"greyoffice";

        public const string WORKDAY_MARKER = "WD";

        public const int const_ALLOWED_LATE_DELAY = 600; // 10 minutes

        public const int const_SHIFT_PRE_TIME = 3600;
        public const int const_SHIFT_POST_TIME = 3600;

        public static string syncid = "";
        public static string activeDB = "meterbox";//this for only using aws testing server by Sivaguru M CHC1704 on 05-03-2024
        //Starts Alter for AWS testing process by Sivaguru M CHC1704 on 05-03-2024
        //public static string activeDB = "meterbox";
        //Ends Alter for AWS testing process
        public static string activeDBLog = "";
        public static string activeDomain = "chchealthcare";//Aws testing server by Sivaguru M CHC1704 on 23-05-2024


        public static readonly string[] constArrayMonths =
    { "Jan", "Feb", "Mar", "Apr","May","Jun","Jly","Aug","Sep","Oct","Nov","Dec" };
        public static string GetDomain()
        {
            //int port = HttpContext.Current.Request.Url.Port;
            if (HttpContext.Current.Request.Url.Host.IndexOf("gingertracker", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                activeDB = "dispatch";
                activeDBLog = "dispatch_log";
                activeDomain = "gingertracker";
                return "Ginger Tracker";
            }
            else if (HttpContext.Current.Request.Url.Host.IndexOf("trilliontaxi", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                activeDB = "dispatch";
                activeDBLog = "dispatch_log";
                activeDomain = "trilliontaxi";
                return "Trillion Taxi";
            }
            else if ((HttpContext.Current.Request.Url.Host.IndexOf("greyoffice", StringComparison.CurrentCultureIgnoreCase) > -1) ||
                (HttpContext.Current.Request.Url.Host.IndexOf("chchealthcare", StringComparison.CurrentCultureIgnoreCase) > -1))
            {
                
                
                if (HttpContext.Current.Request.Url.Host.IndexOf("chchealthcare", StringComparison.CurrentCultureIgnoreCase) > -1)
                    activeDB = "meterbox";
                activeDBLog = "meterbox_log";
                activeDomain = "chchealthcare";
                return "Meter Box";
            }
            else if ((HttpContext.Current.Request.Url.Host.IndexOf("localhost", StringComparison.CurrentCultureIgnoreCase) > -1) ||
                (HttpContext.Current.Request.Url.Host.IndexOf("192.168.", StringComparison.CurrentCultureIgnoreCase) > -1) ||
                (HttpContext.Current.Request.Url.Host.IndexOf("127.0.", StringComparison.CurrentCultureIgnoreCase) > -1))
            {
                if (const_LocalDebug == 1)
                {
                    activeDB = "meterbox";
                    activeDBLog = "meterbox_log";
                    activeDomain = "chchealthcare";
                    //activeDomain = "chc-healthwatch-502072296.us-east-1.elb.amazonaws.com:56000";
                    return "Meter Box";
                }
                else
                {
                    activeDB = "meterbox";
                    activeDBLog = "meterbox_log";
                    activeDomain = "chchealthcare";
                    //activeDomain = "chc-healthwatch-502072296.us-east-1.elb.amazonaws.com:56000";
                    return "Meter Box";

                    //activeDomain = "greyoffice";
                    //activeDB = "meterbox";
                    //return "Grey Office";
                }
            }
            return "";
        }
        public static string GetConnectionString()
        {
            if (HttpContext.Current.Request.Url.Host.IndexOf("greyoffice", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                return "Data Source=localhost; User Id=root; Password=MyGreyoffice; Database=mysql;SslMode=none";
            }
            else if (HttpContext.Current.Request.Url.Host.IndexOf("chchealthcare", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                //Testing Starts for AWS by Sivaguru M CHC1704 on 21-05-2024
                return "Server=localhost; User=root; Password=Healthdb$$$111###;SslMode=none;Convert Zero Datetime=True;allowPublicKeyRetrieval=true";

                //    return "Data Source=localhost; User Id=Root; Password=C8Gdgq_9rXW5E4c$; Database=mysql;SslMode=none;Convert Zero Datetime=True";
                //}
                //else if (HttpContext.Current.Request.Url.Host.IndexOf("chchealthcare", StringComparison.CurrentCultureIgnoreCase) > -1)
                //{
                //return "Data Source=localhost; User Id=Root; Password=xyz; Database=mysql;SslMode=none;Convert Zero Datetime=True";
                //Testing Ends
            }
            else if (HttpContext.Current.Request.Url.Host.IndexOf("chc-healthwatch-502072296.us-east-1.elb.amazonaws.com", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                return "Server=localhost; User=root; Password=Healthdb$$$111###;SslMode=none;Convert Zero Datetime=True;allowPublicKeyRetrieval=true";
            }
            else if (HttpContext.Current.Request.Url.Host.IndexOf("chg-healthwatch-1983770325.ap-south-1.elb.amazonaws.com", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                return "Server=localhost; User=root; Password=Healthdb$$$111###;SslMode=none;Convert Zero Datetime=True;allowPublicKeyRetrieval=true";
            }
            else
            {
                //return "Server=10.0.135.112; User=meterboxuser; Password=meterbox@1234;SslMode=none;Convert Zero Datetime=True;allowPublicKeyRetrieval=true";
                return "Server=localhost;User=root; Password=Healthdb$$$111###;SslMode=none;Convert Zero Datetime=True;allowPublicKeyRetrieval=true";
                //Testing Ends

                //return "Data Source=localhost; User Id=root; Password=root; Database=mysql;SslMode=none;Convert Zero Datetime=True";
            }
        }
        public static string GetMyDomain()
        {
            return HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port;
        }

        public static string EmailCredentioal(string value)
        {
            if (value == "MailId")
            {
                return "hwitch34@gmail.com";

            }
            else
            {
                return "rxql zpyw gllf jsto";
            }
        }

        #region Send Email template 17-05-2024 by Periya Samy P CHC1761
        public static string EmailSending(string to, string subject, string body, string imagePath = null, bool BodyHTML = false)
        {
            //string imagePath = "D:/Office/Image/officename.png";

            string EmaiId = EmailCredentioal("MailId");
            string Password = EmailCredentioal("Password");
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(EmaiId, Password);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(EmaiId);
                        mailMessage.To.Add(to);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        if (BodyHTML == true)
                        {
                            mailMessage.IsBodyHtml = true;
                        }

                        // Add image as attachment
                        if (imagePath != null)
                        {
                            Attachment attachment = new Attachment(imagePath);
                            attachment.ContentId = "image";
                            mailMessage.Attachments.Add(attachment);
                        }
                        smtpClient.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }
        #endregion

        public static string GetRandomNo(int min, int max)
        {
            Random r = new Random();
            return r.Next(min, max).ToString(); //for ints
        }
        public static Int16 GetInt16(String sIn)
        {
            Int16 i = 0;
            if (Int16.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }
        public static Int32 GetInt32(String sIn)
        {
            Int32 i = 0;
            if (Int32.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }
        public static Int64 GetInt64(String sIn)
        {
            Int64 i = 0;
            if (Int64.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }
        public static Double GetDouble(String sIn)
        {
            Double i = 0;
            if (Double.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }
        public static long ToEpochTime(DateTime dateTime)
        {
            var date = dateTime.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }
        /// <summary>
        /// Converts the given epoch time to a <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/> kind.
        /// </summary>
        public static DateTime ToDateTimeFromEpoch(long intDate)
        {
            var timeInTicks = intDate * TimeSpan.TicksPerSecond;
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(timeInTicks);
        }

        public static String GetDDHHMMSS(double secs)
        {
            TimeSpan t = TimeSpan.FromSeconds(secs);
            //string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
            return string.Format("{0:D2} Days {1:D2}:{2:D2}:{3:D2}",
                t.Days,
                            t.Hours,
                            t.Minutes, t.Seconds);
        }
        public static String GetHHMM(long secs)
        {
            TimeSpan t = TimeSpan.FromSeconds(secs);
            //string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
            return string.Format("{0:D2}:{1:D2}",
                            t.Hours,
                            t.Minutes);
        }
        public static String GetHHMMSS(long secs)
        {
            TimeSpan t = TimeSpan.FromSeconds(secs);
            //string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);
        }
        public static Int32 GetSeconds(int iYears, int iMonths)// Jan=0
        {
            var dateTime = new DateTime(iYears, iMonths + 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (Int32)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
        }
        public static Int32 GetSeconds(int iYears, int iMonths, int iDay)// Jan=0
        {
            var dateTime = new DateTime(iYears, iMonths + 1, iDay, 0, 0, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (Int32)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
        }
        public static long GetUnixTime(int iYears, int iMonths, int iDay)// Jan=0
        {
            try
            {
                return ToEpochTime(new DateTime(iYears, iMonths, iDay, 0, 0, 0, DateTimeKind.Utc));
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }

        }
        public static string getFormattedTimeFromSecond(Int32 second)
        {

            TimeSpan t = TimeSpan.FromSeconds(second);

            string formatedTime = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds);

            return formatedTime;
        }
        //--------------------------------------------
        public static void Log(String txt)
        {
            try
            {
                using (var file = new StreamWriter("c:\\temp\\temp\\log.txt", true))
                {
                    file.WriteLine(txt);
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void Error(String txt)
        {
            try
            {
                using (var file = new StreamWriter("c:\\temp\\temp\\Error.txt", true))
                {
                    file.WriteLine(txt);
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //--------------------------------------------
        public static string SendSMS(string code, string mobile, string message)
        {
            if (activeDomain.Equals("chchealthcare"))
            {
                return SendSMS_CHCHEALTHCARE(code, mobile, message);
            }
            else
            {
                return SendSMS_GREYOFFICE(code, mobile, message);
            }
        }
        public static string SendSMS_GREYOFFICE(string code, string mobile, string message)
        {
            String sStatus = "";
            WebClient wc = null;
            try
            {
                var responseString = "";
                using (var client = new WebClient())
                {
                    String sMobileForSendingSMS = code + mobile;
                    //if (mobile.Length == 10) sMobileForSendingSMS = "+91" + mobile;
                    var values = new NameValueCollection();
                    values["user"] = "shareware";
                    values["apikey"] = "HOSi8A94uaFncPwCdbQa";
                    values["mobile"] = sMobileForSendingSMS;    // "+918122004444";
                    values["message"] = message;
                    values["senderid"] = "CrzyTr";
                    values["type"] = "txt";

                    var response = client.UploadValues("http://smshorizon.co.in/api/sendsms.php", values);

                    responseString = Encoding.Default.GetString(response);
                }
                //sStatus = "SMS Generated. Mode=" + signupMode + "[" + responseString + "]";
                sStatus = "OTP sent by SMS.";
            }
            catch (ArgumentNullException ex)
            {
                //code specifically for a ArgumentNullException
                sStatus = "Unable to send SMS.";
            }
            catch (WebException ex)
            {
                //code specifically for a WebException
                sStatus = "Unable to send SMS.";
            }
            catch (Exception ex)
            {
                //code for any other type of exception
                sStatus = "Unable to send SMS.";
            }
            finally
            {
                //call this if exception occurs or not
                //in this example, dispose the WebClient
                wc?.Dispose();
            }
            return sStatus;
        }
        public static string SendSMS_CHCHEALTHCARE(string code, string mobile, string message)
        {
            String sStatus = "";
            WebClient wc = null;
            try
            {
                var responseString = "";
                String sMobileForSendingSMS = code + mobile;

                using (var client = new WebClient())
                {
                    //if (mobile.Length == 10) sMobileForSendingSMS = "+91" + mobile;
                    var values = new NameValueCollection();
                    values["user"] = "meterbox";
                    values["apikey"] = "KmausZq4eMvmu6bJRzpX";
                    values["mobile"] = sMobileForSendingSMS;    // "+918122004444";
                    values["message"] = message;
                    values["senderid"] = "FromHW";
                    values["type"] = "txt";

                    var response = client.UploadValues("http://smshorizon.co.in/api/sendsms.php", values);

                    responseString = Encoding.Default.GetString(response);
                }
                //sStatus = "SMS Generated. Mode=" + signupMode + "[" + responseString + "]";
                sStatus = "OTP sent by SMS.";
            }
            catch (ArgumentNullException ex)
            {
                //code specifically for a ArgumentNullException
                sStatus = "Unable to send SMS.";
            }
            catch (WebException ex)
            {
                //code specifically for a WebException
                sStatus = "Unable to send SMS.";
            }
            catch (Exception ex)
            {
                //code for any other type of exception
                sStatus = "Unable to send SMS.";
            }
            finally
            {
                //call this if exception occurs or not
                //in this example, dispose the WebClient
                wc?.Dispose();
            }
            return sStatus;
        }
        //--------------------------------------------------
        public static void SendHubObject_ToBrowser(string toWhom, HubObject obj)
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            List<String> connections = hub.GetBrowserConnections(toWhom);
            if (connections != null)
            {
                foreach (String connectionID in connections)
                {
                    hubContext.Clients.Client(connectionID).HubToBrowser(obj);
                }
            }
        }
        //-----------------------------------------------
        /*
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        */
        public static string Base64Decode(string base64EncodedData)
        {
            return HttpUtility.UrlDecode(base64EncodedData);
        }
        public static string Base64Encode(string plainText)
        {
            return HttpUtility.UrlEncode(plainText);
        }
        public static string GetPureString(MySqlDataReader reader, string sFldName)
        {
            int ord = reader.GetOrdinal(sFldName);
            if (reader.IsDBNull(ord)) return "";
            return HttpUtility.UrlDecode(reader.GetString(ord));
        }
        public static string GetPureDateTimeString(MySqlDataReader reader, string sFldName)
        {
            int ord = reader.GetOrdinal(sFldName);
            if (reader.IsDBNull(ord)) return "";
            return reader.GetDateTime(ord).ToString("dd-MM-yyyy");

        }
        public static Int16 GetPureInt16(MySqlDataReader reader, string sFldName)
        {
            int ord = reader.GetOrdinal(sFldName);
            if (reader.IsDBNull(ord)) return 0;
            return reader.GetInt16(ord);
        }
        public static Int32 GetPureInt32(MySqlDataReader reader, string sFldName)
        {
            int ord = reader.GetOrdinal(sFldName);
            if (reader.IsDBNull(ord)) return 0;
            return reader.GetInt32(ord);
        }
        public static double GetPureDouble(MySqlDataReader reader, string sFldName)
        {
            int ord = reader.GetOrdinal(sFldName);
            if (reader.IsDBNull(ord)) return 0;
            return reader.GetDouble(ord);
        }
        // Month should be 1-12
        public static int GetDaysInThisMonth(int year, int month)
        {
            try
            {
                return DateTime.DaysInMonth(year, month);
            }
            catch (ArgumentOutOfRangeException)
            {

            }
            return 0;
        }
        public static string Right(string value, int length)
        {
            return value.Substring(value.Length - length);
        }

        public static string GetIPAddress()
        {
            string ipList = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipList))
            {
                return ipList.Split(',')[0];
            }

            return HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }
        public static string GetFieldFromTable(string profile, string table, string field, string keystring)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select " + field + " from  " + MyGlobal.activeDB + "." + table + " " +
                        "where m_Profile='" + profile + "' " + keystring;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    return MyGlobal.GetPureString(reader, field);
                                }
                            }
                        }
                        con.Close();
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException--GetFieldFromTable--" + ex.Message);
            }
            return "";
        }
        public static Int32 GetNewVchNo(MySqlConnection con, string profile)
        {
            Int32 iVchNo = 0;
            string sSQL = "select max(m_VchNo) from " + MyGlobal.activeDB + ".tbl_accounts " +
            "where m_Profile='" + profile + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            iVchNo = reader.GetInt32(0);
                        }
                    }
                }
            }
            iVchNo++;
            return iVchNo;
        }
        public static Int32 UnixFromHHMM(string time)
        {
            int iHours = 0, iMinutes = 0;
            if (time.Length == 3)   // 730
            {
                iHours = MyGlobal.GetInt16(time.Substring(0, 1));
                iMinutes = MyGlobal.GetInt16(time.Substring(1, 2));
            }
            else if (time.Length == 4)//1210
            {
                if (time.Substring(1, 1).Equals(":"))//7:30
                {
                    iHours = MyGlobal.GetInt16(time.Substring(0, 1));
                    iMinutes = MyGlobal.GetInt16(time.Substring(2, 2));
                }
                else // 1210
                {
                    iHours = MyGlobal.GetInt16(time.Substring(0, 2));
                    iMinutes = MyGlobal.GetInt16(time.Substring(2, 2));
                }
            }
            else if (time.Length == 5)//12:10
            {
                iHours = MyGlobal.GetInt16(time.Substring(0, 2));
                iMinutes = MyGlobal.GetInt16(time.Substring(3, 2));
            }
            else
            {
                return -1;
            }
            return iHours * 3600 + iMinutes * 60;
        }
        public static void SendHubObject(string toWhom, HubObject obj)
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            List<String> connections = hub.GetBrowserConnections(toWhom);
            if (connections != null)
            {
                foreach (String connectionID in connections)
                {
                    hubContext.Clients.Client(connectionID).HubToBrowser(obj);
                }
            }
        }
    }
    public class HubObject
    {
        public string Mode { get; set; }
        public string sData { get; set; }
        public long lData { get; set; }
        public string sMess { get; set; }
        public HubObject()
        {
            sData = "";
            sMess = "";
            lData = 0;
        }
    }
}
/*
                             MySqlTransaction trans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = trans;
                            try
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_staffs (m_Profile,m_FName,m_Password,m_MenuKey,m_Status) values ('" + profile + "','_New','1234','" + sOwnerKey + "','active');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();

                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                                "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                                "('" + profile + "','" + staffid + "','" + email + "','" + m_StaffID + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + "New entry created" + "')";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //-------------------
                                trans.Commit();

                                postResponse.status = true;
                                postResponse.result = "Done";
                            }
                            catch (Exception ex) //error occurred
                            {
                                trans.Rollback();
                                postResponse.result = "Error " + ex.Message;
                            }
*/
