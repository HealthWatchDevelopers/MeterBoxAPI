using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class GreyMobileController : Controller
    {
        // GET: GreyMobile
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult MobileReq(string imei, LatLng latlng, int accuracy,
            string email, string data, string mode)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var mobileReqResponse = new MobileReqResponse();
            mobileReqResponse.status = false;
            mobileReqResponse.result = "";
            mobileReqResponse.result = mode; // return
            mobileReqResponse.regstatus = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("register"))
                    {
                        //if (imei.Length != 15)
                        if (imei.Length < 15)
                        {
                            mobileReqResponse.result = "Invalid IMEI received";
                            return Json(mobileReqResponse, JsonRequestBehavior.AllowGet);
                        }
                        string status = "";
                        sSQL = "select m_Status,m_Profile,m_StaffID,m_FName " +
                            "from " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where (m_Email='" + email + "' " +
                            "or m_StaffID ='" + email + "' " +
                            "or m_Username ='" + email + "') " +
                            "and m_Password='" + data + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        status = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        mobileReqResponse.profile = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        mobileReqResponse.staffid = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        mobileReqResponse.m_StaffName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    }
                                }
                            }
                        }
                        if (status.Equals("Active", StringComparison.OrdinalIgnoreCase) || status.Equals("Trainee", StringComparison.OrdinalIgnoreCase))
                        {
                            Int32 m_id_exists = 0;
                            sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_mobile_users " +
                                "where m_IMEI='" + imei + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            m_id_exists = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                        }
                                    }
                                }
                            }
                            if (m_id_exists > 0)
                            {
                                mobileReqResponse.result = "Account already Exists";
                            }
                            else
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_mobile_users " +
                                    "(m_Profile,m_imei,m_Status,m_StaffID) values " +
                                    "('" + mobileReqResponse.profile + "','" + imei + "'," +
                                    "'" + status + "','" + mobileReqResponse.staffid + "')";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                    mobileReqResponse.result = "Mobile registered";
                                }
                            }
                        }
                        else
                        {
                            if (status.Length == 0)
                            {
                                mobileReqResponse.result = "Invalid Login Credentials";
                            }
                            else
                            {
                                mobileReqResponse.result = "Account status is " + status;
                            }
                        }
                    }//"register" END

                    //-------------------------Get Profile Info----------------------------------
                    //sSQL = "select m_Profile,m_Status,m_StaffID " +
                    //  "from " + MyGlobal.activeDB + ".tbl_mobile_users " +
                    //"where m_imei='" + imei + "'";

                    sSQL = "select users.m_Profile,users.m_Status,users.m_StaffID,m_Team,m_FName,staffs.m_Email from " + MyGlobal.activeDB + ".tbl_mobile_users users " +
                    "left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = users.m_StaffID and staffs.m_Profile = users.m_Profile " +
                    "where m_imei = '" + imei + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    mobileReqResponse.profile = reader.IsDBNull(0) ? "" : reader.GetString(0);

                                    if (!reader.IsDBNull(1))
                                    {
                                        if (reader.GetString(1).Equals("active", StringComparison.CurrentCultureIgnoreCase) ||
                                            reader.GetString(1).Equals("trainee", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            mobileReqResponse.regstatus = 1;
                                        }
                                    }
                                    mobileReqResponse.staffid = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    mobileReqResponse.m_Team = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    mobileReqResponse.m_StaffName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    mobileReqResponse.email = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------Get Login Geo Locations
                    //sSQL = "select * from " + MyGlobal.activeDB + ".tbl_mobile_team_locations " +
                    //  "where m_Profile='" + mobileReqResponse.profile + "' " +
                    //"and m_TeamName='" + mobileReqResponse.m_Team + "' " +
                    //"order by m_LocationName;";

                    sSQL = "select m_LocationName,m_Lat,m_Lng,m_Accuracy from " + MyGlobal.activeDB + ".tbl_mobile_team_locations team " +
                    "left join " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations locs " +
                    "on locs.m_Profile = team.m_Profile and locs.m_Name = team.m_LocationName " +
                    "where team.m_Profile = '" + mobileReqResponse.profile + "' " +
                    "and (m_TeamName = '" + mobileReqResponse.m_Team + "' or m_TeamName='" + mobileReqResponse.staffid + "')" +
                    "order by m_LocationName;";
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
                                        Landmark mrk = new Landmark();
                                        mrk.Name = MyGlobal.GetPureString(reader, "m_LocationName");
                                        mrk.Lat = MyGlobal.GetPureDouble(reader, "m_Lat");
                                        mrk.Lng = MyGlobal.GetPureDouble(reader, "m_Lng");
                                        mrk.Accuracy = MyGlobal.GetPureInt16(reader, "m_Accuracy");
                                        mobileReqResponse.landmarks.Add(mrk);
                                    }
                                }
                            }
                        }
                    }
                    //---------------------Get Shift Info------------------------
                    Int32 unixTimestampDayStart = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    Int32 unixTimestampDayNow = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    DateTime tme = DateTime.Now;
                    int iYear = tme.Year;
                    int iMonth = tme.Month - 1;
                    int iDay = tme.Day;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile = '" + mobileReqResponse.profile + "' " +
                        "and m_StaffID = '" + mobileReqResponse.staffid + "' " +
                        "and m_Year = '" + iYear + "' " +
                        "and m_Month = '" + iMonth + "' and " +
                        "( " +
                            "( " +
                                unixTimestampDayNow + " >= (" + (unixTimestampDayStart - MyGlobal.const_SHIFT_PRE_TIME) + " + m_ShiftStartTime) " +
                                "and " + unixTimestampDayNow + " < (" + (unixTimestampDayStart + MyGlobal.const_SHIFT_POST_TIME) + " + m_ShiftEndTime)" +
                            ") " +
                            "or " +
                            "(" +
                                "m_ShiftEndTime>86400 and " +
                                    "( " +
                                    unixTimestampDayNow + " >= (" + (unixTimestampDayStart - 86400 - MyGlobal.const_SHIFT_PRE_TIME) + " + m_ShiftStartTime) " +
                                    "and " + unixTimestampDayNow + " < (" + (unixTimestampDayStart - 86400 + MyGlobal.const_SHIFT_POST_TIME) + " + m_ShiftEndTime)" +
                                    ") " +
                            ")" +
                        ")";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Int32 shiftStart = 0, shiftEnd = 0;
                                    shiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                    shiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");

                                    if (shiftEnd > 86400) // Cross over shift
                                    {
                                        if ((unixTimestampDayNow - unixTimestampDayStart) < 32400) // started 3rd part of the day
                                        {
                                            mobileReqResponse.yesterday = "Yesterday";
                                            if (iDay > 1) iDay--;
                                        }
                                    }
                                    string Day = "m_Day" + iDay;
                                    int ordinal = reader.GetOrdinal(Day);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        mobileReqResponse.rosteroption = reader.GetString(ordinal);
                                        if (mobileReqResponse.rosteroption.Length > 0)
                                        {
                                            mobileReqResponse.m_StaffName = MyGlobal.GetPureString(reader, "m_StaffName");
                                            mobileReqResponse.roster = MyGlobal.GetPureString(reader, "m_RosterName");
                                            mobileReqResponse.shift = MyGlobal.GetPureString(reader, "m_ShiftName");
                                            mobileReqResponse.shiftstart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                            mobileReqResponse.shiftend = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------
                    HubObject hubObj = new HubObject();
                    hubObj.Mode = "mobiledata";
                    hubObj.sData = mobileReqResponse.staffid;

                    Int32 PreviousDay = 0; //Pick information from previous day
                    if (mobileReqResponse.yesterday.Equals("Yesterday")) PreviousDay = 86400;
                    //----------------------SignIn & SignOut Commands
                    if (mode.Equals("signin"))
                    {
                        if (mobileReqResponse.roster.Length > 0) // Valid Shift exists
                        {
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "(m_HardwareID,m_Staff,m_StaffID,m_Activity,m_ActivityTime," +
                                "m_WorkTime,m_ReasonHead,m_ReasonNote,m_Lat,m_Lng,m_IP,m_Profile) values " +
                                "('" + imei + "','" + mobileReqResponse.m_StaffName + "'," +
                                "'" + mobileReqResponse.staffid + "','open','" + (unixTimestampDayNow - 19800) + "'," +
                                "'0','SignIn','','" + latlng.lat + "','" + latlng.lng + "','mobile','" + mobileReqResponse.profile + "');";
                                myCommand.ExecuteNonQuery();

                                myCommand.CommandText = "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendance " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Date," +
                                "m_RosterName,m_ShiftName,m_ShiftStart,m_ShiftEnd,m_ActualStart,m_ActualEnd," +
                                "lWorkhours," +
                                "m_MarkRoster,m_MarkLeave,m_RosterOptions,m_AsOn,m_Mode,m_Source) values " +
                                "('" + mobileReqResponse.profile + "','" + mobileReqResponse.staffid + "'," +
                                "'" + iYear + "','" + iMonth + "','" + unixTimestampDayStart + "'," +
                                "'" + mobileReqResponse.roster + "','" + mobileReqResponse.shift + "'," +
                                "'" + (unixTimestampDayStart + mobileReqResponse.shiftstart - PreviousDay) + "'," +
                                "'" + (unixTimestampDayStart + mobileReqResponse.shiftend - PreviousDay) + "'," +
                                "'" + unixTimestampDayNow + "','" + unixTimestampDayNow + "','0'," +
                                "'" + mobileReqResponse.rosteroption + "','',''," +
                                "'" + unixTimestampDayNow + "','2','mobile') " +
                                "ON DUPLICATE KEY UPDATE " +
                                    "m_ActualEnd = '" + unixTimestampDayNow + "'," +
                                    "m_MarkRoster='" + mobileReqResponse.rosteroption + "'," +
                                    "m_Source='mobile'," +
                                    "m_AsOn='" + unixTimestampDayNow + "'";
                                myCommand.ExecuteNonQuery();
                                //MyGlobal.Log(myCommand.CommandText);

                                myTrans.Commit();
                                mobileReqResponse.result = "Signed IN";

                                hubObj.sMess = "Signed IN. On Roster ► " + mobileReqResponse.roster + " , Shift ► " + mobileReqResponse.shift + " ► " + mobileReqResponse.rosteroption + "";
                                MyGlobal.SendHubObject(mobileReqResponse.profile, hubObj);
                            }
                            catch (Exception e)
                            {
                                myTrans.Rollback();
                                mobileReqResponse.result = "Failed to Signin [" + e.Message + "]";
                            }
                        }
                    }
                    else if (mode.Equals("signout") || mode.Equals("update"))
                    {
                        if (mobileReqResponse.roster.Length > 0) // Valid Shift exists
                        {
                            Int32 lWorktime = 0;
                            string sLastActivity = "";
                            //----------Get last Open or update to calculate the worktime
                            sSQL = "select m_ActivityTime,m_Activity " +
                                                    "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                                    "where m_Profile = '" + mobileReqResponse.profile + "' " +
                                                    "and m_StaffID = '" + mobileReqResponse.staffid + "' " +
                                                    "and m_ActivityTime>=" + (unixTimestampDayStart + mobileReqResponse.shiftstart - 19800 - MyGlobal.const_SHIFT_PRE_TIME - PreviousDay) + " " +
                                                    "and m_ActivityTime<" + (unixTimestampDayStart + mobileReqResponse.shiftend - 19800 + MyGlobal.const_SHIFT_POST_TIME - PreviousDay) +
                                                    " and (m_Activity='open' or m_Activity='update') " +
                                                    " order by m_ActivityTime desc limit 1;";
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
                                                Int32 activityTblNow = unixTimestampDayNow - 19800;
                                                sLastActivity = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                                if (activityTblNow > reader.GetInt32(0))
                                                {
                                                    lWorktime = activityTblNow - reader.GetInt32(0);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //------------------------------------------------------------
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "(m_HardwareID,m_Staff,m_StaffID,m_Activity,m_ActivityTime," +
                                "m_WorkTime,m_ReasonHead,m_ReasonNote,m_Lat,m_Lng,m_IP,m_Profile) values " +
                                "('" + imei + "','" + mobileReqResponse.m_StaffName + "'," +
                                "'" + mobileReqResponse.staffid + "'," +
                                (mode.Equals("signout") ? "'lock'" : "'update'") + "," +
                                "'" + (unixTimestampDayNow - 19800) + "'," +
                                "'" + lWorktime + "'," +
                                (mode.Equals("signout") ? "'SignOut'" : "''") + "," +
                                "'','" + latlng.lat + "','" + latlng.lng + "'," +
                                "'mobile','" + mobileReqResponse.profile + "');";
                                myCommand.ExecuteNonQuery();

                                if (mode.Equals("update") && data.Length > 0)
                                {
                                    myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_update_notes " +
                                        "(m_Profile,m_StaffID,m_ActivityTime,m_Roster,m_Shift,m_Notes) values " +
                                        "('" + mobileReqResponse.profile + "','" + mobileReqResponse.staffid + "'," +
                                        "'" + unixTimestampDayNow + "'," +
                                        "'" + mobileReqResponse.roster + "','" + mobileReqResponse.shift + "'," +
                                        "'" + MyGlobal.Base64Encode(data) + "')";
                                    myCommand.ExecuteNonQuery();
                                }


                                myCommand.CommandText = "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendance " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Date," +
                                "m_RosterName,m_ShiftName,m_ShiftStart,m_ShiftEnd,m_ActualStart,m_ActualEnd," +
                                "lWorkhours," +
                                "m_MarkRoster,m_MarkLeave,m_RosterOptions,m_AsOn,m_Mode,m_Source) values " +
                                "('" + mobileReqResponse.profile + "','" + mobileReqResponse.staffid + "'," +
                                "'" + iYear + "','" + iMonth + "','" + unixTimestampDayStart + "'," +
                                "'" + mobileReqResponse.roster + "','" + mobileReqResponse.shift + "'," +
                                "'" + (unixTimestampDayStart + mobileReqResponse.shiftstart - PreviousDay) + "'," +
                                "'" + (unixTimestampDayStart + mobileReqResponse.shiftend - PreviousDay) + "'," +
                                "'" + unixTimestampDayNow + "','" + unixTimestampDayNow + "','0'," +
                                "'" + mobileReqResponse.rosteroption + "','',''," +
                                "'" + unixTimestampDayNow + "','2','mobile') " +
                                "ON DUPLICATE KEY UPDATE " +
                                    "m_ActualEnd = '" + unixTimestampDayNow + "'," +
                                    "m_MarkRoster='" + mobileReqResponse.rosteroption + "'," +
                                    "m_Source='mobile'," +
                                    "m_AsOn='" + unixTimestampDayNow + "'";
                                myCommand.ExecuteNonQuery();
                                //MyGlobal.Log(myCommand.CommandText);

                                myTrans.Commit();
                                if (mode.Equals("signout"))
                                {
                                    mobileReqResponse.result = "Signed Out";
                                    hubObj.sMess = "Signed Out from Roster ► " + mobileReqResponse.roster + " , Shift ► " + mobileReqResponse.shift + " ► " + mobileReqResponse.rosteroption + "";
                                }
                                else
                                {
                                    mobileReqResponse.result = "Updated";
                                    hubObj.sMess = "Update received. On Roster ► " + mobileReqResponse.roster + " , Shift ► " + mobileReqResponse.shift + " ► " + mobileReqResponse.rosteroption + "";
                                }
                                MyGlobal.SendHubObject(mobileReqResponse.profile, hubObj);
                            }
                            catch (Exception e)
                            {
                                myTrans.Rollback();
                            }

                        }
                    }
                    //----------------------Get Last activities within the shift time-------------------------------------
                    var a = 12;

                    sSQL = "select m_Activity,m_ActivityTime,m_ReasonHead " +
                        "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                        "where m_Profile = '" + mobileReqResponse.profile + "' " +
                        "and m_StaffID = '" + mobileReqResponse.staffid + "' " +
                        "and m_ActivityTime>=" + (unixTimestampDayStart + mobileReqResponse.shiftstart - 19800 - MyGlobal.const_SHIFT_PRE_TIME - PreviousDay) + " " +
                        "and m_ActivityTime<" + (unixTimestampDayStart + mobileReqResponse.shiftend - 19800 + MyGlobal.const_SHIFT_POST_TIME - PreviousDay) +
                        " order by m_ActivityTime;";

                    mobileReqResponse.signedin = 0;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ShiftActivity act = new ShiftActivity();
                                    act.m_Activity = MyGlobal.GetPureString(reader, "m_Activity");
                                    act.m_ActivityTime = MyGlobal.GetPureInt32(reader, "m_ActivityTime");
                                    act.m_ReasonHead = MyGlobal.GetPureString(reader, "m_ReasonHead");
                                    if (act.m_Activity.Equals("open") || act.m_Activity.Equals("update") || act.m_Activity.Equals("approved"))
                                    {
                                        if (mobileReqResponse.actualstart == 0)
                                            mobileReqResponse.actualstart = act.m_ActivityTime + 19800;
                                    }
                                    mobileReqResponse.actualend = act.m_ActivityTime + 19800;
                                    //mobileReqResponse.activities.Add(act);
                                    if (act.m_Activity.Equals("open") || act.m_Activity.Equals("approved")
                                        || act.m_Activity.Equals("update") || act.m_Activity.Equals("lock"))
                                    {
                                        mobileReqResponse.signedin = 1;
                                        if (act.m_ReasonHead.Equals("signout", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            mobileReqResponse.signedin = 0;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------------------------------
                    mobileReqResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MobileInitReq-MySqlException->" + ex.Message);
                mobileReqResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("MobileInitReq-Exception->" + ex.Message);
                mobileReqResponse.result = ex.Message;
            }
            return Json(mobileReqResponse, JsonRequestBehavior.AllowGet);
        }
        //-----------------------------------------------
        [HttpPost]
        public ActionResult MobileSignout(string imei)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var mobileReqResponse = new MobileReqResponse();
            mobileReqResponse.status = false;
            mobileReqResponse.result = "";
            mobileReqResponse.regstatus = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";

                    Int32 m_id_exists = 0;
                    sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_mobile_users " +
                        "where m_IMEI='" + imei + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    m_id_exists = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                }
                            }
                        }
                    }
                    if (m_id_exists == 0)
                    {
                        mobileReqResponse.result = "Sorry. Account does not exists";
                    }
                    else
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_mobile_users " +
                            "where m_id='" + m_id_exists + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            mobileReqResponse.result = "Account Successfully Removed";
                        }
                    }
                    //-----------------------------------------------------------
                    mobileReqResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MobileSignout-MySqlException->" + ex.Message);
                mobileReqResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("MobileSignout-Exception->" + ex.Message);
                mobileReqResponse.result = ex.Message;
            }
            return Json(mobileReqResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------------
        [HttpPost]
        public ActionResult SaveLandmark(string profile, string name, string lat, string lng)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new PostResponse();
            response.status = false;
            response.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                        "where m_Profile = '" + profile + "' " +
                        "and m_Name = '" + name + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                response.result = "The name already exists.<br>Please choose another name <br>or go to [Master Tables ► Geo Locations] menu to manage";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    //-----------------------------------
                    sSQL = "insert into " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                    "(m_Profile,m_Name,m_Lat,m_Lng,m_Accuracy) values " +
                    "('" + profile + "','" + name + "','" + lat + "','" + lng + "','" + 500 + "')";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                        response.result = "New Landmark Created.<br>You can go to [Master Tables ► Geo Locations] menu to manage";
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("SaveLandmark-MySqlException->" + ex.Message);
                response.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("SaveLandmark-Exception->" + ex.Message);
                response.result = ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------------
        [HttpPost]
        public ActionResult ShiftActivities(string profile, string staffid, string imei)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var shiftActivities = new ShiftActivitiesResponse();
            shiftActivities.status = false;
            shiftActivities.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //---------------------Get Shift Info------------------------
                    Int32 unixTimestampDayStart = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    Int32 unixTimestampDayNow = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    shiftActivities.DayStart = unixTimestampDayStart;
                    DateTime tme = DateTime.Now;
                    int iYear = tme.Year;
                    int iMonth = tme.Month - 1;
                    int iDay = tme.Day;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile = '" + profile + "' " +
                        "and m_StaffID = '" + staffid + "' " +
                        "and m_Year = '" + iYear + "' " +
                        "and m_Month = '" + iMonth + "' and " +
                        "( " +
                            "( " +
                                unixTimestampDayNow + " >= (" + (unixTimestampDayStart - MyGlobal.const_SHIFT_PRE_TIME) + " + m_ShiftStartTime) " +
                                "and " + unixTimestampDayNow + " < (" + (unixTimestampDayStart + MyGlobal.const_SHIFT_POST_TIME) + " + m_ShiftEndTime)" +
                            ") " +
                            "or " +
                            "(" +
                                "m_ShiftEndTime>86400 and " +
                                    "( " +
                                    unixTimestampDayNow + " >= (" + (unixTimestampDayStart - 86400 - MyGlobal.const_SHIFT_PRE_TIME) + " + m_ShiftStartTime) " +
                                    "and " + unixTimestampDayNow + " < (" + (unixTimestampDayStart - 86400 + MyGlobal.const_SHIFT_POST_TIME) + " + m_ShiftEndTime)" +
                                    ") " +
                            ")" +
                        ")";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Int32 shiftStart = 0, shiftEnd = 0;
                                    shiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                    shiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                    if (shiftEnd > 86400) // Cross over shift
                                    {
                                        if ((unixTimestampDayNow - unixTimestampDayStart) < 32400) // started 3rd part of the day
                                        {
                                            shiftActivities.yesterday = "Yesterday";
                                            if (iDay > 1) iDay--;
                                            shiftActivities.DayStart = unixTimestampDayStart - 86400;
                                        }
                                    }

                                    string Day = "m_Day" + iDay;
                                    int ordinal = reader.GetOrdinal(Day);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        shiftActivities.rosteroption = reader.GetString(ordinal);
                                        if (shiftActivities.rosteroption.Length > 0)
                                        {
                                            //shiftActivities.staffname = MyGlobal.GetPureString(reader, "m_StaffName");
                                            shiftActivities.roster = MyGlobal.GetPureString(reader, "m_RosterName");
                                            shiftActivities.shift = MyGlobal.GetPureString(reader, "m_ShiftName");
                                            shiftActivities.shiftstart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                            shiftActivities.shiftend = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-------------------------------------------------------
                    Int32 PreviousDay = 0; //Pick information from previous day
                    if (shiftActivities.yesterday.Equals("Yesterday")) PreviousDay = 86400;
                    //--------------------------------------------------------------------
                    sSQL = "select m_Activity,activity.m_ActivityTime,m_ReasonHead,m_Lat,m_Lng,notes.m_Notes " +
                    "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity " +
                    "left join " + MyGlobal.activeDB + ".tbl_update_notes notes on notes.m_ActivityTime=(activity.m_ActivityTime+19800) and notes.m_Profile=activity.m_Profile " +
                    "and notes.m_StaffID = activity.m_StaffID " +
                    "where activity.m_Profile = '" + profile + "' " +
                    "and activity.m_StaffID = '" + staffid + "' " +
                    "and activity.m_ActivityTime>=" + (unixTimestampDayStart + shiftActivities.shiftstart - 19800 - MyGlobal.const_SHIFT_PRE_TIME - PreviousDay) + " " +
                    "and activity.m_ActivityTime<" + (unixTimestampDayStart + shiftActivities.shiftend - 19800 + MyGlobal.const_SHIFT_POST_TIME - PreviousDay) +
                    " order by activity.m_ActivityTime desc;";

                    shiftActivities.signedin = 0;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ShiftActivity act = new ShiftActivity();
                                    act.m_Activity = MyGlobal.GetPureString(reader, "m_Activity");
                                    act.m_ActivityTime = MyGlobal.GetPureInt32(reader, "m_ActivityTime");
                                    act.m_ReasonHead = MyGlobal.GetPureString(reader, "m_ReasonHead");
                                    act.m_Notes = MyGlobal.GetPureString(reader, "m_Notes");
                                    if (act.m_ReasonHead.Length == 0) act.m_ReasonHead = act.m_Activity;
                                    act.m_Lat = MyGlobal.GetPureDouble(reader, "m_Lat");
                                    act.m_Lng = MyGlobal.GetPureDouble(reader, "m_Lng");
                                    if (act.m_Activity.Equals("open") || act.m_Activity.Equals("update") || act.m_Activity.Equals("approved"))
                                    {
                                        if (shiftActivities.actualstart == 0) shiftActivities.actualstart = act.m_ActivityTime + 19800;
                                    }
                                    shiftActivities.actualend = act.m_ActivityTime + 19800;
                                    shiftActivities.activities.Add(act);

                                    if (act.m_Activity.Equals("open") || act.m_Activity.Equals("approved") || act.m_Activity.Equals("update"))
                                    {
                                        shiftActivities.signedin = 1;
                                        if (act.m_ReasonHead.Equals("signout", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            shiftActivities.signedin = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------------------------------
                    shiftActivities.status = true;
                    //---------------------------------------------------------------------
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MobileSignout-MySqlException->" + ex.Message);
                shiftActivities.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("MobileSignout-Exception->" + ex.Message);
                shiftActivities.result = ex.Message;
            }
            return Json(shiftActivities, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------------
        [HttpPost]
        public ActionResult LoadLandmarks(string profile)
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
                    sSQL = @"SELECT m_id,m_Name,m_Lat,m_Lng,m_Accuracy,m_Description 
                    FROM " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                    "where m_Profile='" + profile + "' ";
                    //_____________________________________________________________
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    String id = "", type = "", label = "", title = "", accuracy = "";

                                    if (!reader.IsDBNull(0)) id = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) label = reader.GetString(1);
                                    if (!reader.IsDBNull(4)) accuracy = reader.GetString(4);
                                    if (!reader.IsDBNull(5)) title =
                                            "'" + reader.GetString(5) + "' [Fence of  " + accuracy + " Meters]";

                                    if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                                    {
                                        var aboutAPickup = new AboutAPickup(
                                            id,
                                            type,
                                            new LatLng(reader.GetDouble(2), reader.GetDouble(3)),
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
                pickupResponse.result = "Error-LoadLandmarkLocations-" + ex.Message;
            }
            return Json(pickupResponse, JsonRequestBehavior.AllowGet);
        }
    }
}