using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class ParcelController : Controller
    {
        const int R_NONE = 0;
        const int R_PARTIAL = 1;
        const int R_PENDING_OTP = 5;
        const int R_VERIFIED = 9;

        // GET: Parcel
        public ActionResult Index()
        {
            return View();
        }
        private DateTime GetDate(string sDt)
        {
            DateTime dtPick = DateTime.MinValue;
            try
            {
                //dtPick = DateTime.ParseExact(input.Date+" "+input.Time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                dtPick = DateTime.Parse(sDt, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {

            }
            catch (Exception ex)
            {

            }
            return dtPick;
        }

        [HttpPost]
        public ActionResult OnParcelPicklistInit(string profile, string imei,
            string mode, string selected, string newlocation, LatLng latlng)
        {
            var onParcelPicklistInit = new OnParcelPicklistInit();
            onParcelPicklistInit.status = false;
            onParcelPicklistInit.result = "";
            onParcelPicklistInit.picklist = new List<LocationInfo>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "", usertype = "", sNameCompany = "";
                    //____________________Get/Create picklist client details
                    bool bClientExists = false;
                    sSQL = "select m_type,m_NameCompany from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                        "m_IMEI='" + imei + "' and m_Profile='" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) usertype = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) sNameCompany = reader.GetString(1);
                                    //if (!reader.IsDBNull(2)) companyname = reader.GetString(2);
                                }
                                bClientExists = true;
                            }
                        }

                    }
                    if (!bClientExists)
                    {
                        onParcelPicklistInit.result = "Unauthorized";
                        return Json(onParcelPicklistInit, JsonRequestBehavior.AllowGet);
                    }
                    onParcelPicklistInit.usertype = usertype;
                    //_______________________
                    sSQL = "";
                    if (mode.Equals("new"))
                    {
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist_locations " +
                            "(m_Profile,m_Name,m_CompanyName,m_Lat,m_Lng) values ('" + profile + "','" + newlocation + "','" + sNameCompany + "','" + latlng.lat + "','" + latlng.lng + "');";
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_picklist_locations where " +
                            "m_Profile='" + profile + "' and m_Name='" + selected + "'";
                    }
                    else if (mode.Equals("update"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_picklist_locations " +
                            "Set m_Lat='" + latlng.lat + "',m_Lng='" + latlng.lng + "' " +
                            "where " +
    "m_Profile='" + profile + "' and m_Name='" + selected + "'";
                    }
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            onParcelPicklistInit.result = "Updated";
                        }
                    }
                    //_____________________Get location dropdown items
                    String sClientFilter = "";
                    if (usertype.IndexOf("client") > -1)
                    {
                        if (sNameCompany.Length == 0)
                        {
                            onParcelPicklistInit.result = "Company name not assigned";
                            return Json(onParcelPicklistInit, JsonRequestBehavior.AllowGet);
                        }
                        sClientFilter = " and m_CompanyName='" + sNameCompany + "'";
                    }
                    sSQL = "select m_Name,m_Lat,m_Lng from " + MyGlobal.activeDB + ".tbl_picklist_locations where " +
                        "m_Profile='" + profile + "' " + sClientFilter + " ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LocationInfo locationInfo = new LocationInfo();
                                    if (!reader.IsDBNull(0)) locationInfo.name = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) locationInfo.lat = reader.GetDouble(1);
                                    if (!reader.IsDBNull(2)) locationInfo.lng = reader.GetDouble(2);

                                    onParcelPicklistInit.picklist.Add(locationInfo);
                                }
                                onParcelPicklistInit.status = true;
                            }
                            else
                            {
                                onParcelPicklistInit.result = "No locations assigned yet";

                            }
                        }

                    }
                }
            }
            catch (MySqlException ex)
            {
                onParcelPicklistInit.result = "Error-" + ex.Message;
            }
            return Json(onParcelPicklistInit, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetSlaveUsers(string profile, string imei)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var slaveUsers = new SlaveUsers();
            slaveUsers.status = false;
            slaveUsers.result = "";
            string sSQL = "", sNameCompany = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------------Get company name
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
        "m_Profile='" + profile + "' and m_IMEI='" + imei + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(6)) sNameCompany = reader.GetString(6);
                                }
                            }
                        }
                    }
                    //----------------------Get list
                    sSQL = "select * " +
    "from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
    "m_NameCompany='" + sNameCompany + "' and m_Profile='" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    SlaveUser slaveUser = new SlaveUser();
                                    if (!reader.IsDBNull(0)) slaveUser.m_id = reader.GetInt16(0);
                                    if (!reader.IsDBNull(2)) slaveUser.imei = reader.GetString(2);
                                    if (!reader.IsDBNull(3)) slaveUser.type = reader.GetString(3);
                                    if (!reader.IsDBNull(5)) slaveUser.name = reader.GetString(5);
                                    if (!reader.IsDBNull(10)) slaveUser.mobile = reader.GetString(10);
                                    
                                    slaveUsers.slaveUser.Add(slaveUser);
                                }
                                slaveUsers.status = true;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                slaveUsers.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException:" + ex.Message);
            }
            catch (Exception ex)
            {
                slaveUsers.result = ex.Message;
                MyGlobal.Error("Exception:" + ex.Message);
            }
            return Json(slaveUsers, JsonRequestBehavior.AllowGet);
        }
        private bool MobileAlreadyExists()
        {
            return false;
        }
        [HttpPost]
        public ActionResult OnParcelInit(string profile,string imei,
            string InsertStatus, string InsertResult, string DateSelected,
            string mode,NewUser newUser,int showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var parcelInit = new ParcelInit();
            parcelInit.status = false;
            parcelInit.result = "";
            parcelInit.usertype = "";
            parcelInit.username = "";
            //parcelInit.companyname = "";
            parcelInit.picklist = new List<LocationInfo>();
            parcelInit.picklists = new List<PickList>();
            DateTime dtSelected = GetDate(DateSelected);
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "",sStatus="",sOTP="";
                    //-------------------------------------
                    if (mode.Equals("newstaff"))
                    {
                        //Does mobile already exists?
                        if (MobileAlreadyExists())
                        {

                        }
                        //-------------------------------
                        bool bIsKeyValid = false;
                        sSQL = "select m_UserKey from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                            "m_Profile='" + profile + "' and m_IMEI is null and m_UserKey='"+ newUser.Key + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bIsKeyValid = reader.HasRows;
                            }
                        }
                        if (!bIsKeyValid)
                        {
                            parcelInit.result = "Invalid Key";
                            parcelInit.status = false;
                            return Json(parcelInit, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            String otp = MyGlobal.GetRandomNo(1000, 9999);
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_picklist_users Set "+
                                "m_IMEI='"+imei+"',m_Mobile='"+newUser.Mobile+"',m_Name='"+newUser.Name+"',m_OTP='"+otp+"' " +//m_Status='verified',
                                "where m_Profile = '" + profile + "' and m_IMEI is null and m_UserKey='"+ newUser.Key + "';";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                            MyGlobal.SendSMS("+91", newUser.Mobile, "Dear " + newUser.Name + ". Your 'Parcel Booking' OTP is " + otp + ". Service updated. Thanks for using our service.");
                        }
                    }
                    //____________________Get/Create picklist client details
                    parcelInit.result = "";
