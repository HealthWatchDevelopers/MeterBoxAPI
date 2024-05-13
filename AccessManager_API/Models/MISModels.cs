using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Models
{
    /*
    public class BonusAccruedResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int total_count { get; set; }
        public List<HRMonthVsViewRow> items { get; set; }
        public BonusAccruedResponse()
        {
            status = false;
            result = "";
            total_count =0;
            items = new List<HRMonthVsViewRow>();
        }
    }
    public class HRMonthVsViewRow
    {
        public long m_id { get; set; }
        public string m_StaffID { get; set; }
        public string[] months { get; set; }

        public HRMonthVsViewRow()
        {
            m_StaffID = "";
            months = new string[14]; // 12 months, total, staff name
        }
    }
    */
    //---------------------------------------
    public class PLCreditModel
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int total_count { get; set; }
        public List<PLCreditItem> items { get; set; }
        public PLCreditModel()
        {
            status = false;
            result = "";

            items = new List<PLCreditItem>();
        }
    }
    public class PLCreditItem
    {
        public string m_StaffID { get; set; }
        public string m_FName { get; set; }
        public string m_Status { get; set; }
        public double totaldays { get; set; }
        public DateTime m_DOA { get; set; }
        public int m_Mrs { get; set; }
        public int m_Year { get; set; }
        public int m_Month { get; set; }
        public string dateStart { get; set; }
        public string dateEnd { get; set; }
        public long m_DateStart { get; set; }
        public long m_DateEnd { get; set; }
        public DateTime dtStart { get; set; }
        public DateTime dtEnd { get; set; }
        public int months { get; set; }
        public int CL_SL_LOP_processed { get; set; }
        //public int PL_processed { get; set; }
        public int OneYearPassed { get; set; }
        public int paymentHalfNow { get; set; }
        public int paymentHalf { get; set; }
        public DateTime m_CutOff { get; set; }
        public PLCreditItem()
        {
            CL_SL_LOP_processed = 0;
            //PL_processed = 0;
            OneYearPassed = 0;
            paymentHalfNow = -1;
            paymentHalf = -1;
            totaldays = 0;
            months = 0;
            m_DateStart = 0;
            m_DateEnd = 0;
        }
    }
    public class BonusAccruedResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }
        public List<HRMonthVsViewRow> rows { get; set; }
        public List<HRMonthVsViewRowBank> rowsBank { get; set; }
        public BonusAccruedResponse()
        {
            status = false;
            result = "";
            ret_filename = "BonusAccrued";
            rows = new List<HRMonthVsViewRow>();
            rowsBank = new List<HRMonthVsViewRowBank>();
        }
    }
    public class HRMonthVsViewRow
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string StaffID { get; set; }
        public string Jan { get; set; }
        public string Feb { get; set; }
        public string Mar { get; set; }
        public string Apr { get; set; }
        public string May { get; set; }
        public string Jun { get; set; }
        public string Jly { get; set; }
        public string Aug { get; set; }
        public string Sep { get; set; }
        public string Oct { get; set; }
        public string Nov { get; set; }
        public string Dec { get; set; }


        public string OctS { get; set; }
        public string NovS { get; set; }
        public string DecS { get; set; }

        public string JanS { get; set; }
        public string FebS { get; set; }
        public string MarS { get; set; }
        public string AprS { get; set; }
        public string MayS { get; set; }
        public string JunS { get; set; }
        public string JlyS { get; set; }
        public string AugS { get; set; }
        public string SepS { get; set; }


        public string Total { get; set; }
        public string PL { get; set; }
        public string DOA { get; set; }
        public string oneyearcompleted { get; set; }
        public string CL_SL_LOP_processed { get; set; }
        public string PL_processed { get; set; }
        public string appoinmentHalf { get; set; }
        
        public HRMonthVsViewRow()
        {
            Name = "";
            Status = "";
            StaffID = "";
            Jan = "";
            Feb = "";
            Mar = "";
            Apr = "";
            May = "";
            Jun = "";
            Jly = "";
            Aug = "";
            Sep = "";
            Oct = "";
            Nov = "";
            Dec = "";
            Total = "";
            PL = "";
            DOA = "";
            oneyearcompleted = "0";
            PL_processed = "0";
            CL_SL_LOP_processed = "0";
            appoinmentHalf = "";
        }
    }
    public class BonusAccruedResponse1
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }
        public List<BonusMonthVsViewRow> rows { get; set; }
        public List<HRMonthVsViewRowBank> rowsBank { get; set; }
        public BonusAccruedResponse1()
        {
            status = false;
            result = "";
            ret_filename = "BonusAccrued";
            rows = new List<BonusMonthVsViewRow>();
            rowsBank = new List<HRMonthVsViewRowBank>();
        }
    }
    public class BonusMonthVsViewRow
    { 
        public string Name { get; set; }
        public string StaffID { get; set; }
        public string Status { get; set; }
        
        public string Oct { get; set; }
        public string Nov { get; set; }
        public string Dec { get; set; }

        public string Jan { get; set; }
        public string Feb { get; set; }
        public string Mar { get; set; }
        public string Apr { get; set; }
        public string May { get; set; }
        public string Jun { get; set; }
        public string Jly { get; set; }
        public string Aug { get; set; }
        public string Sep { get; set; }


        public string OctS { get; set; }
        public string NovS { get; set; }
        public string DecS { get; set; }

        public string JanS { get; set; }
        public string FebS { get; set; }
        public string MarS { get; set; }
        public string AprS { get; set; }
        public string MayS { get; set; }
        public string JunS { get; set; }
        public string JlyS { get; set; }
        public string AugS { get; set; }
        public string SepS { get; set; }

        public string Total { get; set; }
        public string Credited { get; set; }

        public double TotalAmt { get; set; }
        

        public BonusMonthVsViewRow()
        {
            Name = "";
            StaffID = "";
            Status = "";
            
            Oct = "";
            Nov = "";
            Dec = "";
            Jan = "";
            Feb = "";
            Mar = "";
            Apr = "";
            May = "";
            Jun = "";
            Jly = "";
            Aug = "";
            Sep = "";

            OctS = "";
            NovS = "";
            DecS = "";
            JanS = "";
            FebS = "";
            MarS = "";
            AprS = "";
            MayS = "";
            JunS = "";
            JlyS = "";
            AugS = "";
            SepS = "";

            Total = "";
            Credited = "";
        }
    }
    public class HRMonthVsViewRowBank
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string StaffID { get; set; }
        public string SBAccount { get; set; }
        public string Total { get; set; }
        public HRMonthVsViewRowBank()
        {
            Name = "";
            Status = "";
            StaffID = "";
            SBAccount = "";
            Total = "";
        }
    }
    //----------------------------------------
    public class SalaryReportResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<SalaryReportRow> items { get; set; }
        public string ret_filename { get; set; }
        
        public SalaryReportResponse()
        {
            items = new List<SalaryReportRow>();
        }
    }
    public class SalaryReportRow
    {
        public Int32 m_id { get; set; }
        public string m_FName { get; set; }
        public string m_StaffID { get; set; }
        public string m_DOJ { get; set; }
        public string m_DOA { get; set; }
        public string m_LWD { get; set; }
        public string m_Designation { get; set; }
        public string m_Status { get; set; }
        public string m_Team { get; set; }
        public string m_Base { get; set; }
        public string m_Band { get; set; }
        public string m_Grade { get; set; }
        public int m_Mrs { get; set; }
        public string m_Payscale { get; set; }

        public string m_ReportToAdministrative { get; set; }
        public string m_ReportToAdministrativeName { get; set; }
        public string m_ReportToFunctional { get; set; }
        public string m_ReportToFunctionalName { get; set; }

        public string m_Mobile { get; set; }

        public string CTC { get; set; }
        public string GROSS_Year { get; set; }
        public string GROSS_Month { get; set; }
        public string GROSS_Fixed { get; set; }
        public string GROSS { get; set; }
        public string TakeHome { get; set; }
        public string RetentionBonus { get; set; }

        public string m_CCTNo { get; set; }
        
        

        /*
        public string m_TimeApproved { get; set; }
        public string m_Time { get; set; }
        public string m_From { get; set; }
        public string m_To { get; set; }
        public string m_Date { get; set; }
        public string m_Type { get; set; }
        public string m_LeaveType { get; set; }
        public string m_Days { get; set; }
        */
    }
}