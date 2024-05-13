using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MyHub.Hubs;
using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http.Cors;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DoomController : Controller
    {
        const int TRACKABLE_YES = 1;    // NULL is also trackable
        const int TRACKABLE_NO = 2;
        const int DEVICE_LAPSE_MARK = 600;
        // GET: Doom
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DispatchModeChanged(string imei, string mode)
        {
            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            if (mode.Equals("off"))
            {
                hub.RemoveFromSubscribeList(imei);
                hub.SetTaxiDataBlockList(imei, 0);
                vehicleAroundResponse.result = "DataBlockMode_OFF";
            }
            else
            {
                hub.SetTaxiDataBlockList(imei, 1);
                vehicleAroundResponse.result = "DataBlockMode_ON";
            }
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult InfoWindowMobile(string imei, string domain)
        {
            String sRegNo = "", sFleetID = "", sDriverID = "", sName = "", sMobile = "", profile = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = @"select m_RegNo,m_FleetID,m_DriverID1,m_Mobile,m_FName,assign.m_Profile from " + MyGlobal.activeDB + ".tbl_assignment as assign " +
"left join " + MyGlobal.activeDB + ".tbl_drivers as driver on driver.m_StaffID=assign.m_DriverID1 and driver.m_Profile=assign.m_Profile where assign.m_DeviceIMEI='" + imei + "'";// and assign.m_Profile='" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sRegNo = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    sFleetID = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    sDriverID = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    sMobile = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    sName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    profile = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                }
                            }
                        }
                    }

                }
            }
            catch (MySqlException ex)
            {
                sRegNo = ex.Message;
            }



            var pop = "";
            pop += "<table style='width:50%;'>";
            pop += "<tr class='poptr'>";
            pop += "<td rowspan=5 style='width:48px;padding-right:3px;text-align:center;'><img src='http://" + domain + "/handlers/GetDvrPhoto.ashx?staffid=" + profile + "_" + sDriverID + "' style='width:48px;border:0.5px solid #ddd;'></td>";
            //pop += "<td rowspan=5 style='width:8px;'>&nbsp</td>";
            pop += "</tr>";
            pop += "<tr class='poptr'><td class='poptd1'>RegNo</td><td class='poptd2b'>" + sRegNo + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>FleetID</td><td class='poptd2'>" + sFleetID + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>Mobile</td><td class='poptd2'>" + sMobile + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>";
            if (sDriverID.Length == 0)
                pop += "<span style='color:#999;'>DriverID?</span>";
            else
                pop += "<span style='color:#000;'>" + sDriverID + "</span>";
            pop += "</td><td class='poptd2'>";
            if (sName.Length == 0)
                pop += "<span style='color:#999;'>Dvr Name?</span>";
            else
                pop += "<span style='color:#000;'>" + sName + "</span>";
            pop += "</td></tr>";
            pop += "<tr><td colspan=2 style='white-space:nowrap;'></td>" +
                "<td colspan=2 style='text-align:center;white-space:nowrap;'><span style='color:#444;font-size:x-small;'>" + imei + "</span></td></tr>";
            pop += "</table>";
            //pop += "<span><img src='assets/img/phone_16.png'></span>";
            //pop += "";
            //pop += "";
            return Content(pop);
        }
        [HttpPost]
        public ActionResult InfoWindow(string profile, string imei, string domain)
        {
            //String sRegNo = "", sFleetID = "", sDriverID = "", sName = "", sMobile = "";
            var infoWindowData = new InfoWindowData();
            infoWindowData.status = false;
            infoWindowData.result = "";
            infoWindowData.imei = imei;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = @"select m_RegNo,m_FleetID,m_DriverID1,m_Mobile,m_FName from " + MyGlobal.activeDB + ".tbl_assignment as assign " +
"left join " + MyGlobal.activeDB + ".tbl_drivers as driver on driver.m_StaffID=assign.m_DriverID1 and driver.m_Profile='" + profile + "' where assign.m_DeviceIMEI='" + imei + "' and assign.m_Profile='" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    infoWindowData.sRegNo = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    infoWindowData.sFleetID = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    infoWindowData.sDriverID = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    infoWindowData.sMobile = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    infoWindowData.sName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    infoWindowData.status = true;
                                }
                            }
                        }
                    }

                }
            }
            catch (MySqlException ex)
            {
                infoWindowData.result = ex.Message;
            }
            /*
            var pop = "";
            pop += "<table style='max-width:100px;'>";
            pop += "<tr class='poptr'>";
            pop += "<td rowspan=6 style='width:64px;'><img src='http://" + domain + "/handlers/GetDvrPhoto.ashx?staffid=" + profile + "_" + sDriverID + "' style='width:64px;border:0.5px solid #ddd;'></td>";
            pop += "<td rowspan=6 style='width:8px;'>&nbsp</td>";
            pop += "</tr>";
            pop += "<tr class='poptr'><td class='poptd1'>RegNo</td><td class='poptd2b'>" + sRegNo + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>FleetID</td><td class='poptd2'>" + sFleetID + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>DriverID</td><td class='poptd2'>" + sDriverID + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>Name</td><td class='poptd2'>" + sName + "</td></tr>";
            pop += "<tr class='poptr'><td class='poptd1'>Mobile</td><td class='poptd2'>" + sMobile + "</td></tr>";
            pop += "<tr class='poptr'><td colspan=2><span style='font-size:x-small;font-weight:bold;cursor:pointer;' onClick='PloteRoute()'>PLOT ROUTE</span></td><td colspan=2 style='text-align:center;'><span style='color:red;font-size:x-small;'>" + imei + "</span></td></tr>";
            pop += "</table>";

            return Content(pop);
            */
            return Json(infoWindowData, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult OnRegisterOut(string imei, string email, string password)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "update " + MyGlobal.activeDB + ".tbl_devices Set " +
                        "m_LinkedProfile=null where m_imei='" + imei + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    postResponse.status = true;
                    postResponse.result = "Account Removed";
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult OnRegister(string imei, string email, string password)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sProfile = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bSuccess = false;
                    sSQL = @"select m_Profile,m_Email from " + MyGlobal.activeDB + ".tbl_staffs " +
"where (m_Email='" + email + "' or m_StaffID='" + email + "' or m_Username='" + email + "') and m_Password='" + password + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    bSuccess = true;
                                    sProfile = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    email = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                }
                            }
                        }
                    }
                    if (bSuccess)
                    {
                        int iAffected = 0;
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_devices Set " +
                            "m_Profile='" + sProfile + "'," +
                            "m_LinkedProfile='" + email + "'," +
                            "m_Trackable='" + TRACKABLE_NO + "' " +
                            "where m_imei='" + imei + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            iAffected=mySqlCommand.ExecuteNonQuery();
                        }
                        if (iAffected == 0)
                        {   // New device. So, create new
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_devices " +
                                "(m_IMEI,m_profile,m_LinkedProfile,m_Trackable) values " +
                                "('" + imei + "','" + sProfile + "','" + email + "','" + TRACKABLE_NO + "');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))mySqlCommand.ExecuteNonQuery();
                        }
                        postResponse.status = true;
                        postResponse.result = sProfile;
                    }
                    else
                    {
                        postResponse.result = "Invalid user credentials";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = ex.Message;
            }
           
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetDeviceList(string imei,bool showall, LatLng latlng, string group)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";
            if (group == null) group = "";
            var iCnt = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------Get profile and group
                    String profile = "";
                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    //Dictionary<string, List<AboutACar>> _groups = new Dictionary<string, List<AboutACar>>(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, CarGroup> _groups = new Dictionary<string, CarGroup>(StringComparer.OrdinalIgnoreCase);
                    DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
                    var hub = hd.ResolveHub("ChatHub") as ChatHub;
                    //,device.m_Email
                    String sSQL = @"SELECT device.m_LinkedProfile,device.m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices as device "+
                    "left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_DeviceIMEI=device.m_IMEI "+
                    "where device.m_IMEI = '" + imei + "';";
                    bool bDoesRecordExists = false;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                bDoesRecordExists = true;
                                if (reader.Read())
                                {
                                    group = GetPure(reader, 0);
                                    profile = GetPure(reader, 1);
                                    //email = GetPure(reader, 2);
                                }
                            }
                        }
                    }
                    /*
                    if (!bDoesRecordExists) // New deviec
                    {
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_devices (m_IMEI,m_CreatedTime,m_UpdatedTime,m_VeriCode,m_Security,m_Type,m_Profile) values ('" + imei + "',Now(),Now(),'" + MyGlobal.GetRandomNo(1000, 9999) + "','9','3','"+ profile + "');";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                    */
                    //-------------------------------------------------------------
                    if (group.Length == 0)
                    {
                        vehicleAroundResponse.result = "Not registered";
                        return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
                    }
                    //_____________________________________________________________New
                    sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_RegNo,m_FleetID,m_DriverID1,assign.m_Group,m_TimeReceived,m_Speed FROM " + MyGlobal.activeDB + ".tbl_devices as device "+
                    "left join " + MyGlobal.activeDB + ".tbl_assignment as assign on assign.m_DeviceIMEI=device.m_IMEI and assign.m_Profile='" + profile + "' where ";
                    if (!showall || (latlng != null && latlng.lat > 0)) sSQL += " device.m_TimeReceived>(" + unixTimestamp + "-"+ DEVICE_LAPSE_MARK + ") and device.m_TimeReceived<(" + unixTimestamp + "+"+ DEVICE_LAPSE_MARK + ") and ";
                    if (latlng != null && latlng.lat > 0) sSQL += "ABS(m_Lat-" + latlng.lat + ")< 0.01 and ABS(m_Lng-" + latlng.lng + ")< 0.01 and ";
                    sSQL += @" m_IMEI is not null and m_Time is not null and m_Lat is not null and m_Lng is not null ";
                    sSQL += "and device.m_Profile='" + profile + "' ";
                    if (!profile.Equals(group, StringComparison.CurrentCultureIgnoreCase))
                        sSQL += "and device.m_IMEI in (select m_IMEI from " + MyGlobal.activeDB + ".tbl_Authorized where m_Profile='" + profile + "' and m_User='" + group + "' and m_Status='1') ";
                    sSQL += "and (device.m_Trackable is null or device.m_Trackable = 1) ";
                    sSQL += " order by assign.m_Group";

                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                //List<AboutACar> listAboutACar = new List<AboutACar>();
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {
                                    int iHeading = 0;
                                    String sGroupName = "ungrouped";
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    if (!reader.IsDBNull(8))
                                    {
                                        if (reader.GetString(8).Length > 0)
                                            sGroupName = reader.GetString(8);
                                    }
                                    long lapse = 9999;
                                    int speed=0;
                                    if (!reader.IsDBNull(9))lapse =unixTimestamp - reader.GetInt32(9); // Lapse on TimeReceived
                                    if (!reader.IsDBNull(10)) speed = reader.GetInt32(10);
                                    var aboutACar = new AboutACar(
                                        reader.GetString(0),
                                        "honda",
                                        new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
                                        reader.GetInt32(1),
                                        iHeading,
                                        reader.IsDBNull(5) ? "" : reader.GetString(5),
                                        reader.IsDBNull(6) ? "" : reader.GetString(6),
                                        reader.IsDBNull(7) ? "" : reader.GetString(7),
                                        lapse,
                                        speed
                                    );

                                    if (_groups.ContainsKey(sGroupName))
                                    {
                                        CarGroup carGroup = _groups[sGroupName];
                                        carGroup.name = sGroupName;
                                        if (lapse < 600) carGroup.countOn++; else carGroup.countOff++;
                                        List<AboutACar> li = carGroup.aboutACars;
                                        li.Add(aboutACar);
                                    }
                                    else
                                    {
                                        CarGroup carGroup = new CarGroup();
                                        carGroup.name = sGroupName;
                                        carGroup.countOn = 0;
                                        carGroup.countOff = 0;
                                        if (lapse < DEVICE_LAPSE_MARK) carGroup.countOn++; else carGroup.countOff++;
                                        carGroup.aboutACars.Add(aboutACar);
                                        _groups.Add(sGroupName, carGroup);
                                    }
                                    _subscribelist.Add(reader.GetString(0));
                                    iCnt++;
                                }
                                hub.SetSubscribeList(imei, _subscribelist);
                            }
                            else
                            {
                                hub.RemoveFromSubscribeList(imei);
                            }
                        }
                    }
                    //_______________Get groups
                    foreach (KeyValuePair<string, CarGroup> aGroup in _groups)
                    {
                        vehicleAroundResponse.carGroups.Add(aGroup.Value);
                    }
                }
                if (iCnt == 0) vehicleAroundResponse.result = "Nothing Assigned";
                vehicleAroundResponse.status = true;
            }
            catch (MySqlException ex)
            {
                vehicleAroundResponse.result = "Error-GetDeviceList-" + ex.Message;
            }
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetVehicleAroundFromServer(string profile, string imei, LatLng latlng)
        {
            return LoadTaxies(profile, imei, true, latlng, null, imei);
        }
        [HttpPost]
        public ActionResult GetVehicleAroundFromServerXXX(string profile, string imei, LatLng latlng)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;

            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //String sSQL = "SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading FROM " + MyGlobal.activeDB + ".tbl_devices " +
                    String sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_RegNo,m_FleetID,m_DriverID1 FROM " + MyGlobal.activeDB + ".tbl_devices as device " +
