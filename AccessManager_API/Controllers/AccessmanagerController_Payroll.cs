using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using MyHub.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public static readonly string[] constArrayMonths =
            { "Jan", "Feb", "Mar", "Apr","May","Jun","Jly","Aug","Sep","Oct","Nov","Dec" };
        [HttpPost]
        public ActionResult ManagePayrolls(string profile, string selected, string mode,
            ManagePayrollsResponse obj)
        //string name, string parentledger, string parentpercentage, string action,string actionday)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var managePayrollsResponse = new ManagePayrollsResponse();
            managePayrollsResponse.status = false;
            managePayrollsResponse.result = "";
            managePayrollsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {

                        bool bDoesNewExists = false;
                        if (!selected.Equals(obj.selected)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payrolls where m_Profile='" + profile + "' and m_Name='" + obj.selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        managePayrollsResponse.result = "Payroll name '" + selected + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {

                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                myCommand.CommandText = "delete from " + MyGlobal.activeDB + ".tbl_payroll_ledgers " +
                                "where m_Profile='" + profile + "' and m_Payroll='" + selected + "';" +
                                "delete from " + MyGlobal.activeDB + ".tbl_payrolls " +
                                "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                                myCommand.ExecuteNonQuery();

                                myCommand.CommandText = "INSERT INTO " + MyGlobal.activeDB + ".tbl_payrolls " +
                                "(m_Profile,m_Name,m_ActionDay) values " +
                                "('" + profile + "','" + obj.selected + "','" + obj.iActionDay + "');";
                                foreach (PayrollLedger ledger in obj.ledgers)
                                {
                                    myCommand.CommandText += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payroll_ledgers " +
                                    "(m_Profile,m_Payroll,m_Ledger,m_LedgerParent," +
                                    "m_LedgerParentPercentage,m_CrDr,m_Amount) " +
                                    "values " +
                                    "('" + profile + "'," +
                                    "'" + obj.selected + "'," +
                                    "'" + ledger.m_Ledger + "'," +
                                    "'" + ledger.m_LedgerParent + "'," +
                                    "'" + ledger.m_LedgerParentPercentage + "'," +
                                    "'" + ledger.m_Action + "'," +
                                    "'" + ledger.m_Amount + "');";
                                }
                                myCommand.ExecuteNonQuery();
                                myTrans.Commit();
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
                            }
                            finally
                            {
                                //myConnection.Close();
                            }

                            selected = obj.selected;
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        selected = "new";
                        managePayrollsResponse.selected = "new";
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payrolls where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    managePayrollsResponse.result = "New already exists";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_payrolls (m_Profile,m_Name,m_ActionDay) values ('" + profile + "','new','0');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payroll_ledgers (m_Profile,m_Payroll,m_Ledger,m_CrDr,m_Amount) values " +
                                "('" + profile + "','new','Basic Pay','Cr','0');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                managePayrollsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payrolls where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            managePayrollsResponse.selected = "";
                            managePayrollsResponse.result = "Payroll deleted";
                            managePayrollsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    //sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payrolls where m_Profile='" + profile + "' order by m_Name;";
                    sSQL = "SELECT payrolls.m_Name,payrolls.m_ActionDay," +
                        "ledgers.m_Ledger,ledgers.m_LedgerParent," +
                        "ledgers.m_LedgerParentPercentage,ledgers.m_CrDr,ledgers.m_Amount " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payrolls payrolls " +
"left join " + MyGlobal.activeDB + ".tbl_payroll_ledgers ledgers on ledgers.m_Profile = payrolls.m_Profile and ledgers.m_Payroll = payrolls.m_Name " +
"where payrolls.m_Profile='" + profile + "'";

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
                                        if (!managePayrollsResponse.payrolls.Contains(reader[0].ToString()))
                                            managePayrollsResponse.payrolls.Add(reader[0].ToString());
                                        if (selected.Equals(reader[0].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            managePayrollsResponse.iActionDay = reader.GetInt16(1);
                                            managePayrollsResponse.selected = selected;
                                            PayrollLedger ledger = new PayrollLedger();
                                            ledger.m_Payroll = selected;
                                            if (!reader.IsDBNull(2)) ledger.m_Ledger = reader.GetString(2);
                                            if (!reader.IsDBNull(3)) ledger.m_LedgerParent = reader.GetString(3);
                                            if (!reader.IsDBNull(4)) ledger.m_LedgerParentPercentage = reader.GetDouble(4);
                                            if (!reader.IsDBNull(5)) ledger.m_Action = reader.GetString(5);
                                            if (!reader.IsDBNull(6)) ledger.m_Amount = reader.GetDouble(6);
                                            managePayrollsResponse.ledgers.Add(ledger);
                                        }
                                    }
                                }
                                managePayrollsResponse.status = true;
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                managePayrollsResponse.result = "Sorry!!! No Payrolls. Check 'Enable Edit' and 'Create New' to have one.";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!managePayrollsResponse.selected.Equals("new"))
                            managePayrollsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                managePayrollsResponse.result = ex.Message;
                MyGlobal.Error("MySqlException -> ManagePayrolls - +" + ex.Message);
            }

            return Json(managePayrollsResponse, JsonRequestBehavior.AllowGet);
        }
        /*
        private string GetGrossSalary(string profile, string payscale, Int32 key, string per_amount)
        {
            string sSQL = "";
            double dblGross = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                sSQL = "SELECT m_Ledger,m_BasedOn,m_Amount FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                "where m_Profile='" + profile + "' and m_Type='cr' and m_Name='" + payscale + "'  and m_Key='" + key + "' ";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                                {
                                    dblGross += MyGlobal.GetDouble(GetRateAmount(profile, payscale,key,
                                        reader.GetString(1),    // BasedOn
                                        reader.GetString(2)     // Amount
                                        ));
                                }
                            }
                        }
                    }
                }
            }
            return (dblGross * MyGlobal.GetDouble(per_amount.Replace("%", "")) / 100.0F).ToString();
        }
        */
        private double GetGrossSalary(string profile, string payscale, Int32 key,
            string per_amount,
            LoadPayslip loadPayslip, string year, string month, int iStartDate)
        {
            string sSQL = "";
            double dblGross = 0;
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                sSQL = "SELECT m_Ledger,m_BasedOn,m_Amount,m_PayMode FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                "where m_Profile='" + profile + "' and m_Type='cr' and m_Ledger!='CTC' " +
                "and m_Name='" + payscale + "'  and m_Key='" + key + "' ";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                                {
                                    double amt = MyGlobal.GetDouble(GetRateAmount(profile, payscale, key,
                                        reader.GetString(1),    // BasedOn
                                        reader.GetString(2)     // Amount
                                        ));

                                    int iPayMode = reader.IsDBNull(reader.GetOrdinal("m_PayMode")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                    if (iPayMode == 2)
                                    {
                                        double amtPerDay = amt / 26;
                                        dblGross += amtPerDay * loadPayslip.m_ActualWorkingDays;
                                    }
                                    else
                                    {
                                        //int daysInMonth = System.DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month));
                                        int daysInMonth = GetDaysForTheSalaryMonth(year, month, iStartDate); // 1 Index
                                        double amtPerDay = amt / daysInMonth;
                                        dblGross += amtPerDay * loadPayslip.m_DaysToBePaidTotal;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (dblGross * MyGlobal.GetDouble(per_amount.Replace("%", "")) / 100.0F);
        }
        /*
            month comes here as 1 index
            Calculate the days between previous month 26th & this month 25th
         */
        private int GetDaysForTheSalaryMonth(string year, string month, int iStartDate)
        {
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month);
            DateTime dtCurrent;
            try
            {
                dtCurrent = new DateTime(iYear, iMonth, iStartDate);
            }
            catch (ArgumentOutOfRangeException e)
            {
                MyGlobal.Error("GetDaysForTheSalary Month-CRITICAL ERROR-" + e.Message);
                return 0;
            }
            DateTime dtPrevious = dtCurrent.AddMonths(-1);
            int daysInMonth = System.DateTime.DaysInMonth(dtPrevious.Year, dtPrevious.Month);
            int iDaysPreviousMonth = daysInMonth - iStartDate;
            return iStartDate + iDaysPreviousMonth;

            /*
            int iYear = MyGlobal.GetInt16(year);
            int iMonth = MyGlobal.GetInt16(month);
            DateTime dtCurrent;
            try
            {
                dtCurrent = new DateTime(iYear, iMonth, 25);
            }
            catch (ArgumentOutOfRangeException e)
            {
                MyGlobal.Error("GetDaysForTheSalary Month-CRITICAL ERROR-" + e.Message);
                return 0;
            }
            DateTime dtPrevious = dtCurrent.AddMonths(-1);
            int daysInMonth = System.DateTime.DaysInMonth(dtPrevious.Year, dtPrevious.Month);
            int iDaysPreviousMonth = daysInMonth - 25;
            return 25 + iDaysPreviousMonth;
            */
        }

        //      What is the basedon ledger's per_amount percentage?
        private string GetRateAmount(string profile, string name, Int32 key, string basedon, string per_amount)
        {
            if (per_amount.IndexOf("%") == -1) return per_amount; // Not %
            //if (basedon.Equals("Gross Salary")) return GetGrossSalary(profile, name, key, per_amount);
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "select m_Amount,m_BasedOn from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                "where m_profile = '" + profile + "' " +
                "and m_Name='" + name + "' " +
                "and m_Key='" + key + "' " +
                "and m_Ledger='" + basedon + "'";
                //"and m_Type='" + crdr + "' " +
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
                                    string amt = reader.GetString(0);
                                    if (amt.IndexOf("%") > -1)
                                    {
                                        if (!reader.IsDBNull(1))
                                        {
                                            string amt_of_this = GetRateAmount(profile, name, key, reader.GetString(1), amt);
                                            return (MyGlobal.GetDouble(amt_of_this) * MyGlobal.GetDouble(per_amount.Replace("%", "")) / 100.0F).ToString();
                                        }
                                    }
                                    else
                                    {
                                        return (MyGlobal.GetDouble(amt) * MyGlobal.GetDouble(per_amount.Replace("%", "")) / 100.0F).ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return "0";
        }
        [HttpPost]
        public ActionResult LoadPayslip(string profile, string email, string staffid, string year, string month,
    string preview, string pagerequested, string attnstartdate, string menukey, Boolean releasebonus)
        {
            LoadPayslip loadPayslip = LoadPayslip_Obj(profile, email, staffid, year, month, preview, pagerequested, attnstartdate, menukey, releasebonus);
            return Json(loadPayslip, JsonRequestBehavior.AllowGet);
        }

        private LoadPayslip LoadPayslip_Obj(string profile, string email, string staffid, string year, string month,
            string preview, string pagerequested, string attnstartdate, string menukey, Boolean releasebonus)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            if (menukey == null) menukey = "";
            if (preview == null) preview = "";
            if (staffid == null) staffid = "";
            if (email == null) email = "";
            int iStartDate = MyGlobal.GetInt16(attnstartdate);
            if (iStartDate < 1 || iStartDate > 28) iStartDate = 1;
            var loadPayslip = new LoadPayslip();

            loadPayslip.status = false;
            loadPayslip.result = "";
            loadPayslip.profile = profile;
            loadPayslip.email = email;
            loadPayslip.staffid = staffid;
            loadPayslip.iYear = MyGlobal.GetInt16(year);
            loadPayslip.iMonth = MyGlobal.GetInt16(month);

            if (MyGlobal.GetDaysInThisMonth(loadPayslip.iYear, loadPayslip.iMonth) == 0)
            {
                loadPayslip.iMonth = DateTime.Now.Month; // 1=Jan
                loadPayslip.iYear = DateTime.Now.Year;
                int iDay = DateTime.Now.Day;
                if (iDay < iStartDate)
                {
                    DateTime tme = DateTime.Now.AddMonths(-1);
                    loadPayslip.iMonth = tme.Month; // 1=Jan
                    loadPayslip.iYear = tme.Year;
                }
            }
            loadPayslip.sMonth = constArrayMonths[loadPayslip.iMonth - 1];

            bool bPayslipExists = false;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //if (loadPayslip.staffid == null || loadPayslip.staffid.Length == 0)

                    sSQL = "SELECT m_Payscale,m_StaffID,m_FName," +
                    "m_Band,m_Grade,m_Team,m_Designation,m_EPF_UAN,m_AccountNo " +
                    "FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_profile = '" + profile + "' " +
                    "and (m_email = '" + email + "' or m_StaffID='" + staffid + "')";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    //loadPayslip.payscaleName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    loadPayslip.staffid = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    loadPayslip.name = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    loadPayslip.band = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                    loadPayslip.grade = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                    loadPayslip.team = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                    loadPayslip.designation = reader.IsDBNull(reader.GetOrdinal("m_Designation")) ? "" : reader.GetString(reader.GetOrdinal("m_Designation"));
                                    loadPayslip.epf_uan = reader.IsDBNull(reader.GetOrdinal("m_EPF_UAN")) ? "" : reader.GetString(reader.GetOrdinal("m_EPF_UAN"));
                                    loadPayslip.sb_acc = reader.IsDBNull(reader.GetOrdinal("m_AccountNo")) ? "" : reader.GetString(reader.GetOrdinal("m_AccountNo"));

                                    /*
                                    if (loadPayslip.payscaleName.Length == 0)
                                    {
                                        loadPayslip.payscaleName = loadPayslip.grade;
                                    }
                                    Int32 key = 0;
                                    Int32 startdate = 0;
                                    loadPayslip.m_PayscaleName = GetActivePayscale(profile, staffid, out key, out startdate);
                                    loadPayslip.m_PayscaleKey = key;
                                    loadPayslip.m_PayscaleStartDate = startdate;
                                    */
                                }
                            }
                        }
                    }
                    /*****************************************************************************************/
                    loadPayslip.Pages = 0;
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_profile = '" + profile + "' and m_StaffID = '" + loadPayslip.staffid + "' " +
                        "and m_Year='" + loadPayslip.iYear + "' and m_Month='" + (loadPayslip.iMonth - 1) + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    loadPayslip.Pages++;
                                    if (pagerequested.Equals(loadPayslip.Pages.ToString()))
                                    {
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_WorkingDays"))) loadPayslip.m_WorkingDays = reader.GetDouble(reader.GetOrdinal("m_WorkingDays"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_OFFs"))) loadPayslip.m_OFFs = reader.GetDouble(reader.GetOrdinal("m_OFFs"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Leaves"))) loadPayslip.m_Leaves = reader.GetDouble(reader.GetOrdinal("m_Leaves"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_ALOPs"))) loadPayslip.m_ALOPs = reader.GetDouble(reader.GetOrdinal("m_ALOPs"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_LOPs"))) loadPayslip.m_LOPs = reader.GetDouble(reader.GetOrdinal("m_LOPs"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_LateSeconds"))) loadPayslip.m_LateSeconds = reader.GetDouble(reader.GetOrdinal("m_LateSeconds"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_LopBasedOnDelay"))) loadPayslip.m_LopBasedOnDelay = reader.GetDouble(reader.GetOrdinal("m_LopBasedOnDelay"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_ActualWorkingDays"))) loadPayslip.m_ActualWorkingDays = reader.GetDouble(reader.GetOrdinal("m_ActualWorkingDays"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DaysToBePaidTotal"))) loadPayslip.m_DaysToBePaidTotal = reader.GetDouble(reader.GetOrdinal("m_DaysToBePaidTotal"));

                                        if (!reader.IsDBNull(reader.GetOrdinal("m_RosterOptions"))) loadPayslip.m_RosterOptions = reader.GetString(reader.GetOrdinal("m_RosterOptions"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_RosterOptionsResult"))) loadPayslip.m_RosterOptionsResult = reader.GetString(reader.GetOrdinal("m_RosterOptionsResult"));
                                        //if (!preview.Equals("1"))
                                        //{
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_PayscaleName"))) loadPayslip.m_PayscaleName = reader.GetString(reader.GetOrdinal("m_PayscaleName"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_PayscaleKey"))) loadPayslip.m_PayscaleKey = reader.GetInt32(reader.GetOrdinal("m_PayscaleKey"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_PayscaleStartDate"))) loadPayslip.m_PayscaleStartDate = reader.GetInt32(reader.GetOrdinal("m_PayscaleStartDate"));

                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) loadPayslip.name = reader.GetString(reader.GetOrdinal("m_Name"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) loadPayslip.band = reader.GetString(reader.GetOrdinal("m_Band"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) loadPayslip.grade = reader.GetString(reader.GetOrdinal("m_Grade"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) loadPayslip.designation = reader.GetString(reader.GetOrdinal("m_Designation"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_epf_uan"))) loadPayslip.epf_uan = reader.GetString(reader.GetOrdinal("m_epf_uan"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_sb_acc"))) loadPayslip.sb_acc = reader.GetString(reader.GetOrdinal("m_sb_acc"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_CTC"))) loadPayslip.CTC = reader.GetString(reader.GetOrdinal("m_CTC"));
                                        //}
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_MonthName"))) loadPayslip.sMonth = reader.GetString(reader.GetOrdinal("m_MonthName"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DateStart"))) loadPayslip.m_DateStart = reader.GetInt32(reader.GetOrdinal("m_DateStart"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DateEnd"))) loadPayslip.m_DateEnd = reader.GetInt32(reader.GetOrdinal("m_DateEnd"));

                                        if (!reader.IsDBNull(reader.GetOrdinal("m_CrTot"))) loadPayslip.m_CrTot = reader.GetDouble(reader.GetOrdinal("m_CrTot"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DrTot"))) loadPayslip.m_DrTot = reader.GetDouble(reader.GetOrdinal("m_DrTot"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_EarnsTot"))) loadPayslip.m_EarnsTot = reader.GetDouble(reader.GetOrdinal("m_EarnsTot"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DeductsTot"))) loadPayslip.m_DeductsTot = reader.GetDouble(reader.GetOrdinal("m_DeductsTot"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_VchNo"))) loadPayslip.m_VchNo = reader.GetInt32(reader.GetOrdinal("m_VchNo"));

                                        if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedBy"))) loadPayslip.m_CreatedBy = reader.GetString(reader.GetOrdinal("m_CreatedBy"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTime"))) loadPayslip.m_CreatedTime = reader.GetDateTime(reader.GetOrdinal("m_CreatedTime")).ToString("dd-MM-yyyy");
                                        loadPayslip.NetPayWords = GetAmountInWords((loadPayslip.m_EarnsTot - loadPayslip.m_DeductsTot).ToString());
                                        loadPayslip.PageNo = loadPayslip.Pages;
                                    }
                                    bPayslipExists = true;
                                    loadPayslip.original = 1;
                                }
                            }
                        }
                    }
                    bool bShowBonusLedgers =
                        menukey.IndexOf("a0-1") > -1 ||   // Admin
                        menukey.IndexOf("a0-2") > -1;//||
                                                     //menukey.IndexOf("h0-1") > -1 ||   // HR
                                                     //menukey.IndexOf("h0-2") > -1 ||
                                                     //menukey.IndexOf("b0-1") > -1 ||   // Accounts
                                                     //menukey.IndexOf("b0-2") > -1;
                                                     //menukey.IndexOf("p1-1") > -1 ||   // HR
                                                     //menukey.IndexOf("p1-2") > -1;
                                                     //---------------------------------------------------------------------------------------

                    if (loadPayslip.original == 1) // Get Payslip ledgers
                    {
                        sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payslips " +
                            "where m_profile = '" + profile + "' and m_StaffID = '" + loadPayslip.staffid + "' " +
                            "and m_Year='" + loadPayslip.iYear + "' and m_Month='" + (loadPayslip.iMonth - 1) + "' " +
                            "and m_VchNo='" + loadPayslip.m_VchNo + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        String sType = "";
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Type"))) sType = reader.GetString(reader.GetOrdinal("m_Type"));
                                        if (sType.Length > 0)
                                        {
                                            if (sType.Equals("cr"))
                                            {
                                                PayLedger led = new PayLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger"))) led.Name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) led.Amount = MyGlobal.GetDouble(reader.GetString(reader.GetOrdinal("m_Amount")));
                                                // New update in May 2021
                                                if ((led.Name.Equals("Annual Bonus") || led.Name.Equals("Gratuity"))
                                                    && loadPayslip.m_DateStart >= 1619395200
                                                    && !bShowBonusLedgers)
                                                {
                                                    loadPayslip.m_CrTot -= led.Amount;
                                                }
                                                else
                                                {
                                                    loadPayslip.ratesCr.Add(led);
                                                }

                                            }
                                            else
                                            if (sType.Equals("dr"))
                                            {
                                                PayLedger led = new PayLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger"))) led.Name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) led.Amount = MyGlobal.GetDouble(reader.GetString(reader.GetOrdinal("m_Amount")));

                                                loadPayslip.deductsDr.Add(led);
                                            }
                                            else
                                            if (sType.Equals("earn"))
                                            {
                                                PayLedger led = new PayLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger"))) led.Name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) led.Amount = MyGlobal.GetDouble(reader.GetString(reader.GetOrdinal("m_Amount")));
                                                // New update in May 2021
                                                if ((led.Name.Equals("Annual Bonus") || led.Name.Equals("Gratuity"))
                                                        && loadPayslip.m_DateStart >= 1619395200
                                                        && !bShowBonusLedgers)
                                                {
                                                    loadPayslip.m_EarnsTot -= led.Amount;
                                                }
                                                else
                                                {
                                                    loadPayslip.earns.Add(led);
                                                }
                                            }
                                            else
                                            if (sType.Equals("deduct"))
                                            {
                                                PayLedger led = new PayLedger();
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger"))) led.Name = reader.GetString(reader.GetOrdinal("m_Ledger"));
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) led.Amount = MyGlobal.GetDouble(reader.GetString(reader.GetOrdinal("m_Amount")));
                                                // New update in May 2021
                                                if ((led.Name.Equals("Bonus Accrued") || led.Name.Equals("Gratuity"))
                                                    && loadPayslip.m_DateStart >= 1619395200
                                                    && !bShowBonusLedgers)
                                                {
                                                    loadPayslip.m_DeductsTot -= led.Amount;
                                                }
                                                else
                                                {
                                                    loadPayslip.deducts.Add(led);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //---------------------Process Bonus release
                        /*
                        //  Added Oct,2021 after bonus release module
                        if (releasebonus && !preview.Equals("1"))
                        {
                            double amtThismonth = 0;
                            double amtPendingMonths = 0;
                            foreach (var payLedger in loadPayslip.deducts)
                            {
                                if (payLedger.Name.Equals("Bonus Accrued"))
                                {
                                    amtThismonth = payLedger.Amount;
                                }
                            }
                            //-------------Get pending Bonus to be release till this month
                            sSQL = "select m_Cr,m_Dr from " + MyGlobal.activeDB + ".tbl_accounts where " +
                            "m_Profile='" + profile + "' and m_Ledger='Bonus Accrued' and m_Head='" + loadPayslip.staffid + "' " +
                            "and (m_Year*12+m_Month)>=24249;";
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
                                                amtPendingMonths += reader.GetDouble(0) - reader.GetDouble(1);
                                            }
                                        }
                                    }
                                }
                            }

                            PayLedger led = new PayLedger();
                            led.Name = "Annual Bonus Credit";//1
                            led.Amount = amtPendingMonths + amtThismonth;
                            loadPayslip.earns.Add(led);
                            loadPayslip.m_EarnsTot += led.Amount;
                        }*/
                    }
                    /*****************************************************************************************/
                }
            }
            catch (MySqlException ex)
            {
                loadPayslip.result = ex.Message;
                MyGlobal.Error("LoadPayslip -> ManagePayrolls - +" + ex.Message);
            }
            catch (Exception ex)
            {
                loadPayslip.result = ex.Message;
                MyGlobal.Error("LoadPayslip -> ManagePayrolls - +" + ex.Message);
            }
            if (bPayslipExists)
            {
                //return Json(loadPayslip, JsonRequestBehavior.AllowGet);
                return loadPayslip;
            }
            else
            {
                if (preview.Equals("1"))
                {
                    double dblActiveMonthBonus = 0;
                    int iRound = 0;
                    //if (releasebonus)// && pagerequested.Equals("1"))
                    //{
                        while (true)
                        {
                            iRound++;
                            LoadPayslip loadPayslip1 =
                                this.GetPayslipFrom_Attendance_And_PayscaleMaster(profile, email, "",
                                loadPayslip.iYear.ToString(), (loadPayslip.iMonth).ToString(), iRound.ToString());
                            if (loadPayslip1.result.Length > 0 || iRound > 6)
                            {
                                break;
                            }
                            else
                            {
                            if (releasebonus)
                            {
                                foreach (var payLedger in loadPayslip1.deducts)
                                {
                                    if (payLedger.Name.Equals("Bonus Accrued"))
                                    {
                                        dblActiveMonthBonus += payLedger.Amount;
                                    }
                                }
                            }
                            }
                        }
                    //}
                    //else
                    //{
                      //  iRound = 2;// will be subtracted below
                    //}
                    
                    LoadPayslip loadPayslip2 = GetPayslipFrom_Attendance_And_PayscaleMaster(profile, email, "", loadPayslip.iYear.ToString(), (loadPayslip.iMonth ).ToString(), pagerequested);
                    loadPayslip2.Pages = iRound-1;
                    

                    if (releasebonus && pagerequested.Equals("1"))
                    {
                        double amtPendingMonths = 0;
                        /*
                        double amtThismonth = 0;
                        foreach (var payLedger in loadPayslip.deducts)
                        {
                            if (payLedger.Name.Equals("Bonus Accrued"))
                            {
                                amtThismonth = payLedger.Amount;
                            }
                        }
                        */
                        //-------------Get pending Bonus to be release till this month
                        using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                        {
                            con.Open();
                            string sSQL = "select sum(m_Cr),sum(m_Dr) from " + MyGlobal.activeDB + ".tbl_accounts where " +
                        "m_Profile='" + profile + "' and m_Ledger='Bonus Accrued' and m_Head='" + loadPayslip.staffid + "' " +
                        "and m_ReleaseVoucherarker is null " +
                        "and (m_Year*12+m_Month)>=24249;";
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
                                                amtPendingMonths = reader.GetDouble(0) - reader.GetDouble(1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        PayLedger led = new PayLedger();
                        led.Name = "Annual Bonus Credit";//2
                        led.Amount = amtPendingMonths + dblActiveMonthBonus;
                        loadPayslip2.earns.Add(led);
                        loadPayslip2.m_EarnsTot += led.Amount;
                    }
                    return loadPayslip2;

                }
                else
                {
                    //return Json(loadPayslip, JsonRequestBehavior.AllowGet);
                    return loadPayslip;
                }
            }
        }

        private LoadPayslip GetPayslipFrom_Attendance_And_PayscaleMaster(
            string profile, string email, string staffid, string year, string month, string pagerequested)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            if (pagerequested == null) pagerequested = "1";
            var loadPayslip = new LoadPayslip();
            loadPayslip.status = false;
            loadPayslip.result = "";
            loadPayslip.profile = profile;

            loadPayslip.email = "";
            loadPayslip.staffid = "";
            if (email != null) loadPayslip.email = email;
            if (staffid != null) loadPayslip.staffid = staffid;

            loadPayslip.original = 0;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-------------------------------------------------Get Attn Start Date
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
                    //-------------------------------------------------
                    loadPayslip.m_PayscaleName = "";
                    sSQL = "SELECT m_Payscale,m_StaffID,m_FName," +
                        "m_Band,m_Grade,m_Team,m_Designation,m_EPF_UAN,m_AccountNo,m_Bank,m_Branch FROM " +
                        "" + MyGlobal.activeDB + ".tbl_staffs " +
                        "where m_profile = '" + profile + "' ";

                    if (loadPayslip.email.Length > 0) sSQL += "and m_email = '" + email + "'";
                    else if (loadPayslip.staffid.Length > 0) sSQL += "and m_StaffID = '" + staffid + "'";

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
                                        //loadPayslip.payscaleName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        loadPayslip.staffid = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        loadPayslip.name = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                        loadPayslip.band = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                        loadPayslip.grade = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                        loadPayslip.team = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                        loadPayslip.designation = reader.IsDBNull(reader.GetOrdinal("m_Designation")) ? "" : reader.GetString(reader.GetOrdinal("m_Designation"));
                                        loadPayslip.epf_uan = reader.IsDBNull(reader.GetOrdinal("m_EPF_UAN")) ? "" : reader.GetString(reader.GetOrdinal("m_EPF_UAN"));
                                        loadPayslip.sb_acc = reader.IsDBNull(reader.GetOrdinal("m_AccountNo")) ? "" : reader.GetString(reader.GetOrdinal("m_AccountNo"));
                                        loadPayslip.m_Bank = reader.IsDBNull(reader.GetOrdinal("m_Bank")) ? "" : reader.GetString(reader.GetOrdinal("m_Bank"));
                                        loadPayslip.m_Branch = reader.IsDBNull(reader.GetOrdinal("m_Branch")) ? "" : reader.GetString(reader.GetOrdinal("m_Branch"));
                                        /*
                                        Int32 key = 0;
                                        Int32 startdate = 0;
                                        loadPayslip.m_PayscaleName = GetActivePayscale(profile, loadPayslip.staffid, out key, out startdate);
                                        loadPayslip.m_PayscaleKey = key;
                                        loadPayslip.m_PayscaleStartDate = startdate;
                                        */
                                    }

                                }
                            }
                        }
                    }
                    //if (loadPayslip.m_PayscaleName.Length == 0)
                    //{
                    //    loadPayslip.result = "Payscale not linked with staff";
                    //}
                    //else
                    //{
                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;

                    DateTime dtCurrent;
                    try
                    {
                        dtCurrent = new DateTime(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month), 1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        dtCurrent = DateTime.Now;
                    }
                    DateTime dtPrevious = dtCurrent.AddMonths(-1);

                    loadPayslip.iYear = dtCurrent.Year;
                    loadPayslip.iMonth = dtCurrent.Month;
                    loadPayslip.sMonth = constArrayMonths[dtCurrent.Month - 1];
                    loadPayslip.Pages = 0;
                    //sSQL = "select m_WorkingDays,m_LOP,m_DateStart,m_DateEnd " +
                    sSQL = "select * " +
                        "from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + loadPayslip.staffid + "' " +
                        "and m_Year='" + dtCurrent.Year + "' and m_Month='" + (dtCurrent.Month - 1) + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    loadPayslip.Pages++;
                                    if (pagerequested.Equals(loadPayslip.Pages.ToString()))
                                    {
                                        loadPayslip.m_WorkingDays = reader.IsDBNull(reader.GetOrdinal("m_WorkingDays")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_WorkingDays"));
                                        loadPayslip.m_OFFs = reader.IsDBNull(reader.GetOrdinal("m_OFFs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_OFFs"));
                                        loadPayslip.m_Leaves = reader.IsDBNull(reader.GetOrdinal("m_Leaves")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_Leaves"));
                                        loadPayslip.m_ALOPs = reader.IsDBNull(reader.GetOrdinal("m_ALOPs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_ALOPs"));
                                        loadPayslip.m_LOPs = reader.IsDBNull(reader.GetOrdinal("m_LOPs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LOPs"));
                                        loadPayslip.m_LateSeconds = reader.IsDBNull(reader.GetOrdinal("m_LateSeconds")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LateSeconds"));
                                        loadPayslip.m_LopBasedOnDelay = reader.IsDBNull(reader.GetOrdinal("m_LopBasedOnDelay")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LopBasedOnDelay"));
                                        loadPayslip.m_ActualWorkingDays = reader.IsDBNull(reader.GetOrdinal("m_ActualWorkingDays")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_ActualWorkingDays"));
                                        loadPayslip.m_DaysToBePaidTotal = reader.IsDBNull(reader.GetOrdinal("m_DaysToBePaidTotal")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_DaysToBePaidTotal"));

                                        loadPayslip.m_RosterOptions = reader.IsDBNull(reader.GetOrdinal("m_RosterOptions")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterOptions"));
                                        loadPayslip.m_RosterOptionsResult = reader.IsDBNull(reader.GetOrdinal("m_RosterOptionsResult")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterOptionsResult"));

                                        loadPayslip.m_DateStart = reader.IsDBNull(reader.GetOrdinal("m_DateStart")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_DateStart"));
                                        loadPayslip.m_DateEnd = reader.IsDBNull(reader.GetOrdinal("m_DateEnd")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_DateEnd"));


                                        loadPayslip.m_PayscaleName = MyGlobal.GetPureString(reader, "pay_scale"); //GetActivePayscale(profile, loadPayslip.staffid, out key, out startdate);
                                        loadPayslip.m_PayscaleKey = MyGlobal.GetPureInt32(reader, "pay_key"); //key;
                                        loadPayslip.m_PayscaleStartDate = MyGlobal.GetPureInt32(reader, "pay_startdate"); //startdate;
                                        loadPayslip.PageNo = loadPayslip.Pages;
                                    }
                                }
                                /*
                                if (reader.Read())
                                {
                                    loadPayslip.Pages++;
                                    if (pagerequested.Equals("2"))
                                    {
                                        loadPayslip.m_WorkingDays = reader.IsDBNull(reader.GetOrdinal("m_WorkingDays")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_WorkingDays"));
                                        loadPayslip.m_OFFs = reader.IsDBNull(reader.GetOrdinal("m_OFFs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_OFFs"));
                                        loadPayslip.m_Leaves = reader.IsDBNull(reader.GetOrdinal("m_Leaves")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_Leaves"));
                                        loadPayslip.m_ALOPs = reader.IsDBNull(reader.GetOrdinal("m_ALOPs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_ALOPs"));
                                        loadPayslip.m_LOPs = reader.IsDBNull(reader.GetOrdinal("m_LOPs")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LOPs"));
                                        loadPayslip.m_LateSeconds = reader.IsDBNull(reader.GetOrdinal("m_LateSeconds")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LateSeconds"));
                                        loadPayslip.m_LopBasedOnDelay = reader.IsDBNull(reader.GetOrdinal("m_LopBasedOnDelay")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_LopBasedOnDelay"));
                                        loadPayslip.m_ActualWorkingDays = reader.IsDBNull(reader.GetOrdinal("m_ActualWorkingDays")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_ActualWorkingDays"));
                                        loadPayslip.m_DaysToBePaidTotal = reader.IsDBNull(reader.GetOrdinal("m_DaysToBePaidTotal")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_DaysToBePaidTotal"));

                                        loadPayslip.m_RosterOptions = reader.IsDBNull(reader.GetOrdinal("m_RosterOptions")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterOptions"));
                                        loadPayslip.m_RosterOptionsResult = reader.IsDBNull(reader.GetOrdinal("m_RosterOptionsResult")) ? "" : reader.GetString(reader.GetOrdinal("m_RosterOptionsResult"));

                                        loadPayslip.m_DateStart = reader.IsDBNull(reader.GetOrdinal("m_DateStart")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_DateStart"));
                                        loadPayslip.m_DateEnd = reader.IsDBNull(reader.GetOrdinal("m_DateEnd")) ? 0 : reader.GetInt32(reader.GetOrdinal("m_DateEnd"));


                                        loadPayslip.m_PayscaleName = MyGlobal.GetPureString(reader, "pay_scale"); //GetActivePayscale(profile, loadPayslip.staffid, out key, out startdate);
                                        loadPayslip.m_PayscaleKey = MyGlobal.GetPureInt32(reader, "pay_key"); //key;
                                        loadPayslip.m_PayscaleStartDate = MyGlobal.GetPureInt32(reader, "pay_startdate"); //startdate;
                                        loadPayslip.PageNo = 2;
                                    }
                                }
                                */
                            }
                        }
                    }
                    if (loadPayslip.m_WorkingDays == 0)
                    {
                        con.Close();
                        loadPayslip.result = "No working days";
                        return loadPayslip;
                    }
                    if (loadPayslip.m_PayscaleName.Length == 0 || loadPayslip.m_PayscaleKey == 0)
                    {
                        con.Close();
                        loadPayslip.result = "No valid Payscale [" + loadPayslip.m_PayscaleName + "][" + loadPayslip.m_PayscaleKey + "]";
                        return loadPayslip;
                    }
                    //--------------------------------------------------
                    loadPayslip.m_GrossSalary = GetGrossSalary(profile,
                        loadPayslip.m_PayscaleName,
                        loadPayslip.m_PayscaleKey,
                        "100%", // Amount in %
                        loadPayslip,
                        year, month, iStartDate
                        );

                    //--------------------------------------------------
                    sSQL = "select m_Ledger,m_Type,m_Amount,m_Basedon,m_PayMode " +
                        "from " + MyGlobal.activeDB + ".tbl_payscale_master payslip " +
                        "where payslip.m_Name='" + loadPayslip.m_PayscaleName + "' " +
                        "and payslip.m_Key = '" + loadPayslip.m_PayscaleKey + "' " +
                        "and payslip.m_profile = '" + profile + "' " +
                        "order by m_Order;";

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

                                        if (reader.GetString(1).Equals("cr") || reader.GetString(1).Equals("cro"))
                                        {
                                            if (!reader.IsDBNull(0))
                                            {
                                                if (reader.GetString(0).Equals("CTC"))
                                                {
                                                    loadPayslip.CTC = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                                }
                                                else
                                                {
                                                    PayLedger led = new PayLedger();
                                                    led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                                    led.Amount = MyGlobal.GetDouble(
                                                        GetRateAmount(profile, loadPayslip.m_PayscaleName,
                                                        loadPayslip.m_PayscaleKey,
                                                            reader.IsDBNull(3) ? "" : reader.GetString(3),  //m_Basedon
                                                            reader.IsDBNull(2) ? "" : reader.GetString(2)   //m_Amount
                                                        )
                                                    );
                                                    led.Amount = Math.Round(led.Amount, 2);
                                                    loadPayslip.ratesCr.Add(led);
                                                    loadPayslip.m_CrTot += led.Amount;
                                                }
                                            }
                                        }
                                        else if (reader.GetString(1).Equals("dr"))
                                        {   // I think this is unused in payslip
                                            PayLedger led = new PayLedger();
                                            led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            led.Amount = MyGlobal.GetDouble(GetRateAmount(profile,
                                                loadPayslip.m_PayscaleName, loadPayslip.m_PayscaleKey,
                                                reader.IsDBNull(3) ? "" : reader.GetString(3),  //m_Basedon
                                                reader.IsDBNull(2) ? "" : reader.GetString(2)   //m_Amount
                                                ));
                                            led.Amount = Math.Round(led.Amount, 2);
                                            loadPayslip.deductsDr.Add(led);
                                            loadPayslip.m_DrTot += led.Amount;
                                        }
                                        else if (reader.GetString(1).Equals("earn"))
                                        {
                                            PayLedger led = new PayLedger();
                                            led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            string basedon = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                            double amt = 0;
                                            if (basedon.Equals("Gross Salary"))
                                            {
                                                led.Amount = GetGrossSalary(profile,
                                                    loadPayslip.m_PayscaleName,
                                                    loadPayslip.m_PayscaleKey,
                                                    reader.IsDBNull(2) ? "" : reader.GetString(2), // Amount in %
                                                    loadPayslip,
                                                    year, month, iStartDate
                                                    );
                                            }
                                            else
                                            {
                                                amt = MyGlobal.GetDouble(
                                                    GetRateAmount(
                                                        profile,
                                                        loadPayslip.m_PayscaleName, loadPayslip.m_PayscaleKey,
                                                        basedon,  //m_Basedon
                                                        reader.IsDBNull(2) ? "" : reader.GetString(2)   //m_Amount
                                                    )
                                                );

                                                int iPayMode = reader.IsDBNull(reader.GetOrdinal("m_PayMode")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                if (iPayMode == 2) // Paid only on physically present days or not
                                                {
                                                    double amtPerDay = amt / 26;
                                                    led.Amount = amtPerDay * loadPayslip.m_ActualWorkingDays;
                                                }
                                                else
                                                {
                                                    //int daysInMonth = System.DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month));
                                                    int daysInMonth = GetDaysForTheSalaryMonth(year, month, iStartDate); // 1 Index
                                                    double amtPerDay = amt / daysInMonth;
                                                    led.Amount = amtPerDay * loadPayslip.m_DaysToBePaidTotal;
                                                }
                                            }
                                            led.Amount = Math.Round(led.Amount, 2);
                                            loadPayslip.earns.Add(led);
                                            loadPayslip.m_EarnsTot += led.Amount;

                                            //-------Store Basic,PF for later use while taking report
                                            if (led.Name.Equals("Basic", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                loadPayslip.m_BasicPay = led.Amount;
                                            }
                                            /*
                                            double amtPerDay = amt / loadPayslip.m_WorkingDays;
                                            led.Amount = (amtPerDay * loadPayslip.m_WorkingDays) -
                                                (amtPerDay * loadPayslip.dblLOP);
                                            loadPayslip.earns.Add(led);
                                            loadPayslip.m_EarnsTot += led.Amount;
                                            */
                                        }
                                        else if (reader.GetString(1).Equals("deduct"))
                                        {
                                            PayLedger led = new PayLedger();
                                            led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            string basedon = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                            double amt = 0;

                                            if (basedon.Equals("Gross Salary"))
                                            {
                                                led.Amount = GetGrossSalary(profile,
                                                    loadPayslip.m_PayscaleName,
                                                    loadPayslip.m_PayscaleKey,
                                                    reader.IsDBNull(2) ? "" : reader.GetString(2), // Amount in %
                                                    loadPayslip,
                                                    year, month, iStartDate
                                                    );
                                            }
                                            else if (basedon.Equals("Auto (custom)"))
                                            {
                                                if (loadPayslip.m_GrossSalary > 75000) led.Amount = Math.Round(1250.00 / 6, 2);
                                                else if (loadPayslip.m_GrossSalary > 60000) led.Amount = Math.Round(1025.00 / 6, 2);
                                                else if (loadPayslip.m_GrossSalary > 45000) led.Amount = Math.Round(690.00 / 6, 2);
                                                else if (loadPayslip.m_GrossSalary > 30000) led.Amount = Math.Round(315.00 / 6, 2);
                                                //else if (loadPayslip.m_GrossSalary > 21000) led.Amount = Math.Round(135.00 / 6, 2);
                                                else if (loadPayslip.m_GrossSalary > 15000) led.Amount = Math.Round(135.00 / 6, 2); // test
                                                else led.Amount = 0;
                                            }
                                            else
                                            {
                                                amt = MyGlobal.GetDouble(GetRateAmount(profile,
                                                    loadPayslip.m_PayscaleName, loadPayslip.m_PayscaleKey,
                                                reader.IsDBNull(3) ? "" : reader.GetString(3),  //m_Basedon
                                                reader.IsDBNull(2) ? "" : reader.GetString(2)   //m_Amount
                                                ));


                                                int iPayMode = reader.IsDBNull(reader.GetOrdinal("m_PayMode")) ? 0 : reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                                if (iPayMode == 2)
                                                {
                                                    double amtPerDay = amt / 26;
                                                    led.Amount = amtPerDay * loadPayslip.m_ActualWorkingDays;
                                                }
                                                else
                                                {
                                                    //int daysInMonth = System.DateTime.DaysInMonth(MyGlobal.GetInt16(year), MyGlobal.GetInt16(month));
                                                    int daysInMonth = GetDaysForTheSalaryMonth(year, month, iStartDate); // 1 Index
                                                    double amtPerDay = amt / daysInMonth;
                                                    led.Amount = amtPerDay * loadPayslip.m_DaysToBePaidTotal;
                                                }
                                            }
                                            led.Amount = Math.Round(led.Amount, 2);
                                            loadPayslip.deducts.Add(led);
                                            loadPayslip.m_DeductsTot += led.Amount;
                                            //-------Store Basic,PF for later use while taking report
                                            if (led.Name.Equals("PF", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                loadPayslip.m_EPFContributionRemitted = led.Amount;
                                            }
                                            if (led.Name.Equals("ESIC", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                loadPayslip.m_ESIC = led.Amount;
                                            }
                                            if (led.Name.Equals("Professional Tax", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                loadPayslip.m_ProfessionalTax = led.Amount;
                                            }
                                            /*
                                            double amtPerDay = amt / loadPayslip.m_WorkingDays;
                                            led.Amount = (amtPerDay * loadPayslip.m_WorkingDays) -
                                                (amtPerDay * loadPayslip.dblLOP);
                                            loadPayslip.deducts.Add(led);
                                            loadPayslip.m_DeductsTot += led.Amount;
                                            */
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //---------------Load ledgers from additional ledgers
                    if (pagerequested.Equals("1"))
                    {
                        sSQL = "select m_Ledger,m_Type,m_Amount,m_Security from " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                            "where m_Profile='" + profile + "' and m_StaffID='" + loadPayslip.staffid + "' " +
                            "and m_Year='" + dtCurrent.Year + "' and m_Month='" + (dtCurrent.Month - 1) + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.GetString(1).Equals("cr"))
                                        {
                                            PayLedger led = new PayLedger();
                                            led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            led.Amount = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                            led.m_Security = reader.IsDBNull(3) ? 0 : reader.GetInt16(3);
                                            loadPayslip.earns.Add(led);
                                            loadPayslip.m_EarnsTot += led.Amount;
                                        }
                                        else if (reader.GetString(1).Equals("dr"))
                                        {
                                            PayLedger led = new PayLedger();
                                            led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                            led.Amount = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                            led.m_Security = reader.IsDBNull(3) ? 0 : reader.GetInt16(3);
                                            loadPayslip.deducts.Add(led);
                                            loadPayslip.m_DeductsTot += led.Amount;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //---------------Load ledgers from additional ledgers END
                    //}
                    //------------------------------
                    //loadPayslip.m_DeductsTot = Math.Round(loadPayslip.m_DeductsTot, 2);
                    //loadPayslip.m_EarnsTot = Math.Round(loadPayslip.m_EarnsTot, 2);

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                loadPayslip.result = ex.Message;
                MyGlobal.Error("MySqlException -> ManagePayrolls - +" + ex.Message);
            }
            return loadPayslip;
            //return Json(loadPayslip, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------Attendance
        //  MarkerOption : PayIndex(0,0.5,1,2) : PhysicalPresense(0-Not required, 1- Required)
        //  ACO:1:0,OFF:1,0,HP:2:1
        // If no conditions satisfied, returns 1
        private int PhysicalPresenseNeeded(string sRosterOptions, string sMarkRoster, out double dblPayIndex)
        {
            dblPayIndex = 0;
            int iPos1 = sRosterOptions.IndexOf(sMarkRoster);
            if (iPos1 > -1)
            {
                int iPos2 = sRosterOptions.IndexOf(',', iPos1);
                if (iPos2 > -1)
                {
                    char[] delimiterChars = { ':' };
                    string[] arData = sRosterOptions.Substring(iPos1, iPos2 - iPos1).Split(delimiterChars);
                    if (arData.Length == 3)
                    {
                        //HP:2:1
                        dblPayIndex = MyGlobal.GetDouble(arData[1]);
                        return MyGlobal.GetInt16(arData[2]);
                    }
                }
            }
            return 1;
        }
        /*
        //  MarkerOption : PayIndex(0,0.5,1,2) : PhysicalPresense(0-Not required, 1- Required)
        //  ACO:1:0,OFF:1,0,HP:2:1
        private double GetPaylevelOfThisMarker(string sRosterOptions, string sMarkRoster)
        {
            int iPos1 = sRosterOptions.IndexOf(sMarkRoster);
            if (iPos1 > -1)
            {
                int iPos2 = sRosterOptions.IndexOf(',', iPos1);
                if (iPos2 > -1)
                {
                    char[] delimiterChars = { ':' };
                    string[] arData = sRosterOptions.Substring(iPos1, iPos2 - iPos1).Split(delimiterChars);
                    if (arData.Length == 3)
                    {
                        //HP:2:1
                        return MyGlobal.GetDouble(arData[2]);
                    }
                }
            }
            return 1;
        }
        */

        //[HttpPost]
        public ActionResult HRProductionResponse(string profile, string staffid, string year, string month, string consolidated, string team)
        {
            if (consolidated.Equals("true"))
                return HRProductionResponse_consolidated(profile, staffid, year, month, team);
            return HRProductionResponse_individual(profile, staffid, year, month, team);


        }

        public ActionResult HRProductionResponse_consolidated(
    string profile, string staffid, string year, string month, string team)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var hrProductionResponse = new HRProductionResponse();
            hrProductionResponse.status = false;
            hrProductionResponse.result = "";
            hrProductionResponse.m_StaffID = staffid;
            hrProductionResponse.m_Name = "";

            int iMonth = MyGlobal.GetInt16(month) - 1;
            hrProductionResponse.monthStr = constArrayMonths[iMonth] + ", " + year;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    sSQL = "select m_StaffID,m_Process,m_Name,sum(m_Target) as target,sum(m_Achived) as achived,sum(m_QASamples) as samples," +
                        "sum(m_QAError) as error,sum(m_QAScore) as score " +
                        "from " + MyGlobal.activeDB + ".tbl_production " +
                        "where m_Profile='" + profile + "' " +
                        "and m_Year='" + year + "' and m_Month='" + iMonth + "' ";
                    if (team.Length > 0) sSQL += "and m_Process='" + team + "' ";
                    sSQL += "group by m_StaffID,m_Process order by m_StaffID;";

                    hrProductionResponse.monthTarget = 0;
                    hrProductionResponse.monthAchived = 0;
                    hrProductionResponse.monthSamples = 0;
                    hrProductionResponse.monthError = 0;
                    hrProductionResponse.monthScore = 0;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (hrProductionResponse.m_Name.Length == 0) hrProductionResponse.m_Name = MyGlobal.GetPureString(reader, "m_Name");
                                    HRProductionRow row = new HRProductionRow();
                                    row.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.m_Name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.m_Process = MyGlobal.GetPureString(reader, "m_Process");

                                    row.m_Target = MyGlobal.GetPureInt16(reader, "target");
                                    row.m_Achived = MyGlobal.GetPureInt16(reader, "achived");
                                    //row.m_Samples = readerInt16(reader, "samples");
                                    //row.m_Error = readerInt16(reader, "error");
                                    //row.m_Score = readerInt16(reader, "score");
                                    row.m_Year = year;
                                    row.m_Month = month;
                                    GetScores(profile, true, row);
                                    /*
                                    row.m_Date = MyGlobal.Right("00" + readerString(reader, "m_Day"), 2) + "-" + MyGlobal.Right("00" + month, 2) + "-" + year;
                                    row.m_DOJ = "";
                                    row.m_Process = readerString(reader, "m_Process");
                                    row.m_Target = readerInt16(reader, "m_Target");
                                    row.m_Achived = readerInt16(reader, "m_Achived");
                                    row.m_Samples = readerInt16(reader, "m_QASamples");
                                    row.m_Error = readerInt16(reader, "m_QAError");
                                    row.m_Score = readerInt16(reader, "m_QAScore");
                                    */
                                    hrProductionResponse.rows.Add(row);
                                    hrProductionResponse.monthTarget += row.m_Target;
                                    hrProductionResponse.monthAchived += row.m_Achived;
                                    hrProductionResponse.monthSamples += row.m_Samples;
                                    hrProductionResponse.monthError += row.m_Error;
                                    hrProductionResponse.monthScore += row.m_Score;

                                }
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("HRProductionResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("HRProductionResponse-Exception->" + ex.Message);
            }

            return Json(hrProductionResponse, JsonRequestBehavior.AllowGet);
        }

        public ActionResult HRProductionResponse_individual(
            string profile, string staffid, string year, string month, string team)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var hrProductionResponse = new HRProductionResponse();
            hrProductionResponse.status = false;
            hrProductionResponse.result = "";
            hrProductionResponse.m_StaffID = staffid;
            hrProductionResponse.m_Name = "";

            int iMonth = MyGlobal.GetInt16(month) - 1;
            hrProductionResponse.monthStr = constArrayMonths[iMonth] + ", " + year;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_production " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + year + "' and m_Month='" + iMonth + "' order by m_Day desc;";
                    hrProductionResponse.monthTarget = 0;
                    hrProductionResponse.monthAchived = 0;
                    hrProductionResponse.monthSamples = 0;
                    hrProductionResponse.monthError = 0;
                    hrProductionResponse.monthScore = 0;
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (hrProductionResponse.m_Name.Length == 0) hrProductionResponse.m_Name = MyGlobal.GetPureString(reader, "m_Name");
                                    HRProductionRow row = new HRProductionRow();
                                    row.m_Date = MyGlobal.Right("00" + MyGlobal.GetPureString(reader, "m_Day"), 2) + "-" + MyGlobal.Right("00" + month, 2) + "-" + year;
                                    row.m_Year = year;
                                    row.m_Month = month;
                                    row.m_Day = MyGlobal.GetPureString(reader, "m_Day");
                                    row.m_DOJ = "";
                                    row.m_StaffID = staffid;
                                    row.m_Process = MyGlobal.GetPureString(reader, "m_Process");
                                    row.m_Target = MyGlobal.GetPureInt16(reader, "m_Target");
                                    row.m_Achived = MyGlobal.GetPureInt16(reader, "m_Achived");
                                    row.m_Confirmed = MyGlobal.GetPureInt16(reader, "m_Confirmed");
                                    row.m_ConfirmedLoaded = MyGlobal.GetPureInt16(reader, "m_Confirmed");

                                    row.m_ConfirmedBy = MyGlobal.GetPureString(reader, "m_ConfirmedBy");
                                    row.m_ConfirmedTime = MyGlobal.GetPureString(reader, "m_ConfirmedTime");

                                    //row.m_Samples = readerInt16(reader, "m_QASamples");
                                    //row.m_Error = readerInt16(reader, "m_QAError");
                                    //row.m_Score = readerInt16(reader, "m_QAScore");

                                    GetScores(profile, false, row);

                                    hrProductionResponse.rows.Add(row);
                                    hrProductionResponse.monthTarget += row.m_Target;
                                    hrProductionResponse.monthAchived += row.m_Achived;
                                    hrProductionResponse.monthSamples += row.m_Samples;
                                    hrProductionResponse.monthError += row.m_Error;
                                    hrProductionResponse.monthScore += row.m_Score;

                                }
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("HRProductionResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("HRProductionResponse-Exception->" + ex.Message);
            }

            return Json(hrProductionResponse, JsonRequestBehavior.AllowGet);
        }
        private void GetScores(string profile, bool consolidated, HRProductionRow row)
        {
            row.m_Error = 0;
            row.m_Samples = 0;
            row.m_Score = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "SELECT " +
    "sum(Case When m_QAFreeze is null Then 0 Else 1 End) as samples," +
    "sum(Case When m_QAScore > 0 Then 1 Else 0 End) as errors," +
    "sum(Case When m_QAScore > 0 Then m_QAScore Else 0 End) as score " +
    "FROM " + MyGlobal.activeDB + ".tbl_production_qatable " +
    "where m_Profile = '" + profile + "' and m_StaffID = '" + row.m_StaffID + "' " +
    "and m_Year = '" + row.m_Year + "' and m_Month = '" + (MyGlobal.GetInt16(row.m_Month) - 1) + "' ";
                    if (consolidated)
                    {
                        sSQL += "and m_Process = '" + row.m_Process + "';";
                    }
                    else
                    {
                        sSQL += "and m_Day = '" + row.m_Day + "';";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    row.m_Samples = reader.IsDBNull(0) ? 0 : reader.GetInt16(0);
                                    row.m_Error = reader.IsDBNull(1) ? 0 : reader.GetInt16(1);
                                    row.m_Score = reader.IsDBNull(2) ? 0 : reader.GetInt16(2);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("HRProductionResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("HRProductionResponse-Exception->" + ex.Message);
            }

        }
        //-------------------------------------------------------------1234 pm
        private bool formatQATime(string str, out string qaTime, out string err)
        {
            qaTime = "";
            err = "";
            if (str.Length == 6)    //1245AM
            {
                qaTime = str.Substring(0, 2) + ":" + str.Substring(2, 2) + " " + str.Substring(4).ToUpper();
                return true;
            }
            else if (str.Length == 7)
            {
                if (str.Substring(2, 1).Equals(" ")) //12 45AM
                {
                    qaTime = str.Substring(0, 2) + ":" + str.Substring(3, 2) + " " + str.Substring(5).ToUpper();

                }
                else //1245 AM
                {
                    qaTime = str.Substring(0, 2) + ":" + str.Substring(2, 2) + " " + str.Substring(5).ToUpper();
                }
                return true;
            }
            else if (str.Length == 8) //12:45 AM
            {
                qaTime = str.ToUpper();
                return true;
            }
            else
            {
                err = "Invalid time format";
            }
            return false;
        }
        [HttpPost]
        public ActionResult OnSaveProductionQATable(string profile, string staffidloggedin, QATableItem row)
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
                    //-------------------------Get Score
                    int iScore = 0;
                    if (row.m_QAComments != null) if (row.m_QAComments.Length > 0) iScore += 1;
                    if (row.m_QATriggerType != null) if (row.m_QATriggerType.Length > 0) iScore += 1;
                    if (row.m_QAHR != null) if (row.m_QAHR.Length > 0) iScore += 1;
                    if (row.m_QAStripPosting != null) if (row.m_QAStripPosting.Length > 0) iScore += 1;

                    if (row.m_QAStripCutting != null) if (row.m_QAStripCutting.Length > 0) iScore += 2;
                    if (row.m_QAFindings != null) if (row.m_QAFindings.Length > 0) iScore += 4;

                    if (row.m_QAMissedMDN != null) if (row.m_QAMissedMDN.Length > 0) iScore = 10;
                    string qaTime = "", err = "";
                    if (formatQATime(row.m_QATime, out qaTime, out err))
                    {
                        //-------------------------
                        string sSQL = "update " + MyGlobal.activeDB + ".tbl_production_qatable Set " +
                            "m_QAInitials='" + row.m_QAInitials.ToUpper() + "', " +
                            "m_QATime='" + qaTime + "', " +
                            "m_QAComments='" + row.m_QAComments + "', " +
                            "m_QATriggerType='" + row.m_QATriggerType + "', " +
                            "m_QAHR='" + row.m_QAHR + "', " +
                            "m_QAStripPosting='" + row.m_QAStripPosting + "', " +
                            "m_QAStripCutting='" + row.m_QAStripCutting + "', " +
                            "m_QAFindings='" + row.m_QAFindings + "', " +
                            "m_QAMissedMDN='" + row.m_QAMissedMDN + "'," +
                            "m_QAScore='" + iScore + "'," +
                            "m_QAFreeze='1'," +
                            "m_QASavedBy='" + staffidloggedin + "'," +
                            "m_QASavedTime=Now() " +
                            "where m_Profile='" + profile + "' and m_id='" + row.m_id + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            int iRet = mySqlCommand.ExecuteNonQuery();
                            if (iRet > 0) postResponse.result = "Updated";
                            else postResponse.result = "Nothing to update";
                            postResponse.iParam1 = iScore;
                            postResponse.status = true;
                        }
                    }
                    else
                    {
                        postResponse.result = err;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("OnSaveProductionQATable-MySqlException->" + ex.Message);
                postResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("OnSaveProductionQATable-Exception->" + ex.Message);
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------------------------------------
        [HttpPost]
        public ActionResult OnSaveProductionConfirmed(
            string profile, string staffidlogged, string staffid, string day, string month, string year, string process, string confirmed)
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
                    string sSQL = "update " + MyGlobal.activeDB + ".tbl_production Set m_Confirmed='" + confirmed + "'," +
                        "m_ConfirmedBy='" + staffidlogged + "',m_ConfirmedTime=Now() " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Day='" + day + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' and m_Year='" + year + "' " +
                        "and m_Process='" + process + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        int iRet = mySqlCommand.ExecuteNonQuery();
                        postResponse.iParam1 = iRet;
                        postResponse.status = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("HRProductionResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("HRProductionResponse-Exception->" + ex.Message);
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        public ActionResult HRAttendanceResponse(
            string profile, string email, string staffid,
            string dtYear, string dtMonth, string dtDay,
            string dtYearTo, string dtMonthTo, string dtDayTo,
            string team, string level, string staffidsearch)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            bool bIsSA = staffid.Equals(staffidsearch);

            team = Server.UrlDecode(team);
            var hrAttendanceResponse = new HRAttendanceResponse();
            hrAttendanceResponse.status = false;
            hrAttendanceResponse.result = "";
            hrAttendanceResponse.AttendanceMethod = "";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            int iDay = MyGlobal.GetInt16(dtDay);
            /*
            int iYearName = iYear;
            int iMonthName = iMonth + 1;
            if (iMonthName > 12)
            {
                iYearName++;
                iMonthName = 1;
            }
            */

            //---------------------
            int iYearTo = MyGlobal.GetInt16(dtYearTo);
            int iMonthTo = MyGlobal.GetInt16(dtMonthTo);
            int iDayTo = MyGlobal.GetInt16(dtDayTo);


            int iYearName = iYearTo;
            int iMonthName = iMonthTo;


            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //----------------------------Get permission for this user
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
                    double dlbDtStart = 0, dlbDtEnd = 0;
                    GetFromToDates(iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo, out dlbDtStart, out dlbDtEnd);
                    //-----------------------------------------------------------
                    if (team.Length == 0)
                    {
                        sSQL = "select m_Team from " + MyGlobal.activeDB + ".tbl_staffs where " +
                            "m_StaffID='" + staffidsearch + "' and m_Profile='" + profile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0)) team = reader.GetString(0);
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------
                    sSQL = "select m_Head,m_State from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                    "where m_Profile='" + profile + "' and  m_StaffID = '" + staffid + "' " +
                    "and m_Team = '" + team + "'";
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
                                        if (reader.GetString(0).Equals("attendance")) hrAttendanceResponse.per_attendance = reader.GetInt16(1);
                                        if (reader.GetString(0).Equals("production")) hrAttendanceResponse.per_production = reader.GetInt16(1);
                                        if (reader.GetString(0).Equals("roster")) hrAttendanceResponse.per_roster = reader.GetInt16(1);
                                    }
                                }
                            }
                        }
                    }
                    if (bIsSA)
                    {
                        hrAttendanceResponse.per_attendance = 2;
                        hrAttendanceResponse.per_production = 2;
                        hrAttendanceResponse.per_roster = 2;
                    }
                    //----------------------------Fill sarTeams

                    hrAttendanceResponse.sarTeams.Add("");
                    bool bIsIncomingTeamInSecurityPermittedList = false;
                    string permission = "select m_Team from " + MyGlobal.activeDB + ".tbl_misc_teams_permissions " +
                    "where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' and m_Head='attendance'";

                    sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_misc_teams " +
                        "where m_Profile='" + profile + "' ";
                    if (!bIsSA) sSQL += "and m_Name in (" + permission + ") ";
                    sSQL += "order by m_Name;";

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
                                        hrAttendanceResponse.sarTeams.Add(reader.GetString(0));
                                        if (team.Equals(reader.GetString(0))) bIsIncomingTeamInSecurityPermittedList = true;
                                    }
                                }
                            }
                        }
                    }
                    if (staffidsearch.Length == 0)
                    {
                        con.Close();
                        hrAttendanceResponse.result = "Select / Search a staff";
                        return Json(hrAttendanceResponse, JsonRequestBehavior.AllowGet);
                    }
                    if (!bIsIncomingTeamInSecurityPermittedList)
                    {
                        //---Do this except this two conditions. Edit permitted or own attendance
                        //team = "";
                        //staffidsearch = "";
                        if (!bIsSA)
                        {
                            con.Close();
                            hrAttendanceResponse.result = "No permission to view staffs from the team '" + team + "'";
                            return Json(hrAttendanceResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //----------------------------Fill sarTeams
                    BreakItem itemEmpty = new BreakItem();
                    itemEmpty.key = "";
                    itemEmpty.value = "";
                    hrAttendanceResponse.sarStaffs.Add(itemEmpty);

                    sSQL = "SELECT m_StaffID,m_FName,m_AttendanceMethod FROM " + MyGlobal.activeDB + ".tbl_staffs " +
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
                                        hrAttendanceResponse.sarStaffs.Add(item);
                                        if (item.key.Equals(staffidsearch)) hrAttendanceResponse.staffidsearchName = item.value;
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                    //---------------------------------------------------------Get Attendance method

                    sSQL = "SELECT m_AttendanceMethod,m_AttendanceSource FROM " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' limit 1";
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
                                        hrAttendanceResponse.AttendanceMethod = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        hrAttendanceResponse.AttendanceSource = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    }
                                }
                            }
                        }
                    }
                    if (hrAttendanceResponse.AttendanceMethod.Length == 0)
                        hrAttendanceResponse.AttendanceMethod = "Functional";
                    if (hrAttendanceResponse.AttendanceSource.Length == 0)
                    {

                        hrAttendanceResponse.AttendanceSource = "Bio-Mobile";
                    }
                    //------------------------------------------------------
                    hrAttendanceResponse.listType = "live";
                    string sDisplayTable = "tbl_attendance";

                    //---------------------------------------------------------
                    sSQL = "select m_ApprovedByTime1 from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' " +
                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
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
                                        sDisplayTable = "tbl_attendance_approved";
                                        hrAttendanceResponse.listType = "approved";
                                    }
                                }
                            }
                        }
                    }
                    //---------------------
                    //--------------------------------------------------------------
                    //string summary_update_string1 = "";
                    //string summary_update_string2 = "";
                    //string message_sql_loop1 = "";
                    //string message_sql_loop2 = "";
                    string[] summary_update_string = new string[6] { "", "", "", "", "", "" };
                    string[] message_sql_loop = new string[6] { "", "", "", "", "", "" };
                    string pay_session = "pay_" + staffidsearch + "_" + iYearName + "_" + (iMonthName - 1);

                    if (level == null) level = "0";
                    if (level.Equals("11"))  // Cancel HR
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance_approved where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance_summary where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            level = "0";
                            hrAttendanceResponse.listType = "live";
                            sDisplayTable = "tbl_attendance";

                            //------------------------------------------Message
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                             "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                             "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                             "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                             "'Cancelled from level 1');";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            myTrans.Commit();
                            //------------------------------------------Message
                        }
                        catch (Exception e)
                        {
                            myTrans.Rollback();
                        }
                    }
                    else if (level.Equals("12"))  // Cancel Production
                    {
                        bool bStage3IsNotNull = true;
                        sSQL = "select m_ApprovedBy3 from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
    "where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        bStage3IsNotNull = reader.IsDBNull(0);
                                    }
                                }
                            }
                        }
                        if (bStage3IsNotNull)
                        {
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                sSQL = "update " + MyGlobal.activeDB + ".tbl_attendance_summary Set m_ApprovedBy2=null,m_ApprovedByTime2=null where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                level = "0";
                                sDisplayTable = "tbl_attendance_approved";
                                hrAttendanceResponse.listType = "approved";
                                //------------------------------------------Message
                                sSQL = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                                 "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                                 "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                                 "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                                 "'Cancelled from level 2');";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                myTrans.Commit();
                                //------------------------------------------Message
                            }
                            catch (Exception e)
                            {
                                myTrans.Rollback();
                            }
                        }
                        else
                        {
                            hrAttendanceResponse.result = "Accounts Apporval is already DONE. You can't cancel now.";
                            //return Json(hrAttendanceResponse, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else if (level.Equals("13"))  // Cancel Accounts
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance_approved where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance_summary where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            level = "0";
                            //------------------------------------------Message
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                             "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                             "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                             "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                             "'Cancelled from level Accounts');";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();

                            myTrans.Commit();
                            //------------------------------------------Message
                        }
                        catch (Exception e)
                        {
                            myTrans.Rollback();
                        }
                    }
                    if (level.Equals("1"))
                    {
                        if (iDay != iStartDate)
                        {
                            con.Close();
                            hrAttendanceResponse.result = "Day should start from " + iStartDate + "th for Approval(It is attendance start date)";
                            return Json(hrAttendanceResponse, JsonRequestBehavior.AllowGet);
                        }
                        //-------------------------------------Process before approving
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_attendance_approved where m_Profile='" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "';";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();


                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendance_approved (m_Profile, m_StaffID, m_Year, m_Month, m_Date, m_RosterName, m_ShiftName, m_ShiftStart, m_ShiftEnd, m_ActualStart, m_ActualEnd, lWorkhours,m_MarkRoster,m_MarkLeave,m_RosterOptions,m_WorkApproved,m_AsOn,m_LateLoginStatus,m_Mode,Working,sShortage,dblDayTobePaid,dblALOPs_Local,dblLOPs_Local,dblPaidLeaves_Local,lLateSeconds_AccumilatedForTheMonth_Local,iNoOfOFFs,dblActualWorkingDays_Local,dblAbsent,m_Source,pay_scale,pay_key,pay_startdate) " +
                            "SELECT m_Profile, m_StaffID, m_Year, m_Month, m_Date, m_RosterName, m_ShiftName, m_ShiftStart, m_ShiftEnd, m_ActualStart, m_ActualEnd, lWorkhours,m_MarkRoster,m_MarkLeave,m_RosterOptions,m_WorkApproved,m_AsOn,m_LateLoginStatus,m_Mode,Working,sShortage,dblDayTobePaid,dblALOPs_Local,dblLOPs_Local,dblPaidLeaves_Local,lLateSeconds_AccumilatedForTheMonth_Local,iNoOfOFFs,dblActualWorkingDays_Local,dblAbsent,m_Source,pay_scale,pay_key,pay_startdate FROM " + MyGlobal.activeDB + ".tbl_attendance WHERE m_Profile = '" + profile + "' and m_StaffID = '" + staffidsearch + "' and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "'";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            myTrans.Commit();

                            sDisplayTable = "tbl_attendance_approved";
                            hrAttendanceResponse.listType = "approved";
                        }
                        catch (Exception e)
                        {
                            myTrans.Rollback();
                            sDisplayTable = "tbl_attendance";
                            hrAttendanceResponse.listType = "live";
                        }

                    }
                    else if (level.Equals("2"))
                    {

                        sDisplayTable = "tbl_attendance_approved";
                        hrAttendanceResponse.listType = "approved";
                    }
                    else if (level.Equals("9")) // Just list the approved
                    {
                        sDisplayTable = "tbl_attendance_approved";
                        hrAttendanceResponse.listType = "approved";
                    }
                    else if (level.Equals("7")) // If approved list exists, list it or live data
                    {
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                            "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' " +
                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    sDisplayTable = "tbl_attendance_approved";
                                    hrAttendanceResponse.listType = "approved";
                                }
                                else
                                {
                                    sDisplayTable = "tbl_attendance";
                                    hrAttendanceResponse.listType = "live";
                                }
                            }
                        }
                    }
                    else if (level.Equals("8")) // Process from meterbox
                    {   // level 8 also comes here. Normal query
                        //---------------------Update attendance table
                        string deleteScript = "";
                        string sInsert = GetAttendanceTableScriptsFromRosterAndActivity(con, profile, staffidsearch,
                            iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo, iYearName, iMonthName, out deleteScript);

                        if (sInsert.Length > 0)
                        {
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                myCommand.CommandText = deleteScript;
                                myCommand.ExecuteNonQuery();
                                myCommand.CommandText = sInsert;
                                myCommand.ExecuteNonQuery();
                                myTrans.Commit();
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    myTrans.Rollback();
                                }
                                catch (MySqlException ex)
                                {
                                }
                            }
                            finally
                            {
                            }
                            //------------------------------
                            ProcessAttendanceTable(profile, staffid, level, sDisplayTable, staffidsearch,
                                iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo,
                                 hrAttendanceResponse.AttendanceMethod);
                            //iYearName, iMonthName,
                            AttendanceRunChecker(profile, staffidsearch,
                                iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo
                                );
                            //iYearName, iMonthName
                        }
                    }
                    else if (level.Equals("20")) // Process Existing attendance table
                    {

                        ProcessAttendanceTable(profile, staffid, level, sDisplayTable, staffidsearch,
                            iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo,
                             hrAttendanceResponse.AttendanceMethod);
                        //iYearName, iMonthName,
                        AttendanceRunChecker(profile, staffidsearch,
                            iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo);
                        //iYearName, iMonthName);
                    }
                    else
                    {
                    }

                    //--------------------------------------------------------------
                    sSQL = "select * from " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' " +
                    "and m_Date>='" + dlbDtStart + "' " +
                    "and m_Date<='" + dlbDtEnd + "' " +
                    "order by m_Date desc";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                string summary_string_tmp = "", message_loop_tmp = "";

                                double dblAbsent_Total = 0;
                                double dblDayTobePaidTotal = 0;
                                double dblALOPs = 0;
                                double dblPaidLeaves = 0;
                                double dblLOPs = 0;
                                int iNoOfOFFs = 0;
                                long lLateSeconds_AccumilatedForTheMonth = 0;
                                double dblActualWorkingDays = 0;
                                double dblLOPBasedOnDelay = 0;
                                Dictionary<string, int> _rosterOptions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                string sRosterOptionsResult = "";
                                string sRosterOptions = "";
                                int iScheduledWorkingDays = 0;
                                //----------------------------------------
                                long int32StartDate = 0, int32EndDate = 0;
                                //---------------------------------------
                                string pay__scale = "";
                                Int32 pay__key = 0, pay__startdate = 0;
                                string payscale_key = "";
                                while (reader.Read())
                                {
                                    //-----------------------INI
                                    string pay_key = MyGlobal.GetPureString(reader, "pay_scale") + MyGlobal.GetPureString(reader, "pay_key") + MyGlobal.GetPureString(reader, "pay_startdate");
                                    if (!payscale_key.Equals(pay_key))
                                    {
                                        if (payscale_key.Length != 0)
                                        {
                                            //---------------------------MOved from Location 1200
                                            foreach (var item in _rosterOptions)
                                            {
                                                if (item.Value > 0) sRosterOptionsResult += item.Key + ":" + item.Value + ", ";
                                            }
                                            dblLOPBasedOnDelay = GetLOPBasedOnDelay(lLateSeconds_AccumilatedForTheMonth);
                                            dblDayTobePaidTotal = dblDayTobePaidTotal - dblLOPBasedOnDelay;

                                            /************************************************/
                                            summary_string_tmp = "insert into " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                                                "(m_Profile,m_StaffID,m_ApprovedBy" + level + ",m_ApprovedByTime" + level + "," +
                                                "m_Year,m_Month," +
                                                "m_WorkingDays,m_OFFs,m_Leaves," +
                                                "m_ALOPs,m_LOPs,m_LateSeconds," +
                                                "m_LOPBasedOnDelay,m_DaysToBePaidTotal," +
                                                "m_ActualWorkingDays," +
                                                "m_RosterOptions,m_RosterOptionsResult," +
                                                "m_DateStart,m_DateEnd," +
                                                "m_AsOn,pay_scale,pay_key,pay_startdate" +
                                                ") values " +
                                                "('" + profile + "','" + staffidsearch + "','" + staffid + "',Now()," +
                                                "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + iScheduledWorkingDays + "','" + iNoOfOFFs + "','" + dblPaidLeaves + "'," +
                                                "'" + dblALOPs + "','" + dblLOPs + "','" + lLateSeconds_AccumilatedForTheMonth + "'," +
                                                "'" + dblLOPBasedOnDelay + "','" + dblDayTobePaidTotal + "'," +
                                                "'" + dblActualWorkingDays + "'," +
                                                "'" + sRosterOptions + "'," +
                                                "'" + sRosterOptionsResult + "'," +
                                                "'" + int32StartDate + "','" + int32EndDate + "'," +
                                                "UNIX_TIMESTAMP()," +
                                                "'" + pay__scale + "'," +
                                                "'" + pay__key + "'," +
                                                "'" + pay__startdate + "'" +
                                                ");";

                                            message_loop_tmp = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                                                "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                                                "'Summary for " + iScheduledWorkingDays + " days created. Approval level 1.');";

                                            for (int i = 0; i < 6; i++) if (summary_update_string[i].Length == 0) { summary_update_string[i] = summary_string_tmp; break; };
                                            for (int i = 0; i < 6; i++) if (message_sql_loop[i].Length == 0) { message_sql_loop[i] = message_loop_tmp; break; };
                                            /*
                                            if (summary_update_string[0].Length == 0) summary_update_string[0] = summary_string_tmp;
                                            else if (summary_update_string[1].Length == 0) summary_update_string[1] = summary_string_tmp;
                                            else if (summary_update_string[2].Length == 0) summary_update_string[2] = summary_string_tmp;
                                            else if (summary_update_string[3].Length == 0) summary_update_string[3] = summary_string_tmp;
                                            else if (summary_update_string[4].Length == 0) summary_update_string[4] = summary_string_tmp;
                                            else if (summary_update_string[5].Length == 0) summary_update_string[5] = summary_string_tmp;

                                            if (message_sql_loop[0].Length == 0) message_sql_loop[0] = message_loop_tmp;
                                            else if (message_sql_loop[1].Length == 0) message_sql_loop[1] = message_loop_tmp;
                                            else if (message_sql_loop[2].Length == 0) message_sql_loop[2] = message_loop_tmp;
                                            else if (message_sql_loop[3].Length == 0) message_sql_loop[3] = message_loop_tmp;
                                            else if (message_sql_loop[4].Length == 0) message_sql_loop[4] = message_loop_tmp;
                                            else if (message_sql_loop[5].Length == 0) message_sql_loop[5] = message_loop_tmp;
*/

                                            //---------------------------Location 1200


                                            string remark1Tmp =
                                            "<div style='display:inline-block;margin-top:4px;'>" +
                                                "<div style='margin-top:-2px;font-size:x-small;background-color:orange;min-width:40em;padding-right:8px;'>Payscale : <span style='font-weight:bold;'>" + pay__scale + " ► " +
                                                "eff. dt. " + MyGlobal.ToDateTimeFromEpoch(pay__key).ToString("dd-MM-yyy") + " ► " +
                                                "start dt. " + MyGlobal.ToDateTimeFromEpoch(pay__startdate).ToString("dd-MM-yyy") +
                                                "</span></div>" +

                                                "<div style='float:left;min-width:40%; display:inline-block;border:1px solid #ccc;padding-right:6px;padding-left:4px;background-color:#eee;'>" +
                                                "<div style='margin-top:-2px;'>Scheduled days : <span style='font-weight:bold'>" + (iScheduledWorkingDays) + "</span></div>" +
                                                "<div style='margin-top:-8px;'>OFFs : <span style='font-weight:bold;color:#000;'>" + iNoOfOFFs + "</span>, leaves : <span style='font-weight:bold;color:#00e;'>" + dblPaidLeaves + "</span></div>" +
                                                "<div style='margin-top:-8px;font-weight:bold;'>&nbsp;" + sRosterOptionsResult + "</div>" +
                                                "<div style='margin-top:-8px;white-space:nowrap;'>Actual Working Days : <span style='font-weight:bold;color:#00e;'>" + dblActualWorkingDays + "</span></div>" +
                                                "</div>" +

                                                "<div style='display:inline-block;vertical-align:top;border:1px solid #ccc;padding-right:6px;padding-left:4px;background-color:#eee;'>" +
                                                "<div style='margin-top:-2px;display:inline-block;' title='Absent from work'>Absent :  <span style='font-weight:bold;color:red;'>" + dblAbsent_Total + "</span>, </div> " +
                                                "<div style='margin-top:-2px;display:inline-block;' title='Additional Loss of Pay'>ALOPs :  <span style='font-weight:bold;color:red;'>" + dblALOPs + "</span>, </div> " +
                                                "<div style='margin-top:-8px;display:inline-block;' title='Loss of Pay'>LOPs :  <span style='font-weight:bold;color:red;'>" + dblLOPs + "</span></div>" +
                                                "<div style='margin-top:-8px;'>Over all delay : <span style='font-weight:bold;color:orange;'>" + MyGlobal.ToDateTimeFromEpoch(lLateSeconds_AccumilatedForTheMonth).ToString("HH:mm:ss") + "</span></div>" +
                                                "<div style='margin-top:-8px;'>LOP based on delay :  <span style='font-weight:bold;color:red;'>" + dblLOPBasedOnDelay + "</span></div>" +
                                                "<div style='margin-top:-8px;'>Pay Days : <span style='font-weight:bold;color:darkgreen;'>" + dblDayTobePaidTotal + "</span></div>" +
                                                "</div>" +
                                            "</div>";
                                            for (int i = 0; i < 6; i++) if (hrAttendanceResponse.summary[i].Length == 0) { hrAttendanceResponse.summary[i] = remark1Tmp; break; };
                                            /*
                                            if (hrAttendanceResponse.summary[0].Length == 0) hrAttendanceResponse.summary[0] = remark1Tmp;
                                            else if (hrAttendanceResponse.summary[1].Length == 0) hrAttendanceResponse.summary[1] = remark1Tmp;
                                            else if (hrAttendanceResponse.summary[2].Length == 0) hrAttendanceResponse.summary[2] = remark1Tmp;
                                            else if (hrAttendanceResponse.summary[3].Length == 0) hrAttendanceResponse.summary[3] = remark1Tmp;
                                            else if (hrAttendanceResponse.summary[4].Length == 0) hrAttendanceResponse.summary[4] = remark1Tmp;
                                            else if (hrAttendanceResponse.summary[5].Length == 0) hrAttendanceResponse.summary[5] = remark1Tmp;
                                            //else if (hrAttendanceResponse.remarks2.Length == 0) hrAttendanceResponse.remarks2 = remark1Tmp;
                                            */
                                            /************************************************/
                                            int32StartDate = 0;
                                            int32EndDate = 0;

                                            dblAbsent_Total = 0;
                                            dblDayTobePaidTotal = 0;
                                            dblALOPs = 0;
                                            dblPaidLeaves = 0;
                                            dblLOPs = 0;
                                            iNoOfOFFs = 0;
                                            lLateSeconds_AccumilatedForTheMonth = 0;
                                            dblActualWorkingDays = 0;
                                            dblLOPBasedOnDelay = 0;
                                            _rosterOptions.Clear();
                                            sRosterOptionsResult = "";
                                            sRosterOptions = "";
                                            iScheduledWorkingDays = 0;
                                        }
                                        payscale_key = pay_key;
                                    }
                                    //-----------------------INI
                                    HRAttendanceRow row = new HRAttendanceRow();
                                    row.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");

                                    row.m_Year = MyGlobal.GetPureInt16(reader, "m_Year");
                                    row.m_Month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    row.m_Date = MyGlobal.GetPureInt32(reader, "m_Date"); // Unixfulltime
                                    //if (int32StartDate == 0) int32StartDate = row.m_Date;
                                    if (int32EndDate == 0) int32EndDate = row.m_Date;
                                    int32StartDate = row.m_Date;

                                    row.m_RosterName = MyGlobal.GetPureString(reader, "m_RosterName");
                                    row.m_ShiftName = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    row.m_ShiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStart");
                                    row.m_ShiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEnd");
                                    row.m_ActualStart = MyGlobal.GetPureInt32(reader, "m_ActualStart");
                                    row.m_ActualEnd = MyGlobal.GetPureInt32(reader, "m_ActualEnd");
                                    row.lWorkhours = MyGlobal.GetPureInt32(reader, "lWorkhours");
                                    row.m_WorkApproved = MyGlobal.GetPureInt32(reader, "m_WorkApproved");
                                    row.m_AsOn = MyGlobal.GetPureInt32(reader, "m_AsOn");
                                    row.m_LateLoginStatus = MyGlobal.GetPureInt16(reader, "m_LateLoginStatus");
                                    row.m_Mode = MyGlobal.GetPureInt16(reader, "m_Mode");

                                    //redefine again if Half day ....
                                    row.logindelay = (row.m_ActualStart - row.m_ShiftStart);
                                    row.workspan = row.m_ActualEnd - row.m_ActualStart;
                                    row.sShortage = "";
                                    row.Working = "";
                                    iScheduledWorkingDays++;
                                    row.m_Source = MyGlobal.GetPureString(reader, "m_Source");
                                    row.m_MarkRoster = MyGlobal.GetPureString(reader, "m_MarkRoster");
                                    row.m_MarkLeave = MyGlobal.GetPureString(reader, "m_MarkLeave");
                                    if (sRosterOptions.Length == 0) sRosterOptions = MyGlobal.GetPureString(reader, "m_RosterOptions");
                                    //--------------------Get from table
                                    row.sShortage = MyGlobal.GetPureString(reader, "sShortage");
                                    row.Working = MyGlobal.GetPureString(reader, "Working");
                                    row.dblDayTobePaid += MyGlobal.GetPureDouble(reader, "dblDayTobePaid");
                                    //--------------------
                                    dblDayTobePaidTotal += MyGlobal.GetPureDouble(reader, "dblDayTobePaid");
                                    dblALOPs += MyGlobal.GetPureDouble(reader, "dblALOPs_Local");
                                    dblPaidLeaves += MyGlobal.GetPureDouble(reader, "dblPaidLeaves_Local");
                                    dblLOPs += MyGlobal.GetPureDouble(reader, "dblLOPs_Local");
                                    iNoOfOFFs += MyGlobal.GetPureInt32(reader, "iNoOfOFFs");
                                    lLateSeconds_AccumilatedForTheMonth += MyGlobal.GetPureInt32(reader, "lLateSeconds_AccumilatedForTheMonth_Local");
                                    dblActualWorkingDays += MyGlobal.GetPureDouble(reader, "dblActualWorkingDays_Local");
                                    dblAbsent_Total += MyGlobal.GetPureDouble(reader, "dblAbsent");
                                    if (!row.m_MarkRoster.Equals(MyGlobal.WORKDAY_MARKER) && !row.m_MarkRoster.Equals("OFF") && row.m_MarkRoster.Length > 0)
                                    {
                                        if (!_rosterOptions.ContainsKey(row.m_MarkRoster))
                                        {
                                            _rosterOptions.Add(row.m_MarkRoster, 1);
                                        }
                                        else
                                        {
                                            _rosterOptions[row.m_MarkRoster]++;
                                        }
                                    }
                                    //--------------------
                                    row.payscale = MyGlobal.GetPureString(reader, "pay_scale");
                                    row.key = MyGlobal.GetPureInt32(reader, "pay_key");
                                    row.startdate = MyGlobal.GetPureInt32(reader, "pay_startdate");
                                    pay__scale = row.payscale;
                                    pay__key = row.key;
                                    pay__startdate = row.startdate;
                                    //--------------------
                                    hrAttendanceResponse.rows.Add(row);
                                }// while end
                                 /************************************************/
                                foreach (var item in _rosterOptions)
                                {
                                    if (item.Value > 0) sRosterOptionsResult += item.Key + ":" + item.Value + ", ";
                                }
                                dblLOPBasedOnDelay = GetLOPBasedOnDelay(lLateSeconds_AccumilatedForTheMonth);
                                dblDayTobePaidTotal = dblDayTobePaidTotal - dblLOPBasedOnDelay;



                                summary_string_tmp = "insert into " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                                    "(m_Profile,m_StaffID,m_ApprovedBy" + level + ",m_ApprovedByTime" + level + "," +
                                    "m_Year,m_Month," +
                                    "m_WorkingDays,m_OFFs,m_Leaves," +
                                    "m_ALOPs,m_LOPs,m_LateSeconds," +
                                    "m_LOPBasedOnDelay,m_DaysToBePaidTotal," +
                                    "m_ActualWorkingDays," +
                                    "m_RosterOptions,m_RosterOptionsResult," +
                                    "m_DateStart,m_DateEnd," +
                                    "m_AsOn,pay_scale,pay_key,pay_startdate" +
                                    ") values " +
                                    "('" + profile + "','" + staffidsearch + "','" + staffid + "',Now()," +
                                    "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                    "'" + iScheduledWorkingDays + "','" + iNoOfOFFs + "','" + dblPaidLeaves + "'," +
                                    "'" + dblALOPs + "','" + dblLOPs + "','" + lLateSeconds_AccumilatedForTheMonth + "'," +
                                    "'" + dblLOPBasedOnDelay + "','" + dblDayTobePaidTotal + "'," +
                                    "'" + dblActualWorkingDays + "'," +
                                    "'" + sRosterOptions + "'," +
                                    "'" + sRosterOptionsResult + "'," +
                                    "'" + int32StartDate + "','" + int32EndDate + "'," +
                                                "UNIX_TIMESTAMP()," +
                                                "'" + pay__scale + "'," +
                                                "'" + pay__key + "'," +
                                                "'" + pay__startdate + "'" +
                                    ");";

                                message_loop_tmp = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                                "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                                "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                                "'Summary for " + iScheduledWorkingDays + " days created. Approval level 1.');";

                                for (int i = 0; i < 6; i++) if (summary_update_string[i].Length == 0) { summary_update_string[i] = summary_string_tmp; break; };
                                for (int i = 0; i < 6; i++) if (message_sql_loop[i].Length == 0) { message_sql_loop[i] = message_loop_tmp; break; };

                                //---------------------------

                                string remark2Tmp =
                                                                               "<div style='display:inline-block;margin-top:4px;'>" +
                                                "<div style='margin-top:-2px;font-size:x-small;background-color:orange;min-width:40em;padding-right:8px;'>Payscale : <span style='font-weight:bold;'>" + pay__scale + " ► " +
                                                "eff. dt. " + MyGlobal.ToDateTimeFromEpoch(pay__key).ToString("dd-MM-yyy") + " ► " +
                                                "start dt. " + MyGlobal.ToDateTimeFromEpoch(pay__startdate).ToString("dd-MM-yyy") +
                                                "</span></div>" +
                                 "<div style='float:left;min-width:40%; display:inline-block;margin-right:11px;border:1px solid #ccc;padding-right:6px;padding-left:4px;background-color:#eee;'>" +
                                    "<div style='margin-top:-2px;'>Scheduled days : <span style='font-weight:bold'>" + (iScheduledWorkingDays) + "</span></div>" +
                                    "<div style='margin-top:-8px;'>OFFs : <span style='font-weight:bold;color:#000;'>" + iNoOfOFFs + "</span>, leaves : <span style='font-weight:bold;color:#00e;'>" + dblPaidLeaves + "</span></div>" +
                                    "<div style='margin-top:-8px;font-weight:bold;'>&nbsp;" + sRosterOptionsResult + "</div>" +
                                    "<div style='margin-top:-8px;white-space:nowrap;'>Actual Working Days : <span style='font-weight:bold;color:#00e;'>" + dblActualWorkingDays + "</span></div>" +
                                 "</div>" +

                                    "<div style='display:inline-block;float:right; vertical-align:top;border:1px solid #ccc;padding-right:6px;padding-left:4px;background-color:#eee;'>" +
                                    "<div style='margin-top:-2px;display:inline-block;' title='Absent from work'>Absent :  <span style='font-weight:bold;color:red;'>" + dblAbsent_Total + "</span>, </div> " +
                                    "<div style='margin-top:-2px;display:inline-block;' title='Additional Loss of Pay'>ALOPs :  <span style='font-weight:bold;color:red;'>" + dblALOPs + "</span>, </div> " +
                                    "<div style='margin-top:-8px;display:inline-block;' title='Loss of Pay'>LOPs :  <span style='font-weight:bold;color:red;'>" + dblLOPs + "</span></div>" +
                                    "<div style='margin-top:-8px;'>Over all delay : <span style='font-weight:bold;color:orange;'>" + MyGlobal.ToDateTimeFromEpoch(lLateSeconds_AccumilatedForTheMonth).ToString("HH:mm:ss") + "</span></div>" +
                                    "<div style='margin-top:-8px;'>LOP based on delay :  <span style='font-weight:bold;color:red;'>" + dblLOPBasedOnDelay + "</span></div>" +
                                    "<div style='margin-top:-8px;'>Pay Days : <span style='font-weight:bold;color:darkgreen;'>" + dblDayTobePaidTotal + "</span></div>" +
                                                "</div>" +
                                            "</div>";
                                //if (hrAttendanceResponse.remarks1.Length == 0) hrAttendanceResponse.remarks1 = remark2Tmp;
                                //else if (hrAttendanceResponse.remarks2.Length == 0) hrAttendanceResponse.remarks2 = remark2Tmp;
                                for (int i = 0; i < 6; i++) if (hrAttendanceResponse.summary[i].Length == 0) { hrAttendanceResponse.summary[i] = remark2Tmp; break; };
                                /************************************************/

                            }
                        }
                    }
                    //----------If approval process, do it here
                    //-----------------------If Approval process, do the below
                    if (level.Equals("1") || level.Equals("2"))// || level.Equals("3")))
                    {
                        string sUpdate = "";
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                            "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' " +
                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    sUpdate = "update " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                                        "Set m_ApprovedBy" + level + "='" + staffid + "'," +
                                        "m_ApprovedByTime" + level + "=Now() " +
                                        "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' " +
                                        "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";

                                    sUpdate += "insert into " + MyGlobal.activeDB + ".tbl_messages " +
                                    "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
                                    "('" + profile + "','" + staffidsearch + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
                                    "'" + pay_session + "','" + email + "','" + email + "',Now()," +
                                    "'Approval by level " + level + ".');";
                                }
                                else
                                {
                                    sUpdate = "";
                                    //sUpdate = summary_update_string1 + summary_update_string2;
                                    //sUpdate += message_sql_loop1 + message_sql_loop2;
                                    for (int i = 0; i < 6; i++) sUpdate += summary_update_string[i];
                                    for (int i = 0; i < 6; i++) sUpdate += message_sql_loop[i];
                                }
                                hrAttendanceResponse.listType = "approved";
                            }
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sUpdate, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            ///hrAttendanceResponse.result = "Approved";
                        }
                    }
                    //---------------------Is approved list exists for this start month
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "' and m_Year='" + iYearName + "' " +
                    "and m_Month='" + (iMonthName - 1) + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    hrAttendanceResponse.approved.m_ApprovedBy1 = reader.IsDBNull(15) ? "" : reader.GetString(15);
                                    hrAttendanceResponse.approved.m_ApprovedByTime1 = reader.IsDBNull(16) ? "" : reader.GetString(16);
                                    hrAttendanceResponse.approved.m_ApprovedBy2 = reader.IsDBNull(17) ? "" : reader.GetString(17);
                                    hrAttendanceResponse.approved.m_ApprovedByTime2 = reader.IsDBNull(18) ? "" : reader.GetString(18);
                                    hrAttendanceResponse.approved.m_ApprovedBy3 = reader.IsDBNull(19) ? "" : reader.GetString(19);
                                    hrAttendanceResponse.approved.m_ApprovedByTime3 = reader.IsDBNull(20) ? "" : reader.GetString(20);

                                    hrAttendanceResponse.approved.m_ApprovedBy4 = MyGlobal.GetPureString(reader, "m_ApprovedBy4");
                                    hrAttendanceResponse.approved.m_ApprovedByTime4 = MyGlobal.GetPureString(reader, "m_ApprovedByTime4");

                                    hrAttendanceResponse.approved.m_WorkingDays = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                                    hrAttendanceResponse.approved.m_OFFs = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);
                                    hrAttendanceResponse.approved.m_Leaves = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);
                                    hrAttendanceResponse.approved.m_ALOPs = reader.IsDBNull(7) ? 0 : reader.GetDouble(7);
                                    hrAttendanceResponse.approved.m_LOPs = reader.IsDBNull(8) ? 0 : reader.GetDouble(8);
                                    hrAttendanceResponse.approved.m_LateSeconds = reader.IsDBNull(9) ? 0 : reader.GetDouble(9);
                                    hrAttendanceResponse.approved.m_LOPBasedOnDelay = reader.IsDBNull(10) ? 0 : reader.GetDouble(10);
                                    hrAttendanceResponse.approved.m_ActualWorkingDays = reader.IsDBNull(11) ? 0 : reader.GetDouble(11);
                                    hrAttendanceResponse.approved.m_DaysToBePaidTotal = reader.IsDBNull(12) ? 0 : reader.GetDouble(12);
                                    hrAttendanceResponse.approved.m_RosterOptions = reader.IsDBNull(13) ? "" : reader.GetString(13);
                                    hrAttendanceResponse.approved.m_RosterOptionsResult = reader.IsDBNull(14) ? "" : reader.GetString(14);
                                }
                            }
                        }
                    }

                    //---------------------Is approved list exists for this start month
                    hrAttendanceResponse.team = team;
                    hrAttendanceResponse.staffidsearch = staffidsearch;

                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("HRAttendanceResponse-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("HRAttendanceResponse-Exception->" + ex.Message);
            }

            return Json(hrAttendanceResponse, JsonRequestBehavior.AllowGet);
        }
        private double GetLOPBasedOnDelay(long delay)
        {
            long lHalfDays = delay / 7200;
            return lHalfDays / 2.00F;
        }
        //----------------------------------------
        private string GetAttendanceTableScriptsFromRosterAndActivity(MySqlConnection con,
            string profile, string staffid,
            int iYear, int iMonth, int iDay, int iYearTo, int iMonthTo, int iDayTo,
            int iYearName, int iMonthName, out string deletescript)
        {
            string sInsert = "", sSQL = "";
            deletescript = "";
            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;
            double dblDtFrom = 0, dblDtTo = 0;
            try
            {
                dblDtFrom = ((new TimeSpan(new DateTime(iYear, (iMonth), iDay).Ticks)) - epochTicks).TotalSeconds;
                dblDtTo = ((new TimeSpan(new DateTime(iYearTo, (iMonthTo), iDayTo).Ticks)) - epochTicks).TotalSeconds;
            }
            catch (ArgumentOutOfRangeException)
            {
                return "";
            }
            //------------------------------------------
            //Dictionary<string, int> _rosterOptions_Table = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            //string sRosterOptions = ""; // /ACO:1,HP:

            //------------------------------------------
            Dictionary<int, string> _dayfilter = new Dictionary<int, string>();

            sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                "left join " + MyGlobal.activeDB + ".tbl_leaves lev on lev.m_Profile = roster.m_Profile " +
                "and lev.m_StaffID = roster.m_StaffID and " +
                "lev.m_Year = roster.m_Year and lev.m_Month = roster.m_Month " +
                "where roster.m_Profile = '" + profile + "' and roster.m_StaffID = '" + staffid + "' " +
                "and ((roster.m_Year = '" + iYear + "' and roster.m_Month = '" + (iMonth - 1) + "') or " +
                "(roster.m_Year = '" + iYearTo + "' and roster.m_Month = '" + (iMonthTo - 1) + "'))";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int year = reader.IsDBNull(5) ? 0 : reader.GetInt16(5);
                            int month = reader.IsDBNull(6) ? 0 : reader.GetInt16(6);
                            int iDay1 = reader.GetOrdinal("m_Day1") - 1;
                            int iDayL1 = reader.GetOrdinal("m_DayL1") - 2;
                            //-------------------------------Process for Roster boxes
                            for (int i = 1; i <= 31; i++)
                            {
                                double dblDt = 0;
                                try
                                {
                                    dblDt = ((new TimeSpan(new DateTime(year, (month + 1), i).Ticks)) - epochTicks).TotalSeconds;

                                    deletescript += "delete from " + MyGlobal.activeDB + ".tbl_attendance " +
                                    "where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' " +
                                    "and m_Date = '" + dblDt + "';";
                                }
                                catch (ArgumentOutOfRangeException)
                                {

                                }
                                double dblEpoxShiftSheduledStart = 0;
                                double dblEpoxShiftSheduledEnd = 0;
                                long lShiftStart = 0, lShiftEnd = 0, m_ActualStart = 0, m_ActualEnd = 0, lWorkhours = 0;
                                if ((dblDt >= dblDtFrom) && (dblDt <= dblDtTo) && dblDt < unixTimestamp)
                                {
                                    try
                                    {
                                        string sMarkRoster = "", sMarkLeave = "";
                                        if (!reader.IsDBNull(iDay1 + i))
                                        {
                                            sMarkRoster = reader.GetString(iDay1 + i);
                                        }
                                        /*
                                        if (!reader.IsDBNull(iDayL1 + (i * 2)))
                                        {
                                            if(  (reader.GetInt16(iDayL1 + (i * 2) + 1) == C_APPROVED) ||
                                                (reader.GetInt16(iDayL1 + (i * 2) + 1) == C_REVOKE_PENDING))
                                            {
                                                sMarkLeave = reader.GetString(iDayL1 + (i * 2));
                                            }
                                        }
                                        */
                                        if ((sMarkRoster.Length > 0))// || sMarkLeave.Length > 0
                                        {
                                            sMarkLeave = GetLeaveMarkerForThisDay(profile, staffid, year, month, i);
                                            bool bAllow = false;
                                            if (_dayfilter.ContainsKey(i))
                                            {
                                                MyGlobal.Error("Duplicate entry - " + sMarkRoster + ", " + sMarkLeave);
                                            }
                                            else
                                            {
                                                _dayfilter.Add(i, sMarkRoster + " * " + sMarkLeave);
                                                bAllow = true;
                                            }
                                            if (bAllow)
                                            {

                                                int iOrdShiftStartTime = reader.GetOrdinal("m_ShiftStartTime");

                                                lShiftStart = (reader.IsDBNull(iOrdShiftStartTime) ? 0 : reader.GetInt32(iOrdShiftStartTime));
                                                lShiftEnd = (reader.IsDBNull(iOrdShiftStartTime) ? 0 : reader.GetInt32(iOrdShiftStartTime + 1));
                                                m_ActualStart = 0;
                                                m_ActualEnd = 0;
                                                lWorkhours = 0;

                                                double epochShiftStart = dblDt + lShiftStart - const_ShiftPaddingPre + 0;
                                                double epochShiftEnd = dblDt + lShiftEnd + const_ShiftPaddingPost + 0;
                                                long lWorkApproved = 0;
                                                int iLateLoginStatus = 0;

                                                GetStaffWorkHours_with_Start_and_End(profile,
                                                    (long)epochShiftStart, //const_ShiftPaddingPre
                                                    (long)epochShiftEnd, //const_ShiftPaddingPost
                                                    staffid,
                                                     out lWorkhours, out lWorkApproved,
                                                     out m_ActualStart, out m_ActualEnd,
                                                     out iLateLoginStatus);

                                                dblEpoxShiftSheduledStart = dblDt + lShiftStart;
                                                dblEpoxShiftSheduledEnd = dblDt + lShiftEnd;
                                                DateTime dt = MyGlobal.ToDateTimeFromEpoch((long)dblDt);
                                                int iYear_ = dt.Year;
                                                int iMonth_ = dt.Month;
                                                //---------------------------
                                                sInsert += "insert into " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "(m_Profile,m_StaffID,m_Year,m_Month,m_Date,m_RosterName,m_ShiftName,m_ShiftStart," +
                                                    "m_ShiftEnd,m_ActualStart,m_ActualEnd,lWorkhours,m_MarkRoster,m_MarkLeave,m_RosterOptions," +
                                                    "m_WorkApproved,m_AsOn,m_LateLoginStatus,m_Source) values (" +
                                                    "'" + profile + "'," + "'" + staffid + "'," +
                                                    "'" + iYear_ + "'," + "'" + (iMonth_ - 1) + "'," +
                                                    "'" + dblDt + "'," +
                                                    "'" + (reader.IsDBNull(1) ? "" : reader.GetString(1)) + "'," +  //m_RosterName
                                                    "'" + (reader.IsDBNull(2) ? "" : reader.GetString(2)) + "'," + //m_ShiftName
                                                    "'" + dblEpoxShiftSheduledStart + "'," + //m_ShiftStart
                                                    "'" + dblEpoxShiftSheduledEnd + "'," + //m_ShiftEnd
                                                    "'" + m_ActualStart + "'," +
                                                    "'" + m_ActualEnd + "'," +
                                                    "'" + lWorkhours + "'," +
                                                    "'" + sMarkRoster + "'," +
                                                    "'" + sMarkLeave + "'," +
                                                    "'" + "" + "'," +
                                                    "'" + lWorkApproved + "'," +
                                                    "UNIX_TIMESTAMP()," +
                                                    "'" + iLateLoginStatus + "'," +
                                                    "'meterbox'" +
                                                      ");";
                                                //deletescript += "delete from " + MyGlobal.activeDB + ".tbl_attendance " +
                                                //"where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' " +
                                                //"and m_Date = '" + dblDt + "' " +
                                                //"and (m_Source='meterbox' or m_Source='' or m_Source is null);";
                                            }
                                        }
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
            //-----------------------Process any missed leaves
            sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters roster " +
                "left join " + MyGlobal.activeDB + ".tbl_leaves lev on lev.m_Profile = roster.m_Profile " +
                "and lev.m_StaffID = roster.m_StaffID and " +
                "lev.m_Year = roster.m_Year and lev.m_Month = roster.m_Month " +
                "where roster.m_Profile = '" + profile + "' and roster.m_StaffID = '" + staffid + "' " +
                "and ((roster.m_Year = '" + iYear + "' and roster.m_Month = '" + (iMonth - 1) + "') or " +
                "(roster.m_Year = '" + iYearTo + "' and roster.m_Month = '" + (iMonthTo - 1) + "'))";

            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            {
                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int year = reader.IsDBNull(5) ? 0 : reader.GetInt16(5);
                            int month = reader.IsDBNull(6) ? 0 : reader.GetInt16(6);
                            int iDay1 = reader.GetOrdinal("m_Day1") - 1;
                            int iDayL1 = reader.GetOrdinal("m_DayL1") - 2;
                            //-------------------------------Process for Leave boxes
                            for (int i = 1; i <= 31; i++)
                            {
                                double dblDt = 0;
                                try
                                {
                                    dblDt = ((new TimeSpan(new DateTime(year, (month + 1), i).Ticks)) - epochTicks).TotalSeconds;
                                }
                                catch (ArgumentOutOfRangeException)
                                {

                                }
                                double dblEpoxShiftSheduledStart = 0;
                                double dblEpoxShiftSheduledEnd = 0;
                                long lShiftStart = 0, lShiftEnd = 0, m_ActualStart = 0, m_ActualEnd = 0, lWorkhours = 0;
                                if ((dblDt >= dblDtFrom) && (dblDt <= dblDtTo) && dblDt < unixTimestamp)
                                {
                                    try
                                    {
                                        string sMarkRoster = "", sMarkLeave = "";
                                        if (!reader.IsDBNull(iDay1 + i))
                                        {
                                            sMarkRoster = reader.GetString(iDay1 + i);
                                        }
                                        if (!reader.IsDBNull(iDayL1 + (i * 2)) &&
                                            !reader.IsDBNull(iDayL1 + (i * 2) + 1)
                                            )
                                        {
                                            if (reader.GetString(iDayL1 + (i * 2)).Length > 0)
                                            {
                                                if ((reader.GetInt16(iDayL1 + (i * 2) + 1) == C_APPROVED) ||
                                                    (reader.GetInt16(iDayL1 + (i * 2) + 1) == C_REVOKE_PENDING))
                                                {
                                                    sMarkLeave = reader.GetString(iDayL1 + (i * 2));
                                                }
                                            }
                                        }
                                        if ((sMarkLeave.Length > 0))// || sMarkRoster.Length > 0
                                        {
                                            bool bAllow = false;
                                            if (_dayfilter.ContainsKey(i))
                                            {
                                                //MyGlobal.Error("Duplicate entry - " + sMarkRoster + ", " + sMarkLeave);
                                            }
                                            else
                                            {
                                                _dayfilter.Add(i, sMarkRoster + " * " + sMarkLeave);
                                                bAllow = true;
                                            }
                                            if (bAllow)
                                            {

                                                int iOrdShiftStartTime = reader.GetOrdinal("m_ShiftStartTime");

                                                lShiftStart = (reader.IsDBNull(iOrdShiftStartTime) ? 0 : reader.GetInt32(iOrdShiftStartTime));
                                                lShiftEnd = (reader.IsDBNull(iOrdShiftStartTime) ? 0 : reader.GetInt32(iOrdShiftStartTime + 1));
                                                m_ActualStart = 0;
                                                m_ActualEnd = 0;
                                                lWorkhours = 0;

                                                double epochShiftStart = dblDt + lShiftStart - const_ShiftPaddingPre + 0;
                                                double epochShiftEnd = dblDt + lShiftEnd + const_ShiftPaddingPost + 0;
                                                long lWorkApproved = 0;
                                                int iLateLoginStatus = 0;
                                                GetStaffWorkHours_with_Start_and_End(profile,
                                                    (long)epochShiftStart, //const_ShiftPaddingPre
                                                    (long)epochShiftEnd, //const_ShiftPaddingPost
                                                    staffid,
                                                     out lWorkhours, out lWorkApproved,
                                                     out m_ActualStart, out m_ActualEnd,
                                                     out iLateLoginStatus);

                                                dblEpoxShiftSheduledStart = dblDt + lShiftStart;
                                                dblEpoxShiftSheduledEnd = dblDt + lShiftEnd;
                                                //---------------------------
                                                sInsert += "insert into " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "(m_Profile,m_StaffID,m_Year,m_Month,m_Date,m_RosterName,m_ShiftName,m_ShiftStart," +
                                                    "m_ShiftEnd,m_ActualStart,m_ActualEnd,lWorkhours,m_MarkRoster,m_MarkLeave,m_RosterOptions," +
                                                    "m_WorkApproved,m_AsOn,m_LateLoginStatus) values (" +
                                                    "'" + profile + "'," + "'" + staffid + "'," +
                                                    "'" + iYearName + "'," + "'" + (iMonthName - 1) + "'," +
                                                    "'" + dblDt + "'," +
                                                    "'" + (reader.IsDBNull(1) ? "" : reader.GetString(1)) + "'," +  //m_RosterName
                                                    "'" + (reader.IsDBNull(2) ? "" : reader.GetString(2)) + "'," + //m_ShiftName
                                                    "'" + dblEpoxShiftSheduledStart + "'," + //m_ShiftStart
                                                    "'" + dblEpoxShiftSheduledEnd + "'," + //m_ShiftEnd
                                                    "'" + m_ActualStart + "'," +
                                                    "'" + m_ActualEnd + "'," +
                                                    "'" + lWorkhours + "'," +
                                                    "'" + sMarkRoster + "'," +
                                                    "'" + sMarkLeave + "'," +
                                                    "'" + "" + "'," +
                                                    "'" + lWorkApproved + "'," +
                                                    "UNIX_TIMESTAMP()," +
                                                    "'" + iLateLoginStatus + "'" +
                                                      ");";
                                                //deletescript += "delete from " + MyGlobal.activeDB + ".tbl_attendance " +
                                                //"where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' " +
                                                //"and m_Date = '" + dblDt + "' ";// +
                                                //"and (m_Source='meterbox' or m_Source='' or m_Source is null);";

                                            }
                                        }
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
            return sInsert;
        }
        private string GetLeaveMarkerForThisDay(string profile, string staffid, int year, int month, int iDay)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT m_DayL" + iDay + ",m_Status" + iDay + " FROM " + MyGlobal.activeDB + ".tbl_leaves " +
"where m_Profile = '" + profile + "' and m_StaffID = '" + staffid + "' " +
"and m_Year = '" + year + "' and m_Month = '" + month + "' limit 1;";

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
                                    int iStatus = reader.GetInt16(1);
                                    if ((iStatus == C_APPROVED) || (iStatus == C_REVOKE_PENDING))
                                    {
                                        if (reader.GetString(0).Length > 0)
                                        {
                                            return reader.GetString(0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return "";
            }
        }
        //---------------------------------
        /*
         Month comes here as JAN=1 indexed
         have to move it to previous month 26th
         -1 for DB
         */
        [HttpPost]
        public ActionResult PayslipSettlement(
            string profile, string email,
            string loginstaffid,
            string mode, string staffid, string staffemail, string yearName, string monthName,
            string ReleaseDay,
            string ledName, string ledType, string ledAmount, string addled, Boolean releasebonus)
        {

            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            int iYearName = MyGlobal.GetInt16(yearName);
            int iMonthName = MyGlobal.GetInt16(monthName);
            /*
            iMonth--;
            if (iMonth < 1)
            {
                iMonth = 12;
                iYear--;
                iYear--;
            }
            */
            var payslipSettlement = new PayslipSettlement();
            payslipSettlement.status = false;
            payslipSettlement.result = "";
            string sSQL = "",sqlBonusReleaseMarker="";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //---------------------------------
                    if (mode.Equals("approve"))
                    {
                        bool bAlreadyExists = false;
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bAlreadyExists = reader.HasRows;
                            }
                        }
                        if (bAlreadyExists)
                        {
                            payslipSettlement.result = "Payslip already exists";
                        }
                        else
                        {
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            Int32 iVchNo_ForUpdate = 0;
                            try
                            {
                                //---------------Account approval process
                                // Get then bonus of this month/consider if there are multiple payslips
                                double dblActiveMonthBonus = 0;
                                int iRound = 0;
                                if (releasebonus)
                                {
                                    while (true)
                                    {
                                        iRound++;
                                        LoadPayslip loadPayslip =
                                            this.GetPayslipFrom_Attendance_And_PayscaleMaster(profile, staffemail, "",
                                            iYearName.ToString(), (iMonthName).ToString(), iRound.ToString());
                                        if (loadPayslip.result.Length > 0 || iRound > 6)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                           
                                        }
                                    }
                                }
                                //---------------
                                iRound = 0;
                                while (true)
                                {
                                    iRound++;
                                    LoadPayslip loadPayslip =
                                        this.GetPayslipFrom_Attendance_And_PayscaleMaster(profile, staffemail, "",
                                        iYearName.ToString(), (iMonthName).ToString(), iRound.ToString());
                                    if (loadPayslip.result.Length > 0 || iRound > 6)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                                        //  Added Oct,2021 after bonus release module
                                        if (releasebonus && iRound==1)
                                        {
                                            double amtPendingMonths = 0;


                                            foreach (var payLedger in loadPayslip.deducts)
                                            {
                                                if (payLedger.Name.Equals("Bonus Accrued"))
                                                {
                                                    dblActiveMonthBonus += payLedger.Amount;

                                                    sqlBonusReleaseMarker +=
                                                              "update meterbox.tbl_accounts Set m_ReleaseVoucherarker='" + iVchNo + "' " +
                                  "where m_Profile='support@SharewareDreams.com' and m_Ledger='Bonus Accrued' and m_Head='" + staffid + "' " +
                                  "and m_Year='" + loadPayslip.iYear + "' and m_Month='" + (loadPayslip.iMonth-1) + "' " +
                                  "and (m_Year*12+m_Month)>=24249;";

                                                }
                                            }



                                            /*
                                            double amtThismonth = 0;
                                            foreach (var payLedger in loadPayslip.deducts)
                                            {
                                                if (payLedger.Name.Equals("Bonus Accrued"))
                                                {
                                                    amtThismonth = payLedger.Amount;
                                                }
                                            }
                                            */
                                            //-------------Get pending Bonus to be release till this month
                                            sSQL = "select (m_Cr),(m_Dr),m_Year,m_Month from " + MyGlobal.activeDB + ".tbl_accounts where " +
                                            "m_Profile='" + profile + "' and m_Ledger='Bonus Accrued' and m_Head='" + loadPayslip.staffid + "' " +
                                            "and m_ReleaseVoucherarker is null "+
                                            "and (m_Year*12+m_Month)>=24249;";
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
                                                                amtPendingMonths += reader.GetDouble(0) - reader.GetDouble(1);
                                                                sqlBonusReleaseMarker += 
                                                                "update meterbox.tbl_accounts Set m_ReleaseVoucherarker='" + iVchNo + "' " +
                                    "where m_Profile='support@SharewareDreams.com' and m_Ledger='Bonus Accrued' and m_Head='" + staffid + "' " +
                                    "and m_Year='"+ reader.GetInt32(2) + "' and m_Month='" + reader.GetInt32(3) + "' " +
                                    "and (m_Year*12+m_Month)>=24249;";
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            PayLedger led = new PayLedger();
                                            led.Name = "Annual Bonus Credit";//2
                                            led.Amount = amtPendingMonths + dblActiveMonthBonus;
                                            loadPayslip.earns.Add(led);
                                            loadPayslip.m_EarnsTot += led.Amount;

                                            //if (iRound == 1)// When you have multiple pages, apply thismarker only on the first page
                                            //{
                                                iVchNo_ForUpdate = iVchNo;
                                            //}
                                        }

                                        //----------------Create if accouting ledgers are not available
                                        myCommand.CommandText =
                                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                                        "select * FROM (select '" + profile + "', '" + loadPayslip.staffid + "','Salary') AS tmp " +
                                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                                        "where m_Name = '" + loadPayslip.staffid + "') LIMIT 1;";
                                        myCommand.ExecuteNonQuery();
                                        //----------------Create Payslip details

                                        double m_CrTot = 0, m_DrTot = 0, m_EarnsTot = 0, m_DeductsTot = 0;
                                        
                                        //-----------------Ledgers
                                        string sAccountsDescription = "Salary Credit. ";
                                        if (loadPayslip.Pages > 1)
                                            sAccountsDescription += " (" + iRound + "/" + loadPayslip.Pages + ") ";

                                        sAccountsDescription += constArrayMonths[(iMonthName - 1)] + ", " + iYearName;
                                        //--------------------Bonus marker

                                        String sInsert = "";
                                        foreach (PayLedger ledger in loadPayslip.ratesCr)
                                        {
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips " +
                                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_VchNo) values " +
                                                "('" + profile + "','" + loadPayslip.staffid + "'," +
                                                "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + ledger.Name + "','" + System.Math.Round(ledger.Amount, 2) + "','" + "cr" + "','" + iVchNo + "');";
                                            m_CrTot += System.Math.Round(ledger.Amount, 2);
                                        }
                                        foreach (PayLedger ledger in loadPayslip.deductsDr)
                                        {
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips " +
                                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_VchNo) values " +
                                                "('" + profile + "','" + loadPayslip.staffid + "'," +
                                                "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + ledger.Name + "','" + System.Math.Round(ledger.Amount, 2) + "','" + "dr" + "','" + iVchNo + "');";
                                            m_DrTot += System.Math.Round(ledger.Amount, 2);
                                        }
                                        foreach (PayLedger ledger in loadPayslip.earns)
                                        {
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips " +
                                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_VchNo) values " +
                                                "('" + profile + "','" + loadPayslip.staffid + "'," +
                                                "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + ledger.Name + "','" + System.Math.Round(ledger.Amount, 2) + "','" + "earn" + "','" + iVchNo + "');";
                                            //------------Add to accounts
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                            "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                            "('" + profile + "','" + loadPayslip.staffid + "',Now()," +
                                            "'" + System.Math.Round(ledger.Amount, 2) + "',0," +
                                            "'" + ledger.Name + "'," +
                                            "'" + sAccountsDescription + "'," +
                                            "'" + loadPayslip.iYear + "','" + (loadPayslip.iMonth - 1) + "','" + loadPayslip.staffid + "','" + iVchNo + "');";

                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                            "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                            "('" + profile + "','" + ledger.Name + "',Now()," +
                                            "0,'" + System.Math.Round(ledger.Amount, 2) + "'," +
                                            "'" + loadPayslip.staffid + "'," +
                                            "'" + sAccountsDescription + "'," +
                                            "'" + loadPayslip.iYear + "','" + (loadPayslip.iMonth - 1) + "','" + loadPayslip.staffid + "','" + iVchNo + "');";
                                            //------------Add to accounts END
                                            m_EarnsTot += System.Math.Round(ledger.Amount, 2);
                                        }
                                        foreach (PayLedger ledger in loadPayslip.deducts)
                                        {
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips " +
                                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_VchNo) values " +
                                                "('" + profile + "','" + loadPayslip.staffid + "'," +
                                                "'" + iYearName + "','" + (iMonthName - 1) + "'," +
                                                "'" + ledger.Name + "','" + System.Math.Round(ledger.Amount, 2) + "','" + "deduct" + "','" + iVchNo + "');";

                                            //------------Add to accounts
                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                            "('" + profile + "','" + loadPayslip.staffid + "',Now()," +
                                            "'" + System.Math.Round(ledger.Amount, 2) + "',0," +
                                            "'" + ledger.Name + "'," +
                                            "'" + sAccountsDescription + "'," +
                                            "'" + loadPayslip.iYear + "','" + (loadPayslip.iMonth - 1) + "','" + loadPayslip.staffid + "','" + iVchNo + "');";

                                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                            "('" + profile + "','" + ledger.Name + "',Now()," +
                                            "0,'" + System.Math.Round(ledger.Amount, 2) + "'," +
                                            "'" + loadPayslip.staffid + "'," +
                                            "'" + sAccountsDescription + "'," +
                                            "'" + loadPayslip.iYear + "','" + (loadPayslip.iMonth - 1) + "','" + loadPayslip.staffid + "','" + iVchNo + "');";
                                            //------------Add to accounts END
                                            m_DeductsTot += System.Math.Round(ledger.Amount, 2);
                                        }

                                        //----------------------Create List
                                        sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips_list " +
                                            "(m_Profile,m_StaffID,m_Year,m_Month,m_CreatedBy,m_CreatedTime," +
                                            "m_WorkingDays,m_OFFs," +
                                            "m_Leaves,m_ALOPs,m_LOPs,m_LateSeconds,m_LopBasedOnDelay,m_ActualWorkingDays,m_DaysToBePaidTotal," +
                                            "m_RosterOptions,m_RosterOptionsResult," +
                                            "m_Email,m_PayscaleName,m_Name," +
                                            "m_Band,m_Grade,m_Designation,m_Team,m_epf_uan,m_sb_acc," +
                                            "m_CTC,m_MonthName,m_DateStart,m_DateEnd," +
                                            "m_CrTot,m_DrTot,m_EarnsTot,m_DeductsTot," +
                                            "m_PayscaleKey,m_PayscaleStartDate,m_Bank,m_VchNo," +
                                            "m_GrossWages,m_BasicPay,m_EPFContributionRemitted,m_ESIC,m_ProfessionalTax) values (" +
                                            "'" + profile + "'," +
                                            "'" + loadPayslip.staffid + "'," +
                                            "'" + loadPayslip.iYear + "'," +
                                            "'" + (loadPayslip.iMonth - 1) + "'," +
                                            "'" + loginstaffid + "'," +
                                            "Now()," +
                                            "'" + loadPayslip.m_WorkingDays + "'," +
                                            "'" + loadPayslip.m_OFFs + "'," +
                                            "'" + loadPayslip.m_Leaves + "'," +
                                            "'" + loadPayslip.m_ALOPs + "'," +
                                            "'" + loadPayslip.m_LOPs + "'," +
                                            "'" + loadPayslip.m_LateSeconds + "'," +
                                            "'" + loadPayslip.m_LopBasedOnDelay + "'," +
                                            "'" + loadPayslip.m_ActualWorkingDays + "'," +
                                            "'" + loadPayslip.m_DaysToBePaidTotal + "'," +
                                            "'" + loadPayslip.m_RosterOptions + "'," +
                                            "'" + loadPayslip.m_RosterOptionsResult + "'," +
                                            "'" + loadPayslip.email + "'," +
                                            "'" + loadPayslip.m_PayscaleName + "'," +
                                            "'" + loadPayslip.name + "'," +
                                            "'" + loadPayslip.band + "'," +
                                            "'" + loadPayslip.grade + "'," +
                                            "'" + loadPayslip.designation + "'," +
                                            "'" + loadPayslip.team + "'," +
                                            "'" + loadPayslip.epf_uan + "'," +
                                            "'" + loadPayslip.sb_acc + "'," +
                                            "'" + loadPayslip.CTC + "'," +
                                            "'" + loadPayslip.sMonth + "'," +
                                            "'" + loadPayslip.m_DateStart + "'," +
                                            "'" + loadPayslip.m_DateEnd + "'," +
                                            "'" + System.Math.Round(loadPayslip.m_CrTot, 2) + "'," +
                                            "'" + System.Math.Round(loadPayslip.m_DrTot, 2) + "'," +
                                            "'" + System.Math.Round(loadPayslip.m_EarnsTot, 2) + "'," +
                                            "'" + System.Math.Round(loadPayslip.m_DeductsTot, 2) + "'," +
                                            "'" + loadPayslip.m_PayscaleKey + "'," +
                                            "'" + loadPayslip.m_PayscaleStartDate + "'," +
                                            "'" + loadPayslip.m_Bank + "'," +
                                            "'" + iVchNo + "'," +
                                            "'" + Math.Round(loadPayslip.m_GrossSalary, 2) + "'," +
                                            "'" + Math.Round(loadPayslip.m_BasicPay, 2) + "'," +
                                            "'" + Math.Round(loadPayslip.m_EPFContributionRemitted, 2) + "'," +
                                            "'" + Math.Round(loadPayslip.m_ESIC, 2) + "'," +
                                            "'" + Math.Round(loadPayslip.m_ProfessionalTax, 2) + "'" +
                                            ");";

                                        //---------------------Update all
                                        if (sInsert.Length > 0)
                                        {
                                            myCommand.CommandText = sInsert;
                                            myCommand.ExecuteNonQuery();
                                        }
                                        

                                        //----------------Mark approval
                                        string level = "3";
                                        myCommand.CommandText = "update " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                                        "Set m_ApprovedBy" + level + "='" + loginstaffid + "'," +
                                        "m_FundsReleaseDate='" + ReleaseDay + "'," +
                                        "m_ApprovedByTime" + level + "=Now() " +
                                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                        "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                                        myCommand.ExecuteNonQuery();
                                        //---------------------------------------
                                        string pay_session = "pay_" + staffid + "_" + iYearName + "_" + (iMonthName - 1);
                                        myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
    "(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
    "('" + profile + "','" + staffid + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
    "'" + pay_session + "','" + email + "','" + email + "',Now()," +
    "'Accounts approval.');";
                                        myCommand.ExecuteNonQuery();
                                        //----------------------------------------
                                        payslipSettlement.result = "Approved";

                                        //----------------Move to accounts
                                    }
                                    /*
                                    if (loadPayslip.Pages == 0) break;
                                    if (loadPayslip.Pages == 1) break;
                                    if (loadPayslip.Pages >= 1)
                                    {
                                        if (iRound >= loadPayslip.Pages) break;
                                    }
                                    */
                                } // while end


                                //if (iRound == 1)// When you have multiple pages, apply thismarker only on the first page
                                //{
                                /*
                                if (iVchNo_ForUpdate > 0)
                                {
                                    myCommand.CommandText =
                                    "update meterbox.tbl_accounts Set m_ReleaseVoucherarker='" + iVchNo_ForUpdate + "' " +
                                    "where m_Profile='support@SharewareDreams.com' and m_Ledger='Bonus Accrued' and m_Head='" + staffid + "' " +
                                    "and (m_Year*12+m_Month)>=24249;";
                                    myCommand.ExecuteNonQuery();
                                }*/
                                //}
                                if (sqlBonusReleaseMarker.Length > 0)
                                {
                                    myCommand.CommandText = sqlBonusReleaseMarker;
                                    myCommand.ExecuteNonQuery();
                                }
                                
                                myTrans.Commit();
                            }
                            catch (Exception e)
                            {
                                MyGlobal.Error("payslipSettlement-Approval-" + e.Message);
                                payslipSettlement.result = "Error-" + e.Message;
                                try
                                {
                                    myTrans.Rollback();
                                }
                                catch (MySqlException ex)
                                {
                                    MyGlobal.Error("payslipSettlement-Approval-Rollback-" + e.Message);
                                }
                            }
                            finally
                            {
                                //myConnection.Close();
                            }
                        }
                    }
                    else if (mode.Equals("cancel"))
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            //----------------Get the concern voucher No
                            int iPages = 0;
                            string sErrMessage = "";
                            Int32[] VchNo = new int[6] { 0, 0, 0, 0, 0, 0 };
                            sSQL = "select m_VchNo,m_List from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "' " +
                                "order by m_VchNo desc";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            for (int i = 0; i < 6; i++)
                                            {
                                                if (VchNo[i] == 0)
                                                {
                                                    VchNo[i] = reader.GetInt16(0);
                                                    iPages++;
                                                    break;
                                                }
                                            }
                                            //if (VchNo[0] == 0) VchNo[0] = reader.GetInt16(0);
                                            //else if (VchNo[1] == 0) VchNo[1] = reader.GetInt16(0);
                                        }
                                        if (!reader.IsDBNull(1))
                                        {
                                            if (reader.GetString(1).Length > 0)
                                            {
                                                sErrMessage += "Can't do. Exists in List '" + reader.GetString(1) + "' of " +
                                                    MyGlobal.constArrayMonths[iMonthName] + "/" + iYearName + ". ";
                                            }
                                        }
                                    }
                                }
                            }
                            if (sErrMessage.Length == 0)
                            {
                                //------------Revert accounts
                                string sInsert = "";

                                for (int idx = 0; idx < 6; idx++)
                                {
                                    if (VchNo[idx] > 0)
                                    {
                                        string sAccountsDescription = "Salary REVERSED ";
                                        sAccountsDescription += "(" + (idx + 1) + "/" + iPages + ") ";
                                        sAccountsDescription += constArrayMonths[(iMonthName - 1)] + ", " + iYearName + ".";

                                        string sSQL_List = "select * from " + MyGlobal.activeDB + ".tbl_accounts " +
                                            "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "' " +
                                            "and m_VchNo='" + VchNo[idx] + "'";
                                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL_List, con))
                                        {
                                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    string sLedger = "", sHead = "";
                                                    double dblCr = 0, dblDr = 0;
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger"))) sLedger = reader["m_Ledger"].ToString();
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Cr"))) dblCr = reader.GetDouble(reader.GetOrdinal("m_Cr"));
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Dr"))) dblDr = reader.GetDouble(reader.GetOrdinal("m_Dr"));
                                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Head"))) sHead = reader.GetString(reader.GetOrdinal("m_Head"));

                                                    sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                    "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo,m_Reversed) values " +
                                                    "('" + profile + "','" + sLedger + "',Now()," +
                                                    "'" + dblCr + "','" + dblDr + "','" + sHead + "','" + sAccountsDescription + "'," +
                                                    "'" + iYearName + "','" + (iMonthName - 1) + "','" + staffid + "','" + VchNo[idx] + "',true);";
                                                }
                                            }
                                        }
                                        //-------------------------
                                        sInsert +=
                                        "update meterbox.tbl_accounts Set m_ReleaseVoucherarker=null " +
