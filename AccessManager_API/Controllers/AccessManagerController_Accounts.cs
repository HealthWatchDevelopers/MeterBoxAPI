using MyHub.Hubs;
using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public partial class AccessmanagerController : Controller
    {

        //[HttpPost]
        public ActionResult LoadAccountsLedgers(string profile,
            string sort, string order, string page, string search, string ledgerFilter)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadLedgersResponse = new LoadAccountsLedgersResponse();
            loadLedgersResponse.status = false;
            loadLedgersResponse.result = "";
            loadLedgersResponse.total_count = "";
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSearchKey =
                        " (m_Ledger like '%" + search + "%' or " +
                        "m_FName like '%" + search + "%') ";
                    if (ledgerFilter.Equals("1"))
                        sSearchKey += "and ledgers.m_Type='salary' and ledgers.m_Type is not null ";
                    else if (ledgerFilter.Equals("2"))
                        sSearchKey += "and (ledgers.m_Type!='salary' or ledgers.m_Type is null) ";
                    /*
                    sSQL = "select count(m_id) as cnt from " +
                        "(select m_id from " + MyGlobal.activeDB + ".tbl_accounts accounts " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "group by m_Ledger) as x;";
                    */
                    sSQL = "select count(m_id) as cnt from " +
                    "(SELECT accounts.m_id FROM " + MyGlobal.activeDB + ".tbl_accounts accounts  " +
    "left join " + MyGlobal.activeDB + ".tbl_accounts_ledgers ledgers on ledgers.m_Name=accounts.m_Ledger and ledgers.m_Profile=accounts.m_Profile " +
"where " + sSearchKey + " and accounts.m_Profile='" + profile + "' " +
"group by m_Ledger) as x;";


                    sSQL = "SELECT count(accounts.m_id) as cnt FROM meterbox.tbl_accounts accounts " +
                        "left join meterbox.tbl_staffs staffs on staffs.m_StaffID=accounts.m_StaffID and staffs.m_Profile=accounts.m_Profile " +
    "where " + sSearchKey + " and accounts.m_Profile='" + profile + "' " +
    "group by m_Ledger";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            /*
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) loadLedgersResponse.total_count = reader["cnt"].ToString();
                                }
                            }*/
                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            loadLedgersResponse.total_count = dt.Rows.Count.ToString();
                        }
                    }

                    //------------------------------
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Ledger";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    //  where m_Profile='grey' 
                    sSQL = "SELECT accounts.m_id,m_Ledger,sum(m_Cr) as Cr,sum(m_Dr) as Dr,ledgers.m_Type,m_FName FROM " + MyGlobal.activeDB + ".tbl_accounts accounts " +
                        "left join " + MyGlobal.activeDB + ".tbl_accounts_ledgers ledgers on ledgers.m_Name=accounts.m_Ledger and ledgers.m_Profile=accounts.m_Profile " +
                        "left join meterbox.tbl_staffs staffs on staffs.m_StaffID=accounts.m_StaffID and staffs.m_Profile=accounts.m_Profile " +
                    "where " + sSearchKey + " and accounts.m_Profile='" + profile + "' " +
                    "group by m_Ledger " +
                    "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    LedgerItem row = new LedgerItem();
                                    if (!reader.IsDBNull(0)) row.m_id = reader.GetInt32(0);
                                    if (!reader.IsDBNull(1)) row.Ledger = reader[1].ToString();
                                    if (!reader.IsDBNull(2) && !reader.IsDBNull(3)) row.Amount = Math.Round(reader.GetDouble(2) - reader.GetDouble(3), 2);
                                    if (!reader.IsDBNull(4)) row.Type = reader[4].ToString();

                                    if (row.Type != null && row.Type.Equals("Salary"))
                                    {
                                        //row.Name = GetStaffName_(profile, row.Ledger);
                                        row.Name = reader.IsDBNull(5) ? "" : reader[5].ToString();
                                    }

                                    loadLedgersResponse.rows.Add(row);
                                }
                                loadLedgersResponse.status = true;
                            }
                            else
                            {

                                loadLedgersResponse.result = "No Ledgers";
                                if (sSearchKey.Length > 0) loadLedgersResponse.result += " [Search has value]";
                            }
                        }
                    }
                    //------------------------------
                }
            }
            catch (MySqlException ex)
            {
                loadLedgersResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                loadLedgersResponse.result = "Error-" + ex.Message;
            }
            return Json(loadLedgersResponse, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadAccountsDetails(string profile,
    string sort, string order, string page, string search,
    string ledger, string name, string detailsFilter)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var loadLedgersResponse = new LoadAccountsDetailsResponse();
            loadLedgersResponse.status = false;
            loadLedgersResponse.result = "";
            loadLedgersResponse.total_count = "";
            loadLedgersResponse.ledger = ledger;
            loadLedgersResponse.name = name;
            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //--------------------------Get total----------------------------------
                    sSQL = "SELECT sum(m_Cr) as Cr,sum(m_Dr) as Dr,m_Type FROM " + MyGlobal.activeDB + ".tbl_accounts accounts " +
    "left join " + MyGlobal.activeDB + ".tbl_accounts_ledgers ledgers on ledgers.m_Name=accounts.m_Ledger and ledgers.m_Profile=accounts.m_Profile " +
"where accounts.m_Profile='" + profile + "' and m_Ledger='" + ledger + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    loadLedgersResponse.CrTot = 0;
                                    loadLedgersResponse.DrTot = 0;
                                    if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                    {
                                        double dbl = reader.GetDouble(0) - reader.GetDouble(1);
                                        if (dbl > 0) loadLedgersResponse.CrTot = dbl;
                                        else loadLedgersResponse.DrTot = dbl;
                                    }
                                }
                            }
                        }
                    }

                    //------------------------------------------------------------

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_accounts accounts " +
                    "where  m_Profile='" + profile + "' and m_Ledger='" + ledger + "' ";
                    if (detailsFilter.Equals("1"))
                        sSQL += "group by m_VchNo ";
                    //sSQL += "group by m_Time,m_Description ";



                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null) loadLedgersResponse.total_count = reader["cnt"].ToString();
                                }
                            }
                        }
                    }
                    //------------------------------
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Time";
                    if (order.Equals("undefined") || order.Length == 0) order = "desc";
                    if (detailsFilter.Equals("1"))
                    {
                        sSQL = "SELECT m_id,UNIX_TIMESTAMP(m_Time),m_Head,m_Description," +
                        "sum(m_Cr) as Cr,sum(m_Dr) as Dr,m_VchNo,m_Reversed FROM " + MyGlobal.activeDB + ".tbl_accounts " +
                        "where  m_Profile='" + profile + "' and m_Ledger='" + ledger + "' " +
                        "group by m_VchNo,m_Reversed " +
                        "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                        //"group by m_Time,m_Description " +
                    }
                    else
                    {
                        sSQL = "SELECT m_id,UNIX_TIMESTAMP(m_Time),m_Head,m_Description," +
                            "m_Cr,m_Dr,m_VchNo,m_Reversed FROM " + MyGlobal.activeDB + ".tbl_accounts " +
                        "where  m_Profile='" + profile + "' and m_Ledger='" + ledger + "' " +
                        "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    }
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Int32 iLastVchNo = 0;
                                bool bReverseStatus = false;
                                while (reader.Read())
                                {
                                    DetailItem row = new DetailItem();
                                    if (!reader.IsDBNull(0)) row.m_id = reader.GetInt32(0);
                                    bool rev = reader.IsDBNull(7) ? false : reader.GetBoolean(7);
                                    if (reader.IsDBNull(6))
                                    {
                                        row.Time = reader.GetInt32(1);
                                    }
                                    else
                                    {
                                        if ((iLastVchNo != reader.GetInt32(6)) || (bReverseStatus != rev))
                                        {

                                            iLastVchNo = reader.GetInt32(6);
                                            bReverseStatus = rev;
                                            row.Time = reader.GetInt32(1);
                                        }
                                        else
                                        {
                                            row.Time = 0;
                                        }
                                    }
                                    row.rev = rev;
                                    if (!reader.IsDBNull(2)) row.Head = reader.GetString(2);
                                    if (!reader.IsDBNull(3)) row.Description = reader.GetString(3);
                                    if (!reader.IsDBNull(4)) row.Cr = reader.GetDouble(4);
                                    if (!reader.IsDBNull(5)) row.Dr = reader.GetDouble(5);
                                    row.Notes = GetVoucherNotes(profile, iLastVchNo);
                                    loadLedgersResponse.rows.Add(row);
                                }
                                loadLedgersResponse.status = true;
                            }
                            else
                            {
                                loadLedgersResponse.result = "Sorry!!! No Details";
                            }
                        }
                    }
                    //------------------------------
                }
            }
            catch (MySqlException ex)
            {
                loadLedgersResponse.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                loadLedgersResponse.result = "Error-" + ex.Message;
            }
            return Json(loadLedgersResponse, JsonRequestBehavior.AllowGet);
        }
        private string GetVoucherNotes(string profile, Int32 vchNo)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT m_Notes " +
                    "FROM " + MyGlobal.activeDB + ".tbl_accounts_notes " +
                    "where m_Profile = '" + profile + "' and m_VchNo = '" + vchNo + "' limit 1;";
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
        private string GetStaffName_(string profile, string staffid)
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                string sSQL = "SELECT m_FName " +
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
                                if (!reader.IsDBNull(0)) return reader.GetString(0);
                            }
                        }
                    }
                }
            }
            return "";
        }
        //------------------------------------------------
        [HttpPost]
        public ActionResult Statement_Accounts_to_Excel(string profile, string bank, string list, int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new StatementAccountsExcelResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.ret_filename = "Accounts_Statement_" + MyGlobal.constArrayMonths[month - 1] + "_" + year;
            statementResponse.bank = bank;
            statementResponse.list = list;
            string sSQL = "select m_BankDate from " + MyGlobal.activeDB + ".tbl_bank_list " +
            "where m_Profile='" + profile + "' " +
            "and (m_List is not null and m_List='" + list + "') " +
            "and m_Year='" + year + "' and m_Month='" + (month - 1) + "' " +
            "and m_Bank='" + bank + "'";
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
                                    if (!reader.IsDBNull(0))
                                    {
                                        statementResponse.txtBankDate = reader.GetString(0);
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------

                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    "where m_Profile='" + profile + "' " +
                    "and (m_List is not null and m_List='" + list + "') " +
                    "and m_Year='" + year + "' and m_Month='" + (month - 1) + "' " +
                    "and m_Bank='" + bank + "'";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int i = 1;
                                while (reader.Read())
                                {
                                    Statement_Accounts_ExcelRow row = new Statement_Accounts_ExcelRow();
                                    //row.m_id = MyGlobal.GetPureInt32(reader, "m_id");
                                    row.NAME = MyGlobal.GetPureString(reader, "m_Name");
                                    row.StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.SB_Acc = MyGlobal.GetPureString(reader, "m_sb_acc");
                                    double dblEars = reader.IsDBNull(reader.GetOrdinal("m_EarnsTot")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_EarnsTot"));
                                    double dblDeducts = reader.IsDBNull(reader.GetOrdinal("m_DeductsTot")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_DeductsTot"));
                                    row.Amount = String.Format("{0:n}", Math.Round((dblEars - dblDeducts), 2));
                                    statementResponse.rows.Add(row);
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
                MyGlobal.Error("MySqlException--Statement_PF_to_Excel--" + ex.Message);
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------
        [HttpPost]
        public ActionResult Statement_PF_to_Excel(string profile, int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new StatementPFExcelResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.ret_filename = "PF_Statement_" +
                MyGlobal.constArrayMonths[month - 1] + "_" + year;
            /*
            Statement_PF_ExcelRow row = new Statement_PF_ExcelRow();
            
            row.m_id = 111;
            row.name = "Eugene";
            row.staffid = "10000";
            statementResponse.rows.Add(row);
            Statement_PF_ExcelRow row1 = new Statement_PF_ExcelRow();
            row1.m_id = 222;
            row1.name = "Anita";
            row1.staffid = "10001";
            statementResponse.rows.Add(row1);
            */
            /*
            string sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
    "list.m_EarnsTot,list.m_DeductsTot," +
    "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
    "list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List," +
    "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_BasicPay,list.m_EPFContributionRemitted " +
    "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
    "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
    "on summary.m_StaffID=list.m_StaffID " +
    "and summary.m_Profile=list.m_Profile " +
    "and summary.m_Year=list.m_Year " +
    "and summary.m_Month=list.m_Month " +
    "where list.m_Profile='" + profile + "' " +
    "and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";
    */
            string sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
"list.m_EarnsTot,list.m_DeductsTot," +
"'','',''," +
"list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List," +
"list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_BasicPay,list.m_EPFContributionRemitted " +
"FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
"where list.m_Profile='" + profile + "' " +
"and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";
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
                                    Statement_PF_ExcelRow row = new Statement_PF_ExcelRow();
                                    row.Name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.UAN = MyGlobal.GetPureString(reader, "m_epf_uan");
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.StaffID, year, month,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                    }
                                    else
                                    {
                                        m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));
                                    }
                                    */
                                    //---------------------------------

                                    row.GROSS_WAGES = MyGlobal.GetPureDouble(reader, "m_GrossWages");

                                    row.EPF_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.EPS_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.EDLI_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.NCP_DAYS = 0;
                                    row.REFUND_OF_ADVANCES = 0;

                                    if (MyGlobal.GetPureDouble(reader, "m_EPFContributionRemitted") > 0)
                                    {
                                        row.EPF_CONTRI_REMITTED = MyGlobal.GetPureDouble(reader, "m_EPFContributionRemitted");
                                        row.EPS_CONTRI_REMITTED = Math.Round((row.EPF_WAGES * 0.0833), 2);
                                        row.EPF_EPS_DIFF_REMITTED = Math.Round((row.EPF_WAGES * 0.0367), 2);
                                        statementResponse.rows.Add(row);
                                    }
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
                MyGlobal.Error("MySqlException--Statement_PF_to_Excel--" + ex.Message);
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------------------
        [HttpPost]
        public ActionResult Statement_PT_to_Excel(string profile, int year, int month, int chkshowall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new StatementPTExcelResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.ret_filename = "Professional_Tax_Statement_" +
                MyGlobal.constArrayMonths[month - 1] + "_" + year;

            string sSQL = "";


            sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_GrossWages,list.m_ProfessionalTax " +
                "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                "where list.m_Profile='" + profile + "' " +
                "and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";

            //if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            if (chkshowall == 0)
            {
                sSQL += "and m_ProfessionalTax > 0 ";
            }
            /*
            //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
            //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
            if (list.Length > 0)
            {
                if (bListSelectedIsApproved)
                    sSQL += "and m_List_PT='" + list + "' ";
                else
                    sSQL += "and (m_List_PT is null or m_List_PT='') ";
            }

            sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
            */
            sSQL += "order by m_Name;";

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
                                    Statement_PT_ExcelRow row = new Statement_PT_ExcelRow();
                                    row.Name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    //row.UAN = MyGlobal.GetPureString(reader, "m_epf_uan");
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "m_GrossWages");
                                    row.m_ProfessionalTax = MyGlobal.GetPureDouble(reader, "m_ProfessionalTax");
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.StaffID, year, month,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                    }
                                    else
                                    {
                                        m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));
                                    }
                                    */
                                    //---------------------------------
                                    /*
                                    row.GROSS_WAGES = MyGlobal.GetPureDouble(reader, "m_GrossWages");

                                    row.EPF_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.EPS_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.EDLI_WAGES = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.NCP_DAYS = 0;
                                    row.REFUND_OF_ADVANCES = 0;

                                    if (MyGlobal.GetPureDouble(reader, "m_EPFContributionRemitted") > 0)
                                    {
                                        row.EPF_CONTRI_REMITTED = MyGlobal.GetPureDouble(reader, "m_EPFContributionRemitted");
                                        row.EPS_CONTRI_REMITTED = Math.Round((row.EPF_WAGES * 0.0833), 2);
                                        row.EPF_EPS_DIFF_REMITTED = Math.Round((row.EPF_WAGES * 0.0367), 2);
                                        statementResponse.rows.Add(row);
                                    }*/
                                    statementResponse.rows.Add(row);
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
                MyGlobal.Error("MySqlException--Statement_PF_to_Excel--" + ex.Message);
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Statement_ESIC_to_Excel(string profile, int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new StatementESICExcelResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.ret_filename = "ESIC_Statement_" +
                MyGlobal.constArrayMonths[month - 1] + "_" + year;
            /*
            Statement_PF_ExcelRow row = new Statement_PF_ExcelRow();
            
            row.m_id = 111;
            row.name = "Eugene";
            row.staffid = "10000";
            statementResponse.rows.Add(row);
            Statement_PF_ExcelRow row1 = new Statement_PF_ExcelRow();
            row1.m_id = 222;
            row1.name = "Anita";
            row1.staffid = "10001";
            statementResponse.rows.Add(row1);
            */
            /*
            string sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
    "list.m_EarnsTot,list.m_DeductsTot," +
    "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
    "list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List," +
    "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_DaysTobePaidTotal," +
    "list.m_BasicPay,list.m_EPFContributionRemitted,list.m_ESIC " +
    "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
    "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
    "on summary.m_StaffID=list.m_StaffID " +
    "and summary.m_Profile=list.m_Profile " +
    "and summary.m_Year=list.m_Year " +
    "and summary.m_Month=list.m_Month " +
    "where list.m_Profile='" + profile + "' " +
    "and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";
    */
            string sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
"list.m_EarnsTot,list.m_DeductsTot," +
"'','',''," +
"list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List," +
"list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_DaysTobePaidTotal," +
"list.m_BasicPay,list.m_EPFContributionRemitted,list.m_ESIC " +
"FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
"where list.m_Profile='" + profile + "' " +
"and list.m_Year='" + year + "' and list.m_Month='" + (month - 1) + "' ";
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
                                    Statement_ESIC_ExcelRow row = new Statement_ESIC_ExcelRow();
                                    row.Name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.StaffID = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.IPNumber = MyGlobal.GetFieldFromTable(profile, "tbl_staffs", "m_ESICNumber", "and m_StaffID='" + row.StaffID + "'");
                                    row.Paydays = MyGlobal.GetPureDouble(reader, "m_DaysTobePaidTotal");
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.StaffID, year, month,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                    }
                                    else
                                    {
                                        m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));
                                    }
                                    */
                                    //---------------------------------
                                    row.ESIC = MyGlobal.GetPureDouble(reader, "m_ESIC");
                                    row.Total_Monthly_Wages = MyGlobal.GetPureDouble(reader, "m_GrossWages");
                                    if (MyGlobal.GetPureDouble(reader, "m_ESIC") > 0)
                                        statementResponse.rows.Add(row);
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
                MyGlobal.Error("MySqlException--Statement_ESIC_to_Excel--" + ex.Message);
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public ActionResult Statement_PFResponse(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
            string dtbank, string chkshowall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            team = team.Trim();
            if (level == null) level = "";
            var statementResponse = new Statement_PFResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.total_count = 0;
            bank = "EPFO Authority";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (level.Equals("8")) // reload button
                    {
                        // update the latest bank details to the tbl_payslips_list where list is not yet created
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list INNER JOIN " + MyGlobal.activeDB + ".tbl_staffs " +
                        "on tbl_payslips_list.m_StaffID = tbl_staffs.m_StaffID " +
                        "Set tbl_payslips_list.m_Bank = tbl_staffs.m_Bank,tbl_payslips_list.m_sb_acc = tbl_staffs.m_AccountNo " +
                        "where (tbl_payslips_list.m_List is null or tbl_payslips_list.m_List = '') and tbl_payslips_list.m_Profile = '" + profile + "'";
                        //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PF=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";
                        //"and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    if (mode.Equals("process"))
                    {
                        statementResponse.result = ProcessRequest_PF(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode, dtbank);
                    }
                    else if (mode.Equals("revert"))
                    {
                        statementResponse.result = RevertRequest_PF(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode);
                    }

                    //--------------------------
                    bool bListSelectedIsApproved = false;
                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_pf_list " +
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
                                    statementResponse.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        statementResponse.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }

                    //--------------------------
                    if (lastaction.Equals("bankchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    else if (lastaction.Equals("teamchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    else if (lastaction.Equals("listclicked") && bank.Length > 0)
                    {
                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PF=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PF=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "' ";
                        if (bListSelectedIsApproved)
                        {
                            sSQL += "and (m_List_PF is not null and m_List_PF='" + list + "') ";
                        }
                        else
                        {
                            sSQL += "and (m_List_PF is null or m_List_PF='') ";
                        }
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        //------------------------------------------
                        // Confirm that bank name and account nos are upto date
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    //---------------------Get total of selected
                    sSQL = "select sum(m_EPFContributionRemitted) as amt,sum(m_BasicPay* 0.0833) as amt1,sum(m_BasicPay* 0.0367) as amt2 " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                     "where m_Profile = '" + profile + "' and m_Selected_PF=true " +
                    "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                                                                                 //"and m_Bank='" + bank + "' ";
                                                                                 //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    statementResponse.dblAmountSelected = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    statementResponse.dblAmountSelected += reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                                    statementResponse.dblAmountSelected += reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (list.m_StaffID like '%" + search + "%' or " +
                        "list.m_Name like '%" + search + "%' or " +
                        "list.m_Email like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
                        sSQL += "and m_EPFContributionRemitted > 0 ";
                    }
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PF='" + list + "' ";
                        else
                            sSQL += "and (m_List_PF is null or m_List_PF='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        statementResponse.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________Get total
                    /*
                    sSQL = "SELECT sum(m_GrossWages) as GrossWages,sum(m_BasicPay) as BasicPay," +
                        "sum(m_EPFContributionRemitted) as EPFContributionRemitted " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                        */
                    sSQL = "SELECT sum(m_GrossWages) as GrossWages,sum(m_BasicPay) as BasicPay," +
                        "sum(m_EPFContributionRemitted) as EPFContributionRemitted " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_EPFContributionRemitted > 0 ";
                    }
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and list.m_List_PF='" + list + "' ";
                        else
                            sSQL += "and (list.m_List_PF is null or list.m_List_PF='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    Statement_PFRow row = new Statement_PFRow();
                                    row.name = "";
                                    row.m_UAN = "";

                                    row.m_BasicPay = MyGlobal.GetPureDouble(reader, "BasicPay");
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "GrossWages");
                                    row.m_EPFWages = MyGlobal.GetPureDouble(reader, "BasicPay");
                                    row.m_EPSWages = MyGlobal.GetPureDouble(reader, "BasicPay");
                                    row.m_ELDIWages = MyGlobal.GetPureDouble(reader, "BasicPay");

                                    row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "EPFContributionRemitted");

                                    row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 2);
                                    row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 2);

                                    statementResponse.rows.Add(row);
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------------
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    /*
                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
                        "list.m_Team,list.m_Selected_PF,list.m_id,list.m_Bank,list.m_List_PF," +
                        "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_BasicPay," +
                        "list.m_EPFContributionRemitted " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                        */
                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "'','',''," +
                        "list.m_Team,list.m_Selected_PF,list.m_id,list.m_Bank,list.m_List_PF," +
                        "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_BasicPay," +
                        "list.m_EPFContributionRemitted " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_EPFContributionRemitted > 0 ";
                    }


                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PF='" + list + "' ";
                        else
                            sSQL += "and (m_List_PF is null or m_List_PF='') ";
                    }

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Statement_PFRow row = new Statement_PFRow();
                                    if (!reader.IsDBNull(0)) row.name = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) row.staffid = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) row.rate = reader.GetDouble(2);
                                    if (!reader.IsDBNull(3)) row.earns = reader.GetDouble(3);
                                    if (!reader.IsDBNull(4)) row.deducts = reader.GetDouble(4);

                                    if (!reader.IsDBNull(5)) row.m_ApprovedBy1 = reader.GetString(5);
                                    if (!reader.IsDBNull(6)) row.m_ApprovedBy2 = reader.GetString(6);
                                    if (!reader.IsDBNull(7)) row.m_ApprovedBy3 = reader.GetString(7);

                                    if (!reader.IsDBNull(8)) row.team = reader.GetString(8);
                                    row.m_Selected = false;
                                    row.m_id = 0;
                                    row.m_Bank = "";
                                    row.m_List = "";
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Selected_PF"))) row.m_Selected = reader.GetBoolean(reader.GetOrdinal("m_Selected_PF"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_Bank"))) row.m_Bank = reader.GetString(reader.GetOrdinal("m_Bank"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_List_PF"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_PF"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_List_PF"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_PF"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_sb_acc"))) row.m_sb_acc = reader.GetString(reader.GetOrdinal("m_sb_acc"));

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_epf_uan"))) row.m_UAN = reader.GetString(reader.GetOrdinal("m_epf_uan"));
                                    //---------------------------------
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_EPFContributionRemitted")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.staffid, iYear, iMonth,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                        row.m_GrossWages = m_GrossWages;
                                        row.m_BasicPay = m_BasicPay;
                                        row.m_EPFContributionRemitted = m_EPFContributionRemitted;
                                    }
                                    else
                                    {
                                        row.m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        row.m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        row.m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));
                                    }
                                    */
                                    //---------------------------------
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "m_GrossWages");
                                    row.m_BasicPay = MyGlobal.GetPureDouble(reader, "m_BasicPay");

                                    row.m_EPFWages = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.m_EPSWages = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.m_ELDIWages = MyGlobal.GetPureDouble(reader, "m_BasicPay");

                                    row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "m_EPFContributionRemitted");
                                    row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 2);
                                    row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 2);
                                    statementResponse.rows.Add(row);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--Statement_PFResponse--" + ex.Message);
            }

            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        /*
        public ActionResult StatementResponse_ProfessionalTax(string profile, string sort, string order,
    string page, string search, string timezone, string team, string bank,
    string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
    string dtbank, string chkshowall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            team = team.Trim();
            if (level == null) level = "";
            var statementResponse = new StatementResponse_ProfessionalTax();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.total_count = 0;
            bank = "ProfessionalTax";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (level.Equals("8")) // reload button
                    {
                        // update the latest bank details to the tbl_payslips_list where list is not yet created
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list INNER JOIN " + MyGlobal.activeDB + ".tbl_staffs " +
                        "on tbl_payslips_list.m_StaffID = tbl_staffs.m_StaffID " +
                        "Set tbl_payslips_list.m_Bank = tbl_staffs.m_Bank,tbl_payslips_list.m_sb_acc = tbl_staffs.m_AccountNo " +
                        "where (tbl_payslips_list.m_List is null or tbl_payslips_list.m_List = '') and tbl_payslips_list.m_Profile = '" + profile + "'";
                        //using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PF=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";
                        //"and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    if (mode.Equals("process"))
                    {
                        statementResponse.result = ProcessRequest_PF(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode, dtbank);
                    }
                    else if (mode.Equals("revert"))
                    {
                        statementResponse.result = RevertRequest_PF(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode);
                    }

                    //--------------------------
                    bool bListSelectedIsApproved = false;
                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_pt_list " +
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
                                    statementResponse.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        statementResponse.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }

                    //--------------------------
                    if (lastaction.Equals("bankchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    else if (lastaction.Equals("teamchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    else if (lastaction.Equals("listclicked") && bank.Length > 0)
                    {
                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PT=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    //---------------------Get total of selected
                    sSQL = "select sum(m_ProfessionalTax) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                     "where m_Profile = '" + profile + "' and m_Selected_PF=true " +
                    "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                    //"and m_Bank='" + bank + "' ";
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    statementResponse.dblAmountSelected = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (list.m_StaffID like '%" + search + "%' or " +
                        "list.m_Name like '%" + search + "%' or " +
                        "list.m_Email like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ProfessionalTax > 0 ";
                    }
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PT='" + list + "' ";
                        else
                            sSQL += "and (m_List_PT is null or m_List_PT='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        statementResponse.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________Get total
                    sSQL = "SELECT sum(m_GrossWages) as GrossWages,sum(m_ProfessionalTax) as ProfessionalTax " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ProfessionalTax > 0 ";
                    }
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and list.m_List_PT='" + list + "' ";
                        else
                            sSQL += "and (list.m_List_PT is null or list.m_List_PT='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    Statement_ProfessionalTaxRow row = new Statement_ProfessionalTaxRow();
                                    row.name = "";
                                    row.m_GrossWages = Math.Round(MyGlobal.GetPureDouble(reader, "GrossWages"), 2);
                                    row.m_ProfessionalTax = Math.Round(MyGlobal.GetPureDouble(reader, "ProfessionalTax"), 2);

                                    statementResponse.rows.Add(row);
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------------
                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";

                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
                        "list.m_Team,list.m_Selected_PT,list.m_id,list.m_Bank,list.m_List_PT," +
                        "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages,list.m_BasicPay," +
                        "list.m_ProfessionalTax " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ProfessionalTax > 0 ";
                    }


                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PT='" + list + "' ";
                        else
                            sSQL += "and (m_List_PT is null or m_List_PT='') ";
                    }

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Statement_ProfessionalTaxRow row = new Statement_ProfessionalTaxRow();
                                    if (!reader.IsDBNull(0)) row.name = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) row.staffid = reader.GetString(1);
                                    row.m_id = 0;

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Selected_PT"))) row.m_Selected = reader.GetBoolean(reader.GetOrdinal("m_Selected_PT"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_Bank"))) row.m_Bank = reader.GetString(reader.GetOrdinal("m_Bank"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_List_PF"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_PF"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_List_PT"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_PT"));


                                    //---------------------------------
                                    //---------------------------------
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "m_GrossWages");


                                    row.m_ProfessionalTax = MyGlobal.GetPureDouble(reader, "m_ProfessionalTax");

                                    statementResponse.rows.Add(row);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--StatementResponse_PT--" + ex.Message);
            }

            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        */
        public ActionResult Statement_ESICResponse(string profile, string sort, string order,
    string page, string search, string timezone, string team, string bank,
    string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
    string dtbank, string chkshowall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            bool bListSelectedIsApproved = false;
            team = team.Trim();
            if (level == null) level = "";
            var statementResponse = new Statement_ESICResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.total_count = 0;
            bank = "ESIC Authority";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("process"))
                    {
                        statementResponse.result = ProcessRequest_ESIC(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode, dtbank);
                    }
                    else if (mode.Equals("revert"))
                    {
                        statementResponse.result = RevertRequest_ESIC(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode);
                    }
                    //--------------------------

                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_esic_list " +
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
                                    statementResponse.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        statementResponse.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }

                    //--------------------------

                    if (lastaction.Equals("listclicked") && bank.Length > 0)
                    {
                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_ESIC=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";

                        if (bListSelectedIsApproved)
                        {
                            sSQL += "and (m_List is not null and m_List='" + list + "') ";
                        }
                        else
                        {
                            sSQL += "and (m_List is null or m_List='') ";
                        }
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        //------------------------------------------
                        // Confirm that bank name and account nos are upto date

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }

                    //________________________________________________________________
                    String sSearchKey = " (list.m_StaffID like '%" + search + "%' or " +
                        "list.m_Name like '%" + search + "%' or " +
                        "list.m_Email like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";

                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ESIC > 0 ";
                    }

                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_ESIC='" + list + "' ";
                        else
                            sSQL += "and (m_List_ESIC is null or m_List_ESIC='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        statementResponse.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________

                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";
                    /*
                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
                        "list.m_Team,list.m_Selected_ESIC,list.m_id,list.m_Bank,list.m_List_ESIC," +
                        "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages," +
                        "list.m_DaysTobePaidTotal,list.m_BasicPay,list.m_ESIC " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                        */
                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
