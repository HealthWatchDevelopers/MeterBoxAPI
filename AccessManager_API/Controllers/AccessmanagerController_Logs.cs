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
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public partial class AccessmanagerController : Controller
    {
        
        public ActionResult GetMaterLog(string profile, string sort, string order,
            string page, string search,string staff_concern)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var masterlogResponse = new MasterlogResponse();
            masterlogResponse.status = false;
            masterlogResponse.result = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_id like '%" + search + "%' or " +
                        "m_id like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_masterlog " +
    "where " + sSearchKey + " and m_Profile='" + profile + "' ";
                    if (staff_concern.Length > 0)
                        sSQL += "and m_StaffID_Concern='" + staff_concern + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) masterlogResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    //and m_Profile='" + profile + "'
                    int iPageSize = 10;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Time";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    sSQL = "SELECT * from " + MyGlobal.activeDB + ".tbl_masterlog " +
                    "where " + sSearchKey + " and m_Profile='' or m_Profile='" + profile + "' ";
                    if (staff_concern.Length > 0)
                        sSQL += "and m_StaffID_Concern='" + staff_concern + "'";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    MasterlogRow row = new MasterlogRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                    row.m_StaffName = GetStaffName_(profile, row.m_StaffID);
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID_Concern"))) row.m_StaffID_Concern = reader.GetString(reader.GetOrdinal("m_StaffID_Concern"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Time"))) row.m_Time = reader.GetString(reader.GetOrdinal("m_Time"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IP"))) row.m_IP = reader.GetString(reader.GetOrdinal("m_IP"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ConcernTable"))) row.m_ConcernTable = reader.GetString(reader.GetOrdinal("m_ConcernTable"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Changes"))) row.m_Changes = reader.GetString(reader.GetOrdinal("m_Changes"));

                                    masterlogResponse.items.Add(row);
                                }
                                masterlogResponse.status = true;
                                masterlogResponse.result = "Done";
                            }
                            else
                            {
                                masterlogResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                masterlogResponse.result = "Error-" + ex.Message;
            }
            return Json(masterlogResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------------
        public ActionResult GetLoginActivities(string profile, string sort, string order,
            string page, string search)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var loginActivitiesResponse = new LoginActivitiesResponse();
            loginActivitiesResponse.status = false;
            loginActivitiesResponse.result = "";

            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_StaffID like '%" + search + "%' or " +
                        "m_Name like '%" + search + "%' or " +
                        "m_Email like '%" + search + "%' or " +
                        "m_IP like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_login_activity " +
    "where " + sSearchKey + " and (m_Profile is null or m_Profile='' or m_Profile='" + profile + "') " +
    "order by m_Time desc;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) loginActivitiesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    //and m_Profile='" + profile + "'
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Time";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    sSQL = "SELECT * from " + MyGlobal.activeDB + ".tbl_login_activity ";
                    sSQL += "where " + sSearchKey + " and (m_Profile is null or m_Profile='' or m_Profile='" + profile + "') ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LoginActivityRow row = new LoginActivityRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_User"))) row.m_User = reader.GetString(reader.GetOrdinal("m_User"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) row.m_Name = reader.GetString(reader.GetOrdinal("m_Name"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Time"))) row.m_Time = reader.GetString(reader.GetOrdinal("m_Time"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Activity"))) row.m_Activity = reader.GetString(reader.GetOrdinal("m_Activity"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) row.m_Status = reader.GetString(reader.GetOrdinal("m_Status"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IP"))) row.m_IP = reader.GetString(reader.GetOrdinal("m_IP"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Browser"))) row.m_Browser = reader.GetString(reader.GetOrdinal("m_Browser"));

                                    loginActivitiesResponse.items.Add(row);
                                }
                                loginActivitiesResponse.status = true;
                                loginActivitiesResponse.result = "Done";
                            }
                            else
                            {
                                loginActivitiesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                loginActivitiesResponse.result = "Error-" + ex.Message;
            }
            return Json(loginActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------------
        public ActionResult GetLeaveActivities(string profile, string sort, string order,
    string page, string search)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var leaveActivitiesResponse = new LeaveActivitiesResponse();
            leaveActivitiesResponse.status = false;
            leaveActivitiesResponse.result = "";

            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (m_StaffID like '%" + search + "%' or " +
                        "m_From like '%" + search + "%' or " +
                        "m_To like '%" + search + "%' or " +
                        "m_LeaveType like '%" + search + "%') ";


                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_messages " +
                    "where (m_Message like '%<span style=''color:darkgreen;''><b>Approved by %'  " +
                    "or m_Message like '%revoked by%') " +
                    "and " + sSearchKey + " and m_Profile='" + profile + "' ";


                    sSQL = "select count(m_id) as cnt from (" +
"select m_Profile,m_id,m_Time,m_Session,m_Message from " + MyGlobal.activeDB + ".tbl_messages where (m_Message like '%<span style=''color:darkgreen;''><b>Approved by %' or m_Message like '%revoked by%') " +
"union " +
"select m_Profile,m_id,m_Time,'',m_Description as m_Message from " + MyGlobal.activeDB + ".tbl_leave " +
") as z " +
"where  m_Profile='" + profile + "' ";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) leaveActivitiesResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    //and m_Profile='" + profile + "'
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Time";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";


                    sSQL = "select m_id,m_Time as m_TimeApproved,m_Session,m_Message from " + MyGlobal.activeDB + ".tbl_messages " +
"where (m_Message like '%<span style=''color:darkgreen;''><b>Approved by %' or m_Message like '%revoked by%') " +
"and " + sSearchKey + " and m_Profile='" + profile + "' " +
"order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    sSQL = "select m_id,m_Time as m_TimeApproved,m_Session,m_Message,m_StaffID,m_Dr,m_Cr,m_Type from (" +
"select m_Profile,m_id,m_Time,m_Session,m_Message,m_StaffID,0 as m_Dr,0 as m_Cr,'' as m_Type from " + MyGlobal.activeDB + ".tbl_messages where (m_Message like '%<span style=''color:darkgreen;''><b>Approved by %' or m_Message like '%revoked by%') " +
"union " +
"select m_Profile,m_id,m_Time,'',m_Description as m_Message,m_StaffID,m_Dr,m_Cr,m_Type from " + MyGlobal.activeDB + ".tbl_leave " +
") as z " +
"where  m_Profile='" + profile + "' " +
"order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LeaveActivityRow row = new LeaveActivityRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_TimeApproved")))
                                    {

                                        row.m_TimeApproved = reader.GetString(reader.GetOrdinal("m_TimeApproved"));
                                        row.m_Time = row.m_TimeApproved;
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Cr")) && !reader.IsDBNull(reader.GetOrdinal("m_Dr"))) {
                                        if (reader.GetDouble(reader.GetOrdinal("m_Cr")) > 0) row.m_Days = reader.GetString(reader.GetOrdinal("m_Cr"));
                                        if (reader.GetDouble(reader.GetOrdinal("m_Dr")) > 0) row.m_Days ="-"+ reader.GetString(reader.GetOrdinal("m_Dr"));
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type")))
                                    {
                                        row.m_LeaveType = reader.GetString(reader.GetOrdinal("m_Type"));
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                    if (reader.GetString(reader.GetOrdinal("m_Message")).IndexOf("approved", StringComparison.CurrentCultureIgnoreCase)>-1)
                                    {
                                        row.m_Type = "Approved";
                                    }
                                    else if (reader.GetString(reader.GetOrdinal("m_Message")).IndexOf("revoked", StringComparison.CurrentCultureIgnoreCase) > -1)
                                    {
                                        row.m_Type = "Revoked";
                                    }
                                    GetLeaveRowDetails( profile, reader.GetString(reader.GetOrdinal("m_Session")), row);

                                    leaveActivitiesResponse.items.Add(row);
                                }
                                leaveActivitiesResponse.status = true;
                                leaveActivitiesResponse.result = "Done";
                            }
                            else
                            {
                                leaveActivitiesResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                leaveActivitiesResponse.result = "Error-" + ex.Message;
            }
            return Json(leaveActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        private void GetLeaveRowDetails(string profile, string session, LeaveActivityRow row)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_messages " +
                "where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                "and m_LeaveStatus is not null and (m_LeaveStatus = 9 or m_LeaveStatus = 7) order by m_id limit 1;";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("m_Time"))) row.m_Time = reader.GetString(reader.GetOrdinal("m_Time"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_From"))) row.m_From = reader.GetString(reader.GetOrdinal("m_From"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_To"))) row.m_To = reader.GetString(reader.GetOrdinal("m_To"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_Year")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("m_Month")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("m_Day")))
                                {
                                    row.m_Date =
                                        MyGlobal.Right("0" + reader.GetString(reader.GetOrdinal("m_Day")), 2) + "-" +
                                        MyGlobal.Right("0" + reader.GetString(reader.GetOrdinal("m_Month")), 2) + "-" +
                                        MyGlobal.Right("0" + reader.GetString(reader.GetOrdinal("m_Year")), 2);
                                }
                                if (!reader.IsDBNull(reader.GetOrdinal("m_LeaveType"))) row.m_LeaveType = reader.GetString(reader.GetOrdinal("m_LeaveType"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_Days"))) row.m_Days = reader.GetString(reader.GetOrdinal("m_Days"));

                            }
                        }
                    }
                }
            }
        }
    }
}
 