"left join " + MyGlobal.activeDB + ".tbl_assignment as assign on assign.m_DeviceIMEI=device.m_IMEI and assign.m_Profile='" + profile + "' " +
"where ";
                    sSQL += "ABS(m_Lat-" + latlng.lat + ")< 0.01 and ABS(m_Lng-" + latlng.lng + ")< 0.01 ";
                    sSQL += "and m_Time>(" + unixTimestamp + "-" + DEVICE_LAPSE_MARK + ") and m_Time<(" + unixTimestamp + "+" + DEVICE_LAPSE_MARK + ") and ";

                    sSQL += @" m_IMEI is not null and m_Time is not null and m_Lat is not null and m_Lng is not null ";
                    sSQL += "and device.m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {

                                    int iHeading = 0;
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    /*
                                    vehicleAroundResponse.aboutACars.Add(
                                        new AboutACar(
                                            reader.GetString(0),
                                            "honda",
                                            new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
                                            reader.GetInt32(1), // Time
                                            iHeading,  // Heading
                                            reader.IsDBNull(5) ? "" : reader.GetString(5),
                                            reader.IsDBNull(6) ? "" : reader.GetString(6),
                                            reader.IsDBNull(7) ? "" : reader.GetString(7)
                                            )
                                    );
                                    */
                                    _subscribelist.Add(reader.GetString(0));

                                }
                                hub.SetSubscribeList(imei, _subscribelist);
                                vehicleAroundResponse.status = true;
                            }
                            else
                            {
                                hub.RemoveFromSubscribeList(imei);
                                hub.SetTaxiDataBlockList(imei, 1);
                                vehicleAroundResponse.result = "Sorry!!! No taxies around.";
                            }
                        }
                    }
                    //Log(test);
                }
            }
            catch (MySqlException ex)
            {
                vehicleAroundResponse.result = "Error-" + ex.Message;
            }
            //vehicleAroundResponse.aboutACar.Add(new AboutACar("im11111", "honda",new LatLng(0,0),1111));
            //vehicleAroundResponse.aboutACar.Add(new AboutACar("im11112", "maruthi", new LatLng(2, 2), 2111));

            //return Content("OK");
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetUngroupDevices(string profile)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";

            var iCntOnline = 0;
            var iCntOffline = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "";
                    //_____________________________________________________________________________
                    var aboutACar = new AboutACar(
                        "none",
                        "honda",
                        null,
                        0,
                        0,
                        "",
                        "",
                        "",
                        9999,
                        0
                    );
                    //___________________________________________________________Get missing group heads
                    sSQL = @"select m_Name from " + MyGlobal.activeDB + ".tbl_groups where "+
                            "m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    String sGroup = "";
                                    if (!reader.IsDBNull(0)) sGroup = reader.GetString(0);

                                    List<AboutACar> li = new List<AboutACar>();
                                    li.Add(aboutACar);
                                    //_groups.Add(sGroup, li);

                                    var carGroup = new CarGroup();
                                    carGroup.name = sGroup;
                                    carGroup.countOn = 0;
                                    carGroup.countOff = 0;
                                    carGroup.aboutACars = li;
                                    vehicleAroundResponse.carGroups.Add(carGroup);

                                }
                            }
                        }
                    }
                    //_____________________________________________________________________________
                    sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_Speed FROM " + MyGlobal.activeDB + ".tbl_devices "+