"where m_Profile='support@SharewareDreams.com' and m_Ledger='Bonus Accrued' and m_Head='" + staffid + "' " +
"and m_ReleaseVoucherarker='" + VchNo[idx] + "' " +
"and (m_Year*12+m_Month)>=24249;";

                                    }
                                }
                                myCommand.CommandText = sInsert;
                                int iRet = myCommand.ExecuteNonQuery();
                                //-----------------Revert Bonus release marker
                                /*
                                myCommand.CommandText =
"update meterbox.tbl_accounts Set m_ReleaseVoucherarker=null " +
"where m_Profile='support@SharewareDreams.com' and m_Ledger='Bonus Accrued' and m_Head='" + staffid + "' " +
"and (m_Year*12+m_Month)>=24249;";
                                myCommand.ExecuteNonQuery();
                                */
                                //------------------------------------
                                sSQL = "update " + MyGlobal.activeDB + ".tbl_attendance_summary Set m_ApprovedBy3=null,m_ApprovedByTime3=null where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                                sSQL += "delete from " + MyGlobal.activeDB + ".tbl_payslips_list where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                                sSQL += "delete from " + MyGlobal.activeDB + ".tbl_payslips where m_Profile='" + profile + "' and m_StaffID = '" + staffid + "' and m_Year = '" + iYearName + "' and m_Month = '" + (iMonthName - 1) + "';";
                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();
                                //---------------------------------------
                                string pay_session = "pay_" + staffid + "_" + iYearName + "_" + (iMonthName - 1);
                                myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