againPlease:
                    bool bClientExists = false;
                    sSQL = "select m_type,m_Status,m_NameCompany,m_Name,m_Address,m_City,m_PIN,m_Mobile,m_OTP " +
                        "from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                        "m_IMEI='" + imei + "' and m_Profile='" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) parcelInit.usertype = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) sStatus = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) parcelInit.newUser.NameCompany = reader.GetString(2);
                                    if (!reader.IsDBNull(3)) parcelInit.newUser.Name = reader.GetString(3);
                                    if (!reader.IsDBNull(4)) parcelInit.newUser.Address = reader.GetString(4);
                                    if (!reader.IsDBNull(5)) parcelInit.newUser.City = reader.GetString(5);
                                    if (!reader.IsDBNull(6)) parcelInit.newUser.PIN = reader.GetString(6);
                                    if (!reader.IsDBNull(7)) parcelInit.newUser.Mobile = reader.GetString(7);
                                    if (!reader.IsDBNull(8)) sOTP = reader.GetString(8);
                                }
                                
                                if (sStatus.Equals("verified"))
                                {
                                    parcelInit.userStatus = R_VERIFIED;
                                }
                                else
                                {
                                    parcelInit.userStatus = R_PENDING_OTP;
                                    if (newUser.OTP == null || newUser.OTP.Length < 1)
                                    {
                                        parcelInit.result = "Enter OTP";
                                    }
                                }
                                parcelInit.newUser.OTP = "";
                                bClientExists = true;
                            }
                        }
                    }
                    //--------------------New
                    if (mode.Equals("new"))
                    {
                        if (newUser.NameCompany == null || newUser.NameCompany.Length < 2 ||
                            newUser.Mobile == null || newUser.Mobile.Length < 8 ||
                            newUser.City == null || newUser.City.Length < 2 ||
                            newUser.PIN == null || newUser.PIN.Length < 2
                            )
                        {
                            parcelInit.userStatus = R_PARTIAL;
                            parcelInit.result = "Kindly fill all the fields";
                        }
                        if (bClientExists)
                        {
                            sSQL = "";
                            string sOTPBit = "";
                            if (newUser != null)
                            {
                                if (imei.Length == 15)
                                {
                                    if ((parcelInit.result.Length == 0) && (newUser.OTP != null) && (newUser.OTP.Length > 0))
                                    {
                                        sSQL = "select m_OTP,m_Mobile from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                                        "m_Profile='" + profile + "' and m_IMEI='" + imei + "'";
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
                                                            string sMobile = "";
                                                            if (!reader.IsDBNull(1)) sMobile = reader.GetString(1);
                                                            if (newUser.Mobile.Equals(sMobile))
                                                            {
                                                                if (newUser.OTP.Equals(reader.GetString(0)))
                                                                {
                                                                    sOTPBit = "m_Status = 'verified',";
                                                                }
                                                                else
                                                                {
                                                                    parcelInit.result = "Invalid OTP";
                                                                }
                                                            }
                                                            else
                                                            {
                                                                sOTPBit = "m_Status = 'mobileChanged',";
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (!parcelInit.newUser.Mobile.Equals(newUser.Mobile))
                            {
                                string sPINNew = MyGlobal.GetRandomNo(1000, 9999);
                                sOTPBit = "m_Status = 'mobileChanged',";
                                sOTPBit += "m_OTP = '" + sPINNew + "',";
                                MyGlobal.SendSMS("+91", newUser.Mobile, "Dear " + newUser.NameCompany + ". Mobile changed. Your 'Parcel Booking' OTP is " + sPINNew + ". New account created. Thanks for using our service.");
                            }

                            sSQL = "update " + MyGlobal.activeDB + ".tbl_picklist_users " +
                                    "Set " + sOTPBit +
                                    "m_NameCompany='" + newUser.NameCompany + "'," +
                                    "m_Name='" + newUser.Name + "'," +
                                    "m_Address='" + newUser.Address + "'," +
                                    "m_City='" + newUser.City + "'," +
                                    "m_PIN='" + newUser.PIN + "'," +
                                    "m_Mobile='" + newUser.Mobile + "' " +
                                    "where " +
                                        "m_Profile='" + profile + "' and m_IMEI='" + imei + "'";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                            mode = "";
                            goto againPlease;
                        }
                        else
                        {
                            if (newUser != null)
                            {
                                if (imei.Length == 15)
                                {
                                    String sPINNew = MyGlobal.GetRandomNo(1000, 9999);
                                    sSQL = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist_users " +
    "(m_Profile,m_IMEI,m_OTP,m_Type,m_Name,m_NameCompany,m_Address,m_City,m_PIN,m_Mobile) values " +
            "('" + profile + "','" + imei + "','" + sPINNew + "','client_admin'," +
            "'" + newUser.Name + "','" + newUser.NameCompany + "','" + newUser.Address + "','" + newUser.City + "','" + newUser.PIN + "','" + newUser.Mobile + "');";
                                    using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                                    parcelInit.newUser = newUser;
                                    MyGlobal.SendSMS("+91", newUser.Mobile, "Dear " + newUser.NameCompany + ". Your 'Parcel Booking' OTP is " + sPINNew + ". New account created. Thanks for using our service.");
                                    mode = "";
                                    goto againPlease;
                                }
                            }
                        }
                    }
                    //--------------------------All okay
                    parcelInit.status = true;
                    //_____________________Get location dropdown items
                    sSQL = "select m_Name,m_Lat,m_Lng from " + MyGlobal.activeDB + ".tbl_picklist_locations where " +
                        "m_Profile='" + profile + "' and m_CompanyName='" + parcelInit.newUser.NameCompany + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LocationInfo locationInfo = new LocationInfo();
                                    if (!reader.IsDBNull(0)) locationInfo.name = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) locationInfo.lat = reader.GetDouble(1);
                                    if (!reader.IsDBNull(2)) locationInfo.lng = reader.GetDouble(2);

                                    parcelInit.picklist.Add(locationInfo);
                                }
                            }
                        }

                    }
                    //_____________________Get Picklists
                    String sClientFilter = "";
                    if (parcelInit.usertype.IndexOf("client") > -1)
                    {
                        sClientFilter = " and picklist.m_NameCompany = '" + parcelInit.newUser.NameCompany + "' ";
                    }
                    sSQL = "select picklist.m_id,m_PickLocation," +
                        "DATE_FORMAT(m_PickTime,'%d/%m/%Y %H:%i:%s')," +
                        "m_PickWeight," +
                        "DATE_FORMAT(m_ActivityTime,'%d/%m/%Y %H:%i:%s'),picklist.m_NameCompany," +
                        "location.m_Lat,location.m_Lng,m_ActivityIMEI,users.m_Name " +
                        "from " + MyGlobal.activeDB + ".tbl_picklist picklist " +
                        "left join " + MyGlobal.activeDB + ".tbl_picklist_locations location on " +
                        "location.m_Name=picklist.m_PickLocation and location.m_Profile=picklist.m_Profile " +
                        "left join " + MyGlobal.activeDB + ".tbl_picklist_users users on " +
                        "users.m_IMEI=picklist.m_IMEI and users.m_Profile=picklist.m_Profile " +
                        "where " +
                        "picklist.m_Profile='" + profile + "' and " +
                        "date(picklist.m_PickTime)='" + dtSelected.ToString("yyyy-MM-dd") + "' ";
                    if (showall == 1)
                    {
                    }
                    else
                    {
                        // sSQL += "and picklist.m_Activity<>'cancelled' and picklist.m_Activity is null ";
                        sSQL += "and picklist.m_Activity is null ";
                    }

                        sSQL+="" + sClientFilter + " order by picklist.m_PickTime;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    PickList picklists = new PickList();
                                    picklists.m_ActivityTime = "";
                                    if (!reader.IsDBNull(0)) picklists.m_id = reader.GetInt16(0);
                                    if (!reader.IsDBNull(1)) picklists.m_PickLocation = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) picklists.m_PickTime = reader.GetString(2);
                                    if (!reader.IsDBNull(3)) picklists.m_PickWeight = reader.GetString(3);
                                    if (!reader.IsDBNull(4)) picklists.m_ActivityTime = reader.GetString(4);
                                    if (parcelInit.usertype.IndexOf("staff") > -1)
                                    {
                                        if (!reader.IsDBNull(5)) picklists.m_NameCompany = reader.GetString(5);
                                        if (!reader.IsDBNull(6)) picklists.m_Lat = Math.Round(reader.GetDouble(6),6);
                                        if (!reader.IsDBNull(7)) picklists.m_Lng = Math.Round(reader.GetDouble(7),6);
                                    }
                                    if (!reader.IsDBNull(8)) picklists.m_ActivityIMEI = reader.GetString(8);
                                    if (!reader.IsDBNull(9)) picklists.m_ActivityBy = reader.GetString(9);
                                    parcelInit.picklists.Add(picklists);
                                }
                            }
                        }
                    }
                    //________________________________________
                }
            }
            catch (MySqlException ex)
            {
                parcelInit.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException:" + ex.Message);
            }
            catch(Exception ex)
            {
                parcelInit.result = ex.Message;
                MyGlobal.Error("Exception:" + ex.Message);
            }
            //if (InsertStatus != null) parcelInit.status = InsertStatus.Equals("ok");
            //if (InsertResult != null) parcelInit.result = InsertResult;

            return Json(parcelInit, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetOTPForUser(string profile, string imei)
        {
            var updateStatus = new UpdateStatus();
            updateStatus.status = false;
            updateStatus.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "";
                    string sNameCompany = "", m_Address = "", m_City = "", m_PIN = "", key = "";
                    //___________________________________________________Check validity
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                            "m_Profile='" + profile + "' and m_IMEI='" + imei + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(6)) sNameCompany = reader.GetString(6);
                                    if (!reader.IsDBNull(7)) m_Address = reader.GetString(7);
                                    if (!reader.IsDBNull(8)) m_City = reader.GetString(8);
                                    if (!reader.IsDBNull(9)) m_PIN = reader.GetString(9);
                                }
                            }
                        }
                    }
                    //-------------------------------------
                    if (sNameCompany.Length > 0)
                    {
                        key = MyGlobal.GetRandomNo(100000, 999999);
                        bool bEmptyRowExists = false;
                        //------------------Does an entry already exists?
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
                        "m_Profile='" + profile + "' and m_NameCompany='"+ sNameCompany + "' " +
                        "and m_IMEI is null;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bEmptyRowExists = reader.HasRows;
                            }
                        }
                        //------------------Does an entry already exists END
                        if (bEmptyRowExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_picklist_users " +
                                "Set m_UserKey='"+key+"' "+
                                "where " +
                            "m_Profile='" + profile + "' and m_NameCompany='" + sNameCompany + "' " +
                            "and m_IMEI is null;";
                        }
                        else
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist_users " +
                                "(m_Profile,m_Type,m_NameCompany,m_Address,m_City,m_PIN,m_UserKey) values " +
                                "('" + profile + "','client','" + sNameCompany + "'," +
                                "'" + m_Address + "','" + m_City + "','" + m_PIN + "','" + key + "')";
                        }
                        using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                        updateStatus.result = key;
                        updateStatus.status = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                updateStatus.result = "Error-" + ex.Message;
            }
            return Json(updateStatus, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult OnParcelUpdateStatus(string profile,string imei, string m_id, string mode,string pin)
        {
            var updateStatus = new UpdateStatus();
            updateStatus.status = false;
            updateStatus.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL="";
                    //___________________________________________________Check validity
                    bool bPINOk = false;
                    sSQL = "select m_OTP from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
    "m_Profile='" + profile + "' and m_IMEI='" + imei + "';";
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
                                        bPINOk = reader.GetString(0).Equals(pin);
                                    }
                                }
                            }
                        }
                    }
                    if (!bPINOk)
                    {
                        updateStatus.status = false;
                        updateStatus.result = "Invalid PIN";
                        return Json(updateStatus, JsonRequestBehavior.AllowGet);
                    }
                    //___________________________________________________Do action
                    HubObject obj = new HubObject();
                    obj.Mode = "parcel";
                    if (mode.Equals("pick"))
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist Set m_ActivityIMEI='" + imei + "',m_ActivityTime=Now(),m_Activity='picked' " +
                            "where m_Profile='" + profile + "' and m_id='" + m_id + "';";
                        obj.sData = "picked";
                    }
                    else if (mode.Equals("cancel"))
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist Set m_ActivityIMEI='" + imei + "',m_ActivityTime=Now(),m_Activity='cancelled' " +
                            "where m_Profile='" + profile + "' and m_id='" + m_id + "';";
                        obj.sData = "cancelled";
                    }
                    
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        updateStatus.status = true;
                        updateStatus.result = "Updated";
                        if(obj.sData.Length>0) MyGlobal.SendHubObject_ToBrowser("", obj);
                    }
                }
            }
            catch (MySqlException ex)
            {
                updateStatus.result = "Error-" + ex.Message;
            }
            return Json(updateStatus, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult OnParcelUpdate(string profile,string imei, 
            Input input, string DateSelected,int showall)
        {
            var parcelInit = new ParcelInit();
            parcelInit.status = false;
            parcelInit.result = "";
            string m_NameUser = "";
            string m_NameCompany = "";
            string clientType = "";

            if (input.AmPm.Equals("PM"))
            {
                if (input.Time.Length == 5)
                {
                    int iHour = int.Parse(input.Time.Substring(0, 2)) + 12;
                    if (iHour > 9)
                    {
                        input.Time = iHour + ":00";
                    }
                    else
                    {
                        input.Time = "0" + iHour + ":00";
                    }
                }
            }
            DateTime dtPick = DateTime.MinValue;
            try
            {
                //dtPick = DateTime.ParseExact(input.Date+" "+input.Time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                dtPick = DateTime.Parse(input.Date, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                parcelInit.result = ex.Message + " [" + input.Date + " " + input.Time + "]";
                //return Json(parcelInit, JsonRequestBehavior.AllowGet);
                return OnParcelInit(profile,imei, (parcelInit.status ? "ok" : ""), parcelInit.result, DateSelected,"",null,showall);
            }
            catch (Exception ex)
            {
                parcelInit.result = "Invalid Date [" + ex.Message + "]";
                return OnParcelInit(profile,imei, (parcelInit.status ? "ok" : ""), parcelInit.result, DateSelected, "", null, showall);
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //____________________Get user validity and Client name
                    String sSQL = "";
                    bool bClientOK = false;
                    sSQL = "select m_Name,m_Type,m_NameCompany from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
    "m_Profile='" + profile + "' and m_IMEI='" + imei + "';";
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
                                        m_NameUser = reader.IsDBNull(0)?"": reader.GetString(0);
                                        clientType = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        m_NameCompany = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        bClientOK = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!bClientOK)
                    {
                        if (m_NameUser.Length == 0) parcelInit.result = "Name not updated";
                        else if (clientType.Length==0) parcelInit.result = "Unauthorized";
                        else parcelInit.result = "No record exists";
                    }
                    else
                    {
                        //___________________________________________________________
                        HubObject obj = new HubObject();
                        obj.Mode = "parcel";
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist (m_Profile,m_IMEI,m_NameCompany,m_NameUser,m_PickLocation,m_PickTime,m_PickWeight) values " +
                            "('" + profile + "','" + imei + "','" + m_NameCompany + "','" + m_NameUser + "','" + input.Pickup + "','" + dtPick.ToString("yyyy-MM-dd") + " " + input.Time + "','" + input.Weight + "');";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            obj.sData = "created";
                            parcelInit.status = true;
                            parcelInit.result = "Picklist Created";
                            if (obj.sData.Length > 0) MyGlobal.SendHubObject_ToBrowser("", obj);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                parcelInit.result = "Error-" + ex.Message;
            }


            return OnParcelInit(profile,imei, parcelInit.status ? "ok" : "", parcelInit.result, DateSelected, "", null,showall);
            //return Json(onParcelInit, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LoadDailyCounts(string profile,string imei,string viewmonth)
        {
            var dailyCount = new DailyCount();
            dailyCount.status = false;
            dailyCount.result = "";
            dailyCount.countSet = new List<CountSet>();
            /*
            DateTime dtPick = DateTime.MinValue;
            try
            {
                //dtPick = DateTime.ParseExact(input.Date+" "+input.Time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                dtPick = DateTime.Parse(DateSelected, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                return Json(dailyCount, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(dailyCount, JsonRequestBehavior.AllowGet);
            }
            */
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string clientName = "",clientCompany="", clientType="", sSQL = "";
                    //______________________________________________________Get company name
                    bool bClientOK = false;
                    sSQL = "select m_Name,m_Type,m_NameCompany from " + MyGlobal.activeDB + ".tbl_picklist_users where " +
    "m_Profile='" + profile + "' and m_IMEI='" + imei + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {

                                        clientName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        clientType = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        clientCompany = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        bClientOK = true;

                                }
                            }
                        }
                    }
                    if (!bClientOK)
                    {
                        //if (clientName.Length == 0) dailyCount.result = "Client Name not updated";
                        //else if (clientType.Length == 0) dailyCount.result = "Unauthorized";
                        //else dailyCount.result = "No record exists";
                        dailyCount.result = "No record exists";
                        return Json(dailyCount, JsonRequestBehavior.AllowGet);
                    }

                    //______________________________________________________
                    String sClientFilter = "";
                    if (clientType.IndexOf("client") > -1) sClientFilter = " and m_NameCompany='" + clientCompany + "' ";
                    sSQL = @"select m_PickTime,"+