"where m_Profile = '" + profile + "' and m_IMEI not in (select m_DeviceIMEI from " + MyGlobal.activeDB + ".tbl_assignment where m_Profile = '" + profile + "' and m_DeviceIMEI is not null and m_Group is not null and m_Group<>'') ";
                    sSQL += "and m_Time>(" + unixTimestamp + "-" + DEVICE_LAPSE_MARK + ") and m_Time<(" + unixTimestamp + "+" + DEVICE_LAPSE_MARK + ") ";
                    sSQL += "and m_IMEI is not null and m_Time is not null and m_Lat is not null and m_Lng is not null ";

                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<AboutACar> listAboutACar = new List<AboutACar>();
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {
                                    int iHeading = 0,iSpeed=0;
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    if (!reader.IsDBNull(5)) iSpeed = reader.GetInt32(5);
                                    listAboutACar.Add(
                                        new AboutACar(
                                            reader.GetString(0),
                                            "honda",
                                            new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
                                            reader.GetInt32(1),
                                            iHeading,
                                            "",
                                            "",
                                            "",
                                            unixTimestamp - reader.GetInt32(1),
                                            iSpeed
                                        )
                                    );
                                    _subscribelist.Add(reader.GetString(0));
                                    if (unixTimestamp - reader.GetInt32(1) < DEVICE_LAPSE_MARK)
                                        iCntOnline++;
                                    else
                                        iCntOffline++;
                                }
                                if ((iCntOnline + iCntOffline) > 0)
                                {
                                    var carGroup = new CarGroup();
                                    carGroup.name = "ungrouped";
                                    carGroup.countOn = iCntOnline;
                                    carGroup.countOff = iCntOffline;
                                    carGroup.aboutACars = listAboutACar;
                                    vehicleAroundResponse.carGroups.Add(carGroup);
                                }
                            }
                        }
                    }
                    //____________________ungrouped last entry
                    if ((iCntOnline + iCntOffline) == 0)
                    {
                        List<AboutACar> liU = new List<AboutACar>();
                        liU.Add(aboutACar);
                        var carGroupU = new CarGroup();
                        carGroupU.name = "ungrouped";
                        carGroupU.countOn = 0;
                        carGroupU.countOff = 0;
                        carGroupU.aboutACars = liU;
                        vehicleAroundResponse.carGroups.Add(carGroupU);
                    }
                }
                if ((iCntOnline + iCntOffline) == 0) vehicleAroundResponse.result = "No ungrouped vehicles are online";
                vehicleAroundResponse.status = true;
            }
            catch (MySqlException ex)
            {
                vehicleAroundResponse.result = "Error-GetUngroupDevices-" + ex.Message;
            }
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        /*
SELECT count(picklist.m_id) as cnt,m_NameCompany,m_PickLocation,m_Lat,m_Lng FROM " + MyGlobal.activeDB + ".tbl_picklist picklist
left join " + MyGlobal.activeDB + ".tbl_picklist_locations locations on locations.m_Profile=picklist.m_Profile and locations.m_Name=picklist.m_PickLocation and locations.m_CompanyName=picklist.m_NameCompany
group by picklist.m_PickLocation,picklist.m_NameCompany          
        */
        [HttpPost]
        public ActionResult LoadPickupLocations(string profile, string imei)
        {
            var pickupResponse = new PickupResponse();
            pickupResponse.status = false;
            pickupResponse.result = "None";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "";
                    if ((imei != null) && (imei.Length == 15))
                    {
                        sSQL = @"SELECT picklist.m_id,count(picklist.m_id) as cnt,m_NameCompany,m_PickLocation,m_Lat,m_Lng FROM " + MyGlobal.activeDB + ".tbl_picklist picklist "+
"left join " + MyGlobal.activeDB + ".tbl_picklist_locations locations on locations.m_Profile=picklist.m_Profile and locations.m_Name=picklist.m_PickLocation and locations.m_CompanyName=picklist.m_NameCompany "+
"where picklist.m_Profile=(select m_Profile from " + MyGlobal.activeDB + ".tbl_picklist_users where m_IMEI='" + imei + "' limit 1) group by picklist.m_PickLocation,picklist.m_NameCompany";
                    }
                    else {
                        sSQL = @"SELECT picklist.m_id,count(picklist.m_id) as cnt,m_NameCompany,m_PickLocation,m_Lat,m_Lng FROM " + MyGlobal.activeDB + ".tbl_picklist picklist "+
"left join " + MyGlobal.activeDB + ".tbl_picklist_locations locations on locations.m_Profile=picklist.m_Profile and locations.m_Name=picklist.m_PickLocation and locations.m_CompanyName=picklist.m_NameCompany "+
"where picklist.m_Profile='" + profile + "' ";
                        sSQL += "group by picklist.m_PickLocation,picklist.m_NameCompany";
                    }
                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                //List<AboutAPickup> pickups = new List<AboutAPickup>();
                                while (reader.Read())
                                {
                                    String id = "", type = "", label = "", title = "";

                                    if (!reader.IsDBNull(0)) id = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) label = reader.GetString(1);
                                    if (!reader.IsDBNull(2) && !reader.IsDBNull(3)) title =
                                            "'" + reader.GetString(3) + "' of " + reader.GetString(2);

                                    if (!reader.IsDBNull(4) && !reader.IsDBNull(5))
                                    {
                                        var aboutAPickup = new AboutAPickup(
                                            id,
                                            type,
                                            new LatLng(reader.GetDouble(4), reader.GetDouble(5)),
                                            label,
                                            title
                                        );
                                        pickupResponse.pickups.Add(aboutAPickup);
                                    }
                                }
                                pickupResponse.status = true;
                                pickupResponse.result = "Done";
                            }
                        }
                    }

                }
            }
            catch (MySqlException ex)
            {
                pickupResponse.result = "Error-LoadPickupLocations-" + ex.Message;
            }
            return Json(pickupResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadTaxies(string profile, string imei, bool showall, LatLng latlng, string group,string email)
        {
            if (group != null) if (group.Equals("ungrouped")) return GetUngroupDevices(profile);
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            //Dictionary<string, List<AboutACar>> _groups = new Dictionary<string, List<AboutACar>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, CarGroup> _groups = new Dictionary<string, CarGroup>(StringComparer.OrdinalIgnoreCase);
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;


            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";
            var iCnt = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "";
                    //___________________________________________________________Get Groups
                    /*
                    if (email != null && email.Length > 0)
                    {
                        sSQL = @"select m_Name from " + MyGlobal.activeDB + ".tbl_groups where 
m_Profile='" + profile + "';"; 
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
                                            if (!_groups.ContainsKey(reader.GetString(0)))
                                            {
                                                CarGroup carGroup = new CarGroup();
                                                carGroup.name = reader.GetString(0);
                                                carGroup.countOn = 0;
                                                carGroup.countOff = 0;
                                                _groups.Add(reader.GetString(0), carGroup);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    */
                    //___________________________________________________________
                    /*
                    sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_RegNo,m_FleetID,m_DriverID1,m_Group FROM " + MyGlobal.activeDB + ".tbl_assignment as assign " +
                        "left join " + MyGlobal.activeDB + ".tbl_devices as device on device.m_IMEI=assign.m_DeviceIMEI and device.m_Profile='" + profile + "' " +
                        "where ";
                    if (!showall || (latlng!=null && latlng.lat > 0)) sSQL += " device.m_Time>(" + unixTimestamp + "-180) and device.m_Time<(" + unixTimestamp + "+180) and ";
                    if (latlng != null && latlng.lat > 0) sSQL += "ABS(m_Lat-" + latlng.lat + ")< 0.01 and ABS(m_Lng-" + latlng.lng + ")< 0.01 and ";
                    sSQL += @" m_IMEI is not null and m_Time is not null and m_Lat is not null and m_Lng is not null ";
                    sSQL += "and assign.m_Profile='" + profile + "' ";
                    sSQL += "and m_DeviceIMEI in (select m_IMEI from " + MyGlobal.activeDB + ".tbl_Authorized where m_Profile='" + profile + "' and m_User='" + email + "' and m_Status='1') ";
                    if (showall)
                    {
                        if (group != null)
                        {
                            if (group.Length > 0)
                                sSQL += "and assign.m_Group='" + group + "' ";
                            else
                                sSQL += "and (assign.m_Group='' or assign.m_Group is null) ";
                        }
                    }
                    sSQL += " order by m_Group";
                    */
                    //_____________________________________________________________New
                    sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_RegNo,m_FleetID,m_DriverID1,m_Group,m_TimeReceived,m_Speed FROM " + MyGlobal.activeDB + ".tbl_devices as device " +
                    "left join " + MyGlobal.activeDB + ".tbl_assignment as assign on assign.m_DeviceIMEI=device.m_IMEI and assign.m_Profile='" + profile + "' " +
                    "where ";
                    if (!showall || (latlng != null && latlng.lat > 0)) sSQL += " device.m_TimeReceived>(" + unixTimestamp + "-" + DEVICE_LAPSE_MARK + ") and device.m_TimeReceived<(" + unixTimestamp + "+" + DEVICE_LAPSE_MARK + ") and ";
                    if (latlng != null && latlng.lat > 0) sSQL += "ABS(m_Lat-" + latlng.lat + ")< 0.01 and ABS(m_Lng-" + latlng.lng + ")< 0.01 and ";
                    sSQL += @" m_IMEI is not null "; // and m_Time is not null and m_Lat is not null and m_Lng is not null
                    sSQL += "and device.m_Profile='" + profile + "' ";
                    if (!profile.Equals(email, StringComparison.CurrentCultureIgnoreCase))
                        sSQL += "and device.m_IMEI in (select m_IMEI from " + MyGlobal.activeDB + ".tbl_Authorized where m_Profile='" + profile + "' and m_User='" + email + "' and m_Status='1') ";
                    sSQL += " and (device.m_Trackable is null or device.m_Trackable = 1) ";
                    sSQL += " order by m_Group";

                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                //List<AboutACar> listAboutACar = new List<AboutACar>();
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {
                                    int iHeading = 0;
                                    String sGroupName = "ungrouped";
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    if (!reader.IsDBNull(8))
                                    {
                                        if (reader.GetString(8).Length > 0)
                                            sGroupName = reader.GetString(8);
                                    }
                                    long lapse = 9999;
                                    int speed = 0;
                                    if (!reader.IsDBNull(9))lapse= unixTimestamp - reader.GetInt32(9); // Lapse on TimeReceived
                                    if (!reader.IsDBNull(10)) speed = reader.GetInt32(10);
                                    var aboutACar = new AboutACar(
                                        reader.GetString(0),
                                        "honda",
                                        new LatLng(
                                            reader.IsDBNull(2) ? 0:reader.GetDouble(2),
                                            reader.IsDBNull(3) ? 0 : reader.GetDouble(3)
                                            ),
                                        reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                        iHeading,
                                        reader.IsDBNull(5) ? "" : reader.GetString(5),
                                        reader.IsDBNull(6) ? "" : reader.GetString(6),
                                        reader.IsDBNull(7) ? "" : reader.GetString(7),
                                        lapse,
                                        speed
                                    );

                                    if (_groups.ContainsKey(sGroupName))
                                    {
                                        CarGroup carGroup = _groups[sGroupName];
                                        carGroup.name = sGroupName;
                                        if (lapse < DEVICE_LAPSE_MARK) carGroup.countOn++; else carGroup.countOff++;
                                        List<AboutACar> li = carGroup.aboutACars;
                                        li.Add(aboutACar);
                                    }
                                    else
                                    {
                                        CarGroup carGroup = new CarGroup();
                                        carGroup.name = sGroupName;
                                        carGroup.countOn = 0;
                                        carGroup.countOff = 0;
                                        if (lapse < DEVICE_LAPSE_MARK) carGroup.countOn++; else carGroup.countOff++;
                                        carGroup.aboutACars.Add(aboutACar);
                                        _groups.Add(sGroupName, carGroup);
                                    }
                                    _subscribelist.Add(reader.GetString(0));
                                    iCnt++;
                                }
                                hub.SetSubscribeList(email, _subscribelist);
                            }
                            else
                            {
                                hub.RemoveFromSubscribeList(imei);
                            }
                        }
                    }
                    //_______________Get groups
                    foreach (KeyValuePair<string, CarGroup> aGroup in _groups)
                    {
                        vehicleAroundResponse.carGroups.Add(aGroup.Value);
                    }
                }
                if (iCnt == 0) vehicleAroundResponse.result = "Sorry!!! No vehicles are online";
                vehicleAroundResponse.status = true;
            }
            catch (MySqlException ex)
            {
                vehicleAroundResponse.result = "Error-LoadTaxies-" + ex.Message;
            }
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadTaxies_OLD(string profile,string imei, bool showall, LatLng latlng, string group)
        {
            if (group != null) if (group.Equals("ungrouped")) return GetUngroupDevices(profile);
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            //Dictionary<string, List<AboutACar>> _groups = new Dictionary<string, List<AboutACar>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, CarGroup> _groups = new Dictionary<string, CarGroup>(StringComparer.OrdinalIgnoreCase);
            
            var vehicleAroundResponse = new VehicleAroundResponse();
            vehicleAroundResponse.status = false;
            vehicleAroundResponse.result = "None";
            var iCnt = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "";
                    //___________________________________________________________
                    sSQL = @"SELECT m_IMEI,m_Time,m_Lat,m_Lng,m_Heading,m_RegNo,m_FleetID,m_DriverID1,m_Group,m_Speed FROM " + MyGlobal.activeDB + ".tbl_assignment as assign " +
                        "left join " + MyGlobal.activeDB + ".tbl_devices as device on device.m_IMEI=assign.m_DeviceIMEI and device.m_Profile='" + profile + "' " +
                        "where ";
                    if (!showall || latlng.lat > 0) sSQL += " device.m_Time>(" + unixTimestamp + "-" + DEVICE_LAPSE_MARK + ") and device.m_Time<(" + unixTimestamp + "+" + DEVICE_LAPSE_MARK + ") and ";
                    if (latlng.lat > 0) sSQL += "ABS(m_Lat-" + latlng.lat + ")< 0.01 and ABS(m_Lng-" + latlng.lng + ")< 0.01 and ";
                    sSQL += @" m_IMEI is not null and m_Time is not null and m_Lat is not null and m_Lng is not null ";
                    sSQL += "and assign.m_Profile='" + profile + "' ";

                    if (showall)
                    {
                        if (group != null)
                        {
                            if (group.Length > 0)
                                sSQL += "and assign.m_Group='" + group + "' ";
                            else
                                sSQL += "and (assign.m_Group='' or assign.m_Group is null) ";
                        }
                    }
                    sSQL += " order by m_Group";
                    
                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<AboutACar> listAboutACar = new List<AboutACar>();
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {
                                    int iHeading = 0,speed=0;
                                    String sGroupName = "ungrouped";
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    if (!reader.IsDBNull(8))
                                    {
                                        if (reader.GetString(8).Length > 0)
                                            sGroupName = reader.GetString(8);
                                    }
                                    if (!reader.IsDBNull(9))
                                    {
                                            speed = reader.GetInt32(9);
                                    }
                                    long lapse = unixTimestamp - reader.GetInt32(1);
                                    var aboutACar = new AboutACar(
                                        reader.GetString(0),
                                        "honda",
                                        new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
                                        reader.GetInt32(1),
                                        iHeading,
                                        reader.IsDBNull(5) ? "" : reader.GetString(5),
                                        reader.IsDBNull(6) ? "" : reader.GetString(6),
                                        reader.IsDBNull(7) ? "" : reader.GetString(7),
                                        lapse,
                                        speed
                                    );

                                    if (_groups.ContainsKey(sGroupName))
                                    {
                                        CarGroup carGroup = _groups[sGroupName];
                                        carGroup.name = sGroupName;
                                        if (lapse < DEVICE_LAPSE_MARK) carGroup.countOn++; else carGroup.countOff++;
                                        List<AboutACar> li = carGroup.aboutACars;
                                        li.Add(aboutACar);
                                    }
                                    else
                                    {
                                        CarGroup carGroup = new CarGroup();
                                        carGroup.name = sGroupName;
                                        carGroup.countOn = 0;
                                        carGroup.countOff = 0;
                                        if (lapse < DEVICE_LAPSE_MARK) carGroup.countOn++; else carGroup.countOff++;
                                        //List<AboutACar> li = new List<AboutACar>();
                                        carGroup.aboutACars.Add(aboutACar);
                                        _groups.Add(sGroupName, carGroup);
                                    }
                                    _subscribelist.Add(reader.GetString(0));
                                    iCnt++;
                                }
                                //_______________Get groups
                                foreach (KeyValuePair<string, CarGroup> aGroup in _groups)
                                {
                                    vehicleAroundResponse.carGroups.Add(aGroup.Value);
                                }
                            }
                        }
                    }

                    
                    /*
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<AboutACar> listAboutACar = new List<AboutACar>();
                                List<string> _subscribelist = new List<string>();
                                while (reader.Read())
                                {
                                    int iHeading = 0;
                                    String sGroupName = "ungrouped";
                                    if (!reader.IsDBNull(4)) iHeading = reader.GetInt32(4);
                                    if (!reader.IsDBNull(8))
                                    {
                                        if(reader.GetString(8).Length>0)
                                            sGroupName = reader.GetString(8);
                                    }
                                    long lapse = unixTimestamp - reader.GetInt32(1);
                                    var aboutACar = new AboutACar(
                                        reader.GetString(0),
                                        "honda",
                                        new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
                                        reader.GetInt32(1),
                                        iHeading,
                                        reader.IsDBNull(5) ? "" : reader.GetString(5),
                                        reader.IsDBNull(6) ? "" : reader.GetString(6),
                                        reader.IsDBNull(7) ? "" : reader.GetString(7),
                                        lapse
                                    );

                                    if (_groups.ContainsKey(sGroupName))
                                    {
                                        CarGroup carGroup = _groups[sGroupName];
                                        if (lapse < 180) carGroup.countOn++; else carGroup.countOff++;
                                        List<AboutACar> li = carGroup.aboutACars;
                                        li.Add(aboutACar);
                                    }
                                    else
                                    {
                                        CarGroup carGroup = new CarGroup();
                                        carGroup.countOn = 0;
                                        carGroup.countOff = 0;
                                        if (lapse < 180) carGroup.countOn++; else carGroup.countOff++;
                                        //List<AboutACar> li = new List<AboutACar>();
                                        carGroup.aboutACars.Add(aboutACar);
                                        _groups.Add(sGroupName, carGroup);
                                    }
                                    _subscribelist.Add(reader.GetString(0));
                                    iCnt++;
                                }
                            }
                        }
                    }
                    
                    
                    //___________________________________________________________Get missing group heads
                    sSQL = @"select m_Name from " + MyGlobal.activeDB + ".tbl_groups where 
                            m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            var aboutACar = new AboutACar(
                                "none",
                                "honda",
                                null,
                                0,
                                0,
                                "",
                                "",
                                "",
                                9999
                            );
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    String sGroup = "";
                                    if (!reader.IsDBNull(0)) sGroup = reader.GetString(0);
                                    if (_groups.ContainsKey(sGroup))
                                    {
                                        //_groups.Remove(sGroup);
                                        //List<AboutACar> aboutACars = _groups[sGroup];
                                        CarGroup carGroup = _groups[sGroup];
                                        //var carGroup = new CarGroup();
                                        //carGroup.name = sGroup;
                                        //carGroup.count = aboutACars.Count;
                                        //carGroup.aboutACars = aboutACars;
                                        vehicleAroundResponse.carGroups.Add(carGroup);
                                    }
                                    else
                                    { 
                                        // Ungrouped
                                        List<AboutACar> li = new List<AboutACar>();
                                        li.Add(aboutACar);
                                        _groups.Add(sGroup, li);

                                        var carGroup = new CarGroup();
                                        carGroup.name = sGroup;
                                        carGroup.count = 0;
                                        carGroup.aboutACars = li;
                                        vehicleAroundResponse.carGroups.Add(carGroup);
                                    }
                                }
                            }
                            //____________________ungrouped last entry
                            List<AboutACar> liU = new List<AboutACar>();
                            liU.Add(aboutACar);
                            _groups.Add("ungrouped", liU);

                            var carGroupU = new CarGroup();
                            carGroupU.name = "ungrouped";
                            carGroupU.count = 0;
                            carGroupU.aboutACars = liU;
                            vehicleAroundResponse.carGroups.Add(carGroupU);
                        }
                    }
                    */
                }


                if (iCnt==0) vehicleAroundResponse.result = "Sorry!!! No vehicles are online";
                vehicleAroundResponse.status = true;
            }
            catch (MySqlException ex)
            {
                vehicleAroundResponse.result = "Error-" + ex.Message;
            }
            return Json(vehicleAroundResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult OnLoadServerDataForClient(string imei,string profile)
        {
            var onLoadResponseToClient = new OnLoadResponseToClient();
            onLoadResponseToClient.result = "";
            try
            {
                string sSQL = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = "SELECT jobs.m_id,m_PickAddress,m_DropAddress,m_PickLat,m_PickLng,m_DropLat,m_DropLng," +
                    "m_VehicleType,m_AssignedTo,m_AssignedToStaffID," +
                    "m_FName,m_TaxiType,m_RegNo," +
                    "clients.m_Name,clients.m_Active,clients.m_Email " +
                    "from " + MyGlobal.activeDB + ".tbl_jobs_doom jobs " +
                    "left join " + MyGlobal.activeDB + ".tbl_drivers dvrs on dvrs.m_StaffID = jobs.m_AssignedToStaffID and dvrs.m_Profile='" + profile + "' " +
                    "left join " + MyGlobal.activeDB + ".tbl_clients clients on clients.m_IMEI = '" + imei + "' and clients.m_Profile='" + profile + "' " +
                    "where m_AssignedToStaffID is not null and m_TimeClosed is null and jobs.m_IMEI = '" + imei + "' and jobs.m_Profile='" + profile + "';";

                    bool bPendingJob = false;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onLoadResponseToClient.JobID = GetFieldString(reader, 0);
                                    onLoadResponseToClient.m_PickAddress = GetFieldString(reader, 1);
                                    onLoadResponseToClient.m_DropAddress = GetFieldString(reader, 2);
                                    onLoadResponseToClient.m_PickLat = GetFieldString(reader, 3);
                                    onLoadResponseToClient.m_PickLng = GetFieldString(reader, 4);
                                    onLoadResponseToClient.m_DropLat = GetFieldString(reader, 5);
                                    onLoadResponseToClient.m_DropLng = GetFieldString(reader, 6);
                                    onLoadResponseToClient.m_VehicleType = GetFieldString(reader, 7);
                                    onLoadResponseToClient.m_AssignedTo = GetFieldString(reader, 8);
                                    onLoadResponseToClient.m_AssignedToStaffID = GetFieldString(reader, 9);
                                    onLoadResponseToClient.m_FName = GetFieldString(reader, 10);
                                    onLoadResponseToClient.m_TaxiType = GetFieldString(reader, 11);
                                    onLoadResponseToClient.m_RegNo = GetFieldString(reader, 12);

                                    onLoadResponseToClient.m_ClientName = GetFieldString(reader, 13);
                                    onLoadResponseToClient.m_ClientActive = GetFieldString(reader, 14);
                                    onLoadResponseToClient.m_ClientEmail = GetFieldString(reader, 15);

                                    onLoadResponseToClient.status = true;
                                    onLoadResponseToClient.result = "Done";
                                    bPendingJob = true;
                                }
                            }
                        }
                    }
                    if (!bPendingJob) // If No pending Job, atleast pick client info
                    {
                        sSQL = "SELECT m_Name,m_Active,m_Email " +
                        "from " + MyGlobal.activeDB + ".tbl_clients where m_IMEI = '" + imei + "' and m_Profile='" + profile + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        onLoadResponseToClient.m_ClientName = GetFieldString(reader, 0);
                                        onLoadResponseToClient.m_ClientActive = GetFieldString(reader, 1);
                                        onLoadResponseToClient.m_ClientEmail = GetFieldString(reader, 2);
                                        onLoadResponseToClient.status = true;
                                        onLoadResponseToClient.result = "Done";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                onLoadResponseToClient.result = "Error-OnLoadServerDataForClient-" + ex.Message;
            }

            return Json(onLoadResponseToClient, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult OnLoadCall(string imei,string profile)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadResponse = new OnLoadResponse();
            onLoadResponse.status = false;
            onLoadResponse.result = "None";

            try
            {
                string sSQL = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_tariffs where m_Name='default' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onLoadResponse.myTariff.m_Name = reader.GetString(1);
                                    onLoadResponse.myTariff.m_FlagFall = reader.GetString(5);
                                    onLoadResponse.myTariff.m_DistanceFree = reader.GetString(6);
                                    onLoadResponse.myTariff.m_WaitingFree = reader.GetString(7);
                                    onLoadResponse.myTariff.m_DistanceSlab = reader.GetString(8);
                                    onLoadResponse.myTariff.m_DistanceCharge = reader.GetString(9);
                                    onLoadResponse.myTariff.m_WaitingSlab = reader.GetString(10);
                                    onLoadResponse.myTariff.m_WaitingCharge = reader.GetString(11);
                                    onLoadResponse.myTariff.m_WaitingSpeedLag = reader.GetString(12);
                                    onLoadResponse.myTariff.m_Surcharge = reader.GetString(13);

                                    onLoadResponse.status = true;
                                    onLoadResponse.result = "Done";
                                }
                            }
                        }
                    }
                    //onLoadResponse.activeJob.jobid = "1234";
                    //_____________________Is there any active job requests?
                    sSQL = "SELECT JobD.m_IMEI,JobA.m_IMEIDriver,m_PickLat,m_PickLng,m_DropLat,m_DropLng,m_PickAddress,m_DropAddress,m_VehicleType,JobD.m_id,JobD.m_Source "+
