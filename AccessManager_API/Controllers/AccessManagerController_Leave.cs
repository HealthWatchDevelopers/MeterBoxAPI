using MyHub.Hubs;
using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
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
        private string Get_if_session_exists_for_the_same_day(
                        MySqlConnection con, string profile, string staffid,
                        string year, int month, int iSelectedDay, ref string sFrom, ref string sTo)
        {
            string sSQL = "select m_Session,m_From,m_To from " + MyGlobal.activeDB + ".tbl_messages " +
                "where m_Profile='" + profile + "' " +
                "and m_Year='" + year + "' and m_Month='" + month + "' " +
                "and m_Day='" + iSelectedDay + "' " +
                "and m_LeaveType is not null and m_LeaveStatus is not null "+
                "and m_StaffID='" + staffid + "';";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(1)) sFrom = reader.GetString(1);
                            if (!reader.IsDBNull(2)) sTo = reader.GetString(2);
                            if (!reader.IsDBNull(0)) return reader.GetString(0);

                        }
                    }
                }
            }
            return "";
        }
        private bool IsThisDateValid(int iYearForDB, int iMonthForDB, int i)
        {
            try
            {
                DateTime value = new DateTime(iYearForDB, (iMonthForDB+1), i);
                return true;
            }
            catch (ArgumentOutOfRangeException ex)
            {
            }
            return false;
        }

        private void CreateRowTablesIfNotExists(string profile, string sTableName,string staffid, int iYearForDB, int iMonthForDB)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL =
                        "INSERT INTO " + MyGlobal.activeDB + "." + sTableName + " (m_Profile, m_StaffID, m_Year, m_Month) " +
"SELECT* FROM(SELECT '" + profile + "', '" + staffid + "', '" + iYearForDB + "', '" + iMonthForDB + "') AS tmp " +
"WHERE NOT EXISTS( " +
    "SELECT m_id FROM " + MyGlobal.activeDB + "." + sTableName + " WHERE m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' and m_Year = '" + iYearForDB + "' and m_Month = '" + iMonthForDB + "' " +
