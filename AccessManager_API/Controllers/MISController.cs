using Dapper;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Http;
using MyHub.Hubs;
using MyHub.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class MISController : Controller
    {
        /*
        public ActionResult GetHRBonusAccruedActivities(
            string profile, string sort, string order, string page, 
            string search, string timezone)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new BonusAccruedResponse();
            response.status = false;
            response.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL =
"SELECT accounts.m_StaffID,staffs.m_FName,staffs.m_Mrs,staffs.m_Status,m_Month,sum(m_Dr),sum(m_Cr),sum(m_Dr-m_Cr) as tot FROM " + MyGlobal.activeDB + ".tbl_accounts accounts " +
"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = accounts.m_StaffID and staffs.m_Profile = accounts.m_Profile " +
"where m_Head = 'Bonus Accrued' and accounts.m_Profile='" + profile + "' " +
"group by accounts.m_StaffID,m_Month " +
"order by accounts.m_StaffID";
                    Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    int month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    if (!dic.Keys.Contains(staffid))
                                    {
                                        string name = MyGlobal.GetPureString(reader, "m_FName");
                                        int iMrs = MyGlobal.GetPureInt16(reader, "m_Mrs");
                                        if (iMrs == 0) name = "Mr." + name;
                                        if (iMrs == 1) name = "Ms." + name;
                                        if (iMrs == 10|| iMrs == 11) name = "Dr." + name;
                                        string status = MyGlobal.GetPureString(reader, "m_Status");
                                        dic.Add(staffid, new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", name,status });
                                    }
                                    if (dic.Keys.Contains(staffid))
                                    {
                                        dic[staffid][month]= MyGlobal.GetPureDouble(reader, "tot").ToString("n2");
                                    }
                                }
                                response.status = true;
                            }
                            else
                            {
                                response.result = "Sorry!!! No Data";
                            }
                        }
                    }
                    foreach (KeyValuePair<string, string[]> entry in dic)
                    {
                        // do something with entry.Value or entry.Key
                        int i = 0;
                        double tot = 0;
                        foreach(string arr in entry.Value)
                        {
                            if (i < 12) tot += MyGlobal.GetDouble(arr);
                        }
                        entry.Value[12] = tot.ToString("n2");
                        HRMonthVsViewRow item = new HRMonthVsViewRow();
                        item.m_StaffID = entry.Key;
                        item.months = entry.Value;
                    }
                    foreach (KeyValuePair<string, string[]> entry in dic)
                    {
                        // do something with entry.Value or entry.Key
                        HRMonthVsViewRow item = new HRMonthVsViewRow();
                        item.m_StaffID = entry.Key;
                        item.months = entry.Value;
                        response.items.Add(item);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetHRBonusAccruedActivities-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetHRBonusAccruedActivities-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        */
        //-----------------------------------

        public ActionResult GetHRBonusAccruedActivities(
    string profile, string sort, string order, string page,
    string search, string timezone, string mode, int year, string chkshowall)
        {
            if (mode == null) mode = "1";
            if (mode.Equals("")) mode = "1";
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new BonusAccruedResponse1();
            response.ret_filename = "BonusAccrued-" + year;
            response.status = false;
            response.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL =
"SELECT accounts.m_StaffID,staffs.m_FName,staffs.m_AccountNo,staffs.m_Mrs,staffs.m_Status,m_Month,sum(m_Dr),sum(m_Cr),sum(m_Cr-m_Dr) as tot,m_ReleaseVoucherarker FROM " + MyGlobal.activeDB + ".tbl_accounts accounts " +
"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = accounts.m_StaffID and staffs.m_Profile = accounts.m_Profile " +
"where m_Ledger = 'Bonus Accrued' and accounts.m_Profile='" + profile + "' " +
"and (m_Year*12+m_Month)>=(" + year + "*12+9) and (m_Year*12+m_Month)<=(" + year + "*12+9+12) ";

                    if (!chkshowall.Equals("True", StringComparison.CurrentCultureIgnoreCase))
                    {
                        sSQL += "and m_Status='Active' ";
                    }
                    sSQL += "group by accounts.m_StaffID,m_Month " +
                    "order by accounts.m_StaffID";

                    Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    int month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    if (!dic.Keys.Contains(staffid))
                                    {
                                        string name = MyGlobal.GetPureString(reader, "m_FName");
                                        int iMrs = MyGlobal.GetPureInt16(reader, "m_Mrs");
                                        if (iMrs == 0) name = "Mr." + name;
                                        if (iMrs == 1) name = "Ms." + name;
                                        if (iMrs == 10 || iMrs == 11) name = "Dr." + name;
                                        string status = MyGlobal.GetPureString(reader, "m_Status");
                                        string accountNo = MyGlobal.GetPureString(reader, "m_AccountNo");

                                        dic.Add(staffid, new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                                            "0", name, status, accountNo,
                                            "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                                        });
                                        //  16 (m_ReleaseVoucherarker index)
                                    }
                                    if (dic.Keys.Contains(staffid))
                                    {
                                        dic[staffid][month] = MyGlobal.GetPureDouble(reader, "tot").ToString("n2");
                                        dic[staffid][month+16] = MyGlobal.GetPureString(reader, "m_ReleaseVoucherarker");
                                    }
                                }
                                response.status = true;
                            }
                            else
                            {
                                response.result = "Sorry!!! No Data";
                            }
                        }
                    }
                    //---------------------Get additional ledger 'Annual Bonus Credit'
                    /*
                    sSQL = "select m_Amount,m_StaffID from " + MyGlobal.activeDB + ".tbl_payslips_addledgers " +
                        "where m_Year='" + (year + 1) + "' and m_Month=8 and m_Ledger='Annual Bonus Credit' " +
                        "and m_Profile = '" + profile + "' ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    if (dic.Keys.Contains(staffid))
                                    {
                                        dic[staffid][12] = MyGlobal.GetPureDouble(reader, "m_Amount").ToString("n2");
                                    }
                                }
                            }
                        }
                    }*/
                    //------------------------------------------------------
                    if (mode.Equals("2"))
                    {
                        foreach (KeyValuePair<string, string[]> entry in dic)
                        {
                            // do something with entry.Value or entry.Key
                            HRMonthVsViewRowBank item = new HRMonthVsViewRowBank();
                            item.StaffID = entry.Key;

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
                            item.SBAccount = entry.Value[15];

                            response.rowsBank.Add(item);
                        }
                    }
                    else
                    {
                        
                        BonusMonthVsViewRow item1 = new BonusMonthVsViewRow();
                        item1.Name = "";
                        
                        item1.StaffID = "REPORT";
                        item1.Status = "YEAR";
                        item1.Oct=year + "";
                        response.rows.Add(item1);
                        foreach (KeyValuePair<string, string[]> entry in dic)
                        {
                            // do something with entry.Value or entry.Key
                            BonusMonthVsViewRow item = new BonusMonthVsViewRow();
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
                                //-----------------get credited
                            }
                            
                            item.Total = tot.ToString("n2");// entry.Value[12];
                            item.TotalAmt = tot;
                            item.Name = entry.Value[13];
                            item.Status = entry.Value[14];
                            //item.SBAccount = entry.Value[15];

                            double dblCredited = 0;
                            if (item.JanS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Jan);
                            if (item.FebS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Feb);
                            if (item.MarS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Mar);
                            if (item.AprS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Apr);

                            if (item.MayS.Length > 0) dblCredited += MyGlobal.GetDouble(item.May);
                            if (item.JunS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Jun);
                            if (item.JlyS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Jly);
                            if (item.AugS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Aug);

                            if (item.SepS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Sep);
                            if (item.OctS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Oct);
                            if (item.NovS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Nov);
                            if (item.DecS.Length > 0) dblCredited += MyGlobal.GetDouble(item.Dec);


                            item.Credited = dblCredited.ToString();// entry.Value[12];
                            response.rows.Add(item);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetHRBonusAccruedActivities-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetHRBonusAccruedActivities-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        /*-------------------------------------------------------------------
        >   activeYear
        >   activeYearFirstHalf(true/false)
        >   activeYearFirstHalf=true
            {
                previousYear_June30_oneyearcompleted
                DOA < (before previousYear_June30-1)
            }
        >   activeYearFirstHalf=false (means second half)
            {
                previousYear_Dec31_oneyearcompleted
                DOA < (before previousYear_Dec31-1)
            }
        //-------------------------------------------------------------------*/
        public ActionResult GetLeaveCredits(
            string profile, string sort, string order, string page,
            string search, string timezone, string year, bool showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var model = new PLCreditModel();
            model.status = false;
            model.result = "";
            int iYear = 0;
            int.TryParse(year, out iYear);
            if (iYear == 0)
            {
                model.result = "Invalid Year Selected";
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //--------------------------------Get All Staffs
                    string showAllCondition = "";
                    if (!showall) showAllCondition = " and (staffs.m_Status='Active') "; // or staffs.m_Status='Trainee'

                    string sSQL = "SELECT " +
"staffs.m_StaffID,staffs.m_DOA,staffs.m_FName,staffs.m_Mrs,staffs.m_Status " +
"from meterbox.tbl_staffs staffs " +
"where staffs.m_Profile = '" + profile + "' " +
"and staffs.m_StaffID!='10000' and staffs.m_StaffID!='CHC0001' " +
showAllCondition +
"order by m_FName;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        model.items = con.Query<PLCreditItem>(sSQL).ToList();
                    }
                    //--------------------------------Find Pay Half and Eligibles
                    int activeYear = DateTime.Now.Year;
                    bool activeYearFirstHalf = (DateTime.Now.Month >= 1) && (DateTime.Now.Month <= 6);
                    DateTime m_CutOff = DateTime.MaxValue;
                    //DateTime lastDay = new DateTime(thisYear, 1, 1);
                    //DateTime middleDay = new DateTime(thisYear, 6, 30);
                    for (var i = 0; i < model.items.Count; i++)
                    {
                        if (model.items[i].m_DOA != DateTime.MinValue)
                        {
                            int DOAYear = model.items[i].m_DOA.Year;
                            // 02-07-2019 -> 01-01-2020  ----> 0(1st Jan)
                            // 02-01-2019 -> 01-07-2019  ----> 1(1st Jly)
                            //DateTime dt_0_0 = new DateTime(DOAYear, 7, 2);
                            //DateTime dt_0_1 = new DateTime(DOAYear+1, 1, 1);
                            DateTime dt_1_0 = new DateTime(DOAYear, 1, 2);
                            DateTime dt_1_1 = new DateTime(DOAYear, 7, 1);

                            //model.items[i].paymentHalf = (model.items[i].m_DOA.Month >= 1) && (model.items[i].m_DOA.Month <= 6) ? 0 : 1;
                            model.items[i].paymentHalf = (model.items[i].m_DOA >= dt_1_0) && (model.items[i].m_DOA <= dt_1_1) ? 1 : 0;

                            if (activeYearFirstHalf)
                            {
                                m_CutOff = new DateTime(activeYear, 1, 1);
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 0 ? 1 : 0;
                                if (model.items[i].paymentHalfNow == 1)
                                {
                                    model.items[i].m_CutOff = m_CutOff;
                                }
                                //if (model.items[i].m_DOA <= new DateTime(activeYear - 1 - 1, 12, 31))
                                if (model.items[i].m_DOA <= new DateTime(activeYear - 1 , 1, 1))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                            else
                            {
                                m_CutOff = new DateTime(activeYear, 7, 1);
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 1 ? 1 : 0;
                                if (model.items[i].paymentHalfNow == 1)
                                {
                                    model.items[i].m_CutOff = m_CutOff;
                                }
                                //if (model.items[i].m_DOA <= new DateTime(activeYear - 1 - 1, 6, 30))
                                if (model.items[i].m_DOA <= new DateTime(activeYear - 1 , 7, 1))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                        }
                        //Console.WriteLine("Value of res=" + res);
                    }
                    //--------------------------------Get Dates
                    sSQL =
"SELECT approved.m_StaffID," +
"from_unixtime(m_Date)," +
//"sum(dblActualWorkingDays_Local) as totaldays," +
"sum(case When from_unixtime(m_Date)>m_DOA then dblActualWorkingDays_Local else 0 End) as totaldays," +
"from_unixtime(min(m_Date)) as dtStart," +//3
"from_unixtime(max(m_Date)) as dtEnd, " +    //4
"min(m_Date) as m_DateStart," + //5
"max(m_Date) as m_DateEnd,m_DOA " +    //6
"FROM meterbox.tbl_attendance_approved approved " +
"left join meterbox.tbl_staffs staffs on staffs.m_StaffID=approved.m_StaffID "+
"where from_unixtime(m_Date)>= '2020-01-01' " +
"and from_unixtime(m_Date)<'" + (m_CutOff.ToString("yyyy-MM-dd")) + "' " +
"and m_PLProcessed is null " +
"group by approved.m_StaffID";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        //model.items = con.Query<PLCreditItem>(sSQL).ToList();
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        var obj = model.items.FirstOrDefault(x => x.m_StaffID == reader.GetString(0));
                                        if (obj != null)
                                        {
                                            if(obj.paymentHalfNow==1 && obj.OneYearPassed == 1)
                                            {
                                                //DateTime dtStart = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                                                //DateTime dtEnd = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);

                                                //if (dtStart<=obj.m_CutOff && dtEnd <= obj.m_CutOff) {
                                                if (obj.dtStart < obj.m_DOA)
                                                {
                                                    obj.dtStart = obj.m_DOA;// reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                                                    obj.dateStart = obj.m_DOA.ToString("dd-MM-yyyy");//reader.IsDBNull(3) ? "" : reader.GetDateTime(3).ToString("dd-MM-yyyy");
                                                    obj.m_DateStart = (Int32)(obj.m_DOA.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                                    obj.m_DateStart = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                                                }
                                                else
                                                {
                                                    obj.dtStart = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                                                    obj.dateStart = reader.IsDBNull(3) ? "" : reader.GetDateTime(3).ToString("dd-MM-yyyy");
                                                    obj.m_DateStart = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                                                }
                                                    obj.dtEnd = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                                                    obj.dateEnd = reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd-MM-yyyy");
                                                    obj.m_DateEnd = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                                                    obj.totaldays = (reader.IsDBNull(2) ? 0 : reader.GetDouble(2));
                                                //}
                                            }
                                            else
                                            {
                                                //obj.dateStart = "";
                                                //obj.dateStart = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------
                    for (var i = 0; i < model.items.Count; i++)
                    {
                        if (model.items[i].dtEnd != DateTime.MinValue &&
                            model.items[i].dtStart != DateTime.MinValue)
                        {
                            model.items[i].months =
                                (model.items[i].dtEnd.Year * 12 + model.items[i].dtEnd.Month) -
                                (model.items[i].dtStart.Year * 12 + model.items[i].dtStart.Month)+1;
                            //(model.items[i].dtEnd - model.items[i].dtStart) + 12 * (model.items[i].dtEnd.Year - model.items[i].dtStart.Year);
                        }

                    }
                    //--------------------is process already done for this year
                    sSQL = "SELECT m_StaffID,m_Type,m_Cr FROM meterbox.tbl_leave " +
                    "where m_Year='" + (iYear + 1) + "' " +
                    "and (m_Type='CL') " +
                    //"and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

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
                                        string staf = reader.GetString(0);
                                        if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                                        {
                                            if (reader.GetString(1).Equals("CL") && reader.GetString(2).Equals("6"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null)
                                                {
                                                    obj.CL_SL_LOP_processed = 1;
                                                    //obj.months = (obj.dtEnd.Month + obj.dtEnd.Year * 12) -  (obj.dtStart.Month + obj.dtStart.Year * 12);
                                                }
                                                /*
                                                if (dic.Keys.Contains(staf))
                                                {
                                                    dic[staf][19] = "1";// CL_SL_LOP_processed
                                                }*/
                                            }/*
                                            if (reader.GetString(1).Equals("PL"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null) obj.PL_processed = 1;
                                                
                                            }*/
                                        }
                                    }
                                }
                            }
                        }
                    }
                    model.result =
                    model.items.Count + " entries listed";
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        //-------------------------------------------------------------------*/
        public ActionResult GetLeaveCredits___2021_02_07(
            string profile, string sort, string order, string page,
            string search, string timezone, string year, bool showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var model = new PLCreditModel();
            model.status = false;
            model.result = "";
            int iYear = 0;
            int.TryParse(year, out iYear);
            if (iYear == 0)
            {
                model.result = "Invalid Year Selected";
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //--------------------------------Get All Staffs
                    string showAllCondition = "";
                    if (!showall) showAllCondition = " and (staffs.m_Status='Active' or staffs.m_Status='Trainee') ";
                    
                    string sSQL = "SELECT " +
"staffs.m_StaffID,staffs.m_DOA,staffs.m_FName,staffs.m_Mrs,staffs.m_Status " +
"from meterbox.tbl_staffs staffs " +
"where staffs.m_Profile = '" + profile + "' " +
"and staffs.m_StaffID!='10000' and staffs.m_StaffID!='CHC0001' " +
showAllCondition +
"order by m_FName;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        model.items = con.Query<PLCreditItem>(sSQL).ToList();
                    }
                    //--------------------------------Find Pay Half and Eligibles
                    int activeYear = DateTime.Now.Year;
                    bool activeYearFirstHalf = (DateTime.Now.Month >= 1) && (DateTime.Now.Month <= 6);

                    //DateTime lastDay = new DateTime(thisYear, 1, 1);
                    //DateTime middleDay = new DateTime(thisYear, 6, 30);
                    for (var i = 0; i < model.items.Count; i++)
                    {
                        if (model.items[i].m_DOA != DateTime.MinValue)
                        {
                            int DOAYear = model.items[i].m_DOA.Year;
                            // 02-07-2019 -> 01-01-2020  ----> 0(1st Jan)
                            // 02-01-2019 -> 01-07-2019  ----> 1(1st Jly)
                            //DateTime dt_0_0 = new DateTime(DOAYear, 7, 2);
                            //DateTime dt_0_1 = new DateTime(DOAYear+1, 1, 1);
                            DateTime dt_1_0 = new DateTime(DOAYear, 1, 2);
                            DateTime dt_1_1 = new DateTime(DOAYear, 7, 1);

                            //model.items[i].paymentHalf = (model.items[i].m_DOA.Month >= 1) && (model.items[i].m_DOA.Month <= 6) ? 0 : 1;
                            model.items[i].paymentHalf = (model.items[i].m_DOA >= dt_1_0) && (model.items[i].m_DOA <= dt_1_1) ? 1 : 0;
                            
                            if (activeYearFirstHalf)
                            {
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 0 ? 1 : 0;
                                if (model.items[i].paymentHalfNow == 1)
                                {
                                    model.items[i].m_CutOff = new DateTime(activeYear-1, 12, 26);
                                }
                                if (model.items[i].m_DOA <= new DateTime(activeYear - 1 - 1, 12, 31))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                            else
                            {
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 1 ? 1 : 0;
                                if (model.items[i].paymentHalfNow == 1)
                                {
                                    model.items[i].m_CutOff = new DateTime(activeYear , 6, 26);
                                }
                                if (model.items[i].m_DOA <= new DateTime(activeYear - 1 - 1, 6, 30))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                        }
                        //Console.WriteLine("Value of res=" + res);
                    }
                    //--------------------------------Get Dates
                    sSQL = "SELECT summary.m_StaffID," +
"from_unixtime((m_DateStart)) as dateStart," +
"(m_DateStart) as m_DateStart," +
"from_unixtime((m_DateEnd)) as dateEnd," +
"(m_DateEnd) as m_DateEnd," +
"summary.m_Year," +
"summary.m_Month," +
"(summary.m_id) as months," +
"(m_ActualWorkingDays) as totaldays," +
"staffs.m_DOA,staffs.m_FName,staffs.m_Mrs,staffs.m_Status " +
"FROM meterbox.tbl_attendance_summary summary " +
"left join meterbox.tbl_staffs staffs " +
"on staffs.m_StaffID = summary.m_StaffID and staffs.m_Profile = summary.m_Profile " +
"where summary.m_Profile = 'support@SharewareDreams.com' " +
"and summary.m_PLProcessed is null " +
"and from_unixtime(summary.m_DateStart)>= '2019-12-26' " +
"order by m_DateEnd";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        //model.items = con.Query<PLCreditItem>(sSQL).ToList();
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        var obj = model.items.FirstOrDefault(x => x.m_StaffID == reader.GetString(0));
                                        if (obj != null)
                                        {

                                            DateTime dtStartDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                                            DateTime dtEndDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                                            //DateTime dtDOA = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9);
                                            /*if (obj.m_StaffID.Equals("CHC0872"))
                                            {
                                                Console.WriteLine("_____");
                                            }*/

                                            if (dtStartDate <= obj.m_CutOff || dtEndDate <= obj.m_CutOff)
                                            {
                                                if (obj.m_DateStart == 0)
                                                {
                                                    if (!reader.IsDBNull(1))
                                                    {
                                                        if (obj.m_DOA <= dtStartDate|| obj.m_DOA <= dtEndDate)
                                                        {
                                                            obj.dateStart = reader.IsDBNull(1) ? "" : reader.GetDateTime(1).ToString("dd-MM-yyyy");
                                                            obj.m_DateStart = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                                                            obj.dtStart = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                                                        }
                                                    }
                                                }
                                                if (obj.m_DateStart > 0)
                                                {
                                                    //obj.months++;// += (reader.IsDBNull(7) ? 0 : reader.GetInt16(7));
                                                    obj.totaldays += (reader.IsDBNull(8) ? 0 : reader.GetInt16(8));
                                                }
                                                obj.dateEnd = reader.IsDBNull(3) ? "" : reader.GetDateTime(3).ToString("dd-MM-yyyy");
                                                obj.m_DateEnd = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                                                obj.dtEnd = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------------------------------
                    for (var i = 0; i < model.items.Count; i++)
                    {
                        if (model.items[i].dtEnd != DateTime.MinValue &&
                            model.items[i].dtStart != DateTime.MinValue)
                        {
                            model.items[i].months =
                                (model.items[i].dtEnd.Year * 12 + model.items[i].dtEnd.Month) -
                                (model.items[i].dtStart.Year * 12 + model.items[i].dtStart.Month);
                            //(model.items[i].dtEnd - model.items[i].dtStart) + 12 * (model.items[i].dtEnd.Year - model.items[i].dtStart.Year);
                        }

                    }
                    //--------------------is process already done for this year
                    sSQL = "SELECT m_StaffID,m_Type,m_Cr FROM meterbox.tbl_leave " +
                    "where m_Year='" + (iYear + 1) + "' " +
                    "and (m_Type='CL') " +
                    //"and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

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
                                        string staf = reader.GetString(0);
                                        if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                                        {
                                            if (reader.GetString(1).Equals("CL") && reader.GetString(2).Equals("6"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null)
                                                {
                                                    obj.CL_SL_LOP_processed = 1;
                                                    //obj.months = (obj.dtEnd.Month + obj.dtEnd.Year * 12) -  (obj.dtStart.Month + obj.dtStart.Year * 12);
                                                }
                                                /*
                                                if (dic.Keys.Contains(staf))
                                                {
                                                    dic[staf][19] = "1";// CL_SL_LOP_processed
                                                }*/
                                            }/*
                                            if (reader.GetString(1).Equals("PL"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null) obj.PL_processed = 1;
                                                
                                            }*/
                                        }
                                    }
                                }
                            }
                        }
                    }
                    model.result =
                    model.items.Count + " entries listed";
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        //-------------------------------------------------------------------*/
        public ActionResult GetLeaveCredits_LastCorrection_05_02_2021(
            string profile, string sort, string order, string page,
            string search, string timezone,string year,bool showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var model = new PLCreditModel();
            model.status = false;
            model.result = "";
            int iYear = 0;
            int.TryParse(year, out iYear);
            if (iYear == 0)
            {
                model.result = "Invalid Year Selected";
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string showAllCondition = "";
                    if (!showall) showAllCondition = " and (staffs.m_Status='Active' or staffs.m_Status='Trainee') ";
                    string sSQL = "";
                    sSQL = "SELECT " +
"staffs.m_StaffID,staffs.m_DOA,staffs.m_FName,staffs.m_Mrs,staffs.m_Status " +
"from meterbox.tbl_staffs staffs " +
"where staffs.m_Profile = '" + profile + "' " +
"and staffs.m_StaffID!='10000' and staffs.m_StaffID!='CHC0001' " +
showAllCondition+
"order by m_FName;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        model.items = con.Query<PLCreditItem>(sSQL).ToList();
                    }

                    sSQL =
"SELECT summary.m_StaffID," +
"from_unixtime( min(m_DateStart)) as dateStart, " +
"min(m_DateStart) as m_DateStart," +
"from_unixtime(max(m_DateEnd)) as dateEnd," +
"max(m_DateEnd) as m_DateEnd," +
"summary.m_Year," +
"summary.m_Month," +
"count(summary.m_id) as months," +
"sum(m_ActualWorkingDays) as totaldays," +
"staffs.m_DOA,staffs.m_FName,staffs.m_Mrs,staffs.m_Status " +
"FROM meterbox.tbl_attendance_summary summary " +
"left join meterbox.tbl_staffs staffs " +
"on staffs.m_StaffID = summary.m_StaffID and staffs.m_Profile = summary.m_Profile " +
"where summary.m_Profile = '" + profile + "' " + showAllCondition +
"and summary.m_PLProcessed is null " +
"and from_unixtime(summary.m_DateStart)>='2019-12-26' " +
"group by summary.m_StaffID " +
"order by staffs.m_FName";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        //model.items = con.Query<PLCreditItem>(sSQL).ToList();
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        var obj = model.items.FirstOrDefault(x => x.m_StaffID == reader.GetString(0));
                                        if (obj != null)
                                        {
                                            obj.dateStart = reader.IsDBNull(1) ? "" : reader.GetDateTime(1).ToString("MM-dd-yyyy");
                                            obj.m_DateStart = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                                            obj.dateEnd = reader.IsDBNull(3) ? "" : reader.GetDateTime(3).ToString("MM-dd-yyyy");
                                            obj.m_DateEnd = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                                            

                                            obj.months = reader.IsDBNull(7) ? 0 : reader.GetInt16(7);
                                            obj.totaldays = reader.IsDBNull(8) ? 0 : reader.GetInt16(8);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //----------------------------------------------------------
                    int activeYear = DateTime.Now.Year;
                    bool activeYearFirstHalf = (DateTime.Now.Month >= 1) && (DateTime.Now.Month <= 6);

                    //DateTime lastDay = new DateTime(thisYear, 1, 1);
                    //DateTime middleDay = new DateTime(thisYear, 6, 30);
                    for (var i = 0; i < model.items.Count; i++)
                    {
                        if (model.items[i].m_DOA != DateTime.MinValue) {
                            int DOAYear = model.items[i].m_DOA.Year;
                            // 02-07-2019 -> 01-01-2020  ----> 0(1st Jan)
                            // 02-01-2019 -> 01-07-2019  ----> 1(1st Jly)
                            //DateTime dt_0_0 = new DateTime(DOAYear, 7, 2);
                            //DateTime dt_0_1 = new DateTime(DOAYear+1, 1, 1);
                            DateTime dt_1_0 = new DateTime(DOAYear, 1, 2);
                            DateTime dt_1_1 = new DateTime(DOAYear, 7, 1);

                            //model.items[i].paymentHalf = (model.items[i].m_DOA.Month >= 1) && (model.items[i].m_DOA.Month <= 6) ? 0 : 1;
                            model.items[i].paymentHalf = (model.items[i].m_DOA >= dt_1_0) && (model.items[i].m_DOA <= dt_1_1) ? 1 : 0;
                            if (activeYearFirstHalf)
                            {
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 0 ? 1 : 0;
                                if (model.items[i].m_DOA<=new DateTime(activeYear - 1 - 1 , 12, 31))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                            else
                            {
                                model.items[i].paymentHalfNow = model.items[i].paymentHalf == 1 ? 1 : 0;
                                if (model.items[i].m_DOA <= new DateTime(activeYear - 1 - 1, 6, 30))
                                {
                                    model.items[i].OneYearPassed = 1;
                                }
                            }
                        }
                        //Console.WriteLine("Value of res=" + res);
                    }
                    //--------------------is process already done for this year
                    sSQL = "SELECT m_StaffID,m_Type,m_Cr FROM meterbox.tbl_leave " +
                    "where m_Year='" + (iYear + 1) + "' " +
                    "and (m_Type='CL') " +
                    //"and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

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
                                        string staf = reader.GetString(0);
                                        if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                                        {
                                            if (reader.GetString(1).Equals("CL") && reader.GetString(2).Equals("6"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null) obj.CL_SL_LOP_processed = 1;
                                                /*
                                                if (dic.Keys.Contains(staf))
                                                {
                                                    dic[staf][19] = "1";// CL_SL_LOP_processed
                                                }*/
                                            }/*
                                            if (reader.GetString(1).Equals("PL"))
                                            {
                                                var obj = model.items.FirstOrDefault(x => x.m_StaffID == staf);
                                                if (obj != null) obj.PL_processed = 1;
                                                
                                            }*/
                                        }
                                    }
                                }
                            }
                        }
                    }
                    model.result =
                    model.items.Count + " entries listed";
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetLeaveCredits-" + ex.Message);
                model.result = "Error-" + ex.Message;
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        //---------------------------------------------------------------------
        public ActionResult GetLeaveCredits_OldWorking(
string profile, string sort, string order, string page,
string search, string timezone, string mode, int year) // Year here is, previoue year of selected year
        {
            if (mode == null) mode = "1";
            if (mode.Equals("")) mode = "1";
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            var response = new BonusAccruedResponse();
            response.status = false;
            response.result = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL =
                        "SELECT summary.m_StaffID,staffs.m_DOA,staffs.m_FName,staffs.m_AccountNo,staffs.m_Mrs,staffs.m_Status,m_Month, " +
"m_ActualWorkingDays as tot,FLOOR(m_ActualWorkingDays/19) as pl FROM " + MyGlobal.activeDB + ".tbl_attendance_summary summary " +
"left join " + MyGlobal.activeDB + ".tbl_staffs staffs on staffs.m_StaffID = summary.m_StaffID and staffs.m_Profile = summary.m_Profile " +
"where summary.m_Profile = '" + profile + "' and m_Year='" + year + "'" +
"group by summary.m_StaffID,m_Month " +
"order by staffs.m_FName";
                    //"order by summary.m_StaffID";

                    Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string staffid = MyGlobal.GetPureString(reader, "m_StaffID");
                                    int month = MyGlobal.GetPureInt16(reader, "m_Month");
                                    if (!dic.Keys.Contains(staffid))
                                    {
                                        string name = MyGlobal.GetPureString(reader, "m_FName");
                                        int iMrs = MyGlobal.GetPureInt16(reader, "m_Mrs");
                                        if (iMrs == 0) name = "Mr." + name;
                                        if (iMrs == 1) name = "Ms." + name;
                                        if (iMrs == 10 || iMrs == 11) name = "Dr." + name;
                                        string status = MyGlobal.GetPureString(reader, "m_Status");
                                        string accountNo = MyGlobal.GetPureString(reader, "m_AccountNo");
                                        string pl = MyGlobal.GetPureString(reader, "pl");
                                        string DOA = MyGlobal.GetPureString(reader, "m_DOA");
                                        string oneyearcompleted = "0";
                                        string appoinmentHalf = "";
                                        int ord = reader.GetOrdinal("m_DOA");
                                        if (!reader.IsDBNull(ord))
                                        {
                                            DateTime dtDOA = reader.GetDateTime(ord);
                                            //  01-01-2021 to 31-06-2021 (June)
                                            if (dtDOA.Month >= 1 && dtDOA.Month <= 6)
                                            {
                                                if (dtDOA.Month == 1)
                                                {
                                                    if (dtDOA.Day == 1)
                                                    {
                                                        appoinmentHalf = "1";
                                                    }
                                                    else
                                                    {
                                                        appoinmentHalf = "2";
                                                    }
                                                }
                                                else
                                                {
                                                    appoinmentHalf = "0";
                                                }
                                            }
                                            else
                                            {
                                                appoinmentHalf = "1";
                                            }
                                            int value = DateTime.Compare(
                                                dtDOA,
                                                new DateTime(year, 1, 1, 0, 0, 0));
                                            if (value <= 0) oneyearcompleted = "1";
                                        }
                                        /*
                                        if (!status.Equals("Active"))
                                        {
                                            eligible = status;
                                        }*/


                                        dic.Add(staffid, new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", name, status, accountNo, pl, DOA, oneyearcompleted, "0", "0", appoinmentHalf });
                                    }
                                    if (dic.Keys.Contains(staffid))
                                    {
                                        dic[staffid][month] = MyGlobal.GetPureDouble(reader, "tot").ToString("n1");
                                    }
                                }
                                response.status = true;
                            }
                            else
                            {
                                response.result = "Sorry!!! No Data";
                            }
                        }
                    }
                    //--------------------is process already done for this year
                    sSQL = "SELECT m_StaffID,m_Type,m_Cr FROM meterbox.tbl_leave " +
                    "where m_Year='" + (year + 1) + "' " +
                    "and (m_Type='CL' or m_Type='PL') " +
                    //"and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

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
                                        string staf = reader.GetString(0);
                                        if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                                        {
                                            if (reader.GetString(1).Equals("CL") && reader.GetString(2).Equals("6"))
                                            {
                                                if (dic.Keys.Contains(staf))
                                                {
                                                    dic[staf][19] = "1";// CL_SL_LOP_processed
                                                }
                                            }
                                            /*
                                            if (reader.GetString(1).Equals("PL"))
                                            {
                                                if (dic.Keys.Contains(staf))
                                                {
                                                    dic[staf][20] = "1";// PL_processed
                                                }
                                            }*/
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //--------------------is process already done for this year
                    if (mode.Equals("2"))
                    {
                        foreach (KeyValuePair<string, string[]> entry in dic)
                        {
                            // do something with entry.Value or entry.Key
                            HRMonthVsViewRowBank item = new HRMonthVsViewRowBank();
                            item.StaffID = entry.Key;

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
                            item.SBAccount = entry.Value[15];

                            response.rowsBank.Add(item);
                        }
                    }
                    else
                    {
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
                            item.PL = entry.Value[16];
                            item.DOA = entry.Value[17];
                            item.oneyearcompleted = entry.Value[18];
                            item.CL_SL_LOP_processed = entry.Value[19];
                            //item.PL_processed = entry.Value[20];
                            item.appoinmentHalf = entry.Value[21];
                            response.rows.Add(item);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MyGlobal.Error("MySqlException GetLeaveCredits-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            catch (Exception ex)
            {
                MyGlobal.Error("Exception GetLeaveCredits-" + ex.Message);
                response.result = "Error-" + ex.Message;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------
        [HttpPost]
        public ActionResult UpdateActualWorkingDays(string profile, string user, string staffid, string year, string month, string days)
        {
            PostResponse response = new PostResponse();

            int iMonth = -1;
            int.TryParse(month, out iMonth);
            if (profile.Length == 0 ||
                user.Length == 0 ||
                staffid.Length == 0 ||
                year.Length != 4 ||
                iMonth == -1)
            {
                response.status = false;
                response.result = "Invalid Request";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL =
                        "SELECT * FROM " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "where m_StaffID = '" + staffid + "' " +
                        "and m_Year='" + year + "' " +
                        "and m_Month='" + month + "' " +
                        "and m_Profile='" + profile + "' ";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                response.status = false;
                                response.result = "Entry Already Exists. Can't Change";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    sSQL = "insert into " + MyGlobal.activeDB + ".tbl_attendance_summary " +
                        "(m_Profile,m_StaffID,m_Year,m_month,m_ActualWorkingDays," +
                        "m_ApprovedBy1,m_ApprovedByTime1) values " +
                        "('" + profile + "','" + staffid + "','" + year + "','" + iMonth + "','" + days + "','" + user + "',Now())";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        int iRes = mySqlCommand.ExecuteNonQuery();
                        response.status = true;
                        if (iRes == 0)
                            response.result = "No records affected. Nothing is Updated";
                        else
                            response.result = "Record Updated";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                }
            } catch (Exception x)
            {
                response.result = "";
            }
            response.status = false;
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------
        [HttpPost]
        public ActionResult ProcessLeaves(
            string profile, string user, string staffid, 
            string year, string datestart, string dateend, 
            string months,
            string allowpl, string pls)
        {
            long dateStart = 0, dateEnd = 0;
            int PLs = 0, iMonths = 0; ;
            long.TryParse(datestart, out dateStart);
            long.TryParse(dateend, out dateEnd);
            int.TryParse(pls, out PLs);
            int.TryParse(months, out iMonths);

            PostResponse response = new PostResponse();
            /*if (true)
            {
                response.status = false;
                response.result = "Not yet released for operation";
                return Json(response, JsonRequestBehavior.AllowGet);
            }*/

            if (allowpl.Equals("1"))
            {
                return ProcessLeaves_PL(profile, user, staffid, year, dateStart, dateEnd, iMonths, PLs);
            }

            int iYear = 0;
            int.TryParse(year, out iYear);
            if (iYear == 0)
            {
                response.result = "Invalid Year Selected";
                response.status = false;
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            if (iYear > DateTime.Now.Year)
            {
                response.result = "Year can't be more than current.";
                response.status = false;
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            if (allowpl.Equals("0"))
            {
                return ProcessLeaves_CL_SL_LOP(profile, user, staffid, year);
            }

            response.result = "Unknown Request";
            response.status = false;
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------
        public ActionResult ProcessLeaves_CL_SL_LOP(string profile, string user, string staffid, string year)
        {
            PostResponse response = new PostResponse();
            response.status = false;
            int iYear = -1;
            int.TryParse(year, out iYear);
            

            if (profile.Length == 0 ||
                user.Length == 0 ||
                staffid.Length == 0 ||
                year.Length != 4 ||
                iYear == -1)
            {
                response.status = false;
                response.result = "Invalid Request";
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            string sInsert = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string message = "", sStaffEmail = "", sStaffName = "";
                    //--------------------is process already done for this year
                    string sSQL = "SELECT m_id FROM meterbox.tbl_leave " +
                    "where m_StaffID = '" + staffid + "' and m_Year='" + (iYear) + "' " +
                    "and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                response.status = false;
                                response.result = "Credit for the year " + (iYear) + " is already processed.";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    //--------------------Get staff details
                    sSQL = "SELECT m_Email,m_FName FROM meterbox.tbl_staffs " +
                    "where m_StaffID = '" + staffid + "' " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sStaffEmail = MyGlobal.GetPureString(reader, "m_Email");
                                    sStaffName = MyGlobal.GetPureString(reader, "m_FName");
                                    message += "Staff " + sStaffName + " ";
                                }
                            }
                        }
                    }
                    if (sStaffEmail.Length == 0)
                    {
                        response.status = false;
                        response.result = "Invalid Staff Data";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                    //--------------------Process Expired CL & LOP
                    sSQL = "SELECT " +
                    "sum(case when m_Type = 'CL' then m_Cr - m_Dr else 0 end) AS cnt_CL, " +
                    "sum(case when m_Type = 'LOP' then m_Cr - m_Dr else 0 end) AS cnt_LOP " +
                    "FROM meterbox.tbl_leave " +
                    "where m_StaffID = '" + staffid + "' and m_Year = '" + (iYear - 1) + "' " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    double dblCLs = MyGlobal.GetPureInt16(reader, "cnt_CL");
                                    double dblLOPs = MyGlobal.GetPureInt16(reader, "cnt_LOP");
                                    if (dblCLs > 0)
                                    {
                                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                                        "('" + profile + "', '" + staffid + "', " + (iYear - 1) + ", 'CL', Now(), 0, " + dblCLs + ", 'CL Expired for the Calendar Year " + (iYear - 1) + "');";
                                        message += dblCLs + " CLs where expired. ";
                                    }
                                    if (dblLOPs > 0)
                                    {
                                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                                        "('" + profile + "', '" + staffid + "', " + (iYear - 1) + ", 'LOP', Now(), 0, " + dblLOPs + ", 'LOP Expired for the Calendar Year " + (iYear - 1) + "');";
                                        message += dblLOPs + " LOPs where expired. ";
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------Add New CL & SL
                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'CL', Now(), 6, 0, 'Fresh CL Credits for the Calendar Year " + (iYear) + "');";
                    message += "6 CLs where credited. ";

                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'SL', Now(), 6, 0, 'Fresh SL Credits for the Calendar Year " + (iYear) + "');";
                    message += "6 SLs where credited. ";

                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'LOP', Now(), 12, 0, 'Fresh LOP Credits for the Calendar Year " + (iYear) + "');";
                    message += "12 LOPs where credited. ";

                    //-----------------------Update Message
                    string session = staffid + "_" + (iYear) + "_" + "0" + "_" + "0" + "_" + DateTime.Now.ToString("HHmmss");
                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                        "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                        "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Param1,m_Param2,m_Param3) values " +
                        "('" + profile + "',2," +
                        "'" + "meterbox@chcgroup.in" + "','MeterBox',''," +
                        "'" + sStaffEmail + "','" + sStaffName + "','" + staffid + "'," +
                        "'" + session + "',Now(),Now()," +
                        "''," +
                        "''," +
                        "'');";

                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_LeaveType,m_LeaveStatus,m_Days) " +
                    "values ('" + profile + "','" + staffid + "','" + (iYear) + "','0','1','" + sStaffEmail + "','" + "meterbox@chcgroup.in" + "'," +
                    "'" + message + "',Now(),'" + session + "','','1','0');";

                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";

                    MySqlTransaction trans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = trans;
                    try
                    {
                        myCommand.CommandText = sInsert;
                        myCommand.ExecuteNonQuery();
                        trans.Commit();
                        response.status = true;
                        response.result = message;
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        response.result = "Operation Rolled back: " + e.Message;
                    }

                }
            }
            catch (Exception x)
            {
                response.result = "Exception: " + x.Message;
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ProcessLeaves_PL(string profile, string user, string staffid, string year, 
            long dateStart,long dateEnd,int months, int PLs)
        {
            PostResponse response = new PostResponse();
            response.status = false;
            int iYear = -1;
            int.TryParse(year, out iYear);
            //double dblPLs = 0;
            if (PLs == 0)
            {
                response.result = "No PLs to credit";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            if (profile.Length == 0 ||
                user.Length == 0 ||
                staffid.Length == 0 ||
                year.Length != 4 ||
                iYear == -1)
            {
                response.result = "Invalid Request";
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            string sInsert = "";


            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();

                string message = "", sStaffEmail = "", sStaffName = "", sSQL = "";
                //----------------------------
                using (MySqlTransaction transact = con.BeginTransaction())
                {
                    try
                    {
                       /* sSQL = "UPDATE " +
    " meterbox.tbl_attendance_summary " +
    "Set m_PLProcessed=1 " +
    "where m_Profile = '" + profile + "' " +
    "and m_PLProcessed is null " +
    "and from_unixtime(m_DateStart)> '2019-01-01' " +
    "and m_StaffID is not null " +
    "and m_StaffID = '" + staffid + "' " +
    "and m_DateStart>= '" + dateStart + "' " +
    "and m_DateEnd<= '" + dateEnd + "'";*/

                        int activeYear = DateTime.Now.Year;
                        bool activeYearFirstHalf = (DateTime.Now.Month >= 1) && (DateTime.Now.Month <= 6);
                        DateTime m_CutOff = DateTime.MaxValue;
                        if (activeYearFirstHalf)
                        {
                            m_CutOff = new DateTime(activeYear, 1, 1);
                        }
                        else
                        {
                            m_CutOff = new DateTime(activeYear, 7, 1);
                        }

                        sSQL = "UPDATE meterbox.tbl_attendance_approved " +
                            "Set m_PLProcessed=1 " +
"where from_unixtime(m_Date)>= '2020-01-01' " +
"and from_unixtime(m_Date)<'" + (m_CutOff.ToString("yyyy-MM-dd")) + "' " +
"and m_Date>='" + dateStart + "' and m_Date<='" + dateEnd + "' " +
"and m_StaffID='" + staffid + "' " +
"and m_PLProcessed is null ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            int iRecords = mySqlCommand.ExecuteNonQuery();
                            /*if (months != iRecords)
                            {
                                transact.Rollback();
                                response.result = "Mismatch in calculation.";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }*/
                            message += "PLs were processed and updated. ";
                        }

                        //--------------------is process already done for this year
                        /*
                        string sSQL = "SELECT m_id FROM meterbox.tbl_leave " +
                        "where m_StaffID = '" + staffid + "' and m_Year='" + (iYear) + "' " +
                        "and m_Type='PL' and m_Cr is not null " +
                        "and m_Profile='" + profile + "'";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    response.status = false;
                                    response.result = "PL Credit for the year " + (iYear) + " is already processed";
                                    return Json(response, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                        */
                        //--------------------Get staff details
                        sSQL = "SELECT m_Email,m_FName FROM meterbox.tbl_staffs " +
                        "where m_StaffID = '" + staffid + "' " +
                        "and m_Profile='" + profile + "'";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        sStaffEmail = MyGlobal.GetPureString(reader, "m_Email");
                                        sStaffName = MyGlobal.GetPureString(reader, "m_FName");
                                        message += "Staff " + sStaffName + " ";
                                    }
                                }
                            }
                        }
                        if (sStaffEmail.Length == 0)
                        {
                            transact.Rollback();
                            response.status = false;
                            response.result = "Invalid Staff Data";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                        //-----------------------Add New PL
                        //if (allowpl.Equals("1"))
                        //{
                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                        "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'PL', Now(), " + PLs + ",0 , 'Fresh PL Credits for the Calendar Year " + (iYear) + (PLs == 0 ? ". NONE" : "") + "');";
                        message += PLs + " PLs where credited";
                        /*}
                        else
                        {
                            message += dblPLs + " PLs <b>NOT</b> credited.(Not 12 months)<br>";
                        }*/
                        //-----------------------Update Message
                        string session = staffid + "_" + (iYear) + "_" + "0" + "_" + "0" + "_" + DateTime.Now.ToString("HHmmss");
                        sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                            "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                            "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Param1,m_Param2,m_Param3) values " +
                            "('" + profile + "',2," +
                            "'" + "meterbox@chcgroup.in" + "','MeterBox',''," +
                            "'" + sStaffEmail + "','" + sStaffName + "','" + staffid + "'," +
                            "'" + session + "',Now(),Now()," +
                            "''," +
                            "''," +
                            "'');";

                        sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_LeaveType,m_LeaveStatus,m_Days) " +
                        "values ('" + profile + "','" + staffid + "','" + (iYear) + "','0','1','" + sStaffEmail + "','" + "meterbox@chcgroup.in" + "'," +
                        "'" + message + "',Now(),'" + session + "','','1','0');";

                        sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                        "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";


                        using (MySqlCommand myCommand = con.CreateCommand())
                        {
                            myCommand.Connection = con;
                            myCommand.CommandText = sInsert;
                            myCommand.ExecuteNonQuery();
                        }
                        transact.Commit();
                        response.status = true;
                        response.result = message;
                    }
                    catch (Exception ex)
                    {
                        transact.Rollback();
                        response.result = "Error: " + ex.Message;
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                }

            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
    
        [HttpPost]
        public ActionResult ProcessLeaves_OLD_Good(string profile,string user, string staffid, string year,string pls,string allowpl)
        {
            PostResponse response = new PostResponse();
            response.status = false;
            int iYear = -1;
            int.TryParse(year, out iYear);
            double dblPLs = 0;
            double.TryParse(pls, out dblPLs);

            if (profile.Length == 0 ||
                user.Length == 0 ||
                staffid.Length == 0 ||
                year.Length != 4 ||
                iYear == -1)
            {
                response.status = false;
                response.result = "Invalid Request";
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            string sInsert = "";
            
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string message = "",sStaffEmail="",sStaffName="";
                    //--------------------is process already done for this year
                    string sSQL = "SELECT m_id FROM meterbox.tbl_leave " +
                    "where m_StaffID = '" + staffid + "' and m_Year='" + (iYear) + "' " +
                    "and m_Type='CL' and m_Cr=6 " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                response.status = false;
                                response.result = "Credit for the year " + (iYear) + " is already processed";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    //--------------------Get staff details
                    sSQL = "SELECT m_Email,m_FName FROM meterbox.tbl_staffs " +
                    "where m_StaffID = '" + staffid + "' " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sStaffEmail = MyGlobal.GetPureString(reader, "m_Email");
                                    sStaffName = MyGlobal.GetPureString(reader, "m_FName");
                                    message += "Staff " + sStaffName +" ";
                                }
                            }
                        }
                    }
                    if (sStaffEmail.Length == 0)
                    {
                        response.status = false;
                        response.result = "Invalid Staff Data";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                    //--------------------Process Expired CL & LOP
                    sSQL = "SELECT "+
                    "sum(case when m_Type = 'CL' then m_Cr - m_Dr else 0 end) AS cnt_CL, "+
                    "sum(case when m_Type = 'LOP' then m_Cr - m_Dr else 0 end) AS cnt_LOP " +
                    "FROM meterbox.tbl_leave " +
                    "where m_StaffID = '" + staffid + "' and m_Year = '" + (iYear-1) + "' " +
                    "and m_Profile='" + profile + "'";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    double dblCLs = MyGlobal.GetPureInt16(reader, "cnt_CL");
                                    double dblLOPs = MyGlobal.GetPureInt16(reader, "cnt_LOP");
                                    if (dblCLs > 0)
                                    {
                                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                                        "('" + profile + "', '" + staffid + "', " + (iYear-1) + ", 'CL', Now(), 0, " + dblCLs + ", 'CL Expired for the Calendar Year " + (iYear-1) + "');";
                                        message += dblCLs + " CLs where expired. ";
                                    }
                                    if (dblLOPs > 0)
                                    {
                                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                                        "('" + profile + "', '" + staffid + "', " + (iYear-1) + ", 'LOP', Now(), 0, " + dblLOPs + ", 'LOP Expired for the Calendar Year " + (iYear-1) + "');";
                                        message += dblLOPs + " LOPs where expired. ";
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------Add New CL & SL
                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'CL', Now(), 6, 0, 'Fresh CL Credits for the Calendar Year " + (iYear) + "');";
                    message += "6 CLs where credited.";

                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'SL', Now(), 6, 0, 'Fresh SL Credits for the Calendar Year " + (iYear) + "');";
                    message += "6 SLs where credited. ";

                    sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                    "('" + profile + "', '" + staffid + "', " + (iYear ) + ", 'LOP', Now(), 12, 0, 'Fresh LOP Credits for the Calendar Year " + (iYear) + "');";
                    message += "12 LOPs where credited. ";
                    if (allowpl.Equals("1"))
                    {
                        sInsert += "insert into meterbox.tbl_leave (m_Profile,m_StaffID,m_Year,m_Type,m_Time,m_Cr,m_Dr,m_Description) values " +
                        "('" + profile + "', '" + staffid + "', " + (iYear) + ", 'PL', Now(), " + dblPLs + ",0 , 'Fresh PL Credits for the Calendar Year " + (iYear) + (dblPLs == 0 ? ". NONE" : "") + "');";
                        message += dblPLs + " PLs where credited. ";
                    }
                    else
                    {
                        message += dblPLs + " PLs <NOT> credited.(Not 12 months). ";
                    }
                    //-----------------------Update Message
                    string session = staffid + "_" + (iYear ) + "_" + "0" + "_" + "0" + "_" + DateTime.Now.ToString("HHmmss");
                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_sessions " +
                        "(m_Profile,m_Type,m_From,m_FromName,m_FromStaffID," +
                        "m_To,m_ToName,m_ToStaffID,m_Session,m_Time,m_TimeUpdated,m_Param1,m_Param2,m_Param3) values " +
                        "('" + profile + "',2," +
                        "'" + "meterbox@chcgroup.in" + "','MeterBox',''," +
                        "'" + sStaffEmail + "','"+sStaffName+"','" + staffid + "'," +
                        "'" + session + "',Now(),Now()," +
                        "''," +
                        "''," +
                        "'');";

                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_Year,m_Month,m_Day,m_From,m_To,m_Message,m_Time,m_Session,m_LeaveType,m_LeaveStatus,m_Days) " +
                    "values ('" + profile + "','" + staffid + "','" + (iYear) + "','0','1','" + sStaffEmail + "','" + "meterbox@chcgroup.in" + "'," +
                    "'" + message + "',Now(),'" + session + "','','1','0');";

                    sInsert += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                    "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";

                    MySqlTransaction trans = con.BeginTransaction();
                    MySqlCommand myCommand = con.CreateCommand();
                    myCommand.Connection = con;
                    myCommand.Transaction = trans;
                    try
                    {
                        myCommand.CommandText = sInsert;
                        myCommand.ExecuteNonQuery();
                        trans.Commit();
                        response.status = true;
                        response.result = message;
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        response.result = "Operation Rolled back: " + e.Message;
                    }

                }
            }
            catch (Exception x)
            {
                response.result = "Exception: "+x.Message;
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //------------------------------------
        public ActionResult GetSalaryReports(string profile, string sort, string order, string page, string search,bool showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var leaveActivitiesResponse = new SalaryReportResponse();
            leaveActivitiesResponse.status = false;
            leaveActivitiesResponse.result = "";

            try
            {
                string sSQL = "";
                //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    using (MySqlConnection con1 = new MySqlConnection(MyGlobal.GetConnectionString()))
                    {
                        con1.Open();
                        //________________________________________________________________
                        String sSearchKey = " (m_StaffID like '%" + search + "%' or " +
                        "m_FName like '%" + search + "%' or " +
                        "m_Mobile like '%" + search + "%' or " +
                        "m_Payscale like '%" + search + "%' or " +
                        "m_Email like '%" + search + "%') ";
                        

                        sSQL = "select count(m_id) as cnt from " + MyGlobal.activeDB + ".tbl_staffs as staffs " +
                            "where m_Profile='" + profile + "' ";
                        if (!showall) sSQL += " and m_LWD is null ";
                        if (search.Length > 0) sSQL += " and " + sSearchKey + " ";


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
                        if (sort.Equals("undefined") || sort.Length == 0) sort = "m_FName";
                        if (order.Equals("undefined") || order.Length == 0) order = "asc";




                        sSQL = "select staffs.m_id,staffs.m_StaffID,staffs.m_FName," +
                            "m_DOJ,m_DOA,m_LWD,m_Designation,m_Status,m_Team,m_Base,m_Band,m_Grade,m_Mrs,m_Payscale," +
                            "m_ReportToAdministrative,m_ReportToFunctional,m_Mobile,m_CCTNo " +
                            " from " + MyGlobal.activeDB + ".tbl_staffs as staffs " +
                            "where m_Profile='" + profile + "' ";
                        if (!showall) sSQL += " and m_LWD is null ";
                        if (search.Length > 0) sSQL += " and " + sSearchKey + " ";
                        sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        SalaryReportRow row = new SalaryReportRow();
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) row.m_FName = reader.GetString(reader.GetOrdinal("m_FName"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Mrs")))
                                        {
                                            row.m_FName = (reader.GetInt16(reader.GetOrdinal("m_Mrs")) == 1 ? "Mr." : "Ms.")
                                                + row.m_FName;
                                        }
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DOJ"))) row.m_DOJ = reader.GetDateTime(reader.GetOrdinal("m_DOJ")).ToString("dd-MM-yyyy");
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_DOA"))) row.m_DOA = reader.GetDateTime(reader.GetOrdinal("m_DOA")).ToString("dd-MM-yyyy");
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_LWD"))) row.m_LWD = reader.GetDateTime(reader.GetOrdinal("m_LWD")).ToString("dd-MM-yyyy");
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) row.m_Designation = reader.GetString(reader.GetOrdinal("m_Designation"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) row.m_Status = reader.GetString(reader.GetOrdinal("m_Status"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) row.m_Team = reader.GetString(reader.GetOrdinal("m_Team"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) row.m_Base = reader.GetString(reader.GetOrdinal("m_Base"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) row.m_Band = reader.GetString(reader.GetOrdinal("m_Band"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) row.m_Grade = reader.GetString(reader.GetOrdinal("m_Grade"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale"))) row.m_Payscale = reader.GetString(reader.GetOrdinal("m_Payscale"));

                                        if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToAdministrative"))) row.m_ReportToAdministrative = reader.GetString(reader.GetOrdinal("m_ReportToAdministrative"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToFunctional"))) row.m_ReportToFunctional = reader.GetString(reader.GetOrdinal("m_ReportToFunctional"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) row.m_Mobile = reader.GetString(reader.GetOrdinal("m_Mobile"));
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_CCTNo"))) row.m_CCTNo = reader.GetString(reader.GetOrdinal("m_CCTNo"));

                                        string sql = "";

                                        sql = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs where " +
                                            "m_email='" + row.m_ReportToFunctional + "' limit 1;";
                                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
                                        {
                                            using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                                            {
                                                if (reader1.HasRows)
                                                {
                                                    if (reader1.Read())
                                                    {
                                                        if (reader1["m_FName"] != null)
                                                        {
                                                            row.m_ReportToFunctionalName = reader1["m_FName"].ToString();
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        sql = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs where " +
                                            "m_email='" + row.m_ReportToAdministrative + "' limit 1;";
                                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
                                        {
                                            using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                                            {
                                                if (reader1.HasRows)
                                                {
                                                    if (reader1.Read())
                                                    {
                                                        if (reader1["m_FName"] != null)
                                                        {
                                                            row.m_ReportToAdministrativeName = reader1["m_FName"].ToString();
                                                        }
                                                    }
                                                }
                                            }
                                        }


                                        row.CTC = "";
                                        row.GROSS = "";
                                        
                                        if (row.m_Payscale != null && row.m_Payscale.Length > 0)
                                        {
                                            //sql = "SELECT m_Amount FROM meterbox.tbl_payscale_master where m_Ledger='CTC' and m_Name='" + row.m_Payscale + "'";
                                            sql = "SELECT "+
"sum(Case When m_Ledger = 'CTC' Then m_Amount Else 0 End) as amount, "+
"sum(Case When m_Type = 'cr' Then m_Amount Else 0 End) as gross " +
"FROM meterbox.tbl_payscale_master where  m_Name = '" + row.m_Payscale + "'";
                                            using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
                                            {
                                                using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                                                {
                                                    if (reader1.HasRows)
                                                    {
                                                        if (reader1.Read())
                                                        {
                                                            if (reader1["amount"] != null)
                                                            {
                                                                row.CTC = reader1["amount"].ToString();
                                                            }
                                                            if (reader1["gross"] != null)
                                                            {
                                                                row.GROSS_Fixed = reader1["gross"].ToString();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        sql = "SELECT m_Year,m_Month," +
                                        "sum(case when m_Type = 'earn' then m_Amount else 0 end) AS 'earn', " +
                                        "sum(case when m_Type = 'cr' then m_Amount else 0 end) AS 'cr', " +
                                        "sum(case when m_Type = 'dr' then m_Amount else 0 end) AS 'dr', " +
                                        "sum(case when m_Type = 'deduct' then m_Amount else 0 end) AS 'deduct'," +
                                        "sum(case when m_Type='earn' and m_Ledger like 'Retention Bonus%' then m_Amount else 0 end) AS 'RetentionBonus' "+
                                        "FROM meterbox.tbl_payslips where m_StaffID = '" + row.m_StaffID + "' " +
                                        "group by m_Year,m_Month " +
                                        "order by m_id desc,m_Year, m_Month " +
                                        "limit 1";
                                        using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
                                        {
                                            using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                                            {
                                                if (reader1.HasRows)
                                                {
                                                    if (reader1.Read())
                                                    {
                                                        row.GROSS_Year = reader1.IsDBNull(0) ? "0" : reader1.GetInt16(0).ToString();
                                                        row.GROSS_Month = reader1.IsDBNull(1) ? "0" : (reader1.GetInt16(1)<9? "0" + (reader1.GetInt16(1) + 1).ToString(): (reader1.GetInt16(1) + 1).ToString());
                                                        row.TakeHome= (reader1.GetFloat(2)-  reader1.GetFloat(5)).ToString();
                                                        row.GROSS = reader1["earn"].ToString();
                                                        row.RetentionBonus= reader1["RetentionBonus"].ToString();
                                                    }
                                                }
                                            }
                                        }

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
                        con1.Close();
                        con.Close();
                    }
                }
            }
            catch (MySqlException ex)
            {
                leaveActivitiesResponse.result = "Error-" + ex.Message;
            }
            return Json(leaveActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        //--------------------------------------
        public ActionResult GetSalaryReportsExcel(string profile, string search, bool showall)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();

            var leaveActivitiesResponse = new SalaryReportResponse();
            leaveActivitiesResponse.status = false;
            leaveActivitiesResponse.result = "";
            leaveActivitiesResponse.ret_filename = "Staff summary as on "+DateTime.Now.ToString("MM-dd-yyyy mm:ss");

            //Starts Test Stored Procedure 08-07-2024 Sivaguru M

            try
            {
                //string sSQL = "";

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();

                    string sSearchKey = " (m_StaffID LIKE '%" + search + "%' OR " +
                                        "m_FName LIKE '%" + search + "%' OR " +
                                        "m_Mobile LIKE '%" + search + "%' OR " +
                                        "m_Payscale LIKE '%" + search + "%' OR " +
                                        "m_Email LIKE '%" + search + "%') ";

                    using (MySqlCommand mySqlCommand = new MySqlCommand("meterbox.GetStaffs", con))
                    {
                        mySqlCommand.CommandType = CommandType.StoredProcedure;
                        mySqlCommand.Parameters.AddWithValue("profile", profile);
                        mySqlCommand.Parameters.AddWithValue("showall", showall);
                        mySqlCommand.Parameters.AddWithValue("search", search);

                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    SalaryReportRow row = new SalaryReportRow();
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) row.m_FName = reader.GetString(reader.GetOrdinal("m_FName"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mrs")))
                                    {
                                        row.m_FName = (reader.GetInt16(reader.GetOrdinal("m_Mrs")) == 1 ? "Mr." : "Ms.") + row.m_FName;
                                    }
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOJ"))) row.m_DOJ = reader.GetDateTime(reader.GetOrdinal("m_DOJ")).ToString("dd-MM-yyyy");
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_DOA"))) row.m_DOA = reader.GetDateTime(reader.GetOrdinal("m_DOA")).ToString("dd-MM-yyyy");
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_LWD"))) row.m_LWD = reader.GetDateTime(reader.GetOrdinal("m_LWD")).ToString("dd-MM-yyyy");
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) row.m_Designation = reader.GetString(reader.GetOrdinal("m_Designation"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) row.m_Status = reader.GetString(reader.GetOrdinal("m_Status"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) row.m_Team = reader.GetString(reader.GetOrdinal("m_Team"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) row.m_Base = reader.GetString(reader.GetOrdinal("m_Base"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) row.m_Band = reader.GetString(reader.GetOrdinal("m_Band"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) row.m_Grade = reader.GetString(reader.GetOrdinal("m_Grade"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale"))) row.m_Payscale = reader.GetString(reader.GetOrdinal("m_Payscale"));

                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToAdministrative"))) row.m_ReportToAdministrative = reader.GetString(reader.GetOrdinal("m_ReportToAdministrative"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToFunctional"))) row.m_ReportToFunctional = reader.GetString(reader.GetOrdinal("m_ReportToFunctional"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) row.m_Mobile = reader.GetString(reader.GetOrdinal("m_Mobile"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_CCTNo"))) row.m_CCTNo = reader.GetString(reader.GetOrdinal("m_CCTNo"));

                                    using (MySqlConnection con1 = new MySqlConnection(MyGlobal.GetConnectionString()))
                                    {
                                        con1.Open();

                                        using (MySqlCommand mySqlCommand1 = new MySqlCommand("meterbox.GetAdministrativeName", con1))
                                        {
                                            mySqlCommand1.CommandType = CommandType.StoredProcedure;
                                            mySqlCommand1.Parameters.AddWithValue("email", row.m_ReportToAdministrative);

                                            using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
                                            {
                                                if (reader1.HasRows && reader1.Read() && reader1["m_FName"] != null)
                                                {
                                                    row.m_ReportToAdministrativeName = reader1["m_FName"].ToString();
                                                }
                                            }
                                        }
                                        con1.Close();
                                    }

                                    row.CTC = "";
                                    row.GROSS = "";

                                    if (!string.IsNullOrEmpty(row.m_Payscale))
                                    {
                                        using (MySqlConnection con2 = new MySqlConnection(MyGlobal.GetConnectionString()))
                                        {
                                            con2.Open();
                                            using (MySqlCommand mySqlCommand2 = new MySqlCommand("meterbox.GetPayscaleDetails", con2))
                                            {
                                                mySqlCommand2.CommandType = CommandType.StoredProcedure;
                                                mySqlCommand2.Parameters.AddWithValue("payscale", row.m_Payscale);

                                                using (MySqlDataReader reader2 = mySqlCommand2.ExecuteReader())
                                                {
                                                    if (reader2.HasRows && reader2.Read())
                                                    {
                                                        row.CTC = reader2["amount"].ToString();
                                                        row.GROSS_Fixed = reader2["gross"].ToString();
                                                    }
                                                }
                                            }
                                            con2.Close();
                                        }
                                    }
                                    using (MySqlConnection con3 = new MySqlConnection(MyGlobal.GetConnectionString()))
                                    {
                                        using (MySqlCommand mySqlCommand3 = new MySqlCommand("meterbox.GetLatestPayslip", con3))
                                        {
                                            con3.Open();
                                            mySqlCommand3.CommandType = CommandType.StoredProcedure;
                                            mySqlCommand3.Parameters.AddWithValue("staffID", row.m_StaffID);

                                            using (MySqlDataReader reader3 = mySqlCommand3.ExecuteReader())
                                            {
                                                if (reader3.HasRows && reader3.Read())
                                                {
                                                    row.GROSS_Year = reader3.IsDBNull(0) ? "0" : reader3.GetInt16(0).ToString();
                                                    row.GROSS_Month = reader3.IsDBNull(1) ? "0" : (reader3.GetInt16(1) < 9 ? "0" + (reader3.GetInt16(1) + 1).ToString() : (reader3.GetInt16(1) + 1).ToString());
                                                    row.TakeHome = (reader3.GetFloat(2) - reader3.GetFloat(5)).ToString();
                                                    row.GROSS = reader3["earn"].ToString();
                                                    row.RetentionBonus = reader3["RetentionBonus"].ToString();
                                                }
                                            }
                                            con3.Close();
                                        }
                                    }

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

                    con.Close();
                    
                }
            }


            //Ends Test Stored Procedure

            //try
            //{



            //    //Starts Previous Code before Stored Procedure 08-07-2024 Sivaguru M
            //    string sSQL = "";
            //    //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            //    using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            //    {
            //        con.Open();
            //        using (MySqlConnection con1 = new MySqlConnection(MyGlobal.GetConnectionString()))
            //        {
            //            con1.Open();
            //            //________________________________________________________________
            //            String sSearchKey = " (m_StaffID like '%" + search + "%' or " +
            //            "m_FName like '%" + search + "%' or " +
            //            "m_Mobile like '%" + search + "%' or " +
            //            "m_Payscale like '%" + search + "%' or " +
            //            "m_Email like '%" + search + "%') ";



            //            sSQL = "select staffs.m_id,staffs.m_StaffID,staffs.m_FName," +
            //                "m_DOJ,m_DOA,m_LWD,m_Designation,m_Status,m_Team,m_Base,m_Band,m_Grade,m_Mrs,m_Payscale," +
            //                "m_ReportToAdministrative,m_ReportToFunctional,m_Mobile,m_CCTNo " +
            //                " from " + MyGlobal.activeDB + ".tbl_staffs as staffs " +
            //                "where m_Profile='" + profile + "' ";
            //            if (!showall) sSQL += " and m_LWD is null ";
            //            if (search.Length > 0) sSQL += " and " + sSearchKey + " ";
            //            //sSQL += "order by " + sort + " " + order + " limit " + iPageSize + " offset " + PAGE + ";";

            //            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
            //            {
            //                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
            //                {
            //                    if (reader.HasRows)
            //                    {
            //                        while (reader.Read())
            //                        {
            //                            SalaryReportRow row = new SalaryReportRow();
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_id"))) row.m_id = reader.GetInt32(reader.GetOrdinal("m_id"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_StaffID"))) row.m_StaffID = reader.GetString(reader.GetOrdinal("m_StaffID"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_FName"))) row.m_FName = reader.GetString(reader.GetOrdinal("m_FName"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Mrs")))
            //                            {
            //                                row.m_FName = (reader.GetInt16(reader.GetOrdinal("m_Mrs")) == 1 ? "Mr." : "Ms.")
            //                                    + row.m_FName;
            //                            }
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_DOJ"))) row.m_DOJ = reader.GetDateTime(reader.GetOrdinal("m_DOJ")).ToString("dd-MM-yyyy");
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_DOA"))) row.m_DOA = reader.GetDateTime(reader.GetOrdinal("m_DOA")).ToString("dd-MM-yyyy");
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_LWD"))) row.m_LWD = reader.GetDateTime(reader.GetOrdinal("m_LWD")).ToString("dd-MM-yyyy");
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Designation"))) row.m_Designation = reader.GetString(reader.GetOrdinal("m_Designation"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Status"))) row.m_Status = reader.GetString(reader.GetOrdinal("m_Status"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Team"))) row.m_Team = reader.GetString(reader.GetOrdinal("m_Team"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Base"))) row.m_Base = reader.GetString(reader.GetOrdinal("m_Base"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Band"))) row.m_Band = reader.GetString(reader.GetOrdinal("m_Band"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Grade"))) row.m_Grade = reader.GetString(reader.GetOrdinal("m_Grade"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Payscale"))) row.m_Payscale = reader.GetString(reader.GetOrdinal("m_Payscale"));

            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToAdministrative"))) row.m_ReportToAdministrative = reader.GetString(reader.GetOrdinal("m_ReportToAdministrative"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_ReportToFunctional"))) row.m_ReportToFunctional = reader.GetString(reader.GetOrdinal("m_ReportToFunctional"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_Mobile"))) row.m_Mobile = reader.GetString(reader.GetOrdinal("m_Mobile"));
            //                            if (!reader.IsDBNull(reader.GetOrdinal("m_CCTNo"))) row.m_CCTNo = reader.GetString(reader.GetOrdinal("m_CCTNo"));

            //                            string sql = "";

            //                            sql = "select m_FName from " + MyGlobal.activeDB + ".tbl_staffs where " +
            //                                "m_email='" + row.m_ReportToAdministrative + "' limit 1;";
            //                            using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
            //                            {
            //                                using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
            //                                {
            //                                    if (reader1.HasRows)
            //                                    {
            //                                        if (reader1.Read())
            //                                        {
            //                                            if (reader1["m_FName"] != null)
            //                                            {
            //                                                row.m_ReportToAdministrativeName = reader1["m_FName"].ToString();
            //                                            }
            //                                        }
            //                                    }
            //                                }
            //                            }


            //                            row.CTC = "";
            //                            row.GROSS = "";

            //                            if (row.m_Payscale != null && row.m_Payscale.Length > 0)
            //                            {
            //                                //sql = "SELECT m_Amount FROM meterbox.tbl_payscale_master where m_Ledger='CTC' and m_Name='" + row.m_Payscale + "'";
            //                                sql = "SELECT " +
            //                                    "sum(Case When m_Ledger = 'CTC' Then m_Amount Else 0 End) as amount, " +
            //                                    "sum(Case When m_Type = 'cr' Then m_Amount Else 0 End) as gross " +
            //                                    "FROM meterbox.tbl_payscale_master where  m_Name = '" + row.m_Payscale + "'";
            //                                using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
            //                                {
            //                                    using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
            //                                    {
            //                                        if (reader1.HasRows)
            //                                        {
            //                                            if (reader1.Read())
            //                                            {
            //                                                if (reader1["amount"] != null)
            //                                                {
            //                                                    row.CTC = reader1["amount"].ToString();
            //                                                }
            //                                                if (reader1["gross"] != null)
            //                                                {
            //                                                    row.GROSS_Fixed = reader1["gross"].ToString();
            //                                                }
            //                                            }
            //                                        }
            //                                    }
            //                                }
            //                            }

            //                            //Ends Previous Code before Stored Procedure 08-07-2024 Sivaguru M

            //                            //Starts Previous Before Stored Procedure 08-07-2024 Sivaguru M
            //                            sql = "SELECT m_Year,m_Month," +
            //                            "sum(case when m_Type = 'earn' then m_Amount else 0 end) AS 'earn', " +
            //                            "sum(case when m_Type = 'cr' then m_Amount else 0 end) AS 'cr', " +
            //                            "sum(case when m_Type = 'dr' then m_Amount else 0 end) AS 'dr', " +
            //                            "sum(case when m_Type = 'deduct' then m_Amount else 0 end) AS 'deduct'," +
            //                            "sum(case when m_Type='earn' and m_Ledger like 'Retention Bonus%' then m_Amount else 0 end) AS 'RetentionBonus' " +
            //                            "FROM meterbox.tbl_payslips where m_StaffID = '" + row.m_StaffID + "' " +
            //                            "group by m_Year,m_Month " +
            //                            "order by m_id desc,m_Year, m_Month " +
            //                            "limit 1";
            //                            using (MySqlCommand mySqlCommand1 = new MySqlCommand(sql, con1))
            //                            {
            //                                using (MySqlDataReader reader1 = mySqlCommand1.ExecuteReader())
            //                                {
            //                                    if (reader1.HasRows)
            //                                    {
            //                                        if (reader1.Read())
            //                                        {
            //                                            row.GROSS_Year = reader1.IsDBNull(0) ? "0" : reader1.GetInt16(0).ToString();
            //                                            row.GROSS_Month = reader1.IsDBNull(1) ? "0" : (reader1.GetInt16(1) < 9 ? "0" + (reader1.GetInt16(1) + 1).ToString() : (reader1.GetInt16(1) + 1).ToString());
            //                                            row.TakeHome = (reader1.GetFloat(2) - reader1.GetFloat(5)).ToString();
            //                                            row.GROSS = reader1["earn"].ToString();
            //                                            row.RetentionBonus = reader1["RetentionBonus"].ToString();
            //                                        }
            //                                    }
            //                                }
            //                            }
            //                            //Ends Previous Before Stored Procedure




            //                            leaveActivitiesResponse.items.Add(row);
            //                        }
            //                        leaveActivitiesResponse.status = true;
            //                        leaveActivitiesResponse.result = "Done";
            //                    }
            //                    else
            //                    {
            //                        leaveActivitiesResponse.result = "Sorry!!! No Records";
            //                    }
            //                }
            //            }
            //            //________________________________________________________________
            //            con1.Close();
            //            con.Close();
            //        }
            //    }
            //}
            catch (MySqlException ex)
            {
                leaveActivitiesResponse.result = "Error-" + ex.Message;
            }



            return Json(leaveActivitiesResponse, JsonRequestBehavior.AllowGet);
        }
        private void GetLeaves(MySqlConnection con1, ref LoadLeaveDataResponse loadLeaveDataResponse, string profile, string year, string staffid)
        {
            AccessmanagerController.GetSumOfDrCrFromLeave_and_leavesTable(con1, ref loadLeaveDataResponse, profile,year,staffid);
        }
        //private string GetCTC()

        #region Pulse Issues File Upload Funtions
        //public ActionResult PulseIssuesUpload()
        //{
        //    try
        //    {
        //        IFormFile pro_img = HttpContext.Request.Form.Files["fileimage"];
        //        var obj = JsonConvert.DeserializeObject<ProductReqDTO>(Request.Form["ProductDetail"]);



        //    }
        //    catch(Exception e)
        //    {
        //        var Message = e.Message;
        //    }


        //    return null;
        //}
        #endregion
    }


}