"list.m_EarnsTot,list.m_DeductsTot," +
"'','',''," +
"list.m_Team,list.m_Selected_ESIC,list.m_id,list.m_Bank,list.m_List_ESIC," +
"list.m_sb_acc,list.m_epf_uan,list.m_GrossWages," +
"list.m_DaysTobePaidTotal,list.m_BasicPay,list.m_ESIC " +
"FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
"where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
"and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";
                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ESIC > 0 ";
                    }

                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_ESIC='" + list + "' ";
                        else
                            sSQL += "and (m_List_ESIC is null or m_List_ESIC='') ";
                    }

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Statement_ESICRow row = new Statement_ESICRow();
                                    row.name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    row.m_Paydays = MyGlobal.GetPureDouble(reader, "m_DaysTobePaidTotal");
                                    row.m_ESICNumber = MyGlobal.GetFieldFromTable(profile, "tbl_staffs", "m_ESICNumber", "and m_StaffID='" + row.staffid + "'");
                                    //---------------------------------
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.staffid, iYear, iMonth,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                        row.m_GrossWages = m_GrossWages;
                                        row.m_BasicPay = m_BasicPay;
                                        //row.m_EPFContributionRemitted = m_EPFContributionRemitted;
                                    }
                                    else
                                    {
                                        row.m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        row.m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        //row.m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));

                                    }
                                    */
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Selected_ESIC"))) row.m_Selected = reader.GetBoolean(reader.GetOrdinal("m_Selected_ESIC"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_List_ESIC"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_ESIC"));
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "m_GrossWages");
                                    row.m_BasicPay = MyGlobal.GetPureDouble(reader, "m_BasicPay");
                                    row.m_ESIC = MyGlobal.GetPureDouble(reader, "m_ESIC");
                                    //---------------------------------
                                    statementResponse.rows.Add(row);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--Statement_ESICResponse--" + ex.Message);
            }

            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Statement_PTResponse(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
            string dtbank, string chkshowall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            bool bListSelectedIsApproved = false;
            team = team.Trim();
            if (level == null) level = "";
            var statementResponse = new Statement_PTResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.total_count = 0;
            bank = "Professional Tax Authority";
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (mode.Equals("process"))
                    {
                        statementResponse.result = ProcessRequest_PT(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode, dtbank);
                    }
                    else if (mode.Equals("revert"))
                    {
                        statementResponse.result = RevertRequest_PT(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode);
                    }
                    //--------------------------

                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_pt_list " +
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
                                    statementResponse.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        statementResponse.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }

                    //--------------------------

                    if (lastaction.Equals("listclicked") && bank.Length > 0)
                    {
                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PT=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";

                        if (bListSelectedIsApproved)
                        {
                            sSQL += "and (m_List is not null and m_List='" + list + "') ";
                        }
                        else
                        {
                            sSQL += "and (m_List is null or m_List='') ";
                        }
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        //------------------------------------------
                        // Confirm that bank name and account nos are upto date

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }

                    //________________________________________________________________
                    String sSearchKey = " (list.m_StaffID like '%" + search + "%' or " +
                        "list.m_Name like '%" + search + "%' or " +
                        "list.m_Email like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";

                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ProfessionalTax > 0 ";
                    }

                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PT='" + list + "' ";
                        else
                            sSQL += "and (m_List_PT is null or m_List_PT='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        statementResponse.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________

                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";

                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
                        "list.m_Team,list.m_Selected_PT,list.m_id,list.m_Bank,list.m_List_PT," +
                        "list.m_sb_acc,list.m_epf_uan,list.m_GrossWages," +
                        "list.m_DaysTobePaidTotal,list.m_BasicPay,list.m_ProfessionalTax " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";

                    if (!chkshowall.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sSQL += "and m_ProfessionalTax > 0 ";
                    }

                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List_PT='" + list + "' ";
                        else
                            sSQL += "and (m_List_PT is null or m_List_PT='') ";
                    }

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Statement_PTRow row = new Statement_PTRow();
                                    row.name = MyGlobal.GetPureString(reader, "m_Name");
                                    row.staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    //row.m_Paydays = MyGlobal.GetPureDouble(reader, "m_DaysTobePaidTotal");
                                    //row.m_ESICNumber = MyGlobal.GetFieldFromTable(profile, "tbl_staffs", "m_ESICNumber", "and m_StaffID='" + row.staffid + "'");
                                    //---------------------------------
                                    /*
                                    double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0;
                                    if (reader.IsDBNull(reader.GetOrdinal("m_GrossWages")) ||
                                        reader.IsDBNull(reader.GetOrdinal("m_BasicPay")))
                                    {
                                        // This may not be required when these details come from tbl_payslips_list table
                                        GetDataFromPayslipForPF(
                                            profile, row.staffid, iYear, iMonth,
                                            out m_GrossWages, out m_BasicPay,
                                            out m_EPFContributionRemitted, out m_ESIC
                                            );
                                        row.m_GrossWages = m_GrossWages;
                                        row.m_BasicPay = m_BasicPay;
                                        //row.m_EPFContributionRemitted = m_EPFContributionRemitted;
                                    }
                                    else
                                    {
                                        row.m_GrossWages = reader.GetDouble(reader.GetOrdinal("m_GrossWages"));
                                        row.m_BasicPay = reader.GetDouble(reader.GetOrdinal("m_BasicPay"));
                                        //row.m_EPFContributionRemitted = reader.GetDouble(reader.GetOrdinal("m_EPFContributionRemitted"));

                                    }
                                    */
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Selected_PT"))) row.m_Selected = reader.GetBoolean(reader.GetOrdinal("m_Selected_PT"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_List_PT"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List_PT"));
                                    row.m_GrossWages = MyGlobal.GetPureDouble(reader, "m_GrossWages");
                                    row.m_ProfessionalTax = MyGlobal.GetPureDouble(reader, "m_ProfessionalTax");
                                    //---------------------------------
                                    statementResponse.rows.Add(row);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
                MyGlobal.Error("MySqlException--Statement_PTResponse--" + ex.Message);
            }

            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        //
        [HttpPost]
        public ActionResult OneTime_(string profile, string year, string month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementResponse = new PostResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            int iMonth = MyGlobal.GetInt16(month);




            string sUpdate = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    iMonth = 5; // May=5 
                    year = "2019";
                    string sSQL = "select m_StaffID from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                       "where m_Profile='" + profile + "' " +
                       "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) {
                                        string staffid = reader.GetString(0);
                                        double m_GrossWages = 0, m_BasicPay = 0, m_EPFContributionRemitted = 0, m_ESIC = 0, m_ProfessionalTax = 0;

                                        GetDataFromPayslipForPF(
                                                profile, staffid, MyGlobal.GetInt16(year), iMonth,
                                                out m_GrossWages, out m_BasicPay,
                                                out m_EPFContributionRemitted,
                                                out m_ESIC,
                                                out m_ProfessionalTax
                                                );

                                        sUpdate += "update " + MyGlobal.activeDB + ".tbl_payslips_list Set " +
                                       "m_GrossWages='" + m_GrossWages + "',m_BasicPay='" + m_BasicPay + "'," +
                                       "m_EPFContributionRemitted='" + m_EPFContributionRemitted + "'," +
                                       "m_ESIC='" + m_ESIC + "' " +
                                       "where m_Profile='" + profile + "' and m_StaffID='" + staffid + "' " +
                                       "and m_Year='" + year + "' and m_Month='" + (iMonth - 1) + "';";
                                    }
                                }
                            }
                        }
                    }
                    //-------------------------------
                    if (sUpdate.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sUpdate, con))
                        {
                            int iRet = mySqlCommand.ExecuteNonQuery();
                            statementResponse.result = iRet + " rows affected";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "MySqlException >>" + ex.Message;
            }
            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        private bool GetDataFromPayslipForPF(string profile, string staffID,
            int iYear, int iMonth, out double m_GrossWages, out double m_BasicPay,
            out double m_EPFContributionRemitted, out double m_ESIC, out double m_ProfessionalTax)
        {
            m_GrossWages = 0;
            m_BasicPay = 0;
            m_EPFContributionRemitted = 0;
            m_ESIC = 0;
            m_ProfessionalTax = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select m_Ledger,m_Amount from " + MyGlobal.activeDB + ".tbl_payslips " +
                        "where m_Profile='" + profile + "' and m_StaffID='" + staffID + "' " +
                        "and m_Year='" + iYear + "' and m_Month='" + (iMonth - 1) + "' " +
                        "and ((m_Ledger='Basic' and m_Type='earn') or " +
                        "(m_Ledger='PF' and m_Type='deduct') or " +
                        "(m_Ledger='Professional Tax' and m_Type='deduct') or " +
                        "(m_Ledger='ESIC' and  m_Type='deduct')) and m_Amount>0 " +
                        "group by m_Ledger;";
                    //and m_Type='cr' 

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (MyGlobal.GetPureString(reader, "m_Ledger").Equals("Basic", StringComparison.CurrentCultureIgnoreCase))
                                        m_BasicPay = MyGlobal.GetPureDouble(reader, "m_Amount");
                                    if (MyGlobal.GetPureString(reader, "m_Ledger").Equals("PF", StringComparison.CurrentCultureIgnoreCase))
                                        m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "m_Amount");
                                    if (MyGlobal.GetPureString(reader, "m_Ledger").Equals("ESIC", StringComparison.CurrentCultureIgnoreCase))
                                        m_ESIC = MyGlobal.GetPureDouble(reader, "m_Amount");
                                    if (MyGlobal.GetPureString(reader, "m_Ledger").Equals("Professional Tax", StringComparison.CurrentCultureIgnoreCase))
                                        m_ProfessionalTax = MyGlobal.GetPureDouble(reader, "m_Amount");
                                }
                            }
                        }
                    }
                    con.Close();
                }

                LoadPayslip loadPayslip = this.GetPayslipFrom_Attendance_And_PayscaleMaster(
                    profile, "", staffID, iYear.ToString(), (iMonth).ToString(), "1");
                m_GrossWages = Math.Round(loadPayslip.m_GrossSalary, 2);
            }
            catch (MySqlException ex)
            {

            }
            return true;
        }

        //[HttpPost]
        public ActionResult StatementResponse(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string level, string lastaction, string list, string mode,
            string dtbank)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            team = team.Trim();
            if (level == null) level = "";
            var statementResponse = new StatementResponse();
            statementResponse.status = false;
            statementResponse.result = "";
            statementResponse.total_count = 0;
            int iYear = MyGlobal.GetInt16(dtYear);
            int iMonth = MyGlobal.GetInt16(dtMonth);
            try
            {
                string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    if (level.Equals("8")) // reload button
                    {
                        // update the latest bank details to the tbl_payslips_list where list is not yet created
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list INNER JOIN " + MyGlobal.activeDB + ".tbl_staffs " +
                        "on tbl_payslips_list.m_StaffID = tbl_staffs.m_StaffID " +
                        "Set tbl_payslips_list.m_Bank = tbl_staffs.m_Bank,tbl_payslips_list.m_sb_acc = tbl_staffs.m_AccountNo " +
                        "where (tbl_payslips_list.m_List is null or tbl_payslips_list.m_List = '') and tbl_payslips_list.m_Profile = '" + profile + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";
                        //"and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();

                    }
                    if (mode.Equals("process"))
                    {
                        statementResponse.result = ProcessRequest(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode, dtbank);
                    }
                    else if (mode.Equals("revert"))
                    {
                        statementResponse.result = RevertRequest(profile, sort, order,
            page, search, timezone, team, bank,
            dtYear, dtMonth, lastaction, list, mode);
                    }
                    //------------------------------
                    statementResponse.sarTeams.Add("");
                    sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_misc_teams " +
                        "where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        statementResponse.sarTeams.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    statementResponse.sarBanks.Add("");
                    sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_misc_staffbanks " +
                        "where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                        statementResponse.sarBanks.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    //--------------------------
                    bool bListSelectedIsApproved = false;
                    sSQL = "select m_List,m_BankDate from " + MyGlobal.activeDB + ".tbl_bank_list " +
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
                                    statementResponse.list += reader.GetString(0) + ",";
                                    if (list.Equals(reader.GetString(0)))
                                    {
                                        if (!bListSelectedIsApproved) bListSelectedIsApproved = true;
                                        //statementResponse.dtBank = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                    }
                                }
                            }
                        }
                    }

                    //--------------------------
                    if (lastaction.Equals("bankchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    else if (lastaction.Equals("teamchanged") && bank.Length > 0)
                    {

                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    else if (lastaction.Equals("listclicked") && bank.Length > 0)
                    {
                        //clear all in the bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=false where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        /*
                        // select only team and bank
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=true where m_Profile='" + profile + "' " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' ";

                        if (bListSelectedIsApproved)
                        {
                            sSQL += "and (m_List is not null and m_List='" + list + "') ";
                        }
                        else
                        {
                            sSQL += "and (m_List is null or m_List='') ";
                        }
                        if (team.Length > 0) sSQL += " and m_Team='" + team + "' ";
                        //------------------------------------------
                        // Confirm that bank name and account nos are upto date

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        */
                    }
                    //---------------------Get total of selected
                    sSQL = "select (sum(m_EarnsTot)-sum(m_DeductsTot)) as amt from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                     "where m_Profile = '" + profile + "' and m_Selected=true " +
                    "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                    "and m_Bank='" + bank + "' ";
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    statementResponse.dblAmountSelected = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________
                    String sSearchKey = " (list.m_StaffID like '%" + search + "%' or " +
                        "list.m_Name like '%" + search + "%' or " +
                        "list.m_Email like '%" + search + "%') ";

                    sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                    "where " + sSearchKey + " and m_Profile='" + profile + "' " +
                    "and m_Year='" + iYear + "' and m_Month='" + iMonth + "' ";
                    if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List='" + list + "' ";
                        else
                            sSQL += "and (m_List is null or m_List='') ";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader["cnt"] != null)
                                        statementResponse.total_count = reader.GetInt16(0);
                                }
                            }
                        }
                    }
                    //________________________________________________________________

                    int iPageSize = 15;
                    int iPage = MyGlobal.GetInt16(page);
                    if (iPage < 1) iPage = 1;
                    int PAGE = iPageSize * (iPage - 1);
                    if (sort.Equals("undefined") || sort.Length == 0) sort = "m_Name";
                    if (order.Equals("undefined") || order.Length == 0) order = "asc";

                    sSQL = "SELECT list.m_Name,list.m_StaffID,list.m_CrTot," +
                        "list.m_EarnsTot,list.m_DeductsTot," +
                        "summary.m_ApprovedBy1,summary.m_ApprovedBy2,summary.m_ApprovedBy3," +
                        "list.m_Team,list.m_Selected,list.m_id,list.m_Bank,list.m_List,list.m_sb_acc," +
                        "summary.m_ApprovedBy4,staffs.m_Email " +
                        "FROM " + MyGlobal.activeDB + ".tbl_payslips_list list " +
                        "left join " +
                        "(select * from " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_Profile='" + profile + "' and m_Year='" + iYear + "' and m_Month='" + iMonth + "' " +
                        "group by m_StaffID" +
                        ") summary " +
                        "on summary.m_StaffID=list.m_StaffID " +
                        "and summary.m_Profile=list.m_Profile " +
                        "and summary.m_Year=list.m_Year " +
                        "and summary.m_Month=list.m_Month " +
                        "left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_Profile=list.m_Profile and staffs.m_StaffID=list.m_StaffID " +
                        "where " + sSearchKey + " and list.m_Profile='" + profile + "' " +
                        "and list.m_Year='" + iYear + "' and list.m_Month='" + iMonth + "' ";

                    if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    if (bank.Length > 0) sSQL += "and list.m_Bank='" + bank + "' ";
                    if (list.Length > 0)
                    {
                        if (bListSelectedIsApproved)
                            sSQL += "and m_List='" + list + "' ";
                        else
                            sSQL += "and (m_List is null or m_List='') ";
                    }

                    sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";
                    
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    StatementRow row = new StatementRow();
                                    if (!reader.IsDBNull(0)) row.name = reader.GetString(0);
                                    if (!reader.IsDBNull(1)) row.staffid = reader.GetString(1);
                                    if (!reader.IsDBNull(2)) row.rate = reader.GetDouble(2);
                                    if (!reader.IsDBNull(3)) row.earns = reader.GetDouble(3);
                                    if (!reader.IsDBNull(4)) row.deducts = reader.GetDouble(4);

                                    if (!reader.IsDBNull(5)) row.m_ApprovedBy1 = reader.GetString(5);
                                    if (!reader.IsDBNull(6)) row.m_ApprovedBy2 = reader.GetString(6);
                                    if (!reader.IsDBNull(7)) row.m_ApprovedBy3 = reader.GetString(7);
                                    row.m_ApprovedBy4 = MyGlobal.GetPureString(reader, "m_ApprovedBy4");
                                    if (!reader.IsDBNull(8)) row.team = reader.GetString(8);
                                    row.m_Selected = false;
                                    row.m_id = 0;
                                    row.m_Bank = "";
                                    row.m_List = "";
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Selected"))) row.m_Selected = reader.GetBoolean(reader.GetOrdinal("m_Selected"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Bank"))) row.m_Bank = reader.GetString(reader.GetOrdinal("m_Bank"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_List"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("m_List"))) row.m_List = reader.GetString(reader.GetOrdinal("m_List"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_sb_acc"))) row.m_sb_acc = reader.GetString(reader.GetOrdinal("m_sb_acc"));

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Email"))) row.m_Email = reader.GetString(reader.GetOrdinal("m_Email"));

                                    if (bListSelectedIsApproved) statementResponse.ApprovedBankReturn = row.m_Bank;
                                    statementResponse.rows.Add(row);
                                }
                                statementResponse.status = true;

                            }
                            else
                            {
                                statementResponse.result = "Sorry!!! No Records";
                            }
                        }
                    }
                    //________________________________________________________________

                    con.Close();
                }
            }
            catch (MySqlException ex)
            {
                statementResponse.result = "Error-" + ex.Message;
            }

            return Json(statementResponse, JsonRequestBehavior.AllowGet);
        }
        //___________________________________________________________________________
        [HttpPost]
        public ActionResult StatementClkCheck(string profile,
            string m_id, bool state, string mode, string team, string bank,
            int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("all"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected=" + state + " where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                        if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                        if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                        sSQL += "and m_StaffID in (select m_StaffID from " + MyGlobal.activeDB + ".tbl_attendance_summary where m_Profile = '" + profile + "' and m_Year='" + year + "' and m_Month='" + month + "'  and m_ApprovedBy4 is not null)";

                    }
                    else
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_Selected=" + state + " where m_Profile='" + profile + "' and m_id='" + m_id + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    //---------------------------
                    sSQL = "select (sum(m_EarnsTot)-sum(m_DeductsTot)) as amt from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                         "where m_Profile = '" + profile + "' and m_Selected=true " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                    if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.dblParam1 = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("StatementClkCheck->" + ex.Message);
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StatementClkCheck_PF(string profile,
            string m_id, bool state, string mode, string team, string bank,
            int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("all"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PF=" + state + " where m_Profile='" + profile + "' " +
                        "and (m_List_PF is null or m_List_PF='') " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                        //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                        //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    }
                    else
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_Selected_PF=" + state + " where m_Profile='" + profile + "' " +
                            "and m_id='" + m_id + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    //---------------------------
                    sSQL = "select sum(m_EPFContributionRemitted) as amt,sum(m_BasicPay* 0.0833) as amt1,sum(m_BasicPay* 0.0367) as amt2 " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                         "where m_Profile = '" + profile + "' and m_Selected_PF=true " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                    //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.dblParam1 = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    postResponse.dblParam1 += reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                                    postResponse.dblParam1 += reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                    //row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "EPFContributionRemitted");
                                    // row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 0);
                                    //row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 0);
                                }
                            }
                        }
                    }
                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StatementClkCheck_ESIC(string profile,
    string m_id, bool state, string mode, string team, string bank,
    int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("all"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_ESIC=" + state + " where m_Profile='" + profile + "' " +
                        "and (m_List_ESIC is null or m_List_ESIC='') " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                        //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                        //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    }
                    else
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_Selected_ESIC=" + state + " where m_Profile='" + profile + "' " +
                            "and m_id='" + m_id + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    //---------------------------
                    sSQL = "select sum(m_ESIC) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                         "where m_Profile = '" + profile + "' and m_Selected_ESIC=true " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.dblParam1 = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    //row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "EPFContributionRemitted");
                                    // row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 0);
                                    //row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 0);
                                }
                            }
                        }
                    }
                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StatementClkCheck_Retention(string profile,
    string m_id, bool state, string mode, string team, string bank,
    int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("all"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_Retention=" + state + " where m_Profile='" + profile + "' " +
                        "and (m_List_Retention is null or m_List_Retention='') " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";
                        //if (bank.Length > 0) sSQL += "and m_Bank='" + bank + "' ";
                        //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";

                    }
                    else
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_Selected_Retention=" + state + " where m_Profile='" + profile + "' " +
                            "and m_id='" + m_id + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    //---------------------------
                    sSQL = "select sum(m_ESIC) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                         "where m_Profile = '" + profile + "' and m_Selected_Retention=true " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.dblParam1 = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    //row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "EPFContributionRemitted");
                                    // row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 0);
                                    //row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 0);
                                }
                            }
                        }
                    }
                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StatementClkCheck_Retention_Approval(string profile,
    string m_id, bool state, string mode,
    string staff)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            if (mode == null || mode.Length == 0)
            {
                postResponse.result = "Invalid request";
                return Json(postResponse, JsonRequestBehavior.AllowGet);
            }

            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                MySqlTransaction myTrans = con.BeginTransaction();
                try
                {
                    string sSQL = "", sSQLInsert = ""; ;
                    if (mode.Equals("hr"))
                    {
                        if (state)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
                                "Set m_ApprovalHR_by='" + staff + "',m_ApprovalHR_date=now() where m_Profile='" + profile + "' " +
                                "and m_id='" + m_id + "'";
                        }
                        else
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
    "Set m_ApprovalHR_by=null,m_ApprovalHR_date=null where m_Profile='" + profile + "' " +
    "and m_id='" + m_id + "'";
                        }
                    }
                    else if (mode.Equals("admin"))
                    {
                        if (state)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
                                "Set m_FreezedBy='" + staff + "',m_FreezedOn=now() where m_Profile='" + profile + "' " +
                                "and m_id='" + m_id + "'";
                            //----------------------Add retention bonus as additional ledger
                            String sSQLLocal = "select m_StaffID,m_Year,m_Month,m_Amount,m_ApprovalHR_date,m_ApprovalAccounts_date," +
                                "m_ActualWorkingDays,m_FreezedOn " +
                                "from " + MyGlobal.activeDB + ".tbl_retention_list " +
                                "where m_Profile='" + profile + "' " +
                                "and m_id='" + m_id + "'";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLLocal, con, myTrans))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        if (reader.Read())
                                        {
                                            if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2) &&
                                                !reader.IsDBNull(3))
                                            {
                                                int iYear = reader.GetInt16(1);
                                                int iMonth = reader.GetInt16(2);
                                                bool m_ApprovalHR = reader.IsDBNull(4);
                                                bool m_ApprovalAccounts = reader.IsDBNull(5);
                                                double m_ActualWorkingDays = reader.IsDBNull(6) ? 0 : reader.GetDouble(6); ;
                                                bool bAddRetentionBonus = false;
                                                if (iMonth == 2)
                                                {
                                                    bAddRetentionBonus = m_ActualWorkingDays >= 23;
                                                }
                                                else
                                                {
                                                    bAddRetentionBonus = m_ActualWorkingDays >= 25;
                                                }
                                                if (bAddRetentionBonus)
                                                {
                                                    if (m_ApprovalHR || m_ApprovalAccounts)
                                                    {
                                                        //myTrans.Rollback();
                                                        postResponse.result = "Credit eligible, but, not approved";
                                                        //return Json(postResponse, JsonRequestBehavior.AllowGet);
                                                        bAddRetentionBonus = false;
                                                    }
                                                }

                                                if (bAddRetentionBonus)
                                                {
                                                    String sCr = "Retention Bonus(" + MyGlobal.constArrayMonths[iMonth] + ")";
                                                    String sDr = "Retention Bonus(Released on " +
                                                        DateTime.Now.ToString("dd-MM-yyyy") +
                                                        ")";


                                                    if (iMonth == 11)
                                                    {
                                                        iYear++;
                                                        iMonth = 0;
                                                    }
                                                    else
                                                    {
                                                        iMonth++;
                                                    }


                                                    sSQLInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                                                       "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_Security) values " +
                                                       "('" + profile + "'," +
                                                       "'" + reader.GetString(0) + "'," +
                                                       "'" + iYear + "'," +
                                                       "'" + iMonth + "'," +
                                                       "'" + sCr + "'," +
                                                       "'" + reader.GetDouble(3) + "'," +
                                                       "'cr'," +
                                                       "1" +
                                                       ");";
                                                    sSQLInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
        "(m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type,m_Security) values " +
        "('" + profile + "'," +
        "'" + reader.GetString(0) + "'," +
        "'" + iYear + "'," +
        "'" + iMonth + "'," +
        "'" + sDr + "'," +
        "'" + reader.GetDouble(3) + "'," +
        "'dr'," +
        "1" +
        ");";
                                                }
                                                else
                                                {
                                                    postResponse.result = "Credit not given.closed";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
    "Set m_FreezedBy=null,m_FreezedOn=null where m_Profile='" + profile + "' " +
    "and m_id='" + m_id + "'";
                        }
                    }
                    else
                    {
                        if (state)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
        "Set m_ApprovalAccounts_by='" + staff + "',m_ApprovalAccounts_date=now() where m_Profile='" + profile + "' " +
        "and m_id='" + m_id + "'";
                        }
                        else
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_retention_list " +
        "Set m_ApprovalAccounts_by=null,m_ApprovalAccounts_date=null where m_Profile='" + profile + "' " +
        "and m_id='" + m_id + "'";
                        }

                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con, myTrans)) mySqlCommand.ExecuteNonQuery();
                    if (sSQLInsert.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLInsert, con, myTrans)) mySqlCommand.ExecuteNonQuery();
                    }
                    myTrans.Commit();
                    postResponse.result = "Credit updated";
                    postResponse.status = true;
                }

                catch (MySqlException ex)
                {
                    myTrans.Rollback();
                    postResponse.result = "Error-" + ex.Message;
                }
                catch (Exception ex)
                {
                    myTrans.Rollback();
                    postResponse.result = "Error-" + ex.Message;
                }
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult StatementClkCheck_PT(string profile,
            string m_id, bool state, string mode, string team, string bank,
            int year, int month)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    if (mode.Equals("all"))
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_Selected_PT=" + state + " where m_Profile='" + profile + "' " +
                        "and (m_List_PT is null or m_List_PT='') " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";

                    }
                    else
                    {
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_Selected_PT=" + state + " where m_Profile='" + profile + "' " +
                            "and m_id='" + m_id + "'";
                    }

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                    //---------------------------
                    sSQL = "select sum(m_ProfessionalTax) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                         "where m_Profile = '" + profile + "' and m_Selected_PT=true " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' ";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.dblParam1 = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    //row.m_EPFContributionRemitted = MyGlobal.GetPureDouble(reader, "EPFContributionRemitted");
                                    // row.m_EPSContributionRemitted = Math.Round((row.m_BasicPay * 0.0833), 0);
                                    //row.m_EPFEPSDifferenceRemitted = Math.Round((row.m_BasicPay * 0.0367), 0);
                                }
                            }
                        }
                    }
                    postResponse.status = true;
                }
            }
            catch (MySqlException ex)
            {
                postResponse.result = "Error-" + ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        private string ProcessRequest(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string lastaction, string list, string mode,
            string dtbank)
        {
            string sRet = "";
            double dblAmount = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select (sum(m_EarnsTot)-sum(m_DeductsTot)) as amt from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    "where m_Profile = '" + profile + "' and m_Selected=true " +
                    "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                    "and m_Bank='" + bank + "' ";
                    //if (team.Length > 0) sSQL += "and m_Team='" + team + "' ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    dblAmount = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_bank_list " +
                            "(m_Profile,m_Year,m_Month,m_Bank,m_List,m_AmountTotal,m_BankDate) " +
                            "values " +
                            "('" + profile + "','" + dtYear + "','" + dtMonth + "'," +
                            "'" + bank + "','" + list + "','" + dblAmount + "','" + dtbank + "')";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //----------------Create Bank ledger, if not exists
                        myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                        "select * FROM (select '" + profile + "', '" + bank + "','Bank') AS tmp " +
                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Name = '" + bank + "') LIMIT 1;";
                        myCommand.ExecuteNonQuery();
                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        sSQL = "select m_StaffID,m_EarnsTot,m_DeductsTot from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' and m_Selected=true";
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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear;
                                                double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + staffid + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "Bank Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_BankList='" + m_id_LastInserted + "',m_List='" + list + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' and m_Selected=true";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "Bank Statement Created";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        //-------------------------------
        private string ProcessRequest_PF(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string lastaction, string list, string mode,
            string dtbank)
        {
            string sRet = "";
            bank = "EPFO Authority";
            double dblAmount = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select sum(m_EPFContributionRemitted) as amt,sum(m_BasicPay* 0.0833) as amt1,sum(m_BasicPay* 0.0367) as amt2 " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    "where m_Profile = '" + profile + "' and m_Selected_PF=true " +
                    "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "'";// +
                    //"and m_Bank='" + bank + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    dblAmount = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                    dblAmount += reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                                    dblAmount += reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                }
                            }
                        }
                    }
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_pf_list " +
                            "(m_Profile,m_Year,m_Month,m_Bank,m_List,m_AmountTotal,m_BankDate) " +
                            "values " +
                            "('" + profile + "','" + dtYear + "','" + dtMonth + "'," +
                            "'" + bank + "','" + list + "','" + dblAmount + "','" + dtbank + "')";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //----------------Create Bank ledger, if not exists
                        myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                        "select * FROM (select '" + profile + "', '" + bank + "','Bank') AS tmp " +
                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Name = '" + bank + "') LIMIT 1;";
                        myCommand.ExecuteNonQuery();
                        //--------------------------------------------
                        string sInsert = "";

                        sSQL = "select m_StaffID,m_EPFContributionRemitted from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PF is null or m_List_PF='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_PF=true";
                        //and m_Bank='" + bank + "' 
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear;
                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2), 2);
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                                //staffid
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "PF" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "PF Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_PFList='" + m_id_LastInserted + "',m_List_PF='" + list + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PF is null or m_List_PF='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_PF=true";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "PF Statement Created";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        //-------------------------------
        private string ProcessRequest_ESIC(string profile, string sort, string order,
            string page, string search, string timezone, string team, string bank,
            string dtYear, string dtMonth, string lastaction, string list, string mode,
            string dtbank)
        {
            string sRet = "";
            bank = "ESIC Authority";
            double dblAmount = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select sum(m_ESIC) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile = '" + profile + "' and m_Selected_ESIC=true " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "'";// +
                                                                                    //"and m_Bank='" + bank + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    dblAmount = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_esic_list " +
                            "(m_Profile,m_Year,m_Month,m_Bank,m_List,m_AmountTotal,m_BankDate) " +
                            "values " +
                            "('" + profile + "','" + dtYear + "','" + dtMonth + "'," +
                            "'" + bank + "','" + list + "','" + dblAmount + "','" + dtbank + "')";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //----------------Create Bank ledger, if not exists
                        myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                        "select * FROM (select '" + profile + "', '" + bank + "','Bank') AS tmp " +
                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Name = '" + bank + "') LIMIT 1;";
                        myCommand.ExecuteNonQuery();
                        //--------------------------------------------
                        string sInsert = "";

                        sSQL = "select m_StaffID,m_ESIC from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_ESIC is null or m_List_ESIC='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_ESIC=true";
                        //and m_Bank='" + bank + "' 
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear;
                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2), 2);
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                                //staffid
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "ESIC" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "ESIC Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_ESICList='" + m_id_LastInserted + "',m_List_ESIC='" + list + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_ESIC is null or m_List_ESIC='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_ESIC=true";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "ESIC Statement Created";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        private string ProcessRequest_PT(string profile, string sort, string order,
    string page, string search, string timezone, string team, string bank,
    string dtYear, string dtMonth, string lastaction, string list, string mode,
    string dtbank)
        {
            string sRet = "";
            bank = "Professional Tax Authority";
            double dblAmount = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select sum(m_ProfessionalTax) as amt " +
                        "from  " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile = '" + profile + "' and m_Selected_PT=true " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "'";// +
                                                                                    //"and m_Bank='" + bank + "' ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    dblAmount = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                                }
                            }
                        }
                    }
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_pt_list " +
                            "(m_Profile,m_Year,m_Month,m_Bank,m_List,m_AmountTotal,m_BankDate) " +
                            "values " +
                            "('" + profile + "','" + dtYear + "','" + dtMonth + "'," +
                            "'" + bank + "','" + list + "','" + dblAmount + "','" + dtbank + "')";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //----------------Create Bank ledger, if not exists
                        myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                        "select * FROM (select '" + profile + "', '" + bank + "','Bank') AS tmp " +
                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Name = '" + bank + "') LIMIT 1;";
                        myCommand.ExecuteNonQuery();
                        //--------------------------------------------
                        string sInsert = "";

                        sSQL = "select m_StaffID,m_ProfessionalTax from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PT is null or m_List_PT='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_PT=true";
                        //and m_Bank='" + bank + "' 
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear;
                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2), 2);
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                                //staffid
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "Professional Tax" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "Professional Tax Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_PTList='" + m_id_LastInserted + "',m_List_PT='" + list + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PT is null or m_List_PT='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Selected_PT=true";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "Professional Tax Statement Created";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Professional Tax is Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Professional Tax Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("Professional Tax-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Professional Tax-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        //-------------------------------
        private string RevertRequest(string profile, string sort, string order,
    string page, string search, string timezone, string team, string bank,
    string dtYear, string dtMonth, string lastaction, string list, string mode)
        {
            string sRet = "";
            double dblAmount = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_bank_list " +
                            "where m_Profile='" + profile + "' and m_Year='" + dtYear + "' " +
                            "and m_Month='" + dtMonth + "' " +
                            "and m_Bank='" + bank + "' and m_List='" + list + "';";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;

                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        sSQL = "select m_StaffID,m_EarnsTot,m_DeductsTot from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is not null and m_List='" + list + "') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "'";

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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear + ". REVERSED";
                                                double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + staffid + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "Bank Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";


                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_BankList=null,m_List=null " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is not null and m_List!='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_Bank='" + bank + "' and m_List='" + list + "'";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //----------------------------------------
                        myTrans.Commit();
                        sRet = "Bank Statement Reverted";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                            MyGlobal.Error("RevertRequest-MySqlException-" + sRet);
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                            MyGlobal.Error("RevertRequest-MySqlException-" + sRet);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("RevertRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("RevertRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        private string RevertRequest_PF(string profile, string sort, string order,
string page, string search, string timezone, string team, string bank,
string dtYear, string dtMonth, string lastaction, string list, string mode)
        {
            string sRet = "";
            double dblAmount = 0;
            bank = "EPFO Authority";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_pf_list " +
                            "where m_Profile='" + profile + "' and m_Year='" + dtYear + "' " +
                            "and m_Month='" + dtMonth + "' " +
                            "and m_List='" + list + "';";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        sSQL = "select m_StaffID,m_EPFContributionRemitted from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PF is not null and m_List_PF='" + list + "') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "'";

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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear + ". REVERSED";

                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2));
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "PF" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "PF Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_PFList=null,m_List_PF=null " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PF is not null and m_List_PF!='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_List_PF='" + list + "'";
                        //and m_Bank = '" + bank + "'

                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //----------------------------------------
                        myTrans.Commit();
                        sRet = "PF Statement Reverted";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                            MyGlobal.Error("RevertRequest-Exception-Rolled back-" + sRet);
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                            MyGlobal.Error("RevertRequest-MySqlException-Rolled back-" + sRet);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("RevertRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("RevertRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        private string RevertRequest_ESIC(string profile, string sort, string order,
string page, string search, string timezone, string team, string bank,
string dtYear, string dtMonth, string lastaction, string list, string mode)
        {
            string sRet = "";
            double dblAmount = 0;
            bank = "ESIC Authority";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_esic_list " +
                            "where m_Profile='" + profile + "' and m_Year='" + dtYear + "' " +
                            "and m_Month='" + dtMonth + "' " +
                            "and m_List='" + list + "';";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        sSQL = "select m_StaffID,m_ESIC from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_ESIC is not null and m_List_ESIC='" + list + "') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "'";

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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear + ". REVERSED";

                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2));
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "PF" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "ESIC Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_ESICList=null,m_List_ESIC=null " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_ESIC is not null and m_List_ESIC!='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_List_ESIC='" + list + "'";
                        //and m_Bank = '" + bank + "'

                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //----------------------------------------
                        myTrans.Commit();
                        sRet = "ESIC Statement Reverted";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                            MyGlobal.Error("ESIC RevertRequest-Exception-Rolled back-" + sRet);
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                            MyGlobal.Error("ESIC RevertRequest-MySqlException-Rolled back-" + sRet);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ESIC RevertRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ESIC RevertRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        private string RevertRequest_PT(string profile, string sort, string order,
string page, string search, string timezone, string team, string bank,
string dtYear, string dtMonth, string lastaction, string list, string mode)
        {
            string sRet = "";
            double dblAmount = 0;
            bank = "Professional Tax Authority";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    //-----------------Critical operations
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        long m_id_LastInserted = -1;
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_pt_list " +
                            "where m_Profile='" + profile + "' and m_Year='" + dtYear + "' " +
                            "and m_Month='" + dtMonth + "' " +
                            "and m_List='" + list + "';";
                        //and m_Bank='" + bank + "' 
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        sSQL = "select m_StaffID,m_ProfessionalTax from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PT is not null and m_List_PT='" + list + "') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' ";// +
                        //"and m_Bank='" + bank + "'";

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
                                            string staffid = reader.GetString(0);
                                            if (staffid.Length > 0)
                                            {
                                                //_______Update account ledgers
                                                string head = list + " of " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(dtMonth)] + " " + dtYear + ". REVERSED";

                                                double dblAmt = Math.Round(reader.GetDouble(1), 2);// - Math.Round(reader.GetDouble(2));
                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + bank + "',Now()," +
                                                "'" + dblAmt + "',0," +
                                                "'" + head + "'," +
                                                "'" + "Paid by the list. Account of " + staffid + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                                sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                                                "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                                                "('" + profile + "','" + "Professional Tax" + "',Now()," +
                                                "0,'" + dblAmt + "'," +
                                                "'" + bank + "'," +
                                                "'" + "Professional Tax Deposit by " + head + "'," +
                                                "'" + dtYear + "','" + dtMonth + "','" + staffid + "','" + iVchNo + "');";

                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_id_PTList=null,m_List_PT=null " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List_PT is not null and m_List_PT!='') " +
                        "and m_Year='" + dtYear + "' and m_Month='" + dtMonth + "' " +
                        "and m_List_PT='" + list + "'";
                        //and m_Bank = '" + bank + "'

                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //----------------------------------------
                        myTrans.Commit();
                        sRet = "Professional Tax Statement Reverted";
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Professional Tax Rolled back [" + e.Message + "]";
                            MyGlobal.Error("Professional Tax RevertRequest-Exception-Rolled back-" + sRet);
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Professional Tax Failed " + ex.Message + " [" + e.Message + "]";
                            MyGlobal.Error("Professional Tax RevertRequest-MySqlException-Rolled back-" + sRet);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("Professional Tax RevertRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Professional Tax RevertRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return sRet;
        }
        //--------------------------------------------------------------
        [HttpPost]
        public ActionResult StatementToPDF(string profile, string bank,
        string year, string month, string list)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var statementToPDF = new StatementToPDF();
            statementToPDF.status = false;
            statementToPDF.result = "";
            statementToPDF.cols.Add("Sl.No");
            statementToPDF.cols.Add("Staff ID");
            statementToPDF.cols.Add("Name");
            statementToPDF.cols.Add("Acc. No");
            statementToPDF.cols.Add("Amount");

            //statementToPDF.rows.Add(statementToPDF.cols);
            //statementToPDF.rows.Add(statementToPDF.cols);
            //statementToPDF.rows.Add(statementToPDF.cols);
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "";
                    statementToPDF.txtBankName = bank;
                    statementToPDF.txtListNo = list;
                    sSQL = "select m_BankDate from " + MyGlobal.activeDB + ".tbl_bank_list " +
                    "where m_Profile='" + profile + "' " +
                    "and (m_List is not null and m_List='" + list + "') " +
                    "and m_Year='" + year + "' and m_Month='" + month + "' " +
                    "and m_Bank='" + bank + "'";

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
                                        statementToPDF.txtBankDate = reader.GetString(0);
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                    "where m_Profile='" + profile + "' " +
                    "and (m_List is not null and m_List='" + list + "') " +
                    "and m_Year='" + year + "' and m_Month='" + month + "' " +
                    "and m_Bank='" + bank + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int i = 1;
                                while (reader.Read())
                                {
                                    List<string> item = new List<string>();
                                    item.Add(i.ToString());
                                    i++;
                                    item.Add(reader.IsDBNull(reader.GetOrdinal("m_StaffID")) ? "" : reader.GetString(reader.GetOrdinal("m_StaffID")));
                                    item.Add(reader.IsDBNull(reader.GetOrdinal("m_Name")) ? "" : reader.GetString(reader.GetOrdinal("m_Name")));
                                    item.Add(reader.IsDBNull(reader.GetOrdinal("m_sb_acc")) ? "" : reader.GetString(reader.GetOrdinal("m_sb_acc")));
                                    double dblEars = reader.IsDBNull(reader.GetOrdinal("m_EarnsTot")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_EarnsTot"));
                                    double dblDeducts = reader.IsDBNull(reader.GetOrdinal("m_DeductsTot")) ? 0 : reader.GetDouble(reader.GetOrdinal("m_DeductsTot"));

                                    item.Add(String.Format("{0:n}", Math.Round((dblEars - dblDeducts), 2)));

                                    statementToPDF.rows.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("RevertRequest-MySqlException-" + ex.Message);

            }
            catch (Exception ex)
            {
                MyGlobal.Error("RevertRequest-Exception-" + ex.Message);

            }

            return Json(statementToPDF, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult OnBonusUpdateVoucher(string profile, string staffid,
            string staffname,
            /*string payto, string payfrom,*/ string notes,
            string year, string month, string amount, string mode, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            if (mode.Equals("process"))
            {
                string sResult = "";
                postResponse.status = ProcessBonusUpdate_Voucher(profile, staffid, staffname,
                    /*payto, payfrom,*/ notes, year, month, amount, mode, out sResult);
                postResponse.result = sResult; // This has VchNo
            }
            else if (mode.Equals("revert"))
            {
                /*
                string sResult = "";
                postResponse.status = RevertRequest_Voucher(profile, staffid, staffname,
                    payto, payfrom, notes, year, month, amount, mode, out sResult, vchno);
                //if (postResponse.status) postResponse.result = sResult; // This has VchNo
                */
                /*
                postResponse.result = RevertRequest_Voucher(profile, sort, order,
                    page, search, timezone, team, bank,
                    dtYear, dtMonth, lastaction, list, mode);
                    */
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult OnBonusReleaseVoucher(string profile, string staffid,
            string staffname,
            /*string payto, string payfrom,*/ string notes,
            string year, string month, string amount, string mode, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            if (mode.Equals("process"))
            {
                string sResult = "";
                postResponse.status = ProcessBonusRelease_Voucher(profile, staffid, staffname,
                    /*payto, payfrom,*/ notes, year, month, amount, mode, out sResult);
                postResponse.result = sResult; // This has VchNo
                //----------------------------------------------
                MailDoc mailDoc = new MailDoc();
                mailDoc.m_To = "support@SharewareDreams.com"; //clientResponse.email;
                mailDoc.Domain = MyGlobal.GetDomain();
                mailDoc.m_Subject = "Health Watch";
                mailDoc.m_Body = LoadBonusReport_HTML(profile, staffid, year);
                Thread newThread = new Thread(ChatHub.SendEmail_MeterBox);
                newThread.Start(mailDoc);
                //-----------------------------------------------
            }
            else if (mode.Equals("revert"))
            {
                /*
                string sResult = "";
                postResponse.status = RevertRequest_Voucher(profile, staffid, staffname,
                    payto, payfrom, notes, year, month, amount, mode, out sResult, vchno);
                //if (postResponse.status) postResponse.result = sResult; // This has VchNo
                */
                /*
                postResponse.result = RevertRequest_Voucher(profile, sort, order,
                    page, search, timezone, team, bank,
                    dtYear, dtMonth, lastaction, list, mode);
                    */
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult LoadBonusReport(string profile, string staffid,
            string year)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = LoadBonusReport_HTML(profile, staffid, year);
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult LoadBonusReportReleased(string profile, string vchno)
        {
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = LoadBonusReportReleased_HTML(profile, vchno);
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        private string LoadBonusReportReleased_HTML(string profile, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";
            double tot = 0;
            string html = "<table style='border: 1px solid #888;width:100%;'>" +
                            "<tr style='border-bottom:1px solid #888;'><td colspan=2><b></b></td></tr>" +
                        "<tr style='border-bottom:1px solid #888;'><td><b>Year</b></td><td><b>Month</b></td><td><b>Bonus</b></td></tr>";
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                string sql = "SELECT m_Year,m_Month,ROUND(sum(m_Cr)-sum(m_Dr)) " +
"FROM meterbox.tbl_accounts t " +
"where m_Profile = 'support@SharewareDreams.com' and m_Ledger = 'Bonus Accrued' " +
"and m_ReleaseVoucherarker = " + vchno + " " +
"group by m_StaffID,(m_Year * 12 + m_Month)";
                
                using (MySqlCommand mySqlCommand = new MySqlCommand(sql, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    html +=
                                        "<tr>" +
                                        "<td style='padding-left:5px;'>" + (reader.GetInt16(0)) + "</td>" +
                                        "<td>" + (constArrayMonths[reader.GetInt16(1)]) + "</td>" +
                                        "<td style='text-align:right;padding-right:5px;'>" + reader.GetDouble(2).ToString("N2") + "</td>" +
                                        "</tr>";
                                    tot += reader.GetDouble(2);
                                }
                            }
                        }
                    }
                }
            }
            html += "<tr style='border-top:1px solid #aaa;'><td></td><td style='text-align:right;padding-right:10px;'>Total </td><td style='text-align:right;padding-right:5px;font-weight:bold;'><b>" + tot.ToString("N2") + "</b></td></tr>";
            html += "</table>";
            postResponse.result = html;
            return postResponse.result;
        }
        private string LoadBonusReport_HTML(string profile, string staffid,
            string year)
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
                    string sSQL =
"SELECT accounts.m_StaffID,staffs.m_FName,staffs.m_AccountNo,staffs.m_Mrs,staffs.m_Status,m_Month,sum(m_Dr),sum(m_Cr),sum(m_Cr-m_Dr) as tot,m_ReleaseVoucherarker FROM " + MyGlobal.activeDB + ".tbl_accounts accounts " +
"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = accounts.m_StaffID and staffs.m_Profile = accounts.m_Profile " +
"where m_Ledger = 'Bonus Accrued' and accounts.m_Profile='" + profile + "' and accounts.m_StaffID='" + staffid + "' " +
"and (m_Year*12+m_Month)>=(" + year + "*12+9) and (m_Year*12+m_Month)<=(" + year + "*12+9+12) " +
"group by m_Month ";
//"order by accounts.m_StaffID";

                    Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string staffidx = MyGlobal.GetPureString(reader, "m_StaffID");
                                    int month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    if (!dic.Keys.Contains(staffidx))
                                    {
                                        string name = MyGlobal.GetPureString(reader, "m_FName");
                                        int iMrs = MyGlobal.GetPureInt16(reader, "m_Mrs");
                                        if (iMrs == 0) name = "Mr." + name;
                                        if (iMrs == 1) name = "Ms." + name;
                                        if (iMrs == 10 || iMrs == 11) name = "Dr." + name;
                                        string status = MyGlobal.GetPureString(reader, "m_Status");
                                        string accountNo = MyGlobal.GetPureString(reader, "m_AccountNo");

                                        dic.Add(staffidx, new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                                            "0", name, status, accountNo,
                                             "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                                        });
                                    }
                                    if (dic.Keys.Contains(staffidx))
                                    {
                                        dic[staffidx][month] = MyGlobal.GetPureDouble(reader, "tot").ToString("n2");
                                        dic[staffid][month + 16] = MyGlobal.GetPureString(reader, "m_ReleaseVoucherarker");
                                    }
                                }
                            }
                        }
                    }
                    var html = "Unable to get details";
                    foreach (KeyValuePair<string, string[]> entry in dic)
                    {
                        // do something with entry.Value or entry.Key
                        HRMonthVsViewRow item = new HRMonthVsViewRow();
                        item.StaffID = entry.Key;
                        item.Jan = entry.Value[0];
                        item.Feb = entry.Value[1];
                        item.Mar = entry.Value[2];
                        item.Apr = entry.Value[3];
                        item.May = entry.Value[4];
                        item.Jun = entry.Value[5];
                        item.Jly = entry.Value[6];
                        item.Aug = entry.Value[7];
                        item.Sep = entry.Value[8];
                        item.Oct = entry.Value[9];
                        item.Nov = entry.Value[10];
                        item.Dec = entry.Value[11];

                        item.JanS = entry.Value[0 + 16];
                        item.FebS = entry.Value[1 + 16];
                        item.MarS = entry.Value[2 + 16];
                        item.AprS = entry.Value[3 + 16];
                        item.MayS = entry.Value[4 + 16];
                        item.JunS = entry.Value[5 + 16];
                        item.JlyS = entry.Value[6 + 16];
                        item.AugS = entry.Value[7 + 16];
                        item.SepS = entry.Value[8 + 16];
                        item.OctS = entry.Value[9 + 16];
                        item.NovS = entry.Value[10 + 16];
                        item.DecS = entry.Value[11 + 16];

                        int i = 0;
                        double tot = 0;
                        foreach (string arr in entry.Value)
                        {
                            if (i < 12) tot += MyGlobal.GetDouble(arr);
                            i++;
                        }

                        item.Total = tot.ToString("n2");// entry.Value[12];
                        item.Name = entry.Value[13];
                        item.Status = entry.Value[14];
                        //item.SBAccount = entry.Value[15];
                        int iYear = 0;
                        int.TryParse(year, out iYear);
                        //--------------------Create HTML
                        html = "<table style='border: 1px solid #888;width:100%;'>" +
                            "<tr style='border-bottom:1px solid #888;'><td colspan=2><b>" + item.Name + "</b></td></tr>" +
                        "<tr style='border-bottom:1px solid #888;'><td><b>Month</b></td><td><b>Bonus</b></td><td title='Paid voucher number, if released'><b>Voucher</b></td></tr>";
                        html += "<tr><td>" + (iYear) + " Oct</td><td>" + item.Oct + "</td><td>" + (item.OctS.Equals("0") ? "" : item.OctS) + "</td></tr>";
                        html += "<tr><td>" + (iYear) + " Nov</td><td>" + item.Nov + "</td><td>" + (item.NovS.Equals("0") ? "" : item.NovS) + "</td></tr>";
                        html += "<tr><td>" + (iYear) + " Dec</td><td>" + item.Dec + "</td><td>" + (item.DecS.Equals("0") ? "" : item.DecS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Jan</td><td>" + item.Jan + "</td><td>" + (item.JanS.Equals("0") ? "" : item.JanS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Feb</td><td>" + item.Feb + "</td><td>" + (item.FebS.Equals("0") ? "" : item.FebS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Mar</td><td>" + item.Mar + "</td><td>" + (item.MarS.Equals("0") ? "" : item.MarS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Apr</td><td>" + item.Apr + "</td><td>" + (item.AprS.Equals("0") ? "" : item.AprS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " May</td><td>" + item.May + "</td><td>" + (item.MayS.Equals("0") ? "" : item.MayS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Jun</td><td>" + item.Jun + "</td><td>" + (item.JunS.Equals("0") ? "" : item.JunS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Jly</td><td>" + item.Jly + "</td><td>" + (item.JlyS.Equals("0") ? "" : item.JlyS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Aug</td><td>" + item.Aug + "</td><td>" + (item.AugS.Equals("0") ? "" : item.AugS) + "</td></tr>";
                        html += "<tr><td>" + (iYear + 1) + " Sep</td><td>" + item.Sep + "</td><td>" + (item.SepS.Equals("0") ? "" : item.SepS) + "</td></tr>";

                        html += "</table>";
                    }
                    postResponse.result = html;
                }
            }
            catch (Exception x)
            {
                postResponse.result = "<html><body>" + x.Message + "</body></html>";
            }
            return postResponse.result;
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult OnPaymentVoucher(string profile, string staffid,
            string staffname,
            string payto, string payfrom, string notes,
            string year, string month, string amount, string mode, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new PostResponse();
            postResponse.status = false;
            postResponse.result = "";

            if (mode.Equals("process"))
            {
                string sResult = "";
                postResponse.status = ProcessRequest_Voucher(profile, staffid, staffname,
                    payto, payfrom, notes, year, month, amount, mode, out sResult);
                postResponse.result = sResult; // This has VchNo
            }
            else if (mode.Equals("revert"))
            {
                string sResult = "";
                postResponse.status = RevertRequest_Voucher(profile, staffid, staffname,
                    payto, payfrom, notes, year, month, amount, mode, out sResult, vchno);
                //if (postResponse.status) postResponse.result = sResult; // This has VchNo
                /*
                postResponse.result = RevertRequest_Voucher(profile, sort, order,
                    page, search, timezone, team, bank,
                    dtYear, dtMonth, lastaction, list, mode);
                    */
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        private bool ProcessRequest_Voucher(string profile, string staffid, string staffname,
            string payto, string payfrom, string notes,
            string year, string month, string amount, string mode, out string sResult)
        {
            string sRet = "";
            sResult = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------Critical operations
                    string sSQL = "";
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        //----------------Check ledger validity
                        bool bLedgerExists = false;
                        sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Profile='" + profile + "' and m_Name='" + payfrom + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bLedgerExists = reader.HasRows;
                            }
                        }
                        if (!bLedgerExists)
                        {
                            sResult = "Payment Source Ledger is Unknown";
                            return false;
                        }
                        //----------------
                        /*
                        long m_id_LastInserted = -1;
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_bank_list " +
                            "(m_Profile,m_Year,m_Month,m_Bank,m_List,m_AmountTotal,m_BankDate) " +
                            "values " +
                            "('" + profile + "','" + year + "','" + month + "'," +
                            "'" + bank + "','" + list + "','" + dblAmount + "','" + dtbank + "')";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        m_id_LastInserted = myCommand.LastInsertedId;
                        //----------------Create Bank ledger, if not exists
                        myCommand.CommandText =
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                        "select * FROM (select '" + profile + "', '" + bank + "','Bank') AS tmp " +
                        "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Name = '" + bank + "') LIMIT 1;";
                        myCommand.ExecuteNonQuery();
                        */
                        //--------------------------------------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        int iYear = MyGlobal.GetInt16(year);
                        string Year = (iYear == 0) ? "null" : "'" + year + "'";
                        string Month = MyGlobal.GetInt16(year) == 0 ? "null" : "'" + month + "'";
                        //_______Update account ledgers
                        string head = "Payment to " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(month)] + " " + year;
                        if (iYear == 0) head = "To Ledger " + payto + " from " + payfrom;
                        //double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                        sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                        "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                        "('" + profile + "','" + payfrom + "',Now()," +
                        "'" + amount + "',0," +
                        "'" + head + "'," +
                        "'" + "Paid by voucher No " + iVchNo + ". Account of " + payto + "'," +
                        "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";

                        sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                        "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                        "('" + profile + "','" + payto + "',Now()," +
                        "0,'" + amount + "'," +
                        "'" + head + "'," +
                        "'" + "Voucher No " + iVchNo + " by  " + payfrom + "'," +
                        "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";

                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        //m_id_BankList='" + m_id_LastInserted + "',
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_List='" + iVchNo + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year=" + Year + " and m_Month=" + Month + " " +
                        "and m_StaffID='" + staffid + "'";
                        // and m_Selected=true

                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //-----------------------
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accounts_notes " +
                            "(m_Profile,m_VchNo,m_Notes) values " +
                            "('" + profile + "','" + iVchNo + "','" + notes + "');";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "Payment Voucher Created";
                        sResult = "Payment Voucher " + iVchNo.ToString() + " Created";
                        return true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return false;
        }
        //---------------------------------------------------------
        //---------------------------------------------------------
        private bool ProcessBonusUpdate_Voucher(string profile, string staffid, string staffname,
            string notes,
            string year, string month, string amount, string mode, out string sResult)
        {
            string sRet = "";
            sResult = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------Critical operations
                    string sSQL = "";
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        //----------------Check ledger validity
                        bool bLedgerExists = false;
                        sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Profile='" + profile + "' and m_Name='" + staffid + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bLedgerExists = reader.HasRows;
                            }
                        }
                        if (!bLedgerExists)
                        {
                            sResult = "Staff ID " + staffid + " doesnot have a Ledger";
                            return false;
                        }
                        if(year==null || month == null)
                        {
                            sResult = "Invalid request";
                            return false;
                        }
                        //----------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        int iYear = MyGlobal.GetInt16(year);
                        string Year = (iYear == 0) ? "null" : "'" + year + "'";
                        string Month = MyGlobal.GetInt16(month) == 0 ? "null" : "'" + month + "'";
                        int iMonth = MyGlobal.GetInt16(month);
                        //DateTime tme = DateTime.Now;
                        //Year = tme.Year.ToString();
                        //Month = (tme.Month - 1).ToString();
                        if (iMonth != -1) {
                            //_______Update account ledgers
                            string head = "Payment to " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(month)] + " " + year;
                            //if (iYear == 0) head = "To Ledger " + payto + " from " + payfrom;
                            //double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                            head = "Annual Bonus";
                            //----------------------------------------------------------------Credit Bonus
                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + staffid + "',Now()," +
                            "'" + amount + "',0," +
                            "'" + "Annual Bonus" + "'," +
                            "'" + "Paid by voucher No " + iVchNo + ". Special Bonus. Account of " + staffid + "'," +
                            "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";

                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + "Annual Bonus" + "',Now()," +
                            "0,'" + amount + "'," +
                            "'" + staffid + "'," +
                            "'" + "Paid by voucher No " + iVchNo + ". Special Bonus. Account of " + staffid + "'," +
                            "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";
                            //----------------------------------------------------------------Credit Bonus END
                            //----------------------------------------------------------------Credit Bonus
                            // Accrued
                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + staffid + "',Now()," +
                            "'" + amount + "',0," +
                            "'" + "Bonus Accrued" + "'," +
                            "'" + "Accrued by voucher No " + iVchNo + ". Special Bonus. Account of " + staffid + "'," +
                            "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";

                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + "Bonus Accrued" + "',Now()," +
                            "0,'" + amount + "'," +
                            "'" + staffid + "'," +
                            "'" + "Accrued by voucher No " + iVchNo + ". Special Bonus. Account of " + staffid + "'," +
                            "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";
                            //----------------------------------------------------------------Credit Bonus END

                            sResult = "Please contact accounts for permission to do bonus credit.";
                            return false;
                        }
                        else
                        {   // -1. Bonus credit to accounts and payslip
                            string ledName = "Annual Bonus Credit";
                            string ledType = "cr";
                            string ledAmount = amount;
                            //-----Add in additional ledger
                            sInsert = "INSERT INTO " + MyGlobal.activeDB + ".tbl_payslips_addledgers (m_Profile,m_StaffID,m_Year,m_Month,m_Ledger,m_Amount,m_Type) " +
"select * FROM (select '" + profile + "','" + staffid + "'," + (iYear + 1) + ",'" + 8 + "','" + ledName + "','" + ledAmount + "','" + ledType + "') AS tmp " +
"where NOT EXISTS(SELECT m_Ledger FROM " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
"where m_Ledger = '" + ledName + "' and m_StaffID='" + staffid + "' and m_Year=" + Year + " and m_Month='" + 8 + "') LIMIT 1;";

                        }
                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        /*
                        //m_id_BankList='" + m_id_LastInserted + "',
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_List='" + iVchNo + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year=" + Year + " and m_Month=" + Month + " " +
                        "and m_StaffID='" + staffid + "'";
                        // and m_Selected=true
                        
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        */
                        //-----------------------
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accounts_notes " +
                            "(m_Profile,m_VchNo,m_Notes) values " +
                            "('" + profile + "','" + iVchNo + "','" + notes + "');";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "Special Bonus Updated";
                        //sResult = "Special Bonus Voucher " + iVchNo.ToString() + " Created";
                        sResult = "reload";
                        return true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return false;
        }
        //---------------------------------------------------------
        private bool ProcessBonusRelease_Voucher(string profile, string staffid, string staffname,
            string notes,
            string year, string month, string amount, string mode, out string sResult)
        {
            string sRet = "";
            sResult = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------Critical operations
                    string sSQL = "";
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        //----------------Check ledger validity
                        bool bLedgerExists = false;
                        sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                        "where m_Profile='" + profile + "' and m_Name='" + staffid + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bLedgerExists = reader.HasRows;
                            }
                        }
                        if (!bLedgerExists)
                        {
                            sResult = "Staff ID " + staffid + " doesnot have a Ledger";
                            return false;
                        }
                        //----------------
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        int iYear = MyGlobal.GetInt16(year);
                        string Year = (iYear == 0) ? "null" : "'" + year + "'";
                        string Month = MyGlobal.GetInt16(year) == 0 ? "null" : "'" + month + "'";
                        DateTime tme = DateTime.Now;
                        Year = tme.Year.ToString();
                        Month = (tme.Month - 1).ToString();
                        //_______Update account ledgers
                        string head = "Payment to " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(month)] + " " + year;
                        //if (iYear == 0) head = "To Ledger " + payto + " from " + payfrom;
                        //double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                        //head = "Annual Bonus";
                        //----------------------------------------------------------------Release Bonus
                        sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                        "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                        "('" + profile + "','" + staffid + "',Now()," +
                        "'" + amount + "',0," +
                        "'" + "Bonus Accrued" + "'," +
                        "'" + "Bonus Accrued, released by voucher No " + iVchNo + ". Account of " + staffid + "'," +
                        "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";

                        sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                        "(m_Profile,m_Ledger,m_Time,m_Cr,m_Dr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                        "('" + profile + "','" + "Bonus Accrued" + "',Now()," +
                        "0,'" + amount + "'," +
                        "'" + staffid + "'," +
                        "'" + "Bonus Accrued, released by voucher No " + iVchNo + ". Account of " + staffid + "'," +
                        "" + Year + "," + Month + ",'" + staffid + "','" + iVchNo + "');";
                        //----------------------------------------------------------------Release Bonus END

                        if (sInsert.Length > 0)
                        {
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        //----------------Mark the list---------
                        /*
                        //m_id_BankList='" + m_id_LastInserted + "',
                        sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "Set m_List='" + iVchNo + "' " +
                        "where m_Profile='" + profile + "' " +
                        "and (m_List is null or m_List='') " +
                        "and m_Year=" + Year + " and m_Month=" + Month + " " +
                        "and m_StaffID='" + staffid + "'";
                        // and m_Selected=true
                        
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        */
                        //-----------------------
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accounts_notes " +
                            "(m_Profile,m_VchNo,m_Notes) values " +
                            "('" + profile + "','" + iVchNo + "','" + notes + "');";
                        myCommand.CommandText = sSQL;
                        myCommand.ExecuteNonQuery();
                        //_______Update account ledgers END
                        myTrans.Commit();
                        sRet = "Bonus Accrued Released";
                        //sResult = "Bonus Accrued Released by Voucher " + iVchNo.ToString();
                        sResult = "reload";
                        return true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return false;
        }
        //---------------------------------------------------------
        private bool RevertRequest_Voucher(string profile, string staffid, string staffname,
            string payto, string payfrom, string notes,
            string year, string month, string amount, string mode, out string sResult, string vchno)
        {
            string sRet = "";
            sResult = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------Critical operations
                    string sSQL = "";
                    MySqlTransaction myTrans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = myTrans;
                    try
                    {
                        string sInsert = "";
                        Int32 iVchNo = MyGlobal.GetNewVchNo(con, profile);
                        Int32 iVchNoOld = MyGlobal.GetInt32(vchno);
                        //------------------------------------------------------
                        bool bThisVoucherExists = false;
                        sSQL = "select m_id from " + MyGlobal.activeDB + ".tbl_payslips_list " +
                        "where m_Profile='" + profile + "' " +
                        "and m_List='" + iVchNoOld + "' " +
                        "and m_Year='" + year + "' and m_Month='" + month + "' " +
                        "and m_StaffID='" + staffid + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                bThisVoucherExists = reader.HasRows;
                            }
                        }
                        if (bThisVoucherExists)
                        {
                            //_______Update account ledgers
                            string head = "Payment to " + staffid + " for " + constArrayMonths[MyGlobal.GetInt16(month)] + " " + year + ". REVERSED";
                            //double dblAmt = Math.Round(reader.GetDouble(1) - reader.GetDouble(2), 2);
                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + payfrom + "',Now()," +
                            "'" + amount + "',0," +
                            "'" + head + "'," +
                            "'" + "REVERSED Voucher No " + iVchNoOld + " by " + iVchNo + ". Account of " + staffid + ".'," +
                            "'" + year + "','" + month + "','" + staffid + "','" + iVchNo + "');";

                            sInsert += "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts " +
                            "(m_Profile,m_Ledger,m_Time,m_Dr,m_Cr,m_Head,m_Description,m_Year,m_Month,m_StaffID,m_VchNo) values " +
                            "('" + profile + "','" + payto + "',Now()," +
                            "0,'" + amount + "'," +
                            "'" + head + "'," +
                            "'" + "REVERSED Voucher No " + iVchNoOld + " by " + iVchNo + " through  " + payfrom + ".'," +
                            "'" + year + "','" + month + "','" + staffid + "','" + iVchNo + "');";

                            if (sInsert.Length > 0)
                            {
                                myCommand.CommandText = sInsert;
                                myCommand.ExecuteNonQuery();
                            }
                            //----------------Mark the list---------
                            //m_id_BankList='" + m_id_LastInserted + "',
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_payslips_list " +
                            "Set m_List=null " +
                            "where m_Profile='" + profile + "' " +
                            "and m_List='" + iVchNoOld + "' " +
                            "and m_Year='" + year + "' and m_Month='" + month + "' " +
                            "and m_StaffID='" + staffid + "'";
                            // and m_Selected=true

                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            //-----------------------
                            sSQL = "insert into " + MyGlobal.activeDB + ".tbl_accounts_notes " +
                                "(m_Profile,m_VchNo,m_Notes) values " +
                                "('" + profile + "','" + iVchNo + "','" + notes + ". REVERSED');";
                            myCommand.CommandText = sSQL;
                            myCommand.ExecuteNonQuery();
                            //_______Update account ledgers END
                            myTrans.Commit();
                            sRet = "Payment Voucher REVERSED";
                        }
                        else
                        {
                            sResult = "Specific Voucher does not exists";
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            myTrans.Rollback();
                            sRet = "Rolled back [" + e.Message + "]";
                        }
                        catch (MySqlException ex)
                        {
                            sRet = "Failed " + ex.Message + " [" + e.Message + "]";
                        }
                    }
                    finally
                    {
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                sRet = "Failed " + ex.Message;
            }
            return false;
        }
        //---------------------------------------------------------
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult LoadVoucher(string profile, string vchno)
        {
            if(MyGlobal.GetInt16(vchno)==0)
            {
                return LoadVoucher_Open(profile, vchno);
            }
            else
            {
                return LoadVoucher_WithVchNo(profile, vchno);
            }
        }
        [HttpPost]
        public ActionResult LoadVoucher_WithVchNo(string profile, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new VoucherDetails();
            postResponse.status = false;
            postResponse.result = "";
            postResponse.vchno = vchno;
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select * from " + MyGlobal.activeDB + ".tbl_accounts " +
                        "where m_Profile='" + profile + "' and m_VchNo='" + vchno + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                int iRows = 0;
                                while (reader.Read())
                                {
                                    if (reader.GetDouble("m_Dr") > 0)
                                    {
                                        postResponse.payto = reader.GetString("m_Ledger");
                                    }
                                    else if (reader.GetDouble("m_Cr") > 0)
                                    {
                                        postResponse.payfrom = reader.GetString("m_Ledger");
                                        postResponse.amount = reader.GetDouble("m_Cr");
                                    }
                                    iRows++;

                                }
                                if (iRows == 2)
                                {
                                    postResponse.status = true;
                                }
                                else if (iRows > 2)
                                {
                                    postResponse.result = "Multiple entry voucher";
                                }
                                else
                                {
                                    postResponse.result = "Voucher has no records";
                                }
                            }
                        }
                    }
                    sSQL = "select m_Notes from " + MyGlobal.activeDB + ".tbl_accounts_notes " +
    "where m_Profile='" + profile + "' and m_VchNo='" + vchno + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    postResponse.notes = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                }
                            }
                        }
                        postResponse.paytoname = GetStaffName_(profile, postResponse.payto);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                postResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
        [HttpPost]
        public ActionResult LoadVoucher_Open(string profile, string vchno)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var postResponse = new VoucherDetails();
            postResponse.status = false;
            postResponse.result = "";
            
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
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
                                        postResponse.sarLedgers.Add(reader.GetString(0));
                                    postResponse.status = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("ProcessRequest-MySqlException-" + ex.Message);
                postResponse.result = ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("ProcessRequest-Exception-" + ex.Message);
                postResponse.result = ex.Message;
            }
            return Json(postResponse, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------
    }
}