") LIMIT 1; ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("CreateRowTablesIfNotExists-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("CreateRowTablesIfNotExists-Exception-" + ex.Message);
            }
        }
        [HttpPost]
        public ActionResult ApplyLeave(string profile, string email, string year, string month,
            string staffid, string leavetype, string selecteddays,
            string leaveyear, string leavemonth, string selectedday, string mode,
            string leavereason,string admin)
        {
            //  year & month is to load the data for the selected month
            // leaveyear & leavemonth is the leave required
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadLeaveDataResponse = new LoadLeaveDataResponse();
            loadLeaveDataResponse.status = false;
            loadLeaveDataResponse.result = "";

            if(string.IsNullOrEmpty(admin)) admin = "false";

            string sErrMessage = "", sSQL = "", sUpdateRequest = "";
            double dblSelectedDays = MyGlobal.GetDouble(selecteddays);
            int iSelectedDay = MyGlobal.GetInt16(selectedday);
            if (iSelectedDay == 0)
            {
                sErrMessage = "Request date not selected";
                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
            }
            if (dblSelectedDays == 0)
            {
                sErrMessage = "No days selected";
                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------------
                    string sRosterPendingMessage = "";

                    if (!CheckLeaveAlreadyExists(profile, staffid, con, leaveyear, leavemonth, iSelectedDay, out sErrMessage))
                    {
                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                    }
                    GetSumOfDrCrFromLeave_and_leavesTable(con, ref loadLeaveDataResponse, profile, leaveyear, staffid);

                    //GetMonthRosterView(con, ref loadLeaveDataResponse, profile, staffid, year, month);
                    if (!admin.Equals("true"))
                    {
                        if (!CheckForLeaveConditions(profile, staffid, con, leaveyear, leavemonth, iSelectedDay, dblSelectedDays, leavetype, out sErrMessage, loadLeaveDataResponse))
                        {
                            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                        }
                    }
                    //bool bApplied = false;
                    //-----Check Roster table for issues
                    GetRosterWarningMessagesIfAny(profile, staffid, leavemonth, leaveyear, con, iSelectedDay, out sRosterPendingMessage);
                    
                    //-------------------Get Staff Details
                    string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                    //GetStaffDetails(con, profile, staffid, out sStaffEmail, out sReportEmail, out sErrMessage);
                    GetStaffDetails_FromStaffID(con, profile, staffid,
                        out sFName, out sStaffEmail, out sReportAdminEmail,
                        out sReportFuncEmail, out sErrMessage);
                    if (sErrMessage.Length > 0)
                    {
                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                    }
                    //-------------------Get SQL-----------------------------------------------
                    int iMonthForDB = (MyGlobal.GetInt16(leavemonth) - 1);
                    int iYearForDB = MyGlobal.GetInt16(leaveyear);
                    bool bContinueToNextMonth = false;
                    int iDaysExhausted = 0;

                    CreateRowTablesIfNotExists(profile, "tbl_leaves", staffid, iYearForDB, iMonthForDB);

                    sUpdateRequest = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                    bool bAdditionalRun = false;
                    for (int i = iSelectedDay; i < (iSelectedDay + dblSelectedDays); i++)
                    {
                        if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                        {
                            if (bAdditionalRun) sUpdateRequest += ",";
                            bAdditionalRun = true;
                            sUpdateRequest += "m_DayL" + i + "='" + leavetype + "',m_Status" + i + "='1' ";
                            iDaysExhausted++;
                        }
                        else
                        {
                            bContinueToNextMonth = true;
                            break;
                        }
                    }
                    sUpdateRequest += "where m_Profile = '" + profile + "' " +
                        "and m_Year='" + iYearForDB + "' and m_Month='" + iMonthForDB + "' " +
                        "and m_StaffID='" + staffid + "';";
                    //------------------------------------------------------------------
                    if (bContinueToNextMonth)
                    {

                        iMonthForDB++;
                        if (iMonthForDB > 11)
                        {
                            iMonthForDB = 0;
                            iYearForDB++;
                        }

                        CreateRowTablesIfNotExists(profile, "tbl_leaves", staffid, iYearForDB, iMonthForDB);

                        sUpdateRequest += "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                        bAdditionalRun = false;
                        for (int i = 1; i <= (dblSelectedDays- iDaysExhausted); i++)
                        {
                            if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                            {
                                if (bAdditionalRun) sUpdateRequest += ",";
                                bAdditionalRun = true;
                                sUpdateRequest += "m_DayL" + i + "='" + leavetype + "',m_Status" + i + "='1' ";

                            }
                            else
                            {
                                bContinueToNextMonth = true;
                            }
                        }
                        sUpdateRequest += "where m_Profile = '" + profile + "' " +
                            "and m_Year='" + iYearForDB + "' and m_Month='" + iMonthForDB + "' " +
                            "and m_StaffID='" + staffid + "';";
                    }
                    //------------------------------------------------------------------
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {

                        myCommand.CommandText = sUpdateRequest;
                        myCommand.ExecuteNonQuery();
                        //-------------------Create and Send System Message
                        string sFrom = "", sTo = "";
                        string session = Get_if_session_exists_for_the_same_day(
                            con, profile, staffid, leaveyear, (MyGlobal.GetInt16(leavemonth) - 1),
                            iSelectedDay, ref sFrom, ref sTo);
                        sSQL = "";
                        if (session.Length == 0)
                        {
                            session = staffid + "_" + leaveyear + "_" + leavemonth + "_" + iSelectedDay +
                            "_" + DateTime.Now.ToString("HHmmss");
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                                "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                                "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Param1,m_Param2,m_Param3) values " +
                                "('" + profile + "',2," +
                                "'" + sStaffEmail + "','','" + staffid + "'," +
                                "'" + sReportAdminEmail + "','',''," +
                                "'" + session + "',Now(),Now()," +
                                "'" + (iSelectedDay + "-" + MyGlobal.GetInt16(leavemonth) + "-" + leaveyear) + "','" + (dblSelectedDays) + "','" + leavetype + "');";
                        }
                        string message = "";
                        message += "<table class=''LveTbl''>";
                        if (admin.Equals("true"))
                        {
                            message += "<tr><td class=''LveTD3''>By ADMIN</td></tr>";
                        }
                        message += "<tr class=''LveTR1''><td>Leave Requested</td>";
                        message += "<td class=''LveTD3''>" + dblSelectedDays + "</td>";
                        message += "<td class=''LveTD1''>" + leavetype + "</td></tr>";
                        message += "<tr><td colspan=4 class=''LveTD2''>" + this.GetLeaveFromTo(iSelectedDay, leavemonth, leaveyear, dblSelectedDays) + "</td></tr>";
                        message += "<tr class=''LveTR2''><td colspan=4>" + sRosterPendingMessage + "</td></tr>";
                        message += "</table>";
                        message += leavereason;
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_LeaveType,m_LeaveStatus,m_Days) " +
                                "values ('" + profile + "','" + staffid + "','" + leaveyear + "','" + (MyGlobal.GetInt16(leavemonth) - 1) + "','" + iSelectedDay + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + leavetype + "','1','" + dblSelectedDays + "');";

                        sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                        "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                        "and m_Session='" + session + "';";

                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                        if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                        if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        //{
                        //  mySqlCommand.ExecuteNonQuery();
                        //}
                        myTrans.Commit();
                        sErrMessage = "<span style='color:darkgreen;'><b>Leave request sent for approval</b></span>";
                        HubObject hub = GetPendingMessagesObject(con, profile, "leaves", sReportAdminEmail);
                        SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);

                        //SendHubObject(sReportAdminEmail, GetPendingMessagesObject(con, profile, sReportAdminEmail));
                        //SendHubObject(sReportFuncEmail, GetPendingMessagesObject(con, profile, sReportFuncEmail));
                        loadLeaveDataResponse.status = true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sErrMessage = "Failed. (" + e.Message + ")";
                            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                        }
                        catch (MySqlException ex)
                        {
                            if (myTrans.Connection != null)
                            {
                                Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                            }
                        }
                        Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                        Console.WriteLine("Neither record was written to database.");
                    }
                    finally
                    {
                        //myConnection.Close();
                    }
                    //--------------------------------------

                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ApplyLeave-MySqlException-" + ex.Message);
                sErrMessage = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ApplyLeave-Exception-" + ex.Message);
                sErrMessage = ex.Message;
            }
            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
        }
        //Month is 1 indexed
        private string GetLeaveFromTo(int iSelectedDay, string month, string year, double dblSelectedDays)
        {
            if (dblSelectedDays < 2)
            {
                return iSelectedDay + "-" + month + "-" + year;
            }
            else
            {
                try { 
                DateTime startDt = new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month), iSelectedDay);
                DateTime endDt=startDt.AddDays(dblSelectedDays-1);
                return startDt.ToString("ddd, dd-MM-yyyy") + " to " + endDt.ToString("ddd, dd-MM-yyyy");
                }
                catch (ArgumentOutOfRangeException)
                {
                    return iSelectedDay + "-" + month + "-" + year;
                }
            }
        }
        [HttpPost]
        public ActionResult cancelrevoke(string profile, string email, string year, string month,
    string staffid, string leavetype,
    string leaveyear, string leavemonth, string selectedday, string mode)
        {
            var loadLeaveDataResponse = new LoadLeaveDataResponse();
            loadLeaveDataResponse.status = false;
            loadLeaveDataResponse.result = "";
            string sErrMessage = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----Check applied leave exists
                    int iDayCode = 0;
                    string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_leaves where m_Profile='" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                        "and m_StaffID='" + staffid + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    int ordinal = reader.GetOrdinal("m_DayL" + selectedday);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        int ordinalStatus = reader.GetOrdinal("m_Status" + selectedday);
                                        if (!reader.IsDBNull(ordinalStatus))
                                            iDayCode = reader.GetInt16(ordinalStatus);
                                    }
                                }
                            }
                            else
                            {
                                sErrMessage = "Invalid request";
                            }
                        }
                    }
                    if (iDayCode == 7 ) // Revoke Request Pending
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves " +
                            "Set m_Status" + selectedday + "=9 " +
                            "where " +
                            "m_Profile = '" + profile + "' " +
                            "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                            "and m_StaffID='" + staffid + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();

                            //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            //{
                            //  mySqlCommand.ExecuteNonQuery();

                            //}
                            //----------------Cancellation message
                            string sFrom = "", sTo = "";
                            string session = Get_if_session_exists_for_the_same_day(
                                con, profile, staffid, year, (MyGlobal.GetInt16(month) - 1),
                                MyGlobal.GetInt16(selectedday), ref sFrom, ref sTo);
                            //if (sFrom.Equals(email, StringComparison.CurrentCultureIgnoreCase)) sReportEmail = sTo;
                            //if (sTo.Equals(email, StringComparison.CurrentCultureIgnoreCase)) sReportEmail = sFrom;

                            string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                            GetStaffDetails_FromStaffID(con, profile, staffid,
                                out sFName, out sStaffEmail, out sReportAdminEmail,
                                out sReportFuncEmail, out sErrMessage);



                            string message = "<span style=''color:red;''><b>Revoke Request cancelled by self</b></span>";
                            sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session," +
                                "m_LeaveType,m_LeaveStatus,m_Days) " +
                                "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "'," +
                                "'" + selectedday + "','" + email + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + leavetype + "','0','" + "1" + "');";

                            sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                            "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                            "and m_Session='" + session + "';";

                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                            if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                            if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";

                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            //{
                            //  mySqlCommand.ExecuteNonQuery();
                            //}
                            myTrans.Commit();
                            sErrMessage = "Revoke request cancelled";
                            HubObject hub = GetPendingMessagesObject(con, profile, "leaves", sReportAdminEmail);
                            SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                myTrans.Rollback();
                                sErrMessage = "Failed. (" + e.Message + ")";
                                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                            }
                            catch (MySqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                                }
                            }
                            Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                            Console.WriteLine("Neither record was written to database.");
                        }
                        finally
                        {
                            //myConnection.Close();
                        }
                        //--------------------------------------

                    }
                    else
                    {
                        sErrMessage = "Can't cancel leave on this day";
                    }

                }
            }
            catch (MySqlException ex)
            {
                sErrMessage = ex.Message;
            }
            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
        }
        
        [HttpPost]
        public ActionResult CancelLeave(string profile, string email, string year, string month,
            string staffid, string leavetype,
            string leaveyear, string leavemonth, string selectedday, string mode)
        {
            var loadLeaveDataResponse = new LoadLeaveDataResponse();
            loadLeaveDataResponse.status = false;
            loadLeaveDataResponse.result = "";
            string sErrMessage = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----Check applied leave exists
                    int iDayCode = 0;
                    string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_leaves where m_Profile='" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                        "and m_StaffID='" + staffid + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    int ordinal = reader.GetOrdinal("m_DayL" + selectedday);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        int ordinalStatus = reader.GetOrdinal("m_Status" + selectedday);
                                        if (!reader.IsDBNull(ordinalStatus))
                                            iDayCode = reader.GetInt16(ordinalStatus);
                                    }
                                }
                            }
                            else
                            {
                                sErrMessage = "Invalid request";
                            }
                        }
                    }
                    if (iDayCode == 1 || iDayCode == 2) // Pending or Rejected
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves " +
                            "Set m_DayL" + selectedday + "=null,m_Status" + selectedday + "=null " +
                            "where " +
                            "m_Profile = '" + profile + "' " +
                            "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                            "and m_StaffID='" + staffid + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();

                            //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            //{
                            //  mySqlCommand.ExecuteNonQuery();
                            
                            //}
                            //----------------Cancellation message
                            string sFrom = "", sTo = "";
                            string session = Get_if_session_exists_for_the_same_day(
                                con, profile, staffid, year, (MyGlobal.GetInt16(month) - 1),
                                MyGlobal.GetInt16(selectedday), ref sFrom, ref sTo);
                            //if (sFrom.Equals(email, StringComparison.CurrentCultureIgnoreCase)) sReportEmail = sTo;
                            //if (sTo.Equals(email, StringComparison.CurrentCultureIgnoreCase)) sReportEmail = sFrom;

                            string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                            GetStaffDetails_FromStaffID(con, profile, staffid,
                                out sFName, out sStaffEmail, out sReportAdminEmail,
                                out sReportFuncEmail, out sErrMessage);



                            string message = "<span style=''color:red;''><b>Request cancelled by self</b></span>";
                            sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session," +
                                "m_LeaveType,m_LeaveStatus,m_Days) " +
                                "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "'," +
                                "'" + selectedday + "','" + email + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + leavetype + "','0','" + "1" + "');";

                            sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                            "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                            "and m_Session='" + session + "';";

                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                            if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                            if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";

                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            //{
                            //  mySqlCommand.ExecuteNonQuery();
                            //}
                            myTrans.Commit();
                            sErrMessage = "Leave request cancelled";
                            HubObject hub = GetPendingMessagesObject(con, profile, "leaves", sReportAdminEmail);
                            SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                myTrans.Rollback();
                                sErrMessage = "Failed. (" + e.Message + ")";
                                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                            }
                            catch (MySqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                                }
                            }
                            Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                            Console.WriteLine("Neither record was written to database.");
                        }
                        finally
                        {
                            //myConnection.Close();
                        }
                        //--------------------------------------

                    }
                    else
                    {
                        sErrMessage = "Can't cancel leave on this day";
                    }

                }
            }
            catch (MySqlException ex)
            {
                sErrMessage = ex.Message;
            }
            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
        }
        //-------------------------------------------------------------
        [HttpPost]
        public ActionResult RevokeLeave(string profile, string email, string year, string month,
            string staffid, string leavetype, string selecteddays,
            string leaveyear, string leavemonth, string selectedday, string mode,
            string leavereason)
        {
            //  year & month is to load the data for the selected month
            // leaveyear & leavemonth is the leave required
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadLeaveDataResponse = new LoadLeaveDataResponse();
            loadLeaveDataResponse.status = false;
            loadLeaveDataResponse.result = "";
            string sErrMessage = "", sSQL = "", sUpdateRequest = "";
            double dblSelectedDays = MyGlobal.GetDouble(selecteddays);
            int iSelectedDay = MyGlobal.GetInt16(selectedday);
            if (iSelectedDay == 0)
            {
                sErrMessage = "Request date not selected";
                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
            }
            if (dblSelectedDays == 0)
            {
                sErrMessage = "No days selected";
                return LoadLeaveData(profile, year, month, staffid, sErrMessage);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------------
                    string sRosterPendingMessage = "";
                    /**
                    if (!CheckLeaveAlreadyExists(profile, staffid, con, leaveyear, leavemonth, iSelectedDay, out sErrMessage))
                    {
                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                    }
                    GetSumOfDrCrFromLeave_and_leavesTable(con, ref loadLeaveDataResponse, profile, leaveyear, staffid);
                    
                    //GetMonthRosterView(con, ref loadLeaveDataResponse, profile, staffid, year, month);
                    if (!CheckForLeaveConditions(profile, staffid, con, leaveyear, leavemonth, iSelectedDay, dblSelectedDays, leavetype, out sErrMessage, loadLeaveDataResponse))
                    {
                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                    }
                    */
                    //bool bApplied = false;
                    //-----Check Roster table for issues
                    ///GetRosterWarningMessagesIfAny(profile, staffid, leavemonth, leaveyear, con, iSelectedDay, out sRosterPendingMessage);

                    //-------------------Get Staff Details
                    string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                    //GetStaffDetails(con, profile, staffid, out sStaffEmail, out sReportEmail, out sErrMessage);
                    GetStaffDetails_FromStaffID(con, profile, staffid,
                        out sFName, out sStaffEmail, out sReportAdminEmail,
                        out sReportFuncEmail, out sErrMessage);
                    if (sErrMessage.Length > 0)
                    {
                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                    }
                    //-------------------Get SQL-----------------------------------------------
                    int iMonthForDB = (MyGlobal.GetInt16(leavemonth) - 1);
                    int iYearForDB = MyGlobal.GetInt16(leaveyear);
                    bool bContinueToNextMonth = false;
                    int iDaysExhausted = 0;

                    ///CreateRowTablesIfNotExists(profile, "tbl_leaves", staffid, iYearForDB, iMonthForDB);

                    sUpdateRequest = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                    bool bAdditionalRun = false;
                    for (int i = iSelectedDay; i < (iSelectedDay + dblSelectedDays); i++)
                    {
                        if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                        {
                            if (bAdditionalRun) sUpdateRequest += ",";
                            bAdditionalRun = true;
                            //sUpdateRequest += "m_DayL" + i + "='" + leavetype + "',m_Status" + i + "='1' ";
                            sUpdateRequest += "m_Status" + i + "='7' ";
                            iDaysExhausted++;
                        }
                        else
                        {
                            bContinueToNextMonth = true;
                            break;
                        }
                    }
                    sUpdateRequest += "where m_Profile = '" + profile + "' " +
                        "and m_Year='" + iYearForDB + "' and m_Month='" + iMonthForDB + "' " +
                        "and m_StaffID='" + staffid + "';";
                    //------------------------------------------------------------------
                    if (bContinueToNextMonth)
                    {

                        iMonthForDB++;
                        if (iMonthForDB > 11)
                        {
                            iMonthForDB = 0;
                            iYearForDB++;
                        }

                        //CreateRowTablesIfNotExists(profile, "tbl_leaves", staffid, iYearForDB, iMonthForDB);

                        sUpdateRequest += "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                        bAdditionalRun = false;
                        for (int i = 1; i <= (dblSelectedDays - iDaysExhausted); i++)
                        {
                            if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                            {
                                if (bAdditionalRun) sUpdateRequest += ",";
                                bAdditionalRun = true;
                                //sUpdateRequest += "m_DayL" + i + "='" + leavetype + "',m_Status" + i + "='1' ";
                                sUpdateRequest += "m_Status" + i + "='7' ";
                            }
                            else
                            {
                                bContinueToNextMonth = true;
                            }
                        }
                        sUpdateRequest += "where m_Profile = '" + profile + "' " +
                            "and m_Year='" + iYearForDB + "' and m_Month='" + iMonthForDB + "' " +
                            "and m_StaffID='" + staffid + "';";
                    }
                    //------------------------------------------------------------------
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {

                        myCommand.CommandText = sUpdateRequest;
                        myCommand.ExecuteNonQuery();
                        /*
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sUpdateRequest, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            sErrMessage = "<span style='color:darkgreen;'><b>Leave request sent for approval</b></span>";
                            bApplied = true;
                        }
                        if (!bApplied)
                        {
                            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                        }
                        */
                        //-------------------Create and Send System Message
                        string sFrom = "", sTo = "";
                        string session = Get_if_session_exists_for_the_same_day(
                            con, profile, staffid, leaveyear, (MyGlobal.GetInt16(leavemonth) - 1),
                            iSelectedDay, ref sFrom, ref sTo);
                        sSQL = "";
                        if (session.Length == 0)
                        {
                            session = staffid + "_" + leaveyear + "_" + leavemonth + "_" + iSelectedDay +
                            "_" + DateTime.Now.ToString("HHmmss");
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                                "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                                "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Param1,m_Param2,m_Param3) values " +
                                "('" + profile + "',2," +
                                "'" + sStaffEmail + "','','" + staffid + "'," +
                                "'" + sReportAdminEmail + "','',''," +
                                "'" + session + "',Now(),Now()," +
                                "'" + (iSelectedDay + "-" + MyGlobal.GetInt16(leavemonth) + "-" + leaveyear) + "','" + (dblSelectedDays) + "','" + leavetype + "');";
                        }
                        string message = "";
                        message += "<table class=''LveTbl''>";
                        message += "<tr class=''LveTR1''><td>REVOKE Leave, Requested</td>";
                        message += "<td class=''LveTD3''>" + dblSelectedDays + "</td>";
                        message += "<td class=''LveTD1''>" + leavetype + "</td></tr>";
                        message += "<tr><td colspan=4 class=''LveTD2''>" + this.GetLeaveFromTo(iSelectedDay, leavemonth, leaveyear, dblSelectedDays) + "</td></tr>";
                        message += "<tr class=''LveTR2''><td colspan=4>" + sRosterPendingMessage + "</td></tr>";
                        message += "</table>";
                        message += leavereason;
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_LeaveType,m_LeaveStatus,m_Days) " +
                                "values ('" + profile + "','" + staffid + "','" + leaveyear + "','" + (MyGlobal.GetInt16(leavemonth) - 1) + "','" + iSelectedDay + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + leavetype + "','1','" + dblSelectedDays + "');";

                        sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                        "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                        "and m_Session='" + session + "';";

                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                        if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                        if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();

                        myTrans.Commit();
                        sErrMessage = "<span style='color:darkgreen;'><b>Revoke Leave request, sent for approval</b></span>";
                        HubObject hub = GetPendingMessagesObject(con, profile, "leaves", sReportAdminEmail);
                        SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);

                        //SendHubObject(sReportAdminEmail, GetPendingMessagesObject(con, profile, sReportAdminEmail));
                        //SendHubObject(sReportFuncEmail, GetPendingMessagesObject(con, profile, sReportFuncEmail));
                        loadLeaveDataResponse.status = true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sErrMessage = "Failed. (" + e.Message + ")";
                            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                        }
                        catch (MySqlException ex)
                        {
                            if (myTrans.Connection != null)
                            {
                                Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                            }
                        }
                        Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                        Console.WriteLine("Neither record was written to database.");
                    }
                    finally
                    {
                        //myConnection.Close();
                    }
                    //--------------------------------------

                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ApplyLeave-MySqlException-" + ex.Message);
                sErrMessage = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ApplyLeave-Exception-" + ex.Message);
                sErrMessage = ex.Message;
            }
            return LoadLeaveData(profile, year, month, staffid, sErrMessage);
        }

        //---------------------------------------------------------
        private string GetLeaveSession(string profile, string staffid, int iYear, int iMonth, string da, string key)
        {
            string sStartTime = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string keyhalf = "/" + key;
                    string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_messages " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' and m_Day='" + da + "' " +
                        "and (m_LeaveType='" + key + "' or m_LeaveType='" + keyhalf + "') and (m_LeaveStatus = 7 or m_LeaveStatus = 9) " +
                        "and m_StaffID is not null and m_Year is not null and m_Month is not null " +
                        "order by m_Time desc limit 1";
                    
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(1)) sStartTime = reader.GetString(1);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {

            }
            return sStartTime;
        }

        [HttpPost]
        public ActionResult GetLeaveHistory(string profile, string staffid, string year, string key)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var getLeaveHistoryResponse = new GetLeaveHistoryResponse();
            getLeaveHistoryResponse.status = false;
            getLeaveHistoryResponse.result = "";
            //var loadLeaveDataResponse = new LoadLeaveDataResponse();
            string sErrMessage = "", sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------Get leave Credits
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_leave where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' ";
                    if (key.Equals("CL"))
                    {
                        sSQL += "and (m_type='CL' or m_Type='/CL' or m_Type='CL/') ";
                    }
                    else if (key.Equals("SL"))
                    {
                        sSQL += "and (m_type='SL' or m_Type='/SL' or m_Type='SL/') ";
                    }
                    else if (key.Equals("LOP"))
                    {
                        sSQL += "and (m_type='LOP' or m_Type='/LOP' or m_Type='LOP/') ";
                    }
                    else if (key.Equals("ALOP"))
                    {
                        sSQL += "and (m_type='ALOP' or m_Type='/ALOP' or m_Type='ALOP/') ";
                    }
                    else
                    {
                        sSQL += "and m_type='" + key + "' ";
                    }
                    sSQL += "order by m_Time;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int iOrdYear = reader.GetOrdinal("m_Year");
                                    int iOrdTime = reader.GetOrdinal("m_Time");
                                    int iOrdDec = reader.GetOrdinal("m_Description");
                                    int iOrdCr = reader.GetOrdinal("m_Cr");
                                    int iOrdDr = reader.GetOrdinal("m_Dr");
                                    if (!reader.IsDBNull(iOrdYear) && !reader.IsDBNull(iOrdTime))
                                    {
                                        LeaveHistoryRow leaveHistoryRow = new LeaveHistoryRow();
                                        leaveHistoryRow.time = reader.GetDateTime(iOrdTime).ToString("yyyy-MM-dd hh:mm:ss");
                                        leaveHistoryRow.dt = "";// leaveHistoryRow.time;
                                        leaveHistoryRow.status = 0;
                                        leaveHistoryRow.description = "";
                                        if (!reader.IsDBNull(iOrdDec)) leaveHistoryRow.description = reader.GetString(iOrdDec);
                                        leaveHistoryRow.pending = 0;
                                        leaveHistoryRow.used = 0;
                                        leaveHistoryRow.credit = 0;
                                        if (!reader.IsDBNull(iOrdCr)) leaveHistoryRow.credit = reader.GetDouble(iOrdCr);
                                        if (!reader.IsDBNull(iOrdDr)) leaveHistoryRow.used = reader.GetDouble(iOrdDr);
                                        getLeaveHistoryResponse.rows.Add(leaveHistoryRow);
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------Get leave activties
                    sSQL = "SELECT * ";
                    sSQL += "FROM " + MyGlobal.activeDB + ".tbl_leaves leav where m_Profile = '" + profile + "' " +
                        "and m_Year='" + year + "' and m_StaffID='" + staffid + "' and (1=2 ";
                    if (key.IndexOf("CL")>-1 || key.IndexOf("SL") > -1 || key.IndexOf("LOP") > -1 || key.IndexOf("ALOP") > -1)
                    {
                        for (int i = 1; i <= 31; i++)
                        {
                            sSQL += "or m_DayL" + i + "='" + key + "' ";
                            sSQL += "or m_DayL" + i + "='/" + key + "' ";
                            sSQL += "or m_DayL" + i + "='" + key + "/' ";
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= 31; i++)
                        {
                            sSQL += "or m_DayL" + i + "='" + key + "' ";
                        }
                    }
                    sSQL += ")";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    for (int i = 1; i <= 31; i++)
                                    {
                                        int ordinal = reader.GetOrdinal("m_DayL" + i);
                                        if (!reader.IsDBNull(ordinal))
                                        {
                                            string leave = reader.GetString(ordinal);


                                            //if (leave.IndexOf(key)>-1)
                                            if(IsThisLeaveType(leave,key))
                                            {
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Year")) &&
                                                    !reader.IsDBNull(reader.GetOrdinal("m_Month")))
                                                {
                                                    LeaveHistoryRow leaveHistoryRow = new LeaveHistoryRow();
                                                    int iYear = reader.GetInt16(reader.GetOrdinal("m_Year"));
                                                    int iMonth = reader.GetInt16(reader.GetOrdinal("m_Month")) + 1;
                                                    string mnth = iMonth + "", da = i + "";
                                                    if (mnth.Length == 1) mnth = "0" + mnth;
                                                    if (da.Length == 1) da = "0" + da;
                                                    leaveHistoryRow.time = GetLeaveSession(profile, staffid, iYear, iMonth - 1, da, key);
                                                    leaveHistoryRow.dt = iYear + "-" + mnth + "-" + da;
                                                    leaveHistoryRow.status = reader.GetInt16(reader.GetOrdinal("m_Status" + i));
                                                    if (leaveHistoryRow.status == 1)
                                                    {
                                                        if (leave.IndexOf('/') > -1)
                                                        {
                                                            leaveHistoryRow.pending = 0.5;
                                                        }
                                                        else
                                                        {
                                                            leaveHistoryRow.pending = 1;
                                                        }
                                                        leaveHistoryRow.description = "Approval Pending";
                                                        getLeaveHistoryResponse.rows.Add(leaveHistoryRow);
                                                    }
                                                    if ((leaveHistoryRow.status == C_REVOKE_PENDING) ||
                                                        (leaveHistoryRow.status == C_APPROVED))
                                                    {
                                                        if (leave.IndexOf('/') > -1)
                                                        {
                                                            leaveHistoryRow.used = 0.5;
                                                        }
                                                        else
                                                        {
                                                            leaveHistoryRow.used = 1;
                                                        }
                                                        leaveHistoryRow.description = "Approved Leave";
                                                        getLeaveHistoryResponse.rows.Add(leaveHistoryRow);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                sErrMessage = ex.Message;
            }
            return Json(getLeaveHistoryResponse, JsonRequestBehavior.AllowGet);
        }
        //--------------------------------------------------
        [HttpPost]
        public ActionResult Update_LeaveAccounts(string mode,string profile, string email,
            string description, string days, string type, string staffid)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            double dblDays = MyGlobal.GetDouble(days);
            if (type.Length == 0)
            {
                postResponse.result = "Leave type if empty";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            if (staffid.Length == 0)
            {
                postResponse.result = "StaffID is empty";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            if (dblDays == 0)
            {
                postResponse.result = "Days is empty";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            if (description.Length == 0)
            {
                postResponse.result = "Description can't be empty";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }
            DateTime tme = DateTime.Today;
            int iYear = tme.Year;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------Get current Dr & Cr
                    double dblCr = 0, dblDr = 0;
                    string sSQL = "SELECT sum(m_Cr) as cr,sum(m_Dr) as dr FROM " + MyGlobal.activeDB + ".tbl_leave " +
                    "where m_Year = '" + iYear + "' and m_StaffID = '" + staffid + "' " +
                    "and m_Type = '" + type + "' and m_Profile = '" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) dblCr = reader.GetDouble(0);
                                    if (!reader.IsDBNull(1)) dblDr = reader.GetDouble(1);
                                }
                            }
                        }
                    }
                    //-----------------------
                    if (mode.Equals("debit")) {
                        if (0 < (dblDays - dblCr))
                        {
                            postResponse.result = "No sufficient leaves to deduct";
                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //------------------------
                    sSQL = "";
                    if (mode.Equals("credit"))
                    {
                        sSQL = "Insert into  " + MyGlobal.activeDB + ".tbl_leave " +
                            "(m_Profile,m_StaffID,m_Year,m_Type,m_Cr,m_Dr,m_Time," +
                            "m_Description) values " +
                            "('" + profile + "','" + staffid + "'," +
                            "'" + iYear + "','" + type + "','" + days + "'," +
                            "'" + "0" + "',Now(),'" + description + "')";
                    }
                    else if (mode.Equals("debit"))
                    {
                        sSQL = "Insert into  " + MyGlobal.activeDB + ".tbl_leave " +
    "(m_Profile,m_StaffID,m_Year,m_Type,m_Cr,m_Dr,m_Time," +
    "m_Description) values " +
    "('" + profile + "','" + staffid + "'," +
    "'" + iYear + "','" + type + "','" + "0" + "'," +
    "'" + days + "',Now(),'" + description + "')";
                    }
                    else
                    {
                        postResponse.result = "Unknown command";
                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                        postResponse.result = "Updated";
                        postResponse.status = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
    }
}