"(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
"('" + profile + "','" + staffid + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
"'" + pay_session + "','" + email + "','" + email + "',Now()," +
"'Accounts approval CANCELLED. Ledgers Reversed');";
                                //----------------------------------------
                                myCommand.ExecuteNonQuery();
                                myTrans.Commit();
                                payslipSettlement.result = "Ledger REVERSED";
                            }
                            else
                            {
                                payslipSettlement.result = sErrMessage;
                            }
                        }
                        catch (Exception e)
                        {
                            payslipSettlement.result = "Error-" + e.Message;
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (MySqlException ex)
                            {
                            }
                        }
                        finally
                        {
                            //myConnection.Close();
                        }
                    }
                    else if (mode.Equals("addpayslip"))
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            //----------------Create if accouting ledgers are not available
                            /*
                            myCommand.CommandText =
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                            "select * FROM (select '" + profile + "', '" + ledName + "','AddPayslip') AS tmp " +
                            "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                            "where m_Name = '" + ledName + "') LIMIT 1;";
                            */
                            myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips_addledgers (m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type) " +
                            "select * FROM (select '" + profile + "','" + staffid + "','" + iYearName + "','" + (iMonthName - 1) + "','" + ledName + "','" + ledAmount + "','" + ledType + "') AS tmp " +
                            "where NOT EXISTS(SELECT m_Ledger FROM " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                            "where m_Ledger = '" + ledName + "' and m_StaffID='" + staffid + "' and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "') LIMIT 1;";
                            int ret = myCommand.ExecuteNonQuery();
                            //--------------------------------
                            if (ret == 0)
                            {
                                payslipSettlement.result = "Unable to add";
                            }
                            else
                            {
                                payslipSettlement.result = "";
                            }
                            myTrans.Commit();
                        }
                        catch (Exception e)
                        {
                            payslipSettlement.result = "Error-" + e.Message;
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (MySqlException ex)
                            {
                            }
                        }
                        finally
                        {
                            //myConnection.Close();
                        }
                    }
                    else if (mode.Equals("deleteaddpayslip"))
                    {
                        if (addled != null)
                        {
                            sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                            "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                            "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "' " +
                            "and m_Ledger='" + addled + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    else if (mode.Equals("approveadmin"))
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            myCommand.CommandText = "update " + MyGlobal.activeDB + ".tbl_attendance_summary " +
"Set m_ApprovedBy4='" + loginstaffid + "'," +
"m_ApprovedByTime4=Now() " +
"where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
"and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                            myCommand.ExecuteNonQuery();
                            //---------------------------------------
                            string pay_session = "pay_" + staffid + "_" + iYearName + "_" + (iMonthName - 1);
                            myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
"(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
"('" + profile + "','" + staffid + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
"'" + pay_session + "','" + email + "','" + email + "',Now()," +
"'Admin Approved.');";
                            myCommand.ExecuteNonQuery();
                            //----------------------------------------
                            payslipSettlement.result = "Admin Approved";
                            myTrans.Commit();
                        }
                        catch (Exception e)
                        {
                            payslipSettlement.result = "Admin Approval Failed.[" + e.Message + "]";
                            myTrans.Rollback();

                        }
                    }
                    else if (mode.Equals("canceladmin"))
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            myCommand.CommandText = "update " + MyGlobal.activeDB + ".tbl_attendance_summary " +
"Set m_ApprovedBy4=null," +
"m_ApprovedByTime4=null " +
"where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
"and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                            myCommand.ExecuteNonQuery();
                            //---------------------------------------
                            string pay_session = "pay_" + staffid + "_" + iYearName + "_" + (iMonthName - 1);
                            myCommand.CommandText = "insert into " + MyGlobal.activeDB + ".tbl_messages " +
