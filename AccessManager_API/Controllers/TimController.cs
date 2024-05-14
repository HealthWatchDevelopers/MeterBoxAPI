using MyHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using MyHub.Hubs;
using System.Threading;
using System.Globalization;
using System.Net.NetworkInformation;
using Microsoft.Ajax.Utilities;
using System.Runtime.Remoting.Messaging;
using Microsoft.AspNet.SignalR.Infrastructure;
using System.Diagnostics;

namespace MyHub.Controllers
{
    public class TimController : Controller
    {
        DateTime pwdUpdatedDate;//20-01-2024 by Sivaguru M CHC1704

        // GET: Tim
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetDesktop(string profile, string timezone)
        {
            var myDash = new MyDash();
            myDash.status = false;
            myDash.result = "";

            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            myDash.taxies_online = hub.GetTaxiesOnline();
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "select " +
                        "sum(Case When m_TripEndTime is not null Then 1 Else 0 End) as trips_completed," +
                        "sum(Case When m_TripEndTime is null Then 1 Else 0 End) as trips_open," +
                        "sum(m_AmountTotal)  as trips_amount " +
                        "from " + MyGlobal.activeDB + ".tbl_trips " +
                        "where m_TripStartTime>=" + (MyGlobal.ToEpochTime(DateTime.Today)) + " " +
                        "and m_TripStartTime < " + (MyGlobal.ToEpochTime(DateTime.Today) + 86400) + " " +
                        "and m_Profile='" + profile + "';";
                    //myDash.result = unixTimestamp.ToString();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) myDash.trips_completed = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) myDash.trips_open = reader.GetInt32(1);
                                    if (!reader.IsDBNull(2)) myDash.trips_amount = reader.GetInt32(2);
                                }
                            }
                        }
                    }
                    myDash.status = true;
                    myDash.result = "Done";
                }
            }
            catch (MySqlException ex)
            {
                myDash.result = "Error-" + ex.Message;
            }

            return Json(myDash, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetDesktop_AccessManager(string profile, string timezone)
        {
            var myDash = new MyDash();
            myDash.status = false;
            myDash.result = "";

            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            myDash.terminals_online = hub.GetAccessManagerTerminalsOnline();
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "select " +
                        "sum(Case When m_TripEndTime is not null Then 1 Else 0 End) as trips_completed," +
                        "sum(Case When m_TripEndTime is null Then 1 Else 0 End) as trips_open," +
                        "sum(m_AmountTotal)  as trips_amount " +
                        "from " + MyGlobal.activeDB + ".tbl_trips " +
                        "where m_TripStartTime>=" + (MyGlobal.ToEpochTime(DateTime.Today)) + " " +
                        "and m_TripStartTime < " + (MyGlobal.ToEpochTime(DateTime.Today) + 86400) + " " +
                        "and m_Profile='" + profile + "';";
                    //myDash.result = unixTimestamp.ToString();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) myDash.trips_completed = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) myDash.trips_open = reader.GetInt32(1);
                                    if (!reader.IsDBNull(2)) myDash.trips_amount = reader.GetInt32(2);
                                }
                            }
                        }
                    }
                    myDash.status = true;
                    myDash.result = "Done";
                }
            }
            catch (MySqlException ex)
            {
                myDash.result = "Error-" + ex.Message;
            }

            return Json(myDash, JsonRequestBehavior.AllowGet);
        }
        /*
        [HttpPost]
        public ActionResult GetTrips_OLD(string timezone)
        {
            var tripResponse = new TripResponse();
            tripResponse.status = false;
            tripResponse.result = "None";
            Int16 iTimeZone;
            Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_trips where " +
                    "m_Profile='grey' order by m_TripStartTime desc limit 20;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string EndTime = "";
                                    int colIndex = reader.GetOrdinal("m_TripEndTime");
                                    if (!reader.IsDBNull(colIndex))
                                        EndTime = (reader.GetInt32("m_TripEndTime") - iTimeZone).ToString();

                                    MYTrip myTrip = new MYTrip();
                                    if (reader["m_TripSequentialNumber"] != null) myTrip.m_TripSequentialNumber = reader["m_TripSequentialNumber"].ToString();
                                    if (reader["m_TripType"] != null) myTrip.m_TripType = reader["m_TripType"].ToString();
                                    if (reader["m_TimeReceived"] != null) myTrip.m_TimeReceived = reader["m_TimeReceived"].ToString();
                                    if (reader["m_AmountTotal"] != null) myTrip.m_AmountTotal = reader["m_AmountTotal"].ToString();
                                    if (reader["m_DistanceTotal"] != null) myTrip.m_DistanceTotal = reader["m_DistanceTotal"].ToString();
                                    if (reader["m_TripStartTime"] != null) myTrip.m_TripStartTime = (reader.GetInt32("m_TripStartTime") - iTimeZone).ToString();
                                    if (reader["m_TripEndTime"] != null) myTrip.m_TripEndTime = EndTime;
                                    if (reader["m_WaitingTime"] != null) myTrip.m_WaitingTime = reader["m_WaitingTime"].ToString();
                                    if (reader["m_DeviceIMEI"] != null) myTrip.m_DeviceIMEI = reader["m_DeviceIMEI"].ToString();
                                    if (reader["m_JobID"] != null) myTrip.m_JobID = reader["m_JobID"].ToString();
                                    if (reader["m_DriverID"] != null) myTrip.m_StaffID = reader["m_DriverID"].ToString();
                                    if (reader["m_DriverName"] != null) myTrip.m_DriverName = reader["m_DriverName"].ToString();
                                    tripResponse.trips.Add(myTrip);
                                }
                                tripResponse.status = true;
                                tripResponse.result = "Done";
                            }
                            else
                            {
                                tripResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                tripResponse.result = "Error-" + ex.Message;
            }
            return Json(tripResponse, JsonRequestBehavior.AllowGet);
        }
        */
        //[HttpPost]
        public ActionResult GetTrips(string profile, string timezone, string sort, string order, string page, string search)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var tripResponse = new TripResponse();
            tripResponse.status = false;
            tripResponse.result = "None";
            tripResponse.total_count = "";
            Int16 iTimeZone;
            Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSearchKey = " (m_TripSequentialNumber like '%" + search + "%' or " +
    "m_DriverID like '%" + search + "%' or " +
    "m_RegNo like '%" + search + "%' or " +
    "m_JobID like '%" + search + "%' or " +
    "m_DriverName like '%" + search + "%' or " +
    "m_DeviceIMEI like '%" + search + "%') ";
                    //______________________________________________________________
                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_trips " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) tripResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //_______________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_TripStartTime";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_trips where " + sSearchKey + " and ";
                    sSQL += "m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string EndTime = "";
                                    int colIndex = reader.GetOrdinal("m_TripEndTime");
                                    if (!reader.IsDBNull(colIndex))
                                        EndTime = (reader.GetInt32("m_TripEndTime") - iTimeZone).ToString();

                                    TripItem myTrip = new TripItem();
                                    if (reader["m_id"] != null) myTrip.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (reader["m_TripSequentialNumber"] != null) myTrip.m_TripSequentialNumber = reader["m_TripSequentialNumber"].ToString();
                                    if (reader["m_TripType"] != null) myTrip.m_TripType = reader["m_TripType"].ToString();
                                    if (reader["m_TimeReceived"] != null) myTrip.m_TimeReceived = reader["m_TimeReceived"].ToString();
                                    if (reader["m_AmountTotal"] != null) myTrip.m_AmountTotal = reader["m_AmountTotal"].ToString();
                                    if (reader["m_DistanceTotal"] != null) myTrip.m_DistanceTotal = reader["m_DistanceTotal"].ToString();
                                    if (reader["m_TripStartTime"] != null) myTrip.m_TripStartTime = (reader.GetInt32("m_TripStartTime") - iTimeZone).ToString();
                                    if (reader["m_TripEndTime"] != null) myTrip.m_TripEndTime = EndTime;
                                    if (reader["m_WaitingTime"] != null) myTrip.m_WaitingTime = reader["m_WaitingTime"].ToString();
                                    if (reader["m_DeviceIMEI"] != null) myTrip.m_DeviceIMEI = reader["m_DeviceIMEI"].ToString();
                                    if (reader["m_JobID"] != null) myTrip.m_JobID = reader["m_JobID"].ToString();
                                    if (reader["m_DriverID"] != null) myTrip.m_DriverID = reader["m_DriverID"].ToString();
                                    if (reader["m_DriverName"] != null) myTrip.m_DriverName = reader["m_DriverName"].ToString();
                                    if (reader["m_RegNo"] != null) myTrip.m_RegNo = reader["m_RegNo"].ToString();
                                    if (reader["m_FleetID"] != null) myTrip.m_FleetID = reader["m_FleetID"].ToString();

                                    int ordStage = reader.GetOrdinal("m_Stage1");
                                    if (!reader.IsDBNull(ordStage)) myTrip.m_Stage1 = reader.GetInt64(ordStage);
                                    if (!reader.IsDBNull(ordStage + 1)) myTrip.m_Stage2 = reader.GetInt64(ordStage + 1);
                                    if (!reader.IsDBNull(ordStage + 2)) myTrip.m_Stage3 = reader.GetInt64(ordStage + 2);
                                    if (!reader.IsDBNull(ordStage + 3)) myTrip.m_Stage4 = reader.GetInt64(ordStage + 3);
                                    if (!reader.IsDBNull(ordStage + 4)) myTrip.m_Stage5 = reader.GetInt64(ordStage + 4);

                                    tripResponse.trips.Add(myTrip);
                                }
                                tripResponse.status = true;
                                tripResponse.result = "Done";
                            }
                            else
                            {
                                tripResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                tripResponse.result = "Error-" + ex.Message;
            }
            return Json(tripResponse, JsonRequestBehavior.AllowGet);
        }
        //__________________________________________________Upload profile photo

        [HttpPost]
        public async Task<JsonResult> UploadPhoto(string m_StaffID)
        {
            string sRet = "";
            try
            {
                foreach (string file in Request.Files)
                {
                    var fileContent = Request.Files[file];
                    if (fileContent != null && fileContent.ContentLength > 0)
                    {
                        var stream = fileContent.InputStream;
                        //var fileName = Path.GetFileName(file)+".jpg";
                        //var fileName = imei + "_" + DateTime.UtcNow.Ticks + ".jpg";
                        var fileName = m_StaffID + ".jpg";
                        //var path = Path.Combine(Server.MapPath("~/OneDrive/TripManager/ProfileImages/"), fileName);

                        var path = Path.Combine(Server.MapPath("~/data/dvrphotos/"), fileName);//Actual Working path

                        //var path = Path.Combine(Server.MapPath("C:/inetpub/wwwroot/meterboxAPITest/data/dvrphotos/"), fileName);
                        //var path = Path.Combine("c:/temp/photos/", fileName);
                        using (var fileStream = System.IO.File.Create(path))
                        {
                            stream.CopyTo(fileStream);
                            //___________________________Update DB
                            //sRet += InsertGalleryRecordIntoDB(imei, fileName);
                            //_______________Pass trigger
                            //var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                            //hubContext.Clients.All.broadcastMessage(imei, "{H}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Image UploadPhoto Exception -> " + ex.Message);

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json("Upload failed.[" + ex.Message + "]");
            }
            return Json("File uploaded successfully[" + sRet + "]");
        }
        //_________________________________________________Upload profile photo END
        [HttpPost]
        public ActionResult ManageTariff(string profile, string name, string mode,
    string m_Name, string m_FlagFall, string m_DistanceFree, string m_WaitingFree,
    string m_DistanceSlab, string m_DistanceCharge,
    string m_WaitingSlab, string m_WaitingCharge, string m_WaitingSpeedLag,
    string m_Surcharge)
        {
            var onLoadResponse = new OnLoadResponse();
            onLoadResponse.status = false;
            onLoadResponse.result = "None";
            /*
            if (name.Length == 0)
            {
                onLoadResponse.result = "Tariff name is empty";
                return Json(onLoadResponse, JsonRequestBehavior.AllowGet);
            }
            */
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (name.Length > 0)
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_tariffs where m_Name='" + name + "' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {

                                    onLoadResponse.status = true;
                                    onLoadResponse.result = "Done";
                                }
                            }
                        }

                        if (onLoadResponse.status)
                        {
                            if (mode.Equals("save"))
                            {
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_tariffs Set " +
                                    "m_FlagFall='" + m_FlagFall + "'," +
                                    "m_DistanceFree='" + m_DistanceFree + "'," +
                                    "m_WaitingFree='" + m_WaitingFree + "'," +
                                    "m_DistanceSlab='" + m_DistanceSlab + "'," +
                                    "m_DistanceCharge='" + m_DistanceCharge + "'," +
                                    "m_WaitingSlab='" + m_WaitingSlab + "'," +
                                    "m_WaitingCharge='" + m_WaitingCharge + "'," +
                                    "m_WaitingSpeedLag='" + m_WaitingSpeedLag + "'," +
                                    "m_Surcharge='" + m_Surcharge + "' " +
                                     "where m_Name = '" + name + "' and m_Profile='" + profile + "'; ";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                    onLoadResponse.status = true;
                                    onLoadResponse.result = "Done";
                                }
                            }
                            else if (mode.Equals("delete"))
                            {
                                if (name.Equals("default"))
                                {
                                    onLoadResponse.status = true;
                                    onLoadResponse.result = "Can't delete default tariff";
                                }
                                else
                                {
                                    sSQL = "delete from " + MyGlobal.activeDB + ".tbl_tariffs " +
                                    "where m_Name = '" + name + "' and m_Profile='" + profile + "';";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                    {
                                        com.ExecuteNonQuery();
                                        onLoadResponse.status = true;
                                        onLoadResponse.result = "Tarif '" + name + "' is deleted";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (name.Equals("new"))
                            {
                                onLoadResponse.status = true;
                                onLoadResponse.result = "Create new tariff";
                            }
                            else if (mode.Equals("save"))
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_tariffs  " +
                                    "(m_Name,m_FlagFall,m_DistanceFree,m_WaitingFree,m_DistanceSlab," +
                                    "m_DistanceCharge,m_WaitingSlab,m_WaitingCharge,m_WaitingSpeedLag," +
                                    "m_Surcharge,m_Profile) values ('" + name + "','" + m_FlagFall + "','" + m_DistanceFree + "'," +
                                    "'" + m_WaitingFree + "','" + m_DistanceSlab + "'," +
                                    "'" + m_DistanceCharge + "','" + m_WaitingSlab + "','" + m_WaitingCharge + "'," +
                                    "'" + m_WaitingSpeedLag + "','" + m_Surcharge + "','" + profile + "')";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                    onLoadResponse.status = true;
                                    onLoadResponse.result = "New tarif " + name + " is created";
                                }
                            }
                        }
                        //______________________Get tarif
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_tariffs where m_Name='" + name + "' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        onLoadResponse.myTariff.m_Name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        onLoadResponse.myTariff.m_FlagFall = reader.IsDBNull(5) ? "" : (reader.GetInt32(5) / 100.00).ToString("0.00");
                                        onLoadResponse.myTariff.m_DistanceFree = reader.IsDBNull(6) ? "" : reader.GetInt32(6).ToString();
                                        onLoadResponse.myTariff.m_WaitingFree = reader.IsDBNull(7) ? "" : reader.GetInt32(7).ToString();
                                        onLoadResponse.myTariff.m_DistanceSlab = reader.IsDBNull(8) ? "" : reader.GetInt32(8).ToString();
                                        onLoadResponse.myTariff.m_DistanceCharge = reader.IsDBNull(9) ? "" : (reader.GetInt32(9) / 100.00).ToString("0.00");
                                        onLoadResponse.myTariff.m_WaitingSlab = reader.IsDBNull(10) ? "" : reader.GetInt32(10).ToString();
                                        onLoadResponse.myTariff.m_WaitingCharge = reader.IsDBNull(11) ? "" : (reader.GetInt32(11) / 100.00).ToString("0.00");
                                        onLoadResponse.myTariff.m_WaitingSpeedLag = reader.IsDBNull(12) ? "" : reader.GetInt32(12).ToString();
                                        onLoadResponse.myTariff.m_Surcharge = reader.IsDBNull(13) ? "" : (reader.GetInt32(13) / 100.00).ToString("0.00");

                                        onLoadResponse.status = true;
                                        onLoadResponse.result = "Done";
                                    }
                                }
                            }
                        }
                    }
                    //______________________________Get the tariff list
                    sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_tariffs where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        onLoadResponse.tariffs.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    onLoadResponse.status = true;
                    if (mode.Equals("save"))
                    {
                        var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                        hubContext.Clients.All.broadcastMessage(name, "{I...}");
                    }
                }
            }
            catch (MySqlException ex)
            {
                onLoadResponse.result = "Error-" + ex.Message;
            }
            return Json(onLoadResponse, JsonRequestBehavior.AllowGet);
        }
        //_________________________________________________Manage Groups
        [HttpPost]
        public ActionResult ManageGroups(string profile, string name, string mode,
    string m_Name, string m_Description)
        {
            var groupResponse = new GroupResponse();
            groupResponse.status = false;
            groupResponse.result = "None";

            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (name.Length > 0)
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_groups where m_Name='" + name + "' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {

                                    groupResponse.status = true;
                                    groupResponse.result = "Done";
                                }
                            }
                        }

                        if (groupResponse.status)
                        {
                            if (mode.Equals("save"))
                            {
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_groups Set " +
                                    "m_Description='" + m_Description + "' " +
                                     "where m_Name = '" + name + "' and m_Profile='" + profile + "';";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                    groupResponse.status = true;
                                    groupResponse.result = "Done";
                                }
                            }
                            else if (mode.Equals("delete"))
                            {
                                if (name.Equals("default"))
                                {
                                    groupResponse.status = true;
                                    groupResponse.result = "Can't delete default tariff";
                                }
                                else
                                {
                                    sSQL = "delete from " + MyGlobal.activeDB + ".tbl_groups " +
                                    "where m_Name = '" + name + "' and m_Profile='" + profile + "';";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                    {
                                        com.ExecuteNonQuery();
                                        groupResponse.status = true;
                                        groupResponse.result = "Group '" + name + "' is deleted";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (name.Equals("new"))
                            {
                                groupResponse.status = true;
                                groupResponse.result = "Create new group";
                            }
                            else if (mode.Equals("save"))
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_groups  " +
                                    "(m_Name,m_Description,m_Profile) values ('" + name + "','" + m_Description + "','" + profile + "')";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                    groupResponse.status = true;
                                    groupResponse.result = "New group " + name + " is created";
                                }
                            }
                        }
                        //______________________Get tarif
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_groups where m_Name='" + name + "' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        groupResponse.myGroup.m_Name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        groupResponse.myGroup.m_Description = reader.IsDBNull(3) ? "" : reader.GetString(3);

                                        groupResponse.status = true;
                                        groupResponse.result = "Done";
                                    }
                                }
                            }
                        }
                    }
                    //______________________________Get the group list
                    sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_groups where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        groupResponse.groups.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    groupResponse.status = true;
                    if (mode.Equals("save"))
                    {
                        // Tarif needs auto refresh, not here
                        //var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                        //hubContext.Clients.All.broadcastMessage(name, "{I...}");
                    }
                }
            }
            catch (MySqlException ex)
            {
                groupResponse.result = "Error-" + ex.Message;
            }
            return Json(groupResponse, JsonRequestBehavior.AllowGet);
        }
        //__________________________________
        private bool CheckValidityOfDeviceIMEI(String sDeviceIMEI, MySqlConnection con)
        {
            string sSQL = "SELECT m_IMEI FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI='" + sDeviceIMEI + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }
        [HttpPost]
        public ActionResult GetDriverProfiles(string m_id, string m_StaffID, string mode,
            string m_FName, string m_AddressLocal, string m_AddressHome, string m_Country,
            string m_DeviceIMEI, string m_RegNo, string m_TaxiType)
        {
            var onProfileResponse = new OnProfileResponse();
            onProfileResponse.status = false;
            onProfileResponse.result = "None";
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bSave = true;
                    if (mode.Equals("save"))
                    {

                        if (m_id.Length == 0)
                        {
                            bSave = false;
                            onProfileResponse.result = "Unable to get data";
                        }
                        if (m_FName.Length == 0)
                        {
                            bSave = false;
                            onProfileResponse.result = "First Name can't be empty";
                        }
                        if (m_StaffID.Length == 0)
                        {
                            bSave = false;
                            onProfileResponse.result = "StaffID can't be empty";
                        }
                        if (m_DeviceIMEI.Length > 0)
                        {
                            if (!CheckValidityOfDeviceIMEI(m_DeviceIMEI, con))
                            {
                                //onProfileResponse.status = false;
                                //onProfileResponse.result = "Device IMEI is invalid";
                                //return Json(onProfileResponse, JsonRequestBehavior.AllowGet);
                                onProfileResponse.result = "Device IMEI is not in record";
                                bSave = false;
                            }
                        }
                    }
                    if (bSave)
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_drivers where m_id='" + m_id + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {

                                    onProfileResponse.status = true;
                                    onProfileResponse.result = "Done";
                                }
                            }
                        }

                        if (onProfileResponse.status)
                        {
                            if (mode.Equals("save"))
                            {
                                try
                                {
                                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_drivers Set " +
                                        "m_StaffID='" + m_StaffID + "'," +
                                        "m_FName='" + m_FName + "'," +
                                        "m_AddressLocal='" + m_AddressLocal + "'," +
                                        "m_AddressHome='" + m_AddressHome + "'," +
                                        "m_Country='" + m_Country + "', " +
                                        "m_DeviceIMEI='" + m_DeviceIMEI + "', " +
                                        "m_RegNo='" + m_RegNo + "', " +
                                        "m_TaxiType='" + m_TaxiType + "' " +
                                         "where m_id = '" + m_id + "'; ";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                    {
                                        com.ExecuteNonQuery();
                                        onProfileResponse.status = true;
                                        onProfileResponse.result = "Done";
                                    }
                                }
                                catch (MySqlException ex)
                                {
                                    onProfileResponse.result = "Error-" + ex.Message;
                                }
                            }
                            else if (mode.Equals("delete"))
                            {
                                if (m_StaffID.Equals("default"))
                                {
                                    onProfileResponse.status = true;
                                    onProfileResponse.result = "Can't delete default tariff";
                                }
                                else
                                {
                                    sSQL = "delete from " + MyGlobal.activeDB + ".tbl_drivers " +
                                    "where m_id = '" + m_id + "';";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                    {
                                        com.ExecuteNonQuery();
                                        onProfileResponse.status = true;
                                        onProfileResponse.result = "Staff ID '" + m_StaffID + "' is deleted";
                                        m_id = "0";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (mode.Equals("new"))
                            {
                                onProfileResponse.status = true;
                                onProfileResponse.result = "Create new driver profile";
                            }
                            else if (mode.Equals("save"))
                            {
                                if (m_StaffID.Length == 0)
                                {
                                    onProfileResponse.status = false;
                                    onProfileResponse.result = "StaffID can't be empty";
                                    return Json(onProfileResponse, JsonRequestBehavior.AllowGet);
                                }
                                try
                                {
                                    sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_drivers  " +
                                        "(m_StaffID," +
                                        "m_FName," +
                                        "m_AddressLocal," +
                                        "m_AddressHome," +
                                        "m_Country," +
                                        "m_DeviceIMEI," +
                                        "m_RegNo," +
                                        "m_TaxiType" +
                                        ") values ('" +
                                        m_StaffID + "','" +
                                        m_FName + "','" +
                                        m_AddressLocal + "','" +
                                        m_AddressHome + "','" +
                                        m_Country + "','" +
                                        m_DeviceIMEI + "','" +
                                        m_RegNo + "','" +
                                        m_TaxiType + "')";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                    {
                                        com.ExecuteNonQuery();
                                        m_id = com.LastInsertedId.ToString();
                                        onProfileResponse.status = true;
                                        onProfileResponse.result = "New driver with ID " + m_StaffID + " is created";
                                    }
                                }
                                catch (MySqlException ex)
                                {
                                    onProfileResponse.result = "Error-" + ex.Message;
                                }
                            }
                        }
                    }
                    //______________________Get Profile detail
                    sSQL = "SELECT m_id,m_FName,m_AddressHome,m_AddressLocal,m_StaffID,m_Country,m_DeviceIMEI,m_RegNo,m_TaxiType FROM " + MyGlobal.activeDB + ".tbl_drivers where m_id='" + m_id + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onProfileResponse.selectedProfile.m_id = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    onProfileResponse.selectedProfile.m_FName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    onProfileResponse.selectedProfile.m_AddressHome = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    onProfileResponse.selectedProfile.m_AddressLocal = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    onProfileResponse.selectedProfile.m_StaffID = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    onProfileResponse.selectedProfile.m_Country = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                    onProfileResponse.selectedProfile.m_DeviceIMEI = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                    onProfileResponse.selectedProfile.m_RegNo = reader.IsDBNull(7) ? "" : reader.GetString(7);
                                    onProfileResponse.selectedProfile.m_TaxiType = reader.IsDBNull(8) ? "" : reader.GetString(8);

                                    //onProfileResponse.status = true;
                                    //onProfileResponse.result = "Done";
                                }
                            }
                        }
                    }
                    //______________________________Get the tariff list
                    sSQL = "SELECT m_id,m_FName,m_StaffID FROM " + MyGlobal.activeDB + ".tbl_drivers order by m_FName;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var listProfile = new ListProfile();
                                    String sName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    //if (sName.Length > 12) sName = sName.Substring(0, 12) + "...";

                                    if (!reader.IsDBNull(0)) listProfile.m_id = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) listProfile.m_Name = sName;
                                    if (!reader.IsDBNull(2)) listProfile.m_StaffID = reader.GetString(2);
                                    onProfileResponse.profiles.Add(listProfile);
                                }
                            }
                        }
                    }
                    onProfileResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                onProfileResponse.result = "Error-" + ex.Message;
            }
            return Json(onProfileResponse, JsonRequestBehavior.AllowGet);
        }
        protected string GetPure(MySqlDataReader reader, int iIndex)
        {
            if (reader.IsDBNull(iIndex))
            {
                return "";
            }
            else
            {
                return reader.GetString(iIndex);
            }
        }
        [HttpPost]
        public ActionResult SendReceiptAgain(string profile, String tripno, String email)
        {
            var tripResponse = new TripResponse();
            tripResponse.status = false;
            tripResponse.result = "None";
            /*
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            tripResponse.result=hub.SendReceiptForThisTripNo(tripno);
            */
            //ChatHub.SendReceiptForThisTripNo(tripno);
            ReceiptRequestObj receiptRequestObj = new ReceiptRequestObj();
            receiptRequestObj.tripno = tripno;
            receiptRequestObj.imei = email;
            receiptRequestObj.profile = profile;

            Thread newThread = new Thread(ChatHub.SendReceiptForThisTripNo);
            newThread.Start(receiptRequestObj);
            tripResponse.result = "Mail Sent";
            tripResponse.status = true;
            return Json(tripResponse, JsonRequestBehavior.AllowGet);
        }
        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        [HttpPost]
        public ActionResult SignUp(String email, String profile, String password, string domain, string port, string firstname)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var tripResponse = new TripResponse();
            tripResponse.status = false;
            tripResponse.result = "None";
            if (!IsValidEmail(email))
            {
                tripResponse.result = "<span style='font-weight:bold;color:red;'>Please provide a valid email</span>";
                return Json(tripResponse, JsonRequestBehavior.AllowGet);
            }
            //__________________________________________
            try
            {
                string sSQL = "", m_Password = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sEmailLoc = "";
                    sSQL = "SELECT m_Email,m_Password FROM " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "m_Profile='" + email + "' and m_Email='" + email + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_Email"] != null) sEmailLoc = reader["m_Email"].ToString();
                                    if (reader["m_Password"] != null) m_Password = reader["m_Password"].ToString();
                                }
                            }
                        }
                    }
                    //__________________
                    if (m_Password.Length > 0)
                    {
                        if (sEmailLoc.Length > 0)
                        {
                            tripResponse.result = "<span style='font-weight:bold;color:red;'>Email  already exists.<span><br><span style='font-size:small;'>Please try forget password</span>";
                        }
                        else
                        {
                            tripResponse.result = "<span style='font-weight:bold;color:red;'>Unknown issue</span><br>Contact support";
                        }
                    }
                    else
                    {
                        string sKey = MyGlobal.GetRandomNo(111111, 999999);

                        sSQL = "";
                        if (MyGlobal.activeDB.Equals("dispatch"))
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_staffs (m_Profile,m_FName,m_Email,m_Password,m_Status,m_MenuKey,m_Designation,m_Band,m_Grade,m_Team,m_AttendanceSource,m_AttendanceMethod) " +
        "values ('" + email + "','" + firstname + "','" + email + "','" + password + "','" + sKey + "'," +
        "'a0-1,d0-2,m0-2,c0-9,g0-1,u0-1,u1-2,u2-2,s0-9,s1-0,s2-0,s3-0,s4-0,s5-0,f0-2,f1-2,f2-2,f3-2,f4-2,f5-0,r0-2,r1-2,l0-9,l1-0,l2-0,l3-0,l4-0,l5-0,w0-1,','CEO','Leadership','L1','Admin','Biometric','Administrative');";
                        }
                        else if (MyGlobal.activeDB.Equals("meterbox"))
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_staffs (m_Profile,m_FName,m_Email,m_Password,m_Status,m_MenuKey,m_Designation,m_Band,m_Grade,m_Team,m_AttendanceSource,m_AttendanceMethod) " +
        "values ('" + email + "','" + firstname + "','" + email + "','" + password + "','" + sKey + "'," +
        "'a0-1,d0-2,m0-9,c0-1,g0-1,g1-2,g2-2,o0-1,u0-1,u1-2,u2-9,u3-9,u4-2,u5-2,u6-2,u7-0,x0-1,x1-2,s0-1,s1-9,s2-9,s3-2,s4-2,s5-2,h0-1,h1-2,h2-9,h3-2,t0-1,t1-2,t2-2,b0-2,b1-2,b2-2,p0-1,p1-2,f0-9,f1-9,f2-9,f3-9,f4-9,f5-9,r0-9,r1-9,l0-9,l1-0,l2-0,l3-0,l4-0,l5-0,w0-1,n0-1,n1-1,n2-1,u8-1,u9-2,','CEO','Leadership','L1','Admin','Biometric','Administrative');";
                        }
                        else
                        {
                            tripResponse.result = "<span style='font-weight:bold;color:red;'>Unknown issue [1]</span><br>Contact support";
                        }
                        if (sSQL.Length > 0)
                        {
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //------------------------------
                                sSQL = "";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Leadership','','9999');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Managerial','','9999');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Integration','','9999');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Supervisory','','9999');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Execution','','9999');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Description,m_Order) values ('" + email + "','Trainee','','9999');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','L1','Leadership','','15');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','M1','Managerial','','271');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','I1','Integration','','527');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','S1','Supervisory','','783');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','E1','Execution','','1039');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Description,m_Order) values ('" + email + "','T1','Trainee','','1295');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_titles (m_Profile,m_Name,m_Head,m_Description) values ('" + email + "','CEO','','');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_titles (m_Profile,m_Name,m_Head,m_Description) values ('" + email + "','CFO','','');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_titles (m_Profile,m_Name,m_Head,m_Description) values ('" + email + "','Manager','','');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_titles (m_Profile,m_Name,m_Head,m_Description) values ('" + email + "','Supervisor','','');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_shiftnames (m_Profile,m_Name,m_ShiftStartTime,m_ShiftEndTime) values ('" + email + "','Morning','06:00','15:00');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_shiftnames (m_Profile,m_Name,m_ShiftStartTime,m_ShiftEndTime) values ('" + email + "','Day','09:00','18:00');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_shiftnames (m_Profile,m_Name,m_ShiftStartTime,m_ShiftEndTime) values ('" + email + "','Evening','15:00','24:00');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_shiftnames (m_Profile,m_Name,m_ShiftStartTime,m_ShiftEndTime) values ('" + email + "','Night','20:00','07:00');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','Basic Pay');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','HRA');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','Conveyance');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','Food Allowance');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','PF');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name) values ('" + email + "','ESIC');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','CTC','cr','CTC','20000',null,'0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Basic Pay','cr','CTC','50%','273','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','HRA','cr','Basic Pay','60%','274','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Conveyance','cr','CTC','2000','275','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Food Allowance','cro','CTC','3000','276','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','PF','dr','Gross Salary','2%','277','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','ESIC','dr','Gross Salary','1%','278','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Basic Pay','earn','CTC','50%','273','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','HRA','earn','Basic Pay','60%','274','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Conveyance','earn','CTC','2000','275','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','Food Allowance','earn','CTC','3000','276','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','PF','deduct','Gross Salary','2%','277','0','1558310060');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile,m_Name,m_Ledger,m_Type,m_BasedOn,m_Amount,m_Order,m_PayMode,m_Key) values ('" + email + "','Sample','ESIC','deduct','Gross Salary','1%','278','0','1558310060');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_payscale_master_list (m_Profile,m_Name,m_Key,m_CreatedBy) values ('" + email + "','Sample','1546300800','auto');";

                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //-----------------------------------------------------
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_misc_teams (m_Profile,m_Name) values ('" + email + "','Admin');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_misc_teams (m_Profile,m_Name) values ('" + email + "','Accounts');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //-----------------------------------------------------
                                sSQL += @"INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters " +
                                    "(m_Profile,m_RosterName,m_Year,m_Month) values " +
                                    "('" + profile + "','Admin'," +
                                    "'" + DateTime.Now.Year.ToString() + "','" + (DateTime.Now.Month - 1) + "');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();

                                //-------------------------------------------------------
                                //-------------------------------------------------------
                                myTrans.Commit();
                                tripResponse.status = true;
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    myTrans.Rollback();
                                    tripResponse.result = "<span style='font-weight:bold;color:red;'>" + "Failed. Rolled back [" + e.Message + "]" + "</span>";
                                }
                                catch (MySqlException ex)
                                {
                                    tripResponse.result = "<span style='font-weight:bold;color:red;'>" + "Failed. Rolled back [" + e.Message + "] [" + ex.Message + "]" + "</span>";
                                }
                            }
                            finally
                            {
                            }
                            //________________________________Send mail
                            if (tripResponse.status)
                            {
                                MailDoc mailDoc = new MailDoc();
                                mailDoc.m_To = email;
                                mailDoc.Domain = MyGlobal.GetDomain();
                                mailDoc.m_Subject = "'" + MyGlobal.GetDomain() + "', Account activation";
                                mailDoc.m_Body = "Hi <br><br>" +
                                "Your account is registered with '" + MyGlobal.GetDomain() + "'.<br>" +
                                "<a style='color:red;' href='http://" + MyGlobal.GetMyDomain() +
                                "/tim/activate?email=" + email + "&key=" + sKey + "&domain=" + domain + "&port=" + port + "'>Click</a> to activate your account<br><br>" +
                                "Thank you for using our service.<br>" +
                                "<b>" + MyGlobal.GetDomain() + "</b><br>";
                                Thread newThread = new Thread(ChatHub.SendEmail_Doom);
                                newThread.Start(mailDoc);
                                tripResponse.status = true;
                                tripResponse.result = "<br><span style='font-weight:bold;color:blue;'>New account created</span><br><span style='font-size:small;color:darkgreen;'>Please activate your account from your email</span>";
                            }
                            //________________________________Send mail END
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                tripResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________
            return Json(tripResponse, JsonRequestBehavior.AllowGet);
        }

        public ActionResult activate(string email, String key, string domain, string port)
        {
            string sMess = "Unknown activate response";
            try
            {
                string sSQL = "", m_Password = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bKeyValid = false;
                    sSQL = "SELECT m_Status FROM " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "m_Profile='" + email + "' and m_Email='" + email + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_Status"] != null)
                                    {
                                        bKeyValid = reader["m_Status"].ToString().Equals(key);
                                    }
                                }
                            }
                        }
                    }
                    //__________________
                    if (bKeyValid)
                    {
                        string sKey = MyGlobal.GetRandomNo(111111, 999999);
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs Set m_Status='Active' where " +
                            "m_Profile='" + email + "' and m_Email='" + email + "';";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();

                            //________________________________Send mail
                            MailDoc mailDoc = new MailDoc();
                            mailDoc.m_To = email;
                            mailDoc.Domain = MyGlobal.GetDomain();
                            mailDoc.m_Subject = "'" + MyGlobal.GetDomain() + "', Account activatated";
                            mailDoc.m_Body = "Hi <br><br>" +
    "Your account with '" + MyGlobal.GetDomain() + "' is active now. " +
    "<br><br>Thank you for using our service.<br>" +
    "<b>" + MyGlobal.GetDomain() + "</b><br>";
                            Thread newThread = new Thread(ChatHub.SendEmail_Doom);
                            newThread.Start(mailDoc);
                            //________________________________Send mail END
                            sMess = "Account activated";
                        }
                    }
                    else
                    {
                        sMess = "Invalid activation key";
                    }
                }
            }
            catch (MySqlException ex)
            {
                sMess = "Failed to activate - " + ex.Message;
            }




            if (port.Length > 0) port = ":" + port;
            //return Redirect("http://" + domain + port + "/pages/login?mess=" + sMess);
            return Redirect("http://" + domain + port + "?mess=" + sMess);
        }



        [HttpPost]
        public ActionResult ForgetPassword(string profile, String email)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var tripResponse = new TripResponse();
            tripResponse.status = false;
            tripResponse.result = "None";
            email = email.Trim();
            String m_Email = "";

            /*
            if (!IsValidEmail(email))
            {
                tripResponse.result = "Please provide a valid email";
                return Json(tripResponse, JsonRequestBehavior.AllowGet);
            }
            */
            if (email.Length == 0)
            {
                tripResponse.result = "<span style='font-weight:bold;color:red;'>Provide Email, Username or Staff ID and try.</span>";
                return Json(tripResponse, JsonRequestBehavior.AllowGet);
            }
            //__________________________________________
            try
            {
                string sSQL = "", m_Password = "", m_Firstname = "", m_Mobile = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_Username, m_Email, m_FName,m_MName,m_LName, m_Password, m_Status, m_Profile, m_StaffID,m_Mobile FROM " + MyGlobal.activeDB + ".tbl_staffs " +
      "where (m_Username = '" + email + "' or m_Email = '" + email + "' or m_StaffID = '" + email + "' or m_Mobile = '" + email + "');";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_Password"] != null) m_Password = reader["m_Password"].ToString();
                                    if (reader["m_FName"] != null) m_Firstname = reader["m_FName"].ToString();
                                    if (reader["m_Email"] != null) m_Email = reader["m_Email"].ToString();
                                    if (reader["m_Mobile"] != null) m_Mobile = reader["m_Mobile"].ToString();
                                }
                            }
                        }
                    }
                    //__________________
                    if (m_Password.Length > 0)
                    {
                        string ret = "";
                        MailDoc mailDoc = new MailDoc();
                        mailDoc.m_To = m_Email;
                        mailDoc.Domain = MyGlobal.GetDomain();
                        mailDoc.m_Subject = MyGlobal.GetDomain() + " Forget password response";
                        mailDoc.m_Body = "Hi <b>" + m_Firstname + "</b><br><br>" +
"Your password to login in " + MyGlobal.GetDomain() + " portal is  '<b>" + m_Password + "</b>'<br><br>" +
"Thank you for using our service.<br>" +
"<b>" + MyGlobal.GetDomain() + "</b><br>";
                        //Thread newThread = new Thread(ChatHub.SendEmail_Doom);
                        Thread newThread = new Thread(ChatHub.SendEmail_MeterBox);
                        newThread.Start(mailDoc);
                        ret += "by Email ";
                        //---------------------------
                        if (m_Mobile.Length > 0)
                        {
                            MyGlobal.SendSMS("+91", m_Mobile, "Dear " + m_Firstname + ". Your '" + MyGlobal.GetDomain() + "' password is " + m_Password + ". Thanks for using our service.");
                            tripResponse.result = "<span style='font-weight:bold;color:bold;color:blue;'>Password sent as email & SMS. Thanks.</span>";
                        }
                        else
                        {
                            tripResponse.result = "<span style='font-weight:bold;color:bold;color:blue;'>Password sent as email. Thanks.</span>";
                        }
                        //---------------------------
                        tripResponse.status = true;
                    }
                    else
                    {
                        tripResponse.result = "<span style='font-weight:bold;color:bold;color:red;'>Sorry. No account associated with this info</span>";
                    }
                }
            }
            catch (MySqlException ex)
            {
                tripResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________

            return Json(tripResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult onChangePassword(string profile, string email, string password,
            string pass_new, string pass_repeat, string pass_old)

        {

            //--- new code for password validation as per Policy---- start
            string SpecialChar = @"!@#$%^&*()-=_+[]{}|;:'""<>,.?/";
            int MinUpperCase = 1;
            int MinLowerCase = 1;
            int MinDigits = 1;
            int MinSpecialCharacters = 1;
            int MinLength = 8;
            int MaxLength = 20;
            //string UserEmail = email.Substring(0, Math.Min(5, email.Length));
            string Account = email;
            //-- as per new policy ---- end


            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            if (pass_new.Length == 0 || pass_repeat.Length == 0 || pass_old.Length == 0)
            {
                postResponse.result = "Need all fields";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            // -- New Policy checking start
            if (pass_new.Length < MinLength || pass_new.Length > MaxLength)
            {
                postResponse.result = "Need 8 characters minimum";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            // Check for at least one uppercase letter
            if (pass_new.Count(char.IsUpper) < MinUpperCase)
            {
                postResponse.result = "Password must contain at least one uppercase letter(s).";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            // Check for at least one lowercase letter
            if (pass_new.Count(char.IsLower) < MinLowerCase)
            {
                postResponse.result = "Password must contain at least one lowercase letter(s).";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            // Check for at least one digit
            if (pass_new.Count(char.IsDigit) < MinDigits)
            {
                postResponse.result = "Password must contain at least 8 digit(s).";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            // Check for at least one special character
            if (pass_new.Count(c => SpecialChar.Contains(c)) < MinSpecialCharacters)
            {
                postResponse.result = "Password must contain at least 1 special character(s).";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }




            //Starts of New password compare with email 20-01-2024 by Sivaguru M CHC1704


            const int consecutiveCount = 5; // Minimum consecutive characters required

            for (int i = 0; i < email.Length - (consecutiveCount - 1); i++)
            {
                string consecutiveSubstring = email.Substring(i, consecutiveCount);

                if (pass_new.Contains(consecutiveSubstring))
                {
                    //throw new Exception($"Password should not contain {consecutiveCount} consecutive characters from the email.");

                    postResponse.result = "Password should not contains same as Name!";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }

            //Ends of New password compare with email

            //Starts checks the new password contains changed in previous 2 year or not 22-01-2024 by Sivaguru M CHC1704

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //"select m_Password from " + MyGlobal.activeDB + ".tbl_staffs " +
                    //"where m_Profile='" + profile + "' and m_Email='" + email + "'";

                    string previousPwdCheck = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_login_activity " + "where m_Profile='" + profile + "' and m_Email='" + email + "'" + " AND m_PwdUpdated = '" + pass_new + "' AND m_PwdUpDateTime >= NOW() - INTERVAL 2 YEAR";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(previousPwdCheck, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader.IsDBNull(0))
                                    {

                                    }
                                    else
                                    {
                                        postResponse.result = "Previously used password will not be use again!!!";
                                        return Json(postResponse, JsonRequestBehavior.AllowGet);

                                    }
                                }


                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            //Ends checks the new password contains changed in previous 2 year or not

            //CheckConsecutiveCharacters(email, pass_new);



            //check email and password is not same

            //if (pass_new.IsNotSequentialChars(Account, 2))
            //{
            //    postResponse.result = "Password should not contains same as email.";
            //    return Json(postResponse, JsonRequestBehavior.AllowGet);
            //}
            // -- New Policy checking end

            if (!pass_new.Equals(pass_repeat))
            {
                postResponse.result = "New password does not match";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            try
            {

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQLUpdate = "";
                    string sSQLActivityUpdate = "";         //22-01-2024 by Sivaguru M CHC1704
                    string sSQL = "select m_Password from " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Profile='" + profile + "' and m_Email='" + email + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader.IsDBNull(0))
                                    {
                                        postResponse.result = "Invalid account";
                                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                                    }
                                    else
                                    {
                                        if (reader.GetString(0).Equals(pass_old))
                                        {
                                            //sSQLUpdate = "update " + MyGlobal.activeDB + ".tbl_staffs " +
                                            //    "Set m_Password='" + pass_new + "' " +
                                            //    "where m_Profile='" + profile + "' and m_Email='" + email + "'";

                                            //Starts Update the Date time of password changed 20-01-2024 by Sivaguru M CHC1704 


                                            sSQLUpdate = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs " +
                                            "SET m_Password='" + pass_new + "', " +
                                            "m_PwdUpDateTime=NOW() " +
                                            "WHERE m_Profile='" + profile + "' AND m_Email='" + email + "'";

                                            //Ends Update the Date time of password changed

                                            //Starts Insert the Date time of password changed in tbl_login_activity 22-01-2024 by Sivaguru M CHC1704 

                                            sSQLActivityUpdate = "UPDATE " + MyGlobal.activeDB + ".tbl_login_activity " +
                                            "SET m_PwdUpdated='" + pass_new + "', m_PwdUpDateTime=NOW() " +
                                            "WHERE m_Profile='" + profile + "' AND m_email='" + email + "' AND m_Activity='SignIn' " +
                                            "ORDER BY m_Time DESC LIMIT 1";

                                            //Ends Insert the Date time of password changed in tbl_login_activity
                                        }
                                        else
                                        {
                                            postResponse.result = "Invalid Old Password";
                                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                postResponse.result = "No account exists";
                                return Json(postResponse, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    if (sSQLUpdate.Length > 0)
                    {
                        //Starts Insert Updated Password to tbl_LoginActivity 22-01-2024 by Sivaguru M CHC1704 
                        using (MySqlCommand comNew = new MySqlCommand(sSQLActivityUpdate, con))
                        {
                            comNew.ExecuteNonQuery();
                        }
                        //Ends

                        using (MySqlCommand com = new MySqlCommand(sSQLUpdate, con))
                        {


                            com.ExecuteNonQuery();
                            postResponse.status = true;
                            postResponse.result = "Updated";
                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                postResponse.result = "Unknown Error";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SignIn(String email, String password)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loginResponse = new LoginResponse();

            loginResponse.status = false;
            loginResponse.result = "";

            loginResponse.m_Email = "";
            string sStatus = "";
            string sSQL = "";
            string profile = "";
            string m_Status = "";

            //Test Starts chc1704 Sivaguru M on 06-04-2024
            //string testBirthdayDate = DateTime.Now.ToString("yyyy-MM-dd");
            List<string> stringCollection = new List<string>();
            try
            {

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {

                    con.Open();
                    //sSQL = "SELECT m_FName,m_Base,m_Team FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                    //"where m_Status = 'active' and (DATE_FORMAT(m_DOB, \"%m%d\") = DATE_FORMAT(CURDATE(), \"%m%d\")) ";
                    sSQL = "SELECT m_FName,m_Base,m_Team FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Status = 'active' and (DATE_FORMAT(m_DOB, \"%m%d\") = DATE_FORMAT(CURDATE(), \"%m%d\")) ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    stringCollection.Add(reader.GetString(0));
                                    stringCollection.Add(reader.GetString(1));
                                    stringCollection.Add(reader.GetString(2));

                                    //Testing Starts on 25-04-2024 by Sivaguru M CHC1704
                                    stringCollection.Add("|");
                                    //Testing Ends
                                }
                                //Testing Starts on 25-04-2024 by Sivaguru M CHC1704
                                stringCollection.RemoveAt(stringCollection.Count - 1);
                                //Testing Ends
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {

            }





            // Concatenate the strings with '|' separator
            //string concatenatedString = string.Join("|", stringCollection);//commented on 25-04-2024 by Sivaguru M
            //Test Ends

            //Testing Starts on 25-04-2024 by Sivaguru M CHC1704
            // Joining elements with custom separator
            string concatenatedString = string.Join(" - ", stringCollection);

            // Adding additional separator between two sets of data
            concatenatedString = concatenatedString.Replace(" - | - ", " | ");

            // Adding additional separator between two sets of data if necessary
            concatenatedString = concatenatedString.Replace("| - ", "| ");
            //Testing Ends


            try
            {

                //---

                Boolean passlenth;
                passlenth = Check_Pass(password);
                if (passlenth == true)
                {

                    //--

                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        string remarks = "";
                        //m_Status = "";
                        bool bCheckPassword = true;

                    //Select additionally m_PwdUpDateTime by latest 20-01-2024 by Sivaguru M CHC1704
                    ByPass:

                        sSQL = "SELECT m_Username,m_Email,m_FName as m_Name,m_Password,m_Status,m_Profile,m_StaffID,m_MenuKey,m_Mobile,m_PwdUpDateTime FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where (m_Username='" + email + "' or m_Email='" + email + "' or m_StaffID='" + email + "' or m_Mobile='" + email + "') ";

                        //Testing for AWS Starts 05-03-2024 by Sivaguru M CHC1704
                        //sSQL = "SELECT m_Username,m_Email,m_FName as m_Name,m_Password,m_Status,m_Profile,m_StaffID,m_MenuKey,m_Mobile,m_PwdUpDateTime FROM meterbox.tbl_staffs " +
                        //"where (m_Username='" + email + "' or m_Email='" + email + "' or m_StaffID='" + email + "' or m_Mobile='" + email + "') ";
                        // Testing Ends
                        if (bCheckPassword) sSQL += "and m_Password='" + password + "';";


                        //left join " + MyGlobal.activeDB + ".tbl_profiles as profile on profile.m_Name=usr.m_Profile 
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {

                                        pwdUpdatedDate = (DateTime)reader["m_PwdUpDateTime"];//20-01-2024 by Sivaguru M CHC1704
                                        sStatus += "2";
                                        if (reader["m_Email"] != null) loginResponse.m_Email = reader["m_Email"].ToString();
                                        if (reader["m_Name"] != null) loginResponse.m_Firstname = reader["m_Name"].ToString();
                                        if (reader["m_Profile"] != null) loginResponse.m_Profile = reader["m_Profile"].ToString();
                                        if (reader["m_Username"] != null) loginResponse.m_Username = reader["m_Username"].ToString();
                                        if (reader["m_StaffID"] != null) loginResponse.m_StaffID = reader["m_StaffID"].ToString();
                                        if (reader["m_Status"] != null) sStatus = reader["m_Status"].ToString();
                                        if (reader["m_MenuKey"] != null) loginResponse.m_MenuKey = reader["m_MenuKey"].ToString();
                                        if (reader["m_Profile"] != null) profile = reader["m_Profile"].ToString();

                                    }
                                    loginResponse.status = true;
                                }
                            }
                        }
                        if (!loginResponse.status && bCheckPassword)
                        {
                            sSQL = "SELECT m_Username,m_Email,m_FName as m_Name,m_Password,m_Status,m_Profile,m_StaffID,m_MenuKey,m_Mobile FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_MenuKey like 'a0-1,%' and m_Password='" + password + "' " +
                            "and (m_StaffID='10000' or m_StaffID='CHC0001');";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            bCheckPassword = false;
                                            remarks = "admin login (" + (reader.IsDBNull(reader.GetOrdinal("m_StaffID")) ? "" : reader.GetString(reader.GetOrdinal("m_StaffID"))) + ")";
                                            goto ByPass;
                                        }
                                    }
                                }
                            }
                        }
                        //__________________________________________
                        if (loginResponse.status)
                        {
                            //Test Starts chc1704 Sivaguru M on 06-04-2024
                            if (concatenatedString != "")
                            {
                                loginResponse.birthdayResult = "Happy Birthday HW Team:) " + concatenatedString;
                            }
                            //Test Ends

                            //Starts of password expiry date checking 20-01-2024 by Sivaguru M CHC1704

                            //DateTime databaseDateTime = pwdUpdatedDate;

                            // Get the current datetime
                            DateTime currentDateTime = DateTime.Now;

                            // Calculate the difference in days
                            TimeSpan difference = currentDateTime - pwdUpdatedDate;
                            int differenceInDays = (int)difference.TotalDays;

                            //Starts Password Expiry alert to user 30-01-2024 by Sivaguru M CHC1704
                            //int diffStarts = 85;
                            //int diffEnds = 89;
                            if (differenceInDays >= 85 && differenceInDays <= 89)
                            {

                                differenceInDays = 90 - differenceInDays;

                                if (differenceInDays > 1 && differenceInDays != 0)
                                {

                                    loginResponse.result = "Your Password will expired in '" + differenceInDays + "' days, please change your password soon!";
                                }
                                else
                                {

                                    loginResponse.result = "Your Password will expired in '" + differenceInDays + "' day, please change your password soon!";
                                }
                                //loginResponse.result = "<span style='font-weight:bold;color:red;'>Your Password will expired in '"+ differenceInDays +"' days, please change your password soon!</span>";

                                //loginResponse.status = false;
                                //Thread.Sleep(10000);
                                //loginResponse.status = true;



                                //

                            }
                            else if (differenceInDays == 90)
                            {
                                loginResponse.result = "Your Password will expired today, please change your password soon!";

                            }
                            //Thread.Sleep(10000);

                            //Ends Password Expiry alert to user

                            if (differenceInDays > 90)
                            {
                                loginResponse.result = "<span style='font-weight:bold;color:red;'>Your Password expired, please contact admin!</span>";
                                loginResponse.status = false;
                            }

                            //Ends of password expiry date checking



                            if (!sStatus.Equals("Active", StringComparison.CurrentCultureIgnoreCase)
                                && !sStatus.Equals("Trainee", StringComparison.CurrentCultureIgnoreCase))
                            {
                                loginResponse.result = "<span style='font-weight:bold;color:red;'>Account is not active</span>" +
                                    "<br><span style='font-size:smaller;'>Check your mail for activation link</span>";
                                loginResponse.status = false;
                                m_Status = "Inactive Account";
                            }
                            else
                            {
                                m_Status = "Success";
                            }
                        }
                        else
                        {
                            loginResponse.result = "<span style='font-weight:bold;color:red;'>Invalid Username or Password</span>";
                            //"<br><span style='font-size:smaller;'></span>";
                            m_Status = "Invalid Credentials";
                        }
                        //---------------------------
                        if (m_Status == "Success" && profile.Length > 0)
                        {

                            sSQL = "SELECT m_CompName,m_AttnStartDate FROM " + MyGlobal.activeDB + ".tbl_profile_info " +
                                "where m_Profile='" + profile + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            loginResponse.m_CompName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            loginResponse.m_AttnStartDate = reader.IsDBNull(1) ? 1 : reader.GetInt16(1);
                                            if (loginResponse.m_AttnStartDate < 1 || loginResponse.m_AttnStartDate > 28)
                                            {
                                                loginResponse.m_AttnStartDate = 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //------------------------------
                        //----------------Log activity

                        string sBrowser = Request.Browser.Type + "__" +
                            Request.Browser.Browser + "__" +
                            Request.Browser.Version + "__" +
                            Request.Browser.MajorVersion + "__" +
                            Request.Browser.MinorVersion + "__" +
                            Request.Browser.Platform;
                        if (sBrowser.Length > 140) sBrowser = sBrowser.Substring(0, 140);

                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_login_activity " +
                            "(m_Profile,m_User,m_Email,m_Name,m_Time,m_Activity,m_IP," +
                            "m_Browser,m_StaffID,m_Status,m_Remarks) values (" +
                            "'" + loginResponse.m_Profile + "'," +
                            "'" + email + "'," +
                            "'" + loginResponse.m_Email + "'," +
                            "'" + loginResponse.m_Firstname + "'," +
                            "Now()," +
                            "'" + "SignIn" + "'," +
                            "'" + GetIPAddress() + "'," +
                            "'" + sBrowser + "'," +
                            "'" + loginResponse.m_StaffID + "'," +
                            "'" + m_Status + "'," +
                            "'" + remarks + "')";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    //Need to write password policy code here........................................../////////////////////////////......................................................
                    //loginResponse.result = "Error- Enter your password characters as per policy!!!";
                    //Starts Changed the previous Error message to new 20-01-2024 by Sivaguru M CHC1704
                    loginResponse.result = "<span style='font-weight:bold;color:red;'>Password Policy Error!!!</span>";
                    //Ends Changed the previous Error message to new

                }
            }





            catch (MySqlException ex)
            {
                loginResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                loginResponse.result = "Error-" + ex.Message;
            }

            // loginResponse.result = "Error- Enter your password characters as per policy!!!";
            //----------------Log activity
            //DateTime currentDateTime2 = DateTime.Now;

            //// Calculate the difference in days
            //TimeSpan difference2 = currentDateTime2 - pwdUpdatedDate;
            //int differenceInDays2 = (int)difference2.TotalDays;
            //if (differenceInDays2 >= 85 && differenceInDays2 <= 89)
            //{

            //    differenceInDays2 = 90 - differenceInDays2;

            //    loginResponse.status = true;
            //    loginResponse.result = "<span style='font-weight:bold;color:red;'>Your Password will expired in '" + differenceInDays2 + "' days, please change your password soon!</span>";
            //    //loginResponse.status = false;


            //    Thread.Sleep(15000);


            //    //

            //}


            return Json(loginResponse, JsonRequestBehavior.AllowGet);

        }


        [HttpPost]
        public ActionResult SignOut(string profile, string email,
            string name, string user, string staffid)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------Log activity
                    string sBrowser = Request.Browser.Type + "__" +
        Request.Browser.Browser + "__" +
        Request.Browser.Version + "__" +
        Request.Browser.MajorVersion + "__" +
        Request.Browser.MinorVersion + "__" +
        Request.Browser.Platform;
                    if (sBrowser.Length > 140) sBrowser = sBrowser.Substring(0, 140);

                    string sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_login_activity " +
        "(m_Profile,m_User,m_Email,m_Name,m_Time,m_Activity,m_IP," +
        "m_Browser,m_StaffID,m_Status,m_Remarks) values (" +
                        "'" + profile + "'," +
                        "'" + user + "'," +
                        "'" + email + "'," +
                        "'" + name + "'," +
                        "Now()," +
                        "'" + "Signed Out" + "'," +
                        "'" + GetIPAddress() + "'," +
                        "'" + sBrowser + "'," +
                        "'" + staffid + "'," +
                        "'" + "SignOut" + "'," +
                        "'" + "" + "')";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                    }

                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("SignOut-MySqlException->" + ex.Message);
                postResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("SignOut-Exception->" + ex.Message);
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------
        protected string GetIPAddress()
        {
            string ipList = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipList))
            {
                return ipList.Split(',')[0];
            }

            return Request.ServerVariables["REMOTE_ADDR"];
        }
        [HttpPost]
        public ActionResult DownloadDetails(string profile)
        {
            var downloadDetails = new DownloadDetails();
            downloadDetails.status = false;
            downloadDetails.result = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_misc where m_Profile='" + profile + "' and " +
                    "(m_Key='download_client' or m_Key='download_driver' or m_Key='download_parcelbooking' or m_Key='download_meterbox');";
                    //m_Profile='" + profile + "' and 
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (reader["m_Key"] != null)
                                    {
                                        if (reader["m_Key"].ToString().Equals("download_client"))
                                        {
                                            downloadDetails.download_client_version = reader["m_Value1"].ToString();
                                            downloadDetails.download_client_time = reader["m_Time"].ToString();
                                        }
                                        else if (reader["m_Key"].ToString().Equals("download_driver"))
                                        {
                                            downloadDetails.download_driver_version = reader["m_Value1"].ToString();
                                            downloadDetails.download_driver_time = reader["m_Time"].ToString();
                                        }
                                        else if (reader["m_Key"].ToString().Equals("download_parcelbooking"))
                                        {
                                            downloadDetails.download_parcelbooking_version = reader["m_Value1"].ToString();
                                            downloadDetails.download_parcelbooking_time =
                                                Convert.ToDateTime(reader["m_Time"]).ToString("yyyy-MM-dd HH:mm:ss");
                                            //reader["m_Time"].ToString();
                                        }
                                        else if (reader["m_Key"].ToString().Equals("download_meterbox"))
                                        {
                                            downloadDetails.download_meterbox_version = reader["m_Value1"].ToString();
                                            downloadDetails.download_meterbox_time =
                                                Convert.ToDateTime(reader["m_Time"]).ToString("yyyy-MM-dd HH:mm:ss");
                                            //reader["m_Time"].ToString();
                                        }
                                    }
                                }
                                downloadDetails.status = true;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                downloadDetails.result = "Error-" + ex.Message;
            }
            return Json(downloadDetails, JsonRequestBehavior.AllowGet);
        }
        /*
        [HttpPost]
        public ActionResult LoadProfile_User(string profile, string username, String email, String sResponse)
        {
            var profileResponse = new ProfileResponse();
            profileResponse.status = false;
            profileResponse.result = "";
            profileResponse.m_id = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_users where " +
                    "m_Profile='" + profile + "' and (m_Username='" + username + "' or m_Email='" + email + "');";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_id"] != null) profileResponse.m_id = reader["m_id"].ToString();
                                    if (reader["m_username"] != null) profileResponse.m_Username = reader["m_username"].ToString();
                                    if (reader["m_FirstName"] != null) profileResponse.m_FirstName = reader["m_FirstName"].ToString();
                                    if (reader["m_MiddleName"] != null) profileResponse.m_MiddleName = reader["m_MiddleName"].ToString();
                                    if (reader["m_LastName"] != null) profileResponse.m_LastName = reader["m_LastName"].ToString();
                                    if (reader["m_Status"] != null) profileResponse.m_Status = reader["m_Status"].ToString();
                                    if (reader["m_UserType"] != null) profileResponse.m_UserType = reader["m_UserType"].ToString();
                                    if (reader["m_Mobile"] != null) profileResponse.m_Mobile = reader["m_Mobile"].ToString();
                                    if (reader["m_Email"] != null) profileResponse.m_Email = reader["m_Email"].ToString();
                                    if (reader["m_Address"] != null) profileResponse.m_Address = reader["m_Address"].ToString();
                                    if (reader["m_City"] != null) profileResponse.m_City = reader["m_City"].ToString();
                                    if (reader["m_Country"] != null) profileResponse.m_Country = reader["m_Country"].ToString();
                                    if (reader["m_PIN"] != null) profileResponse.m_PIN = reader["m_PIN"].ToString();
                                    if (reader["m_AboutMe"] != null) profileResponse.m_AboutMe = reader["m_AboutMe"].ToString();

                                }
                                profileResponse.status = true;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profileResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________
            if (profileResponse.m_id.Length == 0)
            {
                profileResponse.result = "Unable to get the profile details";
            }
            if (sResponse != null) if (sResponse.Length > 0) profileResponse.result = sResponse;
            return Json(profileResponse, JsonRequestBehavior.AllowGet);
        }
        */
        [HttpPost]
        public ActionResult LoadProfile(string profile, string username, String email, String sResponse)
        {
            var profileResponse = new ProfileResponse();
            profileResponse.status = false;
            profileResponse.result = "";
            profileResponse.m_id = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "m_Profile='" + profile + "' and (m_Username='" + username + "' or m_Email='" + email + "');";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_id"] != null) profileResponse.m_id = reader["m_id"].ToString();
                                    if (reader["m_username"] != null) profileResponse.m_Username = reader["m_username"].ToString();
                                    if (reader["m_FName"] != null) profileResponse.m_FirstName = reader["m_FName"].ToString();
                                    if (reader["m_MName"] != null) profileResponse.m_MiddleName = reader["m_MName"].ToString();
                                    if (reader["m_LName"] != null) profileResponse.m_LastName = reader["m_LName"].ToString();
                                    if (reader["m_Status"] != null) profileResponse.m_Status = reader["m_Status"].ToString();
                                    if (reader["m_Type"] != null) profileResponse.m_UserType = reader["m_Type"].ToString();
                                    if (reader["m_Mobile"] != null) profileResponse.m_Mobile = reader["m_Mobile"].ToString();
                                    if (reader["m_Email"] != null) profileResponse.m_Email = reader["m_Email"].ToString();
                                    if (reader["m_Address1"] != null) profileResponse.m_Address = reader["m_Address1"].ToString();
                                    if (reader["m_City"] != null) profileResponse.m_City = reader["m_City"].ToString();
                                    if (reader["m_Country"] != null) profileResponse.m_Country = reader["m_Country"].ToString();
                                    if (reader["m_StaffID"] != null) profileResponse.m_StaffID = reader["m_StaffID"].ToString();
                                    if (reader["m_ReportToFunctional"] != null) profileResponse.m_ReportToFunctional = reader["m_ReportToFunctional"].ToString();
                                    if (reader["m_Base"] != null) profileResponse.m_Base = reader["m_Base"].ToString();
                                    if (reader["m_Team"] != null) profileResponse.m_Team = reader["m_Team"].ToString();


                                    //if (reader["m_PIN"] != null) profileResponse.m_PIN = reader["m_PIN"].ToString();
                                    //if (reader["m_AboutMe"] != null) profileResponse.m_AboutMe = reader["m_AboutMe"].ToString();

                                }
                                profileResponse.status = true;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profileResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________
            if (profileResponse.m_id.Length == 0)
            {
                profileResponse.result = "Unable to get the profile details";
            }
            if (sResponse != null) if (sResponse.Length > 0) profileResponse.result = sResponse;
            return Json(profileResponse, JsonRequestBehavior.AllowGet);
        }
        private string GetUpdateStringKey(String SQL, String sField, String sValue)
        {
            if (sValue != null)
            {
                if (SQL.Length > 0) SQL += ",";
                SQL += sField + "='" + sValue + "'";
            }
            return SQL;
        }
        private Boolean DoesThisUsernameAlreadyExists(string profile, MySqlConnection con, String m_Username, String m_id)
        {
            if (m_Username != null)
            {
                String sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs where " +
"m_Profile='" + profile + "' and m_Username='" + m_Username + "'";
                if (m_id.Length > 0) sSQL += " and m_id !='" + m_id + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
            return false;
        }



        /*
        public ActionResult UpdateProfile_User(string profile, String m_id,
            String m_Username, String m_FirstName, String m_MiddleName, String m_LastName,
            String m_Mobile, String m_Address, String m_City, String m_Country, String m_PIN,
            String m_AboutMe)
        {
            var profileResponse = new ProfileResponse();
            profileResponse.status = false;
            profileResponse.result = "None";
            Boolean bRecordExists = false;
            profileResponse.m_id = m_id;
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //___________________________________________________
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_users where " +
                    "m_Profile='" + profile + "' and m_id='" + m_id + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_Email"] != null) profileResponse.m_Email = reader["m_Email"].ToString();
                                    bRecordExists = true;
                                }
                            }
                        }
                    }
                    sSQL = "";
                    //_______________________
                    if (bRecordExists)
                    {
                        if (DoesThisUsernameAlreadyExists(profile, con, m_Username, m_id))
                        {
                            return LoadProfile(profile, m_Username, profileResponse.m_Email, "Username already exists");
                        }
                        String SQL = "";
                        SQL = GetUpdateStringKey(SQL, "m_Username", m_Username);
                        SQL = GetUpdateStringKey(SQL, "m_FirstName", m_FirstName);
                        SQL = GetUpdateStringKey(SQL, "m_MiddleName", m_MiddleName);
                        SQL = GetUpdateStringKey(SQL, "m_LastName", m_LastName);
                        SQL = GetUpdateStringKey(SQL, "m_Mobile", m_Mobile);
                        SQL = GetUpdateStringKey(SQL, "m_Address", m_Address);
                        SQL = GetUpdateStringKey(SQL, "m_City", m_City);
                        SQL = GetUpdateStringKey(SQL, "m_Country", m_Country);
                        SQL = GetUpdateStringKey(SQL, "m_PIN", m_PIN);
                        SQL = GetUpdateStringKey(SQL, "m_AboutMe", m_AboutMe);

                        if (SQL.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_users Set " + SQL + " where m_id='" + m_id + "'";
                        }
                    }
                    else
                    {
                        profileResponse.result = "No valid record to update";
                        
                        //if (DoesThisUsernameAlreadyExists(con, m_Username, ""))
                        //{
                          //  return LoadProfile(profileResponse.m_Email, "Username already exists");
                        //}
                        //sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_users () values ();";
                        
                    }
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            profileResponse.status = true;
                            //profileResponse.result = "Updated";
                            return LoadProfile(profile, m_Username, profileResponse.m_Email, "Updated");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profileResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________
            return LoadProfile(profile, m_Username, profileResponse.m_Email, "");
            //return Json(profileResponse, JsonRequestBehavior.AllowGet);
        }
        */

        private static bool Check_Pass(string password2)
        {
            int MinLength = 8;
            int MaxLength = 20;
            string SpecialCharacters = @"!@#$%^&*()-=_+[]{}|;:'""<>,.?/";
            int MinUpperCase = 1;
            int MinLowerCase = 1;
            int MinDigits = 1;
            int MinSpecialCharacters = 1;

            if (password2.Length < MinLength || password2.Length > MaxLength)
            {
                Console.WriteLine("Password must be between {0} and {1} characters long.", MinLength, MaxLength);
                return false;
            }

            // Check for at least one uppercase letter
            if (password2.Count(char.IsUpper) < MinUpperCase)
            {
                Console.WriteLine("Password must contain at least {0} uppercase letter(s).", MinUpperCase);
                return false;
            }

            // Check for at least one lowercase letter
            if (password2.Count(char.IsLower) < MinLowerCase)
            {
                Console.WriteLine("Password must contain at least {0} lowercase letter(s).", MinLowerCase);
                return false;
            }

            // Check for at least one digit
            if (password2.Count(char.IsDigit) < MinDigits)
            {
                Console.WriteLine("Password must contain at least {0} digit(s).", MinDigits);
                return false;
            }

            // Check for at least one special character
            if (password2.Count(c => SpecialCharacters.Contains(c)) < MinSpecialCharacters)
            {
                Console.WriteLine("Password must contain at least {0} special character(s).", MinSpecialCharacters);
                return false;
            }
            return true;

        }


        public ActionResult UpdateProfile(string profile, String m_id,
    String m_Username, String m_FirstName, String m_MiddleName, String m_LastName,
    String m_Mobile, String m_Address, String m_City, String m_Country, String m_PIN,
    String m_AboutMe)
        {
            var profileResponse = new ProfileResponse();
            profileResponse.status = false;
            profileResponse.result = "None";
            Boolean bRecordExists = false;
            profileResponse.m_id = m_id;
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //___________________________________________________
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "m_Profile='" + profile + "' and m_id='" + m_id + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["m_Email"] != null) profileResponse.m_Email = reader["m_Email"].ToString();
                                    bRecordExists = true;
                                }
                            }
                        }
                    }
                    sSQL = "";
                    //_______________________
                    if (bRecordExists)
                    {
                        if (DoesThisUsernameAlreadyExists(profile, con, m_Username, m_id))
                        {
                            return LoadProfile(profile, m_Username, profileResponse.m_Email, "Username already exists");
                        }
                        String SQL = "";
                        SQL = GetUpdateStringKey(SQL, "m_Username", m_Username);
                        SQL = GetUpdateStringKey(SQL, "m_FName", m_FirstName);
                        SQL = GetUpdateStringKey(SQL, "m_MName", m_MiddleName);
                        SQL = GetUpdateStringKey(SQL, "m_LName", m_LastName);
                        SQL = GetUpdateStringKey(SQL, "m_Mobile", m_Mobile);
                        SQL = GetUpdateStringKey(SQL, "m_Address1", m_Address);
                        SQL = GetUpdateStringKey(SQL, "m_City", m_City);
                        SQL = GetUpdateStringKey(SQL, "m_Country", m_Country);
                        //SQL = GetUpdateStringKey(SQL, "m_PIN", m_PIN);
                        //SQL = GetUpdateStringKey(SQL, "m_AboutMe", m_AboutMe);

                        if (SQL.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs Set " + SQL + " where m_id='" + m_id + "'";
                        }
                    }
                    else
                    {
                        profileResponse.result = "No valid record to update";
                        /*
                        if (DoesThisUsernameAlreadyExists(con, m_Username, ""))
                        {
                            return LoadProfile(profileResponse.m_Email, "Username already exists");
                        }
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_users () values ();";
                        */
                    }
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            profileResponse.status = true;
                            //profileResponse.result = "Updated";
                            return LoadProfile(profile, m_Username, profileResponse.m_Email, "Updated");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profileResponse.result = "Error-" + ex.Message;
            }
            //__________________________________________
            return LoadProfile(profile, m_Username, profileResponse.m_Email, "");
            //return Json(profileResponse, JsonRequestBehavior.AllowGet);
        }
        private static Int16 GetInt16(String sIn)
        {
            Int16 i = 0;
            if (Int16.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }



        //[HttpPost]
        public ActionResult GetVehicles(string profile, string sort, string order, string page, string search, string timezone)
        {
            var vehiclesResponse = new VehiclesResponse();
            vehiclesResponse.status = false;
            vehiclesResponse.result = "None";
            vehiclesResponse.total_count = "";
            string sSQL = "";
            Int16 iTimeZone;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (" +
                        "cars.m_id like '%" + search + "%' or " +
                        "cars.m_Make like '%" + search + "%' or " +
                        "cars.m_Model like '%" + search + "%' or " +
                        "assign.m_FleetID like '%" + search + "%' or " +
                        "assign.m_DriverID1 like '%" + search + "%' or " +
                        "assign.m_DeviceIMEI like '%" + search + "%' or " +
                        "assign.m_RegNo like '%" + search + "%') ";


                    sSQL = "select count(cars.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_cars as cars ";
                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_CarID = cars.m_id and assign.m_Profile='" + profile + "' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_drivers driver on driver.m_StaffID = assign.m_DriverID1 and driver.m_Profile='grey' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_devices device on device.m_IMEI = assign.m_DeviceIMEI and device.m_Profile='grey' ";
                    sSQL += "where " + sSearchKey + " and cars.m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) vehiclesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_DOR";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    //sort = "cars." + sort;
                    //  where m_Profile='grey' 
                    //sSQL = "SELECT *,device.m_Make as m_Make_device,device.m_Model as m_Model_device FROM " + MyGlobal.activeDB + ".tbl_cars as cars ";
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_cars as cars ";
                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_CarID = cars.m_id and assign.m_Profile='" + profile + "' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_drivers driver on driver.m_StaffID = assign.m_DriverID1 and driver.m_Profile='grey' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_devices device on device.m_IMEI = assign.m_DeviceIMEI and device.m_Profile='grey' ";
                    sSQL += "where " + sSearchKey + " and cars.m_Profile='" + profile + "'"; // and assign.m_Profile='grey'
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    VehicleItem vehicleItem = new VehicleItem();
                                    //_______________________________________________________Vehicle
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) vehicleItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) vehicleItem.m_Make = reader["m_Make"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) vehicleItem.m_Model = reader["m_Model"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOR")))
                                        vehicleItem.m_DOR = Convert.ToDateTime(reader["m_DOR"]).ToString("yyyy-MM-dd");
                                    //_______________________________________________________Assignment
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DriverID1"))) vehicleItem.m_DriverID1 = reader["m_DriverID1"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DeviceIMEI"))) vehicleItem.m_DeviceIMEI = reader["m_DeviceIMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_RegNo"))) vehicleItem.m_RegNo = reader["m_RegNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FleetID"))) vehicleItem.m_FleetID = reader["m_FleetID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Group"))) vehicleItem.m_Group = reader["m_Group"].ToString();
                                    /*
                                    //_______________________________________________________Driver
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) vehicleItem.m_FName = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) vehicleItem.m_Mobile = reader["m_Mobile"].ToString();
                                    //_______________________________________________________Device
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_SIMMobileNo"))) vehicleItem.m_SIMMobileNo = reader["m_SIMMobileNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Make_device"))) vehicleItem.m_Make_device = reader["m_Make_device"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Model_device"))) vehicleItem.m_Model_device = reader["m_Model_device"].ToString();
                                    */
                                    vehiclesResponse.items.Add(vehicleItem);
                                }
                                vehiclesResponse.status = true;
                                vehiclesResponse.result = "Done";
                            }
                            else
                            {
                                vehiclesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                vehiclesResponse.result = "Error-" + ex.Message + "[" + sSQL + "]";
            }
            return Json(vehiclesResponse, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public ActionResult getMobileUsers(string profile, string sort, string order, string page, string search, string timezone)
        {
            var mobileUsersResponse = new MobileUsersResponse();
            mobileUsersResponse.status = false;
            mobileUsersResponse.result = "None";
            mobileUsersResponse.total_count = "";
            string sSQL = "";
            Int16 iTimeZone;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (" +
                        "user.m_IMEI like '%" + search + "%' or " +
                        "user.m_Name like '%" + search + "%' or " +
                        "user.m_MobileNo like '%" + search + "%' or " +
                        "user.m_Version like '%" + search + "%' or " +
                        "user.m_LinkedProfile like '%" + search + "%') ";


                    sSQL = "select count(user.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_devices as user ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_CarID = cars.m_id and assign.m_Profile='" + profile + "' ";
                    sSQL += "where " + sSearchKey + " and user.m_Type='3' and user.m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) mobileUsersResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_CreatedTime";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    //sort = "cars." + sort;
                    //  where m_Profile='grey' 
                    //sSQL = "SELECT *,device.m_Make as m_Make_device,device.m_Model as m_Model_device FROM " + MyGlobal.activeDB + ".tbl_cars as cars ";
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_devices as user ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_CarID = cars.m_id and assign.m_Profile='" + profile + "' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_drivers driver on driver.m_StaffID = assign.m_DriverID1 and driver.m_Profile='grey' ";
                    //sSQL += "left join " + MyGlobal.activeDB + ".tbl_devices device on device.m_IMEI = assign.m_DeviceIMEI and device.m_Profile='grey' ";
                    sSQL += "where " + sSearchKey + " and user.m_Type='3' and user.m_Profile='" + profile + "' "; // and assign.m_Profile='grey'
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    MobileUser mobileUserItem = new MobileUser();
                                    //_______________________________________________________Vehicle
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) mobileUserItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IMEI"))) mobileUserItem.m_IMEI = reader["m_IMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) mobileUserItem.m_Name = reader["m_Name"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MobileNo"))) mobileUserItem.m_Mobile = reader["m_MobileNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTime")))
                                        mobileUserItem.m_CreatedTime = Convert.ToDateTime(reader["m_CreatedTime"]).ToString("yyyy-MM-dd");
                                    //_______________________________________________________Assignment
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Version"))) mobileUserItem.m_Version = reader["m_Version"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) mobileUserItem.m_Status = reader["m_Status"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_LinkedProfile"))) mobileUserItem.m_LinkedProfile = reader["m_LinkedProfile"].ToString();

                                    mobileUsersResponse.items.Add(mobileUserItem);
                                }
                                mobileUsersResponse.status = true;
                                mobileUsersResponse.result = "Done";
                            }
                            else
                            {
                                mobileUsersResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                mobileUsersResponse.result = "Error-" + ex.Message + "[" + sSQL + "]";
            }
            return Json(mobileUsersResponse, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        public ActionResult GetDrivers(string profile, string sort, string order, string page, string search, string timezone)
        {
            var profilesResponse = new ProfilesResponse();
            profilesResponse.status = false;
            profilesResponse.result = "None";
            profilesResponse.total_count = "";

            Int16 iTimeZone;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_FName like '%" + search + "%' or " +
                        "m_StaffID like '%" + search + "%' or " +
                        "m_Country like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_drivers " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) profilesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_drivers ";
                    sSQL += "where " + sSearchKey + " and m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ProfileItem profileItem = new ProfileItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) profileItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) profileItem.m_FName = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) profileItem.m_StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Country"))) profileItem.m_Country = reader["m_Country"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) profileItem.m_Mobile = reader["m_Mobile"].ToString();


                                    profilesResponse.items.Add(profileItem);
                                }
                                profilesResponse.status = true;
                                profilesResponse.result = "Done";
                            }
                            else
                            {
                                profilesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profilesResponse.result = "Error-" + ex.Message;
            }
            return Json(profilesResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetUsers(string profile, string sort, string order, string page, string search, string timezone)
        {
            var profilesResponse = new UsersResponse();
            profilesResponse.status = false;
            profilesResponse.result = "None";
            profilesResponse.total_count = "";

            Int16 iTimeZone;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_FName like '%" + search + "%' or " +
                        "m_LName like '%" + search + "%' or " +
                        "m_Email like '%" + search + "%' or " +
                        "m_Country like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) profilesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs ";
                    sSQL += "where " + sSearchKey + " and m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    UserItem profileItem = new UserItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) profileItem.m_id = reader["m_id"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) profileItem.m_FirstName = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MName"))) profileItem.m_MiddleName = reader["m_MName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_LName"))) profileItem.m_LastName = reader["m_LName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) profileItem.m_Status = reader["m_Status"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) profileItem.m_Email = reader["m_Email"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) profileItem.m_Mobile = reader["m_Mobile"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Address"))) profileItem.m_Address = reader["m_Address"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_City"))) profileItem.m_City = reader["m_City"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Country"))) profileItem.m_Country = reader["m_Country"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Pin"))) profileItem.m_Pin = reader["m_Pin"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_AboutMe"))) profileItem.m_AboutMe = reader["m_AboutMe"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Username"))) profileItem.m_Username = reader["m_Username"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MenuKey"))) profileItem.m_MenuKey = reader["m_MenuKey"].ToString();

                                    profilesResponse.items.Add(profileItem);
                                }
                                profilesResponse.status = true;
                                profilesResponse.result = "Done";
                            }
                            else
                            {
                                profilesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                profilesResponse.result = "Error-" + ex.Message;
            }
            return Json(profilesResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetDevices(string profile, string sort, string order, string page, string search, string timezone)
        {
            var devicesResponse = new DevicesResponse();
            devicesResponse.status = false;
            devicesResponse.result = "None";
            devicesResponse.total_count = "";

            Int16 iTimeZone;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_IMEI like '%" + search + "%' or " +
                        "m_SIMMobileNo like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_devices " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) devicesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_IMEI";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='" + profile + "' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_devices ";
                    sSQL += "where " + sSearchKey + " and m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    DeviceItem deviceItem = new DeviceItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) deviceItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IMEI"))) deviceItem.m_IMEI = reader["m_IMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) deviceItem.m_Make = reader["m_Make"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) deviceItem.m_Model = reader["m_Model"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_SIMMobileNo"))) deviceItem.m_SIMMobileNo = reader["m_SIMMobileNo"].ToString();

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Time")))
                                    {
                                        deviceItem.m_Time = JavaTimeStampToDateTime(reader.GetInt32(reader.GetOrdinal("m_Time"))).ToString("yyyy-MM-dd hh:mm:ss");
                                        //deviceItem.m_Time = Convert.ToDateTime(reader["m_Time"]).ToString("yyyy-MM-dd hh:mm:ss");
                                    }
                                    devicesResponse.items.Add(deviceItem);
                                }
                                devicesResponse.status = true;
                                devicesResponse.result = "Done";
                            }
                            else
                            {
                                devicesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                devicesResponse.result = "Error-" + ex.Message;
            }
            return Json(devicesResponse, JsonRequestBehavior.AllowGet);
        }
        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            //dtDateTime = dtDateTime.AddMilliseconds(javaTimeStamp).ToLocalTime();
            dtDateTime = dtDateTime.AddSeconds(javaTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        [HttpPost]
        public ActionResult Update_Devices(string mode, string m_imei, string m_Make, string m_Model, string m_SIMMobileNo)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_devices Set " +
                        "m_Make='" + m_Make + "', " +
                        "m_Model='" + m_Model + "', " +
                        "m_SIMMobileNo='" + m_SIMMobileNo + "' " +
                        "where m_imei='" + m_imei + "'";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        postResponse.status = true;
                        postResponse.result = "Done";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Update_LinkedProfile(string mode, string m_id, string profile,
            string m_Name, string m_Mobile, string m_Status, string m_LinkedProfile)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------Is Linked Profile valid?
                    /*
                    bool bEmailExists = false;
                    sSQL = @"select m_Email from " + MyGlobal.activeDB + ".tbl_Users where m_Profile='" + profile + "' and m_Email='" + m_LinkedProfile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            bEmailExists = reader.HasRows;
                        }
                    }
                    //-------------------------------------------Is Linked Profile valid?
                    if (bEmailExists)
                    {*/
                    String sEmailFieldValue = "null";
                    if (m_LinkedProfile != null && m_LinkedProfile.Length > 5)
                        sEmailFieldValue = "'" + m_LinkedProfile + "'";

                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_devices Set " +
                        "m_Name='" + m_Name + "', " +
                        "m_MobileNo='" + m_Mobile + "', " +
                        "m_Status='" + m_Status + "', " +
                        "m_LinkedProfile=" + sEmailFieldValue + " " +
                        "where m_id='" + m_id + "'";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        int iRecords = com.ExecuteNonQuery();
                        postResponse.status = true;
                        if (iRecords > 0)
                        {
                            postResponse.result = "Updated";
                        }
                        else
                        {
                            postResponse.result = "Email does not exists";
                        }
                    }
                    /*
                else
                {
                    postResponse.result = "Email does not exists";
                }
                */
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        String getSQLBit(String sSQL, String sField, String sValue)
        {
            if (sValue != null)
            {
                if (sSQL.Length > 0) sSQL += ",";
                if (sValue.Length > 0)
                    sSQL += sField + " = '" + sValue + "' ";
                else
                    sSQL += sField + " = null ";
            }
            return sSQL;
        }
        String FormatName(String sIn)
        {
            if (sIn == null) return null;
            if (sIn.Length == 0) return "";
            sIn = sIn.ToUpper();
            sIn = sIn.Replace(" ", String.Empty);
            String sOut = "";
            bool bLastChracterIsNumeric = IsNumeric(sIn[0]);
            for (int i = 0; i < sIn.Length; i++)
            {
                if (IsNumeric(sIn[i]))
                {
                    if (bLastChracterIsNumeric)
                    {
                        sOut += sIn[i];
                    }
                    else
                    {
                        sOut += " " + sIn[i];
                    }
                    bLastChracterIsNumeric = true;
                }
                else
                {
                    if (bLastChracterIsNumeric)
                    {
                        sOut += " " + sIn[i];
                    }
                    else
                    {
                        sOut += sIn[i];
                    }
                    bLastChracterIsNumeric = false;
                }
            }
            return sOut;
        }
        bool IsNumeric(char ch)
        {
            return ch >= '0' && ch <= '9';
        }
        [HttpPost]
        public ActionResult Update_Vehicles(string profile, string mode, string m_id, string m_Make, string m_Model, string m_DOR, string m_RegNo, string m_FleetID, string m_DriverID1, string m_DeviceIMEI, string m_Group)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            int iDone = 0;
            string sSQL = "";
            if (mode.Equals("new"))
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        //___________________________________________________________________
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_cars Set m_DOR=Now() where m_Make='New' and m_Profile='" + profile + "';";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            iDone = com.ExecuteNonQuery();
                        }
                        if (iDone == 0) // No rows affected, so create one
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_cars (m_Profile,m_Make,m_DOR) values ('" + profile + "','New',Now());";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                iDone = com.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    postResponse.result = "Error-" + ex.Message;
                }
                if (iDone > 0)
                {
                    postResponse.status = true;
                    postResponse.result = "Created new entry";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }
            if (m_id.Length == 0)
            {
                postResponse.result = "Invalid request";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            //__________________________________________________________Above  is for New
            DateTime dt = DateTime.MinValue;
            if (m_DOR != null)
            {
                try
                {
                    dt = DateTime.ParseExact(m_DOR, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (FormatException ex)
                {
                    postResponse.result = ex.Message + " [" + m_DOR + "]";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
                catch (ArgumentNullException ex)
                {
                    postResponse.result = ex.Message + " [" + m_DOR + "]";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //___________________________________________________________________
                    String sParam = "";
                    sParam = getSQLBit(sParam, "m_Make", m_Make);
                    sParam = getSQLBit(sParam, "m_Model", m_Model);
                    if (m_DOR != null) sParam = getSQLBit(sParam, "m_DOR", dt.ToString("yyyy-MM-dd"));

                    if (sParam.Length > 0)
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_cars Set " + sParam + " where m_id=" + m_id;
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            postResponse.status = true;
                            postResponse.result = "Done";
                        }
                    }
                    //ChatHub.Log("3" + m_DriverID1+"x" + m_DeviceIMEI + "x" + m_RegNo + "x" + "x" + m_FleetID + "x" + "x" + m_Group + "x" + "\r\n");
                    //___________________________________________________________________
                    if (m_DriverID1 != null)
                    {
                        if (m_DriverID1.Length > 0)
                        {
                            sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_drivers where m_StaffID='" + m_DriverID1 + "' and m_Profile='" + profile + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        postResponse.status = false;
                                        postResponse.result = "Invalid Driver ID";
                                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                    }
                    if (m_DeviceIMEI != null)
                    {
                        if (m_DeviceIMEI.Length > 0)
                        {
                            sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_devices where m_IMEI='" + m_DeviceIMEI + "' and m_Profile='" + profile + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        postResponse.status = false;
                                        postResponse.result = "Invalid IMEI";
                                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                    }
                    //___________________________________________________________________
                    sParam = "";
                    sParam = getSQLBit(sParam, "m_DriverID1", m_DriverID1);
                    sParam = getSQLBit(sParam, "m_DeviceIMEI", m_DeviceIMEI);
                    sParam = getSQLBit(sParam, "m_RegNo", FormatName(m_RegNo));
                    sParam = getSQLBit(sParam, "m_FleetID", FormatName(m_FleetID));
                    sParam = getSQLBit(sParam, "m_Group", m_Group);

                    if (sParam.Length > 0)
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_assignment Set " + sParam + " where m_CarID='" + m_id + "' and m_Profile='" + profile + "';";

                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            iDone = com.ExecuteNonQuery();
                        }
                        if (iDone == 0) // No rows affected, so create one
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_assignment (m_Profile,";
                            if (m_DriverID1 != null) sSQL += "m_DriverID1,";
                            if (m_DeviceIMEI != null) sSQL += "m_DeviceIMEI,";
                            if (m_RegNo != null) sSQL += "m_RegNo,";
                            if (m_FleetID != null) sSQL += "m_FleetID,";
                            if (m_Group != null) sSQL += "m_Group,";
                            sSQL += "m_CarID) values ('" + profile + "',";
                            if (m_DriverID1 != null) sSQL += "'" + m_DriverID1 + "',";
                            if (m_DeviceIMEI != null) sSQL += "'" + m_DeviceIMEI + "',";
                            if (m_RegNo != null) sSQL += "'" + m_RegNo + "',";
                            if (m_FleetID != null) sSQL += "'" + m_FleetID + "',";
                            if (m_Group != null) sSQL += "'" + m_Group + "',";
                            sSQL += "'" + m_id + "');";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                iDone = com.ExecuteNonQuery();
                            }
                        }
                        postResponse.status = true;
                        postResponse.result = "Done";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Update_Drivers(string profile, string mode, string m_id, string m_FName, string m_StaffID, string m_Country, string m_Mobile)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sSQL = "";
            if (mode.Equals("new"))
            {
                int iDone = 0;
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();

                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_drivers Set m_FName='_New' where m_FName='_New' and m_Profile='" + profile + "';";
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_drivers where m_FName='_New' and m_Profile='" + profile + "';";
                        //using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        //{
                        //    iDone = com.ExecuteNonQuery();
                        //}


                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    iDone = 1;
                                }
                            }
                        }



                        if (iDone == 0) // No rows affected, so create one
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_drivers (m_Profile,m_FName) values ('" + profile + "','_New');";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                iDone = com.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    postResponse.result = "Error-" + ex.Message;
                }
                if (iDone > 0)
                {
                    postResponse.status = true;
                    postResponse.result = "Created new entry";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }
            if (m_id.Length == 0)
            {
                postResponse.result = "Invalid request-" + sSQL;
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            /*
            DateTime dt;
            try
            {
                dt = DateTime.ParseExact(m_DOR, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                postResponse.result = ex.Message + " [" + m_DOR + "]";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            catch (ArgumentNullException ex)
            {
                postResponse.result = ex.Message + " [" + m_DOR + "]";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            */
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_drivers Set m_FName='" + m_FName +
                        "',m_StaffID='" + m_StaffID + "',m_Country='" + m_Country + "',m_Mobile='" + m_Mobile + "' where m_id=" + m_id;
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        postResponse.status = true;
                        postResponse.result = "Done";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        /*
        [HttpPost]
        public ActionResult Update_Users(string profile, string mode,
                    string m_id,
        string m_FirstName,
        string m_MiddleName,
        string m_LastName,
        string m_Status,
        string m_Email,
        string m_Mobile,
        string m_Address,
        string m_City,
        string m_Country,
        string m_Pin,
        string m_AboutMe,
        string m_Username
            )
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sSQL = "";
            if (mode.Equals("new"))
            {
                int iDone = 0;
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();

                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_users where m_FirstName='_New' and m_Profile='" + profile + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    iDone = 1;
                                }
                            }
                        }



                        if (iDone == 0) // No rows affected, so create one
                        {
                            //----------------------Get default security menukey from profile
                            String sMenuKey = "";
                            sSQL = "select m_MenuKey from " + MyGlobal.activeDB + ".tbl_users " +
                                "where m_Profile='" + profile + "' and m_Email='" + profile + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) sMenuKey = reader[0].ToString();
                                        }
                                    }
                                }
                            }
                            //----------------------Create new user with the defaut info
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_users (m_Profile,m_FirstName,m_Password,m_Status,m_MenuKey) values ('" + profile + "','_New','1234','active','" + sMenuKey + "');";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                iDone = com.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    postResponse.result = "Error-" + ex.Message;
                }
                if (iDone > 0)
                {
                    postResponse.status = true;
                    postResponse.result = "Created new entry";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }
            if (m_id.Length == 0)
            {
                postResponse.result = "Invalid request-" + sSQL;
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //--------------------------------------------
                    sSQL = "select m_Username from " + MyGlobal.activeDB + ".tbl_users " +
                        "where ((m_Username='" + m_Username + "' and m_Username<>'') or (m_Email='" + m_Email + "' and m_Email<>'')) and m_id<>" + m_id;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                postResponse.result = "Username/Email already exists";
                                return Json(postResponse, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    //--------------------------------------------
                    String sqlEmail = "";
                    if (m_Email.Length < 4)
                    {
                        //sqlEmail = "null";
                        postResponse.status = false;
                        postResponse.result = "Valid email is needed";
                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        sqlEmail = "'"+ m_Email+"'";
                    }
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_users Set " +
                        "m_FirstName='" + m_FirstName + "',"+
                        "m_MiddleName='" + m_MiddleName + "'," +
                        "m_LastName='" + m_LastName + "'," +
                        "m_Status='" + m_Status + "'," +
                        "m_Email=" + sqlEmail + "," +
                        "m_Mobile='" + m_Mobile + "'," +
                        "m_Address='" + m_Address + "'," +
                        "m_City='" + m_City + "'," +
                        "m_Country='" + m_Country + "'," +
                        "m_Pin='" + m_Pin + "'," +
                        "m_AboutMe='" + m_AboutMe + "'," +
                        "m_Username='" + m_Username + "' " +
                        "where m_id=" + m_id;
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        postResponse.status = true;
                        postResponse.result = "Done";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
*/
        /*
                [HttpPost]
                public ActionResult Update_Security_user(string profile, string email,string securitystring)
                {
                    var postResponse = new PostResponse();
                    postResponse.status = false;
                    postResponse.result = "";
                    string sSQL = "";
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                        {
                            con.Open();
                            //--------------------------------------------
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_users Set " +
                                    "m_MenuKey='" + securitystring + "' " +
                                    "where m_Profile='" + profile + "' and m_Email='" + email + "';";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();

                            sSQL = @"SELECT m_MenuKey FROM " + MyGlobal.activeDB + ".tbl_users "+
                                "where m_Profile='" + profile + "' and m_Email='" + email + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0))
                                            {
                                                postResponse.result = reader.GetString(0);
                                                postResponse.status = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        postResponse.result = "Error-" + ex.Message;
                    }

                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
                */
        [HttpPost]
        public ActionResult Update_Security_staff(string profile, string email, string securitystring)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //--------------------------------------------
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs Set " +
                            "m_MenuKey='" + securitystring + "' " +
                            "where m_Profile='" + profile + "' and m_Email='" + email + "';";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                    sSQL = @"SELECT m_MenuKey FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Profile='" + profile + "' and m_Email='" + email + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        postResponse.result = reader.GetString(0);
                                        postResponse.status = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //        [HttpPost]
        public ActionResult SearchDrivers(string profile, string search)
        {
            var driversResponse = new DriversResponse();
            driversResponse.status = false;
            driversResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_StaffID like '%" + search + "%' or " +
                "m_FName like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_drivers ";
                    sSQL += "where " + sSearchKey + " " +
                        "and m_StaffID not in (select m_DriverID1 from " + MyGlobal.activeDB + ".tbl_assignment where m_DriverID1 is not null and m_Profile = '" + profile + "') " +
                        "and m_Profile='" + profile + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    DriverItem driverItem = new DriverItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) driverItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) driverItem.m_FName = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) driverItem.m_StaffID = reader["m_StaffID"].ToString();

                                    driversResponse.items.Add(driverItem);
                                }
                                driversResponse.status = true;
                                driversResponse.result = "Done";
                            }
                            else
                            {
                                driversResponse.result = "Sorry!!! No drivers" + profile;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                driversResponse.result = "Error-" + ex.Message;
            }

            return Json(driversResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult SearchDevices(string profile, string search)
        {
            var devicesResponse = new DevicesResponse();
            devicesResponse.status = false;
            devicesResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_IMEI like '%" + search + "%' or " +
                "m_Make like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_devices ";
                    sSQL += "where " + sSearchKey + " " +
                        "and m_IMEI not in (select m_DeviceIMEI from " + MyGlobal.activeDB + ".tbl_assignment where m_DeviceIMEI is not null and m_Profile = '" + profile + "') " +
                        "and m_Profile='" + profile + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    DeviceItem deviceItem = new DeviceItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) deviceItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IMEI"))) deviceItem.m_IMEI = reader["m_IMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) deviceItem.m_Make = reader["m_Make"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) deviceItem.m_Model = reader["m_Model"].ToString();
                                    devicesResponse.items.Add(deviceItem);
                                }
                                devicesResponse.status = true;
                                devicesResponse.result = "Done";
                            }
                            else
                            {
                                devicesResponse.result = "Sorry!!! No devices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                devicesResponse.result = "Error-" + ex.Message;
            }

            return Json(devicesResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult searchLinkedProfiles(string profile, string search)
        {
            var linkedProfileResponse = new LinkedProfileResponse();
            linkedProfileResponse.status = false;
            linkedProfileResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_Email like '%" + search + "%' or " +
                "m_Email like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs ";
                    sSQL += "where " + sSearchKey + " " +
                        "and m_Profile='" + profile + "' group by m_Email;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LinkedProfile linkedProfile = new LinkedProfile();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) linkedProfile.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) linkedProfile.m_Email = reader["m_Email"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) deviceItem.m_Make = reader["m_Make"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) deviceItem.m_Model = reader["m_Model"].ToString();
                                    linkedProfileResponse.items.Add(linkedProfile);
                                }
                                linkedProfileResponse.status = true;
                                linkedProfileResponse.result = "Done";
                            }
                            else
                            {
                                linkedProfileResponse.result = "Sorry!!! No profile";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                linkedProfileResponse.result = "Error-" + ex.Message;
            }

            return Json(linkedProfileResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetVehicleExpandInfo(string profile, string m_StaffID, string m_DeviceIMEI)
        {
            VehicleItem deviceItem = new VehicleItem();
            string sSQL = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    if (m_StaffID != null && m_StaffID.Length > 0)
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_drivers ";
                        sSQL += "where m_StaffID='" + m_StaffID + "' and m_Profile='" + profile + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {

                                        //if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) deviceItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) deviceItem.m_FName = reader["m_FName"].ToString();
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) deviceItem.m_Mobile = reader["m_Mobile"].ToString();
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    if (m_DeviceIMEI != null && m_DeviceIMEI.Length > 0)
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_devices ";
                        sSQL += "where m_IMEI='" + m_DeviceIMEI + "' and m_Profile='" + profile + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {

                                        //if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) deviceItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_SIMMobileNo"))) deviceItem.m_SIMMobileNo = reader["m_SIMMobileNo"].ToString();
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) deviceItem.m_Make_device = reader["m_Make"].ToString();
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) deviceItem.m_Model_device = reader["m_Model"].ToString();

                                    }

                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_groups ";
                    sSQL += "where m_Profile='" + profile + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                deviceItem.groupList.Add(
                                                new Group(
                                                    "",
                                                    ""
                                                )
                                            );
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name")))
                                    {
                                        deviceItem.groupList.Add(
                                                new Group(
                                                    reader["m_Name"].ToString(),
                                                    reader["m_Name"].ToString()
                                                )
                                            );
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                    //________________________________________________________________
                }
            }
            catch (MySqlException ex)
            {

            }
            return Json(deviceItem, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GetAllowedList(string profile, string email, string group, string update)
        {
            string sSQL = "";
            var allowedListResponse = new AllowedListResponse();
            allowedListResponse.status = false;
            allowedListResponse.result = "";
            allowedListResponse.groups.Add("All");

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (update != null)
                    {
                        if (update.Length > 0)  // Its an update requests
                        {
                            string sSQLUpdate = "";
                            //__________Delete all entries
                            /*
                            sSQL = @"SELECT m_DeviceIMEI, m_Regno, m_User,assign.m_Group FROM " + MyGlobal.activeDB + ".tbl_assignment assign left join " + MyGlobal.activeDB + ".tbl_authorized as auth on auth.m_IMEI = assign.m_DeviceIMEI and auth.m_Profile = '" + profile + "' where assign.m_Profile = '" + profile + "'";
                            if (group.Length > 0)
                            {
                                if (!group.Equals("All")) sSQL += " and assign.m_Group='" + group + "'";
                            }
                            sSQL += ";";
                            */
                            sSQL = "select m_IMEI from " + MyGlobal.activeDB + ".tbl_devices devices " +
                                "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_DeviceIMEI=devices.m_IMEI and assign.m_Profile=devices.m_Profile " +
                            " where devices.m_Profile = '" + profile + "'";
                            if (group.Length > 0)
                            {
                                if (!group.Equals("All")) sSQL += " and assign.m_Group='" + group + "'";
                            }
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0))
                                            {
                                                if (reader.GetString(0).Length > 0)
                                                {
                                                    sSQLUpdate += "delete from " + MyGlobal.activeDB + ".tbl_authorized where m_IMEI='" + reader.GetString(0) + "' and m_Profile = '" + profile + "' and m_User='" + email + "';";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (sSQLUpdate.Length > 0)
                            {
                                using (MySqlCommand com = new MySqlCommand(sSQLUpdate, con))
                                {
                                    com.ExecuteNonQuery();
                                }
                            }
                            //__________Delete all entries END
                            //_________Add if enabled
                            if (update.Equals("1"))
                            {
                                sSQLUpdate = "";
                                /*
                                sSQL = @"SELECT m_DeviceIMEI, m_Regno, m_User,assign.m_Group FROM " + MyGlobal.activeDB + ".tbl_assignment assign left join " + MyGlobal.activeDB + ".tbl_authorized as auth on auth.m_IMEI = assign.m_DeviceIMEI and auth.m_Profile = assign.m_Profile where assign.m_Profile = '" + profile + "'"; // group by m_DeviceIMEI
                                if (group.Length > 0)
                                {
                                    if(!group.Equals("All"))sSQL += " and assign.m_Group='" + group + "'";
                                }
                                sSQL += ";";
                                */
                                // Same SQL about has to be applied here as it is
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                if (!reader.IsDBNull(0))
                                                {
                                                    if (reader.GetString(0).Length > 0)
                                                    {
                                                        /*
                                                        if (group.Equals("All"))
                                                        {
                                                            sSQLUpdate += "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (m_IMEI,m_Profile,m_User,m_Status) values ('" + reader.GetString(0) + "','" + profile + "','" + email + "','1');";
                                                        }
                                                        else
                                                        {
                                                            if (group.Equals(group))
                                                            {
                                                                sSQLUpdate += "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (m_IMEI,m_Profile,m_User,m_Status) values ('" + reader.GetString(0) + "','" + profile + "','" + email + "','1');";
                                                            }
                                                        }
                                                        */
                                                        sSQLUpdate += "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (m_IMEI,m_Profile,m_User,m_Status) values ('" + reader.GetString(0) + "','" + profile + "','" + email + "','1');";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                if (sSQLUpdate.Length > 0)
                                {
                                    using (MySqlCommand com = new MySqlCommand(sSQLUpdate, con))
                                    {
                                        com.ExecuteNonQuery();
                                    }
                                }
                            }
                            //________________Permission added end
                        }
                    }
                    //___________________________________________________________________________________________
                    sSQL = "SELECT m_DeviceIMEI, m_Regno, m_User,assign.m_Group FROM " + MyGlobal.activeDB + ".tbl_assignment assign " +
"left join " + MyGlobal.activeDB + ".tbl_authorized as auth on auth.m_IMEI = assign.m_DeviceIMEI and auth.m_Profile = '" + profile + "' and auth.m_User = '" + email + "' " +
"where assign.m_Profile = '" + profile + "' and length(m_DeviceIMEI)>5 ";

                    sSQL = "SELECT device.m_IMEI, m_Regno, m_User,assign.m_Group FROM " + MyGlobal.activeDB + ".tbl_devices device " +
"left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_DeviceIMEI = device.m_IMEI and assign.m_Profile = device.m_Profile " +
"left join " + MyGlobal.activeDB + ".tbl_authorized as auth on auth.m_IMEI = device.m_IMEI and auth.m_Profile = device.m_Profile " +
"and auth.m_User = '" + email + "' " +
"where device.m_Profile = '" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string grp = "";
                                    ListRow row = new ListRow();
                                    if (!reader.IsDBNull(0)) row.imei = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) row.regno = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) row.check = reader.GetString(2).Equals(email, StringComparison.CurrentCultureIgnoreCase);
                                    if (!reader.IsDBNull(3))
                                    {
                                        if (reader.GetString(3).Length > 0)
                                        {
                                            grp = reader.GetString(3);
                                            row.group = grp;
                                            if (!allowedListResponse.groups.Contains(grp))
                                            {
                                                allowedListResponse.groups.Add(grp);
                                            }
                                        }
                                    }

                                    if (group.Equals("All") || group.Length == 0 || group.Equals(grp))
                                    {
                                        allowedListResponse.rows.Add(row);
                                    }
                                }
                                allowedListResponse.status = true;
                            }
                            else
                            {
                                allowedListResponse.result = "No records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                allowedListResponse.result += "<tr><td>Error-" + ex.Message + "</td></tr>";
            }
            return Json(allowedListResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult UpdateAllowedList(string imei, string profile, string email, string group, string state)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = @"SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_authorized " +
"where m_Profile = '" + profile + "' and m_User='" + email + "' and m_IMEI='" + imei + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            sSQL = "";
                            if (reader.HasRows)
                            {
                                if (!state.Equals("1")) sSQL = "delete from " + MyGlobal.activeDB + ".tbl_authorized where m_Profile = '" + profile + "' and m_User='" + email + "' and m_IMEI='" + imei + "';";

                            }
                            else
                            {
                                if (state.Equals("1")) sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (m_IMEI,m_Profile,m_User,m_Status) values ('" + imei + "','" + profile + "','" + email + "','1');";
                            }
                        }
                    }
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            postResponse.status = true;
                            postResponse.result = "Updated";
                        }
                    }
                    else
                    {
                        postResponse.status = true;
                        postResponse.result = "No change";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result += ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteUser(string profile, string email)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bUserExists = false;
                    sSQL = @"SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_staffs " +
"where m_Profile = '" + profile + "' and (m_Email='" + email + "' or m_Email is null)";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                bUserExists = true;
                            }
                            else
                            {
                                postResponse.result = "User does not exists";
                            }
                        }
                    }
                    if (bUserExists)
                    {
                        //--------------Protected robin
                        /*
                        sSQL= @"delete from " + MyGlobal.activeDB + ".tbl_staffs where m_Profile = '" + profile + "' and (m_Email='" + email + "' or m_Email is null)";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            postResponse.status = true;
                            postResponse.result = "User deleted";
                        }
                        */
                        postResponse.result = "Critical. Deletion Blocked.";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result += ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ViewSelected(string profile, string email, int check)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //sSQL = @"update " + MyGlobal.activeDB + ".tbl_staffs Set m_ViewSelected=If(m_ViewSelected = 1, 0, 1) where m_Profile = '" + profile + "' and m_Email='" + email + "'";
                    sSQL = @"update " + MyGlobal.activeDB + ".tbl_staffs Set m_ViewSelected='" + check + "' where m_Profile = '" + profile + "' and m_Email='" + email + "'";

                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        int iDone = com.ExecuteNonQuery();
                        postResponse.status = true;
                        if (iDone > 0)
                            postResponse.result = "Status updated";
                        else
                            postResponse.result = "No changes found";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result += ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }

    }
}
