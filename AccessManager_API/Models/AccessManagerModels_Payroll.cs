using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyHub.Models
{
    public class ManagePayrollsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string selected { get; set; }
        public Int16 iActionDay { get; set; }
        public List<string> payrolls { get; set; } = new List<string>();
        public List<PayrollLedger> ledgers { get; set; } = new List<PayrollLedger>();
        public ManagePayrollsResponse()
        {
            status = false;
            result = "";
            selected = "";
            iActionDay = 0;
        }
    }
    public class PayrollLedger
    {
        public string m_Payroll { get; set; }
        public string m_Ledger { get; set; }
        public string m_LedgerParent { get; set; }
        public double m_LedgerParentPercentage { get; set; }
        public string m_Action { get; set; }
        public double m_Amount { get; set; }
        public PayrollLedger()
        {
            m_Payroll = "";
            m_Ledger = "";
            m_LedgerParent = "";
            m_LedgerParentPercentage = 0;
            m_Action = "";
            m_Amount = 0;
        }
    }
    //---------------------------
    public class PayscalesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public bool reload { get; set; }
        public List<PayscaleItem> items { get; set; }
        public PayscalesResponse()
        {
            items = new List<PayscaleItem>();
            reload = false;
        }
    }
    public class PayscaleItem
    {
        public Int32 m_id { get; set; }
        public string m_Name { get; set; }
        public Int32 m_Key { get; set; } // Its also timestamp
        public string m_CreatedBy { get; set; }
        public Int32 m_CreatedTime { get; set; }
        public string m_UpdatedBy { get; set; }
        public Int32 m_UpdatedTime { get; set; }
        public int allowdelete { get; set; }
        public double m_CTC { get; set; }
    }
    //---------------------------
    public class MyPayslipsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public bool reload { get; set; }
        public List<MyPayslipRow> items { get; set; }
        public MyPayslipsResponse()
        {
            items = new List<MyPayslipRow>();
            reload = false;
        }
    }
    public class MyPayslipRow
    {
        public Int32 m_id { get; set; }
        public Int32 m_DateStart { get; set; }
        public Int32 m_DateEnd { get; set; }
        public int m_Year { get; set; }
        public int m_Month { get; set; }
        public string m_MonthName { get; set; }
        public Int32 m_CreatedTime { get; set; }
        public Int16 m_WorkingDays { get; set; }
        public string m_PayscaleName { get; set; }
        public double m_CrTot { get; set; }
        public double m_EarnsTot { get; set; }
        public double m_DeductsTot { get; set; }
        public string m_StaffID{ get; set; }
        public Int16 m_DaysToBePaidTotal { get; set; }
        public Int32 m_VchNo { get; set; }
        public Boolean bHasBonus { get; set; }
    }
    
}