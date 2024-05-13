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
    public partial class AccessmanagerController:Controller
    {
        private bool CheckLeaveAlreadyExists(string profile,string staffid,
            MySqlConnection con,string year,string month,int iSelectedDay,
            out string sErrMessage)
        {
            bool bLevelCrossed = false;
            sErrMessage = "";
            bool bCreateNewRecord = false;
            
            //-----Check Leave table for issues
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
                            int ordinal = reader.GetOrdinal("m_DayL" + iSelectedDay);
                            if (reader.IsDBNull(ordinal))
                            {
                                bLevelCrossed = true;
                            }
                            else
                            {
                                int ordinalStatus = reader.GetOrdinal("m_Status" + iSelectedDay);
                                int iStatus = 0;
                                if (!reader.IsDBNull(ordinalStatus)) iStatus = reader.GetInt16(ordinalStatus);
                                if ((iStatus == C_REVOKE_PENDING) || (iStatus == C_APPROVED))
                                {
                                    sErrMessage = "Confirmed leave [" + reader.GetString(ordinal) + "] already exists for this day";
                                }
                                else if (iStatus == 1)
                                {
                                    sErrMessage = "Leave request [" + reader.GetString(ordinal) + "] pending";
                                }
                                else
                                {
                                    sErrMessage = "Unable to process";
                                }
                            }
                        }
                    }
                    else
                    {
                        bCreateNewRecord = true;
                    }
                }
            }
            if (bCreateNewRecord)
            {
                string staffname = "";
                sSQL = "SELECT m_FName FROM " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' " +
"and m_StaffID='" + staffid + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (reader.IsDBNull(0)) staffname = reader.GetString(0);
                            }
                        }
                    }
                }
                sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_leaves (m_StaffID,m_StaffName,m_Year,m_Month,m_Profile) values ('" + staffid + "','" + staffname + "','" + year + "','" + (MyGlobal.GetInt16(month) - 1) + "','" + profile + "');";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    mySqlCommand.ExecuteNonQuery();
                    bLevelCrossed = true;
                }
            }
            return bLevelCrossed;
        }
        //------------------------------------------------
        private void GetRosterWarningMessagesIfAny(string profile,string staffid, string month,
            string year, MySqlConnection con,int iSelectedDay, out string sRosterPendingMessage)
        {
            sRosterPendingMessage = "";
            string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Profile='" + profile + "' " +
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
                            int ordinal = reader.GetOrdinal("m_Day" + iSelectedDay);
                            if (reader.IsDBNull(ordinal))
                            {
                                sRosterPendingMessage =
                                    "<span style=''color:blue;''>Not linked to any roster</span>";
                            }
                            else
                            {
                                string ssRoster = "", ssShift = "";
                                ordinal = reader.GetOrdinal("m_RosterName");
                                if (!reader.IsDBNull(ordinal)) ssRoster = reader.GetString(ordinal);
                                ordinal = reader.GetOrdinal("m_ShiftName");
                                if (!reader.IsDBNull(ordinal)) ssShift = reader.GetString(ordinal);
                                sRosterPendingMessage =
                                    "<span style=''color:#b22;''>" +
                                    "Shift <span class=''LveM''>" + ssShift + "</span> of Roster <span class=''LveM''>" + ssRoster + "</span> has valid entry" +
                                    "</span>";
                            }
                        }
                    }
                }
            }
        }


        private void GetStaffDetails_FromStaffID(
            MySqlConnection con, string profile, string staffid,
            out string sFName, out string sStaffEmail, 
            out string sReportAdminEmail, out string sReportFuncEmail, out string sErrMessage)
        {
            sFName = "";
            sErrMessage = "";
            sReportAdminEmail = "";
            sReportFuncEmail = "";
            sStaffEmail = "";

            string sSQL = "SELECT " +
            "m_Email as StaffEmail," +
            "m_Team as StaffTeam," +
            "m_ReportToAdministrative as ReportAdminHead," +
            "m_ReportToFunctional as ReportFuncHead," +
            "m_Status,m_FName " +
            "FROM " + MyGlobal.activeDB + ".tbl_staffs " +
            "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) sStaffEmail = reader.GetString(0);
                            if (sStaffEmail.Length == 0)
                            {
                                sErrMessage = "<span style='color:red;'><b>Email not updated in staff profile</b></span>";
                                return;
                            }
                            if (!reader.IsDBNull(2)) sReportAdminEmail = reader.GetString(2);
                            if (!reader.IsDBNull(3)) sReportFuncEmail = reader.GetString(3);
                            if (sReportAdminEmail.Length == 0 && sReportFuncEmail.Length == 0)
                            {
                                sErrMessage = "<span style='color:red;'><b>Both Administrative and Functional email is not configured</b></span>";
                                return;
                            }
                            if (reader.IsDBNull(4))
                            {
                                sErrMessage = "<span style='color:red;'><b>Staff Status not assigned</b></span>";
                                return;
                            }
                            if (!reader.GetString(4).Equals("Active", StringComparison.CurrentCultureIgnoreCase) &&
                                !reader.GetString(4).Equals("Trainee", StringComparison.CurrentCultureIgnoreCase))
                            {
                                sErrMessage = "<span style='color:red;'><b>Staff 'Status' is not Active</b></span><br>[<b>Current status is " + reader.GetString(4) + "</b>]";
                                return;
                            }
                            if (!reader.IsDBNull(5)) sFName = reader.GetString(5);
                        }
                    }
                }
            }
        }
        private void GetStaffDetails_FromEmail(
            MySqlConnection con, string profile, string email,
            out string sEmailName)
        {
            sEmailName = "";

            string sSQL = "SELECT m_FName " +
                "FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                "where m_Profile = '" + profile + "' and m_Email = '" + email + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) sEmailName = reader.GetString(0);
                        }
                    }
                }
            }
        }
    }
}