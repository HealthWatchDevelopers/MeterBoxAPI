using Dapper;
using iText.Layout.Borders;
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
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public partial class AccessmanagerController : Controller
    {
        const int C_NONE = 0;
        const int C_PENDING = 1;
        const int C_REJECTED = 2;
        const int C_ACCEPTED = 4;
        const int C_ReqAPPROVAL = 5;
        const int C_REVOKE_PENDING = 7;
        const int C_APPROVED = 9;

        static string[] sarLeaveCodes = { "CL", "/CL", "CL/", "SL", "/SL", "SL/", "PL", "APL", "LOP", "/LOP", "LOP/", "ALOP", "/ALOP", "ALOP/", "MatL", "PatL" };

        const int const_ShiftPaddingPre = 3600;//14400;  // 1 Hour In seconds
        const int const_ShiftPaddingPost = 7200;  // 2 Hours In seconds
        //32400 36000
        const long const_lShiftDuration = 28800;  // 8 Hours In seconds
        //const long const_lShiftDuration = 600;  // 8 Hours In seconds
        /*
         * Return format
         * client_m_id^
         */
        [HttpPost]
        public ActionResult Index(string profile, string data, string syncid, string staffid)
        {
            var accessManagerResponse = new AccessManagerResponse();
            accessManagerResponse.status = false;
            accessManagerResponse.result = "";
            accessManagerResponse.data = "";
            accessManagerResponse.bSyncDataValid = false;

            if (MyGlobal.syncid.Length == 0)
            {
                MyGlobal.syncid = MyGlobal.GetRandomNo(1111, 9999);
            }
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            String sOutput = "";
            char[] delimiterChars = { '|' };
            string[] arData = data.Split(delimiterChars);
            int iPackets = arData.Length;
            List<string> listMailsToSend = new List<string>();
            String sSQL = "", sSQLTerminal = "", sSQLTerminalInsert = "", sSQLForMessages = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "", sErrMessage = "";
                    string sStaffID = "", sFirstReceivedActivityTime = "", sLastReceivedActivityTime = "";
                    for (int i = 0; i < iPackets; i++)
                    {
                        char[] delimiterChars_sub = { '^' };
                        string[] arData_sub = arData[i].Split(delimiterChars_sub);
                        if (arData_sub.Length > 10)
                        {
                            string sLat = "0", sLng = "0";
                            if (MyGlobal.GetDouble(arData_sub[10]) > 0) sLat = arData_sub[10];
                            if (MyGlobal.GetDouble(arData_sub[11]) > 0) sLng = arData_sub[11];
                            if ((MyGlobal.GetInt64(arData_sub[6]) > 0) && // As safety
                                (MyGlobal.GetInt64(arData_sub[7]) < 28800))
                            {
                                sSQL += @"INSERT INTO " + MyGlobal.activeDB + ".tbl_accessmanager_activity (" +
                                    "m_HardwareID," +
                                    "m_IP," +
                                    "m_Staff," +
                                    "m_StaffID," +
                                    "m_Activity," +
                                    "m_ActivityTime," +
                                    "m_WorkTime," +
                                    "m_id_client," +
                                    "m_ReasonHead," +
                                    "m_ReasonNote," +
                                    "m_Lat," +
                                    "m_Lng," +
                                    "m_Profile) values (";
                                sSQL += "'" + arData_sub[1] + "',"; // m_HardwareID
                                sSQL += "'" + arData_sub[2] + "',"; // m_IP
                                sSQL += "'" + arData_sub[3] + "',"; // m_Staff
                                sSQL += "'" + arData_sub[4] + "',"; // m_StaffID
                                sSQL += "'" + arData_sub[5] + "',"; // m_Activity
                                sSQL += "'" + arData_sub[6] + "',"; // m_ActivityTime
                                sSQL += "'" + arData_sub[7] + "',"; // m_WorkTime
                                sSQL += "'" + arData_sub[0] + "',"; // m_id_client,
                                sSQL += "'" + arData_sub[8] + "',"; // m_ReasonHead
                                sSQL += "'" + MyGlobal.Base64Encode(arData_sub[9]) + "',"; // m_ReasonNote
                                sSQL += "'" + sLat + "',"; // m_Lat
                                sSQL += "'" + sLng + "',"; // m_Lng
                                sSQL += "'" + profile + "'"; // profile
                                sSQL += ");";
                            }
                            else
                            {
                                MyGlobal.Error("Activity Err-" +
                                    "m_StaffID-" + arData_sub[4] + ", " +
                                    "m_ActivityTime=" + arData_sub[6] + ", " +
                                    "m_WorkTime=" + arData_sub[7] + ", "
                                    );
                            }
                            //--------------------------
                            sStaffID = arData_sub[4];
                            if (sFirstReceivedActivityTime.Length == 0) sFirstReceivedActivityTime = arData_sub[6];
                            sLastReceivedActivityTime = arData_sub[6];
                            //--------------------------
                            sOutput += arData_sub[0] + "^";
                            //---Use the last data to update the terminals.
                            sSQLTerminal = "update " + MyGlobal.activeDB + ".tbl_terminals Set " +
                                "m_ActivityStart='" + arData_sub[6] + "' " +
                                "where m_HardwareID='" + arData_sub[1] + "' and " +
                                "(m_Activity!='" + arData_sub[5] + "' or m_Activity='update') and " +
                                "m_Profile='" + profile + "';";

                            sSQLTerminal += "update " + MyGlobal.activeDB + ".tbl_terminals Set " +
                                "m_Staff='" + arData_sub[3] + "'," +
                                "m_StaffID='" + arData_sub[4] + "'," +
                                "m_Activity='" + arData_sub[5] + "'," +
                                "m_ActivityTime='" + arData_sub[6] + "'," +
                                "m_ReasonHead='" + arData_sub[8] + "'," +
                                "m_ReasonNote='" + MyGlobal.Base64Encode(arData_sub[9]) + "'," +
                                "m_Lat='" + arData_sub[10] + "'," +
                                "m_Lng='" + arData_sub[11] + "'," +
                                "m_IP='" + arData_sub[2] + "' " +
                                "where m_HardwareID='" + arData_sub[1] + "' and " +
                                "m_Profile='" + profile + "';";
                            //---------------------------------Keep handy, if needed
                            sSQLTerminalInsert = "insert into " + MyGlobal.activeDB + ".tbl_terminals " +
                                "(m_HardwareID,m_Staff,m_StaffID,m_Activity,m_ActivityTime," +
                                "m_ReasonHead,m_ReasonNote,m_Lat,m_Lng,m_IP,m_Profile) values " +
                                "('" + arData_sub[1] + "','" + arData_sub[3] + "','" + arData_sub[4] + "'," +
                                "'" + arData_sub[5] + "','" + arData_sub[6] + "','" + arData_sub[8] + "','" + MyGlobal.Base64Encode(arData_sub[9]) + "'," +
                                "'" + arData_sub[10] + "','" + arData_sub[11] + "','" + arData_sub[2] + "'," +
                                "'" + profile + "');";
                            //----------------------------------------------------------------------------
                            if (arData_sub[5].Equals("lock"))
                            {
                                staffid = arData_sub[4];
                                if (sStaffEmail.Length == 0)
                                {
                                    GetStaffDetails_FromStaffID(
                                        con, profile, staffid,
                                        out sFName, out sStaffEmail,
                                        out sReportAdminEmail, out sReportFuncEmail, out sErrMessage);
                                }
                                sSQLForMessages += GetMessageSQL_lock(
                                    con,
                                    profile,
                                    arData_sub[1],  // Hardware
                                    arData_sub[4],  // StaffID
                                    arData_sub[5],  // Activity
                                    arData_sub[6],  // ActivityTime
                                    "0",    //arData_sub[7],  // Worktime Just locked
                                    arData_sub[8],  // m_ReasonHead
                                    MyGlobal.Base64Encode(arData_sub[9]),  // m_ReasonNote
                                    sFName,
                                    sStaffEmail,
                                    sReportAdminEmail,
                                    sReportFuncEmail
                                    );
                                if (sStaffEmail.Length > 5) if (!listMailsToSend.Contains(sStaffEmail)) listMailsToSend.Add(sStaffEmail);
                                if (sReportAdminEmail.Length > 5) if (!listMailsToSend.Contains(sReportAdminEmail)) listMailsToSend.Add(sReportAdminEmail);
                                if (sReportFuncEmail.Length > 5) if (!listMailsToSend.Contains(sReportFuncEmail)) listMailsToSend.Add(sReportFuncEmail);
                            }
                            else if (arData_sub[5].Equals("open"))
                            {
                                staffid = arData_sub[4];
                                if (sStaffEmail.Length == 0)
                                {
                                    GetStaffDetails_FromStaffID(
                                        con, profile, staffid,
                                        out sFName, out sStaffEmail,
                                        out sReportAdminEmail, out sReportFuncEmail, out sErrMessage);
                                }
                                sSQLForMessages += GetMessageSQL_open(
                                    con,
                                    profile,
                                    arData_sub[1],  // Hardware
                                    arData_sub[4],  // StaffID
                                    arData_sub[5],  // Activity
                                    arData_sub[6],  // ActivityTime
                                    "0",    //arData_sub[7],  // Worktime Just locked
                                    arData_sub[8],  // m_ReasonHead
                                    MyGlobal.Base64Encode(arData_sub[9]),  // m_ReasonNote
                                    sFName,
                                    sStaffEmail,
                                    sReportAdminEmail,
                                    sReportFuncEmail
                                    );
                                if (sStaffEmail.Length > 5) if (!listMailsToSend.Contains(sStaffEmail)) listMailsToSend.Add(sStaffEmail);
                                if (sReportAdminEmail.Length > 5) if (!listMailsToSend.Contains(sReportAdminEmail)) listMailsToSend.Add(sReportAdminEmail);
                                if (sReportFuncEmail.Length > 5) if (!listMailsToSend.Contains(sReportFuncEmail)) listMailsToSend.Add(sReportFuncEmail);
                            }
                        }
                    }
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            accessManagerResponse.data = sOutput;
                            accessManagerResponse.status = true;
                        }
                    }
                    //---------------------------Update Terminal table
                    if (sSQLTerminal.Length > 0)
                    {
                        int iRowsAffected = 0;
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLTerminal, con))
                        {
                            iRowsAffected = mySqlCommand.ExecuteNonQuery();
                        }
                        if (iRowsAffected == 0)// New, please insert new record
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLTerminalInsert, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    if (sSQLForMessages.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLForMessages, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            //________________________________Since execution for messages passed, signalR them
                            SendHubObjectsFromList(listMailsToSend);
                        }
                    }
                    //***********************************************************
                    //--------------Get Active roster data and update the tbl_attendance table with the info
                    string sRoster = "", sShift = "", staffName = "", sRosterMarker = "";
                    long shiftStart = 0, shiftEnd = 0;
                    long lFirstReceivedActivityTime = MyGlobal.GetInt64(sFirstReceivedActivityTime) + 19800;
                    long lLastActivityTimeReceived = MyGlobal.GetInt32(sLastReceivedActivityTime) + 19800;
                    if (lLastActivityTimeReceived == 0) lLastActivityTimeReceived = lFirstReceivedActivityTime;

                    DateTime swipeTime = MyGlobal.ToDateTimeFromEpoch(lFirstReceivedActivityTime);
                    long unixDate = MyGlobal.ToEpochTime(swipeTime.Date);
                    bool bInTimeSpan = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters " +
                    "where m_Profile = '" + profile + "' and m_StaffID='" + sStaffID + "' " +
                    "and m_Year='" + swipeTime.Year + "' and m_Month='" + (swipeTime.Month - 1) + "';";
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
                                        int iOrdinalDay0 = reader.GetOrdinal("m_Day1") - 1;
                                        sRoster = reader.IsDBNull(reader.GetOrdinal("m_RosterName")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterName"));
                                        sShift = reader.IsDBNull(reader.GetOrdinal("m_ShiftName")) ? "" : reader.GetString(reader.GetOrdinal("m_ShiftName"));
                                        staffName = reader.IsDBNull(reader.GetOrdinal("m_StaffName")) ? "" : reader.GetString(reader.GetOrdinal("m_StaffName"));
                                        shiftStart = reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftStartTime"));
                                        shiftEnd = reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftEndTime"));
                                        sRosterMarker = reader.IsDBNull(iOrdinalDay0 + swipeTime.Day) ? "" : reader.GetString(iOrdinalDay0 + swipeTime.Day);
                                        if (sRosterMarker.Length > 0)
                                        {
                                            if ((lFirstReceivedActivityTime > ((unixDate + shiftStart) - const_ShiftPaddingPre)) &&
                                                (lFirstReceivedActivityTime < ((unixDate + shiftEnd) + const_ShiftPaddingPost)))
                                            {
                                                // Well within the current day shift
                                                bInTimeSpan = true;
                                                break; // moved from live below ...anita 10th Aug2019
                                            }
                                            //break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!bInTimeSpan)
                    {   // Must be within previous day shift continuation to today....
                        swipeTime = swipeTime.AddDays(-1);
                        unixDate = MyGlobal.ToEpochTime(swipeTime.Date);
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile = '" + profile + "' and m_StaffID='" + sStaffID + "' " +
                        "and m_Year='" + swipeTime.Year + "' and m_Month='" + (swipeTime.Month - 1) + "';";
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
                                            int iOrdinalDay0 = reader.GetOrdinal("m_Day1") - 1;
                                            sRoster = reader.IsDBNull(reader.GetOrdinal("m_RosterName")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterName"));
                                            sShift = reader.IsDBNull(reader.GetOrdinal("m_ShiftName")) ? "" : reader.GetString(reader.GetOrdinal("m_ShiftName"));
                                            staffName = reader.IsDBNull(reader.GetOrdinal("m_StaffName")) ? "" : reader.GetString(reader.GetOrdinal("m_StaffName"));
                                            shiftStart = reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftStartTime"));
                                            shiftEnd = reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftEndTime"));
                                            sRosterMarker = reader.IsDBNull(iOrdinalDay0 + swipeTime.Day) ? "" : reader.GetString(iOrdinalDay0 + swipeTime.Day);
                                            if (sRosterMarker.Length > 0)
                                            {
                                                if ((lFirstReceivedActivityTime > ((unixDate + shiftStart) - const_ShiftPaddingPre)) &&
                                                (lFirstReceivedActivityTime < ((unixDate + shiftEnd) + const_ShiftPaddingPost)))
                                                {
                                                    // Well within the current day shift. But, started on previous day
                                                    bInTimeSpan = true;
                                                    bInTimeSpan = true;
                                                    break; // moved from live below ...anita 10th Aug2019
                                                }
                                                //break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------
                    if (bInTimeSpan && sRoster.Length > 0)
                    {
                        if (staffName.Length == 0)
                        {   // If Staff name not in ROster, get it
                            sSQL = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + sStaffID + "' limit 1";
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
                                                staffName = reader.GetString(0).ToLower();
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //--------------If new entry to attendance table, insert new... or update
                        bool bRecordExists = false;
                        long lActualStart = 0;
                        long lWorkhours = 0;

                        sSQL = "select m_ActualStart from " + MyGlobal.activeDB + ".tbl_attendance " +
                        "where m_Profile = '" + profile + "' and m_StaffID='" + sStaffID + "' " +
                        "and m_Year='" + swipeTime.Year + "' and m_Month='" + (swipeTime.Month - 1) + "' " +
                        "and m_Date='" + unixDate + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bRecordExists = true;
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            lActualStart = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                            if (lActualStart > 0)
                                            {
                                                lWorkhours = lLastActivityTimeReceived - lActualStart;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //-----------------------------------------------------------
                        if (!bRecordExists)
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_attendance " +
                                "(m_StaffID,m_Year,m_Month,m_Date,m_RosterName,m_ShiftName,m_ShiftStart,m_ShiftEnd, " +
                                "m_ActualStart,m_ActualEnd,lWorkhours,m_Profile," +
                                "m_MarkRoster,m_MarkLeave,m_RosterOptions,m_AsOn,m_Mode)  " +
                                "values  " +
                                "('" + sStaffID + "','" + swipeTime.Year + "','" + (swipeTime.Month - 1) + "','" + unixDate + "'," +
                                "'" + sRoster + "','" + sShift + "','" + (unixDate + shiftStart) + "','" + (unixDate + shiftEnd) + "'," +
                                "'" + lFirstReceivedActivityTime + "','" + lLastActivityTimeReceived + "','" + lWorkhours + "','" + profile + "','" + sRosterMarker + "'," +
                                "'" + "" + "','" + "" + "','" + lLastActivityTimeReceived + "','1');";
                        }
                        else
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_attendance Set ";
                            if (lActualStart == 0)
                            {
                                sSQL += "m_ActualStart='" + lLastActivityTimeReceived + "',";
                            }
                            sSQL += "m_ActualEnd='" + lLastActivityTimeReceived + "',lWorkhours='" + lWorkhours + "'," +
                                "m_AsOn='" + lLastActivityTimeReceived + "',m_Mode='1' " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + sStaffID + "' " +
                                "and m_RosterName='" + sRoster + "' and m_ShiftName='" + sShift + "' " +
                                "and m_Year='" + swipeTime.Year + "' and m_Month='" + (swipeTime.Month - 1) + "' " +
                                "and m_Date='" + unixDate + "';";
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    }// if valid roster, do the above
                    //***********************************************************
                    //______________Get accumilated worktime and update " + MyGlobal.activeDB + ".tbl_rosters table
                    if (!syncid.Equals(MyGlobal.syncid)) // Device requesting fresh settings
                    {
                        accessManagerResponse.breaks = GetBreakJustifications(con, profile);
                        accessManagerResponse.syncid = MyGlobal.syncid;
                        //----------------------------------------------Get Lock Time
                        sSQL = "select teams.m_LockTime from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                            "left join " + MyGlobal.activeDB + ".tbl_misc_teams teams on teams.m_Profile = staff.m_Profile and teams.m_Name = staff.m_Team " +
                            "where staff.m_Profile = '" + profile + "' and staff.m_StaffID='" + staffid + "'";
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
                                            accessManagerResponse.locktime = reader.GetInt16(0);
                                        }
                                    }
                                }
                            }
                        }
                        //----------------Get Active shift details
                        //string 
                        sRoster = "";
                        sShift = "";
                        long lShiftStartUnix = 0, lShiftEndUnix = 0;
                        int iYearActive = 0, iMonthActive = 0, iDayActive = 0;
                        if (GetActiveShiftDetails(
                            con, profile, staffid,
                            out sRoster, out sShift,
                            out lShiftStartUnix, out lShiftEndUnix,
                            out iYearActive, out iMonthActive, out iDayActive
                             ))
                        {
                            accessManagerResponse.activeShift.StartDate =
                                string.Format("{0:D2}/{1:D2}/{2:D4}",
                                iDayActive, iMonthActive + 1, iYearActive);
                            accessManagerResponse.activeShift.RosterName = sRoster;
                            accessManagerResponse.activeShift.ShiftName = sShift;
                            accessManagerResponse.activeShift.lShiftStartUnix = lShiftStartUnix;
                            accessManagerResponse.activeShift.lShiftEndUnix = lShiftEndUnix;
                            long lWorkhours = 0;
                            GetStaffWorkHours(profile,
                                (lShiftStartUnix), //const_ShiftPaddingPre
                                (lShiftEndUnix), //const_ShiftPaddingPost
                                staffid,
                                 out lWorkhours);
                            accessManagerResponse.activeShift.worktime = MyGlobal.ToDateTimeFromEpoch(lWorkhours).ToString("HH:mm:ss") + " Hrs";
                            accessManagerResponse.activeShift.lWorktime = lWorkhours;
                        }

                        //if (accessManagerResponse.activeShift.RosterName.Length == 0)
                        //{   // No roster available for this day. Check for any Leave
                        DateTime tme = DateTime.Now;
                        int iYear = tme.Year;
                        int iMonth = tme.Month - 1;
                        int iDay = tme.Day;
                        sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_leaves " +
                        "where m_Profile = '" + profile + "' and m_Year = '" + iYear + "' " +
                        "and m_Month = '" + iMonth + "' " +
                        "and m_StaffID='" + staffid + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        int ordinal = reader.GetOrdinal("m_Status" + iDay);
                                        if (!reader.IsDBNull(ordinal))
                                        {
                                            if ((reader.GetInt16(ordinal) == C_APPROVED) ||
                                                (reader.GetInt16(ordinal) == C_REVOKE_PENDING))
                                            {
                                                ordinal = reader.GetOrdinal("m_DayL" + iDay);
                                                if (!reader.IsDBNull(ordinal))
                                                {
                                                    if (reader.GetString(ordinal).Length > 0)
                                                    {
                                                        accessManagerResponse.activeShift.Remark = "Confirmed " + reader.GetString(ordinal) + " today";
                                                        if (!reader.GetString(ordinal).Equals(MyGlobal.WORKDAY_MARKER))
                                                        {
                                                            if (reader.GetString(ordinal).Length == 0)
                                                            {
                                                                accessManagerResponse.activeShift.worktime = "No Shift";
                                                            }
                                                            else
                                                            {
                                                                accessManagerResponse.activeShift.worktime = reader.GetString(ordinal);
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
                        //}
                        //------------------------------------
                        accessManagerResponse.ServerTime =
                             (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        accessManagerResponse.status = true;
                        accessManagerResponse.bSyncDataValid = true;
                    }
                    con.Close();
                }
            }
            catch (MySqlException ex1)
            {
                accessManagerResponse.result = ex1.Message;
                MyGlobal.Error("Err1->" + ex1.Message);
                MyGlobal.Error("Err1-> sSQL=" + sSQL + ", " +
                    "sSQLTerminal=" + sSQLTerminal + ", " +
                    "sSQLTerminalInsert=" + sSQLTerminalInsert + ", " +
                    "sSQLForMessages=" + sSQLForMessages + ", ");

                //accessManagerResponse.data = sOutput;
                //accessManagerResponse.status = true; // Ensure, errro date gets cleared
                sOutput = "";
            }
            return Json(accessManagerResponse, JsonRequestBehavior.AllowGet);
        }
        // const_ShiftPadding isconsidered internally
        private bool GetActiveShiftDetails(
            MySqlConnection con, string profile, string staffid,
            out string sRoster, out string sShift,
            out long lShiftStartUnix, out long lShiftEndUnix,
            out int iYearActive, out int iMonthActive, out int iDayActive
             )
        {
            iYearActive = 0;
            iMonthActive = 0;
            iDayActive = 0;
            sRoster = "";
            sShift = "";
            lShiftStartUnix = 0;
            lShiftEndUnix = 0;
            Int32 unixTimestampDayStart = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Int32 unixTimestampDayNow = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            DateTime tme = DateTime.Now;
            int iYear = tme.Year;
            int iMonth = tme.Month - 1;
            int iDay = tme.Day;
            /*
                 --______________--
             ------------0-----------12-------------24----------------
             */
            if (GetActiveShiftDetails_Sub(
                con, profile, staffid,
                iYear, iMonth, iDay,
                unixTimestampDayStart, unixTimestampDayNow,
                out sRoster, out sShift,
                out lShiftStartUnix, out lShiftEndUnix,
                out iYearActive, out iMonthActive, out iDayActive))
            {
                return true;
            }
            else
            {
                //-----Failed to get active shift on the day
                //So, shift must be starting on the previous day
                tme = DateTime.Now.AddDays(-1);
                iYear = tme.Year;
                iMonth = tme.Month - 1;
                iDay = tme.Day;
                unixTimestampDayStart -= 86400;
                if (GetActiveShiftDetails_Sub(
                    con, profile, staffid,
                    iYear, iMonth, iDay,
                    unixTimestampDayStart, unixTimestampDayNow,
                    out sRoster, out sShift,
                    out lShiftStartUnix, out lShiftEndUnix,
                    out iYearActive, out iMonthActive, out iDayActive))
                {
                    return true;
                }
            }
            return false;
        }
        private bool GetActiveShiftDetails_Sub(MySqlConnection con, string profile,
            string staffid, int iYear, int iMonth, int iDay,
            long unixTimestampDayStart, long unixTimestampDayNow,
            out string sRoster, out string sShift,
            out long lShiftStartUnix, out long lShiftEndUnix,
            out int iYearActive, out int iMonthActive, out int iDayActive
            )
        {
            iYearActive = 0;
            iMonthActive = 0;
            iDayActive = 0;
            sRoster = "";
            sShift = "";
            lShiftStartUnix = 0;
            lShiftEndUnix = 0;
            string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters " +
            "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' " +
            "and m_Year = '" + iYear + "' and m_Month = " + iMonth + "";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            long lShiftStart = 0, lShiftEnd = 0;
                            if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")))
                            {
                                lShiftStart = reader.GetInt32(reader.GetOrdinal("m_ShiftStartTime"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")))
                                {
                                    lShiftEnd = reader.GetInt32(reader.GetOrdinal("m_ShiftEndTime"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Day" + iDay)))
                                    {
                                        if (reader.GetString(reader.GetOrdinal("m_Day" + iDay)).Equals(MyGlobal.WORKDAY_MARKER))
                                        {
                                            lShiftStartUnix = unixTimestampDayStart + lShiftStart;
                                            lShiftEndUnix = unixTimestampDayStart + lShiftEnd;
                                            if (lShiftEndUnix < lShiftStartUnix)
                                            {
                                                lShiftEndUnix += 86400;
                                            }
                                            if ((unixTimestampDayNow >= (lShiftStartUnix - const_ShiftPaddingPre)) &&
                                                (unixTimestampDayNow < (lShiftEndUnix + const_ShiftPaddingPost)))
                                            {
                                                sRoster = reader.GetString(reader.GetOrdinal("m_RosterName"));
                                                sShift = reader.GetString(reader.GetOrdinal("m_ShiftName"));
                                                iYearActive = iYear;
                                                iMonthActive = iMonth;
                                                iDayActive = iDay;
                                                return true;
                                            }
                                        }
                                    }
                                    /*
                                    // Time does not fall under any shift during the current day.
                                    // so, must be a shift starts on previous day
                                    iDay--;
                                    unixTimestampDayStart -= 86400;
                                    if (iDay > 0)
                                    {
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Day" + iDay)))
                                        {
                                            if (reader.GetString(reader.GetOrdinal("m_Day" + iDay)).Equals(MyGlobal.WORKDAY_MARKER))
                                            {
                                                lShiftStartUnix = unixTimestampDayStart + lShiftStart;
                                                lShiftEndUnix = unixTimestampDayStart + lShiftEnd;
                                                if (lShiftEndUnix < lShiftStartUnix)
                                                {
                                                    lShiftEndUnix += 86400;
                                                }
                                                if ((unixTimestampDayNow >= (lShiftStartUnix - const_ShiftPaddingPre)) &&
                                                    (unixTimestampDayNow < (lShiftEndUnix + const_ShiftPaddingPost)))
                                                {
                                                    sRoster = reader.GetString(reader.GetOrdinal("m_RosterName"));
                                                    sShift = reader.GetString(reader.GetOrdinal("m_ShiftName"));
                                                    iYearActive = iYear;
                                                    iMonthActive = iMonth;
                                                    iDayActive = iDay;
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                    */
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool GetShiftOn(MySqlConnection con, string profile,
            string staffid, int iYear, int iMonth, int iDay,
            string sRoster, string sShift, out string sRosterMarker,
            out long lShiftStartUnix, out long lShiftEndUnix
        )
        {
            sRosterMarker = "";
            lShiftStartUnix = 0;
            lShiftEndUnix = 0;
            string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters " +
            "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' " +
            "and m_RosterName='" + sRoster + "' and m_ShiftName='" + sShift + "' " +
            "and m_Year = '" + iYear + "' and m_Month = " + (iMonth - 1) + "";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            long lShiftStart = 0, lShiftEnd = 0;
                            if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")))
                            {
                                lShiftStart = reader.GetInt32(reader.GetOrdinal("m_ShiftStartTime"));
                                if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")))
                                {
                                    lShiftEnd = reader.GetInt32(reader.GetOrdinal("m_ShiftEndTime"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Day" + iDay)))
                                    {
                                        int iOrdinalDay0 = reader.GetOrdinal("m_Day1") - 1;
                                        sRosterMarker = reader.IsDBNull(iOrdinalDay0 + iDay) ? "" : reader.GetString(iOrdinalDay0 + iDay);

                                        if (reader.GetString(reader.GetOrdinal("m_Day" + iDay)).Equals(MyGlobal.WORKDAY_MARKER))
                                        {
                                            lShiftStartUnix = lShiftStart;
                                            lShiftEndUnix = lShiftEnd;
                                            if (lShiftEndUnix < lShiftStartUnix)
                                            {
                                                lShiftEndUnix += 86400;
                                            }
                                            sRoster = reader.GetString(reader.GetOrdinal("m_RosterName"));
                                            sShift = reader.GetString(reader.GetOrdinal("m_ShiftName"));
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private List<BreakItem> GetBreakJustifications(MySqlConnection con, string profile)
        {
            List<BreakItem> breaks = new List<BreakItem>();
            breaks.Add(new BreakItem("Select break option?", "Select break option?"));
            /*
            string sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks " +
                "where m_Profile = '" + profile + "' order by m_Name";
            sSQL = "select m_Name from(" +
            "SELECT m_Name, m_Profile FROM " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks breaks " +
            "union all " +
            "SELECT m_Name, m_Profile FROM " + MyGlobal.activeDB + ".tbl_misc_teams teams " +
            ") as x " +
            "where m_Profile = '" + profile + "' order by m_Name ";
            */
            string sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks " +
                            "where m_Profile = '" + profile + "' order by m_Name";

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
                                breaks.Add(new BreakItem(reader.GetString(0), reader.GetString(0)));
                            }
                        }
                    }
                }
            }
            return breaks;
        }
        [HttpPost]
        public ActionResult GetReportNames(string profile, string head)
        {
            var reportHeadsResponse = new ReportHeadsResponse();
            reportHeadsResponse.status = false;
            reportHeadsResponse.result = "";

            //reportHeadsResponse.breaks.Add(new BreakItem("Select break option?", "Select break option?"));
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------Get the team head from Justtification
                    string sTeamHead = "";
                    string sSQL = "SELECT m_Head FROM " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks " +
"where m_Profile = '" + profile + "' and m_Name='" + head + "'";
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
                                        if (!reader.IsDBNull(0))
                                            sTeamHead = reader.GetString(0);
                                    }
                                }
                            }
                        }
                    }
                    if (sTeamHead.Length == 0)
                    {
                        reportHeadsResponse.status = true;
                        reportHeadsResponse.result = "Team Head not available";
                        return Json(reportHeadsResponse, JsonRequestBehavior.AllowGet);
                    }
                    //------------------------
                    sSQL = "SELECT m_FName,m_StaffID,m_EMail m_Profile FROM " + MyGlobal.activeDB + ".tbl_staffs " +
"where m_Profile = '" + profile + "' and m_Team='" + sTeamHead + "' " +
"and (m_Status='Active' or m_Status='Trainee') " +
"and m_Band<>'Trainee' " +
"order by m_Name ";
                    //and m_Band<>'Execution'
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
                                        reportHeadsResponse.breaks.Add(
                                            new BreakItem(
                                                reader.GetString(2),
                                                reader.GetString(0)
                                                ));
                                    }
                                }
                            }
                        }
                    }
                }
                reportHeadsResponse.status = true;
            }
            catch (MySqlException ex1)
            {
                reportHeadsResponse.result = ex1.Message;
            }
            return Json(reportHeadsResponse, JsonRequestBehavior.AllowGet);
        }
        //-------------------------
        private void GetAdminFunctionalEmails(MySqlConnection con, string profile, string email,
            out string sAdminEmail, out string sFunctionalEmail)
        {
            sAdminEmail = "";
            sFunctionalEmail = "";
            string sSQL = "select m_ReportToAdministrative,m_ReportToFunctional from " + MyGlobal.activeDB + ".tbl_staffs " +
                "where m_Profile='" + profile + "' and m_Email='" + email + "' " +
            "and (m_Status='Active' or m_Status='Trainee') ";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) sAdminEmail = reader.GetString(0);
                            if (!reader.IsDBNull(1)) sFunctionalEmail = reader.GetString(1);
                        }
                    }
                }
            }
        }
        /* ------------------------------------------------------------------------------------- */
        private string GetMessageSQL_lock(MySqlConnection con,
            string profile, string sHardware,
            string sStaffID, string sActivity,
            string sActivityTime,
            string sWorktime,
            string sReasonHead,
            string sReasonNote,
            string sFName,
            string sStaffEmail,
            string sReportAdminEmail,
            string sReportFuncEmail)
        {
            string sNote = "", sAdminEmailOfConcernEvent = "", sAdminNameOfConcernEvent = "";
            string[] arData = sReasonNote.Split(new string[] { "*_*" }, StringSplitOptions.None);
            if (arData.Length >= 3)
            {
                sNote = arData[0];
                sAdminEmailOfConcernEvent = arData[1].Replace("%40", "@");// MyGlobal.Base64Encode(arData[1]);
                sAdminNameOfConcernEvent = arData[2];
            }
            else
            {
                sNote = sReasonNote;
            }
            if (sAdminEmailOfConcernEvent.Length == 0 &&
                !sReasonHead.Equals("Others"))
                return "";

            //----------------------------Get shift details at the specified time
            string sRoster = "", sShift = "";
            long lShiftStartUnix = 0, lShiftEndUnix = 0;
            int iYearActive = 0, iMonthActive = 0, iDayActive = 0;
            if (!GetActiveShiftDetails(
                con, profile, sStaffID,
                out sRoster, out sShift,
                out lShiftStartUnix, out lShiftEndUnix,
                out iYearActive, out iMonthActive, out iDayActive
                 )) return "";
            //--------------------------------

            string message = "<span style=''color:darkred;''><b>Break to meet " + sAdminNameOfConcernEvent + "</b></span>";
            if (sReasonHead.Equals("Others"))
            {
                message = "<span style=''color:darkred;''><b>Break with a note</b></span>";
            }
            message += "<br>" + sNote;

            long lActivityTime = MyGlobal.GetInt64(sActivityTime);
            //var timeSpan = TimeSpan.FromSeconds(lActivityTime);
            //DateTime time = new DateTime(timeSpan.Ticks).ToLocalTime();
            //DateTime tme = DateTime.Now;
            DateTime time = MyGlobal.ToDateTimeFromEpoch(lActivityTime);
            int iYear = time.Year;
            int iMonth = time.Month - 1;
            int iDay = time.Day;

            /*
            string session = "tme_" + sStaffID + "_" +
                iYear + "_" + time.Month + "_" + iDay + "_" +
                sRoster + "_" + sShift + "_" +
                sActivityTime;
                */
            string session = "tme_" + sStaffID + "_" + sActivityTime;
            //----------------------
            if (sReasonHead.Equals("Others"))
            {
                GetAdminFunctionalEmails(con, profile, sStaffEmail, out sReportAdminEmail, out sReportFuncEmail);
                sAdminEmailOfConcernEvent = sReportAdminEmail;
            }
            //------tbl_messages
            string sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated) values " +
                "('" + profile + "',1," +
                "'" + sStaffEmail + "','','" + sStaffID + "'," +
                "'" + sAdminEmailOfConcernEvent + "','',''," +
                "'" + session + "',Now(),Now());";

            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                                            "values ('" + profile + "','" + sStaffID + "','" + iYear + "','" + iMonth + "','" + iDay + "','" + sStaffEmail + "','" + sAdminEmailOfConcernEvent + "'," +
                                            "'" + message + "',Now(),'" + session + "','" + sRoster + "','" + sShift + "');";
            //------tbl_messages_clubs
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
            if (sAdminEmailOfConcernEvent.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
            "values ('" + profile + "','" + session + "','" + sAdminEmailOfConcernEvent + "');";

            if (sReasonHead.Equals("Others"))
            {
                if (sReportAdminEmail.Length > 5 && !sReportAdminEmail.Equals(sAdminEmailOfConcernEvent))
                    sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                        "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                if (sReportFuncEmail.Length > 5 && !sReportFuncEmail.Equals(sAdminEmailOfConcernEvent))
                    sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
            }

            //-------------------tbl_ot
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_ot (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_Roster,m_Shift,m_OTStatus,m_Session,m_OTDuration,m_Time) " +
                "values ('" + profile + "','" + sStaffID + "','" + iYear + "','" + iMonth + "','" + iDay + "'," +
                "'" + sRoster + "','" + sShift + "',1,'" + session + "','" + sWorktime + "',Now());";
            //-------------------tbl_accessmanager_activity
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                "(m_Profile,m_StaffID,m_Activity,m_ActivityTime,m_WorkTime,m_Session,m_IP,m_HardwareID,m_ReasonNote,m_ReasonHead) " +
                "values ('" + profile + "','" + sStaffID + "','requested'," +
                "'" + (lActivityTime + 1) + "','" + sWorktime + "'," +
                "'" + session + "','" + GetIPAddress() + "','" + sHardware + "','" + sReasonNote + "','" + sReasonHead + "');";
            return sSQL;
        }
        private string GetMessageSQL_open(MySqlConnection con,
            string profile, string sHardware,
            string sStaffID, string sActivity,
            string sActivityTime,
            string sWorktime,
            string sReasonHead,
            string sReasonNote,
            string sFName,
            string sStaffEmail,
            string sReportAdminEmail,
            string sReportFuncEmail)
        {
            string sNote = "", sAdminEmailOfConcernEvent = "", sAdminNameOfConcernEvent = "";
            string mode = "", sLastLockActivity = "", sLastLockActivityTime = "";
            string sWorkTimeSinceLastLock = "0";

            string[] arData = sReasonNote.Split(new string[] { "*_*" }, StringSplitOptions.None);
            if (arData.Length >= 7)
            {
                sNote = arData[0];
                sAdminEmailOfConcernEvent = arData[1].Replace("%40", "@");// MyGlobal.Base64Encode(arData[1]);
                sAdminNameOfConcernEvent = arData[2];

                mode = arData[3];
                sLastLockActivity = arData[4];          //  Last lock information
                sLastLockActivityTime = arData[5];      //  So that, open 
                sWorkTimeSinceLastLock = arData[6];
            }
            else
            {
                sNote = sReasonNote;
            }

            if (sLastLockActivityTime.Length == 0) return "";


            string sSQL = "";
            string session = "tme_" + sStaffID + "_" + sLastLockActivityTime;
            if (sLastLockActivity.Equals("lock"))
            {
                string sql = "select m_ReasonNote,m_ReasonHead from  " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "where m_Profile='" + profile + "' and m_Session='" + session + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sql, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (!reader.HasRows) return "";
                        if (reader.Read())
                        {

                            int Ordinal = reader.GetOrdinal("m_ReasonHead");
                            if (!reader.IsDBNull(Ordinal)) sReasonHead = reader.GetString(Ordinal);
                            Ordinal = reader.GetOrdinal("m_ReasonNote");
                            if (!reader.IsDBNull(Ordinal))
                            {
                                string sReasonNoteLock = MyGlobal.Base64Decode(reader.GetString(Ordinal));

                                string[] arDataLock = sReasonNoteLock.Split(new string[] { "*_*" }, StringSplitOptions.None);
                                if (arDataLock.Length >= 7)
                                {
                                    //sNote = arDataLock[0];
                                    sAdminEmailOfConcernEvent = arDataLock[1].Replace("%40", "@");// MyGlobal.Base64Encode(arDataLock[1]);
                                    sAdminNameOfConcernEvent = arDataLock[2];

                                    //mode = arDataLock[3];
                                    //sLastLockActivity = arDataLock[4];          //  Last lock information
                                    //sLastLockActivityTime = arDataLock[5];      //  So that, open 
                                    //sWorkTimeSinceLastLock = arDataLock[6];
                                }
                                else
                                {
                                    //sNote = sReasonNote;
                                }
                            }
                        }
                    }
                }


                if (sAdminEmailOfConcernEvent.Length == 0 &&
                    !sReasonHead.Equals("Others"))
                {

                    return "";
                }

                sSQL = "update " + MyGlobal.activeDB + ".tbl_ot " +
                "Set m_OTDuration='" + sWorkTimeSinceLastLock + "' where m_Profile='" + profile + "' and " +
                "m_OTStatus = 1 and m_OTDuration = 0 and m_Session='" + session + "';";

                sSQL += "Update " + MyGlobal.activeDB + ".tbl_accessmanager_activity Set " +
                "m_WorkTime='" + sWorkTimeSinceLastLock + "' where m_Session='" + session + "' " +
                "and m_Profile='" + profile + "';";

                string message = "";
                if (sReasonHead.Equals("Others"))
                {
                    message += "<span style=''color:darkgreen;''><b>Back to Terminal.</b></span>";
                }
                else
                {
                    message += "<span style=''color:darkgreen;''><b>Back to Terminal from " + sAdminNameOfConcernEvent + ".</b><br>";
                }
                message += " (" + Math.Round(MyGlobal.GetInt64(sWorkTimeSinceLastLock) / 60.00) + " Mins lapsed)</span>";
                if (sNote.Length > 0) message += "<br>" + sNote;


                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message,m_Time,m_Session) " +
                    "values ('" + profile + "','" + "" + "','" + sStaffEmail + "','" + sAdminEmailOfConcernEvent + "'," +
                    "'" + message + "',Now(),'" + session + "');";

                sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                "and m_Session='" + session + "';";


                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                        "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                if (sAdminEmailOfConcernEvent.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sAdminEmailOfConcernEvent + "');";
                if (sReasonHead.Equals("Others"))
                {

                    if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                            "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                    if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                            "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                }
            }
            else if (sLastLockActivity.Equals("forcedlock"))
            {
                //if (sAdminEmailOfConcernEvent.Length == 0) return "";
                if (sAdminEmailOfConcernEvent.Length == 0 &&
                    !sReasonHead.Equals("Others"))
                    return "";

                long lActivityTime = MyGlobal.GetInt64(sActivityTime);
                DateTime time = MyGlobal.ToDateTimeFromEpoch(lActivityTime);

                int iYear = time.Year;
                int iMonth = time.Month - 1;
                int iDay = time.Day;

                //----------------------------Get shift details at the specified time
                string sRoster = "", sShift = "";
                long lShiftStartUnix = 0, lShiftEndUnix = 0;
                int iYearActive = 0, iMonthActive = 0, iDayActive = 0;
                if (!GetActiveShiftDetails(
                    con, profile, sStaffID,
                    out sRoster, out sShift,
                    out lShiftStartUnix, out lShiftEndUnix,
                    out iYearActive, out iMonthActive, out iDayActive
                     )) return "";
                //--------------------------------


                string message = "";
                if (sReasonHead.Equals("Others"))
                {
                    GetAdminFunctionalEmails(con, profile, sStaffEmail, out sReportAdminEmail, out sReportFuncEmail);
                    sAdminEmailOfConcernEvent = sReportAdminEmail;
                    message = "<span style=''color:darkred;''><b>Break with a Note and back to terminal.</b></span>";
                }
                else
                {
                    message = "<span style=''color:darkred;''><b>Break to meet " + sAdminNameOfConcernEvent + " and back to terminal.</b>";
                }
                message += " (" + Math.Round(MyGlobal.GetInt64(sWorkTimeSinceLastLock) / 60.00) + " Mins lapsed)</span>";
                message += "<br>" + sNote;
                //------tbl_messages
                sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
    "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
    "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated) values " +
    "('" + profile + "',1," +
    "'" + sStaffEmail + "','','" + sStaffID + "'," +
    "'" + sAdminEmailOfConcernEvent + "','',''," +
    "'" + session + "',Now(),Now());";
                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                        "values ('" + profile + "','" + sStaffID + "','" + iYear + "','" + iMonth + "','" + iDay + "','" + sStaffEmail + "','" + sAdminEmailOfConcernEvent + "'," +
                        "'" + message + "',Now(),'" + session + "','" + sRoster + "','" + sShift + "');";
                //------tbl_messages_clubs
                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                        "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                if (sAdminEmailOfConcernEvent.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                "values ('" + profile + "','" + session + "','" + sAdminEmailOfConcernEvent + "');";

                if (sReasonHead.Equals("Others"))
                {
                    if (sReportAdminEmail.Length > 5 && !sReportAdminEmail.Equals(sAdminEmailOfConcernEvent))
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                        "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                    if (sReportFuncEmail.Length > 5 && !sReportFuncEmail.Equals(sAdminEmailOfConcernEvent))
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                }
                //-------------------tbl_ot
                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_ot (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_Roster,m_Shift,m_OTStatus,m_Session,m_OTDuration,m_Time) " +
                    "values ('" + profile + "','" + sStaffID + "','" + iYear + "','" + iMonth + "','" + iDay + "'," +
                    "'" + sRoster + "','" + sShift + "',1,'" + session + "','" + sWorkTimeSinceLastLock + "',Now());";

                //-------------------tbl_accessmanager_activity
                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "(m_Profile,m_StaffID,m_Activity,m_ActivityTime,m_WorkTime,m_Session,m_IP,m_HardwareID) " +
                    "values ('" + profile + "','" + sStaffID + "','requested'," +
                    "'" + (lActivityTime + 1) + "','" + sWorkTimeSinceLastLock + "'," +
                    "'" + session + "','" + GetIPAddress() + "','" + sHardware + "');";
            }
            return sSQL;
        }

        /* ------------------------------------------------------------------------------------- */
        public ActionResult TerminalActivities(string profile, string sort, string order, string page, string search, string timezone)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var terminalActivityResponse = new TerminalActivityResponse();
            terminalActivityResponse.status = false;
            terminalActivityResponse.result = "None";
            terminalActivityResponse.total_count = "";

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
                    String sSearchKey = " (activity.m_StaffID like '%" + search + "%' or " +
                        "activity.m_Staff like '%" + search + "%' or " +
                        "activity.m_Activity like '%" + search + "%' or " +
                        "activity.m_HardwareID like '%" + search + "%') ";
                    /*
                    sSQL = "select count(activity.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity " +
                        "left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID=activity.m_StaffID and staffs.m_Profile='" + profile + "' " +
                        "where " + sSearchKey + " and activity.m_Profile='" + profile + "' " +
                        "order by activity.m_ActivityTime desc;";
                        */
                    sSQL = "select count(activity.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity " +
    "where " + sSearchKey + " and activity.m_Profile='" + profile + "' " +
    "order by activity.m_ActivityTime desc;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) terminalActivityResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_ActivityTime";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    //  where m_Profile='grey' 
                    /*
                    sSQL = "SELECT activity.m_id,activity.m_HardwareID,staffs.m_FName,staffs.m_MName,staffs.m_LName," +
                        "activity.m_IP,activity.m_StaffID,activity.m_Activity," +
                        "FROM_UNIXTIME(activity.m_ActivityTime,'%Y-%m-%d %H:%i:%s') AS gmt," +
                        "activity.m_Lat,activity.m_Lng,activity.m_ReasonHead,activity.m_ReasonNote,staffs.m_Email," +
                        "staffs.m_Team " +
                        "FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity ";
                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID=activity.m_StaffID and staffs.m_Profile='" + profile + "'";
                    sSQL += "where " + sSearchKey + " and activity.m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    */
                    sSQL = "SELECT activity.m_id,activity.m_HardwareID,activity.m_Staff,'',''," +
                        "activity.m_IP,activity.m_StaffID,activity.m_Activity," +
                        "FROM_UNIXTIME(activity.m_ActivityTime,'%Y-%m-%d %H:%i:%s') AS gmt," +
                        "activity.m_Lat,activity.m_Lng,activity.m_ReasonHead,activity.m_ReasonNote,''," +
                        "'' " +
                        "FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity ";
                    sSQL += "where " + sSearchKey + " and activity.m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    TerminalActivityRow terminalItem = new TerminalActivityRow();
                                    if (!reader.IsDBNull(0)) terminalItem.m_ID = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) terminalItem.m_HardwareID = reader[1].ToString();
                                    terminalItem.m_HardwareName = "";
                                    if (!reader.IsDBNull(2)) terminalItem.m_Name = reader[2].ToString();
                                    //if (!reader.IsDBNull(3)) terminalItem.m_StaffName += " " + reader[3].ToString();
                                    //if (!reader.IsDBNull(4)) terminalItem.m_StaffName += " " + reader[4].ToString();
                                    if (!reader.IsDBNull(5)) terminalItem.m_IP = reader[5].ToString();
                                    if (!reader.IsDBNull(6)) terminalItem.m_StaffID = reader[6].ToString();
                                    if (!reader.IsDBNull(7)) terminalItem.m_Activity = reader[7].ToString();
                                    if (!reader.IsDBNull(8)) terminalItem.m_ActivityTime = reader[8].ToString();

                                    if (!reader.IsDBNull(9)) terminalItem.m_Lat = reader.GetDouble("m_Lat");
                                    if (!reader.IsDBNull(10)) terminalItem.m_Lng = reader.GetDouble("m_Lng");

                                    if (!reader.IsDBNull(11)) terminalItem.m_ReasonHead = reader[11].ToString();
                                    if (!reader.IsDBNull(12))
                                    {
                                        string[] arData = MyGlobal.Base64Decode(reader[12].ToString()).Split(new string[] { "*_*" }, StringSplitOptions.None);
                                        if (arData.Length >= 3)
                                        {
                                            terminalItem.m_ReasonNote = arData[0];
                                        }
                                        else
                                        {
                                            terminalItem.m_ReasonNote = MyGlobal.Base64Decode(reader[12].ToString());
                                        }
                                    }
                                    if (!reader.IsDBNull(13)) terminalItem.m_Email = reader[13].ToString();
                                    //--------------------------------------------------
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) terminalItem.m_Team = reader.GetString(reader.GetOrdinal("m_Team"));
                                    terminalItem.m_Team = GetTeam(profile, terminalItem.m_StaffID);

                                    terminalActivityResponse.items.Add(terminalItem);
                                }
                                terminalActivityResponse.status = true;
                                terminalActivityResponse.result = "Done";
                            }
                            else
                            {
                                terminalActivityResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                terminalActivityResponse.result = "Error-" + ex.Message;
            }
            return Json(terminalActivityResponse, JsonRequestBehavior.AllowGet);
        }
        //--------------------
        private string GetTeam(string profile, string staffid)
        {
            string sSQL = "select m_Team from " + MyGlobal.activeDB + ".tbl_staffs " +
                "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "'";

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
                                if (!reader.IsDBNull(0)) return reader.GetString(0);
                            }
                        }
                    }

                }
            }
            return "";
        }
        /* ------------------------------------------------------------------------------------- */
        public ActionResult StaffActivities(string profile, string sort, string order, string page, string search, string timezone)
        {
            var staffActivityResponse = new StaffActivityResponse();
            staffActivityResponse.status = false;
            staffActivityResponse.result = "None";
            staffActivityResponse.total_count = "";

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
                    String sSearchKey = " (staffs.m_StaffID like '%" + search + "%' or " +
                        "staffs.m_FName like '%" + search + "%' or " +
                        "activity.m_Activity like '%" + search + "%') ";

                    sSQL = "select count(activity.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_staffs staffs " +
                        "left join " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity on activity.m_StaffID=staffs.m_StaffID and staffs.m_Profile='" + profile + "' " +
                        "where " + sSearchKey + " and staffs.m_Profile='" + profile + "' " +
                        "order by staffs.m_FName asc;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) staffActivityResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT staffs.m_id,staffs.m_FName," +
                        "activity.m_IP,staffs.m_StaffID,activity.m_Activity," +
                        "FROM_UNIXTIME(activity.m_ActivityTime,'%Y-%m-%d %H:%i:%s') AS gmt," +
                        "activity.m_Lat,activity.m_Lng,activity.m_ReasonHead,activity.m_ReasonNote,staffs.m_Email,staffs.m_Team, " +
                        "activity.m_HardwareID " +
                        "FROM " + MyGlobal.activeDB + ".tbl_staffs staffs ";
                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity on activity.m_StaffID=staffs.m_StaffID and staffs.m_Profile='" + profile + "'";
                    sSQL += "where " + sSearchKey + " and activity.m_Profile='" + profile + "' ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    StaffItem staffItem = new StaffItem();
                                    if (!reader.IsDBNull(0)) staffItem.m_id = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) staffItem.m_FName = reader[1].ToString();
                                    if (!reader.IsDBNull(2)) staffItem.m_IP = reader[2].ToString();
                                    if (!reader.IsDBNull(3)) staffItem.m_StaffID = reader[3].ToString();
                                    if (!reader.IsDBNull(4)) staffItem.m_Activity = reader[4].ToString();
                                    if (!reader.IsDBNull(5)) staffItem.m_ActivityTime = reader[5].ToString();

                                    if (!reader.IsDBNull(6)) staffItem.m_Lat = reader.GetDouble("m_Lat");
                                    if (!reader.IsDBNull(7)) staffItem.m_Lng = reader.GetDouble("m_Lng");

                                    if (!reader.IsDBNull(8)) staffItem.m_ReasonHead = reader[8].ToString();
                                    //if (!reader.IsDBNull(9)) staffItem.m_ReasonNote = reader[9].ToString();

                                    if (!reader.IsDBNull(9))
                                    {
                                        string[] arData = MyGlobal.Base64Decode(reader[9].ToString()).Split(new string[] { "*_*" }, StringSplitOptions.None);
                                        if (arData.Length >= 3)
                                        {
                                            staffItem.m_ReasonNote = arData[0];
                                        }
                                        else
                                        {
                                            staffItem.m_ReasonNote = MyGlobal.Base64Decode(reader[9].ToString());
                                        }
                                    }


                                    if (!reader.IsDBNull(10)) staffItem.m_Email = reader[10].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) staffItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_HardwareID"))) staffItem.m_HardwareID = reader["m_HardwareID"].ToString();

                                    staffActivityResponse.items.Add(staffItem);
                                }
                                staffActivityResponse.status = true;
                                staffActivityResponse.result = "Done";
                            }
                            else
                            {
                                staffActivityResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                staffActivityResponse.result = "Error-" + ex.Message;
            }
            return Json(staffActivityResponse, JsonRequestBehavior.AllowGet);
        }
        /* ------------------------------------------------------------------------------------- */
        public ActionResult Terminals(string profile, string sort, string order, string page, string search, string timezone,
            string showoptions)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var terminalActivityResponse = new TerminalActivityResponse();
            terminalActivityResponse.status = false;
            terminalActivityResponse.result = "None";
            terminalActivityResponse.total_count = "";
            terminalActivityResponse.page_size = 15;
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            //Int16.TryParse(timezone, out iTimeZone);
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    String sSearchKey = " (terminal.m_StaffID like '%" + search + "%' or " +
                        "m_Staff like '%" + search + "%' or " +
                        "m_Activity like '%" + search + "%' or " +
                        "m_HardwareID like '%" + search + "%') ";

                    //sSQL = @"SELECT count(m_id) as cnt from (select activity.m_id FROM  " + MyGlobal.activeDB + ".tbl_accessmanager_activity as activity " +
                    //"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID=activity.m_StaffID and staffs.m_Profile='" + profile + "' where " + sSearchKey + " and activity.m_Profile='" + profile + "' group by m_HardwareID) as x";
                    sSQL = "select count(m_HardwareID) as cnt from (" +
                        "select m_HardwareID from " + MyGlobal.activeDB + ".tbl_terminals terminal where " + sSearchKey +
                        "and m_Profile='" + profile + "' " +
                        "group by m_HardwareID " +
                        ") as x";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) terminalActivityResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    //and m_Profile='" + profile + "'
                    int iPageSize = terminalActivityResponse.page_size;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_ActivityTime";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    sSQL = "select *,staff.m_Email,staff.m_Team from " + MyGlobal.activeDB + ".tbl_terminals terminal " +
                    "left join " + MyGlobal.activeDB + ".tbl_staffs staff on staff.m_StaffID=terminal.m_StaffID and staff.m_Profile=terminal.m_Profile " +
                    "where " + sSearchKey + " and terminal.m_Profile='" + profile + "' ";
                    if (showoptions.Equals("1"))    // Active
                    {
                        sSQL += "and (m_ActivityTime >(unix_timestamp(DATE_ADD(NOW(), INTERVAL -5 MINUTE)))) ";
                        sSQL += "and (m_Activity='open' || m_Activity='update') ";
                    }
                    else if (showoptions.Equals("2"))   // Locked
                    {
                        sSQL += "and (m_ActivityTime >(unix_timestamp(DATE_ADD(NOW(), INTERVAL -5 MINUTE)))) ";
                        sSQL += "and (m_Activity='lock' || m_Activity='lockstate') ";
                    }
                    else if (showoptions.Equals("3"))   // Expired ---- 0 to all
                    {
                        sSQL += "and (m_ActivityTime < (unix_timestamp(DATE_ADD(NOW(), INTERVAL -5 MINUTE)))) ";
                    }
                    sSQL += "group by m_HardwareID " +
                    "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    TerminalActivityRow terminalItem = new TerminalActivityRow();
                                    if (!reader.IsDBNull(0)) terminalItem.m_ID = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) terminalItem.m_HardwareID = reader[1].ToString();
                                    if (!reader.IsDBNull(2)) terminalItem.m_Name = reader[2].ToString();
                                    if (!reader.IsDBNull(3)) terminalItem.m_StaffID = reader[3].ToString();
                                    if (!reader.IsDBNull(4)) terminalItem.m_Activity = reader[4].ToString();
                                    if (!reader.IsDBNull(5)) terminalItem.m_ActivityStart =
                                            MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(5) + 19800).ToString("yyyy-MM-dd HH:mm:ss");
                                    if (!reader.IsDBNull(6))
                                    {
                                        //terminalItem.m_ActivityTime = reader.GetDateTime(5).ToString("yyyy-MM-dd HH:mm:ss");
                                        terminalItem.m_ActivityTime =
                                            MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(6) + 19800).ToString("yyyy-MM-dd HH:mm:ss");
                                        //var dateOne = DateTime.Now;
                                        //var dateTwo = reader.GetDateTime(5);
                                        //terminalItem.LiveSince = (int)((TimeSpan)dateOne.Subtract(dateTwo)).TotalSeconds;
                                        terminalItem.LiveSince = unixTimestamp - reader.GetInt32(6);
                                        if (!reader.IsDBNull(5))
                                            terminalItem.SinceActivity = unixTimestamp - reader.GetInt32(5);
                                    }
                                    if (!reader.IsDBNull(7)) terminalItem.m_ReasonHead = reader[7].ToString();
                                    if (!reader.IsDBNull(8)) terminalItem.m_ReasonNote = MyGlobal.Base64Decode(reader[8].ToString());
                                    if (!reader.IsDBNull(9)) terminalItem.m_Lat = reader.GetDouble(9);
                                    if (!reader.IsDBNull(10)) terminalItem.m_Lng = reader.GetDouble(10);
                                    if (!reader.IsDBNull(11)) terminalItem.m_IP = reader.GetString(11);
                                    if (!reader.IsDBNull(13)) terminalItem.m_Version = reader.GetString(13);
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) terminalItem.m_Email = reader["m_Email"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) terminalItem.m_Team = reader["m_Team"].ToString();

                                    /*
                                    if (!reader.IsDBNull(13))
                                    {
                                        Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                        terminalItem.LiveSince = unixTimestamp-reader.GetInt32(13);
                                    } 
                                    */
                                    terminalActivityResponse.items.Add(terminalItem);
                                }
                                terminalActivityResponse.status = true;
                                terminalActivityResponse.result = "Done";
                            }
                            else
                            {
                                terminalActivityResponse.result = "Sorry!!! No terminals";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                terminalActivityResponse.result = "Error-" + ex.Message;
            }
            return Json(terminalActivityResponse, JsonRequestBehavior.AllowGet);
        }
        /* ------------------------------------------------------------------------------------- */
        // This is meterbox signin
        public ActionResult ClientSignIn(string profile, string hardware,
            string user, string pass, string version, string head,
            string lastlock, string adminemail, string adminname)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var clientSignInResponse = new ClientSignInResponse();
            clientSignInResponse.status = false;
            clientSignInResponse.result = "None";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------Update version
                    sSQL = "update " + MyGlobal.activeDB + ".tbl_terminals Set m_Version='" + version + "' " +
                        "where m_HardwareID='" + hardware + "' and m_Profile='" + profile + "'";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con)) com.ExecuteNonQuery();
                    //-----------------------Confirm login
                    sSQL = @"select m_StaffID,m_FName,m_MName,m_LName,m_Email from " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "(m_Username='" + user + "' or m_Email='" + user + "' or m_StaffID='" + user + "') " +
                    "and m_Password='" + pass + "' and m_Profile='" + profile + "' " +
                    "and (m_Status='active' or m_Status='trainee' or m_Status='temporary')";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    clientSignInResponse.m_StaffID = "";
                                    if (!reader.IsDBNull(0)) clientSignInResponse.m_StaffID = reader[0].ToString();
                                    if (!reader.IsDBNull(1)) clientSignInResponse.Name = reader[1].ToString();
                                    //if (!reader.IsDBNull(2)) clientSignInResponse.Name += " " + reader[2].ToString();
                                    //if (!reader.IsDBNull(3)) clientSignInResponse.Name += " " + reader[3].ToString();
                                    if (!reader.IsDBNull(4)) clientSignInResponse.m_Email = reader[4].ToString();
                                    if (clientSignInResponse.m_StaffID.Length > 0)
                                    {
                                        clientSignInResponse.status = true;
                                        clientSignInResponse.result = "";
                                        //-------------Signout, if any other instance
                                        if (ChatHub._StaffsLocation.ContainsKey(clientSignInResponse.m_StaffID))
                                        {
                                            if (!ChatHub._StaffsLocation[clientSignInResponse.m_StaffID].Equals(hardware))
                                            {   // Staff logged in from different hardware
                                                HubObject hubObject = new HubObject();
                                                hubObject.Mode = "logout";
                                                SendHubObject_ToTerminal(ChatHub._StaffsLocation[clientSignInResponse.m_StaffID], hubObject);
                                                //ChatHub.MessageToDebugger("LifeToHub-Command to " +                                                     ChatHub._StaffsLocation[clientSignInResponse.m_StaffID] + " to logout[" + clientSignInResponse.m_StaffID + "]");
                                                ChatHub._StaffsLocation[clientSignInResponse.m_StaffID] = hardware;
                                            }
                                        }
                                        else
                                        {
                                            ChatHub._StaffsLocation.Add(clientSignInResponse.m_StaffID, hardware);
                                        }
                                    }
                                    else
                                    {
                                        clientSignInResponse.result = "StaffID not assigned";
                                    }
                                }
                            }
                        }
                    }
                    if (lastlock.Equals("forcedlock"))
                    {
                        if (head.Length > 0) clientSignInResponse.LockReasonReceived = true;
                        clientSignInResponse.breaks = GetBreakJustifications(con, profile);
                    }
                    // If late in the morning, send approval request or make this delay under their cost
                    if (clientSignInResponse.status)
                    {
                        //----------------------------Get shift details at the specified time
                        string sRoster = "", sShift = "";
                        long lShiftStartUnix = 0, lShiftEndUnix = 0;
                        int iYearActive = 0, iMonthActive = 0, iDayActive = 0;
                        if (GetActiveShiftDetails(
                            con, profile, clientSignInResponse.m_StaffID,
                            out sRoster, out sShift,
                            out lShiftStartUnix, out lShiftEndUnix,
                            out iYearActive, out iMonthActive, out iDayActive
                             ))
                        {

                            long lLoginDelay = GetFirstOpenWithinThisTimeFrame(
                                con, profile, clientSignInResponse.m_StaffID,
                                lShiftStartUnix,
                                lShiftEndUnix
                                );
                            if (lLoginDelay > 0)
                            {   // There is a delay
                                if (lLoginDelay > MyGlobal.const_ALLOWED_LATE_DELAY) // Put your delay accepted here
                                {

                                    if (!IsLateLoginApprovalAlreadySent(con, profile, clientSignInResponse.m_StaffID,
                                        lShiftStartUnix, lShiftEndUnix))
                                    {
                                        //-----------late delay approval process
                                        LateDelayApprovalMessage(
                                            con, profile, clientSignInResponse.m_StaffID,
                                            sRoster, sShift, iYearActive, iMonthActive, iDayActive,
                                            hardware,
                                            lShiftStartUnix,
                                            lLoginDelay);
                                    }
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                // No delay to work

                            }
                            //--------------------------------
                        }
                    }
                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ClientSignIn > MySqlException > " + ex.Message);
                clientSignInResponse.result = "MySqlException-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ClientSignIn > Exception > " + ex.Message);
                clientSignInResponse.result = "Exception-" + ex.Message;
            }
            return Json(clientSignInResponse, JsonRequestBehavior.AllowGet);
        }
        private bool IsLateLoginApprovalAlreadySent(MySqlConnection con, string profile, string staffid,
            long lShiftStartUnix, long lShiftEndUnix)
        {
            lShiftStartUnix -= 19800;
            lShiftEndUnix -= 19800;
            string sSQL = "SELECT m_ActivityTime FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
            "where m_Profile = '" + profile + "' and m_staffID = '" + staffid + "' " +
            "and (m_Activity = 'requested' or m_Activity = 'approved' or m_Activity = 'rejected') " +
            "and m_ActivityTime>= '" + (lShiftStartUnix - const_ShiftPaddingPre) + "' " +
            "and m_ActivityTime<'" + lShiftEndUnix + "' " +
            "and m_ActivityTime= '" + (lShiftStartUnix) + "' " +
            "limit 1;";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }
        private void LateDelayApprovalMessage(MySqlConnection con, string profile, string staffid,
            string roster, string shift, int year, int month, int day, string hardware,
            long lShiftStartUnix, long lLoginDelay)
        {
            string sFName, sStaffEmail = "", sReportAdminEmail = "",
                    sReportFuncEmail = "", sErrMessage = "";
            GetStaffDetails_FromStaffID(con, profile, staffid,
                out sFName, out sStaffEmail, out sReportAdminEmail,
                out sReportFuncEmail, out sErrMessage);

            string session = "ot_" + staffid + "_" + year + "_" + month + "_" + day + "_" +
                roster + "_" + shift + "_" + DateTime.Now.ToString("HHmmss");
            string message = "";
            message += "<table class=''LveTbl''>";
            message += "<tr class=''LveTR1''><td>Late Login Request</td>";
            //message += "<td class=''LveTD1''>" + leavetype + "</td>";
            message += "<td class=''LveTD2''>" + year + "-" + (month + 1) + "-" + day + "</td>" +
                "<td><span class=''LveTD5''>" + Math.Floor((decimal)lLoginDelay / 60) + "</span> Mins</td></tr>";
            message += "<tr class=''LveTR2''><td colspan=3>Shift <span class=''CHT''>" + shift + "</span> of Roster <span class=''CHT''>" + roster + "</span></td></tr>";
            message += "</table>";
            //message += "<span class=''LveR''>" + otreason + "<span>";

            string sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Priority) values " +
                "('" + profile + "',1," +
                "'" + sStaffEmail + "','','" + staffid + "'," +
                "'" + sReportAdminEmail + "','',''," +
                "'" + session + "',Now(),Now(),1);";


            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                "values ('" + profile + "','" + staffid + "','" + year + "','" + month + "','" + day + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                "'" + message + "',Now(),'" + session + "','" + roster + "','" + shift + "');";
            //-------------------------------------
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
            if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
            if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
            //-------------------------------------
            /*
            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_ot Set m_OTStatus=5 where m_Profile='" + profile + "' " +
                "and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "' " +
                "and m_Day='" + day + "' and m_Roster='" + roster + "' and m_Shift='" + shift + "';";
                */
            lLoginDelay = 0; // Because, this is only dummy approval
            //-------------------tbl_ot
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_ot (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_Roster,m_Shift,m_OTStatus,m_Session,m_OTDuration,m_Time) " +
                "values ('" + profile + "','" + staffid + "','" + year + "','" + month + "','" + day + "'," +
                "'" + roster + "','" + shift + "',1,'" + session + "','" + lLoginDelay + "',Now());";

            //-------------------tbl_accessmanager_activity
            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                "(m_Profile,m_StaffID,m_Activity,m_ActivityTime,m_WorkTime,m_Session,m_IP,m_HardwareID) " +
                "values ('" + profile + "','" + staffid + "','requested'," +
                "'" + (lShiftStartUnix - 19800) + "','" + lLoginDelay + "'," +
                "'" + session + "','" + GetIPAddress() + "','" + hardware + "');";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                mySqlCommand.ExecuteNonQuery();
            }
            //--------------------------------------
            HubObject hub = GetPendingMessagesObject(con, profile, "times", sReportAdminEmail);
            SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);


        }
        private long GetFirstOpenWithinThisTimeFrame(
            MySqlConnection con, string profile, string staffid,
            long lShiftStartUnix, long lShiftEndUnix)
        {
            long lFirstLogin = 0;
            lShiftStartUnix -= 19800;
            lShiftEndUnix -= 19800;
            Int32 unixTimestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - 19800;

            string sSQL = "SELECT m_ActivityTime FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                "where m_Profile = '" + profile + "' and m_staffID = '" + staffid + "' and m_Activity = 'open' " +
                "and m_ActivityTime>= '" + (lShiftStartUnix - const_ShiftPaddingPre) + "' " +
                "and m_ActivityTime<'" + lShiftEndUnix + "' " +
                "order by m_ActivityTime " +
                "limit 1;";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) lFirstLogin = reader.GetInt32(0);
                        }
                    }
                }
            }

            //DateTime dt1 = MyGlobal.ToDateTimeFromEpoch(lFirstLogin);
            //MyGlobal.Error("First Login >>>" + dt1.ToString("yyyy-MM-dd HH:mm:ss"));

            if (lFirstLogin == 0)
            {
                return unixTimestamp - lShiftStartUnix;
            }
            else
            {
                //MyGlobal.Error((lShiftStartUnix - lFirstLogin) + "...");
                return lFirstLogin - lShiftStartUnix;
            }
        }
        public ActionResult GetNow(string profile, string email, string sort, string order,
            string page, string search, string date)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var nowResponse = new GetNowResponse();
            nowResponse.status = false;
            nowResponse.result = "None";
            nowResponse.total_count = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //________________________________________________________________
                    DateTime dtNow = MyGlobal.ToDateTimeFromEpoch(MyGlobal.GetInt32(date));//DateTime.Now;
                    int iYear = dtNow.Year;
                    int iMonth = dtNow.Month;
                    int iDate = dtNow.Day;
                    //________________________________________________________________
                    String sSearchKey = " (m_StaffName like '%" + search + "%' or " +
                        "rosters.m_StaffID like '%" + search + "%' or " +
                        "m_Country like '%" + search + "%' or " +
                        "m_Email like '%" + search + "%' or " +
                        "m_Designation like '%" + search + "%' or " +
                        "m_Roll like '%" + search + "%' or " +
                        "m_Team like '%" + search + "%' or " +
                        "m_Type like '%" + search + "%' or " +
                        "m_Base like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";

                    sSQL =
"SELECT count(rosters.m_id) as cnt FROM " + MyGlobal.activeDB + ".tbl_rosters rosters " +
"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = rosters.m_StaffID and staffs.m_Profile = rosters.m_Profile " +
"left join " + MyGlobal.activeDB + ".tbl_attendance attn on attn.m_StaffID = rosters.m_StaffID and attn.m_Profile = rosters.m_Profile " +
"and attn.m_Date='" + MyGlobal.GetUnixTime(iYear, iMonth, iDate) + "' " +
"where  " + sSearchKey + "  and rosters.m_Year = " + iYear + " and rosters.m_Month = " + (iMonth - 1) + " " +
//"and (UNIX_TIMESTAMP(DATE_FORMAT(CONCAT(CURDATE(), ' 00:00:00'), '%Y-%m-%d %H:%i:%s')) + m_ShiftStartTime - 3600) < UNIX_TIMESTAMP() " +
//"and (UNIX_TIMESTAMP(DATE_FORMAT(CONCAT(CURDATE(), ' 23:59:59'), '%Y-%m-%d %H:%i:%s')) + m_ShiftEndTime + 3600) >= UNIX_TIMESTAMP() " +
"and rosters.m_StaffID is not null and rosters.m_Profile = '" + profile + "' " +
"and length(m_Day" + iDate + ")>0 ";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) nowResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //----------------------------------------------------------------

                    int iPageSize = 10;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_StaffName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    if (sort.Equals("m_StaffID")) sort = "rosters." + sort;

                    string sFileds = "rosters.m_id,rosters.m_StaffID,m_Email,m_StaffName," +
                        "m_Designation,m_Team,m_Base," +
"rosters.m_RosterName,rosters.m_ShiftName,m_Day" + iDate + ", " +
"case when (m_ShiftStartTime>19800) then DATE_FORMAT(FROM_UNIXTIME(m_ShiftStartTime-19800), '%H:%i') else '...' end AS 'shiftStart'," +
"case when (m_ShiftEndTime>19800) then DATE_FORMAT(FROM_UNIXTIME(m_ShiftEndTime-19800), '%H:%i') else '...' end AS 'shiftEnd'," +
"case when (m_ActualStart>19800) then DATE_FORMAT(FROM_UNIXTIME(m_ActualStart-19800), '%H:%i') else '...' end AS 'shiftStartActual'," +
"case when (m_ActualEnd>19800) then DATE_FORMAT(FROM_UNIXTIME(m_ActualEnd-19800), '%H:%i') else '...' end AS 'shiftEndActual'," +
"m_ShiftStartTime,m_ShiftEndTime";

                    sSQL = sSQL.Replace("count(rosters.m_id) as cnt", sFileds);

                    sSQL += " order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    NowItem nowItem = new NowItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) nowItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffName"))) nowItem.m_StaffName = reader["m_StaffName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) nowItem.m_StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) nowItem.m_Email = reader["m_Email"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_RosterName"))) nowItem.m_RosterName = reader["m_RosterName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftName"))) nowItem.m_ShiftName = reader["m_ShiftName"].ToString();

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Day" + iDate)))
                                    {
                                        nowItem.m_RosterMarker = reader["m_Day" + iDate].ToString();
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("shiftStart")) &&
                                        !reader.IsDBNull(reader.GetOrdinal("shiftEnd")))
                                    {
                                        nowItem.m_ShiftAssigned = reader["shiftStart"].ToString() +
                                            " - " + reader["shiftEnd"].ToString();
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("shiftStartActual")) &&
                                        !reader.IsDBNull(reader.GetOrdinal("shiftEndActual")))
                                    {
                                        nowItem.m_ShiftActual = reader["shiftStartActual"].ToString() +
                                            " - " + reader["shiftEndActual"].ToString();
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) nowItem.m_Designation = reader["m_Designation"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) nowItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) nowItem.m_Base = reader["m_Base"].ToString();


                                    nowResponse.items.Add(nowItem);
                                }
                                nowResponse.status = true;
                                nowResponse.result = "";
                            }
                            else
                            {
                                //setStaffsResponse.result = "<span style='color:red;'>Sorry!!! No Staffs</span>";
                                nowResponse.result = "Sorry!!! No Staffs";
                            }
                        }
                    }


                }
            }
            catch (MySqlException ex)
            {
                //setStaffsResponse.result = "<span style='color:red;'>Error-" + ex.Message + "</span>";
                nowResponse.result = "Error-" + ex.Message + "";
                MyGlobal.Error("GetNow-MySqlException-" + ex.Message);
            }

            return Json(nowResponse, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        public ActionResult GetStaffs(string profile, string sort, string order,
            string page, string search, string timezone,
            string showoptions, string statuscount, string cardselected, string showalloption)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var setStaffsResponse = new GetStaffsResponse();
            setStaffsResponse.status = false;
            setStaffsResponse.result = "None";
            setStaffsResponse.total_count = "";
            if (showoptions.Length == 0) showoptions = "All";
            if (showoptions.Equals("undefined")) showoptions = "All";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int iStartDate = 1;
                    sSQL = "select m_AttnStartDate from " + MyGlobal.activeDB + ".tbl_profile_info where " +
                        "m_Profile='" + profile + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) iStartDate = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    if (statuscount.Equals("0"))
                    {
                        setStaffsResponse.sarStatus.Add("All");

                        sSQL = "select m_Status from " + MyGlobal.activeDB + ".tbl_staffs  " +
                            "where m_Profile='" + profile + "' group by m_Status order by m_Status;";
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
                                            setStaffsResponse.sarStatus.Add(reader.GetString(0));
                                        }
                                    }
                                }
                            }
                        }
                        setStaffsResponse.sarStatus.Add("Last Worked");
                    }
                    //________________________________________________________________
                    int iYearName = 0;
                    int iMonthName = 0;
                    DateTime dtNow = DateTime.Now;
                    DateTime dtStart = new DateTime(dtNow.Year, dtNow.Month, iStartDate);
                    DateTime dtEnd = dtStart.AddDays(-1);
                    if (dtEnd > dtNow)
                    {
                        dtEnd = dtEnd.AddMonths(-1);
                    }
                    iYearName = dtEnd.Year;
                    iMonthName = dtEnd.Month;

                    /*
                    int iDate = MyGlobal.GetInt16(dt.ToString("dd"));
                    if (iDate > iStartDate)
                    {
                        iYearName = MyGlobal.GetInt16(dt.ToString("yyyy"));
                        iMonthName = MyGlobal.GetInt16(dt.ToString("MM"));
                    }
                    else
                    {
                        iYearName = MyGlobal.GetInt16(dt.ToString("yyyy"));
                        iMonthName = MyGlobal.GetInt16(dt.ToString("MM"));
                        iMonthName--;
                        if (iMonthName < 1)
                        {
                            iMonthName = 12;
                            iYearName--;
                        }
                    }
                    string sYear = dt.ToString("yyyy");
                    int iMonth = MyGlobal.GetInt16(dt.ToString("MM")) - 1;
                    */

                    //________________________________________________________________
                    String sSearchKey = " (m_FName like '%" + search + "%' or " +
                        "staff.m_StaffID like '%" + search + "%' or " +
                        "m_Country like '%" + search + "%' or " +
                        "m_Email like '%" + search + "%' or " +
                        "m_Designation like '%" + search + "%' or " +
                        "m_Roll like '%" + search + "%' or " +
                        "m_Team like '%" + search + "%' or " +
                        "m_Type like '%" + search + "%' or " +
                        "m_Grade like '%" + search + "%' or " +
                        "m_Base like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%') ";
                    if (search.Length == 0)
                    {
                        if (cardselected.Equals("1")) // Un approved
                        {
                            sSearchKey += "and summary.m_ApprovedBy1 is null and summary.m_ApprovedBy2 is null and summary.m_ApprovedBy3 is null ";
                        }
                        else if (cardselected.Equals("2")) // HR Approved
                        {
                            sSearchKey += "and summary.m_ApprovedBy1 is not null and summary.m_ApprovedBy2 is null and summary.m_ApprovedBy3 is null ";
                        }
                        else if (cardselected.Equals("3")) // HR & Production Approved
                        {
                            sSearchKey += "and summary.m_ApprovedBy1 is not null and summary.m_ApprovedBy2 is not null and summary.m_ApprovedBy3 is null ";
                        }
                        else if (cardselected.Equals("4")) // Admin approval
                        {
                            sSearchKey += "and summary.m_ApprovedBy1 is not null and summary.m_ApprovedBy2 is not null and summary.m_ApprovedBy3 is not null and summary.m_ApprovedBy4 is null ";
                        }
                        else if (cardselected.Equals("9")) // Payslip ready
                        {
                            sSearchKey += "and summary.m_ApprovedBy1 is not null and summary.m_ApprovedBy2 is not null and summary.m_ApprovedBy3 is not null and summary.m_ApprovedBy4 is not null ";
                        }
                        else // Nothing to be applied
                        {

                        }
                    }
                    sSQL = "select count(staff.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                        "left join " +
                        "(select * from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) +
                        "' group by m_StaffID) " +
                        "summary on summary.m_StaffID = staff.m_StaffID " +
                        "and summary.m_Profile = staff.m_Profile and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "' " +
                        "where " + sSearchKey + " and staff.m_Profile='" + profile + "' ";
                    if (search.Length == 0)
                    {
                        if (showoptions.Equals("All"))
                        {

                        }
                        else if (showoptions.Equals("Last Worked"))
                        {
                            sSQL += "and (m_LWD is not null and m_LWD<Now()) ";
                        }
                        else
                        {
                            sSQL += "and m_Status='" + showoptions + "' ";
                        }
                    }
                    if (!showalloption.Equals("showall"))
                    {
                        sSQL += "and (m_LWD is null or m_LWD>=Now()) ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) setStaffsResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //----------------------------------------------------------------
                    int iPageSize = 10;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    //  where m_Profile='grey' 
                    if (sort.Equals("m_StaffID")) sort = "staff." + sort;

                    //Testing Starts by CHC1704 Sivaguru M on 18-04-2024
                    if (sort.Equals("m_Grade")) sort = "m_Grade";
                    //Testing Ends

                    sSQL = "SELECT staff.*,summary.*,staff.m_id as staff_m_id FROM " + MyGlobal.activeDB + ".tbl_staffs staff " +
                        "left join " +
                        "(select * from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) +
                        "' group by m_StaffID) " +
                     "summary on summary.m_StaffID = staff.m_StaffID and summary.m_Profile = staff.m_Profile and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "' " +
                    "where " + sSearchKey + " and staff.m_Profile='" + profile + "' ";
                    if (search.Length == 0)
                    {
                        if (showoptions.Equals("All"))
                        {

                        }
                        else if (showoptions.Equals("Last Worked"))
                        {
                            sSQL += "and (m_LWD is not null and m_LWD<Now()) ";
                        }
                        else
                        {
                            sSQL += "and m_Status='" + showoptions + "' ";
                        }
                    } 
                    if (!showalloption.Equals("showall"))
                    {
                        sSQL += "and (m_LWD is null or m_LWD>=Now()) ";
                    }
                    //sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    //Start Sorting issue corrected by Sivaguru M CHC1704 at 24-02-2024
                    sSQL += "order by CASE WHEN m_FName = '_New' THEN 0 ELSE 1 END," + sort + ",staff.m_FName " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    //Ends testing

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    StaffItem staffItem = new StaffItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("staff_m_id"))) staffItem.m_id = reader.GetInt32(reader.GetOrdinal("staff_m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) staffItem.m_FName = reader["m_FName"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_MName"))) staffItem.m_Name += " " + reader["m_MName"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_LName"))) staffItem.m_Name += " " + reader["m_LName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) staffItem.m_StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Username"))) staffItem.m_Username = reader["m_Username"].ToString();// + "["+MyGlobal.activeDB+"]";
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) staffItem.m_Mobile = reader["m_Mobile"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) staffItem.m_Email = reader["m_Email"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) staffItem.m_Designation = reader["m_Designation"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Roll"))) staffItem.m_Roll = reader["m_Roll"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) staffItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) staffItem.m_Base = reader["m_Base"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) staffItem.m_Type = reader["m_Type"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToFunctional"))) staffItem.m_ReportToFunctional = reader["m_ReportToFunctional"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToAdministrative"))) staffItem.m_ReportToAdministrative = reader["m_ReportToAdministrative"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MenuKey"))) staffItem.m_MenuKey = reader["m_MenuKey"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) staffItem.m_Band = reader["m_Band"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) staffItem.m_Grade = reader["m_Grade"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mrs"))) staffItem.m_Mrs = reader["m_Mrs"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOB"))) staffItem.m_DOB = reader.GetDateTime(reader.GetOrdinal("m_DOB"));// reader["m_DOB"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOJ"))) staffItem.m_DOJ = reader.GetDateTime(reader.GetOrdinal("m_DOJ"));//.ToString("yyyy-MM-dd")
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOA"))) staffItem.m_DOA = reader.GetDateTime(reader.GetOrdinal("m_DOA"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_LWD")))
                                    {
                                        staffItem.m_LWD = reader.GetDateTime(reader.GetOrdinal("m_LWD"));
                                        staffItem.m_LWDExpired = DateTime.Compare(staffItem.m_LWD, DateTime.Now);
                                    }

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) staffItem.m_Status = reader["m_Status"].ToString();
                                    int iOrd = reader.GetOrdinal("m_ViewSelected");
                                    if (!reader.IsDBNull(iOrd)) staffItem.m_ViewSelected = reader.GetInt16(iOrd);

                                    iOrd = reader.GetOrdinal("m_Lock");
                                    if (!reader.IsDBNull(iOrd)) staffItem.m_Lock = reader.GetInt16(iOrd);
                                    //----------------------------------------
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ApprovedBy1"))) staffItem.m_ApprovedBy1 = reader["m_ApprovedBy1"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ApprovedBy2"))) staffItem.m_ApprovedBy2 = reader["m_ApprovedBy2"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ApprovedBy3"))) staffItem.m_ApprovedBy3 = reader["m_ApprovedBy3"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ApprovedBy4"))) staffItem.m_ApprovedBy4 = reader["m_ApprovedBy4"].ToString();
                                    /*
                                                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale")))
                                                                        {
                                                                            staffItem.m_Payscale = reader["m_Payscale"].ToString();
                                                                        }
                                                                        */
                                    Int32 key = 0;
                                    Int32 startdate = 0;
                                    //New User Creation error solved 27-05-2024 Starts 
                                    staffItem.m_PayscaleName = ((staffItem.m_StaffID != null && staffItem.m_StaffID != "") ? GetActivePayscale(profile, staffItem.m_StaffID, out key, out startdate) : null);
                                    //ends
                                    staffItem.m_PayscaleKey = key;
                                    staffItem.m_PayscaleStartDate = startdate;
                                    setStaffsResponse.items.Add(staffItem);
                                }
                                setStaffsResponse.status = true;
                                setStaffsResponse.result = "";
                            }
                            else
                            {
                                //setStaffsResponse.result = "<span style='color:red;'>Sorry!!! No Staffs</span>";
                                setStaffsResponse.result = "Sorry!!! No Staffs";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                //setStaffsResponse.result = "<span style='color:red;'>Error-" + ex.Message + "</span>";
                setStaffsResponse.result = "Error-" + ex.Message + "";
            }
            return Json(setStaffsResponse, JsonRequestBehavior.AllowGet);
        }

        //________________________________________________________________________
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
                        var path = Path.Combine(Server.MapPath("~/data/dvrphotos/"), fileName);
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
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json("Upload failed.[" + ex.Message + "]");
            }
            return Json("File uploaded successfully[" + sRet + "]");
        }
        //________________________________________________________________________
        [HttpPost]
        public async Task<JsonResult> UploadImage(string profile, string user, string type, string staffid)
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
                        var fileName = profile + "_" + type + "_" + staffid + ".jpg";
                        //var path = Path.Combine(Server.MapPath("~/OneDrive/TripManager/ProfileImages/"), fileName);
                        var path = Path.Combine(Server.MapPath("~/data/scans/"), fileName);
                        //var path = Path.Combine("c:/temp/photos/", fileName);
                        using (var fileStream = System.IO.File.Create(path))
                        {
                            stream.CopyTo(fileStream);
                            //___________________________Update DB
                            //sRet += InsertGalleryRecordIntoDB(imei, fileName);
                            //_______________Pass trigger
                            //var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                            //hubContext.Clients.All.broadcastMessage(imei, "{H}");
                            bool bOk = false;
                            string sSQL = "update " + MyGlobal.activeDB + ".tbl_staffs Set ";
                            if (type.Equals("aadhar"))
                            {
                                sSQL += "m_AADHAR_Uploaded='by " + user + " @" + DateTime.Now + "' ";
                                bOk = true;
                            }
                            else if (type.Equals("pan"))
                            {
                                sSQL += "m_PAN_Uploaded='by " + user + " @" + DateTime.Now + "' ";
                                bOk = true;
                            }
                            sSQL += "where m_Profile='" + profile + "' " +
                            "and m_StaffID='" + staffid + "';";

                            if (bOk)
                            {

                                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                                {
                                    con.Open();
                                    MySqlTransaction trans = con.BeginTransaction();
                                    MySqlCommand myCommand = con.CreateCommand();
                                    myCommand.Connection = con;
                                    myCommand.Transaction = trans;
                                    try
                                    {
                                        myCommand.CommandText = sSQL;
                                        myCommand.ExecuteNonQuery();

                                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
"(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
"('" + profile + "','" + user + "','" + "" + "','" + staffid + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + type + " Image uploaded')";
                                        myCommand.CommandText = sSQL;
                                        myCommand.ExecuteNonQuery();
                                        trans.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        trans.Rollback();
                                        return Json("Upload failed[" + e.Message + "]");
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json("Upload failed.[" + ex.Message + "]");
            }
            return Json("File uploaded successfully[" + sRet + "]");
        }
        //_________________________________________________________________
        private string GetOwnerMenuKey(MySqlConnection con, string profile, string email)
        {
            string sSQL = "select m_MenuKey from " + MyGlobal.activeDB + ".tbl_staffs where " +
                "m_Email='" + email + "' and m_Profile='" + profile + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) return reader.GetString(0);
                        }
                    }
                }
            }
            return "";
        }
        //-----------------------------
        [HttpPost]
        public ActionResult Update_Staffs(string profile, string email, string staffid, string mode, string m_id, string m_Name,
            string m_StaffID, string m_Country, string m_Mobile,
            string m_ReportToFunctional, string m_ReportToAdministrative,
            string m_Designation, string m_Type, string m_Roll, string m_Team,
            string m_Email, string m_Username, string m_Base, string m_Band, string m_Grade,
            string m_Mrs, string m_DOB, string m_DOJ, string m_DOA, string m_LWD, string m_Status,
            string m_Payscale, string m_Key, string m_AttendanceMethod,
            string m_Bank, string m_AccountNo, string m_Branch, string m_IFSC,
            string m_EPF_UAN, string m_ESICNumber, string m_AttendanceSource,
            string m_AADHAR_Number, string m_AADHAR_Name, string m_AADHAR_FatherName,
            string m_PAN_Number, string m_PAN_Name, string m_PAN_FatherName,
            string m_CCTNo, string m_CCTCleardDate, string m_RetentionBonusEffectiveDate, string m_RetentionBonusAmount)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            string sSQL = "";
            //----------------------------------------Trim
            if (email != null) email = email.Trim();
            if (m_Email != null) m_Email = m_Email.Trim();
            if (m_Username != null) m_Username = m_Username.Trim();
            if (m_StaffID != null) m_StaffID = m_StaffID.Trim();
            if (m_Mobile != null) m_Mobile = m_Mobile.Trim();
            if (staffid != null) staffid = staffid.Trim();
            if (m_Name != null) m_Name = m_Name.Trim();
            if (m_ReportToFunctional != null) m_ReportToFunctional = m_ReportToFunctional.Trim();
            if (m_ReportToAdministrative != null) m_ReportToAdministrative = m_ReportToAdministrative.Trim();
            if (m_Bank != null) m_Bank = m_Bank.Trim();
            if (m_AccountNo != null) m_AccountNo = m_AccountNo.Trim();
            if (m_Branch != null) m_Branch = m_Branch.Trim();
            if (m_IFSC != null) m_IFSC = m_IFSC.Trim();
            if (m_EPF_UAN != null) m_EPF_UAN = m_EPF_UAN.Trim();
            if (m_ESICNumber != null) m_ESICNumber = m_ESICNumber.Trim();
            if (m_AADHAR_Number != null) m_AADHAR_Number = m_AADHAR_Number.Trim();
            if (m_AADHAR_Name != null) m_AADHAR_Name = m_AADHAR_Name.Trim();
            if (m_AADHAR_FatherName != null) m_AADHAR_FatherName = m_AADHAR_FatherName.Trim();
            if (m_PAN_Number != null) m_PAN_Number = m_PAN_Number.Trim();
            if (m_PAN_Name != null) m_PAN_Name = m_PAN_Name.Trim();
            if (m_PAN_FatherName != null) m_PAN_FatherName = m_PAN_FatherName.Trim();
            //----------------------------------------Trim END

            if (staffid == null) staffid = "";
            if (mode.Equals("new") || mode.Equals("newstaff"))
            {
                int iDone = 0;
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_staffs where m_FName='_New' and m_Profile='" + profile + "';";
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
                            string sOwnerKey = GetOwnerMenuKey(con, profile, email);
                            if (mode.Equals("newstaff"))
                            {
                                if (MyGlobal.activeDB.Equals("dispatch"))
                                {
                                    //sOwnerKey = "a0-0,d0-0,m0-9,c0-1,g0-1,o0-1,u0-0,u1-0,x0-0,x1-0,s0-0,s1-9,s2-9,s3-0,s4-0,s5-0,t0-0,t1-0,f0-9,f1-9,f2-9,f3-9,f4-9,f5-9,r0-9,r1-9,l0-9,l1-0,l2-0,l3-0,l4-0,l5-0,w0-0,";
                                    sOwnerKey = sOwnerKey.Replace("a0-1", "a0-0");
                                    sOwnerKey = sOwnerKey.Replace("-2", "-0");
                                    sOwnerKey = sOwnerKey.Replace("-1", "-0");

                                    sOwnerKey = sOwnerKey.Replace("c0-0", "c0-1");
                                    sOwnerKey = sOwnerKey.Replace("g0-0", "g0-1");
                                    sOwnerKey = sOwnerKey.Replace("o0-0", "o0-1");
                                }
                                else if (MyGlobal.activeDB.Equals("meterbox"))
                                {
                                    sOwnerKey = sOwnerKey.Replace("a0-1", "a0-0");

                                    sOwnerKey = sOwnerKey.Replace("-2", "-0");
                                    sOwnerKey = sOwnerKey.Replace("-1", "-0");

                                    sOwnerKey = sOwnerKey.Replace("c0-0", "c0-1");
                                    sOwnerKey = sOwnerKey.Replace("g0-0", "g0-1");
                                    sOwnerKey = sOwnerKey.Replace("o0-0", "o0-1");
                                }
                                else
                                {

                                }
                            }
                            MySqlTransaction trans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = trans;
                            try
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_staffs (m_Profile,m_FName,m_Password,m_MenuKey,m_Status) values ('" + profile + "','_New','1234','" + sOwnerKey + "','Active');";
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
                                postResponse.result = "";
                            }
                            catch (Exception ex) //error occurred
                            {
                                trans.Rollback();
                                postResponse.result = "<span style='color:red;'>Error " + ex.Message + "</span>";
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    postResponse.result = "<span style='color:red;'>Error-" + ex.Message + "</span>";
                }
                if (iDone > 0)
                {
                    postResponse.status = true;
                    postResponse.result = "New entry with name '_New'";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
            }
            else if (mode.Equals("delete"))
            {
                //--------------Protected robin
                /*
                try
                {
                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        MySqlTransaction trans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = trans;
                        try
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' and m_id='" + m_id + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();

                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                            "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                            "('" + profile + "','" + staffid + "','" + email + "','" + m_StaffID + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + "Record created" + "')";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            //-------------------
                            trans.Commit();

                            postResponse.status = true;
                            postResponse.result = "<span style='color:blue;'>Staff deleted</span>";
                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                        }
                        catch (Exception ex) //error occurred
                        {
                            trans.Rollback();
                            postResponse.result = "Error " + ex.Message;
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    postResponse.result = "Error-" + ex.Message;
                }
                */
                postResponse.result = "Critical. Deletion Blocked.";
            }
            if (mode.Equals("new") || mode.Equals("newstaff"))
            {

            }
            else
            {
                if (m_id.Length == 0)
                {
                    postResponse.result = "<span style='color:red;'>Invalid request-" + sSQL + "</span>";
                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                }
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
                    if (mode.Equals("lock") || mode.Equals("unlock"))
                    {
                        string key = "";
                        if (mode.Equals("lock")) key = "1"; else key = "0";
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs Set " +
                        "m_Lock='" + key + "' where m_id=" + m_id;

                        MySqlTransaction trans1 = con.BeginTransaction();
                        MySqlCommand myCommand1 = con.CreateCommand();
                        myCommand1.Connection = con;
                        myCommand1.Transaction = trans1;
                        try
                        {
                            myCommand1.CommandText = sSQL;
                            myCommand1.ExecuteNonQuery();

                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                        "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                        "('" + profile + "','" + staffid + "','" + email + "','" + m_StaffID + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + (key.Equals("1") ? "Locked" : "Unlocked") + "')";
                            myCommand1.CommandText = sSQL;
                            myCommand1.ExecuteNonQuery();

                            trans1.Commit();
                            postResponse.result = "<span style='color:red;'>" + (key.Equals("1") ? "Locked" : "Unlocked") + "</span>";
                            postResponse.status = true;
                        }
                        catch (Exception e)
                        {
                            trans1.Rollback();
                            postResponse.result = "<span style='color:red;'>Lock failed -" + e.Message + "</span>";
                            postResponse.status = true;
                        }

                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    //-----------------------------------------------
                    string username = "null";
                    if (m_Username != null) if (m_Username.Length > 0) username = "'" + m_Username + "'";

                    string staffid_sql = "null";
                    if (m_StaffID != null) if (m_StaffID.Length > 0) staffid_sql = "'" + m_StaffID + "'";
                    if (string.IsNullOrEmpty(m_Mobile)) m_Mobile = "";
                    //string m_Mobile_src = m_Mobile;
                    //if (m_Mobile.Length < 6) m_Mobile = "null";
                    //else m_Mobile = "'" + m_Mobile + "'";

                    if (string.IsNullOrEmpty(m_Email)) m_Email = "";
                    string m_Email_src = m_Email;
                    if (m_Email.Length < 5) m_Email = "null";
                    else m_Email = "'" + m_Email + "'";

                    if (string.IsNullOrEmpty(m_DOB)) m_DOB = "";
                    if (string.IsNullOrEmpty(m_DOJ)) m_DOJ = "";
                    if (string.IsNullOrEmpty(m_DOA)) m_DOA = "";
                    if (string.IsNullOrEmpty(m_LWD)) m_LWD = "";

                    string m_DOB_src = m_DOB;
                    string m_DOJ_src = m_DOJ;
                    string m_DOA_src = m_DOA;
                    string m_LWD_src = m_LWD;
                    if (m_DOB.Length > 0) m_DOB = "'" + m_DOB + "'"; else m_DOB = "null";
                    if (m_DOJ.Length > 0) m_DOJ = "'" + m_DOJ + "'"; else m_DOJ = "null";
                    if (m_DOA.Length > 0) m_DOA = "'" + m_DOA + "'"; else m_DOA = "null";
                    if (m_LWD.Length > 0) m_LWD = "'" + m_LWD + "'"; else m_LWD = "null";

                    if (string.IsNullOrEmpty(m_ReportToFunctional)) m_ReportToFunctional = "";
                    if (string.IsNullOrEmpty(m_ReportToAdministrative)) m_ReportToAdministrative = "";

                    /*  
                     // Done on 24th June 2021 while trying to solve the issue of 
                     // status not getting updated. robin
                    if (m_ReportToFunctional.Length > 0)
                    {
                        if (!IsEmailValid(con, profile, m_ReportToFunctional, "Execution"))
                        {
                            postResponse.result = "<span style='color:red;'>Invalid 'Functional Reporting'</span>";
                            postResponse.status = true;
                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                    if (m_ReportToAdministrative.Length > 0)
                    {
                        if (!IsEmailValid(con, profile, m_ReportToAdministrative, "Execution"))
                        {
                            postResponse.result = "<span style='color:red;'>Invalid 'Administrative Reporting'</span>";
                            postResponse.status = true;
                            return Json(postResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                    */
                    //--------------Log the changes
                    string sFieldChanges = "";
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_id='" + m_id + "' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sFieldChanges += IsFieldChanged(reader, "m_FName", m_Name);
                                    sFieldChanges += IsFieldChanged(reader, "m_Designation", m_Designation);
                                    sFieldChanges += IsFieldChanged(reader, "m_Roll", m_Roll);
                                    sFieldChanges += IsFieldChanged(reader, "m_Team", m_Team);
                                    sFieldChanges += IsFieldChanged(reader, "m_Type", m_Type);
                                    sFieldChanges += IsFieldChanged(reader, "m_Base", m_Base);
                                    sFieldChanges += IsFieldChanged(reader, "m_Email", m_Email_src);
                                    sFieldChanges += IsFieldChanged(reader, "m_ReportToFunctional", m_ReportToFunctional);
                                    sFieldChanges += IsFieldChanged(reader, "m_ReportToAdministrative", m_ReportToAdministrative);
                                    sFieldChanges += IsFieldChanged(reader, "m_Band", m_Band);
                                    sFieldChanges += IsFieldChanged(reader, "m_Grade", m_Grade);
                                    sFieldChanges += IsFieldChanged(reader, "m_Mrs", m_Mrs);
                                    sFieldChanges += IsFieldChanged(reader, "m_Username", m_Username);
                                    sFieldChanges += IsFieldChanged(reader, "m_StaffID", m_StaffID);
                                    sFieldChanges += IsFieldChanged(reader, "m_Country", m_Country);
                                    sFieldChanges += IsFieldChanged_Dt(reader, "m_DOB", m_DOB_src);
                                    sFieldChanges += IsFieldChanged_Dt(reader, "m_DOJ", m_DOJ_src);
                                    sFieldChanges += IsFieldChanged_Dt(reader, "m_DOA", m_DOA_src);
                                    sFieldChanges += IsFieldChanged_Dt(reader, "m_LWD", m_LWD_src);
                                    sFieldChanges += IsFieldChanged(reader, "m_Status", m_Status);
                                    sFieldChanges += IsFieldChanged(reader, "m_Payscale", m_Payscale);
                                    sFieldChanges += IsFieldChanged(reader, "m_Key", m_Key);
                                    sFieldChanges += IsFieldChanged(reader, "m_Bank", m_Bank);
                                    sFieldChanges += IsFieldChanged(reader, "m_Branch", m_Branch);
                                    sFieldChanges += IsFieldChanged(reader, "m_AccountNo", m_AccountNo);
                                    sFieldChanges += IsFieldChanged(reader, "m_IFSC", m_IFSC);
                                    sFieldChanges += IsFieldChanged(reader, "m_EPF_UAN", m_EPF_UAN);
                                    sFieldChanges += IsFieldChanged(reader, "m_ESICNumber", m_ESICNumber);
                                    sFieldChanges += IsFieldChanged(reader, "m_AttendanceMethod", m_AttendanceMethod);
                                    sFieldChanges += IsFieldChanged(reader, "m_Mobile", m_Mobile);
                                    sFieldChanges += IsFieldChanged(reader, "m_AttendanceSource", m_AttendanceSource);

                                    sFieldChanges += IsFieldChanged(reader, "m_AADHAR_Number", m_AADHAR_Number);
                                    sFieldChanges += IsFieldChanged(reader, "m_AADHAR_Name", m_AADHAR_Name);
                                    sFieldChanges += IsFieldChanged(reader, "m_AADHAR_FatherName", m_AADHAR_FatherName);
                                    sFieldChanges += IsFieldChanged(reader, "m_PAN_Number", m_PAN_Number);
                                    sFieldChanges += IsFieldChanged(reader, "m_PAN_Name", m_PAN_Name);
                                    sFieldChanges += IsFieldChanged(reader, "m_PAN_FatherName", m_PAN_FatherName);

                                    sFieldChanges += IsFieldChanged(reader, "m_CCTNo", m_CCTNo);
                                    sFieldChanges += IsFieldChanged(reader, "m_CCTCleardDate", m_CCTCleardDate);
                                    sFieldChanges += IsFieldChanged(reader, "m_RetentionBonusEffectiveDate", m_RetentionBonusEffectiveDate);
                                    sFieldChanges += IsFieldChanged(reader, "m_RetentionBonusAmount", m_RetentionBonusAmount);
                                }
                            }
                        }
                    }
                    MySqlTransaction trans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = trans;
                    try
                    {
                        if (sFieldChanges.Length > 0)
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                            "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                            "('" + profile + "','" + staffid + "','" + email + "','" + m_StaffID + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + sFieldChanges + "')";

                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                        }
                        //--------------Log the changes END
                        if (!string.IsNullOrEmpty(m_id))
                        {
                            string s_m_CCTNo = "null";
                            string s_m_CCTCleardDate = "null";
                            string s_m_RetentionBonusEffectiveDate = "null";
                            string s_m_RetentionBonusAmount = "null";

                            if (m_CCTCleardDate != null && m_CCTCleardDate.Length > 0)
                            {
                                DateTime dt;
                                if (DateTime.TryParseExact(m_CCTCleardDate,
                                                       "dd-MM-yyyy",
                                                       CultureInfo.InvariantCulture,
                                                       DateTimeStyles.None,
                                                       out dt))
                                {
                                    s_m_CCTCleardDate = "'" + dt.ToString("yyyy-MM-dd") + "'";
                                }
                                else
                                {
                                    trans.Rollback();
                                    postResponse.result = "<span style='color:red;'>Invalid CCT Cleard Date</span>";
                                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                                }
                            }
                            if (m_RetentionBonusEffectiveDate != null && m_RetentionBonusEffectiveDate.Length > 0)
                            {
                                DateTime dt;
                                if (DateTime.TryParseExact(m_RetentionBonusEffectiveDate,
                                                   "dd-MM-yyyy",
                                                   CultureInfo.InvariantCulture,
                                                   DateTimeStyles.None,
                                                   out dt))
                                {
                                    s_m_RetentionBonusEffectiveDate = "'" + dt.ToString("yyyy-MM-dd") + "'";
                                }
                                else
                                {
                                    trans.Rollback();
                                    postResponse.result = "<span style='color:red;'>Invalid Retention Bonus Effective Date</span>";
                                    return Json(postResponse, JsonRequestBehavior.AllowGet);
                                }
                            }
                            if (m_CCTNo != null && m_CCTNo.Length > 0) s_m_CCTNo = "'" + m_CCTNo + "'";
                            if (m_RetentionBonusAmount != null)
                            {
                                double dblVal = 0;
                                if (double.TryParse(m_RetentionBonusAmount, out dblVal))
                                {
                                    s_m_RetentionBonusAmount = "'" + dblVal + "'";
                                }
                            }

                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_staffs Set " +
                                "m_FName='" + (m_Name ?? "").Trim() + "'," +
                                "m_Designation='" + (m_Designation ?? "") + "'," +
                                "m_Roll='" + (m_Roll ?? "") + "'," +
                                "m_Team='" + (m_Team ?? "") + "'," +
                                "m_Type='" + (m_Type ?? "") + "'," +
                                "m_Base='" + (m_Base ?? "") + "'," +
                                "m_Email=" + m_Email.Trim() + "," +
                                "m_ReportToFunctional='" + (m_ReportToFunctional ?? "") + "'," +
                                "m_ReportToAdministrative='" + (m_ReportToAdministrative ?? "") + "'," +
                                "m_Band='" + (m_Band ?? "") + "'," +
                                "m_Grade='" + (m_Grade ?? "") + "'," +
                                "m_Mrs='" + (m_Mrs ?? "") + "'," +
                                "m_Username=" + (username ?? "").Trim() + "," +
                                "m_StaffID=" + (staffid_sql ?? "").Trim() + "," +
                                "m_Country='" + (m_Country ?? "") + "'," +
                                "m_DOB=" + m_DOB + "," +
                                "m_DOJ=" + m_DOJ + "," +
                                "m_DOA=" + m_DOA + "," +
                                "m_LWD=" + m_LWD + "," +
                                "m_Status='" + (m_Status ?? "") + "'," +
                                "m_Payscale='" + (m_Payscale ?? "") + "'," +
                                "m_Key='" + (m_Key ?? "") + "'," +
                                "m_Bank='" + (m_Bank ?? "") + "'," +
                                "m_Branch='" + (m_Branch ?? "") + "'," +
                                "m_AccountNo='" + (m_AccountNo ?? "") + "'," +
                                "m_IFSC='" + (m_IFSC ?? "") + "'," +
                                "m_EPF_UAN='" + (m_EPF_UAN ?? "") + "'," +
                                "m_ESICNumber='" + (m_ESICNumber ?? "") + "'," +
                                "m_AttendanceMethod='" + (m_AttendanceMethod ?? "") + "'," +
                                "m_AttendanceSource='" + (m_AttendanceSource ?? "") + "'," +
                                "m_AADHAR_Number='" + (m_AADHAR_Number ?? "") + "'," +
                                "m_AADHAR_Name='" + (m_AADHAR_Name ?? "") + "'," +
                                "m_AADHAR_FatherName='" + (m_AADHAR_FatherName ?? "") + "'," +
                                "m_PAN_Number='" + (m_PAN_Number ?? "") + "'," +
                                "m_PAN_Name='" + (m_PAN_Name ?? "") + "'," +
                                "m_PAN_FatherName='" + (m_PAN_FatherName ?? "") + "'," +
                                "m_Mobile='" + (m_Mobile ?? "").Trim() + "'," +
                                "m_CCTNo=" + s_m_CCTNo + "," +
                                "m_CCTCleardDate=" + s_m_CCTCleardDate + "," +
                                "m_RetentionBonusEffectiveDate=" + s_m_RetentionBonusEffectiveDate + "," +
                                "m_RetentionBonusAmount=" + s_m_RetentionBonusAmount + " " +
                                "where m_id=" + m_id;

                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                        }

                        trans.Commit();

                        postResponse.status = true;
                        postResponse.result = "";
                    }
                    catch (Exception ex) //error occurred
                    {
                        trans.Rollback();
                        postResponse.result = "<span style='color:red;'>Error " + ex.Message + "</span>";
                        //Handel error
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "<span style='color:red;'>Error-" + ex.Message + "[" + MyGlobal.activeDB + "]</span>";
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        private string IsFieldChanged(MySqlDataReader reader, string sField, string sNewValue)
        {
            string sValue = reader.IsDBNull(reader.GetOrdinal(sField)) ? "" : reader.GetString(reader.GetOrdinal(sField));
            if (string.IsNullOrEmpty(sNewValue)) sNewValue = "";
            if (!sValue.Equals(sNewValue))
            {
                return "[" + sField + " : " + sValue + " -> " + sNewValue + "] ";
            }
            return "";
        }
        private string IsFieldChanged_Dt(MySqlDataReader reader, string sField, string sNewValue)
        {
            string sValue = reader.IsDBNull(reader.GetOrdinal(sField)) ? "" : reader.GetString(reader.GetOrdinal(sField));
            if (string.IsNullOrEmpty(sNewValue))
            {
                sNewValue = "";
            }
            else
            {
                char[] delimiterChars = { '-' };
                string[] arData = sNewValue.Split(delimiterChars);
                if (arData.Length == 3)
                {
                    sNewValue = arData[2] + "-" + arData[1] + "-" + arData[0] + " 00:00:00";
                }
            }
            if (!sValue.Equals(sNewValue))
            {
                return "[" + sField + " : " + sValue + " -> " + sNewValue + "] ";
            }
            return "";
        }
        private bool IsEmailValid(MySqlConnection con, string profile, string email, string sExceptBand)
        {
            string sSQL = "select m_Email from " + MyGlobal.activeDB + ".tbl_staffs " +
                "where m_Profile='" + profile + "' and m_Email='" + email + "' ";
            if (sExceptBand.Length > 0) sSQL += "and m_Band is not null and m_Band<>'' and m_Band<>'" + sExceptBand + "' ";
            sSQL += "limit 1;";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }
        [HttpPost]
        public ActionResult GetDesktop_New(string profile, string timezone)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var myAccessDash = new MyAccessDash();
            myAccessDash.status = false;
            myAccessDash.result = "";

            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            myAccessDash.terminals_online = hub.GetAccessManagerTerminalsOnline();

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------------------Get List
                    //-----------------------------------------------------------------------------------------------------
                    long spanFrom = 0, spanTo = 0;
                    spanFrom = MyGlobal.ToEpochTime(DateTime.Today) - 28800 - 3600;
                    spanTo = MyGlobal.ToEpochTime(DateTime.Today) + 86400;
                    int dtYear = DateTime.Today.Year;
                    int dtMonth = DateTime.Today.Month - 1; // to help zero indexed SQL
                    int dtDay = DateTime.Today.Day;


                    sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime," +
                        "count(DISTINCT activities.m_StaffID),count(DISTINCT rosters.m_StaffID) " +

"FROM " + MyGlobal.activeDB + ".tbl_rosters rosters " +

"left join " +
"(select m_Profile, m_StaffID, m_ActivityTime from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
"where m_ActivityTime>= (" + spanFrom + " - 19800) and m_ActivityTime<(" + spanTo + " - 19800) " +
"and m_StaffID is not null and m_StaffID<>'' " +
") activities on rosters.m_Profile = activities.m_Profile " +
"and activities.m_StaffID = rosters.m_StaffID " +
"and activities.m_ActivityTime >= (unix_timestamp(concat('" + dtYear + "', '-', '" + (dtMonth + 1) + "', '-', '" + dtDay + "')) + rosters.m_ShiftStartTime - 19800 - 3600) " +
"and activities.m_ActivityTime < (unix_timestamp(concat('" + dtYear + "', '-', '" + (dtMonth + 1) + "', '-', '" + dtDay + "')) + rosters.m_ShiftEndTime - 19800 + 3600) " +
"and activities.m_StaffID in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_RosterName = rosters.m_RosterName and m_ShiftName = rosters.m_ShiftName " +
"and m_Year = '" + dtYear + "' and m_Month = '" + dtMonth + "' and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' and m_StaffID is not null) " +

"where rosters.m_Profile = 'support@sharewaredreams.com' and m_ShiftName is not null " +
"and m_Year = '" + dtYear + "' and m_Month = '" + dtMonth + "' " +
"and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' " +
"group by m_RosterName,m_ShiftName " +
"order by m_RosterName asc,m_ShiftStartTime";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    RosterStat rosterStat = new RosterStat();
                                    rosterStat.Name = reader.GetString(0);      //m_RosterName
                                    rosterStat.Arrived = reader.GetInt16(4);    //m_StaffsA
                                    rosterStat.Expected = reader.GetInt16(5);   //m_StaffsE

                                    myAccessDash.rosterStats.Add(rosterStat);
                                    /*
                                    DisplayedColumns_Roster_Consolidated_Row row =
                                        new DisplayedColumns_Roster_Consolidated_Row();
                                    if (!reader.IsDBNull(0)) row.m_RosterName = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) row.m_ShiftName = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) row.shift_start = unixDayStart + reader.GetInt32(2);
                                    if (!reader.IsDBNull(3)) row.shift_end = unixDayStart + reader.GetInt32(3);
                                    if (!reader.IsDBNull(4)) row.m_StaffsA = reader.GetInt16(4);
                                    if (!reader.IsDBNull(5)) row.m_StaffsE = reader.GetInt16(5);
                                    hrActivitiesResponse.rows.Add(row);
                                    */
                                }

                            }
                        }
                    }
                    //----------------------------Get Staff Count
                    sSQL = "SELECT count(m_id) as cnt " +
"FROM " + MyGlobal.activeDB + ".tbl_staffs " +
"where m_Profile='" + profile + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) myAccessDash.users_active = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //----------------------------Get Staff Count END
                    myAccessDash.status = true;
                    myAccessDash.result = "Done";
                }
            }
            catch (MySqlException ex)
            {
                myAccessDash.result = "Error-" + ex.Message;
            }
            myAccessDash.status = true;
            return Json(myAccessDash, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetDesktop(string profile, string timezone)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var myAccessDash = new MyAccessDash();
            myAccessDash.status = false;
            myAccessDash.result = "";
            DateTime tme = DateTime.Now;
            int iYear = tme.Year;
            int iMonth = tme.Month - 1;
            int iDay = tme.Day;

            tme = DateTime.Now.AddDays(-1);
            int iYearY = tme.Year;
            int iMonthY = tme.Month - 1;
            int iDayY = tme.Day;

            /*  // Live SignalR connections. The code below is good working code. Removed as this info was taken from DB
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            myAccessDash.terminals_online = hub.GetAccessManagerTerminalsOnline();
            */

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------------------------------------------Get staffs online
                    //count(distinct m_StaffID) as staffs,count(distinct m_HardwareID) as terminals 
                    sSQL = @"SELECT " +
"sum(Case When m_StaffID <> '' and m_StaffID is not null Then 1 Else 0 End) as staffs," +
"sum(Case When m_HardwareID <> '' and m_HardwareID is not null Then 1 Else 0 End) as terminals " +
"FROM " + MyGlobal.activeDB + ".tbl_terminals " +
"where m_ActivityTime> (UNIX_TIMESTAMP() - 240) and m_ActivityTime<(UNIX_TIMESTAMP() + 240) " +
"and m_Profile= '" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) myAccessDash.staffs_online = reader.GetInt16(0);
                                    if (!reader.IsDBNull(1)) myAccessDash.terminals_online = reader.GetInt16(1);
                                }
                            }
                        }
                    }
                    //----------------------------------------------------Gather Physically present RosterMarker
                    string sSQLRosterOptions = "", sSQLRosterOptionsY = "";
                    sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                        "where m_PhysicalPresence=1 and m_Profile = '" + profile + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string sMarker = MyGlobal.GetPureString(reader, "m_Name");
                                    if (sMarker.Length > 0)
                                    {
                                        if (sSQLRosterOptions.Length > 0)
                                        {
                                            sSQLRosterOptions += " or ";
                                            sSQLRosterOptionsY += " or ";
                                        }
                                        sSQLRosterOptionsY += "roster.m_Day" + iDayY + " = '" + sMarker + "' ";
                                        sSQLRosterOptions += "roster.m_Day" + iDay + " = '" + sMarker + "' ";
                                    }
                                }
                            }
                            if (sSQLRosterOptions.Length > 0)
                            {
                                sSQLRosterOptionsY = " and (" + sSQLRosterOptionsY + ")";
                                sSQLRosterOptions = " and (" + sSQLRosterOptions + ")";
                            }
                        }
                    }

                    //----------------------------------------------------
                    long spanFrom = 0, spanTo = 0;
                    spanFrom = MyGlobal.ToEpochTime(DateTime.Today) - 28800 - 3600;
                    spanTo = MyGlobal.ToEpochTime(DateTime.Today) + 86400;
                    string sActivityTable = "(select * " +
                        "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                        "where m_ActivityTime>= (" + spanFrom + ") and m_ActivityTime<(" + spanTo + ") " +
                        "and m_StaffID is not null and m_StaffID<>'' and m_Profile = '" + profile + "')";
                    //--------------------------------PREVIOUSDATE Start
                    sSQL = "SELECT roster.m_RosterName,sum(m_WorkTime)," +
                        "(terminal.m_ActivityTime - terminal.m_ActivityStart) as runningTime, " +
                        "roster.m_ShiftStartTime,roster.m_ShiftEndTime,roster.m_ShiftName " +
                           "FROM " + MyGlobal.activeDB + ".tbl_rosters roster ";

                    sSQL += "left join " + sActivityTable + " as activity on activity.m_StaffID = roster.m_StaffID " +
                           "and m_ActivityTime>= (unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY)) + roster.m_ShiftStartTime) " +
                           "and m_ActivityTime<(unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY))+roster.m_ShiftEndTime) " +
                           "and (m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'update' or m_Activity = 'approved') ";

                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_terminals as terminal on terminal.m_StaffID = roster.m_StaffID " +
                           "and terminal.m_ActivityTime >= (unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY)) + roster.m_ShiftStartTime) " +
                           "and terminal.m_ActivityTime < (unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY)) + roster.m_ShiftEndTime) " +
                           "and terminal.m_Activity = 'open' and terminal.m_ActivityTime is not null and terminal.m_ActivityStart is not null ";

                    sSQL += "where roster.m_StaffID is not null and " +
                        "roster.m_Year = '" + iYearY + "' and roster.m_Month = '" + iMonthY + "' " +
                        "and (roster.m_Day" + iDayY + " is not null and length(m_Day" + iDayY + ")>0 " + sSQLRosterOptionsY + ") " + //and roster.m_Day" + iDayY + " = '" + MyGlobal.WORKDAY_MARKER + "'
                        "and unix_timestamp()>= (unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY)) + roster.m_ShiftStartTime) " +
                        "and unix_timestamp()<(unix_timestamp(DATE_ADD(CURDATE(), INTERVAL -1 DAY))+roster.m_ShiftEndTime) " +
                        "and roster.m_Profile= '" + profile + "' " +
                        "group by roster.m_RosterName,roster.m_ShiftName,roster.m_StaffID";

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
                                        int worktime = 0;
                                        //---Worktime from tbl_accessmanager_activity
                                        if (!reader.IsDBNull(1)) worktime = reader.GetInt32(1);
                                        //---Worktime from tbl_terminals
                                        if (!reader.IsDBNull(2)) worktime += reader.GetInt32(2);
                                        if (!GetAndSetField(
                                                myAccessDash.rosterStats,
                                                reader.GetString(0) + "_" + reader.GetString(5),
                                                worktime))
                                        {
                                            if (!reader.IsDBNull(0) && !reader.IsDBNull(5))
                                            {
                                                RosterStat rosterStat = new RosterStat();
                                                rosterStat.Name = reader.GetString(0) + "_" + reader.GetString(5);
                                                rosterStat.PreShift = const_ShiftPaddingPre;
                                                rosterStat.PostShift = const_ShiftPaddingPost;
                                                if (!reader.IsDBNull(3)) rosterStat.ShiftStart = reader.GetInt32(3);
                                                if (!reader.IsDBNull(4)) rosterStat.ShiftEnd = reader.GetInt32(4);
                                                rosterStat.m_WorkTime = worktime;
                                                rosterStat.Expected = 1;
                                                if (rosterStat.m_WorkTime > 0) rosterStat.Arrived++;
                                                myAccessDash.rosterStats.Add(rosterStat);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                    //--------------------------------CURDATE Start
                    sSQL = "SELECT roster.m_RosterName,sum(m_WorkTime)," +
                        "(terminal.m_ActivityTime - terminal.m_ActivityStart) as runningTime, " +
                        "roster.m_ShiftStartTime,roster.m_ShiftEndTime,roster.m_ShiftName " +
                           "FROM " + MyGlobal.activeDB + ".tbl_rosters roster ";

                    sSQL += "left join " + sActivityTable + " as activity on activity.m_StaffID = roster.m_StaffID " +
                           "and m_ActivityTime>= (unix_timestamp(CURDATE()) + roster.m_ShiftStartTime) " +
                           "and m_ActivityTime<(unix_timestamp(CURDATE())+roster.m_ShiftEndTime) " +
                           "and (m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'update' or m_Activity = 'approved') ";

                    sSQL += "left join " + MyGlobal.activeDB + ".tbl_terminals as terminal on terminal.m_StaffID = roster.m_StaffID " +
                           "and terminal.m_ActivityTime >= (unix_timestamp(CURDATE()) + roster.m_ShiftStartTime) " +
                           "and terminal.m_ActivityTime < (unix_timestamp(CURDATE()) + roster.m_ShiftEndTime) " +
                           "and terminal.m_Activity = 'open' and terminal.m_ActivityTime is not null and terminal.m_ActivityStart is not null ";

                    sSQL += "where roster.m_StaffID is not null and " +
                        "roster.m_Year = '" + iYear + "' and roster.m_Month = '" + iMonth + "' " +
                        "and (roster.m_Day" + iDay + " is not null and length(m_Day" + iDay + ")>0 " + sSQLRosterOptions + ") " + //roster.m_Day" + iDay + " = '" + MyGlobal.WORKDAY_MARKER + "'
                        "and unix_timestamp()>= (unix_timestamp(CURDATE()) + roster.m_ShiftStartTime) " +
                        "and unix_timestamp()<(unix_timestamp(CURDATE())+roster.m_ShiftEndTime) " +
                        "and roster.m_Profile= '" + profile + "' " +
                        "group by roster.m_RosterName,roster.m_ShiftName,roster.m_StaffID";

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
                                        int worktime = 0;
                                        //---Worktime from tbl_accessmanager_activity
                                        if (!reader.IsDBNull(1)) worktime = reader.GetInt32(1);
                                        //---Worktime from tbl_terminals
                                        if (!reader.IsDBNull(2)) worktime += reader.GetInt32(2);
                                        if (!GetAndSetField(
                                                myAccessDash.rosterStats,
                                                reader.GetString(0) + "_" + reader.GetString(5),
                                                worktime))
                                        {
                                            if (!reader.IsDBNull(0) && !reader.IsDBNull(5))
                                            {
                                                RosterStat rosterStat = new RosterStat();
                                                rosterStat.Name = reader.GetString(0) + "_" + reader.GetString(5);
                                                rosterStat.PreShift = const_ShiftPaddingPre;
                                                rosterStat.PostShift = const_ShiftPaddingPost;
                                                if (!reader.IsDBNull(3)) rosterStat.ShiftStart = reader.GetInt32(3);
                                                if (!reader.IsDBNull(4)) rosterStat.ShiftEnd = reader.GetInt32(4);
                                                rosterStat.m_WorkTime = worktime;
                                                rosterStat.Expected = 1;
                                                if (rosterStat.m_WorkTime > 0) rosterStat.Arrived++;
                                                myAccessDash.rosterStats.Add(rosterStat);
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                    //----------------------------Get Staff Count
                    sSQL = "SELECT count(m_id) as cnt " +
"FROM " + MyGlobal.activeDB + ".tbl_staffs " +
"where m_Profile='" + profile + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) myAccessDash.users_active = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //----------------------------Get Staff Count END
                    myAccessDash.status = true;
                    myAccessDash.result = "Done";
                }
            }
            catch (MySqlException ex)
            {
                myAccessDash.result = "Error-" + ex.Message;
                MyGlobal.Error("GetDesktop-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetDesktop-Exception-" + ex.Message);
            }
            myAccessDash.status = true;
            return Json(myAccessDash, JsonRequestBehavior.AllowGet);
        }
        bool GetAndSetField(List<RosterStat> list, string Name, int worktime)
        {
            var tempField = list.FirstOrDefault(x => x.Name.Equals(Name));
            if (tempField != null)
            {
                tempField.m_WorkTime += worktime;
                tempField.Expected++;
                if (worktime > 0) tempField.Arrived++;
                return true;
            }
            return false;
        }
        //_________________________________________________Upload profile photo END

        [HttpPost]
        public ActionResult LoadRosters(string profile, string loginemail, string loginstaffid,
            string roster, string shift, string mode,
            string pop_input, string pop_starttime, string pop_endtime,
            string year, string month,
            string staffname, string staffid, string production)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadRostersResponse = new OnLoadRostersResponse();
            onLoadRostersResponse.status = true;
            onLoadRostersResponse.result = "";
            onLoadRostersResponse.selectedRoster = roster;
            int iMonth = MyGlobal.GetInt16(month);
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //______________________________Crate Roster Name
                    if (mode.Equals("newroster"))
                    {
                        bool bAlreadyExists = false;
                        sSQL = @"SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_Profile = '" + profile + "' and m_RosterName='" + pop_input + "' " +
"and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' and m_ShiftName is null;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bAlreadyExists = reader.HasRows;
                                if (bAlreadyExists)
                                {
                                    onLoadRostersResponse.result = "Roster name [" + pop_input + "] already exists";

                                    onLoadRostersResponse.status = false;
                                }
                            }
                        }
                        if (!bAlreadyExists)
                        {
                            sSQL = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters (m_Profile," +
"m_RosterName,m_Year,m_Month) values ('" + profile + "','" + pop_input + "','" +
year + "','" + (iMonth - 1) + "');";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("removeroster"))
                    {
                        bool bDataAlreadyExists = false;
                        sSQL = @"SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_Profile = '" + profile + "' and m_RosterName='" + roster + "' " +
"and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' " +
"and (m_StaffName is not null or m_StaffID is not null);";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bDataAlreadyExists = reader.HasRows;
                                if (bDataAlreadyExists)
                                {
                                    onLoadRostersResponse.result = "Roster [" + roster + "] already has valid entries. Can't remove";
                                    onLoadRostersResponse.status = false;
                                }
                            }
                        }
                        if (!bDataAlreadyExists)
                        {
                            sSQL = @"DELETE FROM " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_Profile = '" + profile + "' and m_RosterName='" + roster + "' " +
"and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "';";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("newshift"))
                    {
                        bool bAlreadyExists = false;
                        sSQL = @"SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_Profile = '" + profile + "' and m_RosterName='" + roster + "' " +
"and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' and m_ShiftName ='" + pop_input + "' " +
"and m_StaffName is null and m_StaffID is null;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bAlreadyExists = reader.HasRows;
                                if (bAlreadyExists)
                                {
                                    onLoadRostersResponse.result = "Shift name [" + pop_input + "] already exists in roster [" + roster + "]";
                                    onLoadRostersResponse.status = false;
                                }
                            }
                        }
                        if (!bAlreadyExists)
                        {
                            double starttime = TimeSpan.Parse(pop_starttime).TotalSeconds;
                            double endtime = TimeSpan.Parse(pop_endtime).TotalSeconds;
                            if (endtime < starttime) endtime += 86400;

                            sSQL = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters (m_Profile," +
"m_RosterName,m_Year,m_Month,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime) values ('" +
profile + "','" + roster + "','" +
year + "','" + (iMonth - 1) + "','" + pop_input + "'," + starttime + "," + endtime + ");";

                            //year + "','" + month + "','" + pop_input + "',TIME_TO_SEC('" + pop_starttime + "'),TIME_TO_SEC('" + pop_endtime + "'));";

                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("importnames"))
                    {
                        String sUpdateString = "";
                        int iCounts = 0;

                        sSQL = "SELECT m_Name,m_Head FROM " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile = '" + profile + "' " +
                            "and m_Name not in (select m_RosterName from " + MyGlobal.activeDB + ".tbl_rosters where m_Profile = '" + profile + "' and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' group by m_RosterName)";
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
                                            string sName = "", sHead = "";
                                            if (!reader.IsDBNull(0)) sName = reader.GetString(0);
                                            if (!reader.IsDBNull(1)) sHead = reader.GetString(1);
                                            if (sName.Length > 0)
                                            {
                                                sUpdateString += @"INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters (m_Profile,m_Head," +
"m_RosterName,m_Year,m_Month) values ('" + profile + "','" + sHead + "','" + sName + "','" +
                                                    year + "','" + (iMonth - 1) + "');";
                                                iCounts++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sUpdateString.Length > 0)
                        {

                            using (MySqlCommand com = new MySqlCommand(sUpdateString, con))
                            {
                                com.ExecuteNonQuery();
                                onLoadRostersResponse.result = iCounts + " Team names are imported for " + constArrayMonths[(iMonth)] + "," + year;

                            }
                        }
                        else
                        {
                            onLoadRostersResponse.result = "No Teams to Import for " + constArrayMonths[(iMonth - 1)] + "," + year + ". [Create Teams in Master Tables and set this to staffs]";
                        }
                    }
                    else if (mode.Equals("newhead"))
                    {
                        bool bUsernameExists = false;
                        sSQL = "SELECT m_Username FROM " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' " +
                            "and m_Username is not null and m_Username='" + pop_input + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bUsernameExists = reader.HasRows;
                            }
                        }
                        if (bUsernameExists)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set m_Head='" + pop_input + "' " +
                                "where m_Profile = '" + profile + "' and m_RosterName='" + roster + "' " +
                            "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' and m_StaffID is null " +
                            "and m_ShiftName is null;";
                            if (pop_input.Length == 0)
                            {
                                onLoadRostersResponse.result = "Head name is empty";
                            }
                            else if (roster.Length == 0)
                            {
                                onLoadRostersResponse.result = "Roster Name is Empty";
                            }
                            else
                            {
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                    onLoadRostersResponse.result = "Head name updated";
                                }
                            }
                        }
                        else
                        {
                            onLoadRostersResponse.result = "Invalid Username";
                        }
                    }
                    //--------------------------------------------
                    /*
                    sSQL = "select m_MenuKey from " + MyGlobal.activeDB + ".tbl_staffs where " +
                    "m_StaffID='" + loginstaffid + "' and m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) onLoadRostersResponse.per_edit = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    */
                    //----------------------------Get permission for this user
                    sSQL = "select m_Head,m_State from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                    "where m_Profile='" + profile + "' and  m_StaffID = '" + loginstaffid + "' " +
                    "and m_Team = '" + roster + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                    {
                                        if (reader.GetString(0).Equals("attendance")) onLoadRostersResponse.per_attendance = reader.GetInt16(1);
                                        if (reader.GetString(0).Equals("production")) onLoadRostersResponse.per_production = reader.GetInt16(1);
                                        if (reader.GetString(0).Equals("roster")) onLoadRostersResponse.per_roster = reader.GetInt16(1);
                                    }
                                }
                            }
                        }
                    }
                    if (roster.Length > 0)
                    {
                        if (profile.Equals(loginemail, StringComparison.CurrentCultureIgnoreCase))
                        {
                            onLoadRostersResponse.per_attendance = 2;
                            onLoadRostersResponse.per_production = 2;
                            onLoadRostersResponse.per_roster = 2;
                        }
                    }
                    //--------------------------------------------
                    string permission = "select m_Team from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                        "where m_Profile = '" + profile + "' and m_StaffID = '" + loginstaffid + "' ";
                    if (production.Equals("1"))
                    {
                        permission += "and m_Head='production'";
                    }
                    else
                    {
                        permission += "and m_Head='roster'";
                    }

                    sSQL = @"SELECT m_RosterName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile = '" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' ";
                    if (!profile.Equals(loginemail, StringComparison.CurrentCultureIgnoreCase))
                    {
                        sSQL += "and m_RosterName in (" + permission + ") ";
                    }
                    sSQL += "group by m_RosterName order by m_RosterName;";

                    if (!MyGlobal.activeDomain.Equals("chchealthcare")) onLoadRostersResponse.sarRosters.Add("All");

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
                                        onLoadRostersResponse.sarRosters.Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                    //____________________________________Get Shifts
                    if (roster != null && roster.Length > 0)
                    {
                        sSQL = @"SELECT m_ShiftName,m_ShiftStartTime,
m_ShiftEndTime,m_Head FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' " +
"and m_RosterName='" + roster + "' and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' " +
"group by m_ShiftName order by m_ShiftStartTime;";
                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                            {
                                if (reader1.HasRows)
                                {
                                    while (reader1.Read())
                                    {
                                        if (reader1.IsDBNull(0))
                                        {
                                            if (!reader1.IsDBNull(3)) onLoadRostersResponse.AdminHead = reader1.GetString(3);
                                        }
                                        else
                                        {
                                            MyShift myShift = new MyShift();
                                            myShift.m_Name = reader1.GetString(0);
                                            if (!reader1.IsDBNull(1)) myShift.m_StartTime = reader1.GetInt32(1);
                                            if (!reader1.IsDBNull(2)) myShift.m_EndTime = reader1.GetInt32(2);
                                            onLoadRostersResponse.oMyShifts.Add(myShift);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //onLoadRostersResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                onLoadRostersResponse.status = false;
                onLoadRostersResponse.result = ex.Message;
            }

            return Json(onLoadRostersResponse, JsonRequestBehavior.AllowGet);
        }
        private long GetActiveWorkingTimeForThisStaffThisday(string profile, string roster, string shift, string year, string month, int day, string staffid, long lShiftStart, long lShiftEnd)
        {
            long lReturn = 0;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                    double epochDayStart = ((new TimeSpan(new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1, day).Ticks)) - epochTicks).TotalSeconds - 19800;
                    double epochShiftStart = epochDayStart + lShiftStart;
                    double epochShiftEnd = epochDayStart + lShiftEnd;
                    //---------------------------------------------------------
                    sSQL = "select sum(m_WorkTime) as tot from " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
                        "m_Profile='" + profile + "' " +
                        "and m_ActivityTime>='" + epochShiftStart + "' " +
                        "and m_ActivityTime<='" + epochShiftEnd + "' " +
                        "and m_StaffID='" + staffid + "' and (m_Activity = 'update' or m_Activity='lock' or m_Activity='forcedlock' or m_Activity='approved');";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) lReturn = reader.GetInt64(0);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {

            }
            return lReturn;
        }
        private long GetActiveWorkingTimeForThisStaffThisShift(string profile, string roster, string shift, string year, string month, int day, string staffid, long lShiftStart, long lShiftEnd, int mode, double epochDayStart)
        {
            long lReturn = 0;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    double epochShiftStart = epochDayStart + lShiftStart;
                    double epochShiftEnd = epochDayStart + lShiftEnd;
                    double lStart = 0, lEnd = 0;
                    if (mode == -1)
                    {
                        lStart = epochShiftStart - const_ShiftPaddingPre;
                        lEnd = epochShiftStart;
                    }
                    else if (mode == 1)
                    {
                        lStart = epochShiftEnd;
                        lEnd = epochShiftEnd + const_ShiftPaddingPost;
                    }
                    else
                    {
                        lStart = epochShiftStart;
                        lEnd = epochShiftEnd;
                    }
                    if (lEnd < lStart) lEnd += 86400;

                    //---------------------------------------------------------
                    sSQL = "select sum(m_WorkTime) as tot from " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
                    "m_Profile='" + profile + "' " +
                    "and m_ActivityTime>='" + lStart + "' " +
                    "and m_ActivityTime<'" + lEnd + "' " +
                    "and m_StaffID='" + staffid + "' and (m_Activity = 'update' or m_Activity='lock' or m_Activity='forcedlock' or m_Activity='approved');";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) lReturn = reader.GetInt64(0);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {

            }
            return lReturn;
        }
        private void ProcessWorkingHours(MySqlConnection con, string year, string month, string staffid,
            string profile, string roster, string shift)
        {
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month) + 1;
            // 'month' is zero indexed and iMonth is 1 indexed
            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            double epochMonthStart = ((new TimeSpan(new DateTime(iYear, iMonth, 1).Ticks)) - epochTicks).TotalSeconds - 19800;

            iMonth++;
            if (iMonth > 12)
            {
                iYear++;
                iMonth = 1;
            }

            string sSQL = "DELETE from " + MyGlobal.activeDB + ".tbl_rosters_report where m_StaffID='" + staffid + "' or m_LastUpdate<(UNIX_TIMESTAMP()-300);";
            using (MySqlCommand com = new MySqlCommand(sSQL, con))
            {
                com.ExecuteNonQuery();
            }
            double epochMonthEnd = ((new TimeSpan(new DateTime(iYear, iMonth, 1).Ticks)) - epochTicks).TotalSeconds - 19800;
            String sMasterInsertSQL = "";
            Int32[] arWorkhours = new Int32[32]; // 0 index not used
            for (int i = 1; i < 32; i++) arWorkhours[i] = -1;

            sSQL = "select m_StaffID, from_unixtime(m_ActivityTime, '%d'), sum(m_WorkTime) from " +
             "( " +
             "select m_StaffID, m_ActivityTime, m_WorkTime " +
             "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
             "where (m_ActivityTime >= " + epochMonthStart + " and m_ActivityTime < " + epochMonthEnd + ") " +
            "and(m_Activity = 'update' or m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'approved') and m_StaffID = '" + staffid + "' " +
            "union " +
            "select m_StaffID, m_ActivityTime,(m_ActivityTime - m_ActivityStart) " +
            "from " + MyGlobal.activeDB + ".tbl_terminals " +
            "where (m_ActivityTime >= " + epochMonthStart + " and m_ActivityTime<" + epochMonthEnd + ") " +
            "and m_Activity = 'open' and m_StaffID = '" + staffid + "' " +
            ") as x " +
            "group by from_unixtime(m_ActivityTime, '%d');";


            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                            {
                                arWorkhours[reader.GetInt16(1)] = reader.GetInt32(2);
                            }
                        }
                    }
                }
            }

            if (staffid.Length > 0)
            {
                sMasterInsertSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters_report (m_Profile,m_Year,m_Month,m_StaffID,m_RosterName,m_ShiftName";
                for (int i = 1; i < 32; i++)
                {
                    if (arWorkhours[i] != -1)
                    {
                        sMasterInsertSQL += ",m_Day" + i + "_log";
                    }
                }
                sMasterInsertSQL += ",m_LastUpdate) values ";
                sMasterInsertSQL += "('" + profile + "','" + year + "','" + month + "','" + staffid + "','" + roster + "','" + shift + "'";
                for (int i = 1; i < 32; i++)
                {
                    if (arWorkhours[i] != -1)
                    {
                        sMasterInsertSQL += ",'" + arWorkhours[i] + "'";
                    }
                }
                sMasterInsertSQL += ",UNIX_TIMESTAMP());";
            }
            if (sMasterInsertSQL.Length > 0)
            {
                using (MySqlCommand com = new MySqlCommand(sMasterInsertSQL, con))
                {
                    com.ExecuteNonQuery();
                }
            }
        }
        [HttpPost]
        public ActionResult LoadShiftDetails_POP(string profile, string roster, string shift, string mode,
            string year, string month, string staffname, string staffid, string firstoff,
            string staffspecific)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadShiftDetailsResponse = new OnLoadShiftDetailsResponse();
            onLoadShiftDetailsResponse.status = false;
            onLoadShiftDetailsResponse.result = "";
            if (staffspecific == null) staffspecific = "";

            String sErrMessage = "";
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month) + 1;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int iLastDayOfThisMonth = DateTime.DaysInMonth(iYear, iMonth);

                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        String sMonth = (iMonth).ToString();
                        if ((iMonth) < 10) sMonth = "0" + (iMonth);
                        String sDate = i.ToString();
                        if (i < 10) sDate = "0" + i;
                        DateTime dt;
                        if (DateTime.TryParseExact(year + "-" + sMonth + "-" + sDate,
                                               "yyyy-MM-dd",
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.None,
                                               out dt))
                        {

                            onLoadShiftDetailsResponse.sarDayHeaders.Add(
                                dt.Equals(DateTime.Today) ? ("1" + dt.ToString("ddd") + " / " + i) : ("0" + dt.ToString("ddd") + " / " + i)
                                );
                        }
                    }
                    //-------------------Data for individual staff

                    sSQL = "SELECT roster.m_id,roster.m_RosterName,roster.m_ShiftName,roster.m_StaffName,roster.m_StaffID,roster.m_ShiftStartTime,roster.m_ShiftEndTime ";
                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        sSQL += ",m_Day" + i;
                    }
                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        sSQL += ",m_DayL" + i + ",m_Status" + i;
                    }
                    sSQL += " " +
                    "FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                    "left join " + MyGlobal.activeDB + ".tbl_leaves leav on leav.m_StaffID = roster.m_StaffID and leav.m_Year = roster.m_Year and leav.m_Month = roster.m_Month " +
                    "where roster.m_Profile = '" + profile + "' and roster.m_RosterName='" + roster + "' " +
                    "and roster.m_RosterName is not null and roster.m_RosterName is not null " +
                    "and roster.m_StaffID is not null " +
                    "and roster.m_Year = '" + year + "' and roster.m_Month = '" + month + "' ";
                    if (staffspecific.Length > 0) sSQL += "and roster.m_StaffID = '" + staffspecific + "' ";
                    sSQL += "order by roster.m_StaffName";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    onLoadShiftDetailsResponse.staffspecificname = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    onLoadShiftDetailsResponse.staffspecificid = staffspecific;
                                    StaffRow staffRow = new StaffRow();
                                    staffRow.m_id = reader.GetString(0);
                                    staffRow.m_RosterName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    staffRow.m_ShiftName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    staffRow.m_StaffName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    staffRow.m_StaffID = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    staffRow.m_ShiftStart = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                                    staffRow.m_ShiftEnd = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                                    int ordinal = reader.GetOrdinal("m_Day1");
                                    int ordinalLeave = reader.GetOrdinal("m_DayL1");
                                    for (int i = 0; i < iLastDayOfThisMonth; i++)
                                    {
                                        string str =
                                            reader.IsDBNull(ordinal + i) ? "" : reader.GetString(ordinal + i);
                                        string lev = "";
                                        if (!reader.IsDBNull(ordinalLeave + (i * 2) + 1)) // m_Status1
                                        {
                                            if ((reader.GetInt16(ordinalLeave + (i * 2) + 1) == C_REVOKE_PENDING) ||
                                            (reader.GetInt16(ordinalLeave + (i * 2) + 1) == C_APPROVED))
                                            {
                                                if (!reader.IsDBNull(ordinalLeave + (i * 2) + 0))
                                                { // m_DayL1
                                                    lev = reader.GetString(ordinalLeave + (i * 2) + 0);
                                                }
                                            }
                                        }
                                        if (lev.Length > 0) str = str + " " + lev;
                                        staffRow.arRosterOptions.Add(str);
                                    }
                                    onLoadShiftDetailsResponse.oStaffRows.Add(staffRow);
                                }
                            }
                        }
                    }

                    //-------------------Data for individual staff END
                    onLoadShiftDetailsResponse.status = true;
                    if (sErrMessage.Length > 0)
                    {
                        onLoadShiftDetailsResponse.status = false;
                        onLoadShiftDetailsResponse.result = sErrMessage;
                    }
                }
            }
            catch (MySqlException ex)
            {
                onLoadShiftDetailsResponse.result = ex.Message;
                MyGlobal.Error("onLoadShiftDetailsResponse.." + ex.Message);
            }
            return Json(onLoadShiftDetailsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadShiftDetails(string profile, string roster, string shift, string mode,
    string year, string month, string staffname, string staffid, string firstoff,
    string staffspecific, string optioncount, string showproduction, string loginstaffid)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadShiftDetailsResponse = new OnLoadShiftDetailsResponse();
            onLoadShiftDetailsResponse.status = false;
            onLoadShiftDetailsResponse.result = "";
            if (staffspecific == null) staffspecific = "";

            String sErrMessage = "";
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month) + 1;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("processworkinghours"))
                    {
                        long lShiftStart = 0, lShiftEnd = 0;
                        //----------------------------------------Get Shift Start End
                        sSQL = "select m_ShiftStartTime,m_ShiftEndTime from " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' and m_RosterName='" + roster + "' " +
                            "and m_ShiftName='" + shift + "' and m_Year='" + year + "' and m_Month='" + month + "' " +
                            "and m_StaffID is null;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0)) lShiftStart = reader.GetInt64(0);
                                        if (!reader.IsDBNull(1)) lShiftEnd = reader.GetInt64(1);
                                        if (lShiftEnd < lShiftStart) lShiftEnd += 86400;
                                    }
                                }
                            }
                        }
                        //----------------------------------------
                        ProcessWorkingHours(con, year, month, staffid, profile, roster, shift);
                    }
                    else if (mode.Equals("deleteshift"))
                    {
                        bool bStaffExists = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where " +
                            "m_RosterName='" + roster + "' " +
                            "and m_ShiftName='" + shift + "' " +
                            "and m_Year = '" + year + "' " +
                            "and m_Month = '" + month + "' and m_StaffName is not null";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bStaffExists = reader.HasRows;
                            }
                        }
                        if (bStaffExists)
                        {
                            sErrMessage = "Active staff exists";
                        }
                        else
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_rosters where " +
                                "m_RosterName='" + roster + "' " +
                                "and m_ShiftName='" + shift + "' " +
                                "and m_Year = '" + year + "' " +
                                "and m_Month = '" + month + "';";
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                                sErrMessage = "Shift deleted. Reload to update.";
                            }
                        }
                    }
                    else if (mode.Equals("addnewrow"))
                    {   //------------------------------------If new row, do here
                        //______________________Check if the staff is valid
                        sSQL = @"select m_FName,m_StaffID from " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Profile = '" + profile + "' and m_StaffID is not null and m_StaffID='" + staffid + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0)) staffname = reader.GetString(0);
                                    }
                                }
                                else
                                {
                                    sErrMessage = "Invalid Staff";
                                }
                            }
                        }
                        //______________________Check if the staff is free
                        if (sErrMessage.Length == 0)
                        {
                            sSQL = @"select m_RosterName,m_ShiftName,m_StaffName,m_StaffID from " + MyGlobal.activeDB + ".tbl_rosters " +
                            "where m_Profile = '" + profile + "' and m_Year = '" + year + "' and m_Month = '" + month + "' " +
                            "and m_ShiftName = '" + shift + "' and m_RosterName = '" + roster + "' " +
                            "and m_StaffID is not null and m_StaffID='" + staffid + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        /*
                                        String sStr = "";
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) sStr += "Roster: " + reader.GetString(0) + ",";
                                            if (!reader.IsDBNull(1)) sStr += "Shift: " + reader.GetString(1) + ",";
                                            if (!reader.IsDBNull(2)) sStr += "Name: " + reader.GetString(2) + ",";
                                            if (!reader.IsDBNull(3)) sStr += "ID: " + reader.GetString(3) + ",";
                                        }
                                        */
                                        sErrMessage = "Staff " + staffid + " already assigned in ";
                                        sErrMessage += "Roster > " + roster + ", Shift > " + shift;
                                    }
                                }
                            }
                        }
                        //_________________________Check for previous error
                        if (sErrMessage.Length == 0)
                        {
                            //______________________Get staff's OFF day from previous month
                            int iOffPosition = 7;
                            if (firstoff.Length == 0)
                            {
                                int iYearLocal = MyGlobal.GetInt16(year);
                                int imonthLocal = MyGlobal.GetInt16(month);
                                imonthLocal--;
                                if (imonthLocal < 0)
                                {
                                    iYearLocal--;
                                    imonthLocal = 11;
                                }
                                sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_rosters " +
"where m_Profile = '" + profile + "' and m_Year = '" + iYearLocal + "' and m_Month = '" + imonthLocal + "' and m_ShiftName='" + shift + "' order by m_id desc limit 1;";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            if (reader.Read())
                                            {
                                                //iOffPosition = 0;
                                                for (int i = 8; i <= 38; i = i + 1)
                                                {
                                                    if (!reader.IsDBNull(i))
                                                    {
                                                        if (reader.GetString(i).Equals("OFF"))
                                                        {
                                                            iOffPosition = 7;

                                                        }
                                                        else
                                                        {
                                                            //iOffPosition++;
                                                            //if (iOffPosition > 6) iOffPosition = 0;
                                                        }
                                                    }
                                                    iOffPosition--;
                                                    if (iOffPosition > 7) iOffPosition = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                //if (iOffPosition > 6) iOffPosition = 0;
                            }
                            else
                            {
                                iOffPosition = MyGlobal.GetInt16(firstoff);
                            }
                            //_____________________Get Start and End timings...
                            Int32 iShiftStart = 0, iShiftEnd = 0;
                            sSQL = @"select m_ShiftStartTime,m_ShiftEndTime from " + MyGlobal.activeDB + ".tbl_rosters " +
"where m_Profile = '" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' and m_Year = '" + year + "' and m_Month = '" + month + "' and m_StaffID is null limit 1;";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) iShiftStart = reader.GetInt32(0);
                                            if (!reader.IsDBNull(1)) iShiftEnd = reader.GetInt32(1);
                                        }
                                    }
                                }
                            }
                            //if (iShiftEnd < iShiftStart) iShiftEnd = iShiftEnd + 86400;
                            //______________________Add this staff
                            int iStartDay = 0;
                            int iEndDay = 31;
                            DateTime todaysDate = DateTime.Today;
                            for (int i = 1; i <= 31; i++)
                            {
                                try
                                {
                                    if (todaysDate <= (new DateTime(iYear, iMonth, i)))
                                    {
                                        if (iStartDay == 0) iStartDay = i;
                                    }
                                    iEndDay = i;
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    break;
                                }
                            }

                            if (iStartDay > 0)
                            {
                                //---------------Get confirmed leave position
                                int[] arLeaves = Enumerable.Repeat(0, 32).ToArray();
                                sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_leaves " +
    "where m_Profile = '" + profile + "' and m_Year = '" + year + "' and m_Month = '" + month + "' " +
    "and m_StaffID='" + staffid + "';";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            if (reader.Read())
                                            {
                                                int ordinal = reader.GetOrdinal("m_Status1");
                                                for (int i = 0; i < 31; i++)
                                                {
                                                    if (!reader.IsDBNull(ordinal + (i * 2)))
                                                    {
                                                        arLeaves[i] = reader.GetInt16(ordinal + (i * 2));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                //---------------Create roster table
                                sSQL = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters (" +
"m_RosterName,m_ShiftName,m_StaffName,m_StaffID,m_Year,m_Month,";
                                for (int i = iStartDay; i <= iEndDay; i += 1)
                                {
                                    sSQL += "m_Day" + i + ",";
                                }
                                sSQL += "m_Profile,m_ShiftStartTime,m_ShiftEndTime) value (";
                                sSQL += "'" + roster + "','" + shift + "','" + staffname + "','" +
                                    staffid + "','" + year + "','" + month + "',";

                                for (int i = iStartDay; i <= iEndDay; i += 1)
                                {
                                    //A confirmed leave exists on this day
                                    if ((arLeaves[i - 1] == C_REVOKE_PENDING) ||
                                        (arLeaves[i - 1] == C_APPROVED))
                                    {
                                        sSQL += "null,";
                                    }
                                    else
                                    {
                                        // No leave, so go with roster
                                        if (iOffPosition == 1)
                                        {
                                            sSQL += "'OFF',";
                                        }
                                        else
                                        {
                                            sSQL += "'" + MyGlobal.WORKDAY_MARKER + "',";
                                        }
                                        /*
                                        if (iOffPosition >= 6)
                                        {
                                            sSQL += "'OFF',";
                                            iOffPosition = 0;
                                        }
                                        else
                                        {
                                            sSQL += "'" + MyGlobal.WORKDAY_MARKER + "',";
                                            iOffPosition++;
                                        }
                                        */
                                    }
                                    iOffPosition--;
                                    if (iOffPosition < 1) iOffPosition = 7;
                                }
                                sSQL += "'" + profile + "','" + iShiftStart + "','" + iShiftEnd + "')";
                                using (MySqlCommand com = new MySqlCommand(sSQL, con))
                                {
                                    com.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                onLoadShiftDetailsResponse.result = "Expired. Can't add entries now";
                            }
                        }
                    }
                    else if (mode.Equals("deleterow"))
                    {
                        sSQL = @"delete from " + MyGlobal.activeDB + ".tbl_rosters where " +
"m_Profile='" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
"and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "';";

                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                        }
                    }
                    //---------------------------------------------------------
                    if (MyGlobal.GetInt16(optioncount) == 0)
                    {
                        sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                            "where m_Profile = '" + profile + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        onLoadShiftDetailsResponse.sarRosterOptions.Add(MyGlobal.GetPureString(reader, "m_Name"));
                                    }
                                }
                            }
                        }

                    }
                    //**********************************************Get data now
                    //____________________________________Get ro Sun/6 etc...
                    int iLastDayOfThisMonth = DateTime.DaysInMonth(iYear, iMonth);

                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        String sMonth = (iMonth).ToString();
                        if ((iMonth) < 10) sMonth = "0" + (iMonth);
                        String sDate = i.ToString();
                        if (i < 10) sDate = "0" + i;
                        DateTime dt;
                        if (DateTime.TryParseExact(year + "-" + sMonth + "-" + sDate,
                                               "yyyy-MM-dd",
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.None,
                                               out dt))
                        {

                            onLoadShiftDetailsResponse.sarDayHeaders.Add(
                                dt.Equals(DateTime.Today) ? ("1" + dt.ToString("ddd") + " / " + i) : ("0" + dt.ToString("ddd") + " / " + i)
                                );
                        }
                    }
                    //----------------------------Get working day count
                    sSQL = "SELECT roster.m_id";
                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        sSQL += ",sum(Case When m_Day" + i + " = '" + MyGlobal.WORKDAY_MARKER + "' Then 1 Else 0 End) as Day" + i;
                    }
                    sSQL += ",holiday.* FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                    "left join " + MyGlobal.activeDB + ".tbl_holidays holiday on holiday.m_Profile = roster.m_Profile and holiday.m_Year = roster.m_Year and holiday.m_Month = roster.m_Month and holiday.m_StaffID = roster.m_StaffID " +
                    "where roster.m_Profile = '" + profile + "' " +
                    "and roster.m_RosterName = '" + roster + "' and roster.m_ShiftName = '" + shift + "' " +
                    "and roster.m_Year = '" + year + "' and roster.m_Month = '" + month + "'  and roster.m_StaffID is not null ";
                    //"group by roster.m_Day1;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    int iHolidayStart = reader.GetOrdinal("m_DayH1");
                                    for (int i = 1; i <= iLastDayOfThisMonth; i++)
                                    {
                                        if (!reader.IsDBNull(i))
                                        {
                                            onLoadShiftDetailsResponse.sarDayCounters.Add(reader.GetInt16(i));
                                        }
                                        Holiday holiday = new Holiday();
                                        if (reader.IsDBNull(iHolidayStart + ((i - 1) * 3)))
                                        {
                                            onLoadShiftDetailsResponse.sarDayHolidays.Add(holiday);
                                        }
                                        else
                                        {
                                            holiday.c = reader.GetString(iHolidayStart + ((i - 1) * 3));
                                            //MyGlobal.Error("* " + holiday.c);
                                            /*
                                            if (!reader.IsDBNull(iHolidayStart + ((i - 1) * 3) + 1))
                                                holiday.t = reader.GetInt16(iHolidayStart + ((i - 1) * 3) + 1);
                                            if (!reader.IsDBNull(iHolidayStart + ((i - 1) * 3) + 2))
                                                holiday.d = reader.GetString(iHolidayStart + ((i - 1) * 3) + 2);
                                                */
                                            onLoadShiftDetailsResponse.sarDayHolidays.Add(holiday);
                                        }

                                    }
                                }
                            }
                        }
                    }
                    //--------------------------------------------------------------
                    /*
                    string permission = "select m_Team from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                "where m_Profile = '" + profile + "' and m_StaffID = '" + loginstaffid + "' ";
                if (showproduction.Equals("1"))
                        permission += "and m_Head='production'";
                    else
                        permission += "and m_Head='roster'";
                    
                    sSQL = @"SELECT m_RosterName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile = '" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' " +
                        "and m_RosterName in (" + permission + ") " +
                        "group by m_RosterName order by m_RosterName;";
                    
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
                                        onLoadShiftDetailsResponse.sarRosters.Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                    */
                    //--------------------------------------------------------------
                    Int32 unixTimestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    // Show along with the report
                    if (mode.Equals("processworkinghours"))
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                        "left join " + MyGlobal.activeDB + ".tbl_rosters_report report on report.m_StaffID = roster.m_StaffID  " +
                        "left join " + MyGlobal.activeDB + ".tbl_leaves leav on leav.m_StaffID = roster.m_StaffID and leav.m_Year = roster.m_Year and leav.m_Month = roster.m_Month and leav.m_Profile = roster.m_Profile " +
                        "left join " + MyGlobal.activeDB + ".tbl_holidays holiday on holiday.m_Profile = roster.m_Profile and holiday.m_Year = roster.m_Year and holiday.m_Month = roster.m_Month and holiday.m_StaffID = roster.m_StaffID " +
                        "where roster.m_Profile = '" + profile + "' " +
                        "and roster.m_RosterName = '" + roster + "' and roster.m_ShiftName = '" + shift + "' " +
                        "and roster.m_Year = '" + year + "' and roster.m_Month = '" + month + "' " +
                        "and roster.m_StaffID is not null " +
                        "order by roster.m_StaffName;";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                        "left join " + MyGlobal.activeDB + ".tbl_leaves leav on leav.m_StaffID = roster.m_StaffID and leav.m_Year = roster.m_Year and leav.m_Month = roster.m_Month and leav.m_Profile = roster.m_Profile " +
                        "left join " + MyGlobal.activeDB + ".tbl_holidays holiday on holiday.m_Profile = roster.m_Profile and holiday.m_Year = roster.m_Year and holiday.m_Month = roster.m_Month and holiday.m_StaffID = roster.m_StaffID " +
                        "where roster.m_Profile = '" + profile + "' and roster.m_RosterName = '" + roster + "' and roster.m_ShiftName = '" + shift + "' and roster.m_Year = '" + year + "' and roster.m_Month = '" + month + "' " +
                        "and roster.m_StaffID is not null " +
                        "order by roster.m_StaffName";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(1) && !reader.IsDBNull(2) && !reader.IsDBNull(4) && !reader.IsDBNull(5) && !reader.IsDBNull(6) && !reader.IsDBNull(7))
                                    {
                                        Int32 unixMonthStart = 0;
                                        if (reader.GetInt32(5) > 1970 && reader.GetInt32(5) < 9999 && reader.GetInt32(6) >= 0 && reader.GetInt32(6) < 12)
                                        {
                                            unixMonthStart = MyGlobal.GetSeconds(reader.GetInt32(5), reader.GetInt32(6));
                                        }
                                        RosterRow rosterRow = new RosterRow();
                                        rosterRow.m_id = reader.GetString(0);
                                        rosterRow.m_StaffName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                        rosterRow.m_StaffID = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                        String[] sarLeaves = GetLeaves();
                                        for (int i = 0; i < iLastDayOfThisMonth; i++)
                                        {
                                            RosterDay rosterDay = new RosterDay();
                                            rosterDay.id = reader.GetInt16(0) + "_" + (i + 1);
                                            rosterDay.day = i + 1;
                                            rosterDay.Code = "";

                                            rosterDay.production = GetProductionDetals(profile, rosterRow.m_StaffID, iYear, iMonth, i + 1, reader.IsDBNull(reader.GetOrdinal("m_RosterName")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterName")));
                                            //-----------Get WD and OFF from " + MyGlobal.activeDB + ".tbl_rosters
                                            int ordinal = reader.GetOrdinal("m_Day" + (i + 1));
                                            if (!reader.IsDBNull(ordinal)) rosterDay.Code = reader.GetString(ordinal);
                                            //-----------Get Leaves from " + MyGlobal.activeDB + ".tbl_leaves and override
                                            ordinal = reader.GetOrdinal("m_DayL" + (i + 1));
                                            if (!reader.IsDBNull(ordinal))
                                            {
                                                string leaveCode = reader.GetString(ordinal);
                                                int ordinalStatus = reader.GetOrdinal("m_Status" + (i + 1));
                                                if (!reader.IsDBNull(ordinalStatus))
                                                {
                                                    if ((reader.GetInt16(ordinalStatus) == C_APPROVED) ||
                                                        (reader.GetInt16(ordinalStatus) == C_REVOKE_PENDING))
                                                    {
                                                        if (rosterDay.Code.Length == 0)
                                                        {
                                                            rosterDay.Code = leaveCode;
                                                        }
                                                        else
                                                        {
                                                            rosterDay.Code += " (" + leaveCode + ")";
                                                        }
                                                    }
                                                }
                                            }
                                            rosterDay.Holi = "";
                                            int ordinalHoliday = reader.GetOrdinal("m_DayH" + (i + 1));
                                            if (!reader.IsDBNull(ordinalHoliday))
                                            {
                                                rosterDay.Holi = reader.GetString(ordinalHoliday);
                                            }

                                            //-----------Get Holidays from " + MyGlobal.activeDB + ".tbl_holidays and override
                                            /*
                                            ordinal = reader.GetOrdinal("m_DayH" + (i + 1));
                                            if (!reader.IsDBNull(ordinal))
                                            {
                                                rosterDay.Code = reader.GetString(ordinal);
                                                int iOrdiDesc = reader.GetOrdinal("m_Desc" + (i + 1));
                                                if (!reader.IsDBNull(ordinal)) rosterDay.Desc = reader.GetString(iOrdiDesc);
                                            }
                                            */
                                            //---------------------------------------------
                                            rosterDay.Log = "";
                                            if (mode.Equals("processworkinghours"))
                                            {
                                                ordinal = reader.GetOrdinal("m_Day" + (i + 1) + "_log");
                                                if (!reader.IsDBNull(ordinal)) rosterDay.Log = MyGlobal.GetHHMMSS(reader.GetInt32(ordinal));
                                            }
                                            // Replace (i + 1) with i, if same day should be blocked
                                            rosterDay.expired = ((unixMonthStart + (i + 1) * 86400) >= unixTimestamp) ? 0 : 1;
                                            rosterRow.arRosterDays.Add(rosterDay);
                                        }
                                        onLoadShiftDetailsResponse.oRosterRows.Add(rosterRow);
                                    }
                                }
                            }
                        }
                    }
                    //-------------------Data for individual staff
                    if (staffspecific.Length > 0)
                    {
                        sSQL = "SELECT roster.m_id,roster.m_StaffName,roster.m_RosterName,roster.m_ShiftName ";
                        for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                        {
                            sSQL += ",m_Day" + i;
                        }
                        for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                        {
                            sSQL += ",m_DayL" + i + ",m_Status" + i;
                        }
                        sSQL += " " +
"FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
"left join " + MyGlobal.activeDB + ".tbl_leaves leav on leav.m_StaffID = roster.m_StaffID and leav.m_Year = roster.m_Year and leav.m_Month = roster.m_Month " +
"where roster.m_Profile = '" + profile + "' " +
"and roster.m_Year = '" + year + "' and roster.m_Month = '" + month + "'  and roster.m_StaffID is not null and roster.m_StaffID = '" + staffspecific + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        onLoadShiftDetailsResponse.staffspecificname = reader.GetString(1);
                                        onLoadShiftDetailsResponse.staffspecificid = staffspecific;
                                        StaffRow staffRow = new StaffRow();
                                        staffRow.m_id = reader.GetString(0);
                                        staffRow.m_RosterName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        staffRow.m_ShiftName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                        int ordinal = reader.GetOrdinal("m_Day1");
                                        int ordinalLeave = reader.GetOrdinal("m_DayL1");
                                        for (int i = 0; i < iLastDayOfThisMonth; i++)
                                        {
                                            string str =
                                                reader.IsDBNull(ordinal + i) ? "" : reader.GetString(ordinal + i);
                                            string lev = "";
                                            if (!reader.IsDBNull(ordinalLeave + (i * 2) + 1)) // m_Status1
                                            {
                                                if ((reader.GetInt16(ordinalLeave + (i * 2) + 1) == C_REVOKE_PENDING) ||
                                                    (reader.GetInt16(ordinalLeave + (i * 2) + 1) == C_APPROVED))
                                                {
                                                    if (!reader.IsDBNull(ordinalLeave + (i * 2) + 0))
                                                    { // m_DayL1
                                                        lev = reader.GetString(ordinalLeave + (i * 2) + 0);
                                                    }
                                                }
                                            }
                                            if (lev.Length > 0) str = str + " " + lev;
                                            staffRow.arRosterOptions.Add(str);
                                        }
                                        onLoadShiftDetailsResponse.oStaffRows.Add(staffRow);
                                    }
                                }
                            }
                        }
                    }
                    //-------------------Data for individual staff END
                    onLoadShiftDetailsResponse.status = true;
                    if (sErrMessage.Length > 0)
                    {
                        onLoadShiftDetailsResponse.status = false;
                        onLoadShiftDetailsResponse.result = sErrMessage;
                    }
                }
            }
            catch (MySqlException ex)
            {
                onLoadShiftDetailsResponse.result = ex.Message;
                MyGlobal.Error("onLoadShiftDetailsResponse.." + ex.Message);
            }
            return Json(onLoadShiftDetailsResponse, JsonRequestBehavior.AllowGet);
        }
        public String[] GetLeaves()
        {
            string[] items = new string[10];
            //string[] items = { "Item1", "Item2", "Item3", "Item4" };
            return items;
        }
        private PRODuction GetProductionDetals(string profile, string staffid, int year, int month, int day, string rostername)
        {
            PRODuction prod = new PRODuction();
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_production " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (month - 1) + "' and m_Day='" + day + "' " +
                        "order by m_id;";
                    //"and m_Process='" + rostername + "' " +
                    //" limit 1";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    prod.processName1 = reader.IsDBNull(reader.GetOrdinal("m_Process")) ? "" : reader.GetString(reader.GetOrdinal("m_Process"));
                                    prod.processTarget1 = reader.IsDBNull(reader.GetOrdinal("m_Target")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_Target"));
                                    prod.processAchived1 = reader.IsDBNull(reader.GetOrdinal("m_Achived")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_Achived"));
                                }
                                if (reader.Read())
                                {
                                    prod.processName2 = reader.IsDBNull(reader.GetOrdinal("m_Process")) ? "" : reader.GetString(reader.GetOrdinal("m_Process"));
                                    prod.processTarget2 = reader.IsDBNull(reader.GetOrdinal("m_Target")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_Target"));
                                    prod.processAchived2 = reader.IsDBNull(reader.GetOrdinal("m_Achived")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_Achived"));
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {

            }
            return prod;
        }
        //------------------------------------------
        [HttpPost]
        public ActionResult UpdateCell(string profile, string roster, string shift, string cell,
    string year, string month, string staffname, string staffid, string newvalue, string mode)
        {
            var onUpdateCell = new OnUpdateCell();
            onUpdateCell.status = false;
            onUpdateCell.result = "";
            onUpdateCell.cellid = "";
            onUpdateCell.cellvalue = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------------If new row, do here
                    char[] delimiterChars = { '_' };
                    string[] arData = cell.Split(delimiterChars);
                    if (arData.Length != 2)
                    {
                        onUpdateCell.result = "Invalid Request";
                        return Json(onUpdateCell, JsonRequestBehavior.AllowGet);
                    }
                    if (newvalue.Length == 0)
                    {
                        newvalue = "null";
                    }
                    else
                    {
                        newvalue = "'" + newvalue + "'";
                    }
                    //-------------------------------Is this day already approved for attendance?
                    DateTime dt = new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1, MyGlobal.GetInt16(arData[1]));
                    Int32 unixDayStart = (Int32)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_attendance_approved  " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Date='" + unixDayStart + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onUpdateCell.result = "Attendance is already approved for this day. " +
                                        MyGlobal.GetInt16(arData[1]) + "-" + (MyGlobal.GetInt16(month) + 1) + "-" + MyGlobal.GetInt16(year) +
                                        " StaffID = " + staffid;
                                    return Json(onUpdateCell, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                    //-------------------------------
                    if (mode.Equals("allafter"))
                    {
                        int iDay = MyGlobal.GetInt16(arData[1]);
                        for (int i = iDay; i <= DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1); i++)
                        {
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set m_Day" + i + "=" + newvalue + " where " +
                            "m_Profile='" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                            "and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "';";
                        }
                    }
                    else if (mode.Equals("allbefore"))
                    {
                        int iDay = MyGlobal.GetInt16(arData[1]);
                        for (int i = DateTime.Today.Day; i <= iDay; i++)
                        {
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set m_Day" + i + "=" + newvalue + " where " +
                            "m_Profile='" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                            "and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "';";
                        }
                    }
                    else
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set m_Day" + arData[1] + "=" + newvalue + " where " +
                        "m_Profile='" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                        "and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "';";
                    }
                    int iUpdateMobileApps = 0;
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        iUpdateMobileApps = com.ExecuteNonQuery();
                        onUpdateCell.cellid = cell;
                        if (newvalue.Length > 0)
                        {
                            onUpdateCell.cellvalue = newvalue;// sCodes[newvalue[0] - 0x30];
                        }
                    }
                    //-------------Update the Mobile apps, if changes
                    if (iUpdateMobileApps > 0)
                    {
                        //-----------Get all Mobile Apps run by this staff
                        sSQL = "select m_imei from " + MyGlobal.activeDB + ".tbl_mobile_users " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and(m_Status='Active' or m_Status='Trainee');";
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
                                            SignalRObj obj = new SignalRObj();
                                            obj.comm = "reload";
                                            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
                                            var hub = hd.ResolveHub("ChatHub") as ChatHub;
                                            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                                            List<String> connections = hub.GetGreyOfficeMobileConnections(reader.GetString(0));
                                            if (connections != null)
                                            {
                                                foreach (String connectionID in connections)
                                                {
                                                    hubContext.Clients.Client(connectionID).greymobile(obj);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    onUpdateCell.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("UpdateCell-MySqlException-" + ex.Message);
                onUpdateCell.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("UpdateCell-Exception-" + ex.Message);
                onUpdateCell.result = ex.Message;
            }
            return Json(onUpdateCell, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult UpdateCell_production(string profile, string roster, string shift, string cell,
            string year, string month, string staffname, string staffid, string newvalue, string mode,
            string processname, string processtarget,
            string day)
        {
            var onUpdateCell_production = new OnUpdateCell_production();
            onUpdateCell_production.status = false;
            onUpdateCell_production.result = "";
            onUpdateCell_production.cellid = "";
            onUpdateCell_production.cellvalue = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------Get staff team
                    string staffTeam = "";
                    sSQL = "select m_Team,m_FName from " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    staffTeam = reader.IsDBNull(reader.GetOrdinal("m_Team")) ? "" : reader.GetString(reader.GetOrdinal("m_Team"));
                                    staffname = reader.IsDBNull(reader.GetOrdinal("m_FName")) ? "" : reader.GetString(reader.GetOrdinal("m_FName"));

                                }
                            }
                        }
                    }
                    //------------------------------------If new row, do here
                    char[] delimiterChars = { '_' };
                    string[] arData = cell.Split(delimiterChars);
                    if (arData.Length != 2)
                    {
                        onUpdateCell_production.result = "Invalid Request";
                        return Json(onUpdateCell_production, JsonRequestBehavior.AllowGet);
                    }
                    int[] dayMark = new int[32];
                    if (mode.Equals("allafter") || mode.Equals("allbefore"))
                    {
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' and m_Month='" + month + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        for (int i = 1; i <= 31; i++)
                                        {
                                            int iOr = reader.GetOrdinal("m_Day" + i);
                                            dayMark[i - 1] = reader.IsDBNull(iOr) ? 0 : (reader.GetString(iOr).Equals("WD") ? 1 : 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (mode.Equals("allafter"))
                    {
                        int iDay = MyGlobal.GetInt16(arData[1]);
                        for (int i = iDay; i <= DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1); i++)
                        {
                            if (dayMark[i - 1] == 1)
                            {
                                if (processname.Length > 0)
                                {
                                    sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_production " +
                                        "(m_Profile,m_StaffID,m_Name,m_Year,m_Month,m_Day,m_Team,m_Process,m_Target) values " +
                                        "('" + profile + "','" + staffid + "','" + staffname + "'," +
                                        "'" + year + "','" + month + "','" + i + "','" + staffTeam + "'," +
                                            "'" + processname + "','" + processtarget + "') " +
                                        "ON DUPLICATE KEY UPDATE " +
                                            "m_Target = '" + processtarget + "';";
                                }
                            }
                        }
                    }
                    else if (mode.Equals("allbefore"))
                    {
                        int iDay = MyGlobal.GetInt16(arData[1]);
                        for (int i = DateTime.Today.Day; i <= iDay; i++)
                        {
                            if (dayMark[i - 1] == 1)
                            {
                                if (processname.Length > 0)
                                {
                                    sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_production " +
                                        "(m_Profile,m_StaffID,m_Name,m_Year,m_Month,m_Day,m_Team,m_Process,m_Target) values " +
                                        "('" + profile + "','" + staffid + "','" + staffname + "'," +
                                        "'" + year + "','" + month + "','" + i + "','" + staffTeam + "'," +
                                            "'" + processname + "','" + processtarget + "') " +
                                        "ON DUPLICATE KEY UPDATE " +
                                            "m_Target = '" + processtarget + "';";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (processname.Length > 0)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_production " +
                                "(m_Profile,m_StaffID,m_Name,m_Year,m_Month,m_Day,m_Team,m_Process,m_Target) values " +
                                "('" + profile + "','" + staffid + "','" + staffname + "'," +
                                "'" + year + "','" + month + "','" + day + "','" + staffTeam + "'," +
                                    "'" + processname + "','" + processtarget + "') " +
                                "ON DUPLICATE KEY UPDATE " +
                                    "m_Target = '" + processtarget + "';";
                        }
                    }
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                        onUpdateCell_production.cellid = cell;
                        if (newvalue.Length > 0)
                        {
                            onUpdateCell_production.cellvalue = newvalue;// sCodes[newvalue[0] - 0x30];
                        }
                    }
                    onUpdateCell_production.status = true;
                }
            }
            catch (MySqlException ex)
            {
                onUpdateCell_production.result = ex.Message;
            }
            return Json(onUpdateCell_production, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------
        [HttpPost]
        public ActionResult UpdateShift(string mode, string name,
            string roster, string shift, string starttime, string endtime,
            string profile)
        {
            var onCreateRosterResponse = new OnCreateRosterResponse();
            onCreateRosterResponse.status = false;
            onCreateRosterResponse.result = "";
            String errMessage = "";
            if (mode.Equals("save") && roster.Length > 0 && shift.Length > 0)
            {
                try
                {

                    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con.Open();
                        string sSQL = @"UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set m_ShiftName='" + name + "'," +
                            "m_ShiftStartTime=TIME_TO_SEC('" + starttime + "')," +
                            "m_ShiftEndTime=TIME_TO_SEC('" + endtime + "') " +
                            "where m_Name='" + roster + "' and m_ShiftName='" + shift + "' and m_Profile='" + profile + "'";
                        using (MySqlCommand com = new MySqlCommand(sSQL, con))
                        {
                            com.ExecuteNonQuery();
                            onCreateRosterResponse.status = true;
                            onCreateRosterResponse.result = "";
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    errMessage = "Update Error - " + ex.Message;
                }
            }
            return ManageRoster(profile, mode, roster, shift, "", "", "", errMessage);
        }
        [HttpPost]
        public ActionResult ManageRoster(string profile, string mode,
            string roster, string shift,
            string newroster, string newshift, string pass, string errMessage)
        {
            var onCreateRosterResponse = new OnCreateRosterResponse();
            onCreateRosterResponse.status = false;
            onCreateRosterResponse.result = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sRet = "";
                    bool bIsDelete = true;
                    if (mode != null && mode.Equals("newshift"))
                    {
                        bIsDelete = false;
                        if (newshift != null && newshift.Length > 0 && mode.Equals("newshift"))
                        {
                            if (newroster != null && newroster.Length > 0)
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters  " +
                                "(m_Profile,m_Name,m_ShiftName) values ('" + profile +
                                "','" + newroster + "','" + newshift + "')";
                                sRet += "New Roster added. ";
                            }
                            else
                            {
                                if (roster != null && roster.Length > 0)
                                {
                                    sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_rosters  " +
                                    "(m_Profile,m_Name,m_ShiftName) values ('" + profile +
                                    "','" + roster + "','" + newshift + "')";
                                    sRet += "New shift added. ";
                                }
                                else
                                {
                                    onCreateRosterResponse.status = true;
                                    sRet += "Invalid request.  ";
                                }
                            }
                        }
                    }
                    if (mode != null && mode.Equals("deleteroster"))
                    {
                        if (roster != null && roster.Length > 0)
                        {
                            sSQL = @"DELETE from " + MyGlobal.activeDB + ".tbl_rosters where " +
                            "m_Profile='" + profile + "' and m_Name ='" + roster + "';";
                            sRet += "Roster deleted. ";
                        }
                    }
                    if (mode != null && mode.Equals("deleteshift"))
                    {
                        if (roster != null && roster.Length > 0 && shift != null && shift.Length > 0)
                        {
                            sSQL = @"DELETE from " + MyGlobal.activeDB + ".tbl_rosters where " +
                            "m_Profile='" + profile + "' and m_Name ='" + roster +
                            "' and m_ShiftName='" + shift + "';";
                            sRet += "Shift deleted. ";
                        }
                    }
                    if (sSQL.Length > 0)
                    {
                        bool bAuthenticated = true;
                        if (bIsDelete)
                        {
                            String sSQL_A = @"select m_Username,m_Email,m_Name,m_Password,m_Status,m_Profile from (" +
"SELECT m_Username,m_Email,m_FName as m_Name,m_Password,m_Status,m_Profile FROM " + MyGlobal.activeDB + ".tbl_staffs " +
") as x where (m_Username='" + profile + "' or m_Email='" + profile + "') and m_Password='" + pass + "';";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL_A, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    bAuthenticated = reader.HasRows;
                                    if (!bAuthenticated)
                                    {
                                        onCreateRosterResponse.status = false;
                                        onCreateRosterResponse.result = "Authentication failed";
                                        //return Json(onCreateRosterResponse, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        if (bAuthenticated)
                        {
                            using (MySqlCommand com = new MySqlCommand(sSQL, con))
                            {
                                com.ExecuteNonQuery();
                                onCreateRosterResponse.status = true;
                                onCreateRosterResponse.result = sRet;
                            }
                        }
                    }
                    //______________________________Get roster names
                    if (!MyGlobal.activeDomain.Equals("chchealthcare")) onCreateRosterResponse.sarRosters.Add("All");
                    sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' group by m_Name order by m_Name;";
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
                                        //MyRoster myRoster = new MyRoster();
                                        //myRoster.m_Name = reader.GetString(0);
                                        //myRoster.myShifts = GetShifts(profile,myRoster.m_Name);
                                        //________________________________________
                                        //onCreateRosterResponse.myRosters.Add(myRoster);
                                        onCreateRosterResponse.sarRosters.Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                    //______________________________Get selected roster details
                    if (roster != null && roster.Length > 0)
                    {
                        sSQL = "SELECT m_ShiftName,SEC_TO_TIME(m_ShiftStartTime),SEC_TO_TIME(m_ShiftEndTime) FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' and m_Name='" + roster + "' order by m_ShiftStartTime;";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    List<MyShift> myShifts = new List<MyShift>();
                                    onCreateRosterResponse.SelectedRoster = new MyRoster();
                                    while (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            //MyRoster myRoster = new MyRoster();
                                            //myRoster.m_Name = reader.GetString(0);
                                            //myRoster.myShifts = GetShifts(profile,myRoster.m_Name);
                                            //________________________________________
                                            //onCreateRosterResponse.myRosters.Add(myRoster);
                                            //onCreateRosterResponse.myRosters.Add(reader.GetString(0));
                                            //onCreateRosterResponse.SelectedRoster.m_Name = reader.GetString(0);
                                            //onCreateRosterResponse.SelectedRoster.myShifts = GetShifts(profile, reader.GetString(0));
                                            MyShift myShift = new MyShift();
                                            myShift.m_Name = reader.GetString(0);
                                            if (!reader.IsDBNull(1)) myShift.m_StartTime = reader.GetInt32(1);
                                            if (!reader.IsDBNull(2)) myShift.m_EndTime = reader.GetInt32(2);
                                            myShifts.Add(myShift);
                                        }
                                    }
                                    onCreateRosterResponse.SelectedRoster.m_Name = roster;
                                    onCreateRosterResponse.SelectedRoster.myShifts = myShifts;
                                }
                            }
                        }
                    }
                    onCreateRosterResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                onCreateRosterResponse.result = "Error-" + ex.Message;
            }
            if (errMessage != null && errMessage.Length > 0) onCreateRosterResponse.result = errMessage;
            return Json(onCreateRosterResponse, JsonRequestBehavior.AllowGet);
        }
        private List<MyShift> GetShifts(String profile, String sRosterName)
        {
            try
            {
                List<MyShift> myShifts = new List<MyShift>();
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_ShiftName,SEC_TO_TIME(m_ShiftStartTime),SEC_TO_TIME(m_ShiftEndTime) FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' and m_Name='" + sRosterName + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand1 = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                        {
                            if (reader1.HasRows)
                            {
                                while (reader1.Read())
                                {
                                    if (!reader1.IsDBNull(0))
                                    {
                                        MyShift myShift = new MyShift();
                                        myShift.m_Name = reader1.GetString(0);
                                        if (!reader1.IsDBNull(1)) myShift.m_StartTime = reader1.GetInt32(1);
                                        if (!reader1.IsDBNull(2)) myShift.m_EndTime = reader1.GetInt32(2);
                                        myShifts.Add(myShift);
                                    }
                                }
                            }
                        }
                    }
                }
                return myShifts;
            }
            catch (MySqlException ex)
            {
                return null;
            }
        }
        public ActionResult GetFreeStaffs(string profile, string search,
            string roster, string shift,
            string year, string month, string showall)
        {
            var freeStaffResponse = new FreeStaffResponse();
            freeStaffResponse.status = false;
            freeStaffResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_FName like '%" + search + "%' or " +
                "m_StaffID like '%" + search + "%' or " +
                "m_Mobile1 like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs as staffs " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and (m_Status='Active' or m_Status='Trainee') ";
                    if (!showall.Equals("true"))
                        sSQL += "and m_StaffID not in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_rosters where m_Profile = '" + profile + "' and m_Year = '" + year + "' and m_Month = '" + month + "' and m_StaffID is not null) ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    FreeStaffItem freeStaffItem = new FreeStaffItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) freeStaffItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) freeStaffItem.m_Name = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) freeStaffItem.m_StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) freeStaffItem.m_Designation = reader["m_Designation"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Roll"))) freeStaffItem.m_Roll = reader["m_Roll"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) freeStaffItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) freeStaffItem.m_Type = reader["m_Type"].ToString();
                                    freeStaffResponse.staffs.Add(freeStaffItem);
                                }
                                freeStaffResponse.status = true;
                                freeStaffResponse.result = "Done";
                            }
                            else
                            {
                                freeStaffResponse.result = "Sorry!!! No devices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                freeStaffResponse.result = "Error-" + ex.Message;
            }

            return Json(freeStaffResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------------------
        public ActionResult GetLedgers(string profile, string search, string mode, string type1, string type2)
        {
            var getLedgersResponse = new GetLedgersResponse();
            getLedgersResponse.status = false;
            getLedgersResponse.result = "None";
            string sSQL = "";
            if (type1 == null) type1 = "";
            if (type2 == null) type2 = "";
            String sSearchKey = " (m_Name like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' ";
                    if (type1.Length > 0)
                    {
                        if (type2.Length > 0)
                        {
                            sSQL += "and (m_Type='" + type1 + "' or m_Type='" + type2 + "') ";
                        }
                        else
                        {
                            sSQL += "and m_Type='" + type1 + "' ";
                        }
                    }
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name")))
                                    {
                                        LedgerSearchItem ledgerItem = new LedgerSearchItem();
                                        ledgerItem.Name = reader["m_Name"].ToString();
                                        getLedgersResponse.ledgers.Add(ledgerItem);
                                    }
                                }
                                getLedgersResponse.status = true;
                            }
                            else
                            {
                                getLedgersResponse.result = "Sorry!!! No devices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                getLedgersResponse.result = "Error-" + ex.Message;
            }
            return Json(getLedgersResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------------------
        public ActionResult getShiftNames(string profile, string search)
        {
            var shiftNameResponse = new ShiftNameResponse();
            shiftNameResponse.status = false;
            shiftNameResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_Name like '%" + search + "%' ) ";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_misc_shiftnames as staffs " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and length(m_ShiftStartTime)=5 " +
                    "and length(m_ShiftEndTime)=5 " +
                    "and m_Name<>''";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ShiftNameItem shiftNameItem = new ShiftNameItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) shiftNameItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) shiftNameItem.m_Name = reader["m_Name"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime"))) shiftNameItem.m_ShiftStartTime = reader["m_ShiftStartTime"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime"))) shiftNameItem.m_ShiftEndTime = reader["m_ShiftEndTime"].ToString();
                                    shiftNameResponse.names.Add(shiftNameItem);
                                }
                                shiftNameResponse.status = true;
                                shiftNameResponse.result = "Done";
                            }
                            else
                            {
                                shiftNameResponse.result = "Sorry!!! No Names";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                shiftNameResponse.result = "Error-" + ex.Message;
            }

            return Json(shiftNameResponse, JsonRequestBehavior.AllowGet);
        }
        /* ----------------------------------- LoadMyRoster ------------------------------- */
        /*
         let newEvents = [
            {
                id: 1,
                title: 'name a',
                start: '2017-02-20T09:00',
                end: '2017-02-20T11:00'
            },
            {
                id: 2,
                title: 'name b',
                start: '2017-02-20T12:00',
                end: '2017-02-20T13:00'
            }
            ]
         */
        public ActionResult LoadMyRosterx(string profile, string email, string staffid,
   string year, string month)
        {
            var onLoadMyRoster = new OnLoadMyRoster();
            onLoadMyRoster.status = false;
            onLoadMyRoster.result = "None";

            MyEvent myEvent = new MyEvent();
            myEvent.id = 1;
            myEvent.title = "111";
            myEvent.start = "2018-11-01 10:00:00";// new DateTime(2018, 10, 1,2,0,0).ToString();
            myEvent.end = "2018-11-01 16:00:00";//new DateTime(2018, 10, 1,4,0,0).ToString();
            myEvent.className = "event-default";
            onLoadMyRoster.oEvents.Add(myEvent);
            onLoadMyRoster.status = true;
            return Json(onLoadMyRoster, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------------------------------
        private string readerString(MySqlDataReader reader, string sFld)
        {
            return reader.IsDBNull(reader.GetOrdinal(sFld)) ? "" : reader.GetString(reader.GetOrdinal(sFld));
        }
        private int readerInt16(MySqlDataReader reader, string sFld)
        {
            return reader.IsDBNull(reader.GetOrdinal(sFld)) ? 0 : reader.GetInt16(reader.GetOrdinal(sFld));
        }
        public ActionResult LoadMyProduction(string profile, string email, string staffid,
            string year, string month)//month reaching here as zero indexed
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadMyProduction = new OnLoadMyProduction();
            onLoadMyProduction.status = false;
            onLoadMyProduction.result = "None";
            int iMonth = MyGlobal.GetInt16(month) - 1;
            string sSQL = "";
            try
            {
                onLoadMyProduction.StaffID = staffid;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_FName " +
                    "FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    onLoadMyProduction.StaffName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                }
                            }
                        }
                    }
                    //--------------------Load roster into the events table
                    Int32 unixTimestampTodayMorning = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_production where " +
                    "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' " +
                    "and m_Month='" + iMonth + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                HRProductionRow row = new HRProductionRow();
                                while (reader.Read())
                                {
                                    //if (!reader.IsDBNull(1)) sActiveRoster = reader.GetString(1);
                                    //if (!reader.IsDBNull(2)) sActiveShift = reader.GetString(2);
                                    String sMonth = "";
                                    int iMth = iMonth + 1;
                                    if (iMth > 9) sMonth = iMth.ToString(); else sMonth = "0" + iMth;
                                    DateTime dt;
                                    if (DateTime.TryParseExact(year + "-" + sMonth + "-" + "01",
                                                           "yyyy-MM-dd",
                                                           CultureInfo.InvariantCulture,
                                                           DateTimeStyles.None,
                                                           out dt))
                                    {
                                        //if (onLoadMyProduction.StaffName.Length == 0)
                                        //onLoadMyProduction.StaffName = reader.GetString(3);
                                        long iMonthStart = MyGlobal.ToEpochTime(dt) + 19800;
                                        //int days = DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1);
                                        int iDay = reader.IsDBNull(reader.GetOrdinal("m_Day")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_Day"));
                                        if (iDay > 0)
                                        {
                                            MyEvent_Prod myEvent = new MyEvent_Prod();
                                            myEvent.id = reader.GetInt16(0);
                                            myEvent.m_id = reader.GetInt16(0);
                                            myEvent.year = MyGlobal.GetInt16(year);
                                            myEvent.month = MyGlobal.GetInt16(month);
                                            myEvent.day = iDay;
                                            myEvent.start = MyGlobal.ToDateTimeFromEpoch(iMonthStart + ((iDay - 1) * 86400)).ToString("yyyy-MM-dd HH:mm:ss");
                                            myEvent.end = MyGlobal.ToDateTimeFromEpoch(iMonthStart + ((iDay - 1) * 86400)).ToString("yyyy-MM-dd HH:mm:ss");
                                            myEvent.allDay = false;
                                            myEvent.date = MyGlobal.ToDateTimeFromEpoch(iMonthStart + ((iDay - 1) * 86400)).ToString("dd-MM-yyyy");
                                            myEvent.process = reader.IsDBNull(reader.GetOrdinal("m_Team")) ? "" : reader.GetString(reader.GetOrdinal("m_Team"));
                                            myEvent.target = readerInt16(reader, "m_Target");
                                            myEvent.achived = readerInt16(reader, "m_Achived");
                                            //---------------------------------------------------
                                            //myEvent.samples = readerInt16(reader, "m_QASamples");
                                            //myEvent.error = readerInt16(reader, "m_QAError");
                                            //myEvent.score = readerInt16(reader, "m_QAScore");
                                            row.m_StaffID = staffid;
                                            row.m_Year = year;
                                            row.m_Month = month;
                                            row.m_Day = iDay.ToString();
                                            GetScores(profile, false, row);
                                            myEvent.samples = row.m_Samples;
                                            myEvent.error = row.m_Error;
                                            myEvent.score = row.m_Score;
                                            //---------------------------------------------------
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Process")) && !reader.IsDBNull(reader.GetOrdinal("m_Target")))
                                            {
                                                if (reader.GetString(reader.GetOrdinal("m_Process")).Equals(myEvent.process))
                                                {
                                                    myEvent.className = "event-azure";
                                                }
                                                else
                                                {
                                                    myEvent.title += "<span style='font-size:x-small;'>" +
                                                        (reader.GetString(reader.GetOrdinal("m_Process")).Length > 8 ?
                                                        reader.GetString(reader.GetOrdinal("m_Process")).Substring(0, 8) :
                                                        reader.GetString(reader.GetOrdinal("m_Process")));

                                                    myEvent.title += " ▼</span><br>";
                                                    myEvent.className = "event-default";
                                                }
                                                myEvent.title += myEvent.target;
                                                if (myEvent.achived > 0) myEvent.title += " (" + myEvent.achived + ")";
                                                if (myEvent.samples > 0)
                                                {
                                                    myEvent.title +=
                                                        "<span style='background-color:yellow;margin-left:2px;padding-right:2px;'>" +
                                                        "<span style='color:black;font-weight:bold;'>" + myEvent.samples + "</span>" +
                                                        "<span style='color:red;font-weight:bold;'>" + myEvent.error + "</span>" +
                                                        "<span style='color:darkgreen;font-weight:bold;'>" + myEvent.score + "</span>" +
                                                        "</span>";
                                                }
                                            }
                                            //myEvent.className = "event-default";
                                            //myEvent.className = "event-default";


                                            myEvent.expired =
                                                (unixTimestampTodayMorning < iMonthStart + (iDay - 1) * 86400) ? 1 : 0;

                                            onLoadMyProduction.oEvents.Add(myEvent);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //---------------------------------------------------------
                sSQL = "SELECT " +
                    "sum(Case When m_QAFreeze is null Then 0 Else 1 End) as samples," +
                    "sum(Case When m_QAScore > 0 Then 1 Else 0 End) as errors," +
                    "sum(Case When m_QAScore > 0 Then m_QAScore Else 0 End) as score " +
                    "FROM " + MyGlobal.activeDB + ".tbl_production_qatable " +
                    "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' " +
                    "and m_Year = '" + year + "' and m_Month = '" + iMonth + "' ";

                /*
                if (consolidated)
                {
                    sSQL += "and m_Process = '" + row.m_Process + "';";
                }
                else
                {
                    sSQL += "and m_Day = '" + row.m_Day + "';";
                }

                    */
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
                                    onLoadMyProduction.week1.samples = reader.IsDBNull(0) ? 0 : reader.GetInt16(0);
                                    onLoadMyProduction.week1.error = reader.IsDBNull(1) ? 0 : reader.GetInt16(1);
                                    onLoadMyProduction.week1.score = reader.IsDBNull(2) ? 0 : reader.GetInt16(2);
                                }
                            }
                        }
                    }
                }
                //---------------------------------------------------------
                sSQL = "select sum(m_Target) as target,sum(m_Achived) as achived " +
                    "from " + MyGlobal.activeDB + ".tbl_production where " +
                    "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' " +
                    "and m_Month='" + iMonth + "' ";

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
                                    onLoadMyProduction.week1.target = reader.IsDBNull(0) ? 0 : reader.GetInt16(0);
                                    onLoadMyProduction.week1.achived = reader.IsDBNull(1) ? 0 : reader.GetInt16(1);
                                }
                            }
                        }
                    }
                }
                //---------------------------------------------------------
                onLoadMyProduction.status = true;
            }
            catch (MySqlException ex)
            {
                onLoadMyProduction.result = ex.Message;
            }
            catch (Exception ex)
            {
                onLoadMyProduction.result = ex.Message;
            }
            return Json(onLoadMyProduction, JsonRequestBehavior.AllowGet);
        }
        public ActionResult LoadMyRoster_New(string profile, string email, string staffid,
            string year, string month)//month reaching here as zero indexed
        {
            var onLoadMyRoster = new OnLoadMyRoster();
            /*  Working
             * MyEvent myEvent = new MyEvent();
            myEvent.id = 1233;
            myEvent.title = "Final Test";
            myEvent.start = "2021-01-05";
            onLoadMyRoster.oEvents.Add(myEvent);*/

            return Json(onLoadMyRoster, JsonRequestBehavior.AllowGet);
        }
        public ActionResult LoadMyRoster(string profile, string email, string staffid,
            string year, string month)//month reaching here as zero indexed
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var onLoadMyRoster = new OnLoadMyRoster();
            onLoadMyRoster.status = false;
            onLoadMyRoster.result = "";
            string sActiveRoster = "", sActiveShift = "";
            long lIDAdvancer = 0;
            long lMonthStart = 0, lShiftStart = 0, lShiftEnd = 0;
            onLoadMyRoster.StaffID = staffid;
            try
            {
                //--------------------Load roster into the events table
                string sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_rosters where " +
                    "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' " +
                    "and m_Month='" + month + "' ";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    lShiftStart = reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftStartTime"));
                                    lShiftEnd = reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_ShiftEndTime"));
                                    if (!reader.IsDBNull(1)) sActiveRoster = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) sActiveShift = reader.GetString(2);
                                    DateTime dt2;
                                    int iMonth2 = 0;
                                    String sMonth2 = "";
                                    iMonth2 = MyGlobal.GetInt16(month) + 1;
                                    if (iMonth2 > 9) sMonth2 = iMonth2.ToString(); else sMonth2 = "0" + iMonth2;
                                    //DateTime dt;
                                    if (DateTime.TryParseExact(year + "-" + sMonth2 + "-" + "01",
                                                           "yyyy-MM-dd",
                                                           CultureInfo.InvariantCulture,
                                                           DateTimeStyles.None,
                                                           out dt2))
                                    {
                                        if (onLoadMyRoster.StaffName.Length == 0)
                                            onLoadMyRoster.StaffName = reader.GetString(3);

                                        int days = DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1);
                                        lMonthStart = MyGlobal.ToEpochTime(dt2) + 19800;

                                        for (int i = 0; i < days; i++)
                                        {
                                            if (!reader.IsDBNull(i + 8))
                                            {
                                                MyEvent myEvent = new MyEvent();
                                                myEvent.id = reader.GetInt16(0) + (i + 1) + lIDAdvancer;
                                                myEvent.date = MyGlobal.ToDateTimeFromEpoch(lMonthStart + ((i) * 86400)).ToString("dd-MM-yyyy");
                                                myEvent.rosterMarker = reader.GetString(i + 8);
                                                myEvent.staffid = staffid;

                                                myEvent.title = MyGlobal.ToDateTimeFromEpoch(lMonthStart + (i * 86400) + reader.GetInt32(39)).ToString("HH:mm");
                                                myEvent.title += "-";
                                                myEvent.title += MyGlobal.ToDateTimeFromEpoch(lMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("HH:mm");
                                                myEvent.title += " ";
                                                myEvent.title += reader.IsDBNull(i + 8) ? "" : ("<span style='background-color:orange;border:1px solid #666;color:#000;'>" + reader.GetString(i + 8) + "</span>");
                                                //  2018-11-07T18:00
                                                //  https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
                                                myEvent.start = MyGlobal.ToDateTimeFromEpoch(lMonthStart + (i * 86400) + reader.GetInt32(39)).ToString("yyyy-MM-dd HH:mm:ss"); //yyyy-MM-ddTHH:mm
                                                myEvent.end = MyGlobal.ToDateTimeFromEpoch(lMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("yyyy-MM-dd HH:mm:ss");
                                                myEvent.allDay = false;
                                                if (reader.IsDBNull(i + 8))
                                                {
                                                    myEvent.className = "event-default";
                                                }
                                                else
                                                {   //[ event-blue | event-azure | event-green | event-orange | event-red ]
                                                    if (reader.GetString(i + 8).Equals(MyGlobal.WORKDAY_MARKER))
                                                    {
                                                        myEvent.className = "event-azure";
                                                    }
                                                    else if (reader.GetString(i + 8).Equals("OFF"))
                                                    {
                                                        myEvent.className = "event-default";
                                                    }
                                                    else
                                                    {
                                                        myEvent.className = "event-default";
                                                    }
                                                }
                                                onLoadMyRoster.oEvents.Add(myEvent);
                                                //------------------------------------
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ProcessWorkingHours(con, year, month, staffid, profile, sActiveRoster, sActiveShift);
                }
                //--------------------Load roster into the events table END

                //--------------------Load leaves into the event table
                sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_leaves where " +
                    "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' " +
                    "and m_Month='" + month + "' ";

                onLoadMyRoster.StaffID = staffid;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    DateTime dt1;
                                    int iMonth1 = 0;
                                    String sMonth1 = "";
                                    iMonth1 = MyGlobal.GetInt16(month) + 1;
                                    if (iMonth1 > 9) sMonth1 = iMonth1.ToString(); else sMonth1 = "0" + iMonth1;

                                    //DateTime dt;
                                    if (DateTime.TryParseExact(year + "-" + sMonth1 + "-" + "01",
                                                           "yyyy-MM-dd",
                                                           CultureInfo.InvariantCulture,
                                                           DateTimeStyles.None,
                                                           out dt1))
                                    {
                                        if (onLoadMyRoster.StaffName.Length == 0)
                                            onLoadMyRoster.StaffName = reader.GetString(3);
                                        int days = DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1);
                                        long iMonthStart = MyGlobal.ToEpochTime(dt1) + 19800;
                                        for (int i = 0; i < days; i++)
                                        {
                                            if (!reader.IsDBNull(i * 2 + 6))
                                            {
                                                MyEvent myEvent = new MyEvent();
                                                myEvent.id = reader.GetInt16(0) + (i + 1) + lIDAdvancer + 1000;
                                                //myEvent.title = reader.IsDBNull(i*2 + 6) ? "" : reader.GetString(i*2 + 6);//  (reader.GetChar(i + 8) - 0x30) > 4 ? "x" : sCodes[(reader.GetChar(i + 8) - 0x30)];
                                                //myEvent.title = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400)).ToString("HH:mm");
                                                //myEvent.title +=" - "+ MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("HH:mm");

                                                myEvent.title = reader.IsDBNull(i * 2 + 6) ? "" : reader.GetString(i * 2 + 6);
                                                //  2018-11-07T18:00
                                                //  https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
                                                myEvent.start = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400)).ToString("yyyy-MM-dd HH:mm:ss"); //yyyy-MM-ddTHH:mm
                                                //myEvent.end = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("yyyy-MM-dd HH:mm:ss");
                                                myEvent.allDay = false;
                                                if (reader.IsDBNull(i * 2 + 6 + 1))
                                                {
                                                    myEvent.className = "event-default";

                                                }
                                                else
                                                {
                                                    //[ event-blue | event-azure | event-green | event-orange | event-red ]
                                                    int iIdx = reader.GetInt16((i * 2) + 6 + 1);
                                                    if (iIdx == 1)
                                                    {
                                                        myEvent.className = "event-orange";
                                                    }
                                                    else if (iIdx == 2)
                                                    {
                                                        myEvent.className = "event-default";
                                                    }
                                                    else if ((iIdx == C_APPROVED) || (iIdx == C_REVOKE_PENDING))
                                                    {
                                                        myEvent.className = "event-green";
                                                    }
                                                    else
                                                    {
                                                        myEvent.className = "event-default";
                                                    }
                                                }

                                                /*
                                                if (reader.IsDBNull(i + 6))
                                                {
                                                    myEvent.className = "event-default";
                                                }
                                                else
                                                {   //[ event-blue | event-azure | event-green | event-orange | event-red ]
                                                    if (reader.GetString(i + 6).Equals(MyGlobal.WORKDAY_MARKER))
                                                    {
                                                        myEvent.className = "event-green";
                                                    }
                                                    else if (reader.GetString(i + 6).Equals("OFF"))
                                                    {
                                                        myEvent.className = "event-default";
                                                    }
                                                    else
                                                    {
                                                        myEvent.className = "event-red";
                                                    }
                                                }
                                                */
                                                onLoadMyRoster.oEvents.Add(myEvent);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //--------------------Load Current working tables
                sSQL = @"select * from " + MyGlobal.activeDB + ".tbl_rosters_report where " +
                "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                "and m_Year='" + year + "' " +
                "and m_Month='" + month + "' ";

                onLoadMyRoster.StaffID = staffid;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    DateTime dt;
                                    String sMonth = "";
                                    int iMonth = 0;
                                    iMonth = MyGlobal.GetInt16(month) + 1;
                                    if (iMonth > 9) sMonth = iMonth.ToString(); else sMonth = "0" + iMonth;

                                    //DateTime dt;
                                    if (DateTime.TryParseExact(year + "-" + sMonth + "-" + "01",
                                                           "yyyy-MM-dd",
                                                           CultureInfo.InvariantCulture,
                                                           DateTimeStyles.None,
                                                           out dt))
                                    {
                                        //if (onLoadMyRoster.StaffName.Length == 0)
                                        //onLoadMyRoster.StaffName = reader.GetString(3);
                                        int days = DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1);
                                        long iMonthStart = MyGlobal.ToEpochTime(dt) + 19800;
                                        for (int i = 0; i < days; i++)
                                        {
                                            if (!reader.IsDBNull(i + 8))
                                            {
                                                MyEvent myEvent = new MyEvent();
                                                myEvent.id = reader.GetInt32(0) + (i + 1) + lIDAdvancer + 2000;
                                                myEvent.date = MyGlobal.ToDateTimeFromEpoch(lMonthStart + ((i) * 86400)).ToString("dd-MM-yyyy");
                                                myEvent.staffid = staffid;

                                                //myEvent.title = reader.IsDBNull(i*2 + 6) ? "" : reader.GetString(i*2 + 6);//  (reader.GetChar(i + 8) - 0x30) > 4 ? "x" : sCodes[(reader.GetChar(i + 8) - 0x30)];
                                                //myEvent.title = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400)).ToString("HH:mm");
                                                //myEvent.title +=" - "+ MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("HH:mm");
                                                //myEvent.title = MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(i + 8)).ToString("HH:mm:ss");
                                                string sIn = "", sOut = "";
                                                Int32 int32Span = 0;
                                                /*
                                                myEvent.title = "<span style='font-size:x-small;'>" +
                                                    GetInOutSpan(profile, staffid, iMonthStart, i, lShiftStart, lShiftEnd, out sIn, out sOut, out int32Span) +
                                                    " (" + MyGlobal.ToDateTimeFromEpoch(int32Span).ToString("HH:mm") + ")" +
                                                    "</span>";
                                                    */
                                                //" (" + MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(i + 8)).ToString("HH:mm") + ")" +
                                                myEvent.In = sIn;
                                                myEvent.Out = sOut;
                                                //  2018-11-07T18:00
                                                //  https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
                                                myEvent.start = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400)).ToString("yyyy-MM-dd HH:mm:ss"); //yyyy-MM-ddTHH:mm
                                                //myEvent.end = MyGlobal.ToDateTimeFromEpoch(iMonthStart + (i * 86400) + reader.GetInt32(40)).ToString("yyyy-MM-dd HH:mm:ss");
                                                myEvent.allDay = false;
                                                myEvent.className = "event-red";
                                                if (sIn.Length > 0) onLoadMyRoster.oEvents.Add(myEvent);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //---------------------------------------------------------
                onLoadMyRoster.status = true;
            }
            catch (MySqlException ex)
            {
                onLoadMyRoster.result = ex.Message;
            }
            catch (Exception ex)
            {
                onLoadMyRoster.result = ex.Message;
            }
            return Json(onLoadMyRoster, JsonRequestBehavior.AllowGet);
        }
        private string GetInOutSpan(string profile, string staffid, long iMonthStart,
            int day, long lShiftStart, long lShiftEnd, out string sIn, out string sOut, out Int32 int32Span)
        {   // day is zero indexed
            sIn = "";
            sOut = "";
            int32Span = 0;
            string sSQL = "select min(m_ActivityTime),max(m_ActivityTime) " +
                "from " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
    "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
    "and m_ActivityTime >= " + (iMonthStart + (day * 86400) + lShiftStart - 19800) + " " +
    "and m_ActivityTime < " + (iMonthStart + (day * 86400) + lShiftEnd - 19800 + 10);

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
                                Int32 int32In = -1;
                                Int32 int32Out = -1;
                                int32In = reader.IsDBNull(0) ? -1 : reader.GetInt32(0) + 19800;
                                int32Out = reader.IsDBNull(1) ? -1 : reader.GetInt32(1) + 19800;
                                if (int32In != -1) sIn = MyGlobal.ToDateTimeFromEpoch(int32In).ToString("HH:mm");
                                if (int32Out != -1) sOut = MyGlobal.ToDateTimeFromEpoch(int32Out).ToString("HH:mm");
                                if (int32In != -1 && int32Out != -1)
                                {
                                    int32Span = int32Out - int32In;
                                }
                                if (sIn.Equals(sOut))
                                    return sIn + "-" + "***";
                                else
                                    return sIn + "-" + sOut;
                            }
                        }
                    }
                }
            }
            return "...";
        }
        /* ----------------------------------- LoadMyRoster END ------------------------------- */
        [HttpPost]
        public ActionResult LoadFixedArrays(string profile, string band)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var fixedArrayResponse = new FixedArrayResponse();
            fixedArrayResponse.status = false;
            fixedArrayResponse.result = "";
            fixedArrayResponse.sarTitles.Add(null);
            fixedArrayResponse.sarRolls.Add(null);
            fixedArrayResponse.sarTeams.Add(null);
            fixedArrayResponse.sarBases.Add(null);
            fixedArrayResponse.sarBands.Add(null);
            fixedArrayResponse.sarGrades.Add(null);
            fixedArrayResponse.sarPayscales.Add(null);
            fixedArrayResponse.sarBanks.Add(null);
            //fixedArrayResponse.sarRosterOptions.Add(null);
            try
            {
                string sSQL = "";
                /*
                sSQL = @"select 'desig',m_Name from " + MyGlobal.activeDB + ".tbl_misc_titles " +
                    "union select 'roll',m_Name from " + MyGlobal.activeDB + ".tbl_misc_rolls " +
                    "union select 'team',m_Name from " + MyGlobal.activeDB + ".tbl_misc_teams " +
                    "union select 'base',m_Name from " + MyGlobal.activeDB + ".tbl_misc_bases " +
                    "union select 'band',m_Name from " + MyGlobal.activeDB + ".tbl_misc_bands " +
                    "union select 'grade',m_Name from " + MyGlobal.activeDB + ".tbl_misc_grades " +
                    "where m_Profile='" + profile + "' order by m_Name;";
*/
                string sPayScaleBit = "select 'payscale',x.m_Name,x.m_Profile,m_Amount,x.m_Key from " + MyGlobal.activeDB + ".tbl_payscale_master_list x " +
                "left join " + MyGlobal.activeDB + ".tbl_payscale_master y on x.m_Name = y.m_Name and x.m_Key=y.m_Key and y.m_Profile = '" + profile + "' and y.m_Ledger = 'CTC' " +
                "where x.m_Profile = '" + profile + "' order by x.m_Name asc,x.m_Key desc";
                // group by x.m_Name

                //Commented on 27-05-2024 for testing purpose band issues Starts and beloline are commented
                //sSQL = "" +
                //"select 'desig',m_Name,'','' from(select 'desig',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile = '" + profile + "' order by m_Name) as desig " +
                //"union all select 'roll',m_Name,'','' from(select 'roll',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile = '" + profile + "' order by m_Name) as roll " +
                //"union all select 'team',m_Name,'','' from(select 'team',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile = '" + profile + "' order by m_Name) as team " +
                //"union all select 'base',m_Name,'','' from(select 'base',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile = '" + profile + "' order by m_Name) as base " +
                //"union all select 'band',m_Name,'','' from(select 'band',m_Name,m_Profile,m_Order from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile = '" + profile + "' order by m_Order asc) as band  " +
                //"union all select 'grade',m_Name,'','' from(select 'grade',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_grades where m_Profile = '" + profile + "' and m_Band='" + band + "' order by m_Order) as grade " +
                //"union all select 'payscale',m_Name,m_Amount,m_Key from(" + sPayScaleBit + ") as payscale " +
                //"union all select 'bank',m_Name,m_Branch,m_IFSC from(select 'bank',m_Name,m_Branch,m_IFSC from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile = '" + profile + "' and m_Name!='new' order by m_Name) as bank order by desig,m_Name ";
                //Commented Lines Ends

                /////"union all select 'rosteroption',m_Name,'','' from(select 'rosteroption',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions where m_Profile = '" + profile + "' order by m_Order) as rosteroption " +


                //Starts Band Related Issue Testing on 27-05-2024 by Sivaguru M CHC1704
                sSQL = "" +
                "select 'desig',m_Name,'','' from(select 'desig',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile = '" + profile + "' order by m_Name) as desig " +
                "union all select 'roll',m_Name,'','' from(select 'roll',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile = '" + profile + "' order by m_Name) as roll " +
                "union all select 'team',m_Name,'','' from(select 'team',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile = '" + profile + "' order by m_Name) as team " +
                "union all select 'base',m_Name,'','' from(select 'base',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile = '" + profile + "' order by m_Name) as base " +

                //"union all select 'grade',m_Name,'','' from(select 'grade',m_Name,m_Profile from " + MyGlobal.activeDB + ".tbl_misc_grades where m_Profile = '" + profile + "' and m_Band='" + band + "' order by m_Order desc) as grade " +
                "union all select 'payscale',m_Name,m_Amount,m_Key from(" + sPayScaleBit + ") as payscale " +
                "union all select 'bank',m_Name,m_Branch,m_IFSC from(select 'bank',m_Name,m_Branch,m_IFSC from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile = '" + profile + "' and m_Name!='new' order by m_Name) as bank order by desig,m_Name ";

                //"union all select 'band',m_Name,'','' from(select 'band',m_Name,m_Profile,m_Order from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile = '" + profile + "' order by m_Order asc) as band  " +
                //Ends Band Related Issue Testing

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (reader.GetString(0).Equals("desig")) fixedArrayResponse.sarTitles.Add(reader.GetString(1));
                                    if (reader.GetString(0).Equals("roll")) fixedArrayResponse.sarRolls.Add(reader.GetString(1));
                                    if (reader.GetString(0).Equals("team")) fixedArrayResponse.sarTeams.Add(reader.GetString(1));
                                    if (reader.GetString(0).Equals("base")) fixedArrayResponse.sarBases.Add(reader.GetString(1));
                                    //if (reader.GetString(0).Equals("band")) fixedArrayResponse.sarBands.Add(reader.GetString(1));//on 27-05-2024
                                    //if (reader.GetString(0).Equals("grade")) fixedArrayResponse.sarGrades.Add(reader.GetString(1));//on 29-05-2024
                                    //if (reader.GetString(0).Equals("rosteroption")) fixedArrayResponse.sarRosterOptions.Add(reader.GetString(1));
                                    if (reader.GetString(0).Equals("payscale"))
                                    {
                                        PayscalDropdownItem item = new PayscalDropdownItem();
                                        item.m_Name = reader.GetString(1);
                                        item.m_Amount = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                        item.m_Key = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                                        fixedArrayResponse.sarPayscales.Add(item);
                                    }
                                    if (reader.GetString(0).Equals("bank"))
                                    {
                                        Bank item = new Bank();
                                        item.m_Name = reader.GetString(1);
                                        item.m_Branch = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        item.m_IFSC = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                        fixedArrayResponse.sarBanks.Add(item);
                                    }
                                }
                            }
                            //fixedArrayResponse.status = true;
                        }
                    }
                }

                //Starts Adding Grade Related Query by Sivaguru M CHC1704 on 27-05-2024
                string sSQLGrade = "select 'grade',m_Name,'','' from(select 'grade', m_Name, m_Profile from " + MyGlobal.activeDB + ".tbl_misc_grades where m_Profile = '" + profile + "' and m_Band = '" + band + "') as grade order by m_Name desc";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLGrade, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    if (reader.GetString(0).Equals("grade")) fixedArrayResponse.sarGrades.Add(reader.GetString(1));


                                }
                            }
                        }
                        //fixedArrayResponse.status = true;
                    }
                }

                //Ends Grade Related Query

                //Starts Adding Band Related Query by Sivaguru M CHC1704 on 27-05-2024
                string sSQLBand = "select 'band',m_Name,'','' from(select 'band',m_Name,m_Profile,m_Order from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile = '" + profile + "' order by m_Order asc) as band";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLBand, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    if (reader.GetString(0).Equals("band")) fixedArrayResponse.sarBands.Add(reader.GetString(1));//on 25-05-2024


                                }
                            }
                        }
                        fixedArrayResponse.status = true;
                    }
                }




                //Ends Adding Band Related Query

            }
            catch (MySqlException ex)
            {
                fixedArrayResponse.result = ex.Message;
            }
            return Json(fixedArrayResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LoadAccessManagerLog(string profile,
            string staffid, string year, string month, string day,
            string roster, string shift)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";

            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            double epochDayStart = ((new TimeSpan(new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month) + 1, MyGlobal.GetInt16(day)).Ticks)) - epochTicks).TotalSeconds - 19800;
            double epochDayEnd = epochDayStart + 86400;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //---------------------------------------------------------------
                    long lShiftStart = 0, lShiftEnd = 0;
                    string sSQL = "select m_ShiftStartTime,m_ShiftEndTime from " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' and m_RosterName='" + roster + "' " +
                        "and m_ShiftName='" + shift + "' and m_Year='" + year + "' and m_Month='" + month + "' " +
                        "and m_StaffID is null;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) lShiftStart = reader.GetInt64(0);
                                    if (!reader.IsDBNull(1)) lShiftEnd = reader.GetInt64(1);
                                }
                            }
                        }
                    }
                    double epochShiftStart = epochDayStart + lShiftStart;
                    double epochShiftEnd = epochDayStart + lShiftEnd;
                    //---------------------------------------------------------------
                    sSQL = @"select from_unixtime(m_ActivityTime),m_HardwareID,m_Activity,m_WorkTime,m_ReasonHead,m_ReasonNote,m_IP,m_ActivityTime from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
    "and m_ActivityTime >= '" + epochDayStart + "' and m_ActivityTime<'" + epochDayEnd + "' order by m_ActivityTime desc;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                postResponse.result = "<table style='border-collapse: collapse;width:100%;padding:0px;border:1px solid #999;background-color:#e8e8e8;'>";
                                postResponse.result += "<tr><td style='background-color:#aaa;color:#fff;text-align:center;'>Activity Time</td><td style='background-color:#aaa;color:#fff;text-align:center;'>HardwareID</td><td style='background-color:#aaa;color:#fff;text-align:center;'>Activity</td><td style='background-color:#aaa;color:#fff;text-align:center;'>Duration</td><td style='background-color:#aaa;color:#fff;text-align:center;'>Reason</td><td style='background-color:#aaa;color:#fff;text-align:center;'>Note</td><td style='background-color:#aaa;color:#fff;text-align:center;'>IP";
                                postResponse.result += "</td></tr>";

                                while (reader.Read())
                                {
                                    postResponse.result += "<tr style='border-bottom:0.5px solid #fff;";
                                    long lActiveTime = reader.GetInt32(7);
                                    if ((lActiveTime >= epochShiftStart) && (lActiveTime < epochShiftEnd))
                                    {
                                        postResponse.result += "background-color:#ffebcc;";
                                    }
                                    postResponse.result += "'>";
                                    for (int i = 0; i <= 6; i++)
                                    {
                                        if (reader.IsDBNull(i))
                                        {
                                            postResponse.result += "<td style='padding:0; margin:0;'></td>";
                                        }
                                        else
                                        {
                                            postResponse.result += "<td style='padding:0; margin:0;border:0.5px solid #ccc;border-bottom:0.5px solid #fff;padding-left:6px;padding-right:3px;";
                                            if (i == 0) postResponse.result += "white-space: nowrap;";
                                            if (i == 3)
                                            {
                                                if ((lActiveTime >= epochShiftStart) && (lActiveTime < epochShiftEnd))
                                                {
                                                    if (reader.GetString(2).Equals("lock") || reader.GetString(2).Equals("forcedlock") || reader.GetString(2).Equals("approved"))
                                                    {
                                                        postResponse.result += "background-color:#ffb380;";
                                                    }
                                                }
                                                postResponse.result += "text-align:right;";
                                                postResponse.result += "'>";
                                                postResponse.result += MyGlobal.getFormattedTimeFromSecond(reader.GetInt32(i)) + "</td>";
                                            }
                                            else
                                            {
                                                postResponse.result += "'>" + reader.GetString(i) + "</td>";
                                            }
                                        }
                                    }
                                    postResponse.result += "</tr>";
                                }
                                postResponse.result += "</table>";
                            }
                            postResponse.status = true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = ex.Message;
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        private bool GetStaffInformation(MySqlConnection con, string profile,
            string staffid, out string StaffName, out string Mrs)
        {
            StaffName = "";
            Mrs = "";
            string sSQL = "SELECT m_FName,m_Mrs FROM " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' and m_StaffID='" + staffid + "';";
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
                                StaffName = reader.GetString(0);
                                Mrs = reader.GetString(1);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        private void CreateMaxLeaves(MySqlConnection con, string profile, string year, string staffid)
        {
            string sSQL = "";
            sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_leave (m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Time,m_Description) values ('" + profile + "','" + year + "','" + staffid + "','CL','6',Now(),'Opening Balance');";
            sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_leave (m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Time,m_Description) values ('" + profile + "','" + year + "','" + staffid + "','SL','6',Now(),'Opening Balance');";
            sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_leave (m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Time,m_Description) values ('" + profile + "','" + year + "','" + staffid + "','PL','15',Now(),'Opening Balance');";
            sSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_leave (m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Time,m_Description) values ('" + profile + "','" + year + "','" + staffid + "','LOP','12',Now(),'Opening Balance');";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
        }

        public static bool GetSumOfDrCrFromLeave_and_leavesTable(MySqlConnection con, ref LoadLeaveDataResponse loadLeaveDataResponse,
            string profile, string year, string staffid)
        {
            bool bGotData = false;
            //------------------------Cr Db from leave table
            string sSQL = "SELECT m_Type,IFNULL(sum(m_Cr),0) as Cr,IFNULL(sum(m_Dr),0) as Dr FROM " + MyGlobal.activeDB + ".tbl_leave " +
                "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "' and m_Year='" + year + "'" +
                "group by m_Type;";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        bGotData = true;
                        while (reader.Read())
                        {
                            if (reader.GetString(0).Equals("CL")) { loadLeaveDataResponse.leaves.CL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.CL.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("SL")) { loadLeaveDataResponse.leaves.SL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.SL.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("PL")) { loadLeaveDataResponse.leaves.PL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.PL.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("APL")) { loadLeaveDataResponse.leaves.APL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.APL.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("LOP")) { loadLeaveDataResponse.leaves.LOP.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.LOP.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("ALOP")) { loadLeaveDataResponse.leaves.ALOP.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.ALOP.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("MatL")) { loadLeaveDataResponse.leaves.MatL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.MatL.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("PatL")) { loadLeaveDataResponse.leaves.PatL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.PatL.sumDr = reader.GetDouble(2); }
                        }
                    }
                }
            }
            sSQL = "SELECT m_Type,IFNULL(sum(m_Cr),0) as Cr,IFNULL(sum(m_Dr),0) as Dr FROM " + MyGlobal.activeDB + ".tbl_leave " +
                "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "' " +
                "group by m_Type;";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        bGotData = true;
                        while (reader.Read())
                        {
                            //if (reader.GetString(0).Equals("CL")) { loadLeaveDataResponse.leaves.CL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.CL.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("SL")) { loadLeaveDataResponse.leaves.SL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.SL.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("PL")) { loadLeaveDataResponse.leaves.PL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.PL.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("APL")) { loadLeaveDataResponse.leaves.APL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.APL.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("LOP")) { loadLeaveDataResponse.leaves.LOP.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.LOP.sumDr = reader.GetDouble(2); }
                            //if (reader.GetString(0).Equals("ALOP")) { loadLeaveDataResponse.leaves.ALOP.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.ALOP.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("MatL")) { loadLeaveDataResponse.leaves.MatL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.MatL.sumDr = reader.GetDouble(2); }
                            if (reader.GetString(0).Equals("PatL")) { loadLeaveDataResponse.leaves.PatL.sumCr = reader.GetDouble(1); loadLeaveDataResponse.leaves.PatL.sumDr = reader.GetDouble(2); }
                        }
                    }
                }
            }
            //-------------Get approved from leaves table
            sSQL = "SELECT m_id ";
            for (int i = 1; i <= 31; i++)
            {
                foreach (string leaveCode in sarLeaveCodes)
                {
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and (m_Status" + i + "='7' or m_Status" + i + "='9') then 1 else 0 End) as `Confirmed_" + i + "_" + leaveCode + "`";
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and m_Status" + i + "='1' then 1 else 0 End) as `Pending_" + i + "_" + leaveCode + "`";
                }
            }
            sSQL += " FROM " + MyGlobal.activeDB + ".tbl_leaves leav where m_Profile = '" + profile + "' " +
                "and m_Year = '" + year + "'  " +
                "and m_StaffID='" + staffid + "'";

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
                                foreach (string leaveCode in sarLeaveCodes)
                                {
                                    int ordinal = reader.GetOrdinal("Confirmed_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal);
                                        if (leaveCode.Equals("/CL") || leaveCode.Equals("CL/")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal) / 2;
                                        //if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal);
                                        //if (leaveCode.Equals("/SL") || leaveCode.Equals("SL/")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal) / 2;

                                        //if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.used += reader.GetInt16(ordinal);

                                        if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/LOP") || leaveCode.Equals("LOP/")) loadLeaveDataResponse.leaves.LOP.used += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/ALOP") || leaveCode.Equals("ALOP/")) loadLeaveDataResponse.leaves.ALOP.used += reader.GetDouble(ordinal) / 2;

                                        //if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.used += reader.GetInt16(ordinal);
                                    }

                                    ordinal = reader.GetOrdinal("Pending_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/CL") || leaveCode.Equals("CL/")) loadLeaveDataResponse.leaves.CL.pending += reader.GetDouble(ordinal) / 2;
                                        //if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/SL") || leaveCode.Equals("SL/")) loadLeaveDataResponse.leaves.SL.pending += reader.GetDouble(ordinal) / 2;
                                        //if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/LOP") || leaveCode.Equals("LOP/")) loadLeaveDataResponse.leaves.LOP.pending += reader.GetDouble(ordinal) / 2;

                                        if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/ALOP") || leaveCode.Equals("ALOP/")) loadLeaveDataResponse.leaves.ALOP.pending += reader.GetDouble(ordinal) / 2;
                                        //if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.pending += reader.GetInt16(ordinal);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //----------------------------------------
            sSQL = "SELECT m_id ";
            for (int i = 1; i <= 31; i++)
            {
                foreach (string leaveCode in sarLeaveCodes)
                {
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and (m_Status" + i + "='7' or m_Status" + i + "='9') then 1 else 0 End) as `Confirmed_" + i + "_" + leaveCode + "`";
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and m_Status" + i + "='1' then 1 else 0 End) as `Pending_" + i + "_" + leaveCode + "`";
                }
            }
            sSQL += " FROM " + MyGlobal.activeDB + ".tbl_leaves leav where m_Profile = '" + profile + "' " +
                "and m_StaffID='" + staffid + "'";

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
                                foreach (string leaveCode in sarLeaveCodes)
                                {
                                    int ordinal = reader.GetOrdinal("Confirmed_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        //if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal);
                                        //if (leaveCode.Equals("/CL") || leaveCode.Equals("CL/")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal);
                                        if (leaveCode.Equals("/SL") || leaveCode.Equals("SL/")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal) / 2;

                                        if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.used += reader.GetInt16(ordinal);

                                        //if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/LOP") || leaveCode.Equals("LOP/")) loadLeaveDataResponse.leaves.LOP.used += reader.GetDouble(ordinal) / 2;
                                        //if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/ALOP") || leaveCode.Equals("ALOP/")) loadLeaveDataResponse.leaves.ALOP.used += reader.GetDouble(ordinal) / 2;

                                        if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.used += reader.GetInt16(ordinal);
                                    }

                                    ordinal = reader.GetOrdinal("Pending_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        //if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/CL") || leaveCode.Equals("CL/")) loadLeaveDataResponse.leaves.CL.pending += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/SL") || leaveCode.Equals("SL/")) loadLeaveDataResponse.leaves.SL.pending += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/LOP") || leaveCode.Equals("LOP/")) loadLeaveDataResponse.leaves.LOP.pending += reader.GetDouble(ordinal) / 2;

                                        //if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("/ALOP") || leaveCode.Equals("ALOP/")) loadLeaveDataResponse.leaves.ALOP.pending += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.pending += reader.GetInt16(ordinal);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //----------------------------------------
            return bGotData;
        }
        /*
        private bool GetMonthRosterView(MySqlConnection con, ref LoadLeaveDataResponse loadLeaveDataResponse,
            string profile, string staffid, string year, string month)
        {
            bool bRet = false;
            //-----------------------------------------Get leaves utilized
            string sSQL = "SELECT m_id ";
            for (int i = 1; i <= 31; i++)
            {
                foreach (string leaveCode in sarLeaveCodes)
                {
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and m_Status" + i + "='9' then 1 else 0 End) as `Confirmed_" + i + "_" + leaveCode + "`";
                    sSQL += ",sum(case When m_DayL" + i + " = '" + leaveCode + "' and m_Status" + i + "='1' then 1 else 0 End) as `Pending_" + i + "_" + leaveCode + "`";
                }
            }
            sSQL += " FROM " + MyGlobal.activeDB + ".tbl_leaves leav where m_Profile = '" + profile + "' " +
                "and m_Year = '" + year + "'  and m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' " +
                "and m_StaffID='" + staffid + "'  group by m_id";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        bRet = true;
                        if (reader.Read())
                        {
                            for (int i = 1; i <= 31; i++)
                            {
                                foreach (string leaveCode in sarLeaveCodes)
                                {
                                    int ordinal = reader.GetOrdinal("Confirmed_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal);
                                        if (leaveCode.Equals("/CL")) loadLeaveDataResponse.leaves.CL.used += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal);
                                        if (leaveCode.Equals("/SL")) loadLeaveDataResponse.leaves.SL.used += reader.GetDouble(ordinal) / 2;

                                        if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("COff")) loadLeaveDataResponse.leaves.COff.used += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("AWOff")) loadLeaveDataResponse.leaves.AWOff.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.used += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.used += reader.GetInt16(ordinal);
                                    }
                                    ordinal = reader.GetOrdinal("Pending_" + i + "_" + leaveCode);
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        if (leaveCode.Equals("CL")) loadLeaveDataResponse.leaves.CL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/CL")) loadLeaveDataResponse.leaves.CL.pending += reader.GetDouble(ordinal) / 2;
                                        if (leaveCode.Equals("SL")) loadLeaveDataResponse.leaves.SL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("/SL")) loadLeaveDataResponse.leaves.SL.pending += reader.GetDouble(ordinal) / 2;

                                        if (leaveCode.Equals("PL")) loadLeaveDataResponse.leaves.PL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("APL")) loadLeaveDataResponse.leaves.APL.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("COff")) loadLeaveDataResponse.leaves.COff.pending += reader.GetInt16(ordinal);
                                        //if (leaveCode.Equals("AWOff")) loadLeaveDataResponse.leaves.AWOff.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("LOP")) loadLeaveDataResponse.leaves.LOP.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("ALOP")) loadLeaveDataResponse.leaves.ALOP.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("MatL")) loadLeaveDataResponse.leaves.MatL.pending += reader.GetInt16(ordinal);
                                        if (leaveCode.Equals("PatL")) loadLeaveDataResponse.leaves.PatL.pending += reader.GetInt16(ordinal);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return bRet;
        }
        */
        [HttpPost]
        public ActionResult LoadLeaveData(string profile, string year, string month, string staffid, string sRetMessage)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadLeaveDataResponse = new LoadLeaveDataResponse();
            loadLeaveDataResponse.status = false;
            loadLeaveDataResponse.result = "";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------------Get Staff Name
                    string sStaffName = "", Mrs;
                    if (!GetStaffInformation(con, profile, staffid, out sStaffName, out Mrs))
                    {
                        loadLeaveDataResponse.result = "StaffID does not exists";
                        return Json(loadLeaveDataResponse, JsonRequestBehavior.AllowGet);
                    }
                    loadLeaveDataResponse.StaffName = sStaffName;
                    loadLeaveDataResponse.Mrs = Mrs;
                    if (!GetSumOfDrCrFromLeave_and_leavesTable(con, ref loadLeaveDataResponse, profile, year, staffid))
                    {
                        //CreateMaxLeaves(con, profile, year, staffid);
                        //GetSumOfDrCrLeaves(con, ref loadLeaveDataResponse, profile, year, staffid);
                    }
                    /*
                    if (!GetMonthRosterView(con, ref loadLeaveDataResponse, profile, staffid, year, month))
                    {
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_leaves (m_StaffID,m_StaffName,m_Year,m_Month,m_Profile) values ('" + staffid + "','" + sStaffName + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + profile + "');";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                        GetMonthRosterView(con, ref loadLeaveDataResponse, profile, staffid, year, month);
                    }
                    */
                    //--------------------Get leave status to display in BAR
                    int iHasRows = 0;
                    AgainTryPlease:
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_leaves lev " +
"left join " + MyGlobal.activeDB + ".tbl_rosters roster on roster.m_StaffID = lev.m_StaffID and roster.m_Profile = '" + profile + "' and roster.m_Year = '" + year + "' and roster.m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' " +
"left join " + MyGlobal.activeDB + ".tbl_holidays holiday on holiday.m_Profile = roster.m_Profile and holiday.m_Year = roster.m_Year and holiday.m_Month = roster.m_Month " +
"where lev.m_Profile = '" + profile + "' and lev.m_Year = '" + year + "' and lev.m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' and lev.m_StaffID = '" + staffid + "' ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (iHasRows == 0) iHasRows = 1;
                                if (reader.Read())
                                {
                                    //  tbl_leaves
                                    //  tbl_roster
                                    //  tbl_holidays
                                    int ord_roster = reader.GetOrdinal("m_RosterName") + 6;
                                    int ord_holiday = reader.GetOrdinal("m_DayH1") - 3;
                                    for (int i = 1; i <= 31; i++)
                                    {
                                        int ordinal = reader.GetOrdinal("m_DayL" + i);
                                        LeaveItem item = new LeaveItem();
                                        if (reader.IsDBNull(ordinal))   // leave field is null
                                        {
                                            item.Code = "";
                                            item.Status = 0;// voic
                                            if (!reader.IsDBNull(ord_roster + i))
                                            {
                                                if (reader.GetString(ord_roster + i).Equals("OFF"))
                                                {
                                                    item.Code = "OFF";
                                                    item.Status = 8;    // Off
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Leave field has value
                                            item.Code = reader.GetString(ordinal);
                                            ordinal = reader.GetOrdinal("m_Status" + i);
                                            if (reader.IsDBNull(ordinal))
                                            {
                                                item.Status = 0;
                                            }
                                            else
                                            {
                                                item.Status = reader.GetInt16(ordinal);
                                            }
                                            if (item.Status == 0)
                                            {
                                                if (!reader.IsDBNull(ord_roster + i))
                                                {
                                                    if (reader.GetString(ord_roster + i).Equals("OFF"))
                                                    {
                                                        item.Code = "OFF";
                                                        item.Status = 8;    // Off
                                                    }
                                                }
                                            }
                                        }
                                        if (!reader.IsDBNull(ord_holiday + (i * 3)))
                                        {
                                            item.Code = reader.GetString(ord_holiday + (i * 3));
                                            item.Status = reader.IsDBNull(ord_holiday + (i * 3) + 1) ? 0 : reader.GetInt16(ord_holiday + (i * 3) + 1);
                                            item.Desc = reader.IsDBNull(ord_holiday + (i * 3) + 2) ? "" : reader.GetString(ord_holiday + (i * 3) + 2);
                                        }
                                        loadLeaveDataResponse.LeaveStatus.Add(item);
                                    }
                                }
                            }
                        }
                    }
                    if (iHasRows == 0)
                    {
                        iHasRows = 9;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_leaves " +
                            "(m_Profile,m_StaffID,m_Year,m_Month) values " +
                            "('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "');";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        goto AgainTryPlease;
                    }
                    loadLeaveDataResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                loadLeaveDataResponse.result = ex.Message;
            }
            if (sRetMessage != null)
                if (sRetMessage.Length > 0)
                    loadLeaveDataResponse.result = sRetMessage;
            return Json(loadLeaveDataResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------------------
        private string Get2Digits(string sIn)
        {
            if (sIn.Length == 1) return "0" + sIn;
            return sIn;
        }
        private string GetDisplayTime(DateTime dt)
        {
            if (dt.Date == DateTime.Today)
            {
                return dt.ToString("HH:mm:ss");
            }
            else
            {
                return dt.ToString("yyyy-MM-dd");
            }
        }
        private void SetOTStatus(MySqlConnection con, string profile, string session,
            int status, string email, string selectedemail, string mins)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();

            char[] delimiterChars = { '_' };    //ot_20000_2019_1_2_CHC_Night_145156
            string[] arData = session.Split(delimiterChars);
            string year = "", month = "", day = "", staffid = "";
            string roster = "", shift = "";
            if (arData.Length >= 8)
            {
                staffid = arData[1];
                year = arData[2];
                month = (MyGlobal.GetInt16(arData[3]) - 1) + "";
                day = arData[4];
                roster = arData[5];
                shift = arData[6];
            }
            if (arData.Length > 1)
            {
                staffid = arData[1];
            }
            // Accept it
            String sSQL = "";
            int iMins = MyGlobal.GetInt16(mins) * 60;
            try
            {
                //----------------------------------Update OT Table
                sSQL = "Update " + MyGlobal.activeDB + ".tbl_ot Set " +
                    "m_OTStatus=" + status + ",m_OTDuration='" + iMins + "' where " +
                "m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                "and m_Session='" + session + "';";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                //"and m_Year='" + year + "' and m_Month='" + month + "' and m_Day='" + day + "' " +
                //"and m_Roster='" + roster + "' and m_Shift='" + shift + "' " +
                //MyGlobal.Log("check 1 >" + watch.ElapsedMilliseconds);
                //------------------------------Create message
                string sEmailName = "";
                GetStaffDetails_FromEmail(con, profile, email, out sEmailName);
                //MyGlobal.Log("check 2 >" + watch.ElapsedMilliseconds);
                string message = "";
                if (status == 2) message = "<span style=''color:red''><b>Rejected</b></span>";
                if (status == 4) message = "<span style=''color:blue''><b>Accepted</b></span>";
                if (status == 9)
                {
                    message = "<span style=''color:darkgreen''><b>Approved by " + sEmailName + "</b></span>";
                    message += " <span style=''color:orangered''><b>(" + mins + " Minutes)</b></span>";
                }
                sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message," +
                    "m_Time,m_Session) " +
                    "values ('" + profile + "','" + "" + "','" + email + "','" + selectedemail + "'," +
                    "'" + message + "',Now(),'" + session + "');";
                sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
"Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
"and m_Session='" + session + "';";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                //MyGlobal.Log("check 3 >" + watch.ElapsedMilliseconds);
                //-----------------------------Update tbl_accessmanager_activity
                sSQL = "Update " + MyGlobal.activeDB + ".tbl_accessmanager_activity Set ";
                if (status == C_ACCEPTED) sSQL += "m_Activity='accepted' ";
                else if (status == C_APPROVED) sSQL += "m_Activity='approved' ";
                else if (status == C_REJECTED) sSQL += "m_Activity='rejected' ";
                else sSQL += "m_Activity='none' ";
                sSQL += ",m_ReasonHead='by " + sEmailName + "'";
                sSQL += ",m_WorkTime='" + iMins + "' ";
                sSQL += "where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                    "and m_StaffID='" + staffid + "';";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                //------------------------------------------------------------
                //MyGlobal.Log("check 4 >" + watch.ElapsedMilliseconds);
                MyGlobal.SendHubObject(selectedemail, GetPendingMessagesObject(con, profile, "times", selectedemail));
                //MyGlobal.Log("check 5 >" + watch.ElapsedMilliseconds);
            }
            catch (MySqlException ex)
            {

            }
        }
        public int GetNoOfDays(MySqlConnection con, string profile, string session)
        {
            int iRet = 0;
            string sSQL = "select m_Days from " + MyGlobal.activeDB + ".tbl_messages where m_Profile='" + profile + "' " +
            "and m_Session='" + session + "' " +
            "and m_Year is not null and m_Month is not null and m_Day is not null " +
            "and m_LeaveType is not null and m_LeaveStatus is not null " +
            "and m_StaffID is not null and m_Session is not null order by m_Time desc limit 1;";
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
                                if (reader.GetDouble(0) == 0.5) iRet = 1;
                                else
                                    iRet = reader.GetInt16(0);
                            }
                        }
                    }
                }
            }
            return iRet;
        }
        //--------------------------------------------------------------------------
        private int GetCounts(string profile, string email, string session)
        {
            int iCnt = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                //string sSQL = "select count(m_id) from " + MyGlobal.activeDB + ".tbl_messages where m_Profile = '" + profile + "' and m_Session='" + session + "' " +
                //"and (m_To = '" + email + "' and m_ToSeen is null)";
                string sSQL = "select count(m_Session) as cnt from " + MyGlobal.activeDB + ".tbl_messages_clubs where m_Profile = '" + profile + "' and m_Seen is null " +
"and m_Session in (SELECT m_Session FROM " + MyGlobal.activeDB + ".tbl_messages where m_Member = '" + email + "' and m_Profile = '" + profile + "' and m_Session='" + session + "')";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) iCnt = reader.GetInt16(0);
                            }
                        }
                    }
                }
            }
            return iCnt;
        }
        private int GetLeaveCodeAndStatus(string profile, string staffid, string year, string month, string day, ref string sLeaveCode)
        {
            int iRet = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                string sSQL = "select m_DayL" + day + ",m_Status" + day + " from " + MyGlobal.activeDB + ".tbl_leaves " +
                    "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' " +
                    "and m_Year='" + year + "' and m_Month='" + month + "';";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) sLeaveCode = reader.GetString(0);
                                if (!reader.IsDBNull(1)) iRet = reader.GetInt16(1);
                            }
                        }
                    }
                }
            }
            return iRet;
        }
        private double GetEndOfTheShift(MySqlConnection con, string profile,
            string year, string month, string day, string staffid, string roster, string shift)
        {
            string sSQL = "SELECT m_ShiftEndTime FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
"where roster.m_Profile = '" + profile + "' and roster.m_Year = '" + year + "' and roster.m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' and roster.m_StaffID = '" + staffid + "' " +
"and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) return reader.GetDouble(0);
                        }
                    }
                }
            }
            return 0;
        }
        public string GetIPAddress()
        {
            string IPAddress = "";
            IPHostEntry Host = default(IPHostEntry);
            string Hostname = null;
            Hostname = System.Environment.MachineName;
            Host = Dns.GetHostEntry(Hostname);
            foreach (IPAddress IP in Host.AddressList)
            {
                if (IP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    IPAddress = Convert.ToString(IP);
                }
            }
            return IPAddress;
        }
        [HttpPost]
        public ActionResult GetShiftDetails(string profile, string email, string staffid, string shift, string roster,
            string year, string month, string day, string otreason, string otrequestmins, string session,
            string mode)
        {
            var shiftDetailsResponse = new ShiftDetailsResponse();
            shiftDetailsResponse.status = false;
            shiftDetailsResponse.result = "";
            double dblStartTimeOfSelectedShift = 0;
            string sSQL = "";

            string sFName, sStaffEmail = "", sReportAdminEmail = "",
                sReportFuncEmail = "", sErrMessage = "";
            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            double epochDayStart = ((new TimeSpan(new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month), MyGlobal.GetInt16(day)).Ticks)) - epochTicks).TotalSeconds - 19800;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    ProcessWorkingHours(con, year, (MyGlobal.GetInt16(month) - 1) + "", staffid, profile, roster, shift);
                    //-----------------------------------------
                    if (mode.Equals("requestapproval") || mode.Equals("cancel") || mode.Equals("requestaccept"))
                    {
                        if (roster.Length == 0)
                        {
                            sErrMessage = "Unknown Roster";
                        }
                        if (shift.Length == 0)
                        {
                            sErrMessage = "Unknown Shift";
                        }
                        //-------------------Get Staff Details
                        if (sErrMessage.Length == 0)
                        {
                            sErrMessage = "";
                            GetStaffDetails_FromStaffID(con, profile, staffid,
                                out sFName, out sStaffEmail, out sReportAdminEmail,
                                out sReportFuncEmail, out sErrMessage);
                        }
                    }
                    if (mode.Equals("requestapproval"))
                    {
                        if (sErrMessage.Length == 0)
                        {
                            //-------------------Initiate Approval Process
                            if (session.Length == 0)
                                session = "ot_" + staffid + "_" + year + "_" + month + "_" + day + "_" +
                                roster + "_" + shift + "_" + DateTime.Now.ToString("HHmmss");
                            string message = "";
                            message += "<table class=''LveTbl''>";
                            message += "<tr class=''LveTR1''><td>Time Approval Request</td>";
                            //message += "<td class=''LveTD1''>" + leavetype + "</td>";
                            message += "<td class=''LveTD2''>" + year + "-" + MyGlobal.GetInt16(month) + "-" + day + "</td>" +
                                "<td><span class=''LveTD5''>" + otrequestmins + "</span> Mins</td></tr>";
                            message += "<tr class=''LveTR2''><td colspan=3>Shift <span class=''CHT''>" + shift + "</span> of Roster <span class=''CHT''>" + roster + "</span></td></tr>";
                            message += "</table>";
                            message += "<span class=''LveR''>" + otreason + "<span>";

                            sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
    "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
    "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated) values " +
    "('" + profile + "',1," +
    "'" + sStaffEmail + "','','" + staffid + "'," +
    "'" + sReportAdminEmail + "','',''," +
    "'" + session + "',Now(),Now());";


                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                                "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + day + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + roster + "','" + shift + "');";
                            //-------------------------------------
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                            if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                            if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                            //-------------------------------------
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_ot Set m_OTStatus=5 where m_Profile='" + profile + "' " +
                                "and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                                "and m_Day='" + day + "' and m_Roster='" + roster + "' and m_Shift='" + shift + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                            //--------------------------------------
                            HubObject hub = GetPendingMessagesObject(con, profile, "times", sReportAdminEmail);
                            SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                            //SendHubObject(sHeadEmail, GetPendingMessagesObject(con, profile, sHeadEmail));
                        }
                    }
                    else if (mode.Equals("requestaccept"))
                    {
                        if (sErrMessage.Length == 0)
                        {
                            //-------------------Initiate Approval Process
                            sSQL = "";
                            if (session.Length == 0)
                            {
                                session = "ot_" + staffid + "_" + year + "_" + month + "_" + day + "_" +
                                roster + "_" + shift + "_" + DateTime.Now.ToString("HHmmss");
                                sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
    "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
    "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated) values " +
    "('" + profile + "',1," +
    "'" + sStaffEmail + "','','" + staffid + "'," +
    "'" + sReportAdminEmail + "','',''," +
    "'" + session + "',Now(),Now());";
                            }

                            dblStartTimeOfSelectedShift = epochDayStart +
                                GetEndOfTheShift(con, profile, year, month, day, staffid, roster, shift);
                            //-------------------Message
                            string message = "";
                            message += "<table class=''LveTbl''>";
                            message += "<tr class=''LveTR1''><td>Time Requested</td>";
                            //message += "<td class=''LveTD1''>" + leavetype + "</td>";
                            message += "<td class=''LveTD2''>" + year + "-" + MyGlobal.GetInt16(month) + "-" + day + "</td>" +
                                "<td><span class=''LveTD5''>" + otrequestmins + "</span> Mins</td></tr>";
                            message += "<tr class=''LveTR2''><td colspan=3>Shift <span class=''CHT''>" + shift + "</span> of Roster <span class=''CHT''>" + roster + "</span></td></tr>";
                            message += "</table>";
                            message += "<span class=''LveR''>" + otreason + "<span>";
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                                "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + day + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                                "'" + message + "',Now(),'" + session + "','" + roster + "','" + shift + "');";
                            sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                                "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                                "and m_Session='" + session + "';";
                            //-------------------Update activity table
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "(m_Profile,m_StaffID,m_Activity,m_ActivityTime,m_WorkTime,m_Session,m_IP,m_HardwareID) " +
                                "values ('" + profile + "','" + staffid + "','requested'," +
                                "'" + (dblStartTimeOfSelectedShift - 10) + "','" + (MyGlobal.GetInt16(otrequestmins) * 60) + "'," +
                                "'" + session + "','" + GetIPAddress() + "','admin');";
                            //-------------------------------------
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                            if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                            if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                    "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                            //-------------------Update OT Table
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_ot (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_Roster,m_Shift,m_OTStatus,m_Session,m_OTDuration,m_Time,m_Manual) " +
                                "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + day + "'," +
                                "'" + roster + "','" + shift + "',1,'" + session + "','" + (MyGlobal.GetInt16(otrequestmins) * 60) + "',Now(),1);";

                            //------------------Update all
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                            //--------------------------------------
                            //SendHubObject(sHeadEmail, GetPendingMessagesObject(con, profile, sHeadEmail));
                            HubObject hub = GetPendingMessagesObject(con, profile, "times", sReportAdminEmail);
                            SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                        }
                    }
                    else if (mode.Equals("cancel"))
                    {
                        sSQL = "";
                        if (session.Length == 0)
                        {
                            session = "ot_" + staffid + "_" + year + "_" + month + "_" + day + "_" +
                            roster + "_" + shift + "_" + DateTime.Now.ToString("HHmmss");
                            sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                                "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                                "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated) values " +
                                "('" + profile + "',1," +
                                "'" + sStaffEmail + "','','" + staffid + "'," +
                                "'" + sReportAdminEmail + "','',''," +
                                "'" + session + "',Now(),Now());";
                        }

                        string message = "<span style=''color:red;''>Time Request Cancelled</span>";
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_OTRoster,m_OTShift) " +
                            "values ('" + profile + "','" + staffid + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + day + "','" + sStaffEmail + "','" + sReportAdminEmail + "'," +
                            "'" + message + "',Now(),'" + session + "','" + roster + "','" + shift + "');";
                        sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                            "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
                            "and m_Session='" + session + "';";
                        //-------------------------------------
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                        if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                        if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";


                        sSQL += "update " + MyGlobal.activeDB + ".tbl_accessmanager_activity Set " +
                            "m_Activity='cancelled' where m_Profile='" + profile + "' and " +
                            "m_Session='" + session + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                        //------------------------------------
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_ot where m_Profile='" + profile + "' " +
                                "and m_Year='" + year + "' " +
                                "and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                                "and m_Day='" + day + "' " +
                                "and m_StaffID='" + staffid + "' " +
                                "and m_Roster='" + roster + "' " +
                                "and m_Shift='" + shift + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                        //--------------------------------------
                        HubObject hub = GetPendingMessagesObject(con, profile, "times", sReportAdminEmail);
                        SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                        //SendHubObject(sHeadEmail, GetPendingMessagesObject(con, profile, sHeadEmail));
                    }
                    //-----------------------------------------
                    /*
                    sSQL = "SELECT m_RosterName, m_ShiftName, m_ShiftStartTime, m_ShiftEndTime,ot.m_OTStatus," +
                        "ot.m_Session,ot.m_OTDuration,min(ot.m_id),roster.m_Day" + day + " " +
                        "FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
"left join (SELECT * FROM " + MyGlobal.activeDB + ".tbl_ot where m_Manual=1 order by m_id desc) ot on ot.m_Profile = roster.m_Profile and ot.m_Year = roster.m_Year and ot.m_Month = roster.m_Month and ot.m_Day = '" + day + "' and ot.m_Roster = roster.m_RosterName and ot.m_Shift = roster.m_ShiftName " +
"where roster.m_Profile = '" + profile + "' and roster.m_Year = '" + year + "' and roster.m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' and roster.m_StaffID = '" + staffid + "' " +
"group by roster.m_id " +
"order by roster.m_RosterName,roster.m_ShiftName;";
*/
                    sSQL = "SELECT m_RosterName, m_ShiftName, m_ShiftStartTime, m_ShiftEndTime,ot.m_OTStatus," +
                        "ot.m_Session,ot.m_OTDuration,roster.m_Day" + day + " " + //min(ot.m_id),
                        "FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
"left join (SELECT m_OTStatus,m_Session,m_OTDuration,m_Profile,m_Year,m_Month,m_Day,m_Roster,m_Shift " +
"FROM " + MyGlobal.activeDB + ".tbl_ot " +
"where m_Manual=1 and m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' and m_Year = '" + year + "' " +
"and m_StaffID = '" + staffid + "' " +
"order by m_id desc limit 1) ot " +
"on ot.m_Profile = roster.m_Profile and ot.m_Year = roster.m_Year and ot.m_Month = roster.m_Month " +
"and ot.m_Day = '" + day + "' and ot.m_Roster = roster.m_RosterName and ot.m_Shift = roster.m_ShiftName " +
"where roster.m_Profile = '" + profile + "' and roster.m_Year = '" + year + "' " +
"and roster.m_Month = '" + (MyGlobal.GetInt16(month) - 1) + "' and roster.m_StaffID = '" + staffid + "' " +
"order by roster.m_RosterName,roster.m_ShiftName;";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(7)) // 7 is Roster Day
                                    {
                                        Roster_Shift roster_Shift = new Roster_Shift();
                                        roster_Shift.sRoster = reader.GetString(0);
                                        roster_Shift.sShift = reader.GetString(1);
                                        if (roster_Shift.sShift.Equals(shift, StringComparison.CurrentCultureIgnoreCase) &&
                                            roster_Shift.sRoster.Equals(roster, StringComparison.CurrentCultureIgnoreCase)
                                            )
                                        {
                                            //-----Used later, some time
                                            dblStartTimeOfSelectedShift = epochDayStart + reader.GetDouble(2);
                                        }
                                        /*
                                        long lTmeShiftBefor = GetActiveWorkingTimeForThisStaffThisShift(profile, roster_Shift.sRoster, roster_Shift.sShift, year, (MyGlobal.GetInt16(month) - 1) + "", MyGlobal.GetInt16(day), staffid, reader.GetInt64(2), reader.GetInt64(3), -1, epochDayStart);
                                        long lTmeShift = GetActiveWorkingTimeForThisStaffThisShift(profile, roster_Shift.sRoster, roster_Shift.sShift, year, (MyGlobal.GetInt16(month) - 1) + "", MyGlobal.GetInt16(day), staffid, reader.GetInt64(2), reader.GetInt64(3), 0, epochDayStart);
                                        long lTmeShiftAfter = GetActiveWorkingTimeForThisStaffThisShift(profile, roster_Shift.sRoster, roster_Shift.sShift, year, (MyGlobal.GetInt16(month) - 1) + "", MyGlobal.GetInt16(day), staffid, reader.GetInt64(2), reader.GetInt64(3), 1, epochDayStart);

                                        roster_Shift.sTmeShiftBefore = MyGlobal.GetHHMMSS(lTmeShiftBefor);
                                        roster_Shift.sTmeShift = MyGlobal.GetHHMMSS(lTmeShift);
                                        roster_Shift.sTmeShiftAfter = MyGlobal.GetHHMMSS(lTmeShiftAfter);
                                        
                                        roster_Shift.sTmeShiftWork = MyGlobal.GetHHMMSS(
                                            lTmeShiftBefor + lTmeShift + lTmeShiftAfter);
                                            */

                                        long lWorkhours = 0;
                                        GetStaffWorkHours(profile,
                                            (long)(epochDayStart + reader.GetDouble(2) + 19800), //row.shift_start
                                            (long)(epochDayStart + reader.GetDouble(3) + 19800), //row.shift_end, 
                                            staffid,
                                            out lWorkhours);
                                        roster_Shift.lWorktime = lWorkhours;

                                        roster_Shift.lShiftStartTime = reader.GetInt64(2);
                                        roster_Shift.lShiftEndTime = reader.GetInt64(3);
                                        //--------------------------------------------------OT Logic
                                        roster_Shift.lApplicable =
                                            (roster_Shift.lWorktime) - const_lShiftDuration;
                                        if (roster_Shift.lApplicable < 0)
                                        {
                                            roster_Shift.lApplicable = 0;
                                            roster_Shift.sApplicable = "OT not applicable";
                                        }
                                        else
                                        {
                                            roster_Shift.sApplicable = "OT Applicable";
                                        }
                                        //--------------------------------------------------OT Logic ENDS
                                        roster_Shift.otStatus = 0;
                                        if (!reader.IsDBNull(4)) roster_Shift.otStatus = reader.GetInt16(4);
                                        if (!reader.IsDBNull(5)) roster_Shift.session = reader.GetString(5);
                                        if (!reader.IsDBNull(6)) roster_Shift.otDuration = MyGlobal.GetHHMMSS(reader.GetInt16(6));
                                        shiftDetailsResponse.roster_Shifts.Add(roster_Shift);
                                        shiftDetailsResponse.status = true;
                                    }
                                }
                            }
                            else
                            {
                                shiftDetailsResponse.result = "Roster not yet created";
                            }
                        }
                    }
                    //--------------------------------------
                }
                if (sErrMessage.Length > 0) shiftDetailsResponse.result = sErrMessage;
            }
            catch (MySqlException ex1)
            {
                shiftDetailsResponse.result = ex1.Message;
            }

            return Json(shiftDetailsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetShiftActivityDetails(
            string profile, string staffid, string year, string month, string day,
            string roster, string shift, string mode, string classicview)
        {

            var hrActivitiesResponse = new HRActivitiesResponse();
            if (classicview.Equals("1"))
            {
                hrActivitiesResponse.status =
                    GetActivities_ClassicView(hrActivitiesResponse, profile, staffid,
                    MyGlobal.GetInt16(year),
                    MyGlobal.GetInt16(month),
                    MyGlobal.GetInt16(day), roster, shift);
            }
            else
            {
                hrActivitiesResponse.status =
                    GetActivities_AdvanceView(hrActivitiesResponse, profile, staffid,
                    MyGlobal.GetInt16(year),
                    MyGlobal.GetInt16(month),
                    MyGlobal.GetInt16(day), roster, shift);
            }

            return Json(hrActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadOTData(string profile, string staffid, string year, string month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadOTResponse = new LoadOTResponse();
            loadOTResponse.status = false;
            loadOTResponse.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int iYear = MyGlobal.GetInt16(year);
                    int iMonth = MyGlobal.GetInt16(month);
                    int lastday = DateTime.DaysInMonth(iYear, iMonth);
                    TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                    double epochMonthStart = ((new TimeSpan(new DateTime(iYear, iMonth, 1).Ticks)) - epochTicks).TotalSeconds - 19800;
                    double epochMonthEnd = ((new TimeSpan(new DateTime(iYear, iMonth, lastday).Ticks)) - epochTicks).TotalSeconds - 19800;
                    ProcessWorkingHours(con, year, (MyGlobal.GetInt16(month) - 1) + "", staffid, profile, "", "");
                    //loadOTResponse.dayActivities= new List<string>(new string[lastday]);
                    loadOTResponse.dayActivities = Enumerable.Repeat(0, lastday).ToList();
                    /*
                    string sSQL = @"SELECT FROM_UNIXTIME(m_ActivityTime,'%d') as da,sum(m_WorkTime) as work from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
"where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' and m_ActivityTime>='" + epochMonthStart + "' and m_ActivityTime<'" + epochMonthEnd + "' " +
"and (m_Activity = 'update' or m_Activity='lock' or m_Activity='forcedlock' or m_Activity='approved') " +
"group by FROM_UNIXTIME(m_ActivityTime,'%d')";
                    */
                    string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters_report " +
"where m_Profile='" + profile + "' and m_StaffID='" + staffid + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    /*
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                    {
                                        if (reader.GetInt16(0) < lastday && reader.GetInt16(0) > 0)
                                        {
                                            loadOTResponse.dayActivities[reader.GetInt16(0) - 1] = reader.GetInt32(1);
                                        }
                                    }
                                    */
                                    for (int i = 0; i < 32; i++)
                                    {
                                        if (!reader.IsDBNull(i + 8))
                                        {
                                            loadOTResponse.dayActivities[i] = reader.GetInt32(i + 8);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (MySqlException ex1)
            {
                loadOTResponse.result = ex1.Message;
            }
            return Json(loadOTResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult SearchReportingHeads(string profile, string email, string search)
        {
            var reportingToResponse = new ReportingToResponse();
            reportingToResponse.status = false;
            reportingToResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (" +
                "m_Email like '%" + search + "%' or " +
                "m_StaffID like '%" + search + "%' or " +
                "m_FName like '%" + search + "%') ";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_FName,m_Roll,m_Base,m_Email,m_Band from " + MyGlobal.activeDB + ".tbl_staffs ";
                    sSQL += "where " + sSearchKey + " " +
                        "and (m_Band='Leadership' or m_Band='Managerial') " +
                        "and m_Band is not null " +
                        "and m_Band!='' " +
                        "and m_Profile='" + profile + "' ";
                    //"and (m_Roll='Manager' or m_Roll='Supervisor' or m_Roll='Team Leader') and m_Roll is not null " +
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ReportingToItem deviceItem = new ReportingToItem();
                                    if (!reader.IsDBNull(0)) deviceItem.Name = reader.GetString(0);//
                                    if (!reader.IsDBNull(4)) deviceItem.Roll =
                                            reader.GetString(4);//
                                    if (!reader.IsDBNull(2)) deviceItem.Base = reader.GetString(2);//
                                    if (!reader.IsDBNull(3)) deviceItem.Email = reader.GetString(3);//
                                    reportingToResponse.names.Add(deviceItem);
                                }
                                reportingToResponse.status = true;
                            }

                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
            }
            return Json(reportingToResponse, JsonRequestBehavior.AllowGet);
        }
        public HubObject GetPendingMessagesObject(MySqlConnection con, string profile, string mode, string email)
        {
            HubObject hubObject = new HubObject();
            hubObject.Mode = mode;
            /*
            string sSQL = "select count(m_Session) as cnt from " + MyGlobal.activeDB + ".tbl_messages_clubs where m_Profile = '" + profile + "' and m_Seen is null " +
"and m_Session in (SELECT m_Session FROM " + MyGlobal.activeDB + ".tbl_messages where m_Member = '" + email + "' and m_Profile = '" + profile + "')";
*/
            string sSQL = "select count(m_Session) as cnt from " + MyGlobal.activeDB + ".tbl_messages_clubs " +
                "where m_Profile = '" + profile + "' and m_Seen is null " +
                  "and m_Member= '" + email + "';";

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
                                hubObject.lData = reader.GetInt16(0);
                            }
                        }
                    }
                    else
                    {

                    }
                }
            }
            return hubObject;
        }
        public ActionResult GetPendingMessages(string profile, string email)
        {
            var messageResponse = new MessageResponse();
            messageResponse.status = false;
            messageResponse.retLeaves = 0;
            messageResponse.retTimes = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string sSQL = "select count(m_Session) as cnt from " + MyGlobal.activeDB + ".tbl_messages_clubs where m_Profile = '" + profile + "' and m_Seen is null " +
                    "and m_Session in (SELECT m_Session FROM " + MyGlobal.activeDB + ".tbl_messages_sessions where m_Member = '" + email + "' and m_Profile = '" + profile + "' and m_Type=1)";

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
                                        messageResponse.retTimes = reader.GetInt16(0);
                                    }
                                }

                            }
                        }
                    }

                    sSQL = "select count(m_Session) as cnt from " + MyGlobal.activeDB + ".tbl_messages_clubs where m_Profile = '" + profile + "' and m_Seen is null " +
                    "and m_Session in (SELECT m_Session FROM " + MyGlobal.activeDB + ".tbl_messages_sessions where m_Member = '" + email + "' and m_Profile = '" + profile + "' and m_Type=2)";

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
                                        messageResponse.retLeaves = reader.GetInt16(0);
                                    }
                                }

                            }
                        }
                    }
                    messageResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("messageResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("messageResponse-Exception->" + ex.Message);
            }
            return Json(messageResponse, JsonRequestBehavior.AllowGet);
        }
        /*
        public void SendHubObject(string toWhom, HubObject obj)
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
        */
        private void SendHubObjectsFromList(List<string> emails)
        {
            HubObject hubObject = new HubObject();
            hubObject.Mode = "times";
            hubObject.lData = 1;

            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            //Array.ForEach(emails, email =>
            emails.ForEach(email =>
            {
                List<String> connections = hub.GetBrowserConnections(email);
                if (connections != null)
                {
                    foreach (String connectionID in connections)
                    {
                        hubContext.Clients.Client(connectionID).HubToBrowser(hubObject);
                    }
                }
            }
            );
        }
        private void SendHubObjects(string[] emails, HubObject obj)
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            Array.ForEach(emails, email =>
            {
                List<String> connections = hub.GetBrowserConnections(email);
                if (connections != null)
                {
                    foreach (String connectionID in connections)
                    {
                        hubContext.Clients.Client(connectionID).HubToBrowser(obj);
                    }
                }
            }
            );
        }
        //-------------------------------------------------------
        [HttpPost]
        public ActionResult SendTerminalCommand(string profile, string hardware, string command)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            HubObject obj = new HubObject();
            obj.Mode = "command";
            obj.sData = command;// "Sleep";
            if (SendHubObject_ToTerminal(hardware, obj))
            {
                postResponse.result = "Command sent";
                postResponse.status = true;
            }
            else
            {
                postResponse.result = "Terminal not online";
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------------------------------
        private bool SendHubObject_ToTerminal(string terminal, HubObject obj)
        {
            bool bValid = false;
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            List<String> connections = hub.GetAccessManagerConnections(terminal);
            if (connections != null)
            {
                foreach (String connectionID in connections)
                {
                    hubContext.Clients.Client(connectionID).HubToAccessManager(obj);
                    bValid = true;
                }
            }
            return bValid;
        }
        //-------------------------------------------------------
        private int GetIndexOf(string[] arData, string sName)
        {
            for (int i = 0; i < arData.Length; i++)
            {
                if (arData[i].Equals(sName)) return i;
            }
            return -1;
        }
        [HttpPost]
        public ActionResult UpdateArraySeq(string profile, string key, string selected,
            string[] arData)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQLUpdate = "";
                    string sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_bands " +
                        "where m_Profile='" + profile + "';";
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
                                        int iIndex = GetIndexOf(arData, reader.GetString(0));
                                        if (iIndex > -1)
                                        {
                                            sSQLUpdate += "update " + MyGlobal.activeDB + ".tbl_misc_bands Set " +
                                                "m_Order='" + (iIndex * 0x100) + "' where m_Name='" + reader.GetString(0) + "' and m_Profile='" + profile + "';";

                                            sSQLUpdate += "update " + MyGlobal.activeDB + ".tbl_misc_grades Set " +
                                                "m_Order=((m_Order & 0xff) | " + (iIndex * 0x100) + ") where m_Profile='" + profile + "' and m_Band='" + reader.GetString(0) + "';";


                                        }
                                    }
                                }
                                postResponse.status = true;
                            }
                            else
                            {

                            }
                        }
                    }
                    if (sSQLUpdate.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLUpdate, con)) mySqlCommand.ExecuteNonQuery();
                    }

                    //------------------------Grades----------------------
                    sSQLUpdate = "";
                    int iOrderOfSelectedBand = -1;
                    sSQL = "SELECT m_Order FROM " + MyGlobal.activeDB + ".tbl_misc_bands " +
"where m_Profile='" + profile + "' and m_Name='" + selected + "';";
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
                                        iOrderOfSelectedBand = reader.GetInt16(0);
                                    }
                                }
                                postResponse.status = true;
                            }
                            else
                            {

                            }
                        }
                    }
                    if (iOrderOfSelectedBand > -1)
                    {
                        sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_grades " +
        "where m_Profile='" + profile + "';";
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
                                            int iIndex = GetIndexOf(arData, reader.GetString(0));
                                            if (iIndex > -1)
                                            {
                                                int iIdx = iOrderOfSelectedBand + iIndex;
                                                sSQLUpdate += "update " + MyGlobal.activeDB + ".tbl_misc_grades Set " +
                                                    "m_Order='" + iIdx + "' where m_Name='" + reader.GetString(0) + "' and m_Profile='" + profile + "';";
                                            }
                                        }
                                    }
                                    postResponse.status = true;
                                }
                                else
                                {

                                }
                            }
                        }
                        if (sSQLUpdate.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLUpdate, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }

                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("UpdateArraySeq-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("UpdateArraySeq-Exception->" + ex.Message);
            }

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadTeamsAndStaffs(string profile, string team, string selected, string loginstaffid)
        {
            var loadStaffResponse = new LoadStaffResponse();
            loadStaffResponse.status = false;
            loadStaffResponse.result = "";
            loadStaffResponse.team = team;
            loadStaffResponse.selected = selected;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    loadStaffResponse.sarTeams.Add("");

                    string permission = "select m_Team from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
"where m_Profile = '" + profile + "' and m_StaffID = '" + loginstaffid + "' and m_Head='production'";


                    string sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_misc_teams " +
                        "where m_Profile='" + profile + "' " +
                        "and m_Name in (" + permission + ") " +
                        "order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        loadStaffResponse.sarTeams.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    //----------------------------------------
                    BreakItem itemEmpty = new BreakItem();
                    itemEmpty.key = "";
                    itemEmpty.value = "";
                    loadStaffResponse.sarStaffs.Add(itemEmpty);
                    if (team.Length > 0)
                    {
                        sSQL = "SELECT m_StaffID,m_FName FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_Profile='" + profile + "' and m_Team='" + team + "' " +
                            "and (m_Status='Active' or m_Status='Trainee');";
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
                                            BreakItem item = new BreakItem();
                                            item.key = reader.GetString(0);
                                            item.value = reader.GetString(1);
                                            loadStaffResponse.sarStaffs.Add(item);
                                        }
                                    }
                                    loadStaffResponse.status = true;
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadStaffs-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("LoadStaffs-Exception->" + ex.Message);
            }

            return Json(loadStaffResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------------------------
        [HttpPost]
        public ActionResult GetLastShiftsOfStaff(string profile, string staffid,
            string year, string month, string day)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var staffShiftsResponse = new StaffShiftsResponse();
            staffShiftsResponse.status = false;
            staffShiftsResponse.result = "";
            int imonth = MyGlobal.GetInt16(month) - 1;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------------
                    string sSQL = "SELECT m_RosterName,m_ShiftName,m_StaffName," +
                        "m_StaffID,m_Day" + day + ",m_ShiftStartTime,m_ShiftEndTime " +
                        "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + imonth + "' " +
                        "and (m_StaffID like '%" + staffid + "%' or m_StaffName like '%" + staffid + "%') " +
                        "limit 4;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (staffShiftsResponse.staffID.Length == 0)
                                    {
                                        staffShiftsResponse.staffID = GetFldVaue(reader, 3);
                                    }

                                    StaffShiftsRow row = new StaffShiftsRow();
                                    row.id = GetFldVaue(reader, 3);
                                    row.name = GetFldVaue(reader, 2);
                                    row.roster = GetFldVaue(reader, 0);
                                    row.shift = GetFldVaue(reader, 1);
                                    row.day5 = GetFldVaue(reader, 4);
                                    if (!reader.IsDBNull(5)) row.lShiftStart = reader.GetInt32(5);
                                    if (!reader.IsDBNull(6)) row.lShiftEnd = reader.GetInt32(6);
                                    staffShiftsResponse.rows.Add(row);
                                }
                            }
                            staffShiftsResponse.result = "";
                            staffShiftsResponse.status = true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-Exception->" + ex.Message);
            }

            return Json(staffShiftsResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------------------------
        [HttpPost]
        public ActionResult StaffSearch(string profile, string staffid)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var staffShiftsResponse = new StaffShiftsResponse();
            staffShiftsResponse.status = false;
            staffShiftsResponse.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------------
                    string sSQL = "SELECT m_StaffID,m_FName,m_Team " +
                        "FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_StaffID like '%" + staffid + "%' or m_FName like '%" + staffid + "%') " +
                        "limit 4;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (staffShiftsResponse.staffID.Length == 0)
                                    {
                                        staffShiftsResponse.staffID = GetFldVaue(reader, 0);
                                    }

                                    StaffShiftsRow row = new StaffShiftsRow();
                                    row.id = GetFldVaue(reader, 0);
                                    row.name = GetFldVaue(reader, 1);
                                    row.m_Team = GetFldVaue(reader, 2);
                                    //row.roster = GetFldVaue(reader, 0);
                                    //row.shift = GetFldVaue(reader, 1);
                                    //row.day5 = GetFldVaue(reader, 4);
                                    //if (!reader.IsDBNull(5)) row.lShiftStart = reader.GetInt32(5);
                                    //if (!reader.IsDBNull(6)) row.lShiftEnd = reader.GetInt32(6);
                                    staffShiftsResponse.rows.Add(row);
                                }
                            }
                            staffShiftsResponse.result = "";
                            staffShiftsResponse.status = true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-Exception->" + ex.Message);
            }

            return Json(staffShiftsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetStaffFromRosterOfThisMonth(string profile, string staffid, string year, string month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var staffShiftsResponse = new StaffShiftsResponse();
            staffShiftsResponse.status = false;
            staffShiftsResponse.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------------
                    string sSQL = "SELECT m_StaffID,m_StaffName,m_RosterName,m_ShiftName " +
                        "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_StaffID like '%" + staffid + "%' or m_StaffName like '%" + staffid + "%') " +
                        " and m_Month='" + month + "' and m_Year='" + year + "'" +
                        "limit 4;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (staffShiftsResponse.staffID.Length == 0)
                                    {
                                        staffShiftsResponse.staffID = GetFldVaue(reader, 0);
                                    }

                                    StaffShiftsRow row = new StaffShiftsRow();
                                    row.id = GetFldVaue(reader, 0);
                                    row.name = GetFldVaue(reader, 1);
                                    row.roster = GetFldVaue(reader, 2);
                                    row.shift = GetFldVaue(reader, 3);
                                    //row.day5 = GetFldVaue(reader, 4);
                                    //if (!reader.IsDBNull(5)) row.lShiftStart = reader.GetInt32(5);
                                    //if (!reader.IsDBNull(6)) row.lShiftEnd = reader.GetInt32(6);
                                    staffShiftsResponse.rows.Add(row);
                                }
                            }
                            staffShiftsResponse.result = "";
                            staffShiftsResponse.status = true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetLastShiftsOfStaff-Exception->" + ex.Message);
            }

            return Json(staffShiftsResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------------------------
        [HttpPost]
        public ActionResult LoadReport_RosterShiftCombos(string profile, string roster, string shift,
            string year, string month, string day)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var rosterShiftCombos = new RosterShiftCombos();
            rosterShiftCombos.status = false;
            rosterShiftCombos.result = "";
            int iMonth = MyGlobal.GetInt16(month) - 1;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------------
                    //rosterShiftCombos.sarRosters.Add("");
                    if (!MyGlobal.activeDomain.Equals("chchealthcare")) rosterShiftCombos.sarRosters.Add("All");
                    //string sSQL = "SELECT m_RosterName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                    //    "where m_Profile='" + profile + "' and m_Year='" + year + "' and m_Month='" + iMonth + "' " +
                    //    "group by m_RosterName;";
                    //Starts Roster name by Alphabetic order by Sivaguru M CHC1704 at 24-02-2024
                    string sSQL = "SELECT m_RosterName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' and m_Year='" + year + "' and m_Month='" + iMonth + "' " +
                        "group by m_RosterName order by m_RosterName;";
                    //Ends Roster name by Alphabetic order

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        rosterShiftCombos.sarRosters.Add(reader.GetString(0));
                                }
                                rosterShiftCombos.status = true;
                            }
                        }
                    }
                    //-------------------------------------------------------
                    rosterShiftCombos.sarShifts.Add("");
                    //sSQL = "SELECT m_ShiftName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                    //    "where m_Profile='" + profile + "' and m_RosterName='" + roster + "' " +
                    //    "and m_Year = '" + year + "' and m_Month = '" + iMonth + "' " +
                    //    "group by m_ShiftName;";

                    //Starts Shift Name by Alphabetic order by Sivaguru M CHC1704 at 24-02-2024
                    sSQL = "SELECT m_ShiftName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' and m_RosterName='" + roster + "' " +
                        "and m_Year = '" + year + "' and m_Month = '" + iMonth + "' " +
                        "group by m_ShiftName order by m_ShiftName;";
                    //Ends Shift Name by Alphabetic order
                    Int16 iRows = 0;
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
                                        iRows++;
                                        rosterShiftCombos.sarShifts.Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                    rosterShiftCombos.shifts = iRows;
                    if (roster.Length == 0)
                    {
                        rosterShiftCombos.shiftMessage = "Please Select a Roster";
                    }
                    else
                    {
                        if (iRows == 0)
                        {
                            rosterShiftCombos.shiftMessage = "Not on Any Shift for the month";
                        }
                        else
                        {
                            rosterShiftCombos.shiftMessage = iRows + " Shift for the month";
                        }
                    }
                    //-------------------------------------------------------

                    BreakItem brk1 = new BreakItem();
                    brk1.key = "";
                    brk1.value = "";
                    rosterShiftCombos.sarStaffs.Add(brk1);
                    sSQL = "SELECT m_StaffName,m_StaffID  FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                    "where m_Profile='" + profile + "' and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                    "and m_Year = '" + year + "' and m_Month = '" + iMonth + "' " +
                    "and m_StaffID is not null";
                    //"and m_Day" + day + " is not null and m_Day" + day + "='" + MyGlobal.WORKDAY_MARKER + "' " +
                    //"and m_StaffID is not null and m_StaffID<>'' order by m_StaffID;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    //if (!reader.IsDBNull(0))rosterShiftCombos.sarShifts.Add(reader.GetString(0));
                                    BreakItem brk = new BreakItem();
                                    if (!reader.IsDBNull(0)) brk.value = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) brk.key = reader.GetString(1);
                                    rosterShiftCombos.sarStaffs.Add(brk);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-Exception->" + ex.Message);
            }

            return Json(rosterShiftCombos, JsonRequestBehavior.AllowGet);
        }
        private string GetFldVaue(MySqlDataReader reader, int idx)
        {
            if (reader.IsDBNull(idx)) return "";
            return reader.GetString(idx);
        }
        /*
        private void GetActualAttendanceData(string profile, string staffid, double epochShiftStart,
            double epochShiftEnd, out long m_ActualStart, out long m_ActualEnd)
        {
            m_ActualStart = 0;
            m_ActualEnd = 0;

            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT min(m_ActivityTime),max(m_ActivityTime) " +
                    "FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "where m_ActivityTime>= '" + epochShiftStart + "' " +
                    "and m_ActivityTime<'" + epochShiftEnd + "' " +
                    "and m_StaffID='" + staffid + "' " +
                    "and (m_Activity = 'open' or m_Activity = 'update' or m_Activity = 'approved') " +
                    "and m_Profile='" + profile + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                m_ActualStart = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                m_ActualEnd = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            }
                        }
                    }
                }
            }
        }
        */
        //[HttpPost]
        public ActionResult HRActivitiesResponse(string profile,
            string sort, string order, int dtYear, int dtMonth, int dtDay,
            string roster, string shift, string staff, string showoptions, string mode)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            roster = Server.UrlDecode(roster);
            shift = Server.UrlDecode(shift);

            var hrActivitiesResponse = new HRActivitiesResponse();
            hrActivitiesResponse.status = false;
            hrActivitiesResponse.result = "";
            if (dtYear == 4 || dtMonth == 0 || dtDay == 0)
            {
                hrActivitiesResponse.result = "Invalid dates";
                return Json(hrActivitiesResponse, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    DateTime dt = new DateTime(dtYear, dtMonth, dtDay);
                    Int32 unixDayStart = (Int32)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    dtMonth--; // SQL needs zero index
                    //--------------------------------------------------------------------------------
                    if (mode.Equals("bio"))
                    {
                        hrActivitiesResponse.status = GetActivities_Figer(hrActivitiesResponse, profile, staff, dtYear, (dtMonth + 1), dtDay, roster, shift);
                        hrActivitiesResponse.mode = 4;
                        hrActivitiesResponse.status = true;
                        return Json(hrActivitiesResponse, JsonRequestBehavior.AllowGet);
                    }
                    //--------------------------Mode 0 Show All Show All -----------------------------
                    if (
                        (shift.Length == 0 || shift.Equals("")) &&
                        (staff.Length == 0 || staff.Equals("") || staff.Equals("undefined"))
                        )
                    {
                        DateTime myDate; long spanFrom = 0, spanTo = 0;
                        if (DateTime.TryParse(dtYear + "-" + (dtMonth + 1) + "-" + dtDay, out myDate))
                        {
                            spanFrom = MyGlobal.ToEpochTime(myDate);// - 3600 + 19800;
                            spanTo = MyGlobal.ToEpochTime(myDate) + 86400;// + 3600 + 19800;
                        }
                        else
                        {
                            spanFrom = MyGlobal.ToEpochTime(DateTime.Today);//  - 3600 + 19800;
                            spanTo = MyGlobal.ToEpochTime(DateTime.Today) + 86400;// + 3600 + 19800;
                        }

                        sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime," +
                        "count(DISTINCT activities.m_StaffID),count(DISTINCT rosters.m_StaffID) " +

                        "FROM " + MyGlobal.activeDB + ".tbl_rosters rosters " +

                        "left join " +
                        "(select m_Profile, m_StaffID, m_ActivityTime from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                        "where m_ActivityTime>= (" + spanFrom + ") and m_ActivityTime<(" + spanTo + ") " +
                        "and m_StaffID is not null and m_StaffID<>'' " +
                        ") activities on rosters.m_Profile = activities.m_Profile " +
                        "and activities.m_StaffID = rosters.m_StaffID " +
                        "and activities.m_ActivityTime >= (unix_timestamp(concat('" + dtYear + "', '-', '" + (dtMonth + 1) + "', '-', '" + dtDay + "')) + rosters.m_ShiftStartTime - 3600) " +
                        "and activities.m_ActivityTime < (unix_timestamp(concat('" + dtYear + "', '-', '" + (dtMonth + 1) + "', '-', '" + dtDay + "')) + rosters.m_ShiftEndTime + 3600) " +
                        "and activities.m_StaffID in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_rosters where " +
                        "m_RosterName = rosters.m_RosterName and m_ShiftName = rosters.m_ShiftName " +
                        "and m_Year = '" + dtYear + "' and m_Month = '" + dtMonth + "' and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' and m_StaffID is not null) " +

                        "where rosters.m_Profile = '" + profile + "' and m_ShiftName is not null " +
                        "and m_Year = '" + dtYear + "' and m_Month = '" + dtMonth + "' " +
                        "and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' ";

                        if (roster.Length > 0 && !roster.Equals("")) sSQL += "and m_RosterName='" + roster + "' ";

                        sSQL += "group by m_RosterName,m_ShiftName " +
                        "order by m_RosterName asc,m_ShiftStartTime";


                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        DisplayedColumns_Roster_Consolidated_Row row =
                                            new DisplayedColumns_Roster_Consolidated_Row();
                                        if (!reader.IsDBNull(0)) row.m_RosterName = reader.GetString(0);
                                        if (!reader.IsDBNull(1)) row.m_ShiftName = reader.GetString(1);
                                        if (!reader.IsDBNull(2)) row.shift_start = unixDayStart + reader.GetInt32(2);
                                        if (!reader.IsDBNull(3)) row.shift_end = unixDayStart + reader.GetInt32(3);
                                        if (!reader.IsDBNull(4)) row.m_StaffsA = reader.GetInt16(4);
                                        if (!reader.IsDBNull(5)) row.m_StaffsE = reader.GetInt16(5);
                                        hrActivitiesResponse.rows.Add(row);

                                    }
                                    hrActivitiesResponse.status = true;
                                }
                            }
                        }
                        hrActivitiesResponse.mode = 0;
                        hrActivitiesResponse.status = true;
                    }
                    else
                    //--------------------------Mode 1  -----------------------------
                    if (
                        (shift.Length > 0 && !shift.Equals("")
                            && roster.Length > 0 && !roster.Equals(""))
                        && (staff == null || staff.Length == 0 || staff.Equals("") || staff.Equals("undefined"))
                        )
                    {

                        sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime, " +
"m_StaffID,m_StaffName,m_Day" + dtDay + " " +
    "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
"where m_Profile='" + profile + "' and m_ShiftName is not null " +
"and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
"and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' ";
                        sSQL += "and m_StaffID is not null and m_StaffID<>'' ";
                        sSQL += "and m_RosterName='" + roster + "' ";
                        sSQL += "and m_ShiftName='" + shift + "' " +
                        "order by m_StaffName;";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        DisplayedColumns_Roster_Consolidated_Row row =
                                            new DisplayedColumns_Roster_Consolidated_Row();
                                        if (!reader.IsDBNull(0)) row.m_RosterName = reader.GetString(0);
                                        if (!reader.IsDBNull(1)) row.m_ShiftName = reader.GetString(1);
                                        if (!reader.IsDBNull(2)) row.shift_start = unixDayStart + reader.GetInt32(2);
                                        if (!reader.IsDBNull(3)) row.shift_end = unixDayStart + reader.GetInt32(3);

                                        if (!reader.IsDBNull(4)) row.m_StaffID = reader.GetString(4);
                                        if (!reader.IsDBNull(5)) row.m_StaffName = reader.GetString(5);
                                        if (!reader.IsDBNull(6)) row.m_Day = reader.GetString(6);
                                        hrActivitiesResponse.rows.Add(row);

                                    }
                                    hrActivitiesResponse.mode = 1;
                                    hrActivitiesResponse.status = true;
                                }
                            }
                        }
                        //-------------------------------------------------------
                        hrActivitiesResponse.rows.ForEach(
                            (row) =>
                            {
                                long lWorkhours = 0;
                                GetStaffWorkHours(profile,
                                    row.shift_start, row.shift_end, row.m_StaffID,
                                     out lWorkhours);

                                row.worktime = lWorkhours;

                            });

                    }
                    else
                    //--------------------------Mode 2  (Advanced View)-----------------------------
                    if (staff.Length > 0 && !staff.Equals("") && !staff.Equals("undefined") && showoptions.Equals("1"))
                    {
                        int startTime = System.Environment.TickCount;
                        hrActivitiesResponse.status = GetActivities_AdvanceView(hrActivitiesResponse, profile, staff, dtYear, (dtMonth + 1), dtDay, roster, shift);
                        hrActivitiesResponse.mode = 2;
                        hrActivitiesResponse.status = true;
                    }
                    else
                    //--------------------------Mode 3  New -------Classic View----------------------
                    if (staff.Length > 0 && !staff.Equals("") && !staff.Equals("undefined") && showoptions.Equals("0"))
                    {

                        hrActivitiesResponse.status = GetActivities_ClassicView(hrActivitiesResponse, profile, staff, dtYear, (dtMonth + 1), dtDay, roster, shift);
                        //hrActivitiesResponse.mode = 3; // comes from internal of above
                        //hrActivitiesResponse.status = true;
                    }
                    //________________________________________________________________
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-Exception->" + ex.Message);
            }

            return Json(hrActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        private bool GetActivities_Figer(HRActivitiesResponse hrActivitiesResponse,
            string profile, string staff,
            int dtYear, int dtMonth, int dtDay, string roster, string shift)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            hrActivitiesResponse.mode = 2;
            DateTime dt = new DateTime(dtYear, dtMonth, dtDay);
            Int32 unixDayStart = (Int32)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            dtMonth--; // SQL needs zero index

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    long shift_start = 0, shift_end = 0;
                    if (shift.Length > 0 && !shift.Equals("")
                        && roster.Length > 0 && !roster.Equals(""))
                    {
                        sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime, " +
                            "m_StaffID,m_StaffName,m_Day" + dtDay + " " +
                            "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                            "where m_Profile='" + profile + "' and m_ShiftName is not null " +
                            "and m_StaffID = '" + staff + "' " +
                            "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                                                                                         //"and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' ";
                        sSQL += "and m_RosterName='" + roster + "' ";
                        sSQL += "and m_ShiftName='" + shift + "' " +
                        "limit 1;";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        shift_start = unixDayStart + reader.GetInt32(2);
                                        shift_end = unixDayStart + reader.GetInt32(3);
                                        if (!reader.IsDBNull(6))
                                            hrActivitiesResponse.m_Day = reader.GetString(6);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        shift_start = unixDayStart + 0;// - 19800; // From morning
                        shift_end = unixDayStart + 86400;// - 19800;   // till night
                    }
                    //----------------------------------------------------
                    hrActivitiesResponse.shift_start = shift_start;
                    hrActivitiesResponse.shift_end = shift_end;

                    //----------------------------------------------------
                    sSQL = @"SELECT * from " + MyGlobal.activeDB + ".tbl_biometric_activity " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staff + "' " +
                        "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                        "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                        "order by m_ActivityTime;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                long lActualStart = 0, lActualEnd = 0;
                                while (reader.Read())
                                {
                                    DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                    row.m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    row.m_RosterName = MyGlobal.GetPureString(reader, "m_RosterName");
                                    row.m_ShiftName = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    row.sHardwareID = MyGlobal.GetPureString(reader, "m_HardwareID");
                                    row.m_StaffName = MyGlobal.GetPureString(reader, "m_StaffName");
                                    row.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.sActivity = MyGlobal.GetPureString(reader, "m_Activity");

                                    row.lActivityTime = MyGlobal.GetPureInt32(reader, "m_ActivityTime") - 19800;
                                    //row.m_ShiftStartTime = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                    //row.m_ShiftEndTime = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                    row.m_RosterMarker = MyGlobal.GetPureString(reader, "m_RosterMarker");
                                    row.m_Remarks = MyGlobal.GetPureString(reader, "m_Remarks");

                                    hrActivitiesResponse.rows.Add(row);
                                    if (lActualStart == 0) lActualStart = row.lActivityTime;
                                    lActualEnd = row.lActivityTime;
                                }
                                hrActivitiesResponse.lActualStart = lActualStart;
                                hrActivitiesResponse.lActualEnd = lActualEnd;
                                if (lActualEnd < lActualStart)
                                {
                                    hrActivitiesResponse.lWorkTotal = (lActualEnd + 86400) - lActualStart;
                                }
                                else
                                {
                                    hrActivitiesResponse.lWorkTotal = lActualEnd - lActualStart;
                                }

                            }
                        }
                    }
                    //----------------------------------------------------
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetActivities_Figer-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetActivities_Figer-Exception->" + ex.Message);
            }
            return false;
        }
        private bool GetActivities_AdvanceView(HRActivitiesResponse hrActivitiesResponse,
            string profile, string staff,
            int dtYear, int dtMonth, int dtDay, string roster, string shift)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            hrActivitiesResponse.mode = 2;
            DateTime dt = new DateTime(dtYear, dtMonth, dtDay);
            Int32 unixDayStart = (Int32)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            dtMonth--; // SQL needs zero index

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    int startTime = System.Environment.TickCount;
                    long shift_start = 0, shift_end = 0;
                    if (shift.Length > 0 && !shift.Equals("")
                        && roster.Length > 0 && !roster.Equals(""))
                    {
                        sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime, " +
                            "m_StaffID,m_StaffName,m_Day" + dtDay + " " +
                            "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                            "where m_Profile='" + profile + "' and m_ShiftName is not null " +
                            "and m_StaffID = '" + staff + "' " +
                            "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                                                                                         //"and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' ";
                        sSQL += "and m_RosterName='" + roster + "' ";
                        sSQL += "and m_ShiftName='" + shift + "' " +
                        "limit 1;";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        shift_start = unixDayStart + reader.GetInt32(2);
                                        shift_end = unixDayStart + reader.GetInt32(3);
                                        if (!reader.IsDBNull(6))
                                            hrActivitiesResponse.m_Day = reader.GetString(6);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        shift_start = unixDayStart + 0;// - 19800; // From morning
                        shift_end = unixDayStart + 86400;// - 19800;   // till night
                    }
                    //----------------------------------------------------Get Locktime
                    long lLocktime = 0;
                    sSQL = "select teams.m_LockTime,staff.m_FName from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                            "left join " + MyGlobal.activeDB + ".tbl_misc_teams teams on teams.m_Profile = staff.m_Profile and teams.m_Name = staff.m_Team " +
                            "where staff.m_Profile = '" + profile + "' and staff.m_StaffID='" + staff + "'";
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
                                        lLocktime = reader.GetInt16(0) * 60;
                                        hrActivitiesResponse.m_StaffName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        hrActivitiesResponse.m_StaffID = staff;
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------------------------------

                    //----------------------------------------------------
                    long lWorkhours = 0;
                    GetStaffWorkHours(profile,
                        shift_start, shift_end, staff,
                         out lWorkhours);
                    hrActivitiesResponse.shift_start = shift_start;
                    hrActivitiesResponse.shift_end = shift_end;
                    hrActivitiesResponse.lWorkTotal = lWorkhours;
                    //----------------------------------------------------
                    sSQL = @"SELECT * from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staff + "' " +
                        "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                        "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                        "order by m_ActivityTime;";

                    sSQL = "select m_HardwareID,m_Activity,activity.m_ActivityTime,m_ReasonHead," +
                        "m_Lat,m_Lng,notes.m_Notes,m_WorkTime,m_IP,m_ReasonNote " +
"from " + MyGlobal.activeDB + ".tbl_accessmanager_activity activity " +
"left join " + MyGlobal.activeDB + ".tbl_update_notes notes on notes.m_ActivityTime=(activity.m_ActivityTime+19800) and notes.m_Profile=activity.m_Profile " +
"and notes.m_StaffID = activity.m_StaffID " +
"where activity.m_Profile = '" + profile + "' " +
"and activity.m_StaffID = '" + staff + "' " +
"and activity.m_ActivityTime>=" + (shift_start - 19800 - const_ShiftPaddingPre) + " " +
"and activity.m_ActivityTime<" + (shift_end - 19800 + const_ShiftPaddingPost) +
" order by activity.m_ActivityTime desc;";

                    startTime = System.Environment.TickCount;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                long lActualStart = 0, lActualEnd = 0;
                                while (reader.Read())
                                {
                                    DisplayedColumns_Roster_Consolidated_Row row =
                                        new DisplayedColumns_Roster_Consolidated_Row();
                                    row.sHardwareID = MyGlobal.GetPureString(reader, "m_HardwareID");// reader.GetString(1);
                                    row.sActivity = MyGlobal.GetPureString(reader, "m_Activity");//reader.GetString(4);
                                    if (row.sActivity.Equals("open") || row.sActivity.Equals("update"))
                                    {
                                        if (lActualStart == 0)
                                        {
                                            lActualStart = MyGlobal.GetPureInt32(reader, "m_ActivityTime") + 19800;// reader.GetInt64(5) + 19800;
                                            if (!row.sActivity.Equals("update"))
                                            {
                                                row.worktime = MyGlobal.GetPureInt32(reader, "m_WorkTime"); //reader.GetInt64(6);
                                            }
                                        }
                                        else
                                        {
                                            row.worktime = MyGlobal.GetPureInt32(reader, "m_WorkTime"); // reader.GetInt64(6);
                                        }
                                    }
                                    else
                                    {
                                        row.worktime = MyGlobal.GetPureInt32(reader, "m_WorkTime"); // reader.GetInt64(6);
                                    }
                                    lActualEnd = MyGlobal.GetPureInt32(reader, "m_ActivityTime") + 19800;//  reader.GetInt64(5) + 19800;

                                    row.lActivityTime = MyGlobal.GetPureInt32(reader, "m_ActivityTime");// reader.GetInt64(5);

                                    row.sIP = MyGlobal.GetPureString(reader, "m_IP"); //reader.GetString(12);
                                    //if (!reader.IsDBNull(8)) row.ReasonHead = reader.GetString(8);
                                    row.ReasonHead = MyGlobal.GetPureString(reader, "m_ReasonHead");// reader.GetString(8);
                                    int ordReasonNote = reader.GetOrdinal("m_ReasonNote");
                                    if (!reader.IsDBNull(ordReasonNote))
                                    {
                                        string[] arData = reader.GetString(ordReasonNote).Split(new string[] { "*_*" }, StringSplitOptions.None);
                                        if (arData.Length > 0)
                                        {
                                            row.ReasonNote = arData[0];
                                        }
                                        else
                                        {
                                            row.ReasonNote = reader.GetString(ordReasonNote);
                                        }
                                    }
                                    row.m_Notes = MyGlobal.GetPureString(reader, "m_Notes");
                                    hrActivitiesResponse.rows.Add(row);
                                    hrActivitiesResponse.lActualStart = lActualStart;
                                    hrActivitiesResponse.lActualEnd = lActualEnd;
                                }
                            }
                        }
                    }

                    //----------------------------------------------------
                    sSQL = "select (m_ActivityTime - m_ActivityStart),m_Activity," +
                        "m_ActivityStart,m_ActivityTime," +
                        "m_HardwareID,m_IP from " +
                        MyGlobal.activeDB + ".tbl_terminals " +
                        "where (m_Activity = 'open' or m_Activity = 'update') " +
                        "and m_Profile = '" + profile + "' " +
                        "and m_StaffID = '" + staff + "' " +
                        "order by m_ActivityTime desc limit 1";

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
                                        if (reader.GetInt16(0) < 1800)  // 1800 is 30 mts or abnormal
                                        {
                                            if (reader.GetInt16(0) > 0)
                                            {
                                                DisplayedColumns_Roster_Consolidated_Row row =
                                                    new DisplayedColumns_Roster_Consolidated_Row();
                                                row.sHardwareID = reader.GetString(4);
                                                row.sActivity = reader.GetString(1);
                                                row.worktime = reader.GetInt16(0);
                                                row.lActivityTime = reader.GetInt32(3);
                                                row.sIP = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                                if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                                                {
                                                    if (reader.GetInt32(2) > 0 && reader.GetInt32(3) > 0)
                                                    {
                                                        row.ReasonHead =
                                                        "Since " + MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(2) + 19800).ToString("HH:mm:ss");
                                                        hrActivitiesResponse.rows.Add(row);
                                                    }
                                                }
                                                //row.ReasonNote;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-Exception->" + ex.Message);
            }
            return false;
        }
        private bool GetActivities_ClassicView(HRActivitiesResponse hrActivitiesResponse,
            string profile, string staff,
            int dtYear, int dtMonth, int dtDay, string roster, string shift)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            hrActivitiesResponse.lLateSeconds = 0;
            hrActivitiesResponse.mode = 3;
            DateTime dt = new DateTime(dtYear, dtMonth, dtDay);
            Int32 unixDayStart = (Int32)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            dtMonth--; // SQL needs zero index

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    long shift_start = 0, shift_end = 0;
                    if (shift.Length > 0 && !shift.Equals("")
                        && roster.Length > 0 && !roster.Equals(""))
                    {
                        sSQL = "SELECT m_RosterName,m_ShiftName,m_ShiftStartTime,m_ShiftEndTime, " +
                        "m_StaffID,m_StaffName,m_Day" + dtDay + " " +
                        "FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' and m_ShiftName is not null " +
                        "and m_StaffID = '" + staff + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";
                        //"and m_Day" + dtDay + " is not null and m_Day" + dtDay + " = '" + MyGlobal.WORKDAY_MARKER + "' ";
                        sSQL += "and m_RosterName='" + roster + "' ";
                        sSQL += "and m_ShiftName='" + shift + "' " +
                        "limit 1;";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        shift_start = unixDayStart + reader.GetInt32(2);
                                        shift_end = unixDayStart + reader.GetInt32(3);
                                        if (!reader.IsDBNull(6))
                                            hrActivitiesResponse.m_Day = reader.GetString(6);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        shift_start = unixDayStart + 0;// - 19800; // From morning
                        shift_end = unixDayStart + 86400;// - 19800;   // till night
                    }
                    //----------------------------------------------------Get Locktime
                    long lLocktime = 0;
                    sSQL = "select teams.m_LockTime,staff.m_FName from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                            "left join " + MyGlobal.activeDB + ".tbl_misc_teams teams on teams.m_Profile = staff.m_Profile and teams.m_Name = staff.m_Team " +
                            "where staff.m_Profile = '" + profile + "' and staff.m_StaffID='" + staff + "'";
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
                                        lLocktime = reader.GetInt16(0) * 60;
                                        hrActivitiesResponse.m_StaffName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        hrActivitiesResponse.m_StaffID = staff;
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------------------------------
                    /*
                    long lWorkhours = 0;
                    GetStaffWorkHours(con, profile,
                        shift_start, shift_end, staff,
                         out lWorkhours);
                         */
                    hrActivitiesResponse.shift_start = shift_start;
                    hrActivitiesResponse.shift_end = shift_end;
                    //hrActivitiesResponse.worktime = lWorkhours;
                    //hrActivitiesResponse.lWorkTotal = lWorkhours;
                    //----------------------------------------------------
                    int iMode = 0;
                    long lActualStart = 0, lActualEnd = 0;
                    long lWorkTotal = 0, lBreakTotal = 0;
                    long lBreakStartTime = 0, lBreakEndTime = 0;
                    long lWorkStartTime = 0, lWorkEndTime = 0;
                    string sWorkStartTime = "", sWorkEndTime = "";
                    string sBreakStartTime = "", sBreakEndTime = "";

                    sSQL = @"SELECT * from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staff + "' " +
                    "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                    "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                    "order by m_ActivityTime;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                const int S_NONE = 0;
                                const int S_START = 1;
                                const int S_OPENED = 2;
                                const int S_LOCKED = 3;
                                //-------------------------------------------
                                const int F_HARDWARE_ID = 1;
                                const int F_ACTIVITY = 4;
                                const int F_ACTIVITY_TIME = 5;
                                const int F_WORK_TIME = 6;
                                bool bLateLoginAproved = false;
                                string lastHardwareID = "";
                                while (reader.Read())
                                {
                                    string act = reader.GetString(F_ACTIVITY);
                                    long actTime = reader.GetInt64(F_ACTIVITY_TIME) + 19800;
                                    // This care was taken to compensate the system change over error
                                    if (lastHardwareID.Length == 0) lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                    if (!lastHardwareID.Equals(reader.GetString(F_HARDWARE_ID)))
                                    {
                                        if (act.Equals("open"))
                                        {
                                            lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                            continue;
                                        }
                                    }
                                    lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                    // This care was taken to compensate the system change over error END
                                    if (act.Equals("open"))
                                    {
                                        if (lActualStart == 0)
                                        {
                                            lActualStart = actTime;
                                            if (!bLateLoginAproved)
                                            {
                                                if ((lActualStart - shift_start) > 0)
                                                {
                                                    hrActivitiesResponse.lLateSeconds = lActualStart - shift_start;
                                                }
                                            }
                                        }
                                        lBreakEndTime = actTime;
                                        sBreakEndTime = act + GetReasonHead(reader);
                                        //---------------------------------------
                                        if (lWorkEndTime > 0)
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sWorkStartTime;
                                            row.sPostNote = sWorkEndTime;
                                            row.lWorkTime = GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                            lWorkTotal += row.lWorkTime;
                                            row.sTimeSpan = GetTimeSpan(lWorkStartTime, lWorkEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                            //------------------------------------------------------------
                                            DisplayedColumns_Roster_Consolidated_Row row1 = new DisplayedColumns_Roster_Consolidated_Row();
                                            row1.sPreNote = sWorkEndTime;
                                            row1.sPostNote = sBreakEndTime;
                                            row1.lBreakTime = GetTimeDiff(lWorkEndTime, lBreakEndTime);
                                            lBreakTotal += row1.lBreakTime;
                                            row1.sTimeSpan = GetTimeSpan(lWorkEndTime, lBreakEndTime);
                                            hrActivitiesResponse.rows.Add(row1);
                                            lWorkEndTime = 0;
                                            sWorkEndTime = "";
                                            iMode = S_NONE;
                                        }

                                        //---------------------------------------
                                        if (iMode == S_LOCKED) // Already locked
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sBreakStartTime;
                                            row.sPostNote = sBreakEndTime;
                                            row.lBreakTime = GetTimeDiff(lBreakStartTime, lBreakEndTime);
                                            lBreakTotal += row.lBreakTime;
                                            row.sTimeSpan = GetTimeSpan(lBreakStartTime, lBreakEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }
                                        else if (iMode == S_OPENED) // Locking now.
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sWorkStartTime;
                                            row.sPostNote = sBreakEndTime;
                                            row.lBreakTime = GetTimeDiff(lWorkStartTime, lBreakEndTime);
                                            lBreakTotal += row.lBreakTime;
                                            row.sTimeSpan = GetTimeSpan(lWorkStartTime, lBreakEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }
                                        else if (iMode == S_START)
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sBreakStartTime;
                                            row.sPostNote = sBreakEndTime;
                                            row.lBreakTime = GetTimeDiff(lBreakStartTime, lBreakEndTime);
                                            lBreakTotal += row.lBreakTime;
                                            row.sTimeSpan = GetTimeSpan(lBreakStartTime, lBreakEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }

                                        //---------------------------------------
                                        lWorkStartTime = actTime;
                                        sWorkStartTime = act + GetReasonHead(reader);
                                        lBreakStartTime = 0;
                                        lBreakEndTime = 0;
                                        sBreakStartTime = "";
                                        sBreakEndTime = "";
                                        iMode = S_OPENED;
                                    }
                                    else if (act.Equals("lock") || act.Equals("forcedlock"))
                                    {
                                        lActualEnd = actTime;// + 19800;
                                        lWorkEndTime = actTime;// + 19800;
                                        sWorkEndTime = act + GetReasonHead(reader);
                                        //---------------------------------------
                                        if (iMode == S_LOCKED) // Already locked
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sWorkStartTime;
                                            row.sPostNote = sWorkEndTime;
                                            row.lWorkTime = GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                            if (act.Equals("forcedlock")) row.lWorkTime -= lLocktime;
                                            lWorkTotal += row.lWorkTime;
                                            row.sTimeSpan = GetTimeSpan(lWorkStartTime, lWorkEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }
                                        else if (iMode == S_OPENED) // Locking now.
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sWorkStartTime;
                                            row.sPostNote = sWorkEndTime;
                                            row.lWorkTime = GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                            if (act.Equals("forcedlock")) row.lWorkTime -= lLocktime;
                                            lWorkTotal += row.lWorkTime;
                                            row.sTimeSpan = GetTimeSpan(lWorkStartTime, lWorkEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }
                                        else if (iMode == S_START) // Locking now.
                                        {
                                            DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                            row.sPreNote = sWorkStartTime;
                                            row.sPostNote = sWorkEndTime;
                                            row.lWorkTime = GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                            if (act.Equals("forcedlock")) row.lWorkTime -= lLocktime;
                                            lWorkTotal += row.lWorkTime;
                                            row.sTimeSpan = GetTimeSpan(lWorkStartTime, lWorkEndTime);
                                            hrActivitiesResponse.rows.Add(row);
                                        }

                                        //---------------------------------------
                                        lBreakStartTime = actTime;
                                        sBreakStartTime = act + GetReasonHead(reader);
                                        lWorkStartTime = 0;
                                        lWorkEndTime = 0;
                                        sWorkStartTime = "";
                                        sWorkStartTime = "";
                                        iMode = S_LOCKED;
                                    }
                                    else if (act.Equals("approved") || act.Equals("requested"))
                                    {
                                        DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                        row.sPreNote = act;
                                        row.sPostNote = GetReasonHead(reader);
                                        row.sTimeSpan = MyGlobal.ToDateTimeFromEpoch(actTime).ToString("yyyy-MM-dd HH:mm:ss");
                                        row.lWorkTime = reader.GetInt32(F_WORK_TIME);
                                        hrActivitiesResponse.rows.Add(row);
                                        if (act.Equals("approved"))
                                        {
                                            if (!reader.IsDBNull(6)) lWorkTotal += reader.GetInt32(6);

                                            if (actTime == shift_start)
                                            {
                                                hrActivitiesResponse.lLateSeconds = 0;
                                                bLateLoginAproved = true;
                                            }
                                        }
                                    }
                                    else if (act.Equals("lockstate"))
                                    {
                                        lActualEnd = actTime;
                                        //--------------------------------------------

                                        //---------------------------------------
                                        lBreakEndTime = actTime;
                                        sBreakEndTime = act + GetReasonHead(reader);
                                        iMode = S_LOCKED;
                                    }
                                    else if (act.Equals("update"))
                                    {
                                        if (lActualStart == 0)
                                        {
                                            // In case of first is update, it carry some worktime
                                            lActualStart = actTime;
                                            //- reader.GetInt64(F_WORK_TIME);
                                        }
                                        if (lWorkStartTime == 0)
                                            lWorkStartTime = actTime;
                                        //- reader.GetInt64(F_WORK_TIME);
                                        lActualEnd = actTime;
                                        //- reader.GetInt64(F_WORK_TIME);
                                        //--------------------------------------------


                                        //---------------------------------------
                                        lWorkEndTime = actTime;
                                        sWorkEndTime = act + GetReasonHead(reader);
                                        iMode = S_OPENED;
                                    }
                                } // End while
                                  //----------------Update the balance
                                if (lWorkEndTime > 0)
                                {
                                    DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                    row.sPreNote = sWorkStartTime;
                                    row.sPostNote = sWorkEndTime;
                                    row.lWorkTime = GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                    lWorkTotal += row.lWorkTime;
                                    row.sTimeSpan = GetTimeSpan(lWorkStartTime, lWorkEndTime);
                                    hrActivitiesResponse.rows.Add(row);
                                }
                            }
                        }
                    }
                    //----------------------------------------------------
                    sSQL = "select (m_ActivityTime - m_ActivityStart),m_Activity," +
                        "m_ActivityStart,m_ActivityTime from " +
                        MyGlobal.activeDB + ".tbl_terminals " +
                        "where (m_Activity = 'open' or m_Activity = 'update') " +
                        "and m_Profile = '" + profile + "' " +
                        "and m_StaffID = '" + staff + "' " +
                        "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                        "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                        "order by m_ActivityTime desc limit 1";

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
                                        if (reader.GetInt16(0) < 1800)  // 1800 is 30 mts or abnormal
                                        {
                                            if (reader.GetInt16(0) > 0)
                                            {
                                                DisplayedColumns_Roster_Consolidated_Row row = new DisplayedColumns_Roster_Consolidated_Row();
                                                row.sPreNote = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                                row.sPostNote = "active...";
                                                row.lWorkTime = reader.GetInt16(0);
                                                lWorkTotal += row.lWorkTime;
                                                if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                                                {
                                                    if (reader.GetInt32(2) > 0 && reader.GetInt32(3) > 0)
                                                    {
                                                        row.sTimeSpan = GetTimeSpan(
                                                            reader.GetInt32(2) + 19800,
                                                            reader.GetInt32(3) + 19800);
                                                        hrActivitiesResponse.rows.Add(row);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------------------------------
                    hrActivitiesResponse.lActualStart = lActualStart;
                    hrActivitiesResponse.lActualEnd = lActualEnd;
                    hrActivitiesResponse.lWorkTotal = lWorkTotal;
                    hrActivitiesResponse.lBreakTotal = lBreakTotal;
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("LoadReport_RosterShiftCombos-Exception->" + ex.Message);
            }
            return false;
        }
        private string GetReasonHead(MySqlDataReader reader)
        {
            string sRet = "";
            if (!reader.IsDBNull(8))
            {
                if (reader.GetString(8).Length > 0) sRet = " (" + reader.GetString(8) + ")";
            }
            if (!reader.IsDBNull(9))
            {
                if (reader.GetString(9).Length > 0)
                {
                    string[] arData = reader[9].ToString().Split(new string[] { "*_*" }, StringSplitOptions.None);
                    if (arData.Length >= 3)
                    {
                        if (arData[0].Length > 0) sRet += " [" + arData[0] + "]";
                    }
                    else
                    {
                        sRet += " [" + reader[9].ToString() + "]";
                    }
                }
            }
            return sRet;
        }
        private string GetTimeSpan(long lWorkStartTime, long lWorkEndTime)
        {
            return (lWorkStartTime > 0 ?
                (MyGlobal.ToDateTimeFromEpoch(lWorkStartTime).ToString("yyyy-MM-dd HH:mm:ss") +
                    " - ") : "") +
                MyGlobal.ToDateTimeFromEpoch(lWorkEndTime).ToString("HH:mm:ss");
        }
        private long GetTimeDiff(long lStartTime, long lEndTime)
        {
            if (lStartTime == 0) return 0;
            if (lEndTime == 0) return 0;
            return (lEndTime - lStartTime);
        }
        private string GetNote(string sReasonNote)
        {
            string[] arData = sReasonNote.Split(new string[] { "*_*" }, StringSplitOptions.None);
            if (arData.Length >= 3)
            {
                return arData[0];
                //sAdminEmailOfConcernEvent = arData[1];
                //sAdminNameOfConcernEvent = arData[2];
            }
            else
            {
                return sReasonNote;
            }
        }
        private void GetStaffCountExpected(MySqlConnection con, string profile,
            string m_RosterName, string m_ShiftName, int dtYear, int dtMonth, int dtDay,
            out long lStaffCount)
        {
            lStaffCount = 0;
            string sSQL = "SELECT count(m_StaffID) as cnt FROM " + MyGlobal.activeDB + ".tbl_rosters " +
"where m_RosterName = '" + m_RosterName + "' " +
"and m_ShiftName = '" + m_ShiftName + "' and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
"and m_Day" + dtDay + "='" + MyGlobal.WORKDAY_MARKER + "' " +
"and m_StaffID is not null;";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) lStaffCount = reader.GetInt32(0);
                        }
                    }
                }
            }
        }
        //-----------------------------------------------------------------------
        private void GetStaffWorkHours_OLD(MySqlConnection con, string profile,
            long shift_start, long shift_end, string m_StaffID,
             out long lWorkhours)
        {
            lWorkhours = 0; string sSQL = "";
            long locktime = 0;
            //----------------------------------------------Get Lock Time
            sSQL = "select teams.m_LockTime from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                "left join " + MyGlobal.activeDB + ".tbl_misc_teams teams on teams.m_Profile = staff.m_Profile and teams.m_Name = staff.m_Team " +
                "where staff.m_Profile = '" + profile + "' and staff.m_StaffID='" + m_StaffID + "'";
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
                                locktime = reader.GetInt16(0) * 60;
                            }
                        }
                    }
                }
            }
            //----------------------------------
            sSQL =
            "select sum(worktime) from( " +

            "SELECT m_StaffID as staffid, sum(m_WorkTime) as worktime,m_Activity FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
            "(m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'update' or m_Activity = 'approved') " +
            "and m_ActivityTime >= " + (shift_start - 19800 - const_ShiftPaddingPre) + " " +
            "and m_ActivityTime < " + (shift_end - 19800 + const_ShiftPaddingPost) + " " +
            "and m_StaffID is not null and m_StaffID = '" + m_StaffID + "' group by m_id " +

            "union all " +

            "select * from (" +
            "SELECT m_StaffID as staffid, ((-1)*m_WorkTime) as worktime,m_Activity FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
            "where m_ActivityTime >= " + (shift_start - 19800 - const_ShiftPaddingPre) + " " +
            "and m_ActivityTime < " + (shift_end - 19800 + const_ShiftPaddingPost) + " " +
            "and m_StaffID is not null and m_StaffID = '" + m_StaffID + "' limit 1 " +
            ") as vvv where m_Activity='update' " +

            "union all " +

            "SELECT m_StaffID as staffid, ((-1)*count(m_id)*" + locktime + ") as worktime,m_Activity FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
            "(m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'update' or m_Activity = 'approved') " +
            "and m_ActivityTime >= " + (shift_start - 19800 - const_ShiftPaddingPre) + " " +
            "and m_ActivityTime < " + (shift_end - 19800 + const_ShiftPaddingPost) + " " +
            "and m_StaffID is not null and m_StaffID = '" + m_StaffID + "' and m_Activity='forcedlock' group by m_id " +

            ") as xxx ";
            /*
             *                                             "union all " +
                        "select m_StaffID as staffid,(m_ActivityTime - m_ActivityStart) as worktime " +
                        "from " + MyGlobal.activeDB + ".tbl_terminals " +
                        "where (m_ActivityTime >= " + (shift_start - const_ShiftPaddingPre - 19800) +
                        " and m_ActivityTime<" + (shift_end + const_ShiftPaddingPost - 19800) + ") " +
                        "and m_Activity = 'open' and m_StaffID is not null and m_StaffID = '" + m_StaffID + "' " +
             */

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) lWorkhours = reader.GetInt32(0);
                        }
                    }
                }
            }
        }
        private void GetStaffWorkHours(string profile,
            long shift_start, long shift_end, string m_StaffID,
            out long lWorkhours)
        {
            long ActualStart = 0, ActualEnd = 0, lWorkApproved = 0;
            int iLateLoginStatus = 0;
            GetStaffWorkHours_with_Start_and_End(profile,
             shift_start, shift_end, m_StaffID,
            out lWorkhours, out lWorkApproved, out ActualStart, out ActualEnd, out iLateLoginStatus);
        }
        private void GetStaffWorkHours_with_Start_and_End(string profile,
            long shift_start, long shift_end, string m_StaffID,
            out long lWorkhours, out long lWorkApproved, out long lActualStart, out long lActualEnd,
            out int iLateLoginStatus)
        {
            lWorkhours = 0;
            lWorkApproved = 0;
            //long lActualStart = 0, lActualEnd = 0;
            lActualStart = 0;
            lActualEnd = 0;
            string sSQL = "";
            long lLocktime = 0;
            iLateLoginStatus = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                //----------------------------------------------Get Lock Time
                sSQL = "select teams.m_LockTime from " + MyGlobal.activeDB + ".tbl_staffs staff " +
                "left join " + MyGlobal.activeDB + ".tbl_misc_teams teams on teams.m_Profile = staff.m_Profile and teams.m_Name = staff.m_Team " +
                "where staff.m_Profile = '" + profile + "' and staff.m_StaffID='" + m_StaffID + "'";
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
                                    lLocktime = reader.GetInt16(0) * 60;
                                }
                            }
                        }
                    }
                }
                //----------------------------------
                sSQL = @"SELECT * from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + m_StaffID + "' " +
                    "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                    "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                    "order by m_ActivityTime;";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            const int S_NONE = 0;
                            const int S_START = 1;
                            const int S_OPENED = 2;
                            const int S_LOCKED = 3;
                            //-------------------------------------------
                            const int F_HARDWARE_ID = 1;
                            const int F_ACTIVITY = 4;
                            const int F_ACTIVITY_TIME = 5;
                            const int F_WORK_TIME = 6;

                            int iMode = 0;

                            long lWorkTotal = 0, lBreakTotal = 0;
                            long lBreakStartTime = 0, lBreakEndTime = 0;
                            long lWorkStartTime = 0, lWorkEndTime = 0;
                            string lastHardwareID = "";
                            while (reader.Read())
                            {
                                string act = reader.GetString(F_ACTIVITY);
                                long actTime = reader.GetInt64(F_ACTIVITY_TIME) + 19800;
                                // This care was taken to compensate the system change over error
                                if (lastHardwareID.Length == 0) lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                if (!lastHardwareID.Equals(reader.GetString(F_HARDWARE_ID)))
                                {
                                    if (act.Equals("open"))
                                    {
                                        lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                        continue;
                                    }
                                }
                                lastHardwareID = reader.GetString(F_HARDWARE_ID);
                                // This care was taken to compensate the system change over error END
                                if (act.Equals("open"))
                                {
                                    if (lActualStart == 0) lActualStart = actTime;
                                    lBreakEndTime = actTime;
                                    //---------------------------------------
                                    if (lWorkEndTime > 0)
                                    {
                                        lWorkTotal += GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                        //------------------------------------------------------------
                                        lBreakTotal += GetTimeDiff(lWorkEndTime, lBreakEndTime);
                                        lWorkEndTime = 0;
                                        iMode = S_NONE;
                                    }

                                    //---------------------------------------
                                    if (iMode == S_LOCKED) // Already locked
                                    {
                                        lBreakTotal += GetTimeDiff(lBreakStartTime, lBreakEndTime);
                                    }
                                    else if (iMode == S_OPENED) // Locking now.
                                    {
                                        lBreakTotal += GetTimeDiff(lWorkStartTime, lBreakEndTime);
                                    }
                                    else if (iMode == S_START)
                                    {
                                        lBreakTotal += GetTimeDiff(lBreakStartTime, lBreakEndTime);
                                    }

                                    //---------------------------------------
                                    lWorkStartTime = actTime;

                                    lBreakStartTime = 0;
                                    lBreakEndTime = 0;
                                    iMode = S_OPENED;
                                }
                                else if (act.Equals("lock") || act.Equals("forcedlock"))
                                {
                                    lActualEnd = actTime;// + 19800;
                                    lWorkEndTime = actTime;// + 19800;

                                    //---------------------------------------
                                    if (iMode == S_LOCKED) // Already locked
                                    {
                                        lWorkTotal += GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                        if (act.Equals("forcedlock"))
                                        {
                                            if (lWorkTotal > lLocktime) lWorkTotal -= lLocktime;
                                        }
                                    }
                                    else if (iMode == S_OPENED) // Locking now.
                                    {
                                        lWorkTotal += GetTimeDiff(lWorkStartTime, lWorkEndTime);
                                        if (act.Equals("forcedlock"))
                                        {
                                            if (lWorkTotal > lLocktime) lWorkTotal -= lLocktime;
                                        }
                                    }
                                    else if (iMode == S_START) // Locking now.
                                    {
                                        lWorkTotal += GetTimeDiff(lWorkStartTime, lWorkEndTime);

                                        if (act.Equals("forcedlock"))
                                        {
                                            if (lWorkTotal > lLocktime) lWorkTotal -= lLocktime;
                                        }
                                    }

                                    //---------------------------------------
                                    lBreakStartTime = actTime;
                                    lWorkStartTime = 0;
                                    lWorkEndTime = 0;
                                    iMode = S_LOCKED;
                                }
                                else if (act.Equals("approved"))
                                {
                                    if (lActualStart == 0) lActualStart = actTime;
                                    if (!reader.IsDBNull(F_WORK_TIME))
                                    {
                                        lWorkTotal += reader.GetInt32(F_WORK_TIME);
                                        lWorkApproved += reader.GetInt32(F_WORK_TIME);
                                    }
                                }
                                else if (act.Equals("lockstate"))
                                {
                                    lActualEnd = actTime;
                                    //--------------------------------------------

                                    //---------------------------------------
                                    lBreakEndTime = actTime;
                                    iMode = S_LOCKED;
                                }
                                else if (act.Equals("update"))
                                {
                                    if (lActualStart == 0)
                                    {
                                        // In case of first is update, it carry some worktime
                                        lActualStart = actTime;
                                        //- reader.GetInt64(F_WORK_TIME);
                                    }
                                    if (lWorkStartTime == 0)
                                        lWorkStartTime = actTime;
                                    //- reader.GetInt64(F_WORK_TIME);
                                    lActualEnd = actTime;
                                    //- reader.GetInt64(F_WORK_TIME);
                                    //--------------------------------------------


                                    //---------------------------------------
                                    lWorkEndTime = actTime;
                                    iMode = S_OPENED;
                                }
                                else if (act.Equals("accepted"))
                                {
                                    if ((shift_start + const_ShiftPaddingPre) == actTime)
                                    {
                                        iLateLoginStatus = 1;
                                    }
                                }
                            } // End while
                              //----------------Update the balance
                            if (lWorkEndTime > 0)
                            {
                                lWorkTotal += GetTimeDiff(lWorkStartTime, lWorkEndTime);
                            }
                            lWorkhours = lWorkTotal;
                            if (lWorkhours < 0)
                            {
                                Console.WriteLine("lWorkhours negative");
                                lWorkhours = 0;
                            }
                            //----------------------------
                            //hrActivitiesResponse.lActualStart = lActualStart;
                            //hrActivitiesResponse.lActualEnd = lActualEnd;
                            //hrActivitiesResponse.lWorkTotal = lWorkTotal;
                            //hrActivitiesResponse.lBreakTotal = lBreakTotal;

                        }
                    }
                }
                //---------------------------------- Get pending time from tbl_terminals
                sSQL = "select (m_ActivityTime - m_ActivityStart) from " +
                    MyGlobal.activeDB + ".tbl_terminals " +
                    "where (m_Activity = 'open' or m_Activity = 'update') " +
                    "and m_Profile = '" + profile + "' " +
                    "and m_StaffID = '" + m_StaffID + "' " +
                    "and m_ActivityTime>='" + (shift_start - 19800 - const_ShiftPaddingPre) + "' " +
                    "and m_ActivityTime<'" + (shift_end - 19800 + const_ShiftPaddingPost) + "' " +
                    "order by m_ActivityTime desc limit 1";
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
                                    if (reader.GetInt16(0) < 1800)  // 1800 is 30 mts
                                    {
                                        lWorkhours += reader.GetInt16(0);
                                        if (lWorkhours < 0)
                                        {
                                            Console.WriteLine("lWorkhours negative error");
                                            lWorkhours = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //----------------------------------
        }
        //-----------------------------------------------------------------------
        private void GetStaffCountAndWorkhouse(MySqlConnection con, string profile,
            DisplayedColumns_Roster_Consolidated_Row row,
            out long lStaffCount, out long lWorkhours)
        {
            lStaffCount = 0;
            lWorkhours = 0;
            string sSQL =
            //,sum(worktime)
            "select count(staffid) from( " +
            "SELECT m_StaffID as staffid, sum(m_WorkTime) as worktime FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity where " +
            "m_Profile='" + profile + "' " +
            "and (m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'update' or m_Activity = 'approved') " +
            "and m_ActivityTime >= " + (row.shift_start - 19800 - const_ShiftPaddingPre) + " and m_ActivityTime < " + (row.shift_end - 19800 + const_ShiftPaddingPost) + " and m_StaffID is not null and m_StaffID <> '' " +
            "and m_StaffID in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_rosters where m_RosterName='" + row.m_RosterName + "' and m_ShiftName='" + row.m_ShiftName + "' and m_Profile='" + profile + "' and m_StaffID is not null and m_StaffID <> '') " +
            "group by m_StaffID " +
            ") as xxx ";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) lStaffCount = reader.GetInt32(0);
                            //if (!reader.IsDBNull(1)) lWorkhours = reader.GetInt32(1);
                        }
                    }
                }
            }
        }
        //-----------------------------------------------------------------------
        [HttpPost]
        public ActionResult GetStaffResponse(string profile, string email)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var getStaffResponse = new GetStaffResponse();
            getStaffResponse.status = false;
            getStaffResponse.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (email.Length == 0) email = "m_Email is null";
                    else email = "m_Email='" + email + "'";
                    string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where  m_Profile='" + profile + "' and " + email + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    StaffItem staffItem = new StaffItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) staffItem.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) staffItem.m_FName = reader["m_FName"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_MName"))) staffItem.m_Name += " " + reader["m_MName"].ToString();
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_LName"))) staffItem.m_Name += " " + reader["m_LName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) staffItem.m_StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Username"))) staffItem.m_Username = reader["m_Username"].ToString();// + "["+MyGlobal.activeDB+"]";
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) staffItem.m_Mobile = reader["m_Mobile"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) staffItem.m_Email = reader["m_Email"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) staffItem.m_Designation = reader["m_Designation"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Roll"))) staffItem.m_Roll = reader["m_Roll"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) staffItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) staffItem.m_Base = reader["m_Base"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) staffItem.m_Type = reader["m_Type"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToFunctional"))) staffItem.m_ReportToFunctional = reader["m_ReportToFunctional"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToAdministrative"))) staffItem.m_ReportToAdministrative = reader["m_ReportToAdministrative"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MenuKey"))) staffItem.m_MenuKey = reader["m_MenuKey"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) staffItem.m_Band = reader["m_Band"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) staffItem.m_Grade = reader["m_Grade"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mrs"))) staffItem.m_Mrs = reader["m_Mrs"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOB"))) staffItem.m_DOB = reader.GetDateTime(reader.GetOrdinal("m_DOB"));// reader["m_DOB"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOJ"))) staffItem.m_DOJ = reader.GetDateTime(reader.GetOrdinal("m_DOJ"));//.ToString("yyyy-MM-dd")
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOA"))) staffItem.m_DOA = reader.GetDateTime(reader.GetOrdinal("m_DOA"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_LWD"))) staffItem.m_LWD = reader.GetDateTime(reader.GetOrdinal("m_LWD"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) staffItem.m_Status = reader["m_Status"].ToString();
                                    int iOrd = reader.GetOrdinal("m_ViewSelected");
                                    if (!reader.IsDBNull(iOrd)) staffItem.m_ViewSelected = reader.GetInt16(iOrd);

                                    iOrd = reader.GetOrdinal("m_Lock");
                                    if (!reader.IsDBNull(iOrd)) staffItem.m_Lock = reader.GetInt16(iOrd);

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_AttendanceMethod")))
                                    {
                                        staffItem.m_AttendanceMethod = reader.GetString(reader.GetOrdinal("m_AttendanceMethod"));
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Bank"))) staffItem.m_Bank = reader["m_Bank"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_AccountNo"))) staffItem.m_AccountNo = reader["m_AccountNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Branch"))) staffItem.m_Branch = reader["m_Branch"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_IFSC"))) staffItem.m_IFSC = reader["m_IFSC"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_EPF_UAN"))) staffItem.m_EPF_UAN = reader["m_EPF_UAN"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ESICNumber"))) staffItem.m_ESICNumber = reader["m_ESICNumber"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_AttendanceSource"))) staffItem.m_AttendanceSource = reader["m_AttendanceSource"].ToString();

                                    staffItem.m_AADHAR_Uploaded = MyGlobal.GetPureString(reader, "m_AADHAR_Uploaded");
                                    staffItem.m_AADHAR_Number = MyGlobal.GetPureString(reader, "m_AADHAR_Number");
                                    staffItem.m_AADHAR_Name = MyGlobal.GetPureString(reader, "m_AADHAR_Name");
                                    staffItem.m_AADHAR_FatherName = MyGlobal.GetPureString(reader, "m_AADHAR_FatherName");

                                    staffItem.m_PAN_Uploaded = MyGlobal.GetPureString(reader, "m_PAN_Uploaded");
                                    staffItem.m_PAN_Number = MyGlobal.GetPureString(reader, "m_PAN_Number");
                                    staffItem.m_PAN_Name = MyGlobal.GetPureString(reader, "m_PAN_Name");
                                    staffItem.m_PAN_FatherName = MyGlobal.GetPureString(reader, "m_PAN_FatherName");

                                    staffItem.m_CCTNo = MyGlobal.GetPureString(reader, "m_CCTNo");
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CCTCleardDate"))) staffItem.m_CCTCleardDate = reader.GetDateTime(reader.GetOrdinal("m_CCTCleardDate")).ToString("dd-MM-yyyy");
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_RetentionBonusEffectiveDate"))) staffItem.m_RetentionBonusEffectiveDate = reader.GetDateTime(reader.GetOrdinal("m_RetentionBonusEffectiveDate")).ToString("dd-MM-yyyy");
                                    staffItem.m_RetentionBonusAmount = MyGlobal.GetPureString(reader, "m_RetentionBonusAmount");
                                    /*
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale")))
                                    {
                                        staffItem.m_Payscale = reader["m_Payscale"].ToString();
                                    }
                                    */
                                    Int32 key = 0;
                                    Int32 startdate = 0;
                                    //New User Creation error solved 27-05-2024 Starts 
                                    staffItem.m_PayscaleName = ((staffItem.m_StaffID != null && staffItem.m_StaffID != "") ? GetActivePayscale(profile, staffItem.m_StaffID, out key, out startdate) : null);
                                    //ends
                                    staffItem.m_PayscaleKey = key;
                                    staffItem.m_PayscaleStartDate = startdate;
                                    getStaffResponse.staffItem = staffItem;
                                }
                                getStaffResponse.status = true;
                                getStaffResponse.result = "";
                            }
                            else
                            {
                                getStaffResponse.result = "<span style='color:red;'>Sorry!!! No Data</span>";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetStaffResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetStaffResponse-Exception->" + ex.Message);
            }
            return Json(getStaffResponse, JsonRequestBehavior.AllowGet);
        }
        private string GetActivePayscaleForThisDay(string profile, Int32 unixDayStart, string staffid, out Int32 key, out Int32 startdate)
        {
            key = 0;
            startdate = 0;
            //Int32 unixTimestampDayStart = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sPayscaleByEffectiveTable = "";

                string sSQL = "SELECT eff.m_Payscale,mst.m_Key,eff.m_StartDate FROM " + MyGlobal.activeDB + ".tbl_payscale_effective eff " +
                    "left join " + MyGlobal.activeDB + ".tbl_payscale_master_list mst on mst.m_Name = eff.m_Payscale and mst.m_Key <= '" + unixDayStart + "' and mst.m_Profile=eff.m_Profile " +
                    "where m_StaffID = '" + staffid + "' and m_StartDate<= '" + unixDayStart + "' and eff.m_Profile = '" + profile + "' " +
                    "order by eff.m_StartDate desc, mst.m_Key desc limit 1";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                sPayscaleByEffectiveTable = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                key = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                startdate = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            }
                        }
                    }
                }
                return sPayscaleByEffectiveTable;
            }
        }
        private string GetActivePayscale(string profile, string staffid, out Int32 key, out Int32 startdate)
        {
            key = 0;
            startdate = 0;
            Int32 unixTimestampDayStart = (Int32)(DateTime.Today.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sPayscaleByEffectiveTable = "";


                string sSQL = "SELECT eff.m_Payscale,mst.m_Key,eff.m_StartDate FROM " + MyGlobal.activeDB + ".tbl_payscale_effective eff " +
                    "left join " + MyGlobal.activeDB + ".tbl_payscale_master_list mst on mst.m_Name = eff.m_Payscale and mst.m_Key <= '" + unixTimestampDayStart + "' and mst.m_Profile=eff.m_Profile " +
                    "where m_StaffID = '" + staffid + "' and m_StartDate<= '" + unixTimestampDayStart + "' and eff.m_Profile = '" + profile + "' " +
                    "order by eff.m_StartDate desc, mst.m_Key desc limit 1";

                //string sSQL = "SELECT eff.m_Payscale,mst.m_Key,eff.m_StartDate FROM " + MyGlobal.activeDB + ".tbl_payscale_effective eff " +
                //    "left join " + MyGlobal.activeDB + ".tbl_payscale_master_list mst on mst.m_Name = eff.m_Payscale and mst.m_Key <= '" + unixTimestampDayStart + "' and mst.m_Profile=eff.m_Profile " +
                //    "where m_StaffID = ' ' and m_StartDate<= '" + unixTimestampDayStart + "' and eff.m_Profile = '" + profile + "' " +
                //    "order by eff.m_StartDate desc, mst.m_Key desc limit 1";




                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                sPayscaleByEffectiveTable = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                key = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                startdate = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            }
                        }
                    }
                }
                return sPayscaleByEffectiveTable;
                /*
            string sSQL = "SELECT m_Payscale,m_StartDate FROM " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                "where  m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                "and m_Payscale is not null " +
                "and m_StartDate<='" + unixTimestampDayStart + "' " +
                "order by m_StartDate desc limit 1;";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            sPayscaleByEffectiveTable = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            startdate = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        }
                    }
                }
            }
            if (sPayscaleByEffectiveTable.Length == 0)
            {
                startdate = 0;
                return "";
            }
            sSQL = "SELECT m_Key FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                "where  m_Profile='" + profile + "' " +
                "and m_Key<='" + unixTimestampDayStart + "' " +
                "and m_Name='" + sPayscaleByEffectiveTable + "' " +
                "order by m_Key desc limit 1;";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            key = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        }
                    }
                }
            }
            if (key == 0)
            {
                startdate = 0;
                return "";
            }
            return sPayscaleByEffectiveTable;
            */

                /*
                string sSQL = "SELECT m_Payscale,m_Key,m_StartDate FROM " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                        "where  m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Payscale is not null " +
                        "and m_Key<='" + unixTimestampDayStart + "' " +
                        "and m_StartDate<='" + unixTimestampDayStart + "' " +
                        "order by m_StartDate desc limit 1;";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                key = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                startdate = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                                return reader.GetString(0);
                            }
                        }
                    }
                }
                */
            }
        }
        //------------------------------------------------------
        public ActionResult Splash(string profile, string staffid, string rnd)
        {
            ViewBag.profile = profile;
            ViewBag.staffid = staffid;
            ViewBag.rnd = rnd;
            return PartialView();
        }
        //------------------------------------------------------

        [HttpPost]
        public ActionResult StaffSearchResponse(string profile, string search, string count)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var staffSearchResponse = new StaffSearchResponse();
            staffSearchResponse.status = false;
            staffSearchResponse.result = "";
            if (search.Length == 0)
            {
                return Json(staffSearchResponse, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int cnt = MyGlobal.GetInt16(count);
                    if (cnt == 0) cnt = 6;
                    string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where  m_Profile='" + profile + "' " +
                            "and (m_FName like '%" + search + "%' or m_StaffID like '%" + search + "%') " +
                            "order by m_Name limit " + cnt;

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    StaffDetail staffItem = new StaffDetail();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) staffItem.Name = reader["m_FName"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) staffItem.StaffID = reader["m_StaffID"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) staffItem.m_Band = reader["m_Band"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) staffItem.m_Grade = reader["m_Grade"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) staffItem.m_Team = reader["m_Team"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) staffItem.m_Base = reader["m_Base"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) staffItem.m_Designation = reader["m_Designation"].ToString();
                                    staffSearchResponse.staffs.Add(staffItem);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("StaffSearchResponse-MySqlException->" + ex.Message);
                staffSearchResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("StaffSearchResponse-Exception->" + ex.Message);
                staffSearchResponse.result = ex.Message;
            }
            return Json(staffSearchResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult PayslipMaster(string profile, string name, string key, string startdate, string mode,
            string CTC, string crdr, string moveto, PayslipRow row, string force)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var payslipMaster = new PayslipMaster();
            payslipMaster.status = false;
            payslipMaster.result = "";
            payslipMaster.name = name;
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("newcr"))
                    {
                        //-----------------------------------
                        bool bEmptyRecordsExists = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and (m_Ledger is null or m_Ledger='') and m_Type='cr';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bEmptyRecordsExists = reader.HasRows;
                            }
                        }
                        if (!bEmptyRecordsExists)
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "(m_Profile,m_Name,m_Key,m_Type,m_Ledger,m_Order) values ('" + profile + "','" + name + "','" + key + "'," +
                                "'cr'," +
                                "'',(SELECT COALESCE(MAX(m_Order),0) FROM " + MyGlobal.activeDB + ".tbl_payscale_master C) +1);";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            payslipMaster.result = "Empty exists ";
                        }
                    }
                    if (mode.Equals("newcro"))
                    {
                        //-----------------------------------
                        bool bEmptyRecordsExists = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and (m_Ledger is null or m_Ledger='') and m_Type='cro';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bEmptyRecordsExists = reader.HasRows;
                            }
                        }
                        if (!bEmptyRecordsExists)
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "(m_Profile,m_Name,m_Key,m_Type,m_Ledger,m_Order) values ('" + profile + "','" + name + "','" + key + "'," +
                                "'cro'," +
                                "'',(SELECT COALESCE(MAX(m_Order),0) FROM " + MyGlobal.activeDB + ".tbl_payscale_master C) +1);";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            payslipMaster.result = "Empty exists ";
                        }
                    }
                    else if (mode.Equals("newdr"))
                    {
                        //-----------------------------------
                        bool bEmptyRecordsExists = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and (m_Ledger is null or m_Ledger='') and m_Type='dr';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bEmptyRecordsExists = reader.HasRows;
                            }
                        }
                        if (!bEmptyRecordsExists)
                        {

                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "(m_Profile,m_Name,m_Key,m_Type,m_Ledger,m_Order) values ('" + profile + "','" + name + "','" + key + "'," +
                                "'dr'," +
                                "'',(SELECT COALESCE(MAX(m_Order),0) FROM " + MyGlobal.activeDB + ".tbl_payscale_master C) +1);";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            payslipMaster.result = "Empty exists ";
                        }
                    }
                    else if (mode.Equals("newdro"))
                    {
                        //-----------------------------------
                        bool bEmptyRecordsExists = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and (m_Ledger is null or m_Ledger='') and m_Type='dro';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bEmptyRecordsExists = reader.HasRows;
                            }
                        }
                        if (!bEmptyRecordsExists)
                        {

                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "(m_Profile,m_Name,m_Key,m_Type,m_Ledger,m_Order) values ('" + profile + "','" + name + "','" + key + "'," +
                                "'dro'," +
                                "'',(SELECT COALESCE(MAX(m_Order),0) FROM " + MyGlobal.activeDB + ".tbl_payscale_master C) +1);";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            payslipMaster.result = "Empty exists ";
                        }
                    }
                    else if (mode.Equals("save"))
                    {

                        bool bDoesThisLedgerExistsInAccounts = false;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where  m_Profile='" + profile + "' and m_Name='" + row.ledger.name + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bDoesThisLedgerExistsInAccounts = reader.HasRows;
                            }
                        }
                        if (!bDoesThisLedgerExistsInAccounts)
                        {

                            if (force != null && force.Equals("1"))
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                                    "(m_Profile,m_Name) values ('" + profile + "','" + row.ledger.name + "');";

                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                    bDoesThisLedgerExistsInAccounts = true;
                                    payslipMaster.result = "Accounts Ledger Created. ";
                                }
                            }
                            else
                            {

                                payslipMaster.result = "Accounts ledger is not available";
                            }
                        }
                        if (bDoesThisLedgerExistsInAccounts)
                        {
                            string _Payscale = "", _Key = "", _Ledger = "";
                            sSQL = "SELECT m_Name,m_Key,m_Ledger FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_id='" + row.m_id + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            _Payscale = MyGlobal.GetPureString(reader, "m_Name");
                                            _Key = MyGlobal.GetPureString(reader, "m_Key");
                                            _Ledger = MyGlobal.GetPureString(reader, "m_Ledger");
                                        }
                                    }
                                }
                            }
                            if (_Payscale.Length > 0)
                            {
                                MySqlTransaction trans = con.BeginTransaction();
                                MySqlCommand myCommand = con.CreateCommand();
                                myCommand.Connection = con;
                                myCommand.Transaction = trans;
                                try
                                {
                                    sSQL = "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "Set m_BasedOn = '" + row.basedon + "',m_Amount = '" + row.amount + "' " +
                                    "where m_Profile='" + profile + "' " +
                                    "and m_Name='" + _Payscale + "'" +
                                    "and m_Key='" + _Key + "'" +
                                    "and m_Ledger='" + _Ledger + "' " +
                                    "and (m_Type='earn' or m_Type='deduct')";
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();
                                    //-----------------------
                                    sSQL = "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "Set m_Ledger='" + row.ledger.name + "'," +
                                    "m_BasedOn='" + row.basedon + "',m_Amount='" + row.amount + "' " +
                                    "where  m_Profile='" + profile + "' " +
                                    "and m_Name = '" + name + "' " +
                                    "and m_Key = '" + key + "' " +
                                    "and m_id = '" + row.m_id + "';";
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();
                                    trans.Commit();
                                    payslipMaster.result += "Updated.";
                                }
                                catch (Exception ex) //error occurred
                                {
                                    trans.Rollback();
                                    payslipMaster.result += "Failed to Update.";
                                }
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        string _Payscale = "", _Key = "", _Ledger = "";
                        sSQL = "SELECT m_Name,m_Key,m_Ledger FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                        "where  m_Profile='" + profile + "' " +
                        "and m_id='" + row.m_id + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        _Payscale = MyGlobal.GetPureString(reader, "m_Name");
                                        _Key = MyGlobal.GetPureString(reader, "m_Key");
                                        _Ledger = MyGlobal.GetPureString(reader, "m_Ledger");
                                    }
                                }
                            }
                        }

                        if (_Payscale.Length > 0)
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "where m_Profile='" + profile + "' " +
                                "and m_Name='" + _Payscale + "'" +
                                "and m_Key='" + _Key + "'" +
                                "and m_Ledger='" + _Ledger + "';";

                            //"where  m_Profile='" + profile + "' and m_id='" + row.m_id + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("delete_earn"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Type='earn' and m_Ledger='" + row.ledger.name + "' and m_id='" + row.m_id + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    else if (mode.Equals("delete_deduct"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Type='deduct' and m_Ledger='" + row.ledger.name + "' and m_id='" + row.m_id + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    else if (mode.Equals("savectc"))
                    {
                        bool bCTCExists = false;
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' and m_Ledger='CTC' " +
                            "and m_Key='" + key + "' " +
                            "and m_Name='" + name + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bCTCExists = reader.HasRows;
                            }
                        }
                        if (bCTCExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                               "Set m_Amount='" + CTC + "' " +
                               "where  m_Profile='" + profile + "' and m_Ledger='CTC' " +
                               "and m_Name='" + name + "' and m_Key='" + key + "';";
                        }
                        else
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "(m_Profile,m_Name,m_Key,m_Ledger,m_Amount,m_Type) values ('" + profile + "','" + name + "','" + key + "','CTC','" + CTC + "','cr');";
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    else if (mode.Equals("down"))
                    {
                        string sqlUpdate = "";
                        int iCache = -1;
                        sSQL = "SELECT m_Ledger,m_Order,m_id FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and m_Type='" + crdr + "' " +
                            "and m_Ledger is not null " +
                            "order by m_Order asc";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        if (iCache > -1)
                                        {
                                            sqlUpdate += "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                                "Set m_Order='" + iCache + "' where m_id=" + reader.GetInt32(2) + ";";
                                            iCache = -1;
                                        }
                                        if (reader.GetString(0).Equals(row.ledger.name))
                                        {
                                            iCache = reader.GetInt16(1);
                                            sqlUpdate += "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                                "Set m_Order='" + (reader.GetInt16(1) + 1) + "' where m_id=" + reader.GetInt32(2) + ";";
                                        }
                                    }
                                }
                            }
                        }
                        if (sqlUpdate.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sqlUpdate, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("up"))
                    {
                        string sqlUpdate = "";
                        int iCache = -1;
                        sSQL = "SELECT m_Ledger,m_Order,m_id FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Name='" + name + "' " +
                            "and m_Key='" + key + "' " +
                            "and m_Type='" + crdr + "' " +
                            "and m_Ledger is not null " +
                            "order by m_Order desc";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        if (iCache > -1)
                                        {
                                            sqlUpdate += "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                                "Set m_Order='" + iCache + "' where m_id=" + reader.GetInt32(2) + ";";
                                            iCache = -1;
                                        }
                                        if (reader.GetString(0).Equals(row.ledger.name))
                                        {
                                            if (reader.GetInt16(1) > 0)
                                            {
                                                iCache = reader.GetInt16(1);
                                                sqlUpdate += "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                                    "Set m_Order='" + (reader.GetInt16(1) - 1) + "' where m_id=" + reader.GetInt32(2) + ";";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sqlUpdate.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sqlUpdate, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (mode.Equals("move"))
                    {
                        bool bCTCExists = false;
                        int iOrder = 9999;
                        // Does this ledger already exists in earn or deduct?
                        sSQL = "select m_Order from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Ledger='" + row.ledger.name + "' " +
                            "and m_Type='" + moveto + "' " +
                            "and m_Key='" + key + "' " +
                            "and m_Name='" + name + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bCTCExists = reader.HasRows;
                            }
                        }
                        if (bCTCExists)
                        {
                            payslipMaster.result = "Already Exists";
                        }
                        else
                        {
                            //----Get the order of the ledger getting moved...
                            string sInsert = "";
                            string xx = moveto.Equals("earn") ? "(m_Type='cr' or m_Type='cro')" : "(m_Type='dr' or m_Type='dro')";
                            sSQL = "select m_Order,m_BasedOn,m_Amount,m_PayMode from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                "where  m_Profile='" + profile + "' " +
                                "and m_Ledger='" + row.ledger.name + "' " +
                                "and " + xx + " " +
                                "and m_Key='" + key + "' " +
                                "and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) iOrder = reader.GetInt32(0);
                                            sInsert = "insert into " + MyGlobal.activeDB + ".tbl_payscale_master " +
    "(m_Profile,m_Name,m_Key,m_Ledger,m_Type,m_Order,m_BasedOn,m_Amount,m_PayMode) values " +
    "('" + profile + "','" + name + "','" + key + "','" + row.ledger.name + "'," +
    "'" + moveto + "','" + iOrder + "','" + (reader.IsDBNull(1) ? "" : reader.GetString(1)) + "'," +
    "'" + (reader.IsDBNull(2) ? "" : reader.GetString(2)) + "'," +
    "'" + (reader.IsDBNull(3) ? 0 : reader.GetInt16(3)) + "');";
                                        }
                                    }
                                }
                            }
                            //------------------Insert
                            if (sInsert.Length > 0)
                            {
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sInsert, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                }
                            }
                        }

                    }
                    else if (mode.Equals("changeledgertype"))
                    {
                        // Toggle ledger type
                        int iPayMode = 0;
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                        "where  m_Profile='" + profile + "'" +
                        "and m_Name = '" + name + "' " +
                        "and m_Key = '" + key + "' " +
                        "and m_Ledger='" + row.ledger.name + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        iPayMode =
                                            reader.IsDBNull(reader.GetOrdinal("m_PayMode")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_PayMode"));

                                    }
                                }
                            }
                        }

                        if (iPayMode == 2) iPayMode = 1; else iPayMode = 2;

                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payscale_master " +
                        "Set m_PayMode='" + iPayMode + "' " +
                        "where  m_Profile='" + profile + "' " +
                        "and m_Name = '" + name + "' " +
                        "and m_Key = '" + key + "' " +
                        "and m_Ledger='" + row.ledger.name + "';";
                        //"and m_id='" + row.m_id + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            payslipMaster.result += "Updated.";
                        }
                    }

                    //------------------------Get details
                    PaySlipLedger ledEmpty = new PaySlipLedger();
                    ledEmpty.name = "";
                    ledEmpty.paymode = 0;
                    payslipMaster.ledgers.Add(ledEmpty);
                    payslipMaster.allowdelete = 1;
                    sSQL = "select m_PayscaleName from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where  m_Profile='" + profile + "' and m_PayscaleName='" + name + "' " +
                        "and m_PayscaleKey='" + key + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    payslipMaster.allowdelete = reader.IsDBNull(0) ? 1 : 0;
                                }
                            }
                        }
                    }

                    //payslipMaster.ledgers.Add("CTC");
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                    "where  m_Profile='" + profile + "' and m_Name='" + name + "' " +
                    "and m_Key='" + key + "' " +
                    "order by m_Order";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int OrType = reader.GetOrdinal("m_Type");
                                    if (!reader.IsDBNull(OrType))
                                    {
                                        if (reader.GetString(OrType).Equals("cr"))
                                        {
                                            PayslipRow rowLoc = new PayslipRow();
                                            PaySlipLedger led = new PaySlipLedger();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PayMode")))
                                                    {
                                                        led.paymode = reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                    }
                                                    else
                                                    {
                                                        led.paymode = 0;
                                                    }
                                                    rowLoc.ledger = led;
                                                    payslipMaster.ledgers.Add(led);
                                                }
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_BasedOn"))) rowLoc.basedon = reader.GetString(reader.GetOrdinal("m_BasedOn"));
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) rowLoc.amount = reader.GetString(reader.GetOrdinal("m_Amount"));
                                            if (rowLoc.ledger.name.Equals("CTC"))
                                            {
                                                payslipMaster.CTC = rowLoc.amount;
                                            }
                                            else
                                            {
                                                payslipMaster.rows_rate_earn.Add(rowLoc);
                                            }
                                        }
                                        else if (reader.GetString(OrType).Equals("cro"))
                                        {
                                            PayslipRow rowLoc = new PayslipRow();
                                            PaySlipLedger led = new PaySlipLedger();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));

                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PayMode")))
                                                    {
                                                        led.paymode = reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                    }
                                                    else
                                                    {
                                                        led.paymode = 0;
                                                    }

                                                    rowLoc.ledger = led;
                                                    payslipMaster.ledgers.Add(led);
                                                }
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_BasedOn"))) rowLoc.basedon = reader["m_BasedOn"].ToString();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) rowLoc.amount = reader.GetString("m_Amount");

                                            if (rowLoc.ledger.name.Equals("CTC"))
                                            {
                                                payslipMaster.CTC = rowLoc.amount;
                                            }
                                            else
                                            {
                                                payslipMaster.rows_rate_earn_o.Add(rowLoc);
                                            }
                                        }
                                        else if (reader.GetString(OrType).Equals("dr"))
                                        {
                                            PayslipRow rowLoc = new PayslipRow();
                                            PaySlipLedger led = new PaySlipLedger();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PayMode")))
                                                    {
                                                        led.paymode = reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                    }
                                                    else
                                                    {
                                                        led.paymode = 0;
                                                    }
                                                    rowLoc.ledger = led;
                                                    payslipMaster.ledgers.Add(led);


                                                }
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_BasedOn"))) rowLoc.basedon = reader["m_BasedOn"].ToString();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) rowLoc.amount = reader.GetString("m_Amount");
                                            payslipMaster.rows_rate_deduct.Add(rowLoc);
                                        }
                                        else if (reader.GetString(OrType).Equals("dro"))
                                        {
                                            PayslipRow rowLoc = new PayslipRow();
                                            PaySlipLedger led = new PaySlipLedger();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PayMode")))
                                                    {
                                                        led.paymode = reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                    }
                                                    else
                                                    {
                                                        led.paymode = 0;
                                                    }
                                                    rowLoc.ledger = led;
                                                    payslipMaster.ledgers.Add(led);


                                                }
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_BasedOn"))) rowLoc.basedon = reader["m_BasedOn"].ToString();
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) rowLoc.amount = reader.GetString("m_Amount");
                                            payslipMaster.rows_rate_deduct_o.Add(rowLoc);
                                        }
                                        else if (reader.GetString(OrType).Equals("earn"))
                                        {
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                PayslipRow rowLoc = new PayslipRow();
                                                PaySlipLedger led = new PaySlipLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                    led.paymode = 0;
                                                    rowLoc.ledger = led;
                                                    //payslipMaster.ledgers.Add(led);
                                                }
                                                payslipMaster.rows_earn.Add(rowLoc);
                                                //payslipMaster.rows_earn.Add(reader["m_Ledger"].ToString());
                                            }
                                        }
                                        else if (reader.GetString(OrType).Equals("deduct"))
                                        {
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            {
                                                PayslipRow rowLoc = new PayslipRow();
                                                PaySlipLedger led = new PaySlipLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) rowLoc.m_id = reader.GetInt32("m_id");
                                                if (reader.GetString(reader.GetOrdinal("m_Ledger")).Length > 0)
                                                {
                                                    led.name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                    led.paymode = 0;
                                                    rowLoc.ledger = led;
                                                    //payslipMaster.ledgers.Add(led);
                                                }
                                                payslipMaster.rows_deduct.Add(rowLoc);
                                                //payslipMaster.rows_earn.Add(reader["m_Ledger"].ToString());
                                            }
                                            //if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                            //{
                                            //  payslipMaster.rows_deduct.Add(reader["m_Ledger"].ToString());
                                            //}
                                        }
                                    }
                                }
                                payslipMaster.status = true;
                            }
                            else
                            {
                                payslipMaster.result = "Sorry!!! No Data";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("payslipMaster-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("PayslipMaster-Exception->" + ex.Message);
            }
            return Json(payslipMaster, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------
        private string DoesThisPayscaleExistsInAnyLedger(string profile, string name, string key)
        {
            string sRet = "";
            string sSQL = "SELECT m_id,m_StaffID,m_Name,m_Year,m_Month FROM " + MyGlobal.activeDB + ".tbl_payslips_list " +
            "where m_Profile='" + profile + "' and m_PayscaleName='" + name + "' ";
            if (key.Length > 0) sSQL += "and m_PayscaleKey='" + key + "' ";
            sSQL += "limit 1";

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
                                    sRet = "This Payscale exists in ";
                                    if (!reader.IsDBNull(1)) sRet += "StaffID " + reader.GetString(1) + " ";
                                    if (!reader.IsDBNull(2)) sRet += "Name " + reader.GetString(2) + " ";
                                    if (!reader.IsDBNull(3)) sRet += "Year " + reader.GetString(3) + " ";
                                    if (!reader.IsDBNull(4)) sRet += "Month " + reader.GetString(4) + " ";
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("DoesThisPayscaleExistsInAnyLedger-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("DoesThisPayscaleExistsInAnyLedger-Exception->" + ex.Message);
            }
            return sRet;
        }
        //------------------------------------------------------
        [HttpPost]
        public ActionResult LoadPermissionData(string profile, string email, string staffid, string head,
            string selected, int status)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadPermissionData = new LoadPermissionData();
            loadPermissionData.status = false;
            loadPermissionData.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sKeyProfile = "", sKeyUser = "";
                    string sSQL = "SELECT m_MenuKey,m_Email FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where  m_Profile='" + profile + "' " +
                            "and (m_Email='" + email + "' or " +
                            "m_Email='" + profile + "');";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                    {
                                        if (reader.GetString(1).Equals(profile))
                                        {
                                            sKeyProfile = reader.GetString(0);
                                        }
                                        else
                                        {
                                            sKeyUser = reader.GetString(0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (profile.Equals(email)) sKeyUser = sKeyProfile;
                    //----------------------------------
                    char[] delimiterChars = { ',' };
                    string[] arData = sKeyProfile.Split(delimiterChars);
                    int iLen = arData.Length;
                    for (int i = 0; i < iLen; i++)
                    {
                        if (arData[i].Length == 4)
                        {
                            //w0-1
                            string key = arData[i].Substring(0, 3);
                            if (!sKeyUser.Contains(key))
                            {
                                sKeyUser += key + "0,";
                            }
                        }
                    }
                    loadPermissionData.sParam1 = sKeyUser;
                    //----------------------------------------------After 
                    if (selected.Length > 0)
                    {
                        if (selected.Equals("set_global"))
                        {
                            string sUpdateSQL = "";
                            sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_teams " +
                                "where m_Profile='" + profile + "';";
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
                                                sUpdateSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                                                "(m_Profile,m_StaffID,m_Team,m_Head,m_State) values " +
                                                "('" + profile + "','" + staffid + "','" + reader.GetString(0) + "'," +
                                                "'" + head + "','" + status + "') " +
                                                "ON DUPLICATE KEY UPDATE " +
                                                    "m_State = '" + status + "';";
                                            }
                                        }
                                    }
                                }
                            }
                            if (sUpdateSQL.Length > 0) using (MySqlCommand mySqlCommand = new MySqlCommand(sUpdateSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                        else
                        {
                            if (status > 0)
                            {
                                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                                "(m_Profile,m_StaffID,m_Team,m_Head,m_State) values " +
                                "('" + profile + "','" + staffid + "','" + selected + "'," +
                                "'" + head + "','" + status + "') " +
                                "ON DUPLICATE KEY UPDATE " +
                                    "m_State = '" + status + "';";
                            }
                            else
                            {
                                sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                    "and m_Team='" + selected + "' and m_Head='" + head + "'";
                            }
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    //--------------------------------------------------
                    sSQL = "SELECT teams.m_Name,permissions.m_State FROM " + MyGlobal.activeDB + ".tbl_misc_teams teams " +
                    "left join " + MyGlobal.activeDB + ".tbl_misc_teams_permissions permissions on permissions.m_Profile = teams.m_Profile and permissions.m_Team = teams.m_Name and permissions.m_StaffID = '" + staffid + "' and permissions.m_Head = '" + head + "' " +
                    "where teams.m_Profile='" + profile + "' order by teams.m_Name;";
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
                                            TeamPermissions obj = new TeamPermissions();
                                            obj.Name = reader.GetString(0);
                                            obj.state = reader.IsDBNull(1) ? 0 : reader.GetInt16(1);
                                            loadPermissionData.sarTeamsPermissions.Add(obj);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    loadPermissionData.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("StaffSearchResponse-MySqlException->" + ex.Message);
                loadPermissionData.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("StaffSearchResponse-Exception->" + ex.Message);
                loadPermissionData.result = ex.Message;
            }
            return Json(loadPermissionData, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------
        [HttpPost]
        public ActionResult GetApprovalDetails(string profile, string staffid, Int32 date)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string sSQL = "SELECT m_ActivityTime,m_WorkTime,m_ReasonHead,m_ReasonNote FROM " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_StaffID='" + staffid + "' " +
                            "and m_ActivityTime>=" + date + " " +
                            "and m_ActivityTime<" + (date + 86400) + " " +
                            "and m_Activity='approved'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                string html = "Manual Approval of " + staffid + " on " +
                                    MyGlobal.ToDateTimeFromEpoch(date).ToString("yyyy-MM-dd") + "<hr>";
                                html += "<table style='width:100%;padding:3px;font-size:small;'>";
                                while (reader.Read())
                                {
                                    html += "<tr>";
                                    html += "<td style='white-space:nowrap;'>" + (reader.IsDBNull(0) ? "&nbsp;" : MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(0) + 19800).ToString("yyyy-MM-dd HH:mm:ss")) + "</td>";
                                    html += "<td style='white-space:nowrap;'>" + (reader.IsDBNull(1) ? "&nbsp;" : MyGlobal.ToDateTimeFromEpoch(reader.GetInt32(1)).ToString("HH:mm:ss")) + "</td>";
                                    html += "<td style='white-space:nowrap;'>" + (reader.IsDBNull(2) ? "&nbsp;" : reader.GetString(2)) + "</td>";
                                    //html += "<td>" + (reader.IsDBNull(2) ? "&nbsp;" : reader.GetString(2)) + "</td>";
                                    html += "</tr>";
                                }
                                html += "</table>";
                                postResponse.result = html;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetApprovalDetails-MySqlException->" + ex.Message);
                postResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetApprovalDetails-Exception->" + ex.Message);
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //-----------------------------------------------------
        private string HoliSQL(string profile, string staffid, int year, int month, int date, string desc, int type)
        {
            return "insert into " + MyGlobal.activeDB + ".tbl_holidays " +
                        "(m_Profile,m_StaffID,m_Year,m_Month,m_DayH" + date + ",m_Type" + date + ") values " +
                        "('" + profile + "','" + staffid + "','" + year + "','" + month + "','" + desc + "','" + type + "')";
        }
        private string HoliSQL_Update(string profile, string staffid, int year, int month, int date, string desc, int type)
        {
            return "update " + MyGlobal.activeDB + ".tbl_holidays " +
            "Set m_DayH" + date + "='" + desc + "',m_Type" + date + "='" + type + "' " +
            "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' and m_Year='" + year + "' and m_Month='" + month + "';";
        }
        [HttpPost]
        public ActionResult updateholiday(string profile, string staffid, string mode,
    int year, int month, int date, string name, string type, string positions, string group,
    string location, string grpchennai1, string grpchennai2, string grpchennai3, string grpchennai4,
    string grpdelhi1, string grpdelhi2, string grpdelhi3, string grpdelhi4
    )
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var holidayResponse = new HolidayResponse();
            holidayResponse.status = false;
            holidayResponse.result = "";
            holidayResponse.Updated = false;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";

                    sSQL = "SELECT m_FName,m_Base FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where  m_Profile='" + profile + "' and m_StaffID='" + staffid + "' ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    holidayResponse.StaffName = reader.IsDBNull(reader.GetOrdinal("m_FName")) ? "" : reader.GetString(reader.GetOrdinal("m_FName"));
                                    holidayResponse.StaffBase = reader.IsDBNull(reader.GetOrdinal("m_Base")) ? "" : reader.GetString(reader.GetOrdinal("m_Base"));
                                }
                            }
                        }
                    }
                    sSQL = "SELECT m_DayH1 FROM " + MyGlobal.activeDB + ".tbl_holidays " +
                    "where  m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            holidayResponse.Updated = reader.HasRows;
                        }
                    }
                    if (mode.IndexOf("delete") > -1)
                    {
                        string delete = "delete from " + MyGlobal.activeDB + ".tbl_holidays " +
                        "where m_Profile='" + profile + "' and m_Year='" + year + "' and m_StaffID='" + staffid + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(delete, con)) mySqlCommand.ExecuteNonQuery();
                        holidayResponse.Updated = false;
                    }
                    else if (mode.IndexOf("fix") > -1)
                    {
                        string sErr = "";
                        if (holidayResponse.Updated)
                        {
                            sErr = "You have already updated your holidays";
                        }
                        if (holidayResponse.StaffBase.Equals("chennai", StringComparison.CurrentCultureIgnoreCase) ||
                            holidayResponse.StaffBase.Equals("salem", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (grpchennai1.Length == 0) sErr = "Select a Holiday from Group 1";
                            else if (grpchennai2.Length == 0) sErr = "Select a Holiday from Group 2";
                            else if (grpchennai3.Length == 0) sErr = "Select a Holiday from Group 3";
                            else if (grpchennai4.Length == 0) sErr = "Select a Holiday from Group 4";
                        }
                        else if (holidayResponse.StaffBase.Equals("delhi", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (grpdelhi1.Length == 0) sErr = "Select a Holiday from Group 1";
                            else if (grpdelhi2.Length == 0) sErr = "Select a Holiday from Group 2";
                            else if (grpdelhi3.Length == 0) sErr = "Select a Holiday from Group 3";
                            else if (grpdelhi4.Length == 0) sErr = "Select a Holiday from Group 4";
                        }
                        else
                        {
                            sErr = "Unknown base location";
                        }
                        holidayResponse.result = sErr;
                        if (sErr.Length == 0)
                        {
                            char[] delimiterChars = { ',' };
                            MySqlTransaction trans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = trans;
                            try
                            {
                                bool bSuccess = true;
                                //---------------------------Delete all
                                sSQL = "delete from " + MyGlobal.activeDB + ".tbl_holidays " +
                                "where  m_Profile='" + profile + "' " +
                                "and m_StaffID='" + staffid + "' " +
                                "and m_Year='" + year + "';";

                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //--------------------------
                                string monthRecord = ",";
                                myCommand.CommandText = HoliSQL(profile, staffid, year, 0, 26, "Republic Day", 0);
                                myCommand.ExecuteNonQuery();
                                monthRecord += "0,";

                                myCommand.CommandText = HoliSQL(profile, staffid, year, 7, 15, "Independence Day", 0);
                                myCommand.ExecuteNonQuery();
                                monthRecord += "7,";

                                myCommand.CommandText = HoliSQL(profile, staffid, year, 9, 2, "Gandhi Jayanth", 0);
                                myCommand.ExecuteNonQuery();
                                monthRecord += "9,";

                                myCommand.CommandText = HoliSQL(profile, staffid, year, 4, 1, "May Day", 0);
                                myCommand.ExecuteNonQuery();
                                monthRecord += "4,";

                                myCommand.CommandText = HoliSQL(profile, staffid, year, 11, 25, "Christmas", 0);
                                myCommand.ExecuteNonQuery();
                                monthRecord += "11,";

                                if (holidayResponse.StaffBase.Equals("chennai", StringComparison.CurrentCultureIgnoreCase) ||
                                    holidayResponse.StaffBase.Equals("salem", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    string[] ar = grpchennai1.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 1"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 1"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                    ar = grpchennai2.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 2"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 2"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        int test = myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                    ar = grpchennai3.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 3"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 3"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }

                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                    ar = grpchennai4.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 4"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 4"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }

                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }

                                }
                                else if (holidayResponse.StaffBase.Equals("delhi", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    string[] ar = grpdelhi1.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 1"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 1"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }

                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                    ar = grpdelhi2.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 2"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 2"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }

                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                    ar = grpdelhi3.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 3"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 3"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }

                                    ar = grpdelhi4.Split(delimiterChars);
                                    if (ar.Length != 3) { bSuccess = false; sErr = "Invalid Group 4"; }
                                    if (bSuccess)
                                    {
                                        int dt = MyGlobal.GetInt16(ar[1]);
                                        int mt = MyGlobal.GetInt16(ar[2]);
                                        if (dt == 0 || mt == 0) { bSuccess = false; sErr = "Invalid Date/Month in Group 4"; }
                                        else { mt -= 1; }
                                        if (monthRecord.IndexOf("," + mt + ",") > -1)
                                        {
                                            myCommand.CommandText = HoliSQL_Update(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        else
                                        {
                                            myCommand.CommandText = HoliSQL(profile, staffid, year, mt, dt, ar[0], 1);
                                        }
                                        myCommand.ExecuteNonQuery();
                                        monthRecord += mt + ",";
                                    }
                                }
                                if (sErr.Length == 0)
                                {
                                    trans.Commit();
                                    holidayResponse.Updated = true;
                                }
                                else
                                {
                                    trans.Rollback();
                                    holidayResponse.result = sErr;
                                }
                            }
                            catch (Exception ex) //error occurred
                            {
                                trans.Rollback();
                                holidayResponse.result = "Error " + ex.Message;
                                //Handel error
                            }
                        }
                    }
                    //-----------------Display Holidays
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_holidays " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_StaffID='" + staffid + "' and m_Year='" + year + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_month")))
                                    {
                                        int iMonth = reader.GetInt16(reader.GetOrdinal("m_month"));
                                        HolidayMonthRow mRow = new HolidayMonthRow();
                                        mRow.month = iMonth;
                                        int iOrdinalDay0 = reader.GetOrdinal("m_DayH1");
                                        for (int i = 0; i < 31; i++)
                                        {
                                            if (!reader.IsDBNull(iOrdinalDay0 + i * 3 + 0))
                                            {
                                                HolidayCell dRow = new HolidayCell();
                                                dRow.month = iMonth;
                                                dRow.date = i + 1;
                                                dRow.name = reader.IsDBNull(iOrdinalDay0 + i * 3 + 0) ? "" : reader.GetString(iOrdinalDay0 + i * 3 + 0);
                                                dRow.type = reader.IsDBNull(iOrdinalDay0 + i * 3 + 1) ? 0 : reader.GetInt16(iOrdinalDay0 + i * 3 + 1);
                                                holidayResponse.days.Add(dRow);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //--------------------------
                    sSQL = "SELECT m_Slot,m_Value,m_Desc,m_Base FROM " + MyGlobal.activeDB + ".tbl_holidays_year " +
                            "where  m_Profile='" + profile + "' " +
                            "and m_Year='" + year + "' and m_Base='" + holidayResponse.StaffBase + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (reader.GetString(3).Equals("Chennai") || reader.GetString(3).Equals("Salem"))
                                    {
                                        if (reader.GetInt16(0) == 0)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_0_Chennai.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 1)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_1_Chennai.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 2)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_2_Chennai.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 3)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_3_Chennai.Add(mRow);
                                        }
                                    }
                                    else if (reader.GetString(3).Equals("Delhi"))
                                    {
                                        if (reader.GetInt16(0) == 0)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_0_Delhi.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 1)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_1_Delhi.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 2)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_2_Delhi.Add(mRow);
                                        }
                                        else if (reader.GetInt16(0) == 3)
                                        {
                                            HolidayFixed mRow = new HolidayFixed();
                                            mRow.value = reader.GetString(1);
                                            mRow.desc = reader.GetString(2);
                                            holidayResponse.available_holidays_3_Delhi.Add(mRow);
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
                MyGlobal.Error("updateholiday-MySqlException->" + ex.Message);
                holidayResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("updateholiday-Exception->" + ex.Message);
                holidayResponse.result = ex.Message;
            }
            return Json(holidayResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------------

        [HttpPost]
        public ActionResult CreateEffectiveDateSpan(string mode, string profile, string staffid_loggedin, string email,
            string staffid, string payscale, string key, string dtFrom, string dtTo, string m_id)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var payscalesAssignedResponse = new PayscalesAssignedResponse();
            payscalesAssignedResponse.status = false;
            payscalesAssignedResponse.result = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("insert"))
                    {
                        bool bAllowInsert = true;
                        /* Good example for between
                        https://stackoverflow.com/questions/2545947/check-overlap-of-date-ranges-in-mysql
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                            " where (m_StartDate between " + dtFrom + " and " + dtTo + ") " +
                            "or (m_EndDate between " + dtFrom + " and " + dtTo + ") " +
                            "or (" + dtFrom + " between m_StartDate and m_EndDate) " +
                            "or (" + dtTo + " between m_StartDate and m_EndDate)";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale")))
                                        {
                                            postResponse.result = "Payscale " +
                                                reader.GetString(reader.GetOrdinal("m_Payscale")) +
                                                " date range comes under this time span.";
                                        }
                                        else
                                        {
                                            postResponse.result = "Unknown Payscale " +
                                            "date range comes under this time span.";
                                        }
                                    }
                                }
                                else
                                {
                                    bAllowInsert = true;
                                }
                            }
                        }
                        */
                        if (staffid_loggedin.Length == 0)
                        {
                            payscalesAssignedResponse.result = "Session expired. Please logout and login";
                            bAllowInsert = false;
                        }
                        if (bAllowInsert)
                        {


                            MySqlTransaction trans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = trans;
                            try
                            {
                               


                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                                "(m_Profile,m_StaffID,m_Payscale,m_Key,m_StartDate," +
                                "m_CreatedBy,m_CreatedTime) values " +  //,m_EndDate
                                "('" + profile + "','" + staffid + "','" + payscale + "','" + key + "'," +
                                "'" + dtFrom + "','" + staffid_loggedin + "',Now())";  //,'" + dtTo + "'
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                string mes = "Payscale effective date for [" + payscale + "] from [" + MyGlobal.ToDateTimeFromEpoch(MyGlobal.GetInt32(dtFrom)).ToString("dd-MM-yyyy") + "]";
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                                "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                                "('" + profile + "','" + staffid_loggedin + "','" + email + "','" + staffid + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + mes + "')";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //-------------------
                                trans.Commit();

                                payscalesAssignedResponse.status = true;
                                payscalesAssignedResponse.result = "Done";
                            }
                            catch (Exception ex) //error occurred
                            {
                                trans.Rollback();
                                payscalesAssignedResponse.result = "Error " + ex.Message;
                            }


                        }
                    }
                    else if (mode.Equals("delete"))
                    {




                        string mes = "Effective of payscale deleted. ";
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                        "where m_Profile='" + profile + "' and m_id='" + m_id + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale")) &&
                                            !reader.IsDBNull(reader.GetOrdinal("m_Key")) &&
                                            !reader.IsDBNull(reader.GetOrdinal("m_StaffID")) &&
                                            !reader.IsDBNull(reader.GetOrdinal("m_StartDate")))
                                        {
                                            payscale = reader.GetString(reader.GetOrdinal("m_Payscale"));
                                            key = reader.GetString(reader.GetOrdinal("m_Key"));
                                            dtFrom = reader.GetString(reader.GetOrdinal("m_StartDate"));
                                            mes += "Payscale " + payscale + ", ";
                                            mes += "Key " + key + ", ";
                                            mes += "StaffID " + reader.GetString(reader.GetOrdinal("m_StaffID")) + ", ";
                                            mes += "StartDate " + dtFrom + ", ";
                                        }
                                    }
                                }
                            }
                        }
                        //-------------------------------Any payslip created?
                        string mess = "";
                        sSQL = "select m_StaffID,m_Year,m_Month,m_CreatedBy from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "where m_Profile='" + profile + "' " +
                            "and m_PayscaleName='" + payscale + "' " +
                            "and m_PayscaleKey='" + key + "' " +
                            "and m_PayscaleStartDate='" + dtFrom + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        mess = "You can't delete. You already have a payslip " +
                                            "created for StaffID " + MyGlobal.GetPureString(reader, "m_StaffID") +
                                            " for the month of " + MyGlobal.GetPureString(reader, "m_Month") +
                                            "/" + MyGlobal.GetPureString(reader, "m_Year") +
                                            " by " + MyGlobal.GetPureString(reader, "m_CreatedBy");
                                        payscalesAssignedResponse.result = mess;
                                    }
                                }
                            }
                        }
                        if (mess.Length == 0)
                        {
                            MySqlTransaction trans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = trans;
                            try
                            {
                                //-----------------------------------------------------
                                sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                                    "where m_Profile='" + profile + "' and m_id='" + m_id + "'";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();

                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_masterlog " +
                                "(m_Profile,m_StaffID,m_Email,m_StaffID_Concern,m_Time,m_IP,m_ConcernTable,m_Changes) values " +
                                "('" + profile + "','" + staffid_loggedin + "','" + email + "','" + staffid + "',Now(),'" + MyGlobal.GetIPAddress() + "','tbl_staffs','" + mes + "')";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //-------------------
                                trans.Commit();

                                payscalesAssignedResponse.status = true;
                                payscalesAssignedResponse.result = "Done";
                            }
                            catch (Exception ex) //error occurred
                            {
                                trans.Rollback();
                                payscalesAssignedResponse.result = "Error " + ex.Message;
                            }
                        }
                    }
                    sSQL = "select *,lst.m_PayscaleName as PayscaleName from " + MyGlobal.activeDB + ".tbl_payscale_effective eff " +
                        "left join (select * from " + MyGlobal.activeDB + ".tbl_payslips_list where m_PayscaleName is not null group by m_PayscaleName) lst on lst.m_Profile=eff.m_Profile and lst.m_StaffID=eff.m_StaffID and lst.m_PayscaleName=eff.m_Payscale and lst.m_PayscaleKey=eff.m_Key " +
                        "where eff.m_Profile='" + profile + "' and eff.m_StaffID='" + staffid + "' " +
                        "order by eff.m_StartDate desc;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    PayscalesAssignedRow row = new PayscalesAssignedRow();
                                    row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    row.name = reader.GetString(reader.GetOrdinal("m_Payscale"));
                                    row.effectivedate = reader.GetInt32(reader.GetOrdinal("m_key"));
                                    row.startdate = reader.GetInt32(reader.GetOrdinal("m_StartDate"));
                                    row.ctc = GetCTC(profile, row.name, row.effectivedate);
                                    row.createdby = reader.IsDBNull(reader.GetOrdinal("m_CreatedBy")) ? "" : reader.GetString(reader.GetOrdinal("m_CreatedBy"));
                                    row.createdtime = reader.IsDBNull(reader.GetOrdinal("m_CreatedTime")) ? "" : reader.GetString(reader.GetOrdinal("m_CreatedTime"));
                                    row.allowdelete = reader.IsDBNull(reader.GetOrdinal("PayscaleName")) ? 1 : 0;
                                    payscalesAssignedResponse.rows.Add(row);
                                }
                                payscalesAssignedResponse.status = true;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("CreateEffectiveDateSpan-MySqlException->" + ex.Message);
                payscalesAssignedResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("CreateEffectiveDateSpan-Exception->" + ex.Message);
                payscalesAssignedResponse.result = ex.Message;
            }
            return Json(payscalesAssignedResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        private string GetCTC(string profile, string payscale, int effDate)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();


                    string sSQL = "select m_Amount from " + MyGlobal.activeDB + ".tbl_payscale_master eff " +
                        "where m_Profile='" + profile + "' and m_Name='" + payscale + "' " +
                        "and m_Key='" + effDate + "' and m_Ledger='CTC' and m_Type='cr' limit 1";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) return reader.GetString(0);
                                }
                            }
                        }
                    }

                }
            }
            catch (MySqlException ex)
            {

            }
            catch (Exception ex)
            {

            }
            return "";
        }

        [HttpPost]
        public ActionResult MyProductionUpdate(string profile, string staffid,
            string year, string month, string day, string m_id, string target, string achived,
            string qasamples, string qaerror, string qascore)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new PostResponse();
            response.status = false;
            response.result = "";
            response.sParam1 = "";
            int iAchived = MyGlobal.GetInt16(achived);
            int iMonth = MyGlobal.GetInt16(month) - 1;
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sUpdate = "";
                    if (MyGlobal.GetInt16(target) > 0) { if (sUpdate.Length > 0) sUpdate += ","; sUpdate += "m_Target='" + target + "'"; }
                    if (MyGlobal.GetInt16(achived) > 0) { if (sUpdate.Length > 0) sUpdate += ","; sUpdate += "m_Achived='" + achived + "'"; }
                    if (MyGlobal.GetInt16(qasamples) > 0) { if (sUpdate.Length > 0) sUpdate += ","; sUpdate += "m_QASamples='" + qasamples + "'"; }
                    if (MyGlobal.GetInt16(qaerror) > 0) { if (sUpdate.Length > 0) sUpdate += ","; sUpdate += "m_QAError='" + qaerror + "'"; }
                    if (MyGlobal.GetInt16(qascore) > 0) { if (sUpdate.Length > 0) sUpdate += ","; sUpdate += "m_QAScore='" + qascore + "'"; }
                    //-----------------------------------------------------
                    if (sUpdate.Length > 0)
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_production " +
                        "Set " + sUpdate +
                        " where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' and m_Month='" + iMonth + "' and m_Day='" + day + "' " +
                        "and m_id='" + m_id + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    }
                    response.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MyProductionUpdate-MySqlException->" + ex.Message);
                response.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("MyProductionUpdate-Exception->" + ex.Message);
                response.result = ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetFreeStaffID(string profile)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new PostResponse();
            response.status = false;
            response.result = "";
            response.sParam1 = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------Get alphabetic part
                    string sAlphaPart = "";
                    int iNumExpectedLength = 1;
                    /*
                    string sSQL = "select m_StaffID from " + MyGlobal.activeDB + ".tbl_Staffs " +
                        "where m_Profile='" + profile + "' and m_Email='" + profile + "';";
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
                                        string staffid = reader.GetString(0);
                                        string sNum = "", sTxt = "";
                                        int iLen = staffid.Length;
                                        for (int i = 0; i < iLen; i++)
                                        {
                                            if (staffid[i] >= 0x30 && staffid[i] <= 0x39)
                                            {
                                                sNum += staffid[i];
                                            }
                                            else
                                            {
                                                sTxt += staffid[i];
                                            }
                                        }
                                        sAlphaPart = sTxt;
                                        iNumExpectedLength = sNum.Length;
                                    }
                                }
                            }
                        }
                    }
                    */
                    string sSQL = "select m_StaffID_Prefix,m_StaffID_NumericDigits " +
                        "from " + MyGlobal.activeDB + ".tbl_profile_info " +
                        "where m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) sAlphaPart = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) iNumExpectedLength = reader.GetInt16(1);
                                }
                            }
                        }
                    }
                    //----------Get last no---------------------
                    int iAlphaPart = sAlphaPart.Length;
                    if (iAlphaPart > 0)
                    {
                        sSQL = "select SUBSTR(m_StaffID," + (iAlphaPart + 1) + ") as staff from " + MyGlobal.activeDB + ".tbl_Staffs " +
                            "where m_Profile='" + profile + "' " +
                            "and SUBSTR(m_StaffID,1," + iAlphaPart + ")='" + sAlphaPart + "' " +
                            "order by staff desc limit 1;";
                    }
                    else
                    {
                        sSQL = "select m_StaffID as staff from " + MyGlobal.activeDB + ".tbl_Staffs " +
                        "where m_Profile='" + profile + "' and (m_StaffID REGEXP '^[0-9]+$') " +
                        "order by staff desc limit 1;";
                    }
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

                                        string sNum = reader.GetString(0);
                                        Int32 iValue = MyGlobal.GetInt32(sNum) + 1;
                                        string sNumNew = MyGlobal.Right(("0000000" + iValue), iNumExpectedLength);
                                        response.sParam1 = sAlphaPart + sNumNew;
                                        response.status = true;
                                    }
                                }
                            }
                        }
                    }
                    if (response.sParam1.Length == 0)
                    {
                        if (iAlphaPart > 0)
                        {
                            if (iNumExpectedLength < 1) iNumExpectedLength = 1;
                            string sNumNew = MyGlobal.Right(("0000000" + "1"), iNumExpectedLength);
                            response.sParam1 = sAlphaPart + sNumNew;
                        }
                        else
                        {
                            response.sParam1 = "10000";
                        }
                        response.status = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetFreeStaffID-MySqlException->" + ex.Message);
                response.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetFreeStaffID-Exception->" + ex.Message);
                response.result = ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult ToggleAttendanceState(string profile, string staffidloggedin, string staffid,
    string year, string month, string day, string roster, string shift,
    string staffname, string marker, string mode, string rosterList)
        {

            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new PostResponse();
            response.status = false;
            response.result = "";
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month);
            int iDay = MyGlobal.GetInt16(day);
            if (iYear < 2019 || iMonth < 1 || iDay < 1)
            {
                response.result = "Invalid Date";
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------------------Get shift details at the specified time
                    string sRosterMarker = "";
                    long lShiftStartSecs = 0, lShiftEndSecs = 0;
                    long lWorkhours = 0;
                    //, lActualStartUnix = 0, lActualEndUnix = 0;
                    long unixTimeDateStart = (Int32)((new DateTime(iYear, iMonth, iDay)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;


                    if (!GetShiftOn(con, profile,
                        staffid, iYear, iMonth, iDay,
                        roster, shift,
                        out sRosterMarker,
                        out lShiftStartSecs, out lShiftEndSecs))
                    {
                        response.result = "Failed. Check Roster & Leave status";
                        //return Json(response, JsonRequestBehavior.AllowGet);
                        return LoadRosters_Classic(profile, roster, shift, mode,
                            "", "", "", year, month,
                            staffname, staffid, "", response.result, rosterList);
                    }


                    if (lShiftStartSecs > 0 || lShiftEndSecs > 0)
                    {
                        //lWorkhours = lShiftEndSecs - lShiftStartSecs;
                        //if (lWorkhours < 0) lWorkhours = 0;
                        lWorkhours = 3600 * 8;
                    }
                    //-------------------------------------------------
                    MySqlTransaction trans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = trans;
                    try
                    {
                        if (mode.Equals("toggle"))
                        {

                            //-------------------------------------------------Got lWorkhours & bRecordExists
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                  "where m_Profile='" + profile + "' and m_HardwareID='Manual' " +
                                  "and m_Session='" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "' " +
                                  "and m_ActivityTime>=" + (unixTimeDateStart + lShiftStartSecs - 19800) + " " +
                                  "and m_ActivityTime< " + (unixTimeDateStart + lShiftEndSecs - 19800);
                            myCommand.CommandText = sSQL;
                            int iDelCnt = myCommand.ExecuteNonQuery();


                            if (iDelCnt > 0)
                            {

                            }
                            else
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime," +
                                    "m_ReasonHead,m_ReasonNote,m_Session) " +
                                    "values " +
                                    "('" + profile + "','" + staffid + "','Manual','approved'," +
                                    "'" + (unixTimeDateStart + lShiftStartSecs - 19800) + "','" + lWorkhours + "'," +
                                    "'Attendance Toggle by " + staffidloggedin + "'," +
                                    "''," +
                                    "'" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "');";

                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime," +
                                    "m_ReasonHead,m_ReasonNote,m_Session) " +
                                    "values " +
                                    "('" + profile + "','" + staffid + "','Manual','update'," +
                                    "'" + (unixTimeDateStart + lShiftEndSecs - 19800 - 1) + "','" + 1 + "'," +
                                    "'Attendance Toggle by " + staffidloggedin + "'," +
                                    "''," +
                                    "'" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftEndSecs - 19800 - 1)) + "');";



                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                            }


                            //-------------------------------------------------
                            long lStartUnix = 0, lEndUnix = 0, lWorkHoursLocal = 0;
                            if (iDelCnt == 0)
                            {
                                lStartUnix = unixTimeDateStart + lShiftStartSecs;
                                lEndUnix = unixTimeDateStart + lShiftEndSecs;
                                lWorkHoursLocal = lWorkhours;
                            }
                            bool bAttendanceRecordExists = false;
                            sSQL = "select m_ActualStart,m_ActualEnd from " + MyGlobal.activeDB + ".tbl_attendance " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "' " +
                            "and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                            "and m_Year='" + iYear + "' and m_Month='" + (iMonth - 1) + "' " +
                            "and m_Date='" + unixTimeDateStart + "' limit 1";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    bAttendanceRecordExists = reader.HasRows;
                                }
                            }
                            if (!bAttendanceRecordExists)
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_attendance " +
                                    "(m_StaffID,m_Year,m_Month,m_Date," +
                                    "m_RosterName,m_ShiftName," +
                                    "m_ShiftStart,m_ShiftEnd, " +
                                    "m_ActualStart," +
                                    "m_ActualEnd," +
                                    "lWorkhours,m_Profile," +
                                    "m_MarkRoster,m_MarkLeave,m_RosterOptions,m_AsOn,m_Mode)  " +
                                    "values  " +
                                    "('" + staffid + "','" + iYear + "','" + (iMonth - 1) + "','" + unixTimeDateStart + "'," +
                                    "'" + roster + "','" + shift + "'," +
                                    "'" + (unixTimeDateStart + lShiftStartSecs) + "','" + (unixTimeDateStart + lShiftEndSecs) + "'," +
                                    "'" + (lStartUnix) + "'," +
                                    "'" + (lEndUnix) + "'," +
                                    "'" + lWorkHoursLocal + "','" + profile + "'," +
                                    "'" + sRosterMarker + "','" + "" + "','" + "" + "',UNIX_TIMESTAMP(),'1');";
                            }
                            else
                            {

                                sSQL = "update " + MyGlobal.activeDB + ".tbl_attendance Set " +
                                    "m_ActualStart='" + (lStartUnix) + "'," +
                                    "m_ActualEnd='" + (lEndUnix) + "'," +
                                    "lWorkhours='" + lWorkHoursLocal + "'," +
                                    "m_AsOn=UNIX_TIMESTAMP(),m_Mode='1' " +
                                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                    "and m_Year='" + iYear + "' and m_Month='" + (iMonth - 1) + "' " +
                                    "and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                                    "and m_Date='" + unixTimeDateStart + "';";
                            }

                            string retStatus = "";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            trans.Commit();


                            //-----------------------------------------
                            response.result = "In time is updated. Reload to view in calendar.";
                            return LoadRosters_Classic(profile, roster, shift, mode,
                                "", "", "", year, month,
                                staffname, staffid, "", "", rosterList);
                            /*
                            return ThisDay(profile, roster,
                                        shift, staffname, staffid,
                                        year, month, day,
                                        marker, "", retStatus);
                                        */
                        }

                    }
                    catch (Exception ex) //error occurred
                    {
                        trans.Rollback();
                        response.result = "Error " + ex.Message;
                        MyGlobal.Error("Error " + ex.Message);
                    }
                    response.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ManualAttendanceUpdate-MySqlException->" + ex.Message);
                response.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ManualAttendanceUpdate-Exception->" + ex.Message);
                response.result = ex.Message;
            }
            return LoadRosters_Classic(profile, roster, shift, mode,
                "", "", "", year, month,
                staffname, staffid, "", "", rosterList);

            //return Json(response, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManualAttendanceUpdate(string profile, string staffid,
    string year, string month, string day, string timein, string timeout, string roster, string shift,
    string staffname, string marker, string mode)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new PostResponse();
            response.status = false;
            response.result = "";
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month);
            int iDay = MyGlobal.GetInt16(day);
            if (iYear < 2019 || iMonth < 1 || iDay < 1)
            {
                response.result = "Invalid Date";
                return Json(response, JsonRequestBehavior.AllowGet);
            }


            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //----------------------------Get shift details at the specified time
                    string sRosterMarker = "";
                    long lShiftStartSecs = 0, lShiftEndSecs = 0;
                    long unixTimeDateStart = (Int32)((new DateTime(iYear, iMonth, iDay)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    if (!GetShiftOn(con, profile,
                        staffid, iYear, iMonth, iDay,
                        roster, shift,
                        out sRosterMarker,
                        out lShiftStartSecs, out lShiftEndSecs))
                    {
                        response.result = "Unable to get the shift details. Check Roster & Leave status";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                    long lTimeInSecs = 0, lTimeOutSecs = 0;
                    bool bTimeIn = false, bTimeOut = false;
                    //---------------In time
                    if (timein.Length == 0 || timein.Equals("0"))
                    {
                    }
                    else
                    {
                        lTimeInSecs = MyGlobal.UnixFromHHMM(timein);
                        if (lTimeInSecs == -1)
                        {
                            response.result = "Invalid IN Time [" + timein + "] (Format 'HH:MM')";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                        if ((lTimeInSecs >= lShiftStartSecs) && (lTimeInSecs <= lShiftEndSecs))
                        {
                            response.result = "IN within shift > " + roster + "," + shift;
                        }
                        else
                        {
                            lTimeInSecs += 86400;

                            if ((lTimeInSecs > lShiftEndSecs))
                            {
                                response.result = "IN Time not within shift";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                        bTimeIn = true;
                    }
                    //---------------Out time
                    if (timeout.Length == 0 || timeout.Equals("0"))
                    {
                    }
                    else
                    {
                        lTimeOutSecs = MyGlobal.UnixFromHHMM(timeout);
                        if (lTimeOutSecs == -1)
                        {
                            response.result = "Invalid Out Time [" + timeout + "] (Format 'HH:MM')";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        if ((lShiftEndSecs - 86400) > 0) // Mid night cross over
                        {
                            if (lTimeOutSecs < lTimeInSecs)
                            {
                                lTimeOutSecs += 86400;
                            }
                        }
                        /*
                        if ((lTimeOutSecs >= lShiftStartSecs) && (lTimeOutSecs <= lShiftEndSecs))
                        {
                            response.result = "OUT within shift > " + roster + "," + shift;
                        }
                        else
                        {
                            lTimeOutSecs += 86400;

                            if ((lTimeOutSecs > lShiftEndSecs))
                            {
                                response.result = "OUT Time not within shift";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                        */
                        bTimeOut = true;
                    }
                    //-------------------------------------------------
                    MySqlTransaction trans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = trans;
                    try
                    {
                        if (mode.Equals("set"))
                        {
                            bool bRecordExists = false;
                            long lWorkhours = 0;

                            sSQL = "select m_ActualStart,m_ActualEnd from " + MyGlobal.activeDB + ".tbl_attendance " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "' " +
                            "and m_RosterName='" + roster + "' and m_ShiftName='" + shift + "' " +
                            "and m_Year='" + iYear + "' and m_Month='" + (iMonth - 1) + "' " +
                            "and m_Date='" + unixTimeDateStart + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bRecordExists = true;
                                        /*
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) lActualStartUnix = reader.GetInt32(0);
                                            if (!reader.IsDBNull(1)) lActualEndUnix = reader.GetInt32(1);
                                        }
                                        */
                                    }
                                }
                            }
                            if (lTimeInSecs > 0 && lTimeOutSecs > 0)
                            {
                                lWorkhours = (unixTimeDateStart + lTimeOutSecs) - (unixTimeDateStart + lTimeInSecs);
                            }

                            //-------------------------------------------------Got lWorkhours & bRecordExists
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "where m_Profile='" + profile + "' and m_HardwareID='Manual' and m_Activity='open' " +
                                "and (m_Session='" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "' or " +
                                "m_Session='" + (staffid + "_maual_1_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "')";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            if (bTimeIn)
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime,m_Session) " +
                                    "values " +
                                    "('" + profile + "','" + staffid + "','Manual','open'," +
                                    "'" + (unixTimeDateStart + lTimeInSecs - 19800) + "','" + "0" + "'," +
                                    "'" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime,m_Session) " +
    "values " +
    "('" + profile + "','" + staffid + "','Manual','update'," +
    "'" + (unixTimeDateStart + lTimeInSecs - 19800 + 1) + "','" + 14400 + "'," +
    "'" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                            }
                            if (bTimeOut)
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime,m_Session) " +
                                    "values " +
                                    "('" + profile + "','" + staffid + "','Manual','open'," +
                                    "'" + (unixTimeDateStart + lTimeOutSecs - 19800 - 2) + "','0'," +
                                    "'" + (staffid + "_maual_1_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "');";
                                sSQL += "insert into " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
    "(m_Profile,m_StaffID,m_HardwareID,m_Activity,m_ActivityTime,m_WorkTime,m_Session) " +
    "values " +
    "('" + profile + "','" + staffid + "','Manual','update'," +
    "'" + (unixTimeDateStart + lTimeOutSecs - 19800 - 1) + "','" + 14400 + "'," +
    "'" + (staffid + "_maual_1_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                            }
                            //-------------------------------------------------
                            if (!bRecordExists)
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_attendance " +
                                    "(m_StaffID,m_Year,m_Month,m_Date," +
                                    "m_RosterName,m_ShiftName," +
                                    "m_ShiftStart,m_ShiftEnd, " +
                                    "m_ActualStart," +
                                    "m_ActualEnd," +
                                    "lWorkhours,m_Profile," +
                                    "m_MarkRoster,m_MarkLeave,m_RosterOptions,m_AsOn,m_Mode)  " +
                                    "values  " +
                                    "('" + staffid + "','" + iYear + "','" + (iMonth - 1) + "','" + unixTimeDateStart + "'," +
                                    "'" + roster + "','" + shift + "'," +
                                    "'" + (unixTimeDateStart + lShiftStartSecs) + "','" + (unixTimeDateStart + lShiftEndSecs) + "'," +
                                    "'" + (bTimeIn ? (unixTimeDateStart + lTimeInSecs) : 0) + "'," +
                                    "'" + (bTimeOut ? (unixTimeDateStart + lTimeOutSecs) : 0) + "'," +
                                    "'" + lWorkhours + "','" + profile + "'," +
                                    "'" + sRosterMarker + "','" + "" + "','" + "" + "',UNIX_TIMESTAMP(),'1');";
                            }
                            else
                            {

                                sSQL = "update " + MyGlobal.activeDB + ".tbl_attendance Set " +
                                    "m_ActualStart='" + (bTimeIn ? (unixTimeDateStart + lTimeInSecs) : 0) + "'," +
                                    "m_ActualEnd='" + (bTimeOut ? (unixTimeDateStart + lTimeOutSecs) : 0) + "'," +
                                    "lWorkhours='" + lWorkhours + "'," +
                                    "m_AsOn=UNIX_TIMESTAMP(),m_Mode='1' " +
                                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                    "and m_Year='" + iYear + "' and m_Month='" + (iMonth - 1) + "' " +
                                    "and m_Date='" + unixTimeDateStart + "';";
                            }

                            string retStatus = "";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            trans.Commit();
                            //-----------------------------------------
                            response.result = "In time is updated. Reload to view in calendar.";
                            return ThisDay(profile, roster,
                                        shift, staffname, staffid,
                                        year, month, day,
                                        marker, "", retStatus, lShiftStartSecs);
                        }

                    }
                    catch (Exception ex) //error occurred
                    {
                        trans.Rollback();
                        response.result = "Error " + ex.Message;
                    }
                    response.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ManualAttendanceUpdate-MySqlException->" + ex.Message);
                response.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ManualAttendanceUpdate-Exception->" + ex.Message);
                response.result = ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //-----------------------Classic View
        [HttpPost]
        public ActionResult LoadRosters_Classic(string profile, string roster, string shift, string mode,
            string pop_input, string pop_starttime, string pop_endtime,
            string year, string month,
            string staffname, string staffid, string optioncount, string forcedResult,
            string rosterList)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var classicRosterResponse = new ClassicRosterResponse();
            classicRosterResponse.status = true;
            classicRosterResponse.result = "";

            try
            {
                //profile = "support@SharewareDreams.com";
                //year = "2019";
                //roster = "Century Holter (CH)";
                //month = "6";
                int iMonth = MyGlobal.GetInt16(month);
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------------
                    if (MyGlobal.GetInt16(optioncount) == 0)
                    {
                        sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                            "where m_Profile = '" + profile + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        classicRosterResponse.sarRosterOptions.Add(MyGlobal.GetPureString(reader, "m_Name"));
                                    }
                                }
                            }
                        }

                    }
                    //-------------------------------------------------------
                    sSQL = "SELECT m_ShiftName,m_RosterName FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                        "where m_Profile='" + profile + "' and m_Year = '" + year + "' and m_Month = '" + (iMonth - 1) + "' ";
                    if (!rosterList.Equals("All")) sSQL += "and m_RosterName = '" + roster + "' ";
                    sSQL += "and m_ShiftName is not null " +
                        "group by m_RosterName,m_ShiftName " +
                        "order by m_RosterName,m_ShiftStartTime";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    GetThisShiftStaffsIntoTempTable(profile, year, month,
                                        MyGlobal.GetPureString(reader, "m_RosterName"),
                                        MyGlobal.GetPureString(reader, "m_ShiftName"),
                                        classicRosterResponse);

                                }
                                classicRosterResponse.status = true;
                            }
                        }
                    }
                    //________________________Get Date and Day on the header
                    int iLastDayOfThisMonth = DateTime.DaysInMonth(MyGlobal.GetInt16(year), iMonth);
                    for (int i = 1; i <= iLastDayOfThisMonth; i += 1)
                    {
                        String sMonth = (iMonth).ToString();
                        if ((iMonth) < 10) sMonth = "0" + (iMonth);
                        String sDate = i.ToString();
                        if (i < 10) sDate = "0" + i;
                        DateTime dt;
                        if (DateTime.TryParseExact(year + "-" + sMonth + "-" + sDate,
                                               "yyyy-MM-dd",
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.None,
                                               out dt))
                        {

                            classicRosterResponse.sarDayHeaders.Add(
                                dt.Equals(DateTime.Today) ? ("1" + dt.ToString("ddd") + " / " + i) : ("0" + dt.ToString("ddd") + " / " + i)
                                );
                        }
                    }
                    //________________________Get Date and Day on the header END
                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("LoadRosters_Classic>>" + ex.Message);
                classicRosterResponse.status = false;
                classicRosterResponse.result = ex.Message;
            }
            if (forcedResult != null) if (forcedResult.Length > 0) classicRosterResponse.result = forcedResult;
            return Json(classicRosterResponse, JsonRequestBehavior.AllowGet);
            //-----------------------Clasic View END
        }
        private void GetThisShiftStaffsIntoTempTable(string profile, string year, string month,
            string roster, string shiftname, ClassicRosterResponse classicRosterResponse)
        {
            int iMonth = MyGlobal.GetInt16(month);
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                RosterClassicRow row = new RosterClassicRow();
                RosterClassicCell[] cell = new RosterClassicCell[32];
                for (int day = 1; day <= 31; day++)
                {
                    cell[day] = new RosterClassicCell();
                }
                //from_unixtime(m_Date, '%d')
                string atten_sql = "SELECT m_StaffID,m_RosterName,m_ShiftName " +
                    ",sum(case when from_unixtime(m_Date, '%d')=1 then lWorkhours else 0 end) 'att_1'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=2 then lWorkhours else 0 end) 'att_2'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=3 then lWorkhours else 0 end) 'att_3'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=4 then lWorkhours else 0 end) 'att_4'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=5 then lWorkhours else 0 end) 'att_5'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=6 then lWorkhours else 0 end) 'att_6'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=7 then lWorkhours else 0 end) 'att_7'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=8 then lWorkhours else 0 end) 'att_8'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=9 then lWorkhours else 0 end) 'att_9'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=10 then lWorkhours else 0 end) 'att_10'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=11 then lWorkhours else 0 end) 'att_11'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=12 then lWorkhours else 0 end) 'att_12'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=13 then lWorkhours else 0 end) 'att_13'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=14 then lWorkhours else 0 end) 'att_14'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=15 then lWorkhours else 0 end) 'att_15'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=16 then lWorkhours else 0 end) 'att_16'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=17 then lWorkhours else 0 end) 'att_17'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=18 then lWorkhours else 0 end) 'att_18'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=19 then lWorkhours else 0 end) 'att_19'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=20 then lWorkhours else 0 end) 'att_20'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=21 then lWorkhours else 0 end) 'att_21'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=22 then lWorkhours else 0 end) 'att_22'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=23 then lWorkhours else 0 end) 'att_23'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=24 then lWorkhours else 0 end) 'att_24'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=25 then lWorkhours else 0 end) 'att_25'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=26 then lWorkhours else 0 end) 'att_26'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=27 then lWorkhours else 0 end) 'att_27'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=28 then lWorkhours else 0 end) 'att_28'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=29 then lWorkhours else 0 end) 'att_29'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=30 then lWorkhours else 0 end) 'att_30'" +
                    ",sum(case when from_unixtime(m_Date, '%d')=31 then lWorkhours else 0 end) 'att_31'" +
                    " FROM " + MyGlobal.activeDB + ".tbl_attendance " +
                    "where m_Profile='" + profile + "' and m_Year = '" + year + "' and m_Month = '" + (iMonth - 1) + "' " +
                    "and m_RosterName = '" + roster + "' " +
                    "group by m_StaffID,m_ShiftName";

                string sSQL = "SELECT rosters.*,leavs.*,attend.* FROM " + MyGlobal.activeDB + ".tbl_rosters rosters " +
                    "left join " + MyGlobal.activeDB + ".tbl_leaves leavs on leavs.m_Year = rosters.m_Year and leavs.m_Month = rosters.m_Month and rosters.m_Profile=leavs.m_Profile and rosters.m_StaffID = leavs.m_StaffID " +
                    "left join (" + atten_sql + ") attend on attend.m_RosterName=rosters.m_RosterName and attend.m_ShiftName=rosters.m_ShiftName " +
                    "and attend.m_StaffID=rosters.m_StaffID " +
                    "where rosters.m_Profile='" + profile + "' and rosters.m_Year = '" + year + "' and rosters.m_Month = '" + (iMonth - 1) + "' " +
                    "and rosters.m_RosterName = '" + roster + "' and rosters.m_ShiftName = '" + shiftname + "' " +
                    "order by rosters.m_StaffName;";
                //and rosters.m_StaffID is not null 
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {

                            while (reader.Read())
                            {
                                if (row.RosterName.Length == 0) row.RosterName = roster;
                                if (row.ShiftName.Length == 0) row.ShiftName = MyGlobal.GetPureString(reader, "m_ShiftName");
                                if (row.ShiftStart == 0) row.ShiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                if (row.ShiftEnd == 0) row.ShiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");

                                string name = MyGlobal.GetPureString(reader, "m_StaffName");
                                string staffid = MyGlobal.GetPureString(reader, "m_StaffID");

                                for (int day = 1; day <= 31; day++)
                                {
                                    string RosterOption = MyGlobal.GetPureString(reader, "m_Day" + day);
                                    string Leave = MyGlobal.GetPureString(reader, "m_DayL" + day);
                                    Int16 LeaveStatus = MyGlobal.GetPureInt16(reader, "m_Status" + day);
                                    Int32 lWorkhours = MyGlobal.GetPureInt32(reader, "att_" + day);

                                    if (RosterOption.Length > 0 || Leave.Length > 0)
                                    {
                                        RosterClassicCellRow CellRow = new RosterClassicCellRow();
                                        CellRow.Name = name;
                                        CellRow.RosterOption = RosterOption;
                                        CellRow.Leave = Leave;
                                        CellRow.LeaveStatus = LeaveStatus;
                                        CellRow.StaffID = staffid;
                                        CellRow.WorkHrs = lWorkhours;
                                        //CellRow.WorkHrs = GetWorkHours( profile, staffid, MyGlobal.GetInt16(year), iMonth, day);
                                        cell[day].cellRows.Add(CellRow);
                                    }
                                }
                            }
                        }
                    }
                }
                Int32 unixTimestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Int32 unixMonthStart = MyGlobal.GetSeconds(MyGlobal.GetInt16(year), iMonth - 1);

                for (int day = 1; day <= 31; day++)
                {
                    cell[day].Day = day;
                    cell[day].expired = ((unixMonthStart + (day - 1) * 86400) - unixTimestamp + 19800) / 86400;

                    //cell[day].expired = ((unixMonthStart + (day) * 86400) - unixTimestamp - 19800) / 86400;
                    //cell[day].expired = ((unixMonthStart + (day) * 86400) >= unixTimestamp) ? 0 : 1;
                    row.cells.Add(cell[day]);
                }
                classicRosterResponse.rows.Add(row);

            }
        }
        private long GetWorkHours(string profile, string staffid, int year, int iMonth, int day)
        {
            Int32 unixDate = MyGlobal.GetSeconds(year, (iMonth - 1), day);
            string sSQL = "select lWorkHours from " + MyGlobal.activeDB + ".tbl_attendance " +
                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' and m_Date='" + unixDate + "'";

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
                                return reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            }
                        }
                    }
                }
            }
            return 0;
        }
        //-------------------------------------------------------------------------
        [HttpPost]
        public ActionResult ThisDay(string profile, string roster,
            string shift, string staffname, string staffid,
            string year, string month, string day,
            string marker, string mode, string response, long lShiftStartSecs) // lShiftStartSecs used for session key
        {

            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var thisDayResponse = new ThisDayResponse();
            thisDayResponse.status = false;
            thisDayResponse.result = "";
            int iMonth = MyGlobal.GetInt16(month);
            long unixTimeDateStart = (Int32)((new DateTime(MyGlobal.GetInt16(year), iMonth, MyGlobal.GetInt16(day))).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            try
            {

                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("update"))
                    {
                        bool bRowExists;
                        sSQL = "SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                            "where m_Profile = '" + profile + "' and m_RosterName = '" + roster + "' " +
                            "and m_ShiftName = '" + shift + "' " +
                            "and m_StaffID = '" + staffid + "' and m_Year = '" + year + "' " +
                            "and m_Month = '" + (iMonth - 1) + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bRowExists = reader.HasRows;
                            }
                        }
                        sSQL = "";
                        string MarkerField = "'" + marker + "'";
                        if (marker.Equals("void")) MarkerField = "null";
                        if (bRowExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_rosters " +
                                "Set m_Day" + day + "=" + MarkerField + " " +
                                "where m_Profile = '" + profile + "' and m_RosterName = '" + roster + "' " +
                                "and m_ShiftName = '" + shift + "' " +
                                "and m_StaffID = '" + staffid + "' and m_Year = '" + year + "' " +
                                "and m_Month = '" + (iMonth - 1) + "'";
                        }
                        else
                        {
                            Int32 int32ShiftStartTime = 0, int32ShiftEndTime = 0;
                            sSQL = "SELECT m_ShiftStartTime,m_ShiftEndTime FROM " + MyGlobal.activeDB + ".tbl_rosters " +
                                "where m_Profile = '" + profile + "' and m_RosterName = '" + roster + "' " +
                                "and m_ShiftName = '" + shift + "' " +
                                "and m_StaffID is null and m_Year = '" + year + "' " +
                                "and m_Month = '" + (iMonth - 1) + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            int32ShiftStartTime = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                            int32ShiftEndTime = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                        }
                                    }
                                }
                            }
                            //----------------------------------
                            sSQL = "";
                            if (int32ShiftStartTime > 0)
                            {
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_rosters " +
                                    "(m_Profile,m_RosterName,m_ShiftName,m_StaffID,m_StaffName," +
                                    "m_Year,m_Month,m_Day" + day + ",m_ShiftStartTime,m_ShiftEndTime) values " +
                                    "('" + profile + "','" + roster + "','" + shift + "'," +
                                    "'" + staffid + "','" + staffname + "'," +
                                    "'" + year + "','" + (iMonth - 1) + "'," + MarkerField + "," +
                                    "'" + int32ShiftStartTime + "','" + int32ShiftEndTime + "');";
                            }
                            else
                            {
                                response = "Unable to add. Shift time not available";
                            }
                        }
                        if (sSQL.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                thisDayResponse.status = true;
                                thisDayResponse.result = "Updated";
                            }
                        }
                    }
                    else if (mode.Equals("remove"))
                    {
                        MySqlTransaction trans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = trans;
                        try
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_rosters " +
                            "Set m_Day" + day + "=null " +
                            "where m_Profile = '" + profile + "' and m_RosterName = '" + roster + "' " +
                            "and m_ShiftName = '" + shift + "' " +
                            "and m_StaffID = '" + staffid + "' and m_Year = '" + year + "' " +
                            "and m_Month = '" + (iMonth - 1) + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                            "where m_Profile='" + profile + "' and m_HardwareID='Manual' and m_Activity='open' " +
                            "and m_Session='" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "where m_Profile='" + profile + "' and m_HardwareID='Manual' and m_Activity='update' " +
                                "and m_Session='" + (staffid + "_maual_1_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' and m_Year='" + year + "' " +
                                "and m_Month='" + (iMonth - 1) + "' and m_Date='" + unixTimeDateStart + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            trans.Commit();
                            thisDayResponse.status = true;
                            thisDayResponse.result = "Removed";
                        }
                        catch (Exception ex) //error occurred
                        {
                            trans.Rollback();
                            //response.result = "Error " + ex.Message;
                        }
                    }
                    else if (mode.Equals("markabsent"))
                    {
                        MySqlTransaction trans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = trans;
                        try
                        {

                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                            "where m_Profile='" + profile + "' and m_HardwareID='Manual' and m_Activity='open' " +
                            "and m_Session='" + (staffid + "_maual_0_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                                "where m_Profile='" + profile + "' and m_HardwareID='Manual' and m_Activity='update' " +
                                "and m_Session='" + (staffid + "_maual_1_" + (unixTimeDateStart + lShiftStartSecs - 19800)) + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' and m_Year='" + year + "' " +
                                "and m_Month='" + (iMonth - 1) + "' and m_Date='" + unixTimeDateStart + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();

                            trans.Commit();
                        }
                        catch (Exception ex) //error occurred
                        {
                            trans.Rollback();
                            //response.result = "Error " + ex.Message;
                        }
                    }

                    //----------------------------------------------

                    sSQL = "select m_ShiftStart,m_ShiftEnd,m_ActualStart,m_ActualEnd,m_MarkRoster," +
                        "m_MarkLeave " +
                        "from " + MyGlobal.activeDB + ".tbl_attendance " +
                        "where m_Profile = '" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "' " +
                        "and m_Date='" + unixTimeDateStart + "' order by m_ActualStart";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                    {
                                        if (!reader.IsDBNull(0)) thisDayResponse.m_ShiftStart = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                        if (!reader.IsDBNull(1)) thisDayResponse.m_ShiftEnd = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                        if (!reader.IsDBNull(2)) thisDayResponse.m_ActualStart = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                                        if (!reader.IsDBNull(2)) thisDayResponse.m_ActualEnd = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------------------------
                    thisDayResponse.status = true;
                    if (response != null) if (response.Length > 0) thisDayResponse.result = response;
                }
            }
            catch (MySqlException ex)
            {
                thisDayResponse.status = false;
                thisDayResponse.result = ex.Message;
            }
            return Json(thisDayResponse, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------------------------------------------------

        public ActionResult GetBiodevices(string profile, string staffidloggedin, string sort, string order, string page,
            string search, string newbioslno, string newbiomake, string newbiomodel, string mode, string value)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var biodeviceResponse = new BiodeviceResponse();
            biodeviceResponse.status = false;
            biodeviceResponse.result = "";
            biodeviceResponse.total_count = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_biometric_devices " +
                            "where m_Profile='" + profile + "' and m_id='" + value + "'";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            biodeviceResponse.result = "<span style='color:blue;'>Device deleted</span>";
                            biodeviceResponse.reload = true;
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bCreateNew = false;
                        if (newbioslno.Length < 3)
                        {
                            biodeviceResponse.result = "Invalid Machine SlNo";
                        }
                        else if (newbiomake.Length < 1)
                        {
                            biodeviceResponse.result = "Invalid Machine Make";
                        }
                        else if (newbiomodel.Length < 1)
                        {
                            biodeviceResponse.result = "Invalid Machine Model";
                        }
                        else
                        {
                            bCreateNew = true;
                        }
                        if (bCreateNew)
                        {
                            bCreateNew = true;
                            newbioslno = newbioslno.ToUpper().Replace(" ", "");
                            string sProfileIn = "";
                            sSQL = "select m_Profile from " + MyGlobal.activeDB + ".tbl_biometric_devices " +
                                    "where m_MachineSlNo='" + newbioslno + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    bCreateNew = !reader.HasRows;
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            sProfileIn = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        }
                                    }

                                }
                            }

                            if (!bCreateNew)
                            {
                                biodeviceResponse.result = "<span style='color:red;'>SlNo " + newbioslno + " alread exists in profile " + sProfileIn + "</span>";
                            }
                            else
                            {
                                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;
                                DateTime date = DateTime.Now;
                                Int32 unixTimeDayStart = (Int32)((new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                sSQL = "Insert into " + MyGlobal.activeDB + ".tbl_biometric_devices " +
                                  "(m_Profile,m_MachineSlNo,m_Make,m_Model,m_CreatedBy,m_CreatedTime) values " +
                                "('" + profile + "','" + newbioslno + "','" + newbiomake + "','" + newbiomodel + "'," +
                                "'" + staffidloggedin + "',Now());";

                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                    biodeviceResponse.result = "<span style='color:blue;'>New Bio device added</span>";
                                    biodeviceResponse.reload = true;
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (m_MachineSlNo like '%" + search + "%' or " +
                        "m_Make like '%" + search + "%' or " +
                        "m_Model like '%" + search + "%') ";


                    sSQL = "SELECT  count(m_id) as cnt FROM " + MyGlobal.activeDB + ".tbl_biometric_devices " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "group by m_CreatedTime";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) biodeviceResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //----------------------------------------------------------------

                    //________________________________________________________________
                    int iPageSize = 10;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_CreatedTime";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    if (mode.Equals("new"))
                    {
                        sort = "m_CreatedTime";
                        order = "desc";
                        PAGE = 0;
                    }

                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_biometric_devices " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    BiodeviceRow item = new BiodeviceRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) item.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MachineSlNo"))) item.m_MachineSlNo = reader["m_MachineSlNo"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Make"))) item.m_Make = reader["m_Make"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Model"))) item.m_Model = reader["m_Model"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedBy"))) item.m_CreatedBy = reader["m_CreatedBy"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTime"))) item.m_CreatedTime = reader.GetDateTime(reader.GetOrdinal("m_CreatedTime")).ToString("dd-MM-yyyy HH:mm:ss"); ;
                                    biodeviceResponse.items.Add(item);
                                }
                                biodeviceResponse.status = true;
                            }
                            else
                            {
                                if (biodeviceResponse.result.Length == 0)
                                    biodeviceResponse.result = "Sorry!!! No Biodevices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetBiodevices-MySqlException-" + ex.Message);
                biodeviceResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetBiodevices-Exception-" + ex.Message);
                biodeviceResponse.result = "Error-" + ex.Message;
            }
            return Json(biodeviceResponse, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------------------------------------------
        [HttpPost]
        public ActionResult ManageProfile(string profile, string mode, string name, string add1, string add2,
            string mob1, string mob2, string email, string prefix, string length,
            string attnstartdate)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new ProfileInfo();
            response.status = false;
            response.result = "";
            bool bFirstRun = true;
            int iAttnstartdate = MyGlobal.GetInt16(attnstartdate);
            if (iAttnstartdate == 0)
            {
                iAttnstartdate = 1;
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    fetch_again:;
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_profile_info " +
                        "where m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    response.Name = MyGlobal.GetPureString(reader, "m_CompName");
                                    response.Address1 = MyGlobal.GetPureString(reader, "m_CompAdd1");
                                    response.Address2 = MyGlobal.GetPureString(reader, "m_CompAdd2");
                                    response.Mobile1 = MyGlobal.GetPureString(reader, "m_CompMobile1");
                                    response.Mobile2 = MyGlobal.GetPureString(reader, "m_CompMobile2");
                                    response.Email = MyGlobal.GetPureString(reader, "m_CompEmail");
                                    response.StaffIDPrefix = MyGlobal.GetPureString(reader, "m_StaffID_Prefix");
                                    response.StaffIDLength = MyGlobal.GetPureString(reader, "m_StaffID_NumericDigits");
                                    response.AttnStartDate = MyGlobal.GetPureInt16(reader, "m_AttnStartDate");
                                }
                                response.status = true;
                            }
                            else
                            {
                                response.result = "Sorry!!! No Data";
                            }
                        }
                    }
                    //-------------------------------------------
                    if (mode.Equals("update") && bFirstRun)
                    {
                        bFirstRun = false;
                        if (response.status)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_profile_info Set " +
                                "m_CompName='" + name + "'," +
                                "m_CompAdd1='" + add1 + "'," +
                                "m_CompAdd2='" + add2 + "'," +
                                "m_CompMobile1='" + mob1 + "'," +
                                "m_CompMobile2='" + mob2 + "'," +
                                "m_CompEmail='" + email + "', " +
                                "m_StaffID_Prefix='" + prefix + "', " +
                                "m_StaffID_NumericDigits='" + length + "', " +
                                "m_AttnStartDate='" + iAttnstartdate + "' " +
                                "where m_Profile='" + profile + "';";
                        }
                        else
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_profile_info " +
                                "(m_Profile,m_CompName,m_CompAdd1,m_CompAdd2," +
                                "m_CompMobile1,m_CompMobile2,m_CompEmail," +
                                "m_StaffID_Prefix,m_StaffID_NumericDigits) values " +
                                "('" + profile + "','" + name + "','" + add1 + "','" + add2 + "'," +
                                "'" + mob1 + "','" + mob2 + "','" + email + "'," +
                                "'" + prefix + "','" + length + "')";
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            response.result = "Updated";
                            goto fetch_again;
                        }
                    }
                    //-------------------------------------------
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ManageProfile-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ManageProfile-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------------------------------------------
        public ActionResult Statement_RetentionResponse(string profile, string sort, string order,
string page, string search, string timezone, string team, string bank,
string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
string dtbank, string chkshowall, string loginstaff)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            bool bListSelectedIsApproved = false;
            team = team.Trim();
            if (level == null) level = "";
            var retentionBonusModel = new RetentionBonusModel();
            retentionBonusModel.status = false;
            retentionBonusModel.result = "";
            retentionBonusModel.total_count = 0;
            bank = "";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";
                //________________________________________________________________
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "select m_Status,m_Team from " + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + loginstaff + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string _status = reader.GetString(0);
                                    string _team = reader.GetString(1);
                                    if (_team.IndexOf("TAM") == 0 && _status.Equals("Active"))
                                    {
                                        retentionBonusModel.level = "tam";
                                    }
                                    else if (_team.IndexOf("Accounts & Administration") == 0 && _status.Equals("Active"))
                                    {
                                        retentionBonusModel.level = "accounts";
                                    }
                                    if (loginstaff.Equals("10000") || loginstaff.Equals("CHC0001") || loginstaff.Equals("CHC0002"))
                                    {
                                        retentionBonusModel.level = "admin";
                                    }
                                }
                            }
                        }
                    }
                    /*
                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_retention_list " +
                       "where m_Profile='" + profile + "' and m_Year='" + dtYear + "' " +
                       "and m_Month='" + dtMonth + "';";
                    //"and m_Bank='" + bank + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    retentionBonusModel.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        //retentionBonusModel.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }*/
                    //________________________________________________________________
                    String sSearchKey = " (m_StaffID like '%" + search + "%' or " +
                        "m_Name like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_retention_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";
                    /*
                    sSQL = "SELECT count(DISTINCT summary.m_StaffID) as cnt FROM " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
"left join " + MyGlobal.activeDB + ".tbl_staffs as staffs on staffs.m_StaffID = summary.m_StaffID " +
"where summary.m_Year = '" + iYear + "' and summary.m_Month = '" + iMonth + "' and staffs.m_RetentionBonusEffectiveDate is not null " +
"and m_RetentionBonusEffectiveDate<='" + iYear + "-" + (iMonth+1) + "-1'" +
"and summary.m_Profile = '" + profile + "'";
*/
                    /*
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ESIC > 0 ";
                    }*/

                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    /*
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_ESIC='" + list + "' ";
                        else
                            sSQL += "and (m_List_ESIC is null or m_List_ESIC='') ";
                    }
                    */
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        retentionBonusModel.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________

                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_StaffID";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    /*
                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
"list.m_EarnsTot,list.m_DeductsTot," +
"'','',''," +
"list.m_Team,list.m_Selected_ESIC,list.m_id,list.m_Bank,list.m_List_ESIC," +
"list.m_sb_acc,list.m_epf_uan,list.m_GrossWages," +
"list.m_DaysTobePaidTotal,list.m_BasicPay,list.m_ESIC " +
"FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
"where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
"and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";



                    sSQL = "SELECT summary.m_StaffID,staffs.m_FName,sum(summary.m_ActualWorkingDays) as m_ActualWorkingDays,sum(summary.m_DaysTobePaidTotal) as m_DaysTobePaidTotal,staffs.m_Team," +
                        "m_CCTNo,m_CCTCleardDate,m_RetentionBonusEffectiveDate,m_RetentionBonusAmount " +
                        "FROM " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
"left join " + MyGlobal.activeDB + ".tbl_staffs as staffs on staffs.m_StaffID = summary.m_StaffID " +
"where summary.m_Year = '" + iYear + "' and summary.m_Month = '" + iMonth + "' and staffs.m_RetentionBonusEffectiveDate is not null " +
"and m_RetentionBonusEffectiveDate<='" + iYear + "-" + (iMonth + 1) + "-1'" +
"and summary.m_Profile = '" + profile + "' group by summary.m_StaffID ";
*/
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_retention_list list " +
                                       "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                                       "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";

                    /*
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ESIC > 0 ";
                    }
                    */
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    /*
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_ESIC='" + list + "' ";
                        else
                            sSQL += "and (m_List_ESIC is null or m_List_ESIC='') ";
                    }
                    */
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    RetentionBonusItem item = new RetentionBonusItem();
                                    item.m_id = reader.GetInt32(0); // MyGlobal.GetPureString(reader, "m_id");
                                    item.m_Name = MyGlobal.GetPureString(reader, "m_Name");
                                    item.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    item.m_Team = MyGlobal.GetPureString(reader, "m_Team");
                                    item.m_ActualWorkingDays = MyGlobal.GetPureDouble(reader, "m_ActualWorkingDays");
                                    item.m_DaysTobePaidTotal = MyGlobal.GetPureDouble(reader, "m_DaysTobePaidTotal");
                                    item.m_CCTNo = MyGlobal.GetPureString(reader, "m_CCTNo");
                                    item.m_CCTCleardDate = MyGlobal.GetPureDateTimeString(reader, "m_CCTCleardDate");
                                    item.m_RetentionBonusEffectiveDate = MyGlobal.GetPureString(reader, "m_RetentionBonusEffectiveDate");
                                    item.m_RetentionBonusAmount = MyGlobal.GetPureDouble(reader, "m_Amount"); //m_RetentionBonusAmount

                                    int ord = reader.GetOrdinal("m_ApprovalHR_by");
                                    item.m_SelectedHR = !reader.IsDBNull(ord);
                                    ord = reader.GetOrdinal("m_ApprovalAccounts_by");
                                    item.m_SelectedAccounts = !reader.IsDBNull(ord);

                                    ord = reader.GetOrdinal("m_FreezedOn");
                                    item.m_FreezedOn = reader.IsDBNull(ord) ? "" : reader.GetDateTime(ord).ToString("dd-MM-yyyy hh:mm:ss");
                                    ord = reader.GetOrdinal("m_FreezedBy");
                                    item.m_FreezedBy = reader.IsDBNull(ord) ? "" : reader.GetString(ord);

                                    ord = reader.GetOrdinal("m_ApprovalAccounts_date");
                                    item.m_ApprovalAccounts_date = reader.IsDBNull(ord) ? "" : reader.GetDateTime(ord).ToString("dd-MM-yyyy hh:mm:ss");
                                    ord = reader.GetOrdinal("m_ApprovalAccounts_by");
                                    item.m_ApprovalAccounts_by = reader.IsDBNull(ord) ? "" : reader.GetString(ord);

                                    ord = reader.GetOrdinal("m_ApprovalHR_date");
                                    item.m_ApprovalHR_date = reader.IsDBNull(ord) ? "" : reader.GetDateTime(ord).ToString("dd-MM-yyyy hh:mm:ss");
                                    ord = reader.GetOrdinal("m_ApprovalHR_by");
                                    item.m_ApprovalHR_by = reader.IsDBNull(ord) ? "" : reader.GetString(ord);

                                    if (item.m_FreezedOn.Length > 0)
                                    {
                                        ord = reader.GetOrdinal("m_FreezedOn");
                                        string title = "Freezed on " + reader.GetString(ord) + " ";
                                        ord = reader.GetOrdinal("m_FreezedBy");
                                        title += "by " + reader.GetString(ord);
                                        item.title = title;
                                    }
                                    //---------------------------------
                                    retentionBonusModel.items.Add(item);
                                }
                                retentionBonusModel.status = true;
                            }
                            else
                            {
                                retentionBonusModel.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                retentionBonusModel.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--Statement_ESICResponse--" + ex.Message);
            }

            return Json(retentionBonusModel, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Statement_RETENTION_to_Excel(string profile, int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new StatementRetentionExcelResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.ret_filename = "Retention_bonus_Statement_" +
                MyGlobal.constArrayMonths[month - 1] + "_" + year;
            string sSQL = "";
            /*
            string sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
"list.m_EarnsTot,list.m_DeductsTot," +
"'','',''," +
"list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List," +
"list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_DaysTobePaidTotal," +
"list.m_BasicPay,list.m_EPFContributionRemitted,list.m_ESIC " +
"FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
"where list.m_Profile='" + profile + "' " +
"and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";

            sSQL = "SELECT summary.m_StaffID,staffs.m_FName,sum(summary.m_ActualWorkingDays) as m_ActualWorkingDays,sum(summary.m_DaysTobePaidTotal) as m_DaysTobePaidTotal,staffs.m_Team," +
    "m_CCTNo,m_CCTCleardDate,m_RetentionBonusEffectiveDate,m_RetentionBonusAmount " +
    "FROM " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
"left join " + MyGlobal.activeDB + ".tbl_staffs as staffs on staffs.m_StaffID = summary.m_StaffID " +
"where summary.m_Year = '" + year + "' and summary.m_Month = '" + (month- 1) + "' and staffs.m_RetentionBonusEffectiveDate is not null " +
"and m_RetentionBonusEffectiveDate<='" + year + "-" + (month) + "-1'" +
"and summary.m_Profile = '" + profile + "' group by summary.m_StaffID ";
*/
            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_retention_list list " +
                   "where m_Profile='" + profile + "' " +
                   "and m_Year='" + year + "' and m_Month='" + (month - 1) + "' " +
                   "and m_ApprovalHR_by is not null and m_ApprovalAccounts_by is not null and m_FreezedOn is not null;";

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
                                while (reader.Read())
                                {
                                    Statement_RETENTION_ExcelRow item = new Statement_RETENTION_ExcelRow();
                                    item.Name = MyGlobal.GetPureString(reader, "m_Name");
                                    item.StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    item.Team = MyGlobal.GetPureString(reader, "m_Team");

                                    //item.DaysTobePaidTotal = MyGlobal.GetPureDouble(reader, "m_DaysTobePaidTotal");
                                    //item.CCTNo = MyGlobal.GetPureString(reader, "m_CCTNo");
                                    //item.CCTCleardDate = MyGlobal.GetPureDateTimeString(reader, "m_CCTCleardDate");
                                    //item.RetentionBonusEffectiveDate = MyGlobal.GetPureDateTimeString(reader, "m_RetentionBonusEffectiveDate");


                                    item.m_Base = MyGlobal.GetPureString(reader, "m_Base");
                                    item.m_Bank = MyGlobal.GetPureString(reader, "m_Bank");
                                    item.m_Branch = MyGlobal.GetPureString(reader, "m_Branch");
                                    item.m_IFSC = MyGlobal.GetPureString(reader, "m_IFSC");
                                    item.m_AccountNo = MyGlobal.GetPureString(reader, "m_AccountNo");

                                    item.ActualWorkingDays = MyGlobal.GetPureDouble(reader, "m_ActualWorkingDays");
                                    item.Amount = MyGlobal.GetPureDouble(reader, "m_Amount");

                                    statementResponse.rows.Add(item);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--Statement_RETENTION_to_Excel--" + ex.Message);
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------
        [HttpPost]
        public ActionResult GeoLocations(string profile, string mode, string name,
    double lat, double lng, int accuracy)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new MobileAccessLocation();
            response.status = false;
            response.result = "";
            bool bFirstRun = true;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    fetch_again:;
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                        "where m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    response.Name = MyGlobal.GetPureString(reader, "m_Name");
                                    response.m_Lat = MyGlobal.GetPureDouble(reader, "m_Lat");
                                    response.m_Lng = MyGlobal.GetPureDouble(reader, "m_Lng");
                                    response.m_Accuracy = MyGlobal.GetPureInt16(reader, "m_Accuracy");
                                }
                                response.status = true;
                            }
                            else
                            {
                                response.result = "Sorry!!! No Data";
                            }
                        }
                    }
                    //-------------------------------------------
                    if (mode.Equals("update") && bFirstRun)
                    {
                        bFirstRun = false;
                        if (response.status)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations Set " +
                                "m_Name='" + name + "'," +
                                "m_Lat='" + lat + "'," +
                                "m_Lng='" + lng + "'," +
                                "m_Accuracy='" + accuracy + "' " +
                                "where m_Profile='" + profile + "';";
                        }
                        else
                        {
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                                "(m_Profile,m_Name,m_Lat,m_Lng," +
                                "m_Accuracy) values " +
                                "('" + profile + "','" + name + "','" + lat + "','" + lng + "'," +
                                "'" + accuracy + "')";
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            response.result = "Updated";
                            goto fetch_again;
                        }
                    }
                    //-------------------------------------------
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("Manage GeoLocations-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Manage GeoLocations-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ProcessRetentionBonus(string profile, int year, int month, string staff)// staff logged in
        {
            var retentionBonusModel = new RetentionBonusModel();
            retentionBonusModel.status = false;
            retentionBonusModel.result = "";
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                /*
                //-------------Temporary to make the bank updated
                string sqltmpupdate = "";
                string sqltmp = "select m_Base,m_Bank,m_Branch,m_IFSC,m_AccountNo,m_StaffID from " + MyGlobal.activeDB + ".tbl_staffs";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sqltmp, con))
                {
                    //retentionBonusModel.items = con.Query<RetentionBonusItem>(sSQL).ToList();
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string sql = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
                                    "set m_Base='" + (reader.IsDBNull(0)?"":reader.GetString(0)) + "'," +
                                    "m_Bank='" + (reader.IsDBNull(1) ? "" : reader.GetString(1)) + "'," +
                                    "m_Branch='" + (reader.IsDBNull(2) ? "" : reader.GetString(2)) + "'," +
                                    "m_IFSC='" + (reader.IsDBNull(3) ? "" : reader.GetString(3)) + "'," +
                                    "m_AccountNo ='" + (reader.IsDBNull(4) ? "" : reader.GetString(4)) + "' "+
                                    "where m_StaffID='" + (reader.IsDBNull(5) ? "" : reader.GetString(5)) + "';";

                                sqltmpupdate += sql;
                            }
                        }
                    }

                }
                if (sqltmpupdate.Length > 0)
                {
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sqltmpupdate, con))
                    {
                        int iRes = mySqlCommand.ExecuteNonQuery();
                        retentionBonusModel.result = iRes + " records.";
                    }
                }
                return Json(retentionBonusModel, JsonRequestBehavior.AllowGet);
                */
                //------------------------------------------------------

                using (MySqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        //--------------------------------Get All Staffs

                        string sSQL = "", sSQLInsert = "";
                        //------------------------------------------------------
                        sSQL = "DELETE from " + MyGlobal.activeDB + ".tbl_retention_list " +
                            "where m_Profile='" + profile + "' and m_Year = '" + year + "' and m_Month = '" + month + "' " +
    "and m_ApprovalHR_by is null and m_ApprovalAccounts_by is null and m_FreezedOn is null;";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con, trans))
                        {
                            int iRes = mySqlCommand.ExecuteNonQuery();
                        }

                        sSQL = "SELECT summary.m_Profile,staffs.m_FName,staffs.m_StaffID, summary.m_Year,summary.m_Month, " +
                            "sum(summary.m_ActualWorkingDays) as m_ActualWorkingDays,sum(summary.m_DaysTobePaidTotal) as m_DaysTobePaidTotal," +
                            "staffs.m_RetentionBonusAmount,staffs.m_Team,staffs.m_CCTNo,staffs.m_CCTCleardDate," +
                            "staffs.m_RetentionBonusEffectiveDate,summary.m_RosterOptionsResult," +
                            "staffs.m_AccountNo,staffs.m_IFSC,staffs.m_Branch,staffs.m_Base,staffs.m_Bank " +
                            "FROM " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
    "left join " + MyGlobal.activeDB + ".tbl_staffs as staffs on staffs.m_StaffID = summary.m_StaffID " +
    "where summary.m_Year = '" + year + "' and summary.m_Month = '" + month + "' and staffs.m_RetentionBonusEffectiveDate is not null " +
    "and m_RetentionBonusEffectiveDate<='" + year + "-" + (month + 1) + "-26'" +
    "and m_Status='Active' " +
    "and summary.m_Profile = '" + profile + "' " +
    "and summary.m_StaffID not in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_retention_list where " +
    "m_Profile = '" + profile + "' and m_Year='" + year + "' and m_Month='" + month + "')" +
    "group by summary.m_StaffID ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con, trans))
                        {
                            //retentionBonusModel.items = con.Query<RetentionBonusItem>(sSQL).ToList();
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {

                                        /*
                                        if (reader.GetString(2).Equals("CHC0534"))
                                        {
                                            Console.WriteLine("here");
                                        }
                                        */
                                        string sql = "INSERT INTO " + MyGlobal.activeDB + ".tbl_retention_list (" +
        "m_Profile,m_Name,m_StaffID,m_Year,m_Month,m_ActualWorkingDays,m_DaysTobePaidTotal,m_Amount,m_Team,m_CCTNo," +
        "m_CCTCleardDate,m_RetentionBonusEffectiveDate," +
        "m_AccountNo,m_IFSC,m_Branch,m_Base,m_Bank," +
        "m_ProcessedBy, m_ProcessedOn " +
        ") values (";
                                        sql += "'" + reader.GetString(0) + "',";
                                        sql += "'" + reader.GetString(1) + "',";
                                        sql += "'" + reader.GetString(2) + "',";
                                        sql += "'" + reader.GetString(3) + "',";
                                        sql += "'" + reader.GetString(4) + "',";
                                        //sql += "'" + (reader.GetDouble(5) + GetACO_HP_Counts(reader.IsDBNull(12)?"":reader.GetString(12))) + "',";
                                        sql += "'" + (reader.GetDouble(5) + GetACO_HP_Counts_Updated(reader.GetString(0), reader.GetString(2), reader.GetString(3), reader.GetString(4))) + "',";
                                        sql += "'" + reader.GetString(6) + "',";
                                        sql += "'" + (reader.IsDBNull(7)?"0": reader.GetString(7)) + "',";//m_RetentionBonusAmount
                                        sql += "'" + reader.GetString(8) + "',";//m_Team
                                        //sql += "'" + (reader.GetString(9)) + "',";//m_CCTNo
                                        sql += "" + (reader.IsDBNull(9) ? "null" : "'" + reader.GetString(9) + "'") + ",";//m_CCTNo


                                        sql += "" + (reader.IsDBNull(10) ? "null" : "'" + reader.GetDateTime(10).ToString("yyyy-MM-dd") + "'") + ",";
                                        sql += "" + (reader.IsDBNull(11) ? "null" : "'" + reader.GetDateTime(11).ToString("yyyy-MM-dd") + "'") + ",";

                                        sql += "" + (reader.IsDBNull(13) ? "null" : "'" + reader.GetString(13) + "'") + ",";
                                        sql += "" + (reader.IsDBNull(14) ? "null" : "'" + reader.GetString(14) + "'") + ",";
                                        sql += "" + (reader.IsDBNull(15) ? "null" : "'" + reader.GetString(15) + "'") + ",";
                                        sql += "" + (reader.IsDBNull(16) ? "null" : "'" + reader.GetString(16) + "'") + ",";
                                        sql += "" + (reader.IsDBNull(17) ? "null" : "'" + reader.GetString(17) + "'") + ",";

                                        sql += "'" + staff + "',";
                                        sql += "Now()";
                                        sql += ");";
                                        sSQLInsert += sql;
                                    }
                                }
                            }

                        }
                        if (sSQLInsert.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLInsert, con, trans))
                            {
                                int iRes = mySqlCommand.ExecuteNonQuery();
                                retentionBonusModel.result = iRes + " records modified.";
                            }
                        }
                        trans.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        trans.Rollback();
                        retentionBonusModel.result = "Failed";
                        MyGlobal.Error("MySqlException Retention Bonus-" + ex.Message);
                        retentionBonusModel.result = "Error-" + ex.Message;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        retentionBonusModel.result = "Failed";
                        MyGlobal.Error("Exception Retention Bonus-" + ex.Message);
                        retentionBonusModel.result = "Error-" + ex.Message;
                    }
                }
            }
            return Json(retentionBonusModel, JsonRequestBehavior.AllowGet);
        }
        private double GetACO_HP_Counts_Updated(string profile, string staffid, string year, string month)
        {
            double counts = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                string sql = "SELECT m_RosterOptionsResult " +
                "FROM meterbox.tbl_attendance_summary summary " +
                "where summary.m_Year = '" + year + "' and summary.m_Month = '" + month + "' " +
                "and summary.m_Profile = '" + profile + "' and summary.m_StaffID = '" + staffid + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sql, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                counts += GetACO_HP_Counts(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            return counts;
        }
        /*
         /ACO:1, HP:1, 
             */
        private double GetACO_HP_Counts(string str)
        {
            double dblCount = 0;
            char[] delimiterChars = { ',' };
            string[] arData = str.Split(delimiterChars);
            foreach (string Bit in arData)
            {
                string bit= Bit.TrimEnd().TrimStart();
                if (bit.IndexOf("/ACO:") > -1)
                {
                    if (bit.Length > 5)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(5),out val);
                        dblCount += val/ 2.0f;
                    }
                }
                if (bit.IndexOf("ACO/:") > -1)
                {
                    if (bit.Length > 5)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(5), out val);
                        dblCount += val / 2.0f;
                    }
                }
                if (bit.IndexOf("ACO:") > -1)
                {
                    if (bit.Length > 4)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(4), out val);
                        dblCount += val;
                    }
                }
                if (bit.IndexOf("/HP:") > -1)
                {
                    if (bit.Length > 4)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(4), out val);
                        dblCount += val / 2.0f;
                    }
                }
                if (bit.IndexOf("HP/:") > -1)
                {
                    if (bit.Length > 4)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(4), out val);
                        dblCount += val / 2.0f;
                    }
                }
                if (bit.IndexOf("HP:") > -1)
                {
                    if (bit.Length > 3)
                    {
                        int val = 0;
                        int.TryParse(bit.Substring(3), out val);
                        dblCount += val;
                    }
                }
            }

            return dblCount;
        }
    }
}
 
