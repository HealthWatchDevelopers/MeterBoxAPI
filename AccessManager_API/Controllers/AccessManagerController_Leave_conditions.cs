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
        void GetShiftStartEndTimesForThisStaffForThisDay(MySqlConnection con,string profile, string staffid, 
            string year, string month, int iSelectedDay, double dblSelectedDays, 
            ref long lShiftStart, ref long lShiftEnd,
            ref string sShiftMessage)
        {
            string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters where " +
                "m_Profile='" + profile + "' " +
                "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                "and m_StaffID='" + staffid + "' order by m_ShiftStartTime;";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int ordinal = reader.GetOrdinal("m_Day" + iSelectedDay);
                            if (!reader.IsDBNull(ordinal))
                            {
                                if (reader.GetString(ordinal).Equals(MyGlobal.WORKDAY_MARKER))
                                {
                                    string sRoster = "", sShift = "";
                                    ordinal = reader.GetOrdinal("m_ShiftStartTime");
                                    if (!reader.IsDBNull(ordinal)) {
                                        lShiftStart = reader.GetInt64(ordinal);
                                        if(!reader.IsDBNull(1))sRoster = reader.GetString(1);
                                        if (!reader.IsDBNull(2))sShift = reader.GetString(2);
                                    }
                                    ordinal = reader.GetOrdinal("m_ShiftEndTime");
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        lShiftEnd = reader.GetInt64(ordinal);
                                    }
                                    if (lShiftStart > -1)
                                    {
                                        sShiftMessage = "<div style='font-size:small;font-weight:bold;'>(On <u>"+iSelectedDay + "th</u>, Shift <b><u>" + sShift + "</u></b> of Roster <b><u>" + sRoster + "</u></b> starts at " + MyGlobal.GetHHMM(lShiftStart) + "Hours)</div>";
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        //------------------------------------------------------------------
        private bool AnyLeaveOrWeekOffClubed(MySqlConnection con, string profile, string staffid,
                string year, string month, int iSelectedDay, double dblSelectedDays, string leavetype,
                ref string sErrMessage)
        {
            int iDaysInThisMonth = MyGlobal.GetDaysInThisMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month));

            string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_rosters where " +
                "m_Profile='" + profile + "' " +
                "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                "and m_StaffID='" + staffid + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            
                            string sRosterName = "", sShiftName = "";
                            if (!reader.IsDBNull(1)) sRosterName = reader.GetString(1);
                            if (!reader.IsDBNull(2)) sShiftName = reader.GetString(2);

                            if ((iSelectedDay - 1) > 0)
                            {
                                int ordinal = reader.GetOrdinal("m_Day" + (iSelectedDay - 1));
                                if (!reader.IsDBNull(ordinal))
                                {
                                    if (reader.GetString(ordinal).Equals("OFF"))
                                    {
                                        if( (leavetype.IndexOf("/")==0) || (leavetype.IndexOf("/") == -1))
                                        {
                                            sErrMessage = "You have <u><b>OFF</b></u> on " + (iSelectedDay - 1) + "th. " +
                                                "(Roster-<b>" + sRosterName + "</b>, Shift-<b>" + sShiftName + "</b>)<br>" +
                                                leavetype + " can't be clubbed with OFF.";
                                            sErrMessage =
                                                "<span style='color:red;'>" + sErrMessage + "</span>";
                                            return true;
                                        }
                                    }
                                }
                                int iSelectedDays = 0;
                                if (dblSelectedDays < 1.0F) iSelectedDays = 1; else iSelectedDays = (int)dblSelectedDays;
                                
                                if ((iSelectedDay + iSelectedDays) <= iDaysInThisMonth) // robin
                                {
                                    ordinal = reader.GetOrdinal("m_Day" + (iSelectedDay + iSelectedDays));
                                
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        if (reader.GetString(ordinal).Equals("OFF"))
                                        {
                                            if( (leavetype.IndexOf("/") > 0) || (leavetype.IndexOf("/") == -1))
                                            {
                                                sErrMessage = "You have <u><b>OFF</b></u> on " + (iSelectedDay + iSelectedDays) + "th. " +
                                                "(Roster-<b>" + sRosterName + "</b>, Shift-<b>" + sShiftName + "</b>)<br>" +
                                                leavetype + " can't be clubbed with OFF.";
                                                sErrMessage =
                                                    "<span style='color:red;'>" + sErrMessage + "</span>";
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            //-----------------------------------Any other leaves can't be clubbed
            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_leaves where " +
                    "m_Profile='" + profile + "' " +
                    "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                    "and m_StaffID='" + staffid + "';";
            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            if ((iSelectedDay - 1) > 0)
                            {
                                // Check the previous day for any leave conditions
                                int ordinal = reader.GetOrdinal("m_DayL" + iSelectedDay); // Leave Type
                                ordinal -= 2;
                                if (!reader.IsDBNull(ordinal))
                                {
                                    string sLeaveType = reader.GetString(ordinal);
                                    if (!reader.IsDBNull(ordinal+1))
                                    {
                                        if ((reader.GetInt16(ordinal+1) == C_REVOKE_PENDING) ||
                                            (reader.GetInt16(ordinal + 1) == C_APPROVED))
                                        {
                                            bool bDontAllow = false;
                                            if (leavetype.Equals("SL") || leavetype.Equals("/SL"))
                                            {
                                                if (sLeaveType.Equals("SL") || sLeaveType.Equals("SL/") || sLeaveType.IndexOf("/") == 0)
                                                {
                                                }
                                                else
                                                {
                                                    bDontAllow = true;
                                                }
                                            }
                                            if (leavetype.Equals("PL") || leavetype.Equals("/PL"))
                                            {
                                                if (sLeaveType.Equals("PL") || sLeaveType.Equals("PL/") || sLeaveType.IndexOf("/") == 0)
                                                {
                                                }
                                                else
                                                {
                                                    bDontAllow = true;
                                                }
                                            }

                                            if (bDontAllow)
                                            {
                                                sErrMessage = "You have confirmed <u><b>" + sLeaveType + "</b></u> on " + (iSelectedDay - 1) + "th.<br>" +
                                                    leavetype + " can't be clubbed with other leaves.";
                                                sErrMessage = "<span style='color:red;'>" + sErrMessage + "</span>";
                                                return true;
                                            }
                                        }
                                    }
                                }
                                
                                //--------------------------
                                int iSelectedDays = 0;
                                if (dblSelectedDays <  1.0F) iSelectedDays = 1; else iSelectedDays = (int)dblSelectedDays;
                                
                                ordinal = reader.GetOrdinal("m_DayL" + iSelectedDay); // Leave Type
                                ordinal += (int)(2 * iSelectedDays);
                                // Check the next day of the selected day for any leave
                                // At the end of the month, next day is next month, this is not done
                                //if (iSelectedDay <= iDaysInThisMonth) //robin
                                if (iSelectedDay < iDaysInThisMonth) //robin
                                {
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        string sLeaveType = reader.GetString(ordinal);
                                        if (!reader.IsDBNull(ordinal + 1))
                                        {
                                            if( (reader.GetInt16(ordinal + 1) == C_REVOKE_PENDING) ||
                                                (reader.GetInt16(ordinal + 1) == C_APPROVED) )
                                            {
                                                bool bDontAllow = false;
                                                if (leavetype.Equals("SL") || leavetype.Equals("SL/"))
                                                {
                                                    if (sLeaveType.Equals("SL") || sLeaveType.Equals("/SL") || sLeaveType.IndexOf("/") > 0)
                                                    {
                                                    }
                                                    else
                                                    {
                                                        bDontAllow = true;
                                                    }
                                                }

                                                if (leavetype.Equals("PL") || leavetype.Equals("PL/"))
                                                {
                                                    if (sLeaveType.Equals("PL") || sLeaveType.Equals("/PL") || sLeaveType.IndexOf("/") > 0)
                                                    {
                                                    }
                                                    else
                                                    {
                                                        bDontAllow = true;
                                                    }
                                                }
                                                if (bDontAllow)
                                                {
                                                    sErrMessage = "You have confirmed <u><b>" + sLeaveType + "</b></u> on " + (iSelectedDay + iSelectedDays) + "th.<br>" +
                                                        leavetype + " can't be clubbed with other leaves.";
                                                    sErrMessage = "<span style='color:red;'>" + sErrMessage + "</span>";
                                                    return true;
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
            
            return false;
        }
        //------------------------------------------------------------------
        private bool CheckForLeaveConditions(string profile, string staffid,
            MySqlConnection con, string year, string month, int iSelectedDay,
            double dblSelectedDays, string leavetype,
            out string sErrMessage, LoadLeaveDataResponse loadLeaveDataResponse)
        {
            sErrMessage = "";
            try
            {
                string sSQL = "";

                long lShiftStart = -1, lShiftEnd = -1; string sShiftMessage = "";

                GetShiftStartEndTimesForThisStaffForThisDay(con, profile, staffid, year, month, iSelectedDay,
                    dblSelectedDays,
                    ref lShiftStart, ref lShiftEnd, ref sShiftMessage);

                DateTime dtTimeConcern = new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month), iSelectedDay);
                if (lShiftStart > -1) dtTimeConcern = dtTimeConcern.AddSeconds(lShiftStart);
                TimeSpan diff = dtTimeConcern - DateTime.Now;
                double hours = diff.TotalHours;
                //-------------------------Common to all leaves
                if (leavetype.Equals("SL") || leavetype.Equals("/SL") || leavetype.Equals("SL/")
                    || leavetype.Equals("ALOP") || leavetype.Equals("/ALOP") || leavetype.Equals("ALOP/")
                    )
                {

                }
                else
                {
                    if (hours < -24)
                    {
                        sErrMessage = "<span style='color:red;font-weight:bold;'>Sorry. Back dated</span><br>";
                        return false;
                    }
                    if (dblSelectedDays <= 3)
                    {
                        int iHours = 24;
                        //---- LOP shold be able to apply same day. Later, even back date allowed
                        //if (leavetype.Equals("LOP") || leavetype.Equals("/LOP") || leavetype.Equals("LOP/"))iHours = -24;

                        if (hours < iHours)
                        {
                            sErrMessage = "<b><span style='color:red;'>Leave for 3 days or less should be applied one day in advance.</span><br>";
                            if((lShiftStart == -1))
                            {
                                sErrMessage += "[Only <u>" + MyGlobal.GetDDHHMMSS(diff.TotalSeconds) + "</u> Hours to go for the next day]</b>";
                            }
                            else
                            {
                                sErrMessage += "[Only <u>" + MyGlobal.GetDDHHMMSS(diff.TotalSeconds) + "</u> Hours to go for the next shift]</b>";
                            }
                            if (sShiftMessage.Length > 0) sErrMessage += "<br>" + sShiftMessage;
                            return false;
                        }
                    }
                    else
                    {
                        if (hours < 240)    // 10 days
                        {
                            sErrMessage = "<b><span style='color:red;'>Leave above 3 days should be applied 10 days in advance.</span><br>";
                            if ((lShiftStart == -1))
                            {
                                sErrMessage += "[Only <u>" + MyGlobal.GetDDHHMMSS(diff.TotalSeconds) + "</u> Hours to go for the next day]</b>";
                            }
                            else
                            {
                                sErrMessage += "[Only <u>" + MyGlobal.GetDDHHMMSS(diff.TotalSeconds) + "</u> Hours to go for the next shift]</b>";
                            }
                            if (sShiftMessage.Length > 0) sErrMessage += "<br>" + sShiftMessage;
                            return false;
                        }
                    }
                }
                //-----------------------------------------------CL
                if (leavetype.Equals("CL") || leavetype.Equals("/CL") || leavetype.Equals("/CL"))
                {

                    if ((loadLeaveDataResponse.leaves.CL.sumDr + loadLeaveDataResponse.leaves.CL.used + loadLeaveDataResponse.leaves.CL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.CL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No CL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.CL.sumCr- loadLeaveDataResponse.leaves.CL.sumDr - loadLeaveDataResponse.leaves.CL.used - loadLeaveDataResponse.leaves.CL.pending) + " CLs]</b></span>";
                        
                        return false;
                    }
                    if (dblSelectedDays > 3) {
                        sErrMessage = "<b><span style='color:red;'>Sorry. Maximum 3 days in a stretch allowed</span><br>" +
                            "You have selected " + dblSelectedDays + " days</b>";
                        return false;
                    }
                }
                //-----------------------------------------------SL
                if (leavetype.Equals("SL") || leavetype.Equals("/SL") || leavetype.Equals("SL/"))
                {
                    if ((loadLeaveDataResponse.leaves.SL.sumDr + loadLeaveDataResponse.leaves.SL.used + loadLeaveDataResponse.leaves.SL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.SL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No SL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.SL.sumCr - loadLeaveDataResponse.leaves.SL.sumDr - loadLeaveDataResponse.leaves.SL.used - loadLeaveDataResponse.leaves.SL.pending) + " SLs]</b></span>";

                        return false;
                    }
                    sErrMessage = "";
                    if(AnyLeaveOrWeekOffClubed(con, profile, staffid, year, month, iSelectedDay,
                    dblSelectedDays, leavetype, ref sErrMessage))
                    {
                        return false;
                    }
                }
                //-----------------------------------------------PL
                if (leavetype.Equals("PL"))
                {
                    if ((loadLeaveDataResponse.leaves.PL.sumDr + loadLeaveDataResponse.leaves.PL.used + loadLeaveDataResponse.leaves.PL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.PL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No PL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.PL.sumCr - loadLeaveDataResponse.leaves.PL.sumDr - loadLeaveDataResponse.leaves.PL.used - loadLeaveDataResponse.leaves.PL.pending) + " PLs]</b></span>";

                        return false;
                    }
                    sErrMessage = "";
                    if (AnyLeaveOrWeekOffClubed(con, profile, staffid, year, month, iSelectedDay,
                    dblSelectedDays, leavetype, ref sErrMessage))
                    {
                        return false;
                    }
                }
                //-----------------------------------------------APL
                if (leavetype.Equals("APL"))
                {
                    if ((loadLeaveDataResponse.leaves.APL.sumDr + loadLeaveDataResponse.leaves.APL.used + loadLeaveDataResponse.leaves.APL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.APL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No APL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.APL.sumCr - loadLeaveDataResponse.leaves.APL.sumDr - loadLeaveDataResponse.leaves.APL.used - loadLeaveDataResponse.leaves.APL.pending) + " APLs]</b></span>";

                        return false;
                    }
                    sErrMessage = "";
                    if (AnyLeaveOrWeekOffClubed(con, profile, staffid, year, month, iSelectedDay,
                    dblSelectedDays, leavetype, ref sErrMessage))
                    {
                        return false;
                    }
                }
                //-----------------------------------------------LOP
                if (leavetype.Equals("LOP"))
                {
                    if ((loadLeaveDataResponse.leaves.LOP.sumDr + loadLeaveDataResponse.leaves.LOP.used + loadLeaveDataResponse.leaves.LOP.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.LOP.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No LOP available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.LOP.sumCr - loadLeaveDataResponse.leaves.LOP.sumDr - loadLeaveDataResponse.leaves.LOP.used - loadLeaveDataResponse.leaves.LOP.pending) + " LOPs]</b></span>";

                        return false;
                    }
                }
                //-----------------------------------------------ALOP
                if (leavetype.Equals("ALOP"))
                {
                    /*
                    if ((loadLeaveDataResponse.leaves.ALOP.pending + loadLeaveDataResponse.leaves.ALOP.used + dblSelectedDays) > loadLeaveDataResponse.leaves.ALOP.max)
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No ALOP available. [Max=" + loadLeaveDataResponse.leaves.ALOP.max + ", Used=" + loadLeaveDataResponse.leaves.ALOP.used + ", Pending=" + loadLeaveDataResponse.leaves.ALOP.pending + "]</b></span>";
                        return false;
                    }
                    */
                }
                //-----------------------------------------------MatL
                if (leavetype.Equals("MatL"))
                {
                    if ((loadLeaveDataResponse.leaves.MatL.sumDr + loadLeaveDataResponse.leaves.MatL.used + loadLeaveDataResponse.leaves.MatL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.MatL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No APL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.MatL.sumCr - loadLeaveDataResponse.leaves.MatL.sumDr - loadLeaveDataResponse.leaves.MatL.used - loadLeaveDataResponse.leaves.MatL.pending) + " MatLs]</b></span>";

                        return false;
                    }
                }
                //-----------------------------------------------PatL
                if (leavetype.Equals("PatL"))
                {
                    if ((loadLeaveDataResponse.leaves.PatL.sumDr + loadLeaveDataResponse.leaves.PatL.used + loadLeaveDataResponse.leaves.PatL.pending + dblSelectedDays) > (loadLeaveDataResponse.leaves.PatL.sumCr))
                    {
                        sErrMessage = "<span style='color:red;'><b>Sorry. No APL available. " +
                          "[You only have " + (loadLeaveDataResponse.leaves.PatL.sumCr - loadLeaveDataResponse.leaves.PatL.sumDr - loadLeaveDataResponse.leaves.PatL.used - loadLeaveDataResponse.leaves.PatL.pending) + " PatLs]</b></span>";

                        return false;
                    }
                }
                return true;

            }
            catch (MySqlException ex1)
            {
                return false;
            }
        }

    }
}