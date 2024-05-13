using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace MyHub.Controllers
{
    public partial class AccessmanagerController : Controller
    {
        public void ProcessWorkingHours(
            string profile, string roster, string shift,
            string year, string month, string staffid)
        {
            long lShiftStart = 0, lShiftEnd = 0;String sSQL = "";
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month) + 1;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
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
                                }
                            }
                        }
                    }
                    //----------------------------------------
                    TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                    double epochMonthStart = ((new TimeSpan(new DateTime(iYear, iMonth, 1).Ticks)) - epochTicks).TotalSeconds + 19800;
                    iMonth++;
                    if (iMonth > 12)
                    {
                        iYear++;
                        iMonth = 1;
                    }
                    sSQL = "DELETE from " + MyGlobal.activeDB + ".tbl_rosters_report where m_StaffID='" + staffid + "' or m_LastUpdate<(UNIX_TIMESTAMP()-300);";
                    using (MySqlCommand com = new MySqlCommand(sSQL, con))
                    {
                        com.ExecuteNonQuery();
                    }
                    double epochMonthEnd = ((new TimeSpan(new DateTime(iYear, iMonth, 1).Ticks)) - epochTicks).TotalSeconds + 19800;
                    String sMasterInsertSQL = "";
                    long[] arWorkhours = new long[32]; // 0 index not used
                    for (int i = 1; i < 32; i++) arWorkhours[i] = -1;
                    sSQL = @"select m_StaffID, from_unixtime(m_ActivityTime, '%d') from " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
"where (m_ActivityTime >= " + epochMonthStart + " and m_ActivityTime<" + epochMonthEnd + ") and (m_Activity = 'update' or m_Activity = 'lock' or m_Activity = 'forcedlock' or m_Activity = 'approved') " +
"and m_StaffID='" + staffid + "' " +
"group by from_unixtime(m_ActivityTime, '%d');";

                    String sStaffIDActive = "";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(1))
                                    {
                                        if (sStaffIDActive.Equals(reader.GetString(0)))
                                        {
                                            arWorkhours[reader.GetInt16(1)] = GetActiveWorkingTimeForThisStaffThisday(profile, roster, shift, year, month, reader.GetInt16(1), sStaffIDActive, lShiftStart, lShiftEnd);
                                        }
                                        else
                                        {
                                            if (sStaffIDActive.Length == 0)
                                            {
                                                sStaffIDActive = reader.GetString(0);
                                                for (int i = 1; i < 32; i++) arWorkhours[i] = -1;
                                                arWorkhours[reader.GetInt16(1)] = GetActiveWorkingTimeForThisStaffThisday(profile, roster, shift, year, month, reader.GetInt16(1), sStaffIDActive, lShiftStart, lShiftEnd);
                                            }
                                            else
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
                                                sMasterInsertSQL += "('" + profile + "','" + year + "','" + month + "','" + sStaffIDActive + "','" + roster + "','" + shift + "'";
                                                for (int i = 1; i < 32; i++)
                                                {
                                                    if (arWorkhours[i] != -1)
                                                    {
                                                        sMasterInsertSQL += ",'" + arWorkhours[i] + "'";
                                                    }
                                                }
                                                sMasterInsertSQL += ",UNIX_TIMESTAMP());";
                                                //-----------------------------------------
                                                for (int i = 1; i < 32; i++) arWorkhours[i] = -1;
                                                sStaffIDActive = reader.GetString(0);
                                                arWorkhours[reader.GetInt16(1)] = GetActiveWorkingTimeForThisStaffThisday(profile, roster, shift, year, month, reader.GetInt16(1), sStaffIDActive, lShiftStart, lShiftEnd);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (sStaffIDActive.Length > 0)
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
                        sMasterInsertSQL += "('" + profile + "','" + year + "','" + month + "','" + sStaffIDActive + "','" + roster + "','" + shift + "'";
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

            }
            catch (MySqlException ex)
            {
                //loadMessagesResponse.result = ex.Message;
            }
        }
        /*
         * Key will be CL like
         * leave may be CL or /CL or CL/
         */ 
        //string[] constLeaveCodesFULLOnly = { "CL", "SL", "PL", "APL", "LOP", "ALOP", "MatL", "PatL" };
        private bool IsThisLeaveType(string leave, string key)
        {
            if (leave.Equals(key)) return true;
            if (leave.Equals("/" + key)) return true;
            if (leave.Equals(key+"/")) return true;
            return false;
        }
    }
}