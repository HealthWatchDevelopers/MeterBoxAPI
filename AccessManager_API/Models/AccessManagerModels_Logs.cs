using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Models
{
    public class MasterlogResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<MasterlogRow> items { get; set; }
        public MasterlogResponse()
        {
            items = new List<MasterlogRow>();
        }
    }
    public class MasterlogRow
    {
        public Int32 m_id { get; set; }
        public string m_StaffID { get; set; }
        public string m_StaffName { get; set; }
        public string m_StaffID_Concern { get; set; }
        public string m_Time { get; set; }
        public string m_IP { get; set; }
        public string m_ConcernTable { get; set; }
        public string m_Changes { get; set; }
    }
    //----------------------------------------
    public class LoginActivitiesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<LoginActivityRow> items { get; set; }
        public LoginActivitiesResponse()
        {
            items = new List<LoginActivityRow>();
        }
    }
    public class LoginActivityRow
    {
        public Int32 m_id { get; set; }
        public string m_User { get; set; }
        public string m_Name { get; set; }
        public string m_StaffID { get; set; }
        public string m_Time { get; set; }
        public string m_Activity { get; set; }
        public string m_Status { get; set; }
        public string m_IP { get; set; }
        public string m_Browser { get; set; }
    }
    //----------------------------------------
    public class LeaveActivitiesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<LeaveActivityRow> items { get; set; }
        public LeaveActivitiesResponse()
        {
            items = new List<LeaveActivityRow>();
        }
    }
    public class LeaveActivityRow
    {
        public Int32 m_id { get; set; }
        public string m_TimeApproved { get; set; }
        public string m_Time { get; set; }
        public string m_StaffID { get; set; }
        public string m_From { get; set; }
        public string m_To { get; set; }
        public string m_Date { get; set; }
        public string m_Type { get; set; }
        public string m_LeaveType { get; set; }
        public string m_Days { get; set; }
    }
}