"FROM " + MyGlobal.activeDB + ".tbl_jobs_assigned as JobA " +
"left join " + MyGlobal.activeDB + ".tbl_jobs_doom as JobD on JobD.m_id = JobA.m_id_job  and JobD.m_Profile='" + profile + "' " +
"where m_IMEIDriver = '"+ imei + "' and JobA.m_TimeAssigned > DATE_SUB(NOW(), INTERVAL 120 SECOND) and JobA.m_Profile='" + profile + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onLoadResponse.activeJob.imeiClient = GetPure(reader, 0);
                                    onLoadResponse.activeJob.imeiDriver = GetPure(reader, 1);
                                    onLoadResponse.activeJob.pickloclat = GetPure(reader, 2);
                                    onLoadResponse.activeJob.pickloclng = GetPure(reader, 3);
                                    onLoadResponse.activeJob.droploclat = GetPure(reader, 4);
                                    onLoadResponse.activeJob.droploclng = GetPure(reader, 5);
                                    onLoadResponse.activeJob.pickadd = GetPure(reader, 6);
                                    onLoadResponse.activeJob.dropadd = GetPure(reader, 7);
                                    onLoadResponse.activeJob.vehicletype = GetPure(reader, 8);
                                    onLoadResponse.activeJob.distance = "";
                                    onLoadResponse.activeJob.duration = "";
                                    onLoadResponse.activeJob.fare = "";
                                    onLoadResponse.activeJob.jobid = GetPure(reader, 9);
                                    onLoadResponse.activeJob.src = GetPure(reader, 10);

                                    //onLoadResponse.status = true;
                                    //onLoadResponse.result = "Done";
                                }
                            }
                        }
                    }
                    //_____________________Additional Information
                    sSQL = "SELECT m_DriverID1,m_RegNo,m_FleetID FROM " + MyGlobal.activeDB + ".tbl_assignment " +
                            "where m_DeviceIMEI = '" + imei + "' and m_Profile='" + profile + "';";

                    sSQL = @"SELECT assign.m_DriverID1,assign.m_RegNo,m_FleetID,driver.m_FName FROM " + MyGlobal.activeDB + ".tbl_assignment as assign "+
