using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class EventsController : Controller
    {
        // GET: Events
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ProcessTraineeCLCredits(string profile,string code)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "None";

            if (!code.Equals("shiffin"))
            {
                postResponse.status = false;
                postResponse.result = "Who are you?";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            string sSQL = "";
            DateTime dtEnd = DateTime.Now;
            DateTime dtStart = dtEnd.AddMonths(-1);
            int iYear = dtStart.Year;
            int iMonth = dtStart.Month;
            Int32 unixMonthStart = (Int32)(dtStart.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Int32 unixMonthEnd = (Int32)(dtEnd.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int iCount = 0;
                    string sInsertQuery = "",sLeaveCreditQuery="",sMessage="";
                    sSQL = "select m_StaffID,m_Email from " + MyGlobal.activeDB + ".tbl_staffs where m_StaffID not in " +
                        "(select m_StaffID from " + MyGlobal.activeDB + ".tbl_events_summary where m_Year=" + iYear + " and m_Month=" + (iMonth - 1) + " and m_Profile = '" + profile + "') " +
                        "and m_Profile = '" + profile + "' and m_Status='Trainee'";
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
                                        string email = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        double dblSLCredits = GetWorkingDays(profile, reader.GetString(0), unixMonthStart, unixMonthEnd);
                                        sInsertQuery += "insert into " + MyGlobal.activeDB + ".tbl_events_summary " +
                                            "(m_Profile,m_StaffID,m_Year,m_Month,m_TraineeSLCredits) values " +
                                            "('" + profile + "','" + reader.GetString(0) + "','" + iYear + "','" + (iMonth - 1) + "','" + dblSLCredits + "');";
                                        int iCr = 0;
                                        string sDes = "",sLeaveMessage="";

                                        if (dblSLCredits >= 22)
                                        {
                                            iCr = 1;
                                            sDes = "Auto credit of 1 CL for 22 working days and one LOP for the calendar month of " + dtStart.ToString("MMM") + ", " + iYear;
                                            sLeaveMessage = "<span style=''color:blue;''><b>One CL and LOP is auto credited for your " + dblSLCredits + " days of attendance for the month of " + dtStart.ToString("MMM") + ", " + iYear + "</b></span>";
                                        }
                                        else
                                        {
                                            sDes = "No CL credit for the calendar month of " + dtStart.ToString("MMM") + ", " + iYear + " as your working days are less than 22 days [" + dblSLCredits + "]";
                                            sLeaveMessage = "<span style=''color:blue;''><b>Unable to credit CL into your account for the month of " + dtStart.ToString("MMM") + ". You had only "+dblSLCredits+" days of attendance";
                                        }
                                        //--------------------Leave credits
                                        sLeaveCreditQuery += "insert into " + MyGlobal.activeDB + ".tbl_leave " +
                                            "(m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Dr,m_Time,m_Description) values " +
                                            "('" + profile + "','" + iYear + "','" + reader.GetString(0) + "','CL','" + iCr + "','0',Now(),'" + sDes + "');";
                                        if (dblSLCredits > 1)
                                        {
                                            iCr = 1;
                                            sLeaveCreditQuery += "insert into " + MyGlobal.activeDB + ".tbl_leave " +
    "(m_Profile,m_Year,m_StaffID,m_Type,m_Cr,m_Dr,m_Time,m_Description) values " +
    "('" + profile + "','" + iYear + "','" + reader.GetString(0) + "','LOP','" + iCr + "','0',Now(),'" + sDes + "');";
                                        }
                                        //--------------------Message
                                        string session = reader.GetString(0) + "_" + iYear + "_" + iMonth + "_" + "1" + "_" + DateTime.Now.ToString("HHmmss");
                                        sMessage += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message,m_Time,m_Session) " +
                                            "values ('" + profile + "','" + "" + "','meterbox@chcgroup.in','" + email + "'," +
                                            "'" + sLeaveMessage + "',Now(),'" + session + "');";
                                        sMessage += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                                            "(m_Profile,m_Type,m_From,m_FromName,m_To,m_ToStaffID,m_Session,m_Time,m_TimeUpdated," +
                                            "m_Param1,m_Param2,m_Param3,m_Priority) values " +
                                            "('" + profile + "','2','meterbox@chcgroup.in','MeterBox','" + email + "'," +
                                            "'" + reader.GetString(0) + "','" + session + "',Now(),Now()," +
                                            "'1-" + iMonth + "-" + iYear + "','1','CL',1);";
                                        sMessage += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                        "values ('" + profile + "','" + session + "','" + email + "');";
                                        //--------------------------------------
                                        iCount++;
                                    }
                                }
                            }
                        }
                    }
                    //--------------------------
                    if (sInsertQuery.Length > 0)
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        { 
                            myCommand.CommandText = sInsertQuery + sLeaveCreditQuery+ sMessage;
                            myCommand.ExecuteNonQuery();
                            //--------------------------------------
                            myTrans.Commit();
                            postResponse.result = iCount + " new records created";
                            postResponse.status = true;
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                myTrans.Rollback();

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
                            postResponse.result ="Failed to create records. Transactions revoked";
                            MyGlobal.Error("ProcessTraineeSLCredits FAILED - " + e.GetType() + ", " + e.Message);
                        }
                        finally
                        {
                            //myConnection.Close();
                        }
                    }
                    else
                    {
                        postResponse.result = "No new records to create";
                    }
                    //---------------------------------------Check for CL credits

                    
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "ProcessTraineeSLCredits ERROR - "+ex.Message;

            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        private double GetWorkingDays(string profile, string staffid, Int32 unixMonthStart, Int32 unixMonthEnd)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "select count(worktime) as days from ( " +
                    "select FROM_UNIXTIME(`m_ActivityTime`, '%d.%m.%Y') as ndate,sum(m_WorkTime) as worktime " +
                    "from  " + MyGlobal.activeDB + ".tbl_accessmanager_activity " +
                    "where m_ActivityTime >= " + unixMonthStart + " and m_ActivityTime<" + unixMonthEnd + " " +
                    "and m_StaffID = '"+ staffid + "' and m_worktime> 0 and m_Profile='" + profile + "' " +
                    "group by ndate " +
                    ") x;";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                return reader.GetInt16(0);
                            }
                        }
                    }
                }
            }
            return 0;
        }
    }
}