"sum(Case When m_ActivityIMEI is null Then 1 Else 0 End) as not_picked," +
"sum(Case When m_ActivityIMEI is not null Then 1 Else 0 End) as picked  " +
"from " + MyGlobal.activeDB + ".tbl_picklist where " +
"m_Profile = '" + profile + "' and month(m_PickTime)= '" + viewmonth + "' and (m_Activity<>'cancelled' or m_Activity is null) " + sClientFilter + " group by date(m_PickTime)";
                    //and m_ClientName='" + clientName + "'

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)){
                                        if(reader.GetInt16(1)>0)
                                        {
                                            CountSet countSet = new CountSet();
                                            countSet.key = "X_Y_" + reader.GetDateTime(0).Day;
                                            countSet.value = reader.GetString(1);
                                            dailyCount.countSet.Add(countSet);
                                        }
                                        if (reader.GetInt16(2) > 0)
                                        {
                                            CountSet countSet = new CountSet();
                                            countSet.key = "X_P_" + reader.GetDateTime(0).Day;
                                            countSet.value = reader.GetString(2);
                                            dailyCount.countSet.Add(countSet);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                dailyCount.status = true;
            }
            catch (MySqlException ex)
            {
                dailyCount.result = "Error-" + ex.Message;
            }
            return Json(dailyCount, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetClients(string profile,string sort, string order, string page, string search, string timezone)
        {
            var logisticClientProfilesResponse = new LogisticClientProfilesResponse();
            logisticClientProfilesResponse.status = false;
            logisticClientProfilesResponse.result = "None";
            logisticClientProfilesResponse.total_count = "";

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
                    String sSearchKey = " (m_Name like '%" + search + "%' or " +
                        "m_NameCompany like '%" + search + "%' or " +
                        "m_IMEI like '%" + search + "%' or " +
                        "m_Type like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_picklist_users " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "' and m_Type like '%client%';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) logisticClientProfilesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_NameCompany";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_picklist_users ";
                    sSQL += "where " + sSearchKey + " and m_Profile='" + profile + "' and m_Type like '%client%' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LogisticsClientItem logisticsClientItem = new LogisticsClientItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) logisticsClientItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) logisticsClientItem.m_Name = reader["m_Name"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_NameCompany"))) logisticsClientItem.m_NameCompany = reader["m_NameCompany"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IMEI"))) logisticsClientItem.m_IMEI = reader["m_IMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) logisticsClientItem.m_Type = reader["m_Type"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) logisticsClientItem.m_Mobile = reader["m_Mobile"].ToString();


                                    logisticClientProfilesResponse.items.Add(logisticsClientItem);
                                }
                                logisticClientProfilesResponse.status = true;
                                logisticClientProfilesResponse.result = "Done";
                            }
                            else
                            {
                                logisticClientProfilesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                logisticClientProfilesResponse.result = "Error-" + ex.Message;
            }
            return Json(logisticClientProfilesResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetStaffs(string profile,string sort, string order, string page, string search, string timezone)
        {
            var logisticClientProfilesResponse = new LogisticClientProfilesResponse();
            logisticClientProfilesResponse.status = false;
            logisticClientProfilesResponse.result = "None";
            logisticClientProfilesResponse.total_count = "";

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
                    String sSearchKey = " (m_Name like '%" + search + "%' or " +
                        "m_IMEI like '%" + search + "%' or " +
                        "m_Type like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_picklist_users " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "' and m_Type like '%staff%';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) logisticClientProfilesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_NameUser";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_picklist_users ";
                    sSQL += "where " + sSearchKey + " and m_Profile='" + profile + "' and m_Type like '%staff%' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LogisticsClientItem logisticsClientItem = new LogisticsClientItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) logisticsClientItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) logisticsClientItem.m_Name = reader["m_Name"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_NameCompany"))) logisticsClientItem.m_NameCompany = reader["m_NameCompany"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IMEI"))) logisticsClientItem.m_IMEI = reader["m_IMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) logisticsClientItem.m_Type = reader["m_Type"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) logisticsClientItem.m_Mobile = reader["m_Mobile"].ToString();


                                    logisticClientProfilesResponse.items.Add(logisticsClientItem);
                                }
                                logisticClientProfilesResponse.status = true;
                                logisticClientProfilesResponse.result = "Done";
                            }
                            else
                            {
                                logisticClientProfilesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                logisticClientProfilesResponse.result = "Error-" + ex.Message;
            }
            return Json(logisticClientProfilesResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Update_LogisticsClients(string profile,string mode, string m_id, 
            string m_Name,string m_NameCompany, string m_IMEI, string m_Type, string m_Mobile)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            m_NameCompany = m_NameCompany.Trim();

            string sSQL = "";
            if (mode.Equals("new"))
            {
                string m_id_exists = "";
                string sOutMessage = "";
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();

                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_picklist_users where m_Name='_New' and m_Profile='" + profile + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_id_exists = reader.GetString(0);
                                    }
                                }
                            }
                        }

                        if (m_id_exists.Length == 0) // No rows affected, so create one
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist_users (m_Profile,m_Name,m_Type) values ('" + profile + "','_New','client');";
                            sOutMessage= "New entry created";
                        }
                        else
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist_users Set m_Type='client' where m_id='" + m_id_exists + "'";
                            sOutMessage = "Updated an entry";
                        }
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    sOutMessage = "";
                    postResponse.result = "Error-" + ex.Message;
                }
                if (sOutMessage.Length > 0)
                {
                    postResponse.status = true;
                    postResponse.result = sOutMessage;
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
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist_users Set m_Name='" + m_Name + "',"+
                        "m_NameCompany = '" + m_NameCompany + "',"+
                        "m_IMEI = '" + m_IMEI + "'," +
                        "m_Type='" + m_Type + "',m_Mobile='" + m_Mobile + "' where m_id=" + m_id;
                    //m_StaffID='" + m_StaffID + "'
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
        public ActionResult Update_LogisticsStaffs(string profile,string mode, string m_id, string m_Name, string m_IMEI, string m_Type, string m_Mobile)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";
            string sSQL = "";
            if (mode.Equals("new"))
            {
                string m_id_exists = "";
                string sOutMessage = "";
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_picklist_users where m_Name='_New' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_id_exists = reader.GetString(0);
                                    }
                                }
                            }
                        }



                        if (m_id_exists.Length == 0) // No rows affected, so create one
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_picklist_users (m_Profile,m_Name,m_Type) values ('" + profile + "','_New','staff');";
                            sOutMessage = "New entry entry";
                        }
                        else
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist_users Set m_Type='staff' where m_id='" + m_id_exists + "'";
                            sOutMessage = "Updated an entry";
                        }
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    sOutMessage = "";
                    postResponse.result = "Error-" + ex.Message;
                }
                if (sOutMessage.Length > 0)
                {
                    postResponse.status = true;
                    postResponse.result = sOutMessage;
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
                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_picklist_users Set m_Name='" + m_Name +
                        "',m_Type='" + m_Type + "',m_Mobile='" + m_Mobile + "' where m_id=" + m_id;
                    //m_StaffID='" + m_StaffID + "'
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
        public ActionResult LoadActiveBookings(string profile)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadActiveBookings = new LoadActiveBookings();
            loadActiveBookings.status = false;
            loadActiveBookings.result = "";
            loadActiveBookings.items = new List<LoadActiveBookings_row>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    String sSQL = @"select list.m_id,m_NameCompany,m_NameUser, " +
"sum(Case When m_ActivityIMEI is null Then 1 Else 0 End) as not_picked, " +
"sum(Case When m_ActivityIMEI is not null Then 1 Else 0 End) as picked," +
"location.m_Lat,location.m_Lng " +
"from " + MyGlobal.activeDB + ".tbl_picklist list " +
"left join " + MyGlobal.activeDB + ".tbl_picklist_locations location " +
"on location.m_Name=list.m_PickLocation and location.m_Profile=list.m_Profile " +
"and location.m_CompanyName=list.m_NameCompany " +
"where list.m_Profile = '" + profile + "' ";
                        //sSQL += "and list.m_Activity is null "; //and(list.m_Activity <> 'cancelled' or

                    sSQL += "group by list.m_NameCompany order by m_ActivityTime desc";
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

                                        LoadActiveBookings_row loadActiveBookings_row = new LoadActiveBookings_row();
                                        loadActiveBookings_row.m_id = reader.GetInt16(0);
                                        loadActiveBookings_row.m_NameCompany = reader.GetString(1);
                                        loadActiveBookings_row.m_NameUser = reader.GetString(2);
                                        loadActiveBookings_row.not_picked = reader.GetString(3);
                                        loadActiveBookings_row.picked = reader.GetString(4);
                                        if(!reader.IsDBNull(5))loadActiveBookings_row.m_Lat = reader.GetDouble(5);
                                        if (!reader.IsDBNull(6))loadActiveBookings_row.m_Lng = reader.GetDouble(6);
                                        loadActiveBookings.items.Add(loadActiveBookings_row);
                                    }
                                }
                            }
                        }
                    }
                }
                loadActiveBookings.status = true;
            }
            catch (MySqlException ex)
            {
                loadActiveBookings.result = "Error-" + ex.Message;
            }
            return Json(loadActiveBookings, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadActiveBookingsDetails(string profile,string companyname, int showall)
        {
            var loadActiveBookingsDetails = new LoadActiveBookingsDetails();
            loadActiveBookingsDetails.status = false;
            loadActiveBookingsDetails.result = "";
            loadActiveBookingsDetails.items = new List<LoadActiveBookingsDetails_row>();
            String sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    sSQL = "SELECT picklist.m_id,picklist.m_IMEI, m_PickLocation, m_PickTime, m_PickWeight," +
                        "DATE_FORMAT(m_ActivityTime,'%d/%m/%Y %H:%i:%s')," +
                        "location.m_Lat,location.m_Lng,m_Activity,staff.m_Name " +
                        "FROM " + MyGlobal.activeDB + ".tbl_picklist picklist " +
                        "left join " + MyGlobal.activeDB + ".tbl_picklist_locations location on " +
                        "location.m_Name=picklist.m_PickLocation and location.m_Profile=picklist.m_Profile " +
                        "left join " + MyGlobal.activeDB + ".tbl_picklist_users staff on " +
                        "staff.m_IMEI=picklist.m_IMEI and staff.m_Profile=picklist.m_Profile " +
                        "where picklist.m_Profile = '" + profile + "' and picklist.m_NameCompany = '" + companyname + "' ";
                    if (showall != 1)
                    {
                        sSQL += "and picklist.m_Activity is null ";
                    }
                    sSQL+= "order by m_PickTime,m_PickLocation ";
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

                                        LoadActiveBookingsDetails_row loadActiveBookingsDetails_row = new LoadActiveBookingsDetails_row();
                                        loadActiveBookingsDetails_row.m_id = reader.GetString(0);
                                        loadActiveBookingsDetails_row.m_IMEI = reader.GetString(1);
                                        loadActiveBookingsDetails_row.m_PickLocation = reader.GetString(2);
                                        loadActiveBookingsDetails_row.m_PickTime = reader.GetString(3);
                                        loadActiveBookingsDetails_row.m_PickWeight = reader.GetString(4);
                                        loadActiveBookingsDetails_row.m_ActivityTime = reader.IsDBNull(5)?"":reader.GetString(5);
                                        loadActiveBookingsDetails_row.m_Lat = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);
                                        loadActiveBookingsDetails_row.m_Lng = reader.IsDBNull(7) ? 0 : reader.GetDouble(7);
                                        loadActiveBookingsDetails_row.m_Activity = reader.IsDBNull(8) ? "" : reader.GetString(8);
                                        loadActiveBookingsDetails_row.m_ActivityBy = reader.IsDBNull(9) ? "" : reader.GetString(9);
                                        loadActiveBookingsDetails.items.Add(loadActiveBookingsDetails_row);
                                    }
                                }
                            }
                        }
                    }
                }
                loadActiveBookingsDetails.status = true;
                loadActiveBookingsDetails.result = sSQL;
            }
            catch (MySqlException ex)
            {
                loadActiveBookingsDetails.result = "Error-" + ex.Message;
            }
            return Json(loadActiveBookingsDetails, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getbookinghistory(string profile,string sort, string order, string page, string search, string timezone)
        {
            var bookingHistoryResponse = new BookingHistoryResponse();
            bookingHistoryResponse.status = false;
            bookingHistoryResponse.result = "None";
            bookingHistoryResponse.total_count = "";

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
                        "m_NameCompany like '%" + search + "%' or " +
                        "m_PickLocation like '%" + search + "%' or " +
                        "m_NameUser like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_picklist " +
                        "where " + sSearchKey + " and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) bookingHistoryResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_picklist ";
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
                                    BookingHistoryItem bookingHistoryItem = new BookingHistoryItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) bookingHistoryItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_NameCompany"))) bookingHistoryItem.m_NameCompany = reader["m_NameCompany"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_NameUser"))) bookingHistoryItem.m_NameUser = reader["m_NameUser"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PickTime"))) bookingHistoryItem.m_PickTime = reader["m_PickTime"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PickWeight"))) bookingHistoryItem.m_PickWeight = reader["m_PickWeight"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PickLocation"))) bookingHistoryItem.m_PickLocation = reader["m_PickLocation"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ActivityIMEI"))) bookingHistoryItem.m_ActivityIMEI = reader["m_ActivityIMEI"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ActivityTime"))) bookingHistoryItem.m_ActivityTime = reader["m_ActivityTime"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_WaybillNo"))) bookingHistoryItem.m_WaybillNo = reader["m_WaybillNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Activity"))) bookingHistoryItem.m_Activity = reader["m_Activity"].ToString();

                                    bookingHistoryResponse.items.Add(bookingHistoryItem);
                                }
                                bookingHistoryResponse.status = true;
                                bookingHistoryResponse.result = "Done";
                            }
                            else
                            {
                                bookingHistoryResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                bookingHistoryResponse.result = "Error-" + ex.Message;
            }
            return Json(bookingHistoryResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPickupLocations(string profile, string m_NameCompany)
        {
            var devicesResponse = new DevicesResponse();
            devicesResponse.status = false;
            devicesResponse.result = "None";
            string sSQL = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_picklist_locations ";
                    sSQL += "where m_CompanyName='" + m_NameCompany + "' and m_Profile='" + profile + "';";
                    String sHTMLOut = "";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                sHTMLOut += "<table>";
                                while (reader.Read())
                                {
                                    sHTMLOut += "<tr style='border-bottom:1px solid #ccc;'>";
                                    sHTMLOut += "<td><img src='assets/img/flag24.png'/></td>";
                                        /*
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id")))
                                    {
                                        sHTMLOut += "<td>" + reader.GetInt32(reader.GetOrdinal("m_id")) + "</td>";
                                    }
                                    */
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name")))
                                    {
                                        sHTMLOut += "<td style='padding-left:10px;'>" + reader.GetString(reader.GetOrdinal("m_Name")) + "</td>";
                                    }
                                    sHTMLOut += "</tr>";
                                }
                                sHTMLOut += "</table>";
                                devicesResponse.result = sHTMLOut;
                            }
                            else
                            {
                                devicesResponse.result = "No data exists";
                            }
                            devicesResponse.status = true;
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
        //--------------------------------------------------
    }
}