"left join " + MyGlobal.activeDB + ".tbl_drivers as driver on driver.m_StaffID = assign.m_DriverID1 and driver.m_Profile='" + profile + "' where assign.m_DeviceIMEI = '" + imei + "' and assign.m_Profile='" + profile + "'";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onLoadResponse.DriverID = GetPure(reader, 0);
                                    onLoadResponse.RegNo = GetPure(reader, 1);
                                    onLoadResponse.FleetID = GetPure(reader, 2);
                                    onLoadResponse.DriverName = GetPure(reader, 3);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                onLoadResponse.result = "Error-OnLoadCall-" + ex.Message;
            }
            return Json(onLoadResponse, JsonRequestBehavior.AllowGet);
        }
        private String GetFieldString(MySqlDataReader reader, int iIndex)
        {
            if (reader.IsDBNull(iIndex)) return "";
            return reader.GetString(iIndex);
        }
        private bool IsValidEmail(string email)
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
        public ActionResult UpdateClientProfile(string imei, string mode, string email, string otp, string name, string profile)
        {
            var clientResponse = new ClientResponse();
            clientResponse.email = "";
            clientResponse.name = "";
            clientResponse.otp = "";
            clientResponse.active = -1;

            try
            {
                string sSQL = "", sOTPGenerated = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bHasRecord = false;
                    //______________load data
                    sSQL = "SELECT m_Email,m_OTP,m_Active,m_Name FROM " + MyGlobal.activeDB + ".tbl_clients where m_IMEI='" + imei + "' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) clientResponse.email = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) clientResponse.otp = reader.GetString(1);
                                    //if (!reader.IsDBNull(2)) clientResponse.active = reader.GetInt16(2);
                                    if (!reader.IsDBNull(3)) clientResponse.name = reader.GetString(3);

                                    clientResponse.status = true;
                                    clientResponse.result = "Done";
                                    bHasRecord = true;
                                }
                            }
                        }
                    }
                    //_____________________________________
                    if (mode.Equals("update"))
                    {
                        /*
                        //____________________________________Does this email exists
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_clients where m_email='" + email + "' and m_IMEI != '" + imei + "' and m_Active=1;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    clientResponse.result = "Email already exists";
                                    clientResponse.active = 2;
                                    clientResponse.name = name;
                                    return Json(clientResponse, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                        */
                        //____________________________________
                        if (bHasRecord)
                        {
                            if (!clientResponse.email.Equals(email) ||
                                !clientResponse.name.Equals(name))
                            {
                                sOTPGenerated = MyGlobal.GetRandomNo(1000, 9999);
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_clients Set m_Email='" + email + "'" +
                                    ",m_Name='" + name + "'";
                                if (!clientResponse.email.Equals(email))
                                {
                                    sSQL += ",m_OTP='" + sOTPGenerated + "',m_Active=2";
                                    clientResponse.result = "Please verify OTP";
                                    clientResponse.active = 2; // Show OTP
                                }
                                else
                                {
                                    clientResponse.result = "Updated";
                                }
                                sSQL += " where m_IMEI='" + imei + "' and m_Profile='" + profile + "';";

                                if (!clientResponse.email.Equals(email) &&
                                    !IsValidEmail(email)) // Not valid email
                                {
                                    sSQL = "";
                                    clientResponse.result = "Invalid email";
                                    clientResponse.active = 0;
                                }
                            }
                            else
                            {
                                clientResponse.result = "Nothing to update";
                            }
                        }
                        else
                        {
                            sOTPGenerated = MyGlobal.GetRandomNo(1000, 9999);
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_clients (m_IMEI,m_Name,m_Email,m_OTP,m_Active,m_Profile)" +
                                " values ('" + imei + "','" + name + "','" + email + "','" + sOTPGenerated + "','2','" + profile + "');";
                            // Show OTP
                            if (!IsValidEmail(email)) // Not valid email
                            {
                                sSQL = "";
                                clientResponse.result = "Invalid email";
                                clientResponse.active = 0;
                            }
                            else
                            {
                                clientResponse.active = 2;
                            }
                        }
                        if (sSQL.Length > 0)
                        {
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("resend"))
                    {

                    }
                    else if (mode.Equals("otp"))
                    {
                        if (otp.Equals(clientResponse.otp))
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_clients Set m_Active='1' " +
                                    "where m_IMEI='" + imei + "' and m_Profile='" + profile + "';";
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_clients Set m_Email=null,m_Active=0 " +
                                "where m_Email='" + email + "' and m_IMEI !='" + imei + "' and m_Profile='" + profile + "';";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                            }
                            clientResponse.active = 1;
                        }
                        else
                        {
                            clientResponse.result = "Invalid OTP";
                            clientResponse.active = 2;
                        }
                    }

                    //______________load data
                    String sOTPForMail = "";
                    sSQL = "SELECT m_Email,m_OTP,m_Active,m_Name FROM " + MyGlobal.activeDB + ".tbl_clients where m_IMEI='" + imei + "' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) clientResponse.email = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) sOTPForMail = reader.GetString(1);
                                    if (clientResponse.active == -1)
                                    {
                                        if (!reader.IsDBNull(2)) clientResponse.active = reader.GetInt16(2);
                                    }
                                    if (!reader.IsDBNull(3)) clientResponse.name = reader.GetString(3);
                                    clientResponse.otp = "";
                                    clientResponse.status = true;
                                    //clientResponse.result = "Done";
                                    bHasRecord = true;
                                }
                            }
                        }
                    }
                    //_____________________________________
                    if (clientResponse.active == 2)
                    {
                        MailDoc mailDoc = new MailDoc();
                        mailDoc.m_To = clientResponse.email;
                        mailDoc.Domain = MyGlobal.GetDomain();
                        mailDoc.m_Subject = "Cartrac OTP alert " + sOTPForMail;
                        mailDoc.m_Body = "<b>Your CARTRAC client OTP</b><br><br>" +
"Your OTP for registration on CARTRAC client applicatiopm is " + sOTPForMail + "<br><br>" +
"Thank you for using our service.";
                        Thread newThread = new Thread(ChatHub.SendEmail_Doom);
                        newThread.Start(mailDoc);
                    }
                    else
                    {
                        clientResponse.otp = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                clientResponse.result = "Error-UpdateClientProfile-" + ex.Message;
            }

            return Json(clientResponse, JsonRequestBehavior.AllowGet);
        }

        /*
        [HttpPost]
        public ActionResult LoadDriverInfo(string imei,string profile)
        {
            var objProfile = new OnDriverInfoResponse();
            objProfile.status = false;
            objProfile.result = "None";

            try
            {
                string sSQL = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = "SELECT m_FName,m_MName,m_LName,m_StaffID,m_Country,m_RegNo,m_TaxiType FROM " + MyGlobal.activeDB + ".tbl_drivers where m_DeviceIMEI='" + imei + "' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    objProfile.dvrPhoto = this.GetFieldString(reader, 3) + ".jpg";
                                    objProfile.dvrName = this.GetFieldString(reader, 0);
                                    objProfile.dvrDesignation = "Driver";
                                    objProfile.staffID = this.GetFieldString(reader, 3);
                                    objProfile.regNo = this.GetFieldString(reader, 5);
                                    objProfile.taxiType = this.GetFieldString(reader, 6);
                                    objProfile.country = this.GetFieldString(reader, 4);


                                    objProfile.status = true;
                                    objProfile.result = "Done";
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                objProfile.result = "Error-" + ex.Message;
            }
            return Json(objProfile, JsonRequestBehavior.AllowGet);
        }
        */
        [HttpPost]
        public ActionResult LoadDriverInfo(string imei, string profile)
        {
            var objProfile = new OnDriverInfoResponse();
            objProfile.status = false;
            objProfile.result = "None";

            try
            {
                string sSQL = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //sSQL = "SELECT m_FName,m_MName,m_LName,m_StaffID,m_Country,m_RegNo,m_TaxiType FROM " + MyGlobal.activeDB + ".tbl_drivers where m_DeviceIMEI='" + imei + "' and m_Profile='" + profile + "';";
                    sSQL = "SELECT m_FName, m_MName, m_LName, m_StaffID, m_Country, m_RegNo, m_TaxiType,device.m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices device " +
"left join " + MyGlobal.activeDB + ".tbl_assignment assign on assign.m_DeviceIMEI = device.m_IMEI and assign.m_Profile = device.m_Profile " +
"left join " + MyGlobal.activeDB + ".tbl_drivers driver on driver.m_StaffID = assign.m_DriverID1 " +
"where device.m_IMEI = '" + imei + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    objProfile.dvrPhoto = this.GetFieldString(reader, 3) + ".jpg";
                                    objProfile.dvrName = this.GetFieldString(reader, 0);
                                    objProfile.dvrDesignation = "Driver";
                                    objProfile.staffID = this.GetFieldString(reader, 3);
                                    objProfile.country = this.GetFieldString(reader, 4);
                                    objProfile.regNo = this.GetFieldString(reader, 5);
                                    objProfile.taxiType = this.GetFieldString(reader, 6);
                                    objProfile.profile = this.GetFieldString(reader, 7);


                                    objProfile.status = true;
                                    objProfile.result = "Done";
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                objProfile.result = "Error-LoadDriverInfo-" + ex.Message;
            }
            return Json(objProfile, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult MeterStatusUpdate(string imei, string imeiclient, string jobid,
            string status, string tripno, string wait, string kms, string pay,string profile)
        {
            var onResponse = new onResponse();
            onResponse.status = false;
            onResponse.result = "";
            //__________________Send to Client
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;

            MeterData meterData = new MeterData();
            meterData.imei = imei;
            meterData.imeiclient = imeiclient;
            meterData.jobid = jobid;
            meterData.status = status;
            meterData.tripno = tripno;
            meterData.wait = wait;
            meterData.kms = kms;
            meterData.pay = pay;
            List<String> lstListConnections1 = hub.GetClientConnections(imeiclient);
            if (lstListConnections1 != null)
            {
                foreach (String connectionID in lstListConnections1)
                {
                    hubContext.Clients.Client(connectionID).MeterStatusToClient(meterData);
                }
            }
            onResponse.status = true;
            onResponse.result = "";
            return Json(onResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult JobDecision(string imei, string jobid, string mode,string src,string profile)
        {
            var jobDecisionResponse = new JobDecisionResponse();
            jobDecisionResponse.status = false;
            jobDecisionResponse.result = "None";
            jobDecisionResponse.responsemode = 0;
            jobDecisionResponse.mode = mode;
            jobDecisionResponse.jobid = jobid;
            jobDecisionResponse.src = src;
            try
            {
                string sSQL = "", sAssignedTo = "", sClientIMEI = "", staffid = "", dvrname = "";
                string regno = "", taxitype = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("deny"))
                    {
                        jobDecisionResponse.status = true;
                        jobDecisionResponse.responsemode = 1;
                    }
                    else
                    {
                        sSQL = "select m_IMEI,m_AssignedTo from " + MyGlobal.activeDB + ".tbl_jobs_doom where m_id='" + jobid + "' and m_Profile='" + profile + "';";// and m_AssignedTo is not null;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0)) sClientIMEI = reader.GetString(0);
                                        if (!reader.IsDBNull(1)) sAssignedTo = reader.GetString(1);
                                    }
                                }
                            }
                        }
                        if (sAssignedTo.Length > 0)
                        {
                            jobDecisionResponse.status = true;
                            jobDecisionResponse.responsemode = 2;
                            jobDecisionResponse.result = "Sorry. Already Assigned";
                        }
                        else
                        {
                            //_____________________________Get Staff ID from drivers table
                            //sSQL = "select m_StaffID,m_FName,m_RegNo,m_TaxiType from " + MyGlobal.activeDB + ".tbl_drivers where m_DeviceIMEI='" + imei + "';";
                            sSQL = @"select assign.m_DriverID1,assign.m_RegNo,driver.m_FName from " + MyGlobal.activeDB + ".tbl_assignment as assign "+
"left join " + MyGlobal.activeDB + ".tbl_drivers as driver on driver.m_StaffID = assign.m_DriverID1 and driver.m_Profile = '" + profile + "' where assign.m_DeviceIMEI = '" + imei + "' and assign.m_Profile='" + profile + "';";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) staffid = reader.GetString(0);
                                            if (!reader.IsDBNull(1)) regno = reader.GetString(1);
                                            if (!reader.IsDBNull(2)) dvrname = reader.GetString(2);
                                            //if (!reader.IsDBNull(3)) taxitype =  reader.GetString(3);

                                        }
                                    }
                                }
                            }
                            //______________________________Assign Job
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_jobs_doom Set m_AssignedTo='" + imei + "',m_AssignedToStaffID='" + staffid + "',m_TimeAssigned=Now() where m_id='" + jobid + "' and m_Profile='" + profile + "';";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                                //__________________Send to Driver
                                jobDecisionResponse.status = true;
                                jobDecisionResponse.responsemode = 1;
                                jobDecisionResponse.result = "Job assigned to you";
                            }
                            //___________________Job Assigned. Inform this to client

                            //__________________Send to Client
                            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
                            var hub = hd.ResolveHub("ChatHub") as ChatHub;
                            
                            JobMessage jobMessage = new JobMessage();
                            jobMessage.Mode = "assigned";
                            jobMessage.JobID = jobid;
                            jobMessage.DriverIMEI = imei;
                            jobMessage.StaffID = staffid;
                            jobMessage.DvrName = dvrname;
                            jobMessage.RegNo = regno;
                            jobMessage.TaxiType = taxitype;
                            jobMessage.src = src;
                            List<String> lstListConnections1;
                            if (src.Equals("CallCenter"))
                            {
                                lstListConnections1 = hub.GetBrowserConnections(sClientIMEI);
                            }
                            else
                            {
                                lstListConnections1 = hub.GetClientConnections(sClientIMEI);
                            }

                            if (lstListConnections1 != null)
                            {
                                foreach (String connectionID in lstListConnections1)
                                {

                                    hubContext.Clients.Client(connectionID).JobMessageToClient(jobMessage);
                                }
                            }
                            List<string> _subscribelist = new List<string>();
                            _subscribelist.Add(imei);
                            hub.SetSubscribeList(sClientIMEI, _subscribelist);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                jobDecisionResponse.result = "Error-JobDecision-" + ex.Message;
            }
            return Json(jobDecisionResponse, JsonRequestBehavior.AllowGet);
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
        public ActionResult StartJob(string imei, LatLng pickloc, LatLng droploc,
            string pickadd, string dropadd, string TaxiType,
            string distance, string duration, string fare, string src,string profile)
        {
            var jobRequestResponse = new JobRequestResponse();
            jobRequestResponse.status = false;
            jobRequestResponse.result = "None";
            jobRequestResponse.job_id = 0;
            long m_id_LastInserted = -1;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            try
            {
                string sSQL = "", sSQLFinal = "";
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_jobs_doom (" +
    "m_IMEI,m_PickLat,m_PickLng,m_DropLat,m_DropLng," +
    "m_PickAddress,m_DropAddress,m_VehicleType,m_TimeCreated,m_Source,m_Profile) values (" +
    "'" + imei + "','" + pickloc.lat + "','" + pickloc.lng + "'," +
    "'" + droploc.lat + "','" + droploc.lng + "'," +
    "'" + pickadd + "','" + dropadd + "','" + TaxiType + "',Now(),'" + src + "','" + profile + "');";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        m_id_LastInserted = com.LastInsertedId;
                    }
                    //_____________________________________________
                    ClassDispatchMessage cDM = new ClassDispatchMessage();
                    cDM.imeiClient = imei;
                    cDM.imeiDriver = "";
                    cDM.pickloc = pickloc;
                    cDM.droploc = droploc;
                    cDM.pickadd = pickadd;
                    cDM.dropadd = dropadd;
                    cDM.TaxiType = TaxiType;
                    cDM.distance = distance;
                    cDM.duration = duration;
                    cDM.fare = fare;
                    cDM.jobid = m_id_LastInserted.ToString();
                    cDM.src = src;
                    //_____________________________________________
                    sSQL = "SELECT m_IMEI,m_Time,m_Lat,m_Lng FROM " + MyGlobal.activeDB + ".tbl_devices where " +
                        "ABS(m_Lat-" + pickloc.lat + ")< 0.01 and ABS(m_Lng-" + pickloc.lng + ")< 0.01 " +
                        "and m_Time>(" + unixTimestamp + "-100) and m_Time<(" + unixTimestamp + "+100) and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if ((reader["m_IMEI"] != null) && (reader["m_Time"] != null) &&
                                        (reader["m_Lng"] != null) && (reader["m_Lat"] != null))
                                    {
                                        sSQLFinal += "INSERT INTO " + MyGlobal.activeDB + ".tbl_jobs_Assigned " +
                                            "(m_IMEIDriver,m_IMEIClient,m_id_job,m_TimeAssigned,m_Profile) values " +
                                            "('" + reader.GetString(0) + "','" + imei + "','" + m_id_LastInserted + "',Now(),'" + profile + "');";

                                        cDM.imeiDriver = reader.GetString(0); // Driver IMEI
                                        List<String> lstListConnections = hub.GetDriverConnections(cDM.imeiDriver);
                                        if (lstListConnections != null)
                                        {
                                            foreach (String connectionID in lstListConnections)
                                            {
                                                //hubContext.Clients.Client(connectionID).SendJson(cls);
                                                //hub.SendJson(cls);
                                                //hubContext.Clients.All.dispatchJsonToClient(cls);
                                                hubContext.Clients.Client(connectionID).dispatchJsonToDriver(cDM);
                                            }
                                        }
                                    }
                                }

                                jobRequestResponse.status = true;
                            }
                            else
                            {
                                jobRequestResponse.status = false;
                                jobRequestResponse.result = "Sorry!!! No taxies around.";
                            }
                        }
                    }
                    if (sSQLFinal.Length > 0)
                    {
                        using (MySqlCommand com = new MySqlCommand(sSQLFinal, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                    jobRequestResponse.job_id = m_id_LastInserted;
                    //jobRequestResponse.result = sSQL;
                    jobRequestResponse.waittime = 30;
                }
            }
            catch (MySqlException ex)
            {
                jobRequestResponse.result = "Error-StartJob-" + ex.Message;
            }
            return Json(jobRequestResponse, JsonRequestBehavior.AllowGet);
        }
        //_________________________________________________________loadroute
        private Int32 GetInt(string sIn)
        {
            Int32 i = 0;
            if (Int32.TryParse(sIn, out i))
            {
            }
            return i;
        }
        private bool IsDate(string sIn)
        {
            DateTime dt;
            if (DateTime.TryParse(sIn, out dt))
            {
                return true;
            }
            return false; ;
        }
        protected string GetPure(MySqlDataReader reader, string sFieldName)
        {
            if (reader[sFieldName] == null) return "";
            return reader[sFieldName].ToString();
        }
        public static double GetUNIXTime(DateTime dateTime)
        {
            return Math.Round((dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
        }
        protected double GetDouble(MySqlDataReader reader, string sFieldName)
        {
            if (reader[sFieldName] == null) return 0;
            double dblRet = 0;
            if (double.TryParse(reader[sFieldName].ToString(), out dblRet))
            {
                return dblRet;
            }
            return 0;
        }
        [HttpPost]
        public ActionResult loadroute(string deviceid, string dt1, string dt2,string profile)
        {
            DateTime dtFrom, dtTo;
            String sAcc = "10000";
            try
            {
                dtFrom = DateTime.Parse(dt1 + " 00:00", CultureInfo.InvariantCulture);
                dtTo = DateTime.Parse(dt2 + " 23:59", CultureInfo.InvariantCulture);
            }
            catch (ArgumentNullException)
            {
                return Content("ArgumentNullException");
            }
            catch (FormatException)
            {
                return Content("FormatException");
            }
            if (((dtFrom - dtTo).TotalDays > 4) ||
                ((dtFrom - dtTo).TotalDays < -4))
            {
                return Content("Plot time span should be less than 5 days");
            }
            //_______________________Get route string
            string sErr = "";
            string sOut = "<rts>";
            //string sSQL = @"select m_Type,m_Speed,m_Alt,m_Lat,m_Lng,m_Time from log_" + sDeviceIDPlotRequested + " where " + sType + " and m_Time>=" + GetUNIXTime(dtFrom) + "  and m_Time<=" + GetUNIXTime(dtTo) + " group by m_Lat,m_lat order by m_Time desc";
            string sSQL = @"select m_Type,m_Speed,m_Alt,m_Lat,m_Lng,m_Time,m_id_device from dispatch_log.log_" + deviceid + " where m_Time>=" + GetUNIXTime(dtFrom) + "  and m_Time<=" + GetUNIXTime(dtTo) + " and m_Accuracy<" + sAcc + " order by m_Time asc,m_TimeReceived asc";

            int iCnt1 = 0, iCnt0 = 0;
            try
            {
                using (MySqlConnection mySQLconnection = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    mySQLconnection.Open();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, mySQLconnection))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                string sIconType = "0", sMessage = " ";
                                Int32 lLastTime = 0;
                                double lLatLast = 0;
                                double lLngLast = 0;
                                Int32 i32Lapse = 0;
                                string sLastDataReceivedAtAStandingLocation = "", sLastTime = "";
                                while (reader.Read())
                                {
                                    double dblLat = 0;
                                    double dblLng = 0;
                                    try
                                    {
                                        dblLat = GetDouble(reader, "m_Lat");
                                        dblLng = GetDouble(reader, "m_Lng");
                                        if ((dblLat != 0) && (dblLng != 0))
                                        {
                                            double dblLatDiff = Math.Abs(lLatLast - dblLat);
                                            double dblLngDiff = Math.Abs(lLngLast - dblLng);
                                            if ((dblLatDiff > 0.001) || (dblLatDiff < -0.001) || (dblLngDiff > 0.001) || (dblLngDiff < -0.001) || sAcc.Equals("3000"))
                                            {
                                                // Movement detected
                                                Int32 iThisTime = GetInt(GetPure(reader, "m_Time"));
                                                if (lLastTime == 0) lLastTime = iThisTime;
                                                int iLapse = iThisTime - lLastTime;
                                                //if (((dblLatDiff > 0.02) || (dblLatDiff < -0.02) || (dblLngDiff > 0.02) || (dblLngDiff < -0.02)) && (iLapse < 120) && (!sAcc.Equals("3000")))
                                                if (((dblLatDiff > 0.02) || (dblLatDiff < -0.02) || (dblLngDiff > 0.02) || (dblLngDiff < -0.02)) && (iLapse < 120) && (!sAcc.Equals("3000")))
                                                {   // Abnormal movement detected
                                                }
                                                else
                                                {
                                                    if (sLastDataReceivedAtAStandingLocation.Length > 0)
                                                    {
                                                        sOut += sLastDataReceivedAtAStandingLocation;
                                                        sLastDataReceivedAtAStandingLocation = "";
                                                    }
                                                    Int32 iThisTime1 = GetInt(GetPure(reader, "m_Time"));
                                                    if (lLastTime == 0) lLastTime = iThisTime;
                                                    int iLapse1 = iThisTime - lLastTime;
                                                    /*
                                                    if (iLapse > 600)
                                                    {
                                                        sIconType = "1";
                                                        sMessage = "Stopped for nearly " + iLapse / 60 + " minutes";
                                                    }
                                                    else
                                                    {
                                                     * */
                                                    sIconType = "0";
                                                    sMessage = " ";
                                                    //}
                                                    //________________________________________________________________
                                                    lLastTime = iThisTime;
                                                    sOut += GetPure(reader, "m_Type") + "^" + GetPure(reader, "m_Speed") + "^" + GetPure(reader, "m_Alt") + "^" + i32Lapse + "^" + dblLat + "^" + dblLng + "^" + GetPure(reader, "m_Time") + "^" + GetPure(reader, "m_id_device") + "^" + sIconType + "^" + sMessage + "^|";
                                                    lLatLast = dblLat;
                                                    lLngLast = dblLng;
                                                    //sLastTime = GetPure(reader.GetString("m_Time"));
                                                }
                                                iCnt1++;
                                            }
                                            else
                                            {   // No movement. Stationary
                                                iCnt0++;
                                                Int32 iThisTime = GetInt(GetPure(reader, "m_Time"));
                                                if (lLastTime == 0) lLastTime = iThisTime;
                                                int iLapse = iThisTime - lLastTime;
                                                if (iLapse > 600)
                                                {
                                                    sIconType = "1";
                                                    //sMessage = "Stopped for nearly " + iLapse / 60 + " minutes ["+sLastTime+"]";
                                                    sMessage = "Stopped for nearly " + iLapse / 60 + " minutes.";

                                                    sLastDataReceivedAtAStandingLocation =
                                                    GetPure(reader, "m_Type") + "^" + GetPure(reader, "m_Speed") + "^" +
                                                    GetPure(reader, "m_Alt") + "^" + i32Lapse + "^" + dblLat + "^" + dblLng + "^" +
                                                    GetPure(reader, "m_Time") + "^" + GetPure(reader, "m_id_device") + "^" +
                                                    sIconType + "^" + sMessage + "^|";
                                                }
                                            }
                                        }
                                    }
                                    catch (FormatException ex)
                                    {
                                        sErr += "Err1:" + ex.Message + "<br>";
                                    }
                                    catch (OverflowException ex)
                                    {
                                        sErr += "Err2:" + ex.Message + "<br>";
                                    }
                                }
                                if (sLastDataReceivedAtAStandingLocation.Length > 0)
                                {
                                    sOut += sLastDataReceivedAtAStandingLocation;
                                    sLastDataReceivedAtAStandingLocation = "";
                                }
                            }
                            reader.Close();
                            mySQLconnection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Content("Error-loadroute-" + ex.Message);
            }
            sOut += "</rts>";
            if(sErr.Length>0) return Content(sErr);
            return Content(sOut);
        }
        [HttpPost]
        public ActionResult loadroute_v1(string deviceid, string dt1, string dt2, string profile)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            DateTime dtFrom, dtTo;
            String sAcc = "10000";
            try
            {
                dtFrom = DateTime.Parse(dt1 + " 00:00", CultureInfo.InvariantCulture);
                dtTo = DateTime.Parse(dt2 + " 23:59", CultureInfo.InvariantCulture);
            }
            catch (ArgumentNullException)
            {
                return Content("ArgumentNullException");
            }
            catch (FormatException)
            {
                return Content("FormatException");
            }
            if (((dtFrom - dtTo).TotalDays > 4) ||
                ((dtFrom - dtTo).TotalDays < -4))
            {
                return Content("Plot time span should be less than 5 days");
            }
            //_______________________Get route string
            string sErr = "";
            string sOut = "<rts>";
            //string sSQL = @"select m_Type,m_Speed,m_Alt,m_Lat,m_Lng,m_Time from log_" + sDeviceIDPlotRequested + " where " + sType + " and m_Time>=" + GetUNIXTime(dtFrom) + "  and m_Time<=" + GetUNIXTime(dtTo) + " group by m_Lat,m_lat order by m_Time desc";
            string sSQL = @"select m_Type,m_Speed,m_Alt,m_Lat,m_Lng,m_Time,m_id_device from dispatch_log.log_" + deviceid + " where m_Time>=" + GetUNIXTime(dtFrom) + "  and m_Time<=" + GetUNIXTime(dtTo) + " and m_Accuracy<" + sAcc + " order by m_Time asc,m_TimeReceived asc";

            int iCnt1 = 0, iCnt0 = 0;
            try
            {
                using (MySqlConnection mySQLconnection = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    mySQLconnection.Open();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, mySQLconnection))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                string sIconType = "0", sMessage = " ";
                                Int32 lLastTime = 0;
                                double lLatLast = 0;
                                double lLngLast = 0;
                                Int32 i32Lapse = 0;
                                string sLastDataReceivedAtAStandingLocation = "", sLastTime = "";
                                while (reader.Read())
                                {
                                    double dblLat = 0;
                                    double dblLng = 0;
                                    try
                                    {
                                        dblLat = GetDouble(reader, "m_Lat");
                                        dblLng = GetDouble(reader, "m_Lng");
                                        if ((dblLat != 0) && (dblLng != 0))
                                        {
                                            double dblLatDiff = Math.Abs(lLatLast - dblLat);
                                            double dblLngDiff = Math.Abs(lLngLast - dblLng);
                                            if ((dblLatDiff > 0.001) || (dblLatDiff < -0.001) || (dblLngDiff > 0.001) || (dblLngDiff < -0.001) || sAcc.Equals("3000"))
                                            {
                                                // Movement detected
                                                Int32 iThisTime = GetInt(GetPure(reader, "m_Time"));
                                                if (lLastTime == 0) lLastTime = iThisTime;
                                                int iLapse = iThisTime - lLastTime;
                                                //if (((dblLatDiff > 0.02) || (dblLatDiff < -0.02) || (dblLngDiff > 0.02) || (dblLngDiff < -0.02)) && (iLapse < 120) && (!sAcc.Equals("3000")))
                                                if (((dblLatDiff > 0.02) || (dblLatDiff < -0.02) || (dblLngDiff > 0.02) || (dblLngDiff < -0.02)) && (iLapse < 120) && (!sAcc.Equals("3000")))
                                                {   // Abnormal movement detected
                                                }
                                                else
                                                {
                                                    if (sLastDataReceivedAtAStandingLocation.Length > 0)
                                                    {
                                                        sOut += sLastDataReceivedAtAStandingLocation;
                                                        sLastDataReceivedAtAStandingLocation = "";
                                                    }
                                                    Int32 iThisTime1 = GetInt(GetPure(reader, "m_Time"));
                                                    if (lLastTime == 0) lLastTime = iThisTime;
                                                    int iLapse1 = iThisTime - lLastTime;
                                                    /*
                                                    if (iLapse > 600)
                                                    {
                                                        sIconType = "1";
                                                        sMessage = "Stopped for nearly " + iLapse / 60 + " minutes";
                                                    }
                                                    else
                                                    {
                                                     * */
                                                    sIconType = "0";
                                                    sMessage = " ";
                                                    //}
                                                    //________________________________________________________________
                                                    lLastTime = iThisTime;
                                                    sOut += GetPure(reader, "m_Type") + "^" + GetPure(reader, "m_Speed") + "^" + GetPure(reader, "m_Alt") + "^" + i32Lapse + "^" + dblLat + "^" + dblLng + "^" + GetPure(reader, "m_Time") + "^" + GetPure(reader, "m_id_device") + "^" + sIconType + "^" + sMessage + "^|";
                                                    lLatLast = dblLat;
                                                    lLngLast = dblLng;
                                                    //sLastTime = GetPure(reader.GetString("m_Time"));
                                                }
                                                iCnt1++;
                                            }
                                            else
                                            {   // No movement. Stationary
                                                iCnt0++;
                                                Int32 iThisTime = GetInt(GetPure(reader, "m_Time"));
                                                if (lLastTime == 0) lLastTime = iThisTime;
                                                int iLapse = iThisTime - lLastTime;
                                                if (iLapse > 600)
                                                {
                                                    sIconType = "1";
                                                    //sMessage = "Stopped for nearly " + iLapse / 60 + " minutes ["+sLastTime+"]";
                                                    sMessage = "Stopped for nearly " + iLapse / 60 + " minutes.";

                                                    sLastDataReceivedAtAStandingLocation =
                                                    GetPure(reader, "m_Type") + "^" + GetPure(reader, "m_Speed") + "^" +
                                                    GetPure(reader, "m_Alt") + "^" + i32Lapse + "^" + dblLat + "^" + dblLng + "^" +
                                                    GetPure(reader, "m_Time") + "^" + GetPure(reader, "m_id_device") + "^" +
                                                    sIconType + "^" + sMessage + "^|";
                                                }
                                            }
                                        }
                                    }
                                    catch (FormatException ex)
                                    {
                                        sErr += "Err1:" + ex.Message + "<br>";
                                    }
                                    catch (OverflowException ex)
                                    {
                                        sErr += "Err2:" + ex.Message + "<br>";
                                    }
                                }
                                if (sLastDataReceivedAtAStandingLocation.Length > 0)
                                {
                                    sOut += sLastDataReceivedAtAStandingLocation;
                                    sLastDataReceivedAtAStandingLocation = "";
                                }
                            }
                            reader.Close();
                            mySQLconnection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Content("Error-loadroute_v1-" + ex.Message);
            }
            sOut += "</rts>";
            if (sErr.Length > 0)
            {
                postResponse.result = sErr;
            }
            else
            {
                postResponse.result = sOut;
            }
            //return Content(sOut);
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
    }
}