"(m_Profile,m_StaffID,m_Year,m_Month,m_Session,m_From,m_To,m_Time,m_Message) values " +
"('" + profile + "','" + staffid + "','" + iYearName + "','" + (iMonthName - 1) + "'," +
"'" + pay_session + "','" + email + "','" + email + "',Now()," +
"'Admin Approval Cancelled.');";
                            myCommand.ExecuteNonQuery();
                            //----------------------------------------
                            payslipSettlement.result = "Admin Approval Cancelled";
                            myTrans.Commit();
                        }
                        catch (Exception e)
                        {
                            payslipSettlement.result = "Admin Approval CANCEL Failed.[" + e.Message + "]";
                            myTrans.Rollback();

                        }
                    }
                    //---------------------------------
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                bool bFirstRun = false;
                                while (reader.Read())
                                {
                                    if (!bFirstRun)
                                    {
                                        payslipSettlement.m_ApprovedBy1 = reader.IsDBNull(15) ? "" : reader.GetString(15);
                                        payslipSettlement.m_ApprovedByTime1 = reader.IsDBNull(16) ? "" : reader.GetString(16);
                                        payslipSettlement.m_ApprovedBy2 = reader.IsDBNull(17) ? "" : reader.GetString(17);
                                        payslipSettlement.m_ApprovedByTime2 = reader.IsDBNull(18) ? "" : reader.GetString(18);
                                        payslipSettlement.m_ApprovedBy3 = reader.IsDBNull(19) ? "" : reader.GetString(19);
                                        payslipSettlement.m_ApprovedByTime3 = reader.IsDBNull(20) ? "" : reader.GetString(20);

                                        payslipSettlement.m_ApprovedBy4 = MyGlobal.GetPureString(reader, "m_ApprovedBy4");
                                        payslipSettlement.m_ApprovedByTime4 = MyGlobal.GetPureString(reader, "m_ApprovedByTime4");


                                        payslipSettlement.m_FundsReleaseDate = reader.IsDBNull(26) ? "" : reader.GetString(26);
                                        payslipSettlement.ExistsInList = MyGlobal.GetFieldFromTable(profile, "tbl_payslips_list", "m_List",
                                            "and m_StaffID='" + staffid + "' and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "'");
                                        bFirstRun = true;
                                    }
                                    PayslipSettlementPart part = new PayslipSettlementPart();
                                    part.m_WorkingDays = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                                    part.m_OFFs = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);
                                    part.m_Leaves = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);
                                    part.m_ALOPs = reader.IsDBNull(7) ? 0 : reader.GetDouble(7);
                                    part.m_LOPs = reader.IsDBNull(8) ? 0 : reader.GetDouble(8);
                                    part.m_LateSeconds = reader.IsDBNull(9) ? 0 : reader.GetDouble(9);
                                    part.m_LOPBasedOnDelay = reader.IsDBNull(10) ? 0 : reader.GetDouble(10);
                                    part.m_ActualWorkingDays = reader.IsDBNull(11) ? 0 : reader.GetDouble(11);
                                    part.m_DaysToBePaidTotal = reader.IsDBNull(12) ? 0 : reader.GetDouble(12);
                                    part.m_RosterOptions = reader.IsDBNull(13) ? "" : reader.GetString(13);
                                    part.m_RosterOptionsResult = reader.IsDBNull(14) ? "" : reader.GetString(14);

                                    part.m_DateStart = reader.IsDBNull(22) ? 0 : reader.GetInt32(22);
                                    part.m_DateEnd = reader.IsDBNull(23) ? 0 : reader.GetInt32(23);

                                    part.m_PayscaleName = MyGlobal.GetPureString(reader, "pay_scale");
                                    part.m_PayscaleKey = MyGlobal.GetPureInt32(reader, "pay_key");
                                    part.m_PayscaleStartDate = MyGlobal.GetPureInt32(reader, "pay_startdate");
                                    payslipSettlement.parts.Add(part);
                                }
                            }
                        }
                    }
                    //-------------Add if any additional ledgers
                    sSQL = "select m_Ledger,m_Type,m_Amount,m_Security from " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                        "and m_Year='" + iYearName + "' and m_Month='" + (iMonthName - 1) + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    PayLedger led = new PayLedger();
                                    led.Name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    if (led.Name.Length > 0)
                                    {
                                        led.Type = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        led.Amount = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                        led.m_Security = reader.IsDBNull(3) ? 0 : reader.GetInt16(3);
                                        payslipSettlement.addLedgers.Add(led);
                                    }
                                }
                            }
                        }
                    }
                    //-------------Add if any additional ledgers END
                    payslipSettlement.dblBonusFunds = 0;
                    for (int i = 0; i < 12; i++) payslipSettlement.m_BonusTable[i] = 0;
                    sSQL = "SELECT (m_Year*12+m_Month) as MonthsLast,sum(m_Cr)-sum(m_Dr) as amount,m_Year,m_Month,m_ReleaseVoucherarker " +
                   "FROM meterbox.tbl_accounts where m_Ledger = 'Bonus Accrued' " +
                   "and m_StaffID = '" + staffid + "' " +
                   "and m_ReleaseVoucherarker is null " +
                   //"and (m_Year * 12 + m_Month) > (" + (iYearName-1) + " * 12 + " + (iMonthName - 1) + ") " +
                   "and (m_Year*12+m_Month)>=24249 " +
                   "group by (m_Year * 12 + m_Month) order by (m_Year * 12 + m_Month)";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    payslipSettlement.BonusMonths++;// = (iYearName*12+ (iMonthName - 1))- reader.GetInt16(0);
                                    payslipSettlement.dblBonusFunds += reader.GetDouble(1);
                                    int iMonth = reader.GetInt16(3);
                                    payslipSettlement.m_BonusTable[iMonth] = reader.GetDouble(1);
                                    if (!reader.IsDBNull(4))
                                    {
                                        payslipSettlement.m_BonusTableRelease[iMonth] = reader.GetInt16(4);
                                        if (reader.GetInt16(4) > 0) payslipSettlement.m_BonusTableReleaseVoucher = reader.GetInt16(4);
                                    }
                                }
                            }
                        }
                    }


                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("payslipSettlement-MySqlException->" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("payslipSettlement-Exception->" + ex.Message);
            }

            return Json(payslipSettlement, JsonRequestBehavior.AllowGet);
        }
        //-------------------------------
        public ActionResult GetPayscales(string profile, string sort, string order,
    string page, string search, string timezone, string showoptions, string staffid,
    string mode, string value, string payscalename, string key)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var payscalesResponse = new PayscalesResponse();
            payscalesResponse.status = false;
            payscalesResponse.result = "";
            payscalesResponse.total_count = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    if (mode.Equals("new") && value.Length > 0)
                    {
                        bool bCreateNew = false;

                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                "where m_Profile='" + profile + "' and m_Name='" + value + "';";// and m_Key='" + key + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bCreateNew = !reader.HasRows;
                            }
                        }

                        if (!bCreateNew)
                        {
                            payscalesResponse.result = "<span style='color:red;'>Name already exists</span>";
                        }
                        else
                        {
                            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;
                            DateTime date = DateTime.Now;
                            Int32 unixTimeDayStart = (Int32)((new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            sSQL = "Insert into " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                "(m_Profile,m_Name,m_Key,m_CreatedBy,m_CreatedTime) values " +
                                "('" + profile + "','" + value + "','" + unixTimeDayStart + "'," +
                                "'" + staffid + "','" + unixTimestamp + "');";
                            //-------------Add few basic ledgers...
                            if (!MyGlobal.activeDomain.Equals("chchealthcare"))
                            {
                                sSQL +=
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Basic','cr','','10000','110','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','HRA','cr','Basic','60%','111','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Conveyance','cr','','1000','112','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','PF','dr','Basic','12%','113','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Food Allowance','cro','','1000','114','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','CTC','cr',null,'15000','115','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Basic','earn','','10000','110','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','HRA','earn','Basic','12%','111','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Conveyance','earn','','1000','112','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','PF','deduct','Gross Salary','12%','113','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','Food Allowance','earn','Gross Salary','12%','114','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','ESIC','dr','Gross Salary','4.75%','116','0','" + unixTimeDayStart + "');" +
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "(m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) values " +
                                    "('" + profile + "','" + value + "','ESIC','deduct','Gross Salary','4.75%','116','0','" + unixTimeDayStart + "');";
                            }
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {

                                mySqlCommand.ExecuteNonQuery();
                                payscalesResponse.result = "<span style='color:blue;'>New Payscale created</span>";
                                payscalesResponse.reload = true;
                            }
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (lst.m_Name like '%" + search + "%' or " +
                        "lst.m_Name like '%" + search + "%' or " +
                        "lst.m_Name like '%" + search + "%') ";

                    //sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payscale_master_list lst " +
                    //    "where " + sSearchKey + " and lst.m_Profile='" + profile + "' ";

                    sSQL = "select count(cnt) as cnt from (" +
                        "SELECT  count(lst.m_id) as cnt FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list lst " +
"left join " + MyGlobal.activeDB + ".tbl_payscale_master as mast on mast.m_Name=lst.m_Name and mast.m_Key=lst.m_Key and mast.m_Ledger='CTC' and mast.m_Profile=lst.m_Profile " +
"left join " + MyGlobal.activeDB + ".tbl_payslips_list paysliplist on paysliplist.m_Profile=lst.m_Profile and paysliplist.m_PayscaleName=lst.m_Name and paysliplist.m_PayscaleKey=lst.m_Key " +
"where " + sSearchKey + " and lst.m_Profile='" + profile + "' " +
"group by lst.m_Name ) as cnt";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) payscalesResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";

                    //if (sort.Equals("m_Ledger")sort = "lst.m_Ledger";
                    if (sort.Equals("m_Amount")) sort = "mast.m_Amount";
                    else sort = "lst." + sort;

                    if (mode.Equals("new") && value.Length > 0)
                    {
                        sort = "lst.m_CreatedTime";
                        order = "desc";
                        PAGE = 0;
                    }
                    /*
                    sSQL = "SELECT *,lst.m_Name as Name,max(lst.m_Key) as MaxKey FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list lst " +
"left join " + MyGlobal.activeDB + ".tbl_payscale_master as mast on mast.m_Name=lst.m_Name and mast.m_Key=lst.m_Key and mast.m_Ledger='CTC' and mast.m_Profile=lst.m_Profile " +
"where " + sSearchKey + " and lst.m_Profile='" + profile + "' " +
"group by lst.m_Name ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    */
                    //https://stackoverflow.com/questions/1313120/retrieving-the-last-record-in-each-group-mysql
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list lst " +
"left join " + MyGlobal.activeDB + ".tbl_payscale_master as mast on mast.m_Name=lst.m_Name and mast.m_Key=lst.m_Key and mast.m_Ledger='CTC' and mast.m_Profile=lst.m_Profile " +
                        "where " + sSearchKey + " and " +
                    "NOT EXISTS( " +
                    "   SELECT * FROM meterbox.tbl_payscale_master_list as M2 " +
                    "   WHERE M2.m_Name = lst.m_Name " +
                    "   AND M2.m_id > lst.m_id " +
                    ") ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    PayscaleItem item = new PayscaleItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) item.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    /*
                                                                        if (!reader.IsDBNull(reader.GetOrdinal("Name"))) item.m_Name = reader["Name"].ToString();
                                                                        int ord = reader.GetOrdinal("MaxKey"); //m_Key
                                                                        if (!reader.IsDBNull(ord))
                                                                            item.m_Key = reader.GetInt32(ord);
                                                                            */
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name"))) item.m_Name = reader["m_Name"].ToString();
                                    int ord = reader.GetOrdinal("m_Key"); //m_Key
                                    if (!reader.IsDBNull(ord))
                                        item.m_Key = reader.GetInt32(ord);

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedBy"))) item.m_CreatedBy = reader["m_CreatedBy"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTime"))) item.m_CreatedTime = reader.GetInt32(reader.GetOrdinal("m_CreatedTime"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_UpdatedBy"))) item.m_UpdatedBy = reader["m_UpdatedBy"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_UpdatedTime"))) item.m_UpdatedTime = reader.GetInt32(reader.GetOrdinal("m_UpdatedTime"));
                                    //item.m_CTC = GetCTC(profile,item.m_Name);
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Amount"))) item.m_CTC = MyGlobal.GetDouble(reader.GetString(reader.GetOrdinal("m_Amount")));
                                    item.allowdelete = DoesThisPayscaleExistsInAnyLedger(profile, item.m_Name, "").Length == 0 ? 1 : 0;
                                    //reader.IsDBNull(reader.GetOrdinal("PayscaleName")) ? 1 : 0;
                                    payscalesResponse.items.Add(item);
                                }
                                payscalesResponse.status = true;
                            }
                            else
                            {
                                payscalesResponse.result = "Sorry!!! No Payscales";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetPayscales-MySqlException-" + ex.Message);
                payscalesResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetPayscales-Exception-" + ex.Message);
                payscalesResponse.result = "Error-" + ex.Message;
            }
            return Json(payscalesResponse, JsonRequestBehavior.AllowGet);
        }
        /*
        private double GetCTC(string profile,string payscalename)
        {
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_Amount FROM " + MyGlobal.activeDB + ".tbl_payscale_master " +
                    "where m_Profile='" + profile + "' and m_Name='" + payscalename + "' and m_Ledger='CTC' order by m_Key desc limit 1";
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
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("GetCTC-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("GetCTC-Exception-" + ex.Message);
            }
            return 0.0;
        }
        */
        public ActionResult GetPayscaleKeys(string profile, string sort, string order,
            string page, string search, string timezone, string showoptions, string staffid,
            string mode, string value, string payscalename, string key)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var payscalesResponse = new PayscalesResponse();
            payscalesResponse.status = false;
            payscalesResponse.result = "";
            payscalesResponse.total_count = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    if (mode.Equals("newkey"))
                    {
                        if (payscalename != null) // same name with new effective date
                        {
                            if (payscalename.Length > 0)
                            {
                                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;
                                DateTime date = DateTime.Now;
                                Int32 unixTimeDayStart = (Int32)((new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                //------------Get the last working master table--------------------------
                                string sLatestKey = "";
                                sSQL = "SELECT m_Key FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                "where m_Profile='" + profile + "' and m_Name='" + payscalename + "' " +
                                "order by m_Key desc limit 1"; //and m_Key='" + key + "' 

                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            if (reader.Read())
                                            {
                                                if (!reader.IsDBNull(0)) sLatestKey = reader.GetString(0);
                                            }
                                        }
                                    }
                                }
                                //-----------------------------------------------------------------------
                                bool bDateUsed = false;
                                sSQL = "SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                "where m_Profile='" + profile + "' and m_Name='" + payscalename + "' " +
                                "and m_Key='" + unixTimeDayStart + "' limit 1";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        bDateUsed = reader.HasRows;
                                    }
                                }
                                //-----------------------------------------------------------------------
                                if (bDateUsed)
                                {
                                    payscalesResponse.result = "<span style='color:red;'>Already you have a Payscale on this date</span>";
                                    payscalesResponse.reload = false;
                                }
                                else
                                {
                                    MySqlTransaction myTrans = con.BeginTransaction();
                                    MySqlCommand myCommand = con.CreateCommand();
                                    myCommand.Connection = con;
                                    myCommand.Transaction = myTrans;
                                    try
                                    {
                                        myCommand.CommandText =
                                            "Insert into " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                            "(m_Profile,m_Name,m_Key,m_CreatedBy,m_CreatedTime) " +
                                            "values ('" + profile + "','" + payscalename + "','" + unixTimeDayStart + "'," +
                                            "'" + staffid + "','" + unixTimestamp + "');";
                                        myCommand.ExecuteNonQuery();
                                        payscalesResponse.result = "<span style='color:blue;'>New Payscale created</span>";
                                        payscalesResponse.reload = true;

                                        //----------Copy the latest template
                                        if (sLatestKey.Length > 0)
                                        {
                                            myCommand.CommandText =
                                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_payscale_master (m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, m_Key) " +
                                            "SELECT m_Profile, m_Name, m_Ledger, m_Type, m_BasedOn, m_Amount, m_Order, m_PayMode, '" + unixTimeDayStart + "' FROM " + MyGlobal.activeDB + ".tbl_payscale_master where m_Profile = '" + profile + "' and m_Key = '" + sLatestKey + "'";
                                            myCommand.ExecuteNonQuery();
                                        }
                                        myTrans.Commit();
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
                                    }
                                    finally
                                    {
                                        //myConnection.Close();
                                    }
                                }
                            }
                        }
                    }
                    else if (mode.Equals("delete") && value.Length > 0) //value has the timestamp of the key, which is effective from
                    {
                        if (payscalename != null) // same name with new effective date
                        {
                            string sRet = DoesThisPayscaleExistsInAnyLedger(profile, payscalename, value);
                            if (sRet.Length == 0)
                            {
                                sSQL = "delete from " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                                    "where m_Profile='" + profile + "' and m_Name='" + payscalename + "' and m_Key='" + value + "';";
                                sSQL += "delete from " + MyGlobal.activeDB + ".tbl_payscale_master " +
                                    "where m_Profile='" + profile + "' and m_Name='" + payscalename + "' and m_Key='" + value + "'";

                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                    payscalesResponse.result = "<span style='color:blue;'>Paycale deleted</span>";
                                    payscalesResponse.reload = true;
                                }
                            }
                            else
                            {
                                payscalesResponse.result = "<span style='color:blue;'>" + sRet + "</span>";
                                payscalesResponse.reload = false;
                            }
                        }
                        else
                        {
                            payscalesResponse.result = "<span style='color:red;'>Sorry. Can't delete</span>";
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (payslips.m_Name like '%" + search + "%' or " +
                        "payslips.m_Name like '%" + search + "%' or " +
                        "payslips.m_Name like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payscale_master_list payslips " +
                        "where " + sSearchKey + " and payslips.m_Profile='" + profile + "' ";
                    if (payscalename != null)
                    {
                        if (payscalename.Length > 0)
                        {
                            sSQL += "and payslips.m_Name='" + payscalename + "' ";
                        }
                    }


                    sSQL = "SELECT count(cnt) as cnt  FROM (" +
                        "SELECT count(payslips.m_id) as cnt  FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list payslips " +
                    "left join " + MyGlobal.activeDB + ".tbl_payslips_list lst on lst.m_Profile = payslips.m_Profile and lst.m_PayscaleName = payslips.m_Name and lst.m_PayscaleKey = payslips.m_Key " +
                    "where " + sSearchKey + " and payslips.m_Profile='" + profile + "' ";
                    if (payscalename != null)
                    {
                        if (payscalename.Length > 0)
                        {
                            sSQL += "and payslips.m_Name='" + payscalename + "' ";
                        }
                    }
                    sSQL += "group by lst.m_PayscaleName,m_Key,payslips.m_id ";
                    sSQL += ") as xxx";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) payscalesResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "payslips.m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";

                    sSQL = "SELECT *,lst.m_PayscaleName as PayscaleName,payslips.m_Name as Name  FROM " + MyGlobal.activeDB + ".tbl_payscale_master_list payslips " +
                        "left join " + MyGlobal.activeDB + ".tbl_payslips_list lst on lst.m_Profile = payslips.m_Profile and lst.m_PayscaleName = payslips.m_Name and lst.m_PayscaleKey = payslips.m_Key " +
                        "where " + sSearchKey + " and payslips.m_Profile='" + profile + "' ";
                    if (payscalename != null)
                    {
                        if (payscalename.Length > 0)
                        {
                            sSQL += "and payslips.m_Name='" + payscalename + "' ";
                        }
                    }
                    sSQL += "group by lst.m_PayscaleName,m_Key,payslips.m_id ";
                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + " ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    PayscaleItem item = new PayscaleItem();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) item.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("Name"))) item.m_Name = reader["Name"].ToString();
                                    int ord = reader.GetOrdinal("m_Key");
                                    if (!reader.IsDBNull(ord))
                                        item.m_Key = reader.GetInt32(ord);

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedBy"))) item.m_CreatedBy = reader["m_CreatedBy"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTime"))) item.m_CreatedTime = reader.GetInt32(reader.GetOrdinal("m_CreatedTime"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_UpdatedBy"))) item.m_UpdatedBy = reader["m_UpdatedBy"].ToString();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_UpdatedTime"))) item.m_UpdatedTime = reader.GetInt32(reader.GetOrdinal("m_UpdatedTime"));
                                    item.allowdelete = reader.IsDBNull(reader.GetOrdinal("PayscaleName")) ? 1 : 0;
                                    payscalesResponse.items.Add(item);
                                }
                                payscalesResponse.status = true;
                            }
                            else
                            {
                                payscalesResponse.result = "Sorry!!! No Payscales";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                payscalesResponse.result = "Error-" + ex.Message;
            }
            return Json(payscalesResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult UpdateEffectiveDate(string profile,
            string email, string name, string key, string newkey)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------
                    string mess = "";
                    sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                        "where m_Profile='" + profile + "' " +
                        "and m_Name='" + name + "' " +
                        "and m_Key='" + newkey + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                mess = "You already have a Payscale on this date";
                            }
                        }
                    }
                    if (mess.Length > 0)
                    {
                        postResponse.result = mess;
                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    //-----------------------------
                    sSQL = "select m_StaffID,m_Year,m_Month,m_CreatedBy from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and m_PayscaleName='" + name + "' " +
                        "and m_PayscaleKey='" + key + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    mess = "You can't change. You already have a payslip " +
                                        "created for StaffID " + MyGlobal.GetPureString(reader, "m_StaffID") +
                                        " for the month of " + MyGlobal.GetPureString(reader, "m_Month") +
                                        "/" + MyGlobal.GetPureString(reader, "m_Year") +
                                        " by " + MyGlobal.GetPureString(reader, "m_CreatedBy");
                                }
                            }
                        }
                    }
                    if (mess.Length > 0)
                    {
                        postResponse.result = mess;
                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        myCommand.CommandText = "update  " + MyGlobal.activeDB + ".tbl_payscale_master_list " +
                        "set m_Key='" + newkey + "' where m_Profile='" + profile + "' and m_Name='" + name + "' " +
                        "and m_Key='" + key + "';";
                        myCommand.ExecuteNonQuery();

                        myCommand.CommandText = "update  " + MyGlobal.activeDB + ".tbl_payscale_master " +
                        "set m_Key='" + newkey + "' where m_Profile='" + profile + "' and m_Name='" + name + "' " +
                        "and m_Key='" + key + "';";
                        myCommand.ExecuteNonQuery();

                        myCommand.CommandText = "update  " + MyGlobal.activeDB + ".tbl_payscale_effective " +
                        "set m_Key='" + newkey + "' where m_Profile='" + profile + "' and m_Payscale='" + name + "' " +
                        "and m_Key='" + key + "';";
                        myCommand.ExecuteNonQuery();



                        myTrans.Commit();

                        postResponse.iParam1 = MyGlobal.GetInt32(newkey);
                        postResponse.result = "Updated";
                        postResponse.status = true;
                        return Json(postResponse, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        myTrans.Rollback();
                        postResponse.result = "Failed to update. [" + ex.Message + "]";
                    }
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPayscaleLedgers(string profile, string search,
    string activetab)
        {
            var payscaleLedgersResponse = new PayscaleLedgersResponse();
            payscaleLedgersResponse.status = false;
            payscaleLedgersResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (m_Ledger like '%" + search + "%' )";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_Ledger,m_PayMode FROM " +
                    "(select m_Ledger,m_PayMode,m_Profile,m_Type from " + MyGlobal.activeDB + ".tbl_payscale_master where m_Profile='" + profile + "' order by m_id desc) as x " +
                    "where " + sSearchKey + " ";
                    if (activetab.Equals("0")) sSQL += "and (m_Type='cr' or m_Type='cro') ";
                    else if (activetab.Equals("1")) sSQL += "and (m_Type='cr' or m_Type='cro') ";
                    else if (activetab.Equals("2")) sSQL += "and (m_Type='dr' or m_Type='dro') ";
                    else if (activetab.Equals("3")) sSQL += "and (m_Type='dr' or m_Type='dro') ";
                    sSQL += "group by m_Ledger order by m_Ledger;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                    {
                                        PayLedger item = new PayLedger();
                                        item.Name = reader["m_Ledger"].ToString();
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_PayMode")))
                                            item.paymode = reader.GetInt16(reader.GetOrdinal("m_PayMode"));
                                        payscaleLedgersResponse.sarLedgers.Add(item);
                                    }
                                }
                                payscaleLedgersResponse.status = true;
                            }
                            else
                            {
                                payscaleLedgersResponse.result = "Sorry!!! No devices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                payscaleLedgersResponse.result = "Error-" + ex.Message;
            }

            return Json(payscaleLedgersResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPayscaleAddLedgers(string profile, string search)
        {
            var payscaleLedgersResponse = new PayscaleLedgersResponse();
            payscaleLedgersResponse.status = false;
            payscaleLedgersResponse.result = "None";
            string sSQL = "";
            String sSearchKey = " (m_Ledger like '%" + search + "%' )";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    sSQL = "SELECT m_Ledger,m_Type,m_Amount,m_Security FROM " +
                    "(select m_Ledger,m_Amount,m_Profile,m_Type,m_Security from " + MyGlobal.activeDB + ".tbl_payslips_addledgers where m_Profile='" + profile + "' order by m_id desc) as x " +
                    "where " + sSearchKey + " ";
                    sSQL += "group by m_Ledger order by m_Ledger;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Ledger")))
                                    {
                                        PayLedger item = new PayLedger();
                                        item.Name = reader["m_Ledger"].ToString();
                                        item.Type = reader["m_Type"].ToString();
                                        item.Amount = reader.GetDouble(2);
                                        item.m_Security = reader.GetInt16(3);
                                        payscaleLedgersResponse.sarLedgers.Add(item);
                                    }
                                }
                                payscaleLedgersResponse.status = true;
                            }
                            else
                            {
                                payscaleLedgersResponse.result = "Sorry!!! No devices";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                payscaleLedgersResponse.result = "Error-" + ex.Message;
            }

            return Json(payscaleLedgersResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------------------------------------------
        public ActionResult GetMyPayslips(string profile, string staffid, string sort, string order,
            string page, string search)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var myPayslipsResponse = new MyPayslipsResponse();
            myPayslipsResponse.status = false;
            myPayslipsResponse.result = "";

            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    "where m_StaffID='" + staffid + "' and m_Profile='" + profile + "' ";
                    sSQL =
                    "select count(payslip.m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list payslip " +
                    "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary on summary.m_StaffID = payslip.m_StaffID and summary.m_ApprovedByTime4 is not null and summary.m_Profile = summary.m_Profile " +
                    "and summary.m_Month = payslip.m_Month and summary.m_Year = payslip.m_Year " +
                    "and summary.pay_scale=payslip.m_PayscaleName " +
                    "where payslip.m_StaffID = '" + staffid + "' and payslip.m_Profile = '" + profile + "' " +
                    "and summary.m_ApprovedByTime4 is not null ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) myPayslipsResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_DateStart";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";

                    //sSQL = "select *,unix_timestamp(m_CreatedTime) as m_CreatedTimeUnix from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    //"where m_StaffID='" + staffid + "' and m_Profile='" + profile + "' "

                    sSQL =
                    "select payslip.m_DaysToBePaidTotal,payslip.m_id,payslip.m_DateStart,payslip.m_DateEnd,payslip.m_Year,payslip.m_Month,payslip.m_MonthName," +
                    "payslip.m_WorkingDays,payslip.m_PayscaleName,payslip.m_CrTot,payslip.m_EarnsTot,payslip.m_DeductsTot," +
                    "unix_timestamp(m_CreatedTime) as m_CreatedTimeUnix,m_VchNo from " + MyGlobal.activeDB + ".tbl_payslips_list payslip " +
                    "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary on summary.m_StaffID = payslip.m_StaffID and summary.m_ApprovedByTime4 is not null and summary.m_Profile = summary.m_Profile " +
                    "and summary.m_Month = payslip.m_Month and summary.m_Year = payslip.m_Year " +
                    "and summary.pay_scale=payslip.m_PayscaleName " +
                    "where payslip.m_StaffID = '" + staffid + "' and payslip.m_Profile = '" + profile + "' " +
                    "and summary.m_ApprovedByTime4 is not null " +

                                       "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    MyPayslipRow row = new MyPayslipRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DateStart"))) row.m_DateStart = reader.GetInt32(reader.GetOrdinal("m_DateStart"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DateEnd"))) row.m_DateEnd = reader.GetInt32(reader.GetOrdinal("m_DateEnd"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Year"))) row.m_Year = reader.GetInt16(reader.GetOrdinal("m_Year"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Month"))) row.m_Month = reader.GetInt16(reader.GetOrdinal("m_Month")) + 1;
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_MonthName"))) row.m_MonthName = reader.GetString(reader.GetOrdinal("m_MonthName"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CreatedTimeUnix"))) row.m_CreatedTime = reader.GetInt32(reader.GetOrdinal("m_CreatedTimeUnix"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_WorkingDays"))) row.m_WorkingDays = reader.GetInt16(reader.GetOrdinal("m_WorkingDays"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_PayscaleName"))) row.m_PayscaleName = reader.GetString(reader.GetOrdinal("m_PayscaleName"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CrTot"))) row.m_CrTot = reader.GetInt32(reader.GetOrdinal("m_CrTot"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_EarnsTot"))) row.m_EarnsTot = reader.GetInt32(reader.GetOrdinal("m_EarnsTot"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DeductsTot"))) row.m_DeductsTot = reader.GetInt32(reader.GetOrdinal("m_DeductsTot"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DaysToBePaidTotal"))) row.m_DaysToBePaidTotal = reader.GetInt16(reader.GetOrdinal("m_DaysToBePaidTotal"));

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_VchNo"))) row.m_VchNo = reader.GetInt32(reader.GetOrdinal("m_VchNo"));

                                    row.bHasBonus = DoesThisVoucherHasBonusCreditEntry(row.m_VchNo);

                                    row.m_StaffID = staffid;

                                    myPayslipsResponse.items.Add(row);
                                }
                                myPayslipsResponse.status = true;
                                myPayslipsResponse.result = "Done";
                            }
                            else
                            {
                                myPayslipsResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                myPayslipsResponse.result = "Error-" + ex.Message;
            }
            return Json(myPayslipsResponse, JsonRequestBehavior.AllowGet);
        }
        public Boolean DoesThisVoucherHasBonusCreditEntry( int m_VchNo)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT m_id FROM meterbox.tbl_accounts where m_Ledger='Annual Bonus Credit' and m_VchNo=" + m_VchNo;
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        return reader.HasRows;

                    }
                }
            }
        }
        //-----------------------------
        //int iYearName, int iMonthName
        private void AttendanceRunChecker(string profile, string staffidsearch,
            int iYear, int iMonth, int iDay, int iYearTo, int iMonthTo, int iDayTo
            )
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sUpdateSQL = "";
                    //-------------------------------------------------------------
                    int iStartDate = 1;
                    string sSQL = "select m_AttnStartDate from " + MyGlobal.activeDB + ".tbl_profile_info where " +
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
                    double dlbDtStart = 0, dlbDtEnd = 0;
                    //GetFromToDates(iYearName, iMonthName, iStartDate, out dlbDtStart, out dlbDtEnd);
                    GetFromToDates(iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo, out dlbDtStart, out dlbDtEnd);
                    //---------------------Get it from attendance table
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_attendance " +
                        "where m_Profile='" + profile + "' " +
                        "and m_StaffID = '" + staffidsearch + "' " +
                        "and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "' " +
                        " order by m_Date;";

                    int iDayCounter = 0;
                    double[] dblPhysical = new double[124];
                    for (int i = 0; i < 124; i++) dblPhysical[i] = 0;

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int iWorkdayCount = 0;
                                //double dblPhysicalWorkDays = 0;
                                bool bLastPassWasAbsent = false;
                                while (reader.Read())
                                {
                                    iWorkdayCount++;
                                    Int32 m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    //dblPayDays += MyGlobal.GetPureDouble(reader, "dblDayTobePaid");
                                    //dblPhysicalWorkDays += MyGlobal.GetPureDouble(reader, "dblActualWorkingDays_Local");
                                    //dblPhysical[iDayCounter] = MyGlobal.GetPureDouble(reader, "dblActualWorkingDays_Local");
                                    dblPhysical[iDayCounter] = MyGlobal.GetPureDouble(reader, "dblDayTobePaid");
                                    string roster = MyGlobal.GetPureString(reader, "m_MarkRoster");
                                    if (roster.Equals("OFF"))
                                    {

                                        string Working = "", sShortage = "";
                                        //get the physical present days in this week (7 days)
                                        double dblPhysicalWorkDays = 0;
                                        int iWeekCnt = 0;
                                        for (int i = (iDayCounter - 1); i >= 0; i--)
                                        {
                                            dblPhysicalWorkDays += dblPhysical[i];
                                            iWeekCnt++;
                                            if (iWeekCnt >= 7) break;
                                        }
                                        //  WD WD WD WD WD WD OFF
                                        if ((iWorkdayCount < 7) && (dblPhysicalWorkDays < 3))
                                        {
                                            FillTheseManyDaysFromPreviousMonthToPhysicalWorkArray(profile, staffidsearch, iDayCounter, dblPhysical, dlbDtEnd);
                                            // Process for Physical workdays again
                                            iWeekCnt = 0;
                                            for (int i = (iDayCounter - 1); i >= 0; i--)
                                            {
                                                dblPhysicalWorkDays += dblPhysical[i];
                                                iWeekCnt++;
                                                if (iWeekCnt >= 7) break;
                                            }
                                        }
                                        //get the physical present days in this week (7 days) END
                                        bool bMarkNoPay = false;
                                        if (dblPhysicalWorkDays < 3 && iWorkdayCount > 3)
                                        {
                                            Working = "Not physically worked for minmum 3 days [Only " + dblPhysicalWorkDays + " days]";
                                            sShortage = MyGlobal.GetPureString(reader, "sShortage");
                                            sShortage += "<span style=\"color:red\">Pay NA</span>";
                                            bMarkNoPay = true;
                                        }
                                        else if (bLastPassWasAbsent)
                                        {
                                            Working = "Reason for no Pay: Previous day is marked as Absent";
                                            sShortage = MyGlobal.GetPureString(reader, "sShortage");
                                            sShortage += "<span style=\"color:red\">Pay NA</span>";
                                            bMarkNoPay = true;
                                        }
                                        if (bMarkNoPay)
                                        {
                                            sUpdateSQL += "update " + MyGlobal.activeDB + ".tbl_attendance " +
                                            "Set dblDayTobePaid=0,sShortage='" + sShortage + "'," +
                                            "Working='" + Working + "' " +
                                            "where m_Profile='" + profile + "' " +
                                            "and m_StaffID = '" + staffidsearch + "' " +
                                            "and m_id='" + m_id + "';";
                                        }

                                        dblPhysicalWorkDays = 0;
                                        iWorkdayCount = 0;
                                        //for (int i = 0; i < 36; i++) dblPhysical[i] = 0;
                                        //iDayCounter = 0;
                                    } // OFF end
                                    else
                                    {
                                        iDayCounter++;
                                    }
                                    bLastPassWasAbsent = (MyGlobal.GetPureInt32(reader, "dblAbsent") == 1);
                                    //MyGlobal.GetPureString(reader, "m_MarkLeave");
                                } // while end
                            }
                        }
                    }
                    if (sUpdateSQL.Length > 0) using (MySqlCommand mySqlCommand1 = new MySqlCommand(sUpdateSQL, con)) mySqlCommand1.ExecuteNonQuery();
                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException-> AttendanceRunChecker - " + ex.Message);
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception-> AttendanceRunChecker - " + ex.Message);
            }
        }
        private void FillTheseManyDaysFromPreviousMonthToPhysicalWorkArray(
            string profile, string staffidsearch, int iDayCounter, double[] dblPhysical, double dlbDtEnd)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_attendance " +
                    "where m_Profile='" + profile + "' " +
                    "and m_StaffID = '" + staffidsearch + "' " +
                    "and m_Date > '" + dlbDtEnd + "' " +
                    "order by m_Date desc limit 7;";
                    // Take the last 7 working days from the last date + 1
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read()) // This won't be more than seven days, so no problem
                                {
                                    //dblPhysical[iDayCounter++] = MyGlobal.GetPureDouble(reader, "dblActualWorkingDays_Local");
                                    dblPhysical[iDayCounter++] = MyGlobal.GetPureDouble(reader, "dblDayTobePaid");
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException-> FillTheseManyDaysFromPreviousMonthToPhysicalWorkArray - " + ex.Message);
            }
        }
        private void GetFromToDates(int iYear, int iMonth, int iDate,
            int iYearTo, int iMonthTo, int iDateTo,
            out double dlbDtStart, out double dlbDtEnd)
        {
            dlbDtStart = 0;
            dlbDtEnd = 0;
            DateTime dtFrom, dtTo;
            try
            {
                dtFrom = new DateTime(iYear, iMonth, iDate);
                dtTo = new DateTime(iYearTo, iMonthTo, iDateTo);
                //dtFrom = dtFrom.AddMonths(-1);

                //dtTo = new DateTime(iYear, iMonth, iStartDate);
                //dtTo = dtTo.AddMonths(1);
                //dtTo = dtTo.AddDays(-1);

            }
            catch (ArgumentOutOfRangeException e)
            {
                MyGlobal.Error("GetDaysForTheSalary Month-CRITICAL ERROR 2-" + e.Message);
                return;
            }
            dlbDtStart = MyGlobal.ToEpochTime(dtFrom) + 19800;
            dlbDtEnd = MyGlobal.ToEpochTime(dtTo) + 19800;
        }
        /*
        private void GetFromToDates(int iYear, int iMonth, int iStartDate, out double dlbDtStart, out double dlbDtEnd)
        {
            dlbDtStart = 0;
            dlbDtEnd = 0;
            DateTime dtFrom, dtTo;
            try
            {
                dtFrom = new DateTime(iYear, iMonth, iStartDate);
                //dtFrom = dtFrom.AddMonths(-1);

                dtTo = new DateTime(iYear, iMonth, iStartDate);
                dtTo = dtTo.AddMonths(1);
                dtTo = dtTo.AddDays(-1);

            }
            catch (ArgumentOutOfRangeException e)
            {
                MyGlobal.Error("GetDaysForTheSalary Month-CRITICAL ERROR 2-" + e.Message);
                return;
            }
            dlbDtStart = MyGlobal.ToEpochTime(dtFrom) + 19800;
            dlbDtEnd = MyGlobal.ToEpochTime(dtTo) + 19800;
        }
        */
        private bool DoesThisAttendanceEntryAvailable(string profile, string staffid, int iYear, int iMonth, long date, string roster, string shift)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT m_id FROM " + MyGlobal.activeDB + ".tbl_attendance " +
                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' " +
                "and m_Date='" + date + "' and m_RosterName='" + roster + "' " +
                "and m_ShiftName='" + shift + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }
        //---------------------------------------------------------------------
        private void ProcessAttendanceTable_OLD_TILL_31_08_2019(string profile, string staffid, string level, string sDisplayTable, string staffidsearch,
            int iYearName, int iMonthName, string AttendanceMethod)
        {

            //----------------------Useful for appeoval
            double dlbToday = MyGlobal.ToEpochTime(DateTime.Today) + 19800;
            double dlbDtStart = 0, dlbDtEnd = 0;
            //GetFromToDates(iYearName, iMonthName,26, out dlbDtStart, out dlbDtEnd);
            if (dlbDtEnd > dlbToday) dlbDtEnd = dlbToday;

            double workingdays = 0, lop = 0;
            long int32StartDate = 0, int32EndDate = 0;

            int iScheduledWorkingDays = 0;
            double dblActualWorkingDays = 0;
            int iNoOfOFFs = 0;
            double dblPaidLeaves = 0;
            double dblLOPs = 0;
            double dblALOPs = 0;
            long lLateSeconds_AccumilatedForTheMonth = 0;
            //Dictionary<string, int> _rosterOptions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            //string sRosterOption = "";
            string sRosterOptions = ""; // /ACO:1,HP:
            string sRosterOptionsResult = "";
            double dblDayTobePaidTotal = 0;
            double dblLOPBasedOnDelay = 0;
            //--------------------------Load roster options table
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    //--------------------------Load roster options table
                    string sSQL = "SELECT m_Name,m_PayIndex,m_PhysicalPresence FROM " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                        "where m_Profile='" + profile + "' ";
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
                                        sRosterOptions +=
                                            reader.GetString(0) + ":" +     // Roster Option Name
                                            reader.GetInt16(1) + ":" +      //  PayIndex
                                            reader.GetInt16(2) + ",";       //  PhysicalPresense
                                    }
                                }
                            }
                        }
                    }

                    //---------------------------------------------------Update for Robin issue END
                    List<long> dates = new List<long>();
                    //long[] arDates = new long[31];
                    //for (int i = 0; i <= 31; i++) arDates[i] = 0;
                    sSQL = "SELECT m_Date FROM " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                        "where m_Profile='" + profile + "' " +
                        "and m_StaffID = '" + staffidsearch + "' " +
                        "and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "' " +
                        "order by m_Date desc;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    dates.Add(reader.GetInt32(0));
                                }
                            }
                        }
                    }

                    //---------------------------------------------------
                    DateTime dtStart = MyGlobal.ToDateTimeFromEpoch((long)dlbDtStart);
                    DateTime dtEnd = MyGlobal.ToDateTimeFromEpoch((long)dlbDtEnd);
                    int iYearStart = dtStart.Year;
                    int iMonthStart = dtStart.Month; // one indexed
                    int iYearEnd = dtEnd.Year;
                    int iMonthEnd = dtEnd.Month; // one indexed

                    string attnSQL = "";

                    string queryRoster = "";
                    queryRoster += "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Year='" + iYearStart + "' and m_Month='" + (iMonthStart - 1) + "' and m_ShiftName is not null and m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "'";
                    if ((iYearStart != iYearEnd) || (iMonthStart != iMonthEnd))
                    {
                        queryRoster += "union SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Year='" + iYearEnd + "' and m_Month='" + (iMonthEnd - 1) + "' and m_ShiftName is not null and m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(queryRoster, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    int iYear = MyGlobal.GetPureInt16(reader, "m_Year");
                                    int iMonth = MyGlobal.GetPureInt16(reader, "m_Month");
                                    string roster = MyGlobal.GetPureString(reader, "m_RosterName");
                                    string shift = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    long shiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                    long shiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                    for (int day = 0; day <= 30; day++)
                                    {
                                        string rosterOption = MyGlobal.GetPureString(reader, "m_Day" + (day + 1));
                                        long date = MyGlobal.GetUnixTime(
                                            iYear, // year
                                            iMonth + 1, // month
                                            day + 1
                                            );

                                        if ((rosterOption.Length > 0) && (date >= dlbDtStart) && (date <= dlbDtEnd))
                                        {

                                            if (DoesThisAttendanceEntryAvailable(profile, staffidsearch, iYear, iMonth, date, roster, shift))
                                            {
                                                /*
                                                attnSQL += "update " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "Set m_ShiftStart='" + shiftStart + "',m_ShiftEnd='" + shiftEnd + "'," +
                                                    "m_MarkRoster='" + rosterOption + "',m_RosterOptions='" + sRosterOptions + "'," +
                                                    "m_AsOn='" + date + "' " +
                                                    "where m_Profile='" + profile + "' and m_Year='" + iYear + "' " +
                                                    "and m_Month='" + iMonth + "' and m_Date='" + date + "' " +
                                                    "and m_RosterName='" + roster + "' " +
                                                    "and m_ShiftName='" + shift + "';";
                                                    */
                                            }
                                            else
                                            {
                                                attnSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "(m_StaffID,m_Year,m_Month,m_Date,m_RosterName,m_ShiftName," +
                                                    "m_ShiftStart,m_ShiftEnd,m_Profile,m_MarkRoster,m_MarkLeave," +
                                                    "m_RosterOptions,m_AsOn) values (" +
                                                    "'" + staffidsearch + "','" + iYear + "','" + iMonth + "','" + date + "'," +
                                                    "'" + roster + "','" + shift + "','" + (date + shiftStart) + "','" + (date + shiftEnd) + "'," +
                                                    "'" + profile + "','" + rosterOption + "','','" + sRosterOptions + "'," +
                                                    "'" + date + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (attnSQL.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(attnSQL, con)) mySqlCommand1.ExecuteNonQuery();
                    }
                    //---------------------------------------------------Update for Robin issue END
                    string sUpdateSQL = "";
                    //---------------------Get it from attendance table
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                        "where m_Profile='" + profile + "' " +
                        "and m_StaffID = '" + staffidsearch + "' " +
                        "and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "' " +
                        "order by m_Date desc;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                ///hrAttendanceResponse.result = "No Data";// "Processed list not available";
                                ///hrAttendanceResponse.listType = "live";
                            }
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    iNoOfOFFs = 0;
                                    HRAttendanceRow row = new HRAttendanceRow();
                                    row.m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    row.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");

                                    row.m_Year = MyGlobal.GetPureInt16(reader, "m_Year");
                                    row.m_Month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    row.m_Date = MyGlobal.GetPureInt32(reader, "m_Date"); // Unixfulltime

                                    Int32 key = 0, startdate = 0;//robin
                                    row.payscale = GetActivePayscaleForThisDay(profile, (Int32)row.m_Date, row.m_StaffID, out key, out startdate);
                                    row.key = key;
                                    row.startdate = startdate;

                                    if (int32StartDate == 0) int32StartDate = row.m_Date;
                                    int32EndDate = row.m_Date;

                                    row.m_RosterName = MyGlobal.GetPureString(reader, "m_RosterName");
                                    row.m_ShiftName = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    row.m_ShiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStart");
                                    row.m_ShiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEnd");
                                    row.m_ActualStart = MyGlobal.GetPureInt32(reader, "m_ActualStart");
                                    row.m_ActualEnd = MyGlobal.GetPureInt32(reader, "m_ActualEnd");
                                    row.lWorkhours = MyGlobal.GetPureInt32(reader, "lWorkhours");
                                    row.m_WorkApproved = MyGlobal.GetPureInt32(reader, "m_WorkApproved");
                                    row.m_AsOn = MyGlobal.GetPureInt32(reader, "m_AsOn");
                                    row.m_LateLoginStatus = MyGlobal.GetPureInt16(reader, "m_LateLoginStatus");
                                    row.m_Mode = MyGlobal.GetPureInt16(reader, "m_Mode");

                                    //redefine again if Half day ....
                                    row.logindelay = (row.m_ActualStart - row.m_ShiftStart);
                                    row.workspan = row.m_ActualEnd - row.m_ActualStart;
                                    row.sShortage = "";
                                    row.Working = "";
                                    iScheduledWorkingDays++;

                                    row.m_MarkRoster = MyGlobal.GetPureString(reader, "m_MarkRoster");
                                    row.m_MarkLeave = MyGlobal.GetPureString(reader, "m_MarkLeave");
                                    //------------------------------------------------------------
                                    //if (sRosterOptions.Length == 0)sRosterOptions = MyGlobal.GetPureString(reader, "m_RosterOptions");
                                    //------------------------------------------------------------
                                    double dblAbsent_Total = 0;
                                    double dblLOPs_Local = 0; // Consolidated of leaves & roster options
                                    double dblAdiLOPs_Local = 0;
                                    double dblALOPs_Local = 0;
                                    long lLateSeconds_AccumilatedForTheMonth_Local = 0;
                                    double dblActualWorkingDays_Local = 0;
                                    double dblPaidLeaves_Local = 0;
                                    double dblWorkExceptionMarker = 0; // 1 - full day, 0.5 - Half day
                                    double dblDayTobePaid = 0;
                                    double dblAbsent = 0;
                                    if (row.m_MarkLeave.Length > 0)
                                    {
                                        if (row.m_MarkLeave.IndexOf("LOP") == -1)
                                        {
                                            // Leave exists. Check its full or half
                                            if (row.m_MarkLeave.IndexOf('/') > -1)
                                            {
                                                dblPaidLeaves_Local = 0.5;
                                                dblWorkExceptionMarker = 0.5;
                                                dblDayTobePaid = 0.5;
                                                // First half leave, so login delay is calculated after 4.5 Hrs
                                                if (row.m_MarkLeave.IndexOf('/') == 0) row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                            }
                                            else
                                            {
                                                dblPaidLeaves_Local = 1.0;
                                                dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                                dblDayTobePaid = 1.0;
                                            }
                                        }
                                        else
                                        {   // LOP
                                            // Leave exists. Check its full or half
                                            if (row.m_MarkLeave.IndexOf('/') > -1)
                                            {
                                                if (row.m_MarkLeave.IndexOf("ALOP") > -1)
                                                {
                                                    dblAdiLOPs_Local += 0.5;
                                                }
                                                else
                                                {
                                                    dblLOPs_Local += 0.5;
                                                }

                                                //dblPaidLeaves_Local = 0.5;
                                                dblWorkExceptionMarker = 0.5;
                                                //dblDayTobePaid = 0.5;
                                                // First half leave, so login delay is calculated after 4.5 Hrs
                                                if (row.m_MarkLeave.IndexOf('/') == 0)
                                                    row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                            }
                                            else
                                            {
                                                if (row.m_MarkLeave.IndexOf("ALOP") > -1)
                                                {
                                                    dblAdiLOPs_Local += 1.0;
                                                }
                                                else
                                                {
                                                    dblLOPs_Local += 1.0;
                                                }
                                                //dblPaidLeaves_Local = 1.0;
                                                dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                                                              //dblDayTobePaid = 1.0;
                                            }
                                        }
                                    }
                                    double dblPayIndex = 0;
                                    int iPhysicalPresenseNeeded = PhysicalPresenseNeeded(sRosterOptions, row.m_MarkRoster, out dblPayIndex);

                                    if (!row.m_MarkRoster.Equals(MyGlobal.WORKDAY_MARKER) && !row.m_MarkRoster.Equals("OFF") && row.m_MarkRoster.Length > 0)
                                    {
                                        /*
                                        if (!_rosterOptions.ContainsKey(row.m_MarkRoster))
                                        {
                                            _rosterOptions.Add(row.m_MarkRoster, 1);
                                        }
                                        else
                                        {
                                            _rosterOptions[row.m_MarkRoster]++;
                                        }
                                        */
                                        //sRosterOption = row.m_MarkRoster;
                                        if (iPhysicalPresenseNeeded == 0)
                                        {
                                            // No need to come to office
                                            if (row.m_MarkRoster.IndexOf('/') > -1)
                                            {
                                                dblWorkExceptionMarker += 0.5;
                                                dblDayTobePaid += (dblPayIndex * 0.5);
                                            }
                                            else
                                            {
                                                dblWorkExceptionMarker = 1;
                                                dblDayTobePaid += dblPayIndex;
                                            }
                                        }
                                        else
                                        {
                                            // Has to work on this day
                                            if (row.m_MarkRoster.IndexOf('/') > -1)
                                            {
                                                //dblWorkExceptionMarker += 0.5;
                                                // First half leave, so login delay is calculated after 4.5 Hrs
                                                //if (row.m_MarkLeave.IndexOf('/') == 0) row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                            }
                                            else
                                            {
                                                //dblWorkExceptionMarker = 1;
                                            }
                                        }

                                    }
                                    if (row.m_MarkRoster.Equals("OFF"))
                                    {
                                        iNoOfOFFs++;
                                        dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                        dblDayTobePaid = dblPayIndex;
                                    }
                                    // No exception by leave or roster and roster has schedule value 
                                    // 1 means, full exception from work
                                    if (row.m_ShiftStart > 0 && dblWorkExceptionMarker != 1) // Roster has value
                                    {
                                        if (row.lWorkhours == 0)
                                        {
                                            if (dblWorkExceptionMarker == 0.5)
                                            {
                                                //row.sShortage = "<span style='color:red'>/ALOP</span>";
                                                //dblALOPs += 0.5;
                                                row.sShortage = "<span style='color:red'>/Ab</span>";
                                                dblAbsent += 0.5;
                                            }
                                            else
                                            {
                                                //row.sShortage = "<span style='color:red'>ALOP</span>";
                                                //dblALOPs += 1;
                                                row.sShortage = "<span style='color:red'>Ab</span>";
                                                dblAbsent += 1;
                                            }
                                        }
                                        else
                                        {
                                            bool bHalfDayCalculationDone = false;
                                            //--------------------------Process Leave based times
                                            if (row.m_MarkLeave.Length > 0)
                                            {
                                                if (row.m_MarkLeave.IndexOf('/') > -1)
                                                {
                                                    // Half day leave applied
                                                    long lShortage = (const_lShiftDuration / 2) - row.lWorkhours;

                                                    if (lShortage > (2 * 3600)) // More than 2 hours
                                                    {
                                                        //dblALOPs_Local += 0.5;
                                                        //row.Working += "[More than 2 hours. /ALOP marked], ";
                                                        dblAbsent += 0.5;
                                                        row.Working += "[More than 2 hours. /Ab marked], ";
                                                    }
                                                    else if (lShortage > 0) // Less than 2 hours
                                                    {
                                                        dblActualWorkingDays_Local = 0.5;
                                                        lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                        dblDayTobePaid += dblPayIndex * 0.5;
                                                    }
                                                    else
                                                    {   // exact or excell
                                                        dblActualWorkingDays_Local = 0.5;
                                                        dblDayTobePaid += dblPayIndex * 0.5;
                                                    }
                                                    bHalfDayCalculationDone = true;
                                                }
                                            }
                                            //--------------------------Process Roster based times
                                            bool bIsPaidLeave = true;

                                            if (iPhysicalPresenseNeeded == 1)
                                            {
                                                bIsPaidLeave = false;
                                            }
                                            if (row.m_MarkRoster.Length > 0 && bIsPaidLeave)
                                            {
                                                if (row.m_MarkRoster.IndexOf('/') > -1)
                                                {
                                                    // Half day leave applied
                                                    long lShortage = (const_lShiftDuration / 2) - row.lWorkhours;

                                                    if (lShortage > (2 * 3600)) // More than 2 hours
                                                    {
                                                        // If /LOP marked by Leave, clear it here
                                                        //if (dblALOPs_Local > 0) dblALOPs_Local -= 0.5;
                                                        //row.Working += "[More than 2 hours. /ALOP marked], ";
                                                        if (dblAbsent > 0) dblAbsent -= 0.5;
                                                        row.Working += "[More than 2 hours. /Ab marked], ";
                                                    }
                                                    else if (lShortage > 0) // Less than 2 hours
                                                    {
                                                        // If no working day marked by leave, do here
                                                        if (dblActualWorkingDays_Local == 0) dblActualWorkingDays_Local = 0.5;
                                                        lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                        dblDayTobePaid += dblPayIndex * 0.5;
                                                    }
                                                    else
                                                    {   // exact or excell
                                                        // If no working day marked by leave, do here
                                                        if (dblActualWorkingDays_Local == 0) dblActualWorkingDays_Local = 0.5;
                                                        dblDayTobePaid += dblPayIndex * 0.5;
                                                    }
                                                    bHalfDayCalculationDone = true;
                                                }
                                            }

                                            //----------------?????
                                            if (!bHalfDayCalculationDone) // Do for full day calculation
                                            {
                                                // No leaves applied
                                                long lShortage = const_lShiftDuration - row.lWorkhours;
                                                if (lShortage > (4 * 3600)) // more than 4 hours
                                                {
                                                    ///dblALOPs_Local += 1;
                                                    ///row.Working += "[More than 4 hours. ALOP marked], ";
                                                    dblAbsent += 1;
                                                    row.Working += "[More than 4 hours. Absent marked], ";
                                                }
                                                else if (lShortage > (2 * 3600)) // more than 2 hours
                                                {
                                                    ///dblALOPs_Local += 0.5;
                                                    dblAbsent += 0.5;
                                                    dblActualWorkingDays_Local = 0.5;
                                                    dblDayTobePaid += dblPayIndex * 0.5;
                                                    row.Working += "[More than 2 hours. /Absent marked], ";
                                                }
                                                else if (lShortage > 0) // less than 2 hours
                                                {
                                                    dblActualWorkingDays_Local = 1;
                                                    lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                    dblDayTobePaid += dblPayIndex;
                                                }
                                                else
                                                {   // exact or exces
                                                    dblActualWorkingDays_Local = 1;
                                                    dblDayTobePaid += dblPayIndex;
                                                }
                                            }
                                            //--------------------------Accomilate the results
                                            /*
                                            if (dblALOPs_Local == 1)
                                            {
                                                row.sShortage += "<span style='color:red'>ALOP</span>";
                                            }
                                            else if (dblALOPs_Local == 0.5)
                                            {
                                                row.sShortage += "<span style='color:red'>/ALOP</span>";
                                            }
                                            */
                                            if (dblAbsent == 1)
                                            {
                                                row.sShortage += "<span style='color:red'>Ab</span>";
                                            }
                                            else if (dblAbsent == 0.5)
                                            {
                                                row.sShortage += "<span style='color:red'>/Ab</span>";
                                            }

                                            string sBit = "";
                                            ///if (hrAttendanceResponse.AttendanceMethod.Equals("Administrative", StringComparison.CurrentCultureIgnoreCase))
                                            if (AttendanceMethod.Equals("Administrative", StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                long WORKING_SPAN = 0;
                                                if (dblActualWorkingDays_Local == 1)
                                                {
                                                    WORKING_SPAN = 32400;
                                                }
                                                else if (dblActualWorkingDays_Local == 0.5)
                                                {
                                                    WORKING_SPAN = 16200;
                                                }

                                                if (WORKING_SPAN > 0)
                                                {
                                                    if (row.workspan >= WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local <= 0)
                                                    {   // All perfect
                                                        sBit += " [Workspan OK,Shift work OK";
                                                        if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                        {
                                                            lLateSeconds_AccumilatedForTheMonth_Local = row.logindelay;
                                                            sBit += ",But login delay";
                                                        }
                                                        sBit += "]";
                                                    }
                                                    else if (row.workspan >= WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                                    {   // Work span is ok. But, 8 hours not worked
                                                        sBit += " [Workspan OK,Shift work is LESS";
                                                        if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                        {
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                            sBit += ",login delay also added";
                                                        }
                                                        sBit += "]";
                                                    }
                                                    else if (row.workspan < WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local <= 0)
                                                    {   // Work span is LESS. But, 8 hours is ok
                                                        sBit += " [Workspan is LESS,Shift work OK";
                                                        long lapseInWorkSpan = WORKING_SPAN - row.workspan;

                                                        lLateSeconds_AccumilatedForTheMonth_Local = lapseInWorkSpan;

                                                        if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                        {
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                            sBit += ",login delay added";
                                                        }
                                                        sBit += "]";
                                                    }
                                                    else if (row.workspan < WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                                    {   // Work span is LESS & 8 hours is also not worked
                                                        sBit += " [Workspan is LESS,Shift work also LESS";
                                                        long lapseInWorkSpan = WORKING_SPAN - row.workspan;
                                                        if (lapseInWorkSpan > lLateSeconds_AccumilatedForTheMonth_Local)
                                                        {
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lapseInWorkSpan;
                                                            sBit += ",work span delay is considered";
                                                        }
                                                        else
                                                        {
                                                            sBit += ",shift work delay is considered";
                                                        }
                                                        if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                        {
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                            sBit += ",login delay added";
                                                        }
                                                        sBit += "]";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                {
                                                    lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                    sBit += " [login delay added]";
                                                }

                                                //if (dblALOPs_Local > 0) // ALOP marked
                                                if (dblAbsent > 0) // ALOP marked
                                                {
                                                    lLateSeconds_AccumilatedForTheMonth_Local = 0;
                                                    sBit += " [Absent marked. So delay ignored]";
                                                }
                                            }
                                            if (lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                            {
                                                string color = "orange";
                                                if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1) color = "red";
                                                row.sShortage += " (<span style='color:" + color + ";' ";
                                                if (color.Equals("red")) row.sShortage += "title='Late login delay'";
                                                row.sShortage += ">" +
                                                    MyGlobal.ToDateTimeFromEpoch(lLateSeconds_AccumilatedForTheMonth_Local).ToString("HH:mm:ss") +
                                                    "</span>)";
                                            }
                                            row.Working = sBit;
                                            lLateSeconds_AccumilatedForTheMonth += lLateSeconds_AccumilatedForTheMonth_Local;
                                            dblActualWorkingDays += dblActualWorkingDays_Local;
                                            //---------------------------
                                        }
                                    }// only if row.m_ShiftStart>0
                                    dblALOPs += dblALOPs_Local;     //  Absent LOPs from time based shortage
                                    dblALOPs += dblAdiLOPs_Local;   //  Leave like scanctioned ALOPs
                                    dblLOPs += dblLOPs_Local;
                                    dblAbsent_Total += dblAbsent;
                                    dblPaidLeaves += dblPaidLeaves_Local;
                                    row.dblDayTobePaid = dblDayTobePaid;
                                    dblDayTobePaidTotal += dblDayTobePaid;


                                    sUpdateSQL += "update " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                                        "Set Working='" + row.Working + "'," +
                                        "sShortage=\"" + row.sShortage + "\"," +
                                        "dblDayTobePaid='" + dblDayTobePaid + "'," +
                                        "dblALOPs_Local='" + (dblALOPs_Local + dblAdiLOPs_Local) + "'," +
                                        "dblLOPs_Local='" + dblLOPs_Local + "'," +
                                        "dblPaidLeaves_Local='" + dblPaidLeaves_Local + "'," +
                                        "iNoOfOFFs='" + iNoOfOFFs + "'," +
                                        "lLateSeconds_AccumilatedForTheMonth_Local='" + lLateSeconds_AccumilatedForTheMonth_Local + "'," +
                                        "dblActualWorkingDays_Local='" + dblActualWorkingDays_Local + "'," +
                                        "dblAbsent='" + dblAbsent + "'," +
                                        "m_RosterOptions='" + sRosterOptions + "'," +
                                        "pay_scale='" + row.payscale + "'," +
                                        "pay_key='" + row.key + "'," +
                                        "pay_startdate='" + row.startdate + "' " +
                                        "where m_Profile='" + profile + "' and m_id='" + row.m_id + "';";
                                }
                                dblLOPBasedOnDelay = GetLOPBasedOnDelay(lLateSeconds_AccumilatedForTheMonth);
                                dblDayTobePaidTotal = dblDayTobePaidTotal - dblLOPBasedOnDelay;
                                workingdays = iScheduledWorkingDays;
                                lop = (dblLOPs + dblLOPBasedOnDelay);

                            }
                        }
                    }

                    if (sUpdateSQL.Length > 0) using (MySqlCommand mySqlCommand1 = new MySqlCommand(sUpdateSQL, con)) mySqlCommand1.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessAttendanceTable -> +" + ex.Message);
            }
        }//-------------End ProcessAttendanceTable_OLD function 
        //int iYearName, int iMonthName,
        private void ProcessAttendanceTable(string profile, string staffid, string level, string sDisplayTable, string staffidsearch,
            int iYear, int iMonth, int iDay, int iYearTo, int iMonthTo, int iDayTo,
             string AttendanceMethod)
        {

            //----------------------Useful for appeoval
            double dlbToday = MyGlobal.ToEpochTime(DateTime.Today) + 19800;


            double workingdays = 0, lop = 0;
            long int32StartDate = 0, int32EndDate = 0;

            int iScheduledWorkingDays = 0;
            double dblActualWorkingDays = 0;
            int iNoOfOFFs = 0;
            double dblPaidLeaves = 0;
            double dblLOPs = 0;
            double dblALOPs = 0;
            long lLateSeconds_AccumilatedForTheMonth = 0;
            //Dictionary<string, int> _rosterOptions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            //string sRosterOption = "";
            string sRosterOptions = ""; // /ACO:1,HP:
            string sRosterOptionsResult = "";
            double dblDayTobePaidTotal = 0;
            double dblLOPBasedOnDelay = 0;
            //--------------------------Load roster options table
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------------------------
                    int iStartDate = 1;
                    string sSQL = "select m_AttnStartDate from " + MyGlobal.activeDB + ".tbl_profile_info where " +
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
                    double dlbDtStart = 0, dlbDtEnd = 0;
                    //GetFromToDates(iYearName, iMonthName, iStartDate, out dlbDtStart, out dlbDtEnd);
                    GetFromToDates(iYear, iMonth, iDay, iYearTo, iMonthTo, iDayTo, out dlbDtStart, out dlbDtEnd);
                    if (dlbDtEnd > dlbToday) dlbDtEnd = dlbToday;
                    //--------------------------Load roster options table
                    sSQL = "SELECT m_Name,m_PayIndex,m_PhysicalPresence FROM " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                        "where m_Profile='" + profile + "' ";
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
                                        sRosterOptions +=
                                            reader.GetString(0) + ":" +     // Roster Option Name
                                            reader.GetInt16(1) + ":" +      //  PayIndex
                                                                            //reader.GetDouble(1) + ":" +      //  PayIndex
                                            reader.GetInt16(2) + ",";       //  PhysicalPresense
                                    }
                                }
                            }
                        }
                    }

                    //---------------------------------------------------Update for Robin issue END
                    List<long> dates = new List<long>();
                    //long[] arDates = new long[31];
                    //for (int i = 0; i <= 31; i++) arDates[i] = 0;
                    sSQL = "SELECT m_Date FROM " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                        "where m_Profile='" + profile + "' " +
                        "and m_StaffID = '" + staffidsearch + "' " +
                        "and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "' " +
                        "order by m_Date desc;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    dates.Add(reader.GetInt32(0));
                                }
                            }
                        }
                    }

                    //---------------------------------------------------
                    DateTime dtStart = MyGlobal.ToDateTimeFromEpoch((long)dlbDtStart);
                    DateTime dtEnd = MyGlobal.ToDateTimeFromEpoch((long)dlbDtEnd);
                    int iYearStart = dtStart.Year;
                    int iMonthStart = dtStart.Month; // one indexed
                    int iYearEnd = dtEnd.Year;
                    int iMonthEnd = dtEnd.Month; // one indexed

                    string attnSQL = "";

                    string queryRoster = "";
                    queryRoster += "SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Year='" + iYearStart + "' and m_Month='" + (iMonthStart - 1) + "' and m_ShiftName is not null and m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "'";
                    if ((iYearStart != iYearEnd) || (iMonthStart != iMonthEnd))
                    {
                        queryRoster += "union SELECT * FROM " + MyGlobal.activeDB + ".tbl_rosters where m_Year='" + iYearEnd + "' and m_Month='" + (iMonthEnd - 1) + "' and m_ShiftName is not null and m_Profile='" + profile + "' and m_StaffID='" + staffidsearch + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(queryRoster, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    int iYearLoc = MyGlobal.GetPureInt16(reader, "m_Year");
                                    int iMonthLoc = MyGlobal.GetPureInt16(reader, "m_Month");
                                    string roster = MyGlobal.GetPureString(reader, "m_RosterName");
                                    string shift = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    long shiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStartTime");
                                    long shiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEndTime");
                                    for (int day = 0; day <= 30; day++)
                                    {
                                        string rosterOption = MyGlobal.GetPureString(reader, "m_Day" + (day + 1));
                                        long date = MyGlobal.GetUnixTime(
                                            iYearLoc, // year
                                            iMonthLoc + 1, // month
                                            day + 1
                                            );
                                        if (rosterOption.Equals("WD/"))
                                        {
                                            Console.WriteLine("Got");
                                        }
                                        if ((rosterOption.Length > 0) && (date >= dlbDtStart) && (date <= dlbDtEnd))
                                        {

                                            if (DoesThisAttendanceEntryAvailable(profile, staffidsearch, iYearLoc, iMonthLoc, date, roster, shift))
                                            {
                                                /*
                                                attnSQL += "update " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "Set m_ShiftStart='" + shiftStart + "',m_ShiftEnd='" + shiftEnd + "'," +
                                                    "m_MarkRoster='" + rosterOption + "',m_RosterOptions='" + sRosterOptions + "'," +
                                                    "m_AsOn='" + date + "' " +
                                                    "where m_Profile='" + profile + "' and m_Year='" + iYear + "' " +
                                                    "and m_Month='" + iMonth + "' and m_Date='" + date + "' " +
                                                    "and m_RosterName='" + roster + "' " +
                                                    "and m_ShiftName='" + shift + "';";
                                                    */
                                            }
                                            else
                                            {
                                                attnSQL += "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendance " +
                                                    "(m_StaffID,m_Year,m_Month,m_Date,m_RosterName,m_ShiftName," +
                                                    "m_ShiftStart,m_ShiftEnd,m_Profile,m_MarkRoster,m_MarkLeave," +
                                                    "m_RosterOptions,m_AsOn) values (" +
                                                    "'" + staffidsearch + "','" + iYearLoc + "','" + iMonthLoc + "','" + date + "'," +
                                                    "'" + roster + "','" + shift + "','" + (date + shiftStart) + "','" + (date + shiftEnd) + "'," +
                                                    "'" + profile + "','" + rosterOption + "','','" + sRosterOptions + "'," +
                                                    "'" + date + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (attnSQL.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(attnSQL, con)) mySqlCommand1.ExecuteNonQuery();
                    }
                    //---------------------------------------------------Update for Robin issue END
                    string sUpdateSQL = "";
                    //---------------------Get it from attendance table
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                        "where m_Profile='" + profile + "' " +
                        "and m_StaffID = '" + staffidsearch + "' " +
                        "and m_Date >= '" + dlbDtStart + "' and m_Date <= '" + dlbDtEnd + "' " +
                        "order by m_Date desc;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                ///hrAttendanceResponse.result = "No Data";// "Processed list not available";
                                ///hrAttendanceResponse.listType = "live";
                            }
                            if (reader.HasRows)
                            {
                                int startTime = System.Environment.TickCount;
                                while (reader.Read())
                                {
                                    iNoOfOFFs = 0;
                                    HRAttendanceRow row = new HRAttendanceRow();
                                    row.m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    row.m_StaffID = MyGlobal.GetPureString(reader, "m_StaffID");

                                    row.m_Year = MyGlobal.GetPureInt16(reader, "m_Year");
                                    row.m_Month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    row.m_Date = MyGlobal.GetPureInt32(reader, "m_Date"); // Unixfulltime

                                    Int32 key = 0, startdate = 0;//robin
                                    row.payscale = GetActivePayscaleForThisDay(profile, (Int32)row.m_Date, row.m_StaffID, out key, out startdate);
                                    row.key = key;
                                    row.startdate = startdate;

                                    if (int32StartDate == 0) int32StartDate = row.m_Date;
                                    int32EndDate = row.m_Date;

                                    row.m_RosterName = MyGlobal.GetPureString(reader, "m_RosterName");
                                    row.m_ShiftName = MyGlobal.GetPureString(reader, "m_ShiftName");
                                    row.m_ShiftStart = MyGlobal.GetPureInt32(reader, "m_ShiftStart");
                                    row.m_ShiftEnd = MyGlobal.GetPureInt32(reader, "m_ShiftEnd");
                                    row.m_ActualStart = MyGlobal.GetPureInt32(reader, "m_ActualStart");
                                    row.m_ActualEnd = MyGlobal.GetPureInt32(reader, "m_ActualEnd");
                                    row.lWorkhours = MyGlobal.GetPureInt32(reader, "lWorkhours");
                                    row.m_WorkApproved = MyGlobal.GetPureInt32(reader, "m_WorkApproved");
                                    row.m_AsOn = MyGlobal.GetPureInt32(reader, "m_AsOn");
                                    row.m_LateLoginStatus = MyGlobal.GetPureInt16(reader, "m_LateLoginStatus");
                                    row.m_Mode = MyGlobal.GetPureInt16(reader, "m_Mode");

                                    //redefine again if Half day ....
                                    row.logindelay = (row.m_ActualStart - row.m_ShiftStart);
                                    row.workspan = row.m_ActualEnd - row.m_ActualStart;
                                    row.sShortage = "";
                                    row.Working = "";
                                    iScheduledWorkingDays++;

                                    row.m_MarkRoster = MyGlobal.GetPureString(reader, "m_MarkRoster");
                                    row.m_MarkLeave = MyGlobal.GetPureString(reader, "m_MarkLeave");
                                    row.m_Source = MyGlobal.GetPureString(reader, "m_Source");
                                    if (row.m_MarkRoster.Equals("WD/"))
                                    {
                                        Console.WriteLine("Got");
                                    }
                                    //------------------------------------------------------------
                                    //if (sRosterOptions.Length == 0)sRosterOptions = MyGlobal.GetPureString(reader, "m_RosterOptions");
                                    //------------------------------------------------------------
                                    double dblAbsent_Total = 0;
                                    double dblLOPs_Local = 0; // Consolidated of leaves & roster options
                                    double dblAdiLOPs_Local = 0;
                                    double dblALOPs_Local = 0;
                                    long lLateSeconds_AccumilatedForTheMonth_Local = 0;
                                    double dblActualWorkingDays_Local = 0;
                                    double dblPaidLeaves_Local = 0;
                                    double dblWorkExceptionMarker = 0; // 1 - full day, 0.5 - Half day
                                    double dblDayTobePaid = 0;
                                    double dblAbsent = 0;
                                    // Start place of all work calculations

                                    if (row.m_MarkLeave.Length > 0)
                                    {
                                        if (row.m_MarkLeave.IndexOf("LOP") == -1)
                                        {
                                            // Leave exists. Check its full or half
                                            if (row.m_MarkLeave.IndexOf('/') > -1)
                                            {
                                                dblPaidLeaves_Local = 0.5;
                                                dblWorkExceptionMarker = 0.5;
                                                dblDayTobePaid = 0.5;
                                                // First half leave, so login delay is calculated after 4.5 Hrs
                                                if (row.m_MarkLeave.IndexOf('/') == 0) row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                            }
                                            else
                                            {
                                                dblPaidLeaves_Local = 1.0;
                                                dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                                dblDayTobePaid = 1.0;
                                            }
                                        }
                                        else
                                        {   // LOP
                                            // Leave exists. Check its full or half
                                            if (row.m_MarkLeave.IndexOf('/') > -1)
                                            {
                                                if (row.m_MarkLeave.IndexOf("ALOP") > -1)
                                                {
                                                    dblAdiLOPs_Local += 0.5;
                                                }
                                                else
                                                {
                                                    dblLOPs_Local += 0.5;
                                                }

                                                //dblPaidLeaves_Local = 0.5;
                                                dblWorkExceptionMarker = 0.5;
                                                //dblDayTobePaid = 0.5;
                                                // First half leave, so login delay is calculated after 4.5 Hrs
                                                if (row.m_MarkLeave.IndexOf('/') == 0)
                                                    row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                            }
                                            else
                                            {
                                                if (row.m_MarkLeave.IndexOf("ALOP") > -1)
                                                {
                                                    dblAdiLOPs_Local += 1.0;
                                                }
                                                else
                                                {
                                                    dblLOPs_Local += 1.0;
                                                }
                                                //dblPaidLeaves_Local = 1.0;
                                                dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                                                              //dblDayTobePaid = 1.0;
                                            }
                                        }
                                    }
                                    double dblPayIndex = 0;
                                    int iPhysicalPresenseNeeded = PhysicalPresenseNeeded(sRosterOptions, row.m_MarkRoster, out dblPayIndex);

                                    if (iPhysicalPresenseNeeded != 2) // Not double
                                    {
                                        if (row.m_MarkRoster.Equals("OFF"))
                                        {
                                            iNoOfOFFs++;
                                            dblWorkExceptionMarker = 1.0; // No need to consider attendance
                                            dblDayTobePaid = dblPayIndex;
                                        }
                                        if (!row.m_MarkRoster.Equals("OFF") && !row.m_MarkRoster.Equals(MyGlobal.WORKDAY_MARKER) && row.m_MarkRoster.Length > 0)
                                        {
                                            //sRosterOption = row.m_MarkRoster;
                                            if (iPhysicalPresenseNeeded == 0)
                                            {
                                                // No need to come to office
                                                if (row.m_MarkRoster.IndexOf('/') > -1)
                                                {
                                                    dblWorkExceptionMarker += 0.5;
                                                    dblDayTobePaid += (dblPayIndex * 0.5);
                                                }
                                                else
                                                {
                                                    dblWorkExceptionMarker = 1;
                                                    dblDayTobePaid += dblPayIndex;
                                                }
                                            }
                                            else
                                            {
                                                // Has to work on this day
                                                if (row.m_MarkRoster.IndexOf('/') > -1)
                                                {
                                                    dblWorkExceptionMarker += 0.5;
                                                    // First half leave, so login delay is calculated after 4.5 Hrs
                                                    //if (row.m_MarkLeave.IndexOf('/') == 0) row.logindelay = (row.m_ActualStart - (row.m_ShiftStart + 16200));
                                                }
                                                else
                                                {
                                                    //dblWorkExceptionMarker = 1;
                                                }
                                            }

                                        }

                                        // No exception by leave or roster and roster has schedule value 
                                        // 1 means, full exception from work
                                        if (row.m_ShiftStart > 0 && dblWorkExceptionMarker != 1) // Roster has value
                                        {
                                            if (row.lWorkhours == 0)
                                            {
                                                if (dblWorkExceptionMarker == 0.5)
                                                {
                                                    //row.sShortage = "<span style='color:red'>/ALOP</span>";
                                                    //dblALOPs += 0.5;
                                                    row.sShortage = "<span style='color:red'>/Ab</span>";
                                                    dblAbsent += 0.5;
                                                }
                                                else
                                                {
                                                    //row.sShortage = "<span style='color:red'>ALOP</span>";
                                                    //dblALOPs += 1;
                                                    row.sShortage = "<span style='color:red'>Ab</span>";
                                                    dblAbsent += 1;
                                                }
                                            }
                                            else
                                            {
                                                bool bHalfDayCalculationDone = false;
                                                //--------------------------Process Leave based times
                                                if (row.m_MarkLeave.Length > 0)
                                                {
                                                    if (row.m_MarkLeave.IndexOf('/') > -1)
                                                    {
                                                        // Half day leave applied
                                                        long lShortage = (const_lShiftDuration / 2) - row.lWorkhours;

                                                        if (lShortage > (2 * 3600)) // More than 2 hours
                                                        {
                                                            //dblALOPs_Local += 0.5;
                                                            //row.Working += "[More than 2 hours. /ALOP marked], ";
                                                            dblAbsent += 0.5;
                                                            row.Working += "[More than 2 hours. /Ab marked], ";
                                                        }
                                                        else if (lShortage > 0) // Less than 2 hours
                                                        {
                                                            dblActualWorkingDays_Local = 0.5;
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                            dblDayTobePaid += dblPayIndex * 0.5;
                                                        }
                                                        else
                                                        {   // exact or excell
                                                            dblActualWorkingDays_Local = 0.5;
                                                            dblDayTobePaid += dblPayIndex * 0.5;
                                                        }
                                                        bHalfDayCalculationDone = true;
                                                    }
                                                }
                                                //--------------------------Process Roster based times
                                                bool bIsPaidLeave = true;

                                                if (iPhysicalPresenseNeeded == 1)
                                                {
                                                    bIsPaidLeave = false;
                                                }
                                                if (row.m_MarkRoster.Length > 0 && bIsPaidLeave)
                                                {
                                                    if (row.m_MarkRoster.IndexOf('/') > -1)
                                                    {
                                                        // Half day leave applied
                                                        long lShortage = (const_lShiftDuration / 2) - row.lWorkhours;

                                                        if (lShortage > (2 * 3600)) // More than 2 hours
                                                        {
                                                            // If /LOP marked by Leave, clear it here
                                                            //if (dblALOPs_Local > 0) dblALOPs_Local -= 0.5;
                                                            //row.Working += "[More than 2 hours. /ALOP marked], ";
                                                            if (dblAbsent > 0) dblAbsent -= 0.5;
                                                            row.Working += "[More than 2 hours. /Ab marked], ";
                                                        }
                                                        else if (lShortage > 0) // Less than 2 hours
                                                        {
                                                            // If no working day marked by leave, do here
                                                            if (dblActualWorkingDays_Local == 0) dblActualWorkingDays_Local = 0.5;
                                                            lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                            dblDayTobePaid += dblPayIndex * 0.5;
                                                        }
                                                        else
                                                        {   // exact or excell
                                                            // If no working day marked by leave, do here
                                                            if (dblActualWorkingDays_Local == 0) dblActualWorkingDays_Local = 0.5;
                                                            dblDayTobePaid += dblPayIndex * 0.5;
                                                        }
                                                        bHalfDayCalculationDone = true;
                                                    }
                                                }

                                                //----------------?????
                                                if (!bHalfDayCalculationDone) // Do for full day calculation
                                                {
                                                    // No leaves applied
                                                    long lShortage = const_lShiftDuration - row.lWorkhours;
                                                    if (lShortage > (4 * 3600)) // more than 4 hours
                                                    {
                                                        ///dblALOPs_Local += 1;
                                                        ///row.Working += "[More than 4 hours. ALOP marked], ";
                                                        dblAbsent += 1;
                                                        row.Working += "[More than 4 hours. Absent marked], ";
                                                    }
                                                    else if (lShortage > (2 * 3600)) // more than 2 hours
                                                    {
                                                        ///dblALOPs_Local += 0.5;
                                                        dblAbsent += 0.5;
                                                        dblActualWorkingDays_Local = 0.5;
                                                        dblDayTobePaid += dblPayIndex * 0.5;
                                                        row.Working += "[More than 2 hours. /Absent marked], ";
                                                    }
                                                    else if (lShortage > 0) // less than 2 hours
                                                    {
                                                        dblActualWorkingDays_Local = 1;
                                                        lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                        dblDayTobePaid += dblPayIndex;
                                                    }
                                                    else
                                                    {   // exact or exces
                                                        dblActualWorkingDays_Local = 1;
                                                        dblDayTobePaid += dblPayIndex;
                                                    }
                                                }
                                                //--------------------------Accomilate the results
                                                /*
                                                if (dblALOPs_Local == 1)
                                                {
                                                    row.sShortage += "<span style='color:red'>ALOP</span>";
                                                }
                                                else if (dblALOPs_Local == 0.5)
                                                {
                                                    row.sShortage += "<span style='color:red'>/ALOP</span>";
                                                }
                                                */
                                                if (dblAbsent == 1)
                                                {
                                                    row.sShortage += "<span style='color:red'>Ab</span>";
                                                }
                                                else if (dblAbsent == 0.5)
                                                {
                                                    row.sShortage += "<span style='color:red'>/Ab</span>";
                                                }

                                                string sBit = "";
                                                ///if (hrAttendanceResponse.AttendanceMethod.Equals("Administrative", StringComparison.CurrentCultureIgnoreCase))
                                                if (AttendanceMethod.Equals("Administrative", StringComparison.CurrentCultureIgnoreCase))
                                                {
                                                    long WORKING_SPAN = 0;
                                                    if (dblActualWorkingDays_Local == 1)
                                                    {
                                                        WORKING_SPAN = 32400;
                                                    }
                                                    else if (dblActualWorkingDays_Local == 0.5)
                                                    {
                                                        WORKING_SPAN = 16200;
                                                    }

                                                    if (WORKING_SPAN > 0)
                                                    {
                                                        if (row.workspan >= WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local <= 0)
                                                        {   // All perfect
                                                            sBit += " [Workspan OK,Shift work OK";
                                                            if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                            {
                                                                lLateSeconds_AccumilatedForTheMonth_Local = row.logindelay;
                                                                sBit += ",But login delay";
                                                            }
                                                            sBit += "]";
                                                        }
                                                        else if (row.workspan >= WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                                        {   // Work span is ok. But, 8 hours not worked
                                                            sBit += " [Workspan OK,Shift work is LESS";
                                                            if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                            {
                                                                lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                                sBit += ",login delay also added";
                                                            }
                                                            sBit += "]";
                                                        }
                                                        else if (row.workspan < WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local <= 0)
                                                        {   // Work span is LESS. But, 8 hours is ok
                                                            sBit += " [Workspan is LESS,Shift work OK";
                                                            long lapseInWorkSpan = WORKING_SPAN - row.workspan;

                                                            lLateSeconds_AccumilatedForTheMonth_Local = lapseInWorkSpan;

                                                            if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                            {
                                                                lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                                sBit += ",login delay added";
                                                            }
                                                            sBit += "]";
                                                        }
                                                        else if (row.workspan < WORKING_SPAN && lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                                        {   // Work span is LESS & 8 hours is also not worked
                                                            sBit += " [Workspan is LESS,Shift work also LESS";
                                                            long lapseInWorkSpan = WORKING_SPAN - row.workspan;
                                                            if (lapseInWorkSpan > lLateSeconds_AccumilatedForTheMonth_Local)
                                                            {
                                                                lLateSeconds_AccumilatedForTheMonth_Local = lapseInWorkSpan;
                                                                sBit += ",work span delay is considered";
                                                            }
                                                            else
                                                            {
                                                                sBit += ",shift work delay is considered";
                                                            }
                                                            if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                            {
                                                                lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                                sBit += ",login delay added";
                                                            }
                                                            sBit += "]";
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1)
                                                    {
                                                        lLateSeconds_AccumilatedForTheMonth_Local = lLateSeconds_AccumilatedForTheMonth_Local + row.logindelay;
                                                        sBit += " [login delay added]";
                                                    }

                                                    //if (dblALOPs_Local > 0) // ALOP marked
                                                    if (dblAbsent > 0) // ALOP marked
                                                    {
                                                        lLateSeconds_AccumilatedForTheMonth_Local = 0;
                                                        sBit += " [Absent marked. So delay ignored]";
                                                    }
                                                }
                                                if (lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                                {
                                                    string color = "orange";
                                                    if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1) color = "red";
                                                    row.sShortage += " (<span style='color:" + color + ";' ";
                                                    if (color.Equals("red")) row.sShortage += "title='Late login delay'";
                                                    row.sShortage += ">" +
                                                        MyGlobal.ToDateTimeFromEpoch(lLateSeconds_AccumilatedForTheMonth_Local).ToString("HH:mm:ss") +
                                                        "</span>)";
                                                }
                                                row.Working = sBit;
                                                lLateSeconds_AccumilatedForTheMonth += lLateSeconds_AccumilatedForTheMonth_Local;
                                                dblActualWorkingDays += dblActualWorkingDays_Local;
                                                //---------------------------
                                            }
                                        }// only if row.m_ShiftStart>0
                                    }
                                    else
                                    {
                                        // If iPhysicalPresenseNeeded==2 ... Double Pay
                                        dblDayTobePaid = dblPayIndex; // Add base pay
                                        if (row.m_MarkRoster.IndexOf('/') > -1)
                                        {
                                            long lShortage = (const_lShiftDuration / 2) - row.lWorkhours;
                                            if (lShortage > (2 * 3600))
                                            {
                                                row.Working += "[Work shortage is more than 2 hours. No additional Pay], ";
                                            }
                                            else if (lShortage > 0)
                                            {
                                                dblDayTobePaid += dblPayIndex * 0.5;
                                                lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                row.Working += "[Work shortage for half day added to delay], ";
                                            }
                                            else
                                            {
                                                dblDayTobePaid += dblPayIndex * 0.5;
                                            }
                                        }
                                        else
                                        {
                                            long lShortage = const_lShiftDuration - row.lWorkhours;
                                            if (lShortage > (4 * 3600)) // more than 4 hours
                                            {
                                                row.Working += "[Work shortage is more than 4 hours. No additional Pay], ";
                                            }
                                            else if (lShortage > (2 * 3600)) // more than 2 hours
                                            {
                                                dblDayTobePaid += dblPayIndex * 0.5;
                                                row.Working += "[More than 2 hours. Half day considered], ";
                                            }
                                            else if (lShortage > 0)
                                            {
                                                dblDayTobePaid += dblPayIndex;
                                                lLateSeconds_AccumilatedForTheMonth_Local = lShortage;
                                                row.Working += "[Additional Paid and delay added], ";
                                            }
                                            else
                                            {
                                                dblDayTobePaid += dblPayIndex;
                                            }
                                        }
                                        if (lLateSeconds_AccumilatedForTheMonth_Local > 0)
                                        {
                                            string color = "orange";
                                            if (row.logindelay > MyGlobal.const_ALLOWED_LATE_DELAY && row.m_LateLoginStatus != 1) color = "red";
                                            row.sShortage += " (<span style='color:" + color + ";' ";
                                            if (color.Equals("red")) row.sShortage += "title='Late login delay'";
                                            row.sShortage += ">" +
                                                MyGlobal.ToDateTimeFromEpoch(lLateSeconds_AccumilatedForTheMonth_Local).ToString("HH:mm:ss") +
                                                "</span>)";
                                        }
                                    }
                                    dblALOPs += dblALOPs_Local;     //  Absent LOPs from time based shortage
                                    dblALOPs += dblAdiLOPs_Local;   //  Leave like scanctioned ALOPs
                                    dblLOPs += dblLOPs_Local;
                                    dblAbsent_Total += dblAbsent;
                                    dblPaidLeaves += dblPaidLeaves_Local;
                                    row.dblDayTobePaid = dblDayTobePaid;
                                    dblDayTobePaidTotal += dblDayTobePaid;


                                    sUpdateSQL += "update " + MyGlobal.activeDB + "." + sDisplayTable + " " +
                                        "Set Working='" + row.Working + "'," +
                                        "sShortage=\"" + row.sShortage + "\"," +
                                        "dblDayTobePaid='" + dblDayTobePaid + "'," +
                                        "dblALOPs_Local='" + (dblALOPs_Local + dblAdiLOPs_Local) + "'," +
                                        "dblLOPs_Local='" + dblLOPs_Local + "'," +
                                        "dblPaidLeaves_Local='" + dblPaidLeaves_Local + "'," +
                                        "iNoOfOFFs='" + iNoOfOFFs + "'," +
                                        "lLateSeconds_AccumilatedForTheMonth_Local='" + lLateSeconds_AccumilatedForTheMonth_Local + "'," +
                                        "dblActualWorkingDays_Local='" + dblActualWorkingDays_Local + "'," +
                                        "dblAbsent='" + dblAbsent + "'," +
                                        "m_RosterOptions='" + sRosterOptions + "'," +
                                        "pay_scale='" + row.payscale + "'," +
                                        "pay_key='" + row.key + "'," +
                                        "pay_startdate='" + row.startdate + "' " +
                                        "where m_Profile='" + profile + "' and m_id='" + row.m_id + "';";
                                }
                                dblLOPBasedOnDelay = GetLOPBasedOnDelay(lLateSeconds_AccumilatedForTheMonth);
                                dblDayTobePaidTotal = dblDayTobePaidTotal - dblLOPBasedOnDelay;
                                workingdays = iScheduledWorkingDays;
                                lop = (dblLOPs + dblLOPBasedOnDelay);

                            }
                        }
                    }

                    if (sUpdateSQL.Length > 0) using (MySqlCommand mySqlCommand1 = new MySqlCommand(sUpdateSQL, con)) mySqlCommand1.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessAttendanceTable -> +" + ex.Message);
            }
        }//-------------End ProcessAttendanceTable function 
        //--------------------------------------------
        [HttpPost]
        public ActionResult ProcessAttendance()
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------
        public ActionResult LoadQATable(string profile, string sort, string order,
            string page, string search, string staffid, string staffidqa, string mode, string value,
            string year, string month, string day, string process)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var qaTableResponse = new QATableResponse();
            qaTableResponse.status = false;
            qaTableResponse.result = "";
            qaTableResponse.total_count = "";
            int iMonth = (MyGlobal.GetInt16(month) - 1);
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //________________________________________________________________
                    if (mode.Equals("new"))
                    {
                        bool bCreateNew = false;

                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_production_qatable " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                "and m_Year='" + year + "' and m_Month='" + iMonth + "' and m_Day='" + day + "' " +
                                "and m_QAInitials='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bCreateNew = !reader.HasRows;
                            }
                        }

                        if (!bCreateNew)
                        {
                            qaTableResponse.result = "<span style='color:red;'>Empty row already exists</span>";
                        }
                        else
                        {
                            int iSlNo = 0;
                            sSQL = "select max(m_QASlNo) as slno from " + MyGlobal.activeDB + ".tbl_production_qatable " +
                                "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                "and m_Year='" + year + "' and m_Month='" + iMonth + "' and m_Day='" + day + "'";

                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0)) iSlNo = reader.GetInt16(0);
                                        }
                                    }
                                }
                            }
                            iSlNo++;
                            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 19800;
                            DateTime date = DateTime.Now;
                            Int32 unixTimeDayStart = (Int32)((new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            sSQL = "Insert into " + MyGlobal.activeDB + ".tbl_production_qatable " +
                                "(m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_Process,m_QASlNo,m_QAInitials) values " +
                                "('" + profile + "','" + staffid + "','" + year + "'," +
                                "'" + iMonth + "','" + day + "','" + process + "','" + iSlNo + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                qaTableResponse.result = "<span style='color:blue;'>New empty row created</span>";
                            }
                        }
                    }
                    //________________________________________________________________
                    qaTableResponse.staffID = staffid;
                    qaTableResponse.staffIDQA = staffidqa;
                    sSQL = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) if (reader.HasRows) if (reader.Read()) if (!reader.IsDBNull(0)) qaTableResponse.staffName = reader.GetString(0);
                    sSQL = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs where m_Profile='" + profile + "' and m_StaffID='" + staffidqa + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) if (reader.HasRows) if (reader.Read()) if (!reader.IsDBNull(0)) qaTableResponse.staffNameQA = reader.GetString(0);
                    //________________________________________________________________
                    String sSearchKey = " (lst.m_Name like '%" + search + "%' or " +
                        "lst.m_Name like '%" + search + "%' or " +
                        "lst.m_Name like '%" + search + "%') ";
                    sSearchKey = "";

                    sSQL = "select  count(m_id) as cnt FROM " + MyGlobal.activeDB + ".tbl_production_qatable " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' and m_Month='" + iMonth + "' and m_Day='" + day + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) qaTableResponse.total_count = reader["cnt"].ToString();
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
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_QASlNo";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    /*
                    if (mode.Equals("new") && value.Length > 0)
                    {
                        sort = "lst.m_CreatedTime";
                        order = "desc";
                        PAGE = 0;
                    }
                    */


                    sSQL = "select * FROM " + MyGlobal.activeDB + ".tbl_production_qatable " +
                    "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                    "and m_Year='" + year + "' and m_Month='" + iMonth + "' and m_Day='" + day + "' ";

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    QATableItem item = new QATableItem();
                                    item.m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    item.m_QASlNo = MyGlobal.GetPureInt16(reader, "m_QASlNo");
                                    item.m_QAScore = MyGlobal.GetPureInt16(reader, "m_QAScore");
                                    item.m_QAInitials = MyGlobal.GetPureString(reader, "m_QAInitials");
                                    item.m_QATime = MyGlobal.GetPureString(reader, "m_QATime");
                                    item.m_QAComments = MyGlobal.GetPureString(reader, "m_QAComments");
                                    item.m_QATriggerType = MyGlobal.GetPureString(reader, "m_QATriggerType");
                                    item.m_QAHR = MyGlobal.GetPureString(reader, "m_QAHR");
                                    item.m_QAStripPosting = MyGlobal.GetPureString(reader, "m_QAStripPosting");
                                    item.m_QAStripCutting = MyGlobal.GetPureString(reader, "m_QAStripCutting");
                                    item.m_QAFindings = MyGlobal.GetPureString(reader, "m_QAFindings");
                                    item.m_QAMissedMDN = MyGlobal.GetPureString(reader, "m_QAMissedMDN");
                                    item.m_QAScore = MyGlobal.GetPureInt16(reader, "m_QAScore");
                                    item.m_QAFreeze = MyGlobal.GetPureInt16(reader, "m_QAFreeze");

                                    item.m_QASavedBy = MyGlobal.GetPureString(reader, "m_QASavedBy");
                                    item.m_QASavedTime = MyGlobal.GetPureString(reader, "m_QASavedTime");
                                    qaTableResponse.items.Add(item);
                                }
                                qaTableResponse.status = true;
                            }
                            else
                            {
                                qaTableResponse.result = "List is empty";
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("qaTableResponse-MySqlException-" + ex.Message);
                qaTableResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("qaTableResponse-Exception-" + ex.Message);
                qaTableResponse.result = "Error-" + ex.Message;
            }
            return Json(qaTableResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------
        [HttpPost]
        public void LoadPayslipPDFx(string profile)
        {
            PdfWriter pdfWriter = new PdfWriter(Server.MapPath("~/")+"data/payslip_pdfs/hello.pdf");
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            using (Document document = new Document(pdfDocument))
            {
                document.Add(new Paragraph("Hello World!"));
            }
        }
        //[HttpPost]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        //public  async Task<ActionResult> LoadPayslipPDF(string profile)
         public ActionResult LoadPayslipPDF(string profile, string email, string staffid, string year, string month,
    string preview, string pagerequested, string attnstartdate,string menukey,Boolean releasebonus)
        {
            /*
            PdfWriter pdfWriter = new PdfWriter(Server.MapPath("~/") + "data/payslip_pdfs/hello.pdf");
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            using (Document document = new Document(pdfDocument))
            {
                document.Add(new Paragraph("Hello World!"));
            }
            return Json(new { status=true, result="" }, JsonRequestBehavior.AllowGet);
            */
            LoadPayslip loadPayslip = LoadPayslip_Obj(profile, email, staffid, year, month, preview, pagerequested, attnstartdate, menukey, releasebonus);

            byte[] pdfBytes;
            using (var stream = new MemoryStream())
            using (var wri = new PdfWriter(stream))
            using (var pdf = new PdfDocument(wri))
            using (var doc = new Document(pdf))
            {
                PdfFont font = PdfFontFactory.CreateFont(FontConstants.HELVETICA);
                doc.SetFont(font).SetFontSize(10);
                /*
                doc.Add(new Paragraph("Hello World!"));
                Table table2 = new Table(UnitValue.CreatePercentArray(8)).UseAllAvailableWidth();

                for (int i = 0; i < 16; i++)
                {
                    table2.AddCell("hi");
                }
                doc.Add(table2);
                //-----------------
                Table table1 = new Table(UnitValue.CreatePercentArray(8)).UseAllAvailableWidth();

                for (int i = 0; i < 16; i++)
                {
                    table1.AddCell("hi");
                }
                doc.Add(table1);
                //------------------
                */
                /*
                Table table = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();

                Cell cell = new Cell().Add(new Paragraph("Cell 1"));
                cell.SetBorderTop(new SolidBorder(ColorConstants.RED, 0.1f));
                cell.SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 1));
                table.AddCell(cell);

                cell = new Cell().Add(new Paragraph("Cell 2"));
                cell.SetBorderLeft(new SolidBorder(ColorConstants.GREEN, 5));
                cell.SetBorderTop(new SolidBorder(ColorConstants.YELLOW, 8));
                table.AddCell(cell);

                cell = new Cell().Add(new Paragraph("Cell 3"));
                cell.SetBorderLeft(new SolidBorder(ColorConstants.RED, 1));
                cell.SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 1));
                table.AddCell(cell);

                doc.Add(table);
                */
                //---------------------------------
                Paragraph p1;
                Cell cell;
                string path = "";
                Table tableLogo = new Table(UnitValue.CreatePercentArray(new float[] { 0.7f,2, 3}));
                tableLogo.SetWidth(UnitValue.CreatePercentValue(100));
                
                try
                {
                    path = Path.Combine(Server.MapPath("~/data/logos/"), "hw.png"); //"healthwatch.png" hw1024
                    Image img1 = new Image(ImageDataFactory.Create(path));
                    cell = new Cell().SetBorder(Border.NO_BORDER).Add(img1.SetAutoScale(true));
                    tableLogo.AddCell(cell);
                }
                catch(Exception ex)
                {
                    cell = new Cell().SetBorder(Border.NO_BORDER).Add(new Paragraph(ex.Message));
                    tableLogo.AddCell(cell);
                }


                p1 = new Paragraph("").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("");
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableLogo.AddCell(cell);

                p1 = new Paragraph("Healthwatch TeleDiagnostics Pvt. Ltd.").SetFontSize(12).SetTextAlignment(TextAlignment.CENTER); //SetBold().
                p1.SetFixedLeading(5);
                p1.SetMultipliedLeading(1);

                Paragraph p2 = new Paragraph("Payslip for " + loadPayslip.sMonth + " " + loadPayslip.iYear).SetTextAlignment(TextAlignment.CENTER);

                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1).Add(p2);
                cell.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                tableLogo.AddCell(cell);

                doc.Add(tableLogo);
                //---------------------------------
                Table tableHeader = new Table(UnitValue.CreatePercentArray(new float[] { 2.4f, 2.3f, 8.5f, 2.3f, 3.3f }));
                tableHeader.SetWidth(UnitValue.CreatePercentValue(100));
                //"http://" + APIDomain + "/handlers/GetDvrPhoto.ashx?staffid=" +                       getCookie('login_profile') + "_" + this.data.email + "&dummy=" + this.global.iImageRandamize;

                try
                {
                    //path = Path.Combine(Server.MapPath("~/data/dvrphotos/"), "support@SharewareDreams.com_ssheetal@chcgroup.in.jpg");
                    path = Path.Combine(Server.MapPath("~/data/dvrphotos/"), profile + "_" + loadPayslip.email + ".jpg");
                    Image img1 = new Image(ImageDataFactory.Create(path));
                    cell = new Cell(4,1).SetBorderRight(Border.NO_BORDER).Add(img1.SetAutoScale(true));
                    tableHeader.AddCell(cell);
                }
                catch (Exception ex)
                {
                    //cell = new Cell(4, 1).SetBorderRight(Border.NO_BORDER).Add(new Paragraph(ex.Message));
                    //tableHeader.AddCell(cell);
                    path = Path.Combine(Server.MapPath("~/data/dvrphotos/"), "dummy.jpg");
                    Image img1 = new Image(ImageDataFactory.Create(path));
                    cell = new Cell(4, 1).SetBorderRight(Border.NO_BORDER).Add(img1.SetAutoScale(true));
                    tableHeader.AddCell(cell);
                }

                /*------------row 1---------------*/
                p1 = new Paragraph("Emp Code:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.1f)).Add(p1);
                cell.SetWidth(34f);
                tableHeader.AddCell(cell);
                p1 = new Paragraph(loadPayslip.staffid).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.1f)).Add(p1);
                cell.SetPaddingLeft(0f);
                tableHeader.AddCell(cell);

                p1 = new Paragraph("EPF-UAN:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);
                p1 = new Paragraph(loadPayslip.epf_uan).SetFontColor(new DeviceRgb(99, 99, 99)).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.1f)).SetBorderRight(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);
                /*------------row 2---------------*/
                p1 = new Paragraph("Emp Name:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableHeader.AddCell(cell);
                if (loadPayslip.name.Length > 20)
                {
                    p1 = new Paragraph(loadPayslip.name).SetTextAlignment(TextAlignment.LEFT).SetFontSize(8f);
                }
                else
                {
                    p1 = new Paragraph(loadPayslip.name).SetTextAlignment(TextAlignment.LEFT);
                }
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableHeader.AddCell(cell);
                /*
                p1 = new Paragraph("").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableHeader.AddCell(cell);
                */
                // row space
                //p1 = new Paragraph("").SetFontColor(new DeviceRgb(99, 99, 99));
                //cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                //tableHeader.AddCell(cell);
                p1 = new Paragraph("Bank A/c No:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableHeader.AddCell(cell);
                p1 = new Paragraph(loadPayslip.sb_acc).SetFontColor(new DeviceRgb(99, 99, 99)).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderRight(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);
                /*------------row 3---------------*/
                p1 = new Paragraph("Grade:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                tableHeader.AddCell(cell);
                p1 = new Paragraph(loadPayslip.grade).SetFontColor(new DeviceRgb(99, 99, 99)).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell(1, 3).SetBorder(Border.NO_BORDER).SetBorderRight(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);

                /*
                p1 = new Paragraph("CTC:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);
                p1 = new Paragraph(loadPayslip.CTC).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(0.1f)).Add(p1);
                cell.SetPaddingLeft(0f);
                tableHeader.AddCell(cell);
                */
                /*------------row 4---------------*/
                p1 = new Paragraph("Designation:").SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);

                p1 = new Paragraph(loadPayslip.designation).SetFontColor(new DeviceRgb(99, 99, 99)).SetTextAlignment(TextAlignment.LEFT);
                cell = new Cell(1,3).SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(0.1f)).SetBorderRight(new SolidBorder(0.1f)).Add(p1);
                tableHeader.AddCell(cell);


                /*                
                cell = new Cell().SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER).SetFont(font).Add(GetHeaderTable1(loadPayslip));
                tableHeader.AddCell(cell);
                cell = new Cell().SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER).SetFont(font).Add(GetHeaderTable2(loadPayslip));
                tableHeader.AddCell(cell);
                cell = new Cell().SetBorderLeft(Border.NO_BORDER).SetFont(font).Add(GetHeaderTable3(loadPayslip));
                tableHeader.AddCell(cell);
                */

                doc.Add(tableHeader);
                //---------------------------------


                Table tableHead = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
                tableHead.SetMarginTop(2);
                //p1 = new Paragraph("Rate").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("Amount in Rs.");
                //cell = new Cell().SetBackgroundColor(new DeviceRgb(235, 235, 235)).SetBorderBottom(Border.NO_BORDER).Add(p1);
                //tableHead.AddCell(cell);
                p1 = new Paragraph("Earnings").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("Amount in Rs.");
                cell = new Cell().SetBackgroundColor(new DeviceRgb(235, 235, 235)).SetBorderBottom(Border.NO_BORDER).Add(p1);
                tableHead.AddCell(cell);
                p1 = new Paragraph("Deductions").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("Amount in Rs.");
                cell = new Cell().SetBackgroundColor(new DeviceRgb(235, 235, 235)).SetBorderBottom(Border.NO_BORDER).Add(p1);
                tableHead.AddCell(cell);
                doc.Add(tableHead);
                //---------------------------------
                Table tableBody = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
                //cell = new Cell().Add(GetRateTable(loadPayslip));
                //tableBody.AddCell(cell);
                cell = new Cell().Add(GetEarningsTable(loadPayslip));
                tableBody.AddCell(cell);
                cell = new Cell().Add(GetDeductionsTable(loadPayslip));
                tableBody.AddCell(cell);
                /*
                p1 = new Paragraph("Basic").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("12,000.00");
                cell = new Cell().Add(p1);
                tableBody.AddCell(cell);
                p1 = new Paragraph("Basic").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("12,000.00");
                cell = new Cell().Add(p1);
                tableBody.AddCell(cell);
                */
                doc.Add(tableBody);
                //---------------------------------


                Table tableBottom = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
                //p1 = new Paragraph("").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT))
                //    .Add(@String.Format("{0:0.00}", loadPayslip.m_CrTot));
                //cell = new Cell().SetBorderTop(Border.NO_BORDER).Add(p1);
                //tableBottom.AddCell(cell);
                p1 = new Paragraph("GROSS EARNINGS").SetBold().Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT))
                    .Add(@String.Format("{0:0.00}", loadPayslip.m_EarnsTot));
                cell = new Cell().SetBorderTop(Border.NO_BORDER).Add(p1);
                tableBottom.AddCell(cell);
                p1 = new Paragraph("GROSS DEDUCTIONS").SetBold().Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT))
                    .Add(@String.Format("{0:0.00}", loadPayslip.m_DeductsTot));
                cell = new Cell().SetBorderTop(Border.NO_BORDER).Add(p1);
                tableBottom.AddCell(cell);
                doc.Add(tableBottom);
                //---------------------------------
                Table tableFooter = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3 })).UseAllAvailableWidth();
                p1 = new Paragraph("Total Working Days: " + loadPayslip.m_ActualWorkingDays + ", Total Pay Days: " + loadPayslip.m_DaysToBePaidTotal);
                cell = new Cell()
                    .SetBorderRight(Border.NO_BORDER)
                    .SetBackgroundColor(new DeviceRgb(235, 235, 235)).Add(p1);
                tableFooter.AddCell(cell);

                p1 = new Paragraph("NET PAY    " + @String.Format("{0:0.00}", (loadPayslip.m_EarnsTot - loadPayslip.m_DeductsTot)))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBold();
                cell = new Cell()
                    .SetBorderLeft(Border.NO_BORDER)
                    .SetBackgroundColor(new DeviceRgb(235, 235, 235)).Add(p1);
                tableFooter.AddCell(cell);

                doc.Add(tableFooter);
                //-------------------------------Net Pay in words
                Table tableNetWords = new Table(UnitValue.CreatePercentArray(new float[] { 100 })).UseAllAvailableWidth();
                p1 = new Paragraph(loadPayslip.NetPayWords).SetTextAlignment(TextAlignment.RIGHT).SetBold();
                cell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetBackgroundColor(new DeviceRgb(255, 255, 255)).Add(p1);
                tableNetWords.AddCell(cell);

                doc.Add(tableNetWords);
                //-------------------------------Net Pay in words END
                //PdfFont font = PdfFontFactory.CreateFont(FontConstants.TIMES_ROMAN);
                //PdfFont bold = PdfFontFactory.CreateFont(FontConstants.TIMES_BOLD);
                //Text title = new Text("Basic");//.SetFont(bold);

                //---------------
                Table tableStatus = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3 }));
                tableStatus.SetWidth(UnitValue.CreatePercentValue(100));

                p1 = new Paragraph("Payscale "+ loadPayslip.m_PayscaleName+" - "+ MyGlobal.ToDateTimeFromEpoch(loadPayslip.m_PayscaleKey).ToString("dd-MM-yyyy")).SetHorizontalAlignment(HorizontalAlignment.LEFT).SetFontColor(new DeviceRgb(99, 99, 99));
                p2 = new Paragraph("Start Date "+ MyGlobal.ToDateTimeFromEpoch(loadPayslip.m_PayscaleStartDate).ToString("dd-MM-yyyy")).SetHorizontalAlignment(HorizontalAlignment.LEFT).SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1)).Add(p1).Add(p2);
                //cell.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                tableStatus.AddCell(cell);

                p1 = new Paragraph("From "+ MyGlobal.ToDateTimeFromEpoch(loadPayslip.m_DateStart).ToString("dd-MM-yyyy") + " to "+ MyGlobal.ToDateTimeFromEpoch(loadPayslip.m_DateEnd).ToString("dd-MM-yyyy")).SetTextAlignment(TextAlignment.RIGHT).SetFontColor(new DeviceRgb(99, 99, 99));
                p2 = new Paragraph("Created on " + loadPayslip.m_CreatedTime).SetTextAlignment(TextAlignment.RIGHT).SetFontColor(new DeviceRgb(99, 99, 99));
                cell = new Cell().SetBorder(Border.NO_BORDER).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1)).Add(p1).Add(p2);
                tableStatus.AddCell(cell);
                doc.Add(tableStatus);
                //---------------  
                Table tableDisclaimer = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
                tableDisclaimer.SetWidth(UnitValue.CreatePercentValue(100));
                p1 = new Paragraph("This is a computer generated statement and does not need any signature.").SetTextAlignment(TextAlignment.LEFT).SetFontColor(new DeviceRgb(99, 99, 99)).SetFontSize(8);
                p2 = new Paragraph("Discrepancy if any noted, should be intimated to TAM department within two days.").SetTextAlignment(TextAlignment.LEFT).SetFontColor(new DeviceRgb(99, 99, 99)).SetFontSize(8);
                cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1).Add(p2);
                tableDisclaimer.AddCell(cell);
                doc.Add(tableDisclaimer);
                //----------------

                /*
                PdfCanvas canvas = new PdfCanvas(pdf.AddNewPage())
            //.SetStrokeColor(Color..BLACK)
            //.SetFillColor(Color.GRAY)
            .RoundRectangle(100, 100, 100, 100, 10)
            .Fill()
            .Rectangle(100, 210, 100, 100)
            .Fill()
            //.SetFillColor(Color.WHITE)
            .SetLineWidth(0)
            .RoundRectangle(210, 100, 100, 100, 10)
            .Stroke()
            .Rectangle(210, 210, 100, 100)
            .Stroke();
            */

                //----------------------------------End
                doc.Close();
                doc.Flush();
                pdfBytes = stream.ToArray();
            }
            return new FileContentResult(pdfBytes, "application/pdf");
        }
        private Table GetRateTable(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            foreach (PayLedger ledger in loadPayslip.ratesCr)
            {
                Paragraph p1 = new Paragraph(ledger.Name).Add(new Tab())
                    .AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add(@String.Format("{0:0.00}", ledger.Amount));
                Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                table.AddCell(cell);
            }
            /*Paragraph p1 = new Paragraph("Basic").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("12,000.00");
            Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph("HRA").Add(new Tab()).AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add("15,000.00");
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);*/
            return table;
        }
        private Table GetEarningsTable(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            foreach (PayLedger ledger in loadPayslip.earns)
            {
                Paragraph p1 = new Paragraph(ledger.Name).Add(new Tab())
                    .AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add(@String.Format("{0:0.00}", ledger.Amount));
                Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                table.AddCell(cell);
            }
            return table;
        }
        private Table GetDeductionsTable(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            foreach (PayLedger ledger in loadPayslip.deducts)
            {
                Paragraph p1 = new Paragraph(ledger.Name).Add(new Tab())
                    .AddTabStops(new TabStop(1000, TabAlignment.RIGHT)).Add(@String.Format("{0:0.00}", ledger.Amount));
                Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
                table.AddCell(cell);
            }
            return table;
        }
        private Table GetHeaderTable1(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            //table.SetMarginRight(10);
            Paragraph p1 = new Paragraph("Emp Code:").SetFontColor(new DeviceRgb(99, 99, 99));
            Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            cell.SetMinWidth(54f);
            //cell.SetNoWrap(true);
            //cell.SetHeight(31.0f);
            //cell.SetPaddingRight(0f);
            table.AddCell(cell);

            p1 = new Paragraph(loadPayslip.staffid).SetTextAlignment(TextAlignment.LEFT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            cell.SetPaddingLeft(0f);
            table.AddCell(cell);

            p1 = new Paragraph("Emp Name:").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            //cell.SetPaddingRight(0f);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.name).SetTextAlignment(TextAlignment.LEFT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            cell.SetPaddingLeft(0f);
            table.AddCell(cell);

            p1 = new Paragraph("CTC:").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            //cell.SetPaddingRight(0f);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.CTC).SetTextAlignment(TextAlignment.LEFT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            cell.SetPaddingLeft(0f);
            table.AddCell(cell);
            return table;
        }
        private Table GetHeaderTable2(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            //table.SetMarginRight(10);
            /*
            Paragraph p1 = new Paragraph("Band").SetFontColor(new DeviceRgb(99, 99, 99));
            Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph("Managerial").SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            */
            Paragraph p1 = new Paragraph("Grade:").SetFontColor(new DeviceRgb(99, 99, 99));
            Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.grade).SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);

            p1 = new Paragraph("Designation:").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.designation).SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);

            p1 = new Paragraph("").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph("").SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);

            return table;
        }
        private Table GetHeaderTable3(LoadPayslip loadPayslip)
        {
            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 5, 10 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            //table.SetMarginRight(10);
            Paragraph p1 = new Paragraph("EPF-UAN:").SetFontColor(new DeviceRgb(99, 99, 99));
            Cell cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.epf_uan).SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);

            p1 = new Paragraph("Bank A/c No.:").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph(loadPayslip.sb_acc).SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);

            p1 = new Paragraph("").SetFontColor(new DeviceRgb(99, 99, 99));
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            p1 = new Paragraph("").SetTextAlignment(TextAlignment.RIGHT);
            cell = new Cell().SetBorder(Border.NO_BORDER).Add(p1);
            table.AddCell(cell);
            return table;
        }
        //----------------------------------------------------mount in words End
        private string GetAmountInWords(string numb)
        {
            String val = "", wholeNo = numb, points = "", andStr = "", pointStr = "";
            String endStr = "Only";
            try
            {
                int decimalPlace = numb.IndexOf(".");
                if (decimalPlace > 0)
                {
                    wholeNo = numb.Substring(0, decimalPlace);
                    points = numb.Substring(decimalPlace + 1);
                    if (Convert.ToInt32(points) > 0)
                    {
                        andStr = "and";// just to separate whole numbers from points/cents    
                        endStr = "Paisa " + endStr;//Cents    
                        pointStr = ConvertDecimals(points);
                    }
                }
                val = String.Format("{0} {1}{2} {3}", ConvertWholeNumber(wholeNo).Trim(), andStr, pointStr, endStr);
            }
            catch { }
            return val;
        }
        private static String ConvertDecimals(String number)
        {
            String cd = "", digit = "", engOne = "";
            for (int i = 0; i < number.Length; i++)
            {
                digit = number[i].ToString();
                if (digit.Equals("0"))
                {
                    engOne = "Zero";
                }
                else
                {
                    engOne = ones(digit);
                }
                cd += " " + engOne;
            }
            return cd;
        }
        private static String ConvertWholeNumber(String Number)
        {
            string word = "";
            try
            {
                bool beginsZero = false;//tests for 0XX    
                bool isDone = false;//test if already translated    
                double dblAmt = (Convert.ToDouble(Number));
                //if ((dblAmt > 0) && number.StartsWith("0"))    
                if (dblAmt > 0)
                {//test for zero or digit zero in a nuemric    
                    beginsZero = Number.StartsWith("0");

                    int numDigits = Number.Length;
                    int pos = 0;//store digit grouping    
                    String place = "";//digit grouping name:hundres,thousand,etc...    
                    switch (numDigits)
                    {
                        case 1://ones' range    

                            word = ones(Number);
                            isDone = true;
                            break;
                        case 2://tens' range    
                            word = tens(Number);
                            isDone = true;
                            break;
                        case 3://hundreds' range    
                            pos = (numDigits % 3) + 1;
                            place = " Hundred ";
                            break;
                        case 4://thousands' range    
                        case 5:
                        case 6:
                            pos = (numDigits % 4) + 1;
                            place = " Thousand ";
                            break;
                        case 7://millions' range    
                        case 8:
                        case 9:
                            pos = (numDigits % 7) + 1;
                            place = " Million ";
                            break;
                        case 10://Billions's range    
                        case 11:
                        case 12:

                            pos = (numDigits % 10) + 1;
                            place = " Billion ";
                            break;
                        //add extra case options for anything above Billion...    
                        default:
                            isDone = true;
                            break;
                    }
                    if (!isDone)
                    {//if transalation is not done, continue...(Recursion comes in now!!)    
                        if (Number.Substring(0, pos) != "0" && Number.Substring(pos) != "0")
                        {
                            try
                            {
                                word = ConvertWholeNumber(Number.Substring(0, pos)) + place + ConvertWholeNumber(Number.Substring(pos));
                            }
                            catch { }
                        }
                        else
                        {
                            word = ConvertWholeNumber(Number.Substring(0, pos)) + ConvertWholeNumber(Number.Substring(pos));
                        }

                        //check for trailing zeros    
                        //if (beginsZero) word = " and " + word.Trim();    
                    }
                    //ignore digit grouping names    
                    if (word.Trim().Equals(place.Trim())) word = "";
                }
            }
            catch { }
            return word.Trim();
        }
        private static String ones(String Number)
        {
            int _Number = Convert.ToInt32(Number);
            String name = "";
            switch (_Number)
            {

                case 1:
                    name = "One";
                    break;
                case 2:
                    name = "Two";
                    break;
                case 3:
                    name = "Three";
                    break;
                case 4:
                    name = "Four";
                    break;
                case 5:
                    name = "Five";
                    break;
                case 6:
                    name = "Six";
                    break;
                case 7:
                    name = "Seven";
                    break;
                case 8:
                    name = "Eight";
                    break;
                case 9:
                    name = "Nine";
                    break;
            }
            return name;
        }
        private static String tens(String Number)
        {
            int _Number = Convert.ToInt32(Number);
            String name = null;
            switch (_Number)
            {
                case 10:
                    name = "Ten";
                    break;
                case 11:
                    name = "Eleven";
                    break;
                case 12:
                    name = "Twelve";
                    break;
                case 13:
                    name = "Thirteen";
                    break;
                case 14:
                    name = "Fourteen";
                    break;
                case 15:
                    name = "Fifteen";
                    break;
                case 16:
                    name = "Sixteen";
                    break;
                case 17:
                    name = "Seventeen";
                    break;
                case 18:
                    name = "Eighteen";
                    break;
                case 19:
                    name = "Nineteen";
                    break;
                case 20:
                    name = "Twenty";
                    break;
                case 30:
                    name = "Thirty";
                    break;
                case 40:
                    name = "Fourty";
                    break;
                case 50:
                    name = "Fifty";
                    break;
                case 60:
                    name = "Sixty";
                    break;
                case 70:
                    name = "Seventy";
                    break;
                case 80:
                    name = "Eighty";
                    break;
                case 90:
                    name = "Ninety";
                    break;
                default:
                    if (_Number > 0)
                    {
                        name = tens(Number.Substring(0, 1) + "0") + " " + ones(Number.Substring(1));
                    }
                    break;
            }
            return name;
        }
        //----------------------------------------------------mount in words End
    }
    
}

