using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Models
{
    public class AccessPacket
    {
        public Int32 m_id { get; set; }
        public string m_HardwareID { get; set; }
        public string m_User { get; set; }
        public string m_Activity { get; set; }
        public Int32 m_ActivityTime { get; set; }
        public Int32 m_WorkTime { get; set; }
        public string m_ReasonHead { get; set; }
        public string m_ReasonNote { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
    }
    public class AccessManagerResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string data { get; set; }
        public string syncid { get; set; }
        public bool bSyncDataValid { get; set; }
        public int locktime { get; set; }
        public Int32 ServerTime { get; set; }
        public List<BreakItem> breaks { get; set; }
        public ActiveShift activeShift { get; set; }
        public AccessManagerResponse()
        {
            activeShift = new ActiveShift();
            breaks = new List<BreakItem>();
            locktime = 5;
            bSyncDataValid = false;
        }
    }
    public class ReportHeadsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<BreakItem> breaks { get; set; }
        public ReportHeadsResponse()
        {
            breaks = new List<BreakItem>();
        }
    }
    public class ActiveShift
    {
        public string StartDate;
        public string RosterName;
        public string ShiftName;
        public long lShiftStartUnix;
        public long lShiftEndUnix;
        public string worktime { get; set; }
        public long lWorktime { get; set; }
        public string Remark;
        public ActiveShift()
        {
            worktime = "";
            lWorktime = 0;
            StartDate = "";
            RosterName = "";
            ShiftName = "";
            lShiftStartUnix = 0;
            lShiftEndUnix = 0;
            Remark = "";
        }
    }
    public class BreakItem
    {
        public string key;
        public string value;
        public BreakItem(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
        public BreakItem()
        {
            this.key = "";
            this.value = "";
        }
    }
    //________________________________________
    public class TerminalActivityResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<TerminalActivityRow> items { get; set; }
        public TerminalActivityResponse()
        {
            items = new List<TerminalActivityRow>();
        }
    }
    public class TerminalActivityRow
    {
        public Int32 m_ID { get; set; }
        public string m_HardwareID { get; set; }
        public string m_HardwareName { get; set; }
        public string m_IP { get; set; }
        public string m_StaffID { get; set; }
        public string m_Name { get; set; }
        public string m_Email { get; set; }
        public string m_Activity { get; set; }
        public string m_ActivityStart { get; set; }
        public string m_ActivityTime { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public string m_WorkTime { get; set; }
        public string m_ReasonHead { get; set; }
        public string m_ReasonNote { get; set; }
        public double LiveSince { get; set; }
        public double SinceActivity { get; set; }
        public string m_Version { get; set; }
        public string m_Team { get; set; }
        public TerminalActivityRow()
        {
            m_ID = 0;
            m_HardwareID = "";
            m_HardwareName = "";
            m_IP = "";
            m_StaffID = "";
            m_Name = "";
            m_Email = "";
            m_Activity = "";
            m_ActivityTime = "";
            m_ActivityTime = "";
            m_Lat = 0;
            m_Lng = 0;
            m_WorkTime = "";
            m_ReasonHead = "";
            m_ReasonNote = "";
            LiveSince = 99999;
            SinceActivity = 99999;
            m_Version = "";
            m_Team = "";
        }
    }
    //________________________________________
    public class GetStaffResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        //public string total_count { get; set; }
        public StaffItem staffItem { get; set; }
        public GetStaffResponse()
        {
            staffItem = new StaffItem();
        }
    }
    public class StaffActivityResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<StaffItem> items { get; set; }
        public StaffActivityResponse()
        {
            items = new List<StaffItem>();
        }
    }
    public class StaffItem
    {
        public Int32 m_id { get; set; }
        public string m_StaffID { get; set; }
        public string m_FName { get; set; }
        public string m_Email { get; set; }
        public string m_Activity { get; set; }
        public string m_ActivityTime { get; set; }
        public string m_IP { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public string m_WorkTime { get; set; }
        public string m_Username { get; set; }
        public string m_ReasonHead { get; set; }
        public string m_ReasonNote { get; set; }
        public string m_Mobile { get; set; }
        public string m_Designation { get; set; }
        public string m_Roll { get; set; }
        public string m_Team { get; set; }
        public string m_Base { get; set; }
        public string m_Type { get; set; }
        public string m_ReportToFunctional { get; set; }
        public string m_ReportToAdministrative { get; set; }
        public string m_MenuKey { get; set; }
        public string m_Band { get; set; }
        public string m_Grade { get; set; }
        public string m_Mrs { get; set; }
        public DateTime m_DOB { get; set; }
        public DateTime m_DOJ { get; set; }
        public DateTime m_DOA { get; set; }
        public DateTime m_LWD { get; set; }
        public int m_LWDExpired { get; set; }
        public int m_ViewSelected { get; set; }
        public string m_Status { get; set; }
        public Int16 m_Lock { get; set; }
        public string m_HardwareID { get; set; }
        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string m_ApprovedBy4 { get; set; }
        public string m_PayscaleName { get; set; }
        public Int32 m_PayscaleKey { get; set; }
        public Int32 m_PayscaleStartDate { get; set; }
        public string m_AttendanceMethod { get; set; }
        public string m_Bank { get; set; }
        public string m_AccountNo { get; set; }
        public string m_Branch { get; set; }
        public string m_IFSC { get; set; }
        public string m_EPF_UAN { get; set; }
        public string m_ESICNumber { get; set; }
        public string m_AttendanceSource { get; set; }
        public string m_AADHAR_Uploaded { get; set; }
        public string m_AADHAR_Number { get; set; }
        public string m_AADHAR_Name { get; set; }
        public string m_AADHAR_FatherName { get; set; }
        public string m_PAN_Uploaded { get; set; }
        public string m_PAN_Number { get; set; }
        public string m_PAN_Name { get; set; }
        public string m_PAN_FatherName { get; set; }

        public string m_CCTNo { get; set; }
        public string m_CCTCleardDate { get; set; }
        public string m_RetentionBonusEffectiveDate { get; set; }
        public string m_RetentionBonusAmount { get; set; }

        public StaffItem()
        {
            m_id = 0;
            m_StaffID = "";
            m_FName = "";
            m_Activity = "";
            m_ActivityTime = "";
            m_IP = "";
            m_Lat = 0;
            m_Lng = 0;
            m_WorkTime = "";
            m_ReasonHead = "";
            m_ReasonNote = "";
            m_Email = "";
            m_Mobile = "";
            m_Designation = "";
            m_Roll = "";
            m_Team = "";
            m_Base = "";
            m_Type = "";
            m_ReportToFunctional = "";
            m_ReportToAdministrative = "";
            m_MenuKey = "";
            m_Band = "";
            m_Grade = "";
            m_Mrs = "";
            m_ViewSelected = 0;
            m_AttendanceSource = "";
            //m_DOB = "";
            //m_DOJ = "";
            //m_DOA = "";
            m_HardwareID = "";
            m_Status = "";
            m_Lock = 0;// lock by default
            m_ApprovedBy1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedBy3 = "";
            m_ApprovedBy4 = "";
            m_PayscaleName = "";
            m_PayscaleKey = 0;
            m_PayscaleStartDate = 0;
            m_AttendanceMethod = "";
            m_Bank = "";
            m_AccountNo = "";
            m_Branch = "";
            m_IFSC = "";
            m_EPF_UAN = "";
            m_ESICNumber = "";
            m_AADHAR_Uploaded = "";
            m_AADHAR_Number = "";
            m_AADHAR_Name = "";
            m_AADHAR_FatherName = "";
            m_PAN_Uploaded = "";
            m_PAN_Number = "";
            m_PAN_Name = "";
            m_PAN_FatherName = "";
        }
    }
    //---------------------------------------------------
    public class ClientSignInResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string m_StaffID { get; set; }
        public string Name { get; set; }
        public string m_Email { get; set; }
        public bool LockReasonReceived { get; set; }
        public List<BreakItem> breaks { get; set; }
        public ClientSignInResponse()
        {
            breaks = new List<BreakItem>();
            LockReasonReceived = false;
        }
    }
    //--------------------------------------------------
    public class GetNowResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }

        public List<string> sarStatus { get; set; }
        public List<NowItem> items { get; set; }
        public GetNowResponse()
        {
            items = new List<NowItem>();
            sarStatus = new List<string>();
        }
    }
    public class NowItem
    {
        public Int32 m_id { get; set; }
        public string m_StaffID { get; set; }
        public string m_StaffName { get; set; }
        public string m_Email { get; set; }
        public string m_Activity { get; set; }
        public string m_ActivityTime { get; set; }
        public string m_IP { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public string m_WorkTime { get; set; }
        public string m_Username { get; set; }
        public string m_ReasonHead { get; set; }
        public string m_ReasonNote { get; set; }
        public string m_Mobile { get; set; }
        public string m_Designation { get; set; }
        public string m_Roll { get; set; }
        public string m_Team { get; set; }
        public string m_Base { get; set; }
        public string m_Type { get; set; }
        public string m_ReportToFunctional { get; set; }
        public string m_ReportToAdministrative { get; set; }
        public string m_MenuKey { get; set; }
        public string m_Band { get; set; }
        public string m_Grade { get; set; }
        public string m_Mrs { get; set; }
        public DateTime m_DOB { get; set; }
        public DateTime m_DOJ { get; set; }
        public DateTime m_DOA { get; set; }
        public DateTime m_LWD { get; set; }
        public int m_LWDExpired { get; set; }
        public int m_ViewSelected { get; set; }
        public string m_Status { get; set; }
        public Int16 m_Lock { get; set; }
        public string m_HardwareID { get; set; }
        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string m_ApprovedBy4 { get; set; }
        public string m_PayscaleName { get; set; }
        public Int32 m_PayscaleKey { get; set; }
        public Int32 m_PayscaleStartDate { get; set; }
        public string m_AttendanceMethod { get; set; }
        public string m_Bank { get; set; }
        public string m_AccountNo { get; set; }
        public string m_Branch { get; set; }
        public string m_IFSC { get; set; }
        public string m_EPF_UAN { get; set; }
        public string m_ESICNumber { get; set; }
        public string m_AttendanceSource { get; set; }
        public string m_AADHAR_Uploaded { get; set; }
        public string m_AADHAR_Number { get; set; }
        public string m_AADHAR_Name { get; set; }
        public string m_AADHAR_FatherName { get; set; }
        public string m_PAN_Uploaded { get; set; }
        public string m_PAN_Number { get; set; }
        public string m_PAN_Name { get; set; }
        public string m_PAN_FatherName { get; set; }

        public string m_RosterMarker { get; set; }
        public string m_RosterName { get; set; }
        public string m_ShiftName { get; set; }
        public string m_ShiftAssigned { get; set; }
        public string m_ShiftActual { get; set; }
        public NowItem()
        {
            m_RosterMarker = "";
            m_RosterName = "";
            m_ShiftName = "";
            m_ShiftAssigned = "";
            m_ShiftActual = "";
            //---------------------
            m_id = 0;
            m_StaffID = "";
            m_StaffName = "";
            m_Activity = "";
            m_ActivityTime = "";
            m_IP = "";
            m_Lat = 0;
            m_Lng = 0;
            m_WorkTime = "";
            m_ReasonHead = "";
            m_ReasonNote = "";
            m_Email = "";
            m_Mobile = "";
            m_Designation = "";
            m_Roll = "";
            m_Team = "";
            m_Base = "";
            m_Type = "";
            m_ReportToFunctional = "";
            m_ReportToAdministrative = "";
            m_MenuKey = "";
            m_Band = "";
            m_Grade = "";
            m_Mrs = "";
            m_ViewSelected = 0;
            m_AttendanceSource = "";
            //m_DOB = "";
            //m_DOJ = "";
            //m_DOA = "";
            m_HardwareID = "";
            m_Status = "";
            m_Lock = 0;// lock by default
            m_ApprovedBy1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedBy3 = "";
            m_ApprovedBy4 = "";
            m_PayscaleName = "";
            m_PayscaleKey = 0;
            m_PayscaleStartDate = 0;
            m_AttendanceMethod = "";
            m_Bank = "";
            m_AccountNo = "";
            m_Branch = "";
            m_IFSC = "";
            m_EPF_UAN = "";
            m_ESICNumber = "";
            m_AADHAR_Uploaded = "";
            m_AADHAR_Number = "";
            m_AADHAR_Name = "";
            m_AADHAR_FatherName = "";
            m_PAN_Uploaded = "";
            m_PAN_Number = "";
            m_PAN_Name = "";
            m_PAN_FatherName = "";
        }
    }
    //---------------------------------------------------
    public class GetStaffsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }

        public List<string> sarStatus { get; set; }
        public List<StaffItem> items { get; set; }
        public GetStaffsResponse()
        {
            items = new List<StaffItem>();
            sarStatus = new List<string>();
        }
    }
    //---------------------------------------------------
    public class OnUpdateCell
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string cellid { get; set; }
        public string cellvalue { get; set; }
    }
    public class OnUpdateCell_production
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string cellid { get; set; }
        public string cellvalue { get; set; }
    }
    public class OnLoadRostersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<string> sarRosters { get; set; }
        public List<string> sarRosterOptions { get; set; }
        public string selectedRoster { get; set; }
        public string AdminHead { get; set; }
        public int per_edit { get; set; }
        public int per_attendance { get; set; }
        public int per_production { get; set; }
        public int per_roster { get; set; }
        public List<MyShift> oMyShifts { get; set; }
        public List<RosterRow> oRosterRows { get; set; }
        public OnLoadRostersResponse()
        {
            per_attendance = 0;
            per_production = 0;
            per_roster = 0;
            sarRosters = new List<string>();
            sarRosterOptions = new List<string>();
            oMyShifts = new List<MyShift>();
            oRosterRows = new List<RosterRow>();
            AdminHead = "";
        }
    }
    public class OnLoadShiftDetailsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string staffspecificname { get; set; }
        public string staffspecificid { get; set; }
        public List<string> sarRosterOptions { get; set; }
        public List<string> sarDayHeaders { get; set; }
        public List<int> sarDayCounters { get; set; }
        public List<Holiday> sarDayHolidays { get; set; }
        public List<RosterRow> oRosterRows { get; set; }
        public List<StaffRow> oStaffRows { get; set; }
        public List<string> sarRosters { get; set; }
        public OnLoadShiftDetailsResponse()
        {
            sarRosterOptions = new List<string>();
            sarDayHeaders = new List<string>();
            sarDayCounters = new List<int>();
            sarDayHolidays = new List<Holiday>();
            oRosterRows = new List<RosterRow>();
            oStaffRows = new List<StaffRow>();
            sarRosters = new List<string>();
        }
    }
    public class Holiday
    {
        public string c { get; set; }
        public int t { get; set; }
        public string d { get; set; }
        public Holiday()
        {
            c = "";
            t = 0;
            d = "";
        }
    }
    public class RosterRow
    {
        public List<RosterDay> arRosterDays { get; set; }
        public string m_id { get; set; }
        public string m_StaffName { get; set; }
        public string m_StaffID { get; set; }
        public RosterRow()
        {
            arRosterDays = new List<RosterDay>();
        }
    }
    public class StaffRow
    {
        public List<string> arRosterOptions { get; set; }
        public string m_id { get; set; }
        public string m_RosterName { get; set; }
        public string m_ShiftName { get; set; }
        public string m_StaffName { get; set; }
        public string m_StaffID { get; set; }
        public Int32 m_ShiftStart { get; set; }
        public Int32 m_ShiftEnd { get; set; }
        public StaffRow()
        {
            arRosterOptions = new List<string>();
        }
    }
    public class RosterDay
    {
        public string id { get; set; }
        public int day { get; set; }
        public string Code { get; set; }
        public string Desc { get; set; }
        public string Holi { get; set; }
        public string Log { get; set; }
        public string cls { get; set; }
        public int expired { get; set; }
        public PRODuction production { get; set; }
    }
    //---------------------------------------------------
    public class OnCreateRosterResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        //public List<MyRoster> myRosters { get; set; }
        public List<string> sarRosters { get; set; }
        public MyRoster SelectedRoster { get; set; }
        public OnCreateRosterResponse()
        {
            status = false;
            result = "";
            sarRosters = new List<string>();
        }
    }
    public class MyRoster
    {
        public string m_Name { get; set; }
        public List<MyShift> myShifts { get; set; }
        public MyRoster()
        {
            myShifts = new List<MyShift>();
        }
    }
    public class MyShift
    {
        public string m_Name { get; set; }
        public Int32 m_StartTime { get; set; }
        public Int32 m_EndTime { get; set; }
    }
    //---------------------------------------------------FreeStaffResponse
    public class FreeStaffResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<FreeStaffItem> staffs { get; set; }
        public FreeStaffResponse()
        {
            staffs = new List<FreeStaffItem>();
        }
    }
    public class FreeStaffItem
    {
        public long m_id { get; set; }
        public string m_Name { get; set; }
        public string m_StaffID { get; set; }
        public string m_Designation { get; set; }
        public string m_Roll { get; set; }
        public string m_Team { get; set; }
        public string m_Base { get; set; }
        public string m_Type { get; set; }
        public FreeStaffItem()
        {
            m_id = 0;
            m_Name = "";
            m_StaffID = "";
            m_Type = "";
        }
    }
    public class ShiftNameResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<ShiftNameItem> names { get; set; }
        public ShiftNameResponse()
        {
            names = new List<ShiftNameItem>();
        }
    }
    public class ShiftNameItem
    {
        public long m_id { get; set; }
        public string m_Name { get; set; }
        public string m_ShiftStartTime { get; set; }
        public string m_ShiftEndTime { get; set; }
        public string m_Designation { get; set; }
        public ShiftNameItem()
        {
            m_id = 0;
            m_Name = "";
            m_ShiftStartTime = "";
            m_ShiftEndTime = "";
            m_Designation = "";
        }
    }
    //---------------------------------------------------FreeStaffResponse END
    /* ----------------------------------- LoadMyRoster ------------------------------- */
    public class OnLoadMyRoster
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string StaffName { get; set; }
        public string StaffID { get; set; }
        public List<MyEvent> oEvents { get; set; }
        public OnLoadMyRoster()
        {
            StaffName = "";
            StaffID = "";
            oEvents = new List<MyEvent>();
        }
    }
    public class MyEvent
    {
        public long id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public bool allDay { get; set; }
        public string className { get; set; }
        public string date { get; set; }
        public string rosterMarker { get; set; }
        public string staffid { get; set; }
        public string In { get; set; }
        public string Out { get; set; }

    }
    public class OnLoadMyProduction
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string StaffName { get; set; }
        public string StaffID { get; set; }
        public weekProd week1 { get; set; }
        public List<MyEvent_Prod> oEvents { get; set; }
        public OnLoadMyProduction()
        {
            StaffName = "";
            StaffID = "";
            week1 = new weekProd();
            oEvents = new List<MyEvent_Prod>();
        }
    }
    public class MyEvent_Prod
    {
        public long id { get; set; }
        public long m_id { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string date { get; set; }
        public bool allDay { get; set; }
        public string className { get; set; }
        public string process { get; set; }
        public int target { get; set; }
        public int achived { get; set; }
        public int samples { get; set; }
        public int error { get; set; }
        public int score { get; set; }
        public int expired { get; set; }
        public MyEvent_Prod()
        {
            process = "";
            target = 0;
            achived = 0;
            samples = 0;
            error = 0;
            score = 0;
            expired = 1;
        }
    }
    public class weekProd
    {
        public int target { get; set; }
        public int achived { get; set; }
        public int samples { get; set; }
        public int error { get; set; }
        public int score { get; set; }
        public weekProd()
        {
            target = 0;
            achived = 0;
            samples = 0;
            error = 0;
            score = 0;
        }
    }
    /* ----------------------------------- LoadMyRoster END ------------------------------- */
    public class FixedArrayResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<string> sarTitles { get; set; }
        public List<string> sarRolls { get; set; }
        public List<string> sarTeams { get; set; }
        public List<string> sarBases { get; set; }
        public List<string> sarBands { get; set; }
        public List<string> sarGrades { get; set; }
        public List<PayscalDropdownItem> sarPayscales { get; set; }
        public List<Bank> sarBanks { get; set; }
        public FixedArrayResponse()
        {
            sarTitles = new List<string>();
            sarRolls = new List<string>();
            sarTeams = new List<string>();
            sarBases = new List<string>();
            sarBands = new List<string>();
            sarGrades = new List<string>();
            sarPayscales = new List<PayscalDropdownItem>();
            sarBanks = new List<Bank>();
        }
    }
    /* ---------------------------------Leave Response -------------------------------------------------*/

    public class LoadLeaveDataResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string StaffName { get; set; }
        public string Mrs { get; set; }
        public List<LeaveItem> LeaveStatus { get; set; } = new List<LeaveItem>();
        public Leaves leaves { get; set; } = new Leaves();
        //public List<Leave> leaves { get; set; } = new List<Leave>();
    }
    public class LeaveItem
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public int Status { get; set; }
    }
    public class Leaves
    {
        public Leave CL { get; set; } = new Leave();
        public Leave SL { get; set; } = new Leave();
        public Leave PL { get; set; } = new Leave();
        public Leave APL { get; set; } = new Leave();
        //public Leave COff { get; set; } = new Leave();
        //public Leave AWOff { get; set; } = new Leave();
        public Leave LOP { get; set; } = new Leave();
        public Leave ALOP { get; set; } = new Leave();
        public Leave MatL { get; set; } = new Leave();
        public Leave PatL { get; set; } = new Leave();
    }
    public class Leave
    {
        public double used { get; set; }
        public double pending { get; set; }
        public double sumCr { get; set; }
        public double sumDr { get; set; }
        public Leave()
        {
            used = 0;
            pending = 0;
            sumCr = 0;
            sumDr = 0;
        }
    }
    /* ---------------------------------Leave Response -------------------------------------------------*/
    /* ---------------------------------Team Mnagement -------------------------------------------------*/
    public class ManageBandsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string selected { get; set; }
        public string selectedsub { get; set; }
        public List<string> bands { get; set; } = new List<string>();
        public List<string> grades { get; set; } = new List<string>();
        public List<string> payscales { get; set; } = new List<string>();
    }
    public class ManageTeamsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string mode { get; set; }
        public string m_TeamHead { get; set; }
        public string m_LockTime { get; set; }
        public string m_PayIndex { get; set; }
        public string m_PhysicalPresence { get; set; }
        public string m_ShiftStartTime { get; set; }
        public string m_ShiftEndTime { get; set; }
        public string selected { get; set; }
        public string m_Description { get; set; }
        public string m_IFSC { get; set; }
        public string m_Branch { get; set; }
        public string m_Type { get; set; }
        public string m_Positions { get; set; }
        public string m_HolidayGroup { get; set; }
        public string m_Lat { get; set; }
        public string m_Lng { get; set; }
        public string m_Accuracy { get; set; }
        public List<string> landmarksAll { get; set; } = new List<string>();
        public List<string> landmarks { get; set; } = new List<string>();
        public List<string> teams { get; set; } = new List<string>();
    }
    /* ---------------------------------Chat Messages -------------------------------------------------*/
    public class LoadChatResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string selectedSession { get; set; }
        public string SenderDetails { get; set; }
        public bool bReload { get; set; }
        public List<Message> messages { get; set; } = new List<Message>();
        public LoadChatResponse()
        {
            bReload = false;
        }
    }
    public class LoadMessagesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string selectedSession { get; set; }
        public int selectedLeaveStatus { get; set; }
        public int selectedOTStatus { get; set; }
        public string SenderDetails { get; set; }
        public List<ListItem> listItems { get; set; } = new List<ListItem>();
        public List<Message> messages { get; set; } = new List<Message>();
    }
    public class ListItem
    {
        public int IsAdmin { get; set; }
        public string NameFrom { get; set; }
        public string NameTo { get; set; }
        public string staffidFrom { get; set; }
        public string staffidTo { get; set; }
        public string statusLine { get; set; }
        public Int32 tmCreated { get; set; }
        public Int32 tmUpdated { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string session { get; set; }
        public int counts { get; set; }
        public string time { get; set; }
        public int leavestatus { get; set; }
        public int Priority { get; set; }
        public string LeaveCode { get; set; }
        public int otStatus { get; set; }
        public Int32 mins { get; set; }
        public string param1 { get; set; }
        public string param2 { get; set; }
        public string param3 { get; set; }

        public ListItem()
        {
            NameFrom = "";
            NameTo = "";
            staffidFrom = "";
            staffidTo = "";
            statusLine = "";
            EmailFrom = "";
            EmailTo = "";
            session = "";
            leavestatus = 0;
            LeaveCode = "";
            counts = 0;
            time = "";
            otStatus = 0;
            tmCreated = 0;
            tmUpdated = 0;
            mins = 0;
        }
    }
    public class Message
    {
        public Int16 MySelf { get; set; }
        public string sMessage { get; set; }
        public string sTime { get; set; }
        public string image { get; set; }
        public string By { get; set; } // email
    }
    //-----------------------------------------------------------
    public class ShiftDetailsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<Roster_Shift> roster_Shifts { get; set; } = new List<Roster_Shift>();
    }
    public class Roster_Shift
    {
        public string sRoster { get; set; }
        public string sShift { get; set; }
        public string sTmeShift { get; set; }
        public string sTmeShiftBefore { get; set; }
        public string sTmeShiftAfter { get; set; }
        public string sTmeShiftWork { get; set; }
        public long lWorktime { get; set; }
        public long lShiftStartTime { get; set; }
        public long lShiftEndTime { get; set; }
        public string sApplicable { get; set; }
        public long lApplicable { get; set; }
        public int otStatus { get; set; }
        public string session { get; set; }
        public string otDuration { get; set; }
        public Roster_Shift()
        {
            session = "";
        }
    }
    //--------------------------------------------------
    public class ShiftActivityResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<DisplayedColumns_Roster_Consolidated_Row> rows { get; set; }
        //public List<ActivityItem> activities { get; set; } = new List<ActivityItem>();
        public ShiftActivityResponse()
        {
            rows = new List<DisplayedColumns_Roster_Consolidated_Row>();
        }
    }
    public class ActivityItem
    {
        public string sHardwareID { get; set; }
        public string sActivity { get; set; }
        public Int64 lActivityTime { get; set; }
        public Int64 lWorkTime { get; set; }
        public string ReasonHead { get; set; }
        public string ReasonNote { get; set; }
        public string sIP { get; set; }
        public ActivityItem()
        {
            ReasonHead = "";
            ReasonNote = "";
        }
    }
    public class LoadOTResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<Int32> dayActivities { get; set; } = new List<Int32>();
    }
    public class GetLeaveHistoryResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<LeaveHistoryRow> rows { get; set; } = new List<LeaveHistoryRow>();
    }
    public class LeaveHistoryRow
    {
        public string time { get; set; }
        public string dt { get; set; }
        public string description { get; set; }
        public int status { get; set; }
        public double pending { get; set; }
        public double used { get; set; }
        public double credit { get; set; }
    }
    //-----------------------------------------------
    public class ReportingToResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<ReportingToItem> names { get; set; } = new List<ReportingToItem>();
    }
    public class ReportingToItem
    {
        public string Name { get; set; }
        public string Roll { get; set; }
        public string Base { get; set; }
        public string Email { get; set; }
    }
    //------------------------------------------------
    public class MessageResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int retLeaves { get; set; }
        public int retTimes { get; set; }
    }
    public class MyAccessDash
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int trips_completed { get; set; }
        public int trips_open { get; set; }
        public int trips_amount { get; set; }
        public int taxies_online { get; set; }
        public int terminals_online { get; set; }
        public int staffs_online { get; set; }
        public int users_active { get; set; }

        public List<RosterStat> rosterStats { get; set; } = new List<RosterStat>();
        public MyAccessDash()
        {
            status = false;
            result = "";
            trips_completed = 0;
            trips_open = 0;
            trips_amount = 0;
            taxies_online = 0;
            terminals_online = 0;
            staffs_online = 0;
            users_active = 0;
        }
    }
    public class RosterStat
    {
        public string Name { get; set; }
        public int Expected { get; set; }
        public int Arrived { get; set; }
        public int m_WorkTime { get; set; }
        public int PreShift { get; set; }
        public int PostShift { get; set; }
        public int ShiftStart { get; set; }
        public int ShiftEnd { get; set; }
        public RosterStat()
        {
            Name = "";
            Expected = 0;
            Arrived = 0;
            m_WorkTime = 0;
            PreShift = 0;
            PostShift = 0;
            ShiftStart = 0;
            ShiftEnd = 0;
        }

    }
    public class RosterShiftCombos
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string shiftMessage { get; set; }
        public Int16 shifts { get; set; }
        public List<string> sarRosters { get; set; }
        public List<string> sarShifts { get; set; }
        public List<BreakItem> sarStaffs { get; set; }
        public RosterShiftCombos()
        {
            sarRosters = new List<string>();
            sarShifts = new List<string>();
            sarStaffs = new List<BreakItem>();
            shiftMessage = "";
            shifts = 0;
        }
    }
    public class LoadStaffResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string team { get; set; }
        public string selected { get; set; }
        public List<string> sarTeams { get; set; }
        public List<BreakItem> sarStaffs { get; set; }
        public LoadStaffResponse()
        {
            sarStaffs = new List<BreakItem>();
            sarTeams = new List<string>();
        }
    }
    public class HRProductionResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string remarks { get; set; }
        public string m_StaffID { get; set; }
        public string m_Name { get; set; }
        public string monthStr { get; set; }
        public int monthTarget { get; set; }
        public int monthAchived { get; set; }
        public int monthSamples { get; set; }
        public int monthError { get; set; }
        public int monthScore { get; set; }

        public List<HRProductionRow> rows { get; set; }
        public HRProductionResponse()
        {
            rows = new List<HRProductionRow>();
        }
    }
    public class HRProductionRow
    {
        public string m_StaffID { get; set; }
        public string m_Name { get; set; }
        public string m_DOJ { get; set; }
        public string m_Date { get; set; }
        public string m_Day { get; set; }
        public string m_Month { get; set; }
        public string m_Year { get; set; }
        public string m_Process { get; set; }
        public int m_Target { get; set; }
        public int m_Confirmed { get; set; }
        public string m_ConfirmedBy { get; set; }
        public string m_ConfirmedTime { get; set; }
        public int m_ConfirmedLoaded { get; set; }
        public int m_Achived { get; set; }
        public int m_Samples { get; set; }
        public int m_Error { get; set; }
        public int m_Score { get; set; }
    }
    public class HRAttendanceResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        //public string remarks1 { get; set; }
        //public string remarks2 { get; set; }
        public string[] summary { get; set; }
        public string listType { get; set; }
        public string AttendanceMethod { get; set; }
        public string AttendanceSource { get; set; }
        public int per_attendance { get; set; }
        public int per_production { get; set; }
        public int per_roster { get; set; }
        public int per_edit { get; set; }
        public string team { get; set; }
        public string staffidsearch { get; set; }
        public string staffidsearchName { get; set; }
        public ApprovedItems approved { get; set; }
        public List<string> sarTeams { get; set; }
        public List<BreakItem> sarStaffs { get; set; }
        public List<HRAttendanceRow> rows { get; set; }
        public HRAttendanceResponse()
        {
            per_edit = 0;
            team = "";
            staffidsearch = "";
            staffidsearchName = "";
            listType = "";
            AttendanceSource = "";
            sarTeams = new List<string>();
            sarStaffs = new List<BreakItem>();
            rows = new List<HRAttendanceRow>();
            approved = new ApprovedItems();
            summary = new string[6] { "", "", "", "", "", "" };
            per_attendance = 0;
            per_production = 0;
            per_roster = 0;
            //remarks1 = "";
            //remarks2 = "";
        }
    }
    public class ApprovedItems
    {
        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedByTime1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedByTime2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string m_ApprovedByTime3 { get; set; }
        public string m_ApprovedBy4 { get; set; }
        public string m_ApprovedByTime4 { get; set; }

        public double m_WorkingDays { get; set; }
        public double m_OFFs { get; set; }
        public double m_Leaves { get; set; }
        public double m_ALOPs { get; set; }
        public double m_LOPs { get; set; }
        public double m_LateSeconds { get; set; }
        public double m_LOPBasedOnDelay { get; set; }
        public double m_ActualWorkingDays { get; set; }
        public double m_DaysToBePaidTotal { get; set; }
        public string m_RosterOptions { get; set; }
        public string m_RosterOptionsResult { get; set; }
        public ApprovedItems()
        {
            m_ApprovedBy1 = "";
            m_ApprovedByTime1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedByTime2 = "";
            m_ApprovedBy3 = "";
            m_ApprovedByTime3 = "";
            m_ApprovedBy4 = "";
            m_ApprovedByTime4 = "";

            m_WorkingDays = 0;
            m_OFFs = 0;
            m_Leaves = 0;
            m_ALOPs = 0;
            m_LOPs = 0;
            m_LateSeconds = 0;
            m_LOPBasedOnDelay = 0;
            m_ActualWorkingDays = 0;
            m_DaysToBePaidTotal = 0;
            m_RosterOptions = "";
            m_RosterOptionsResult = "";
        }
    }
    public class HRAttendanceRow
    {
        public long m_id { get; set; }
        public string m_StaffID { get; set; }
        public long m_Date { get; set; }
        public int m_Year { get; set; }
        public int m_Month { get; set; }
        public string m_RosterName { get; set; }
        public string m_ShiftName { get; set; }
        public long m_ShiftStart { get; set; }
        public long m_ShiftEnd { get; set; }
        public long m_ActualStart { get; set; }
        public long m_ActualEnd { get; set; }
        public long lWorkhours { get; set; }
        public string sShortage { get; set; }
        public long logindelay { get; set; }
        public long workspan { get; set; }
        public string m_MarkRoster { get; set; }
        public string m_MarkLeave { get; set; }
        public long m_WorkApproved { get; set; }
        public double dblDayTobePaid { get; set; }
        public Int32 m_AsOn { get; set; }
        public Int16 m_LateLoginStatus { get; set; }
        public string Working { get; set; }
        public string m_Source { get; set; }
        public Int16 m_Mode { get; set; }
        public string payscale { get; set; }
        public Int32 key { get; set; }
        public Int32 startdate { get; set; }
        public HRAttendanceRow()
        {
            m_ActualStart = 0;
            m_ActualEnd = 0;
            lWorkhours = 0;
            sShortage = "";
            logindelay = 0;
            m_Date = 0;
            m_MarkRoster = "";
            m_MarkLeave = "";
            m_WorkApproved = 0;
            dblDayTobePaid = 0;
            m_AsOn = 0;
            m_LateLoginStatus = 0;
            m_Mode = 0;
            m_Source = "";
        }
    }
    public class HRActivitiesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int16 total_count { get; set; }
        public long worktime { get; set; }
        public long shift_start { get; set; }
        public long shift_end { get; set; }
        public Int16 mode { get; set; }
        public long lLateSeconds { get; set; }
        public long lWorkTotal { get; set; }
        public long lBreakTotal { get; set; }
        public long lActualStart { get; set; }
        public long lActualEnd { get; set; }
        public string m_Day { get; set; }
        public string m_StaffName { get; set; }
        public string m_StaffID { get; set; }
        public List<DisplayedColumns_Roster_Consolidated_Row> rows { get; set; }
        //public List<BioActivityRow> bioRows { get; set; }
        public HRActivitiesResponse()
        {
            mode = 0;
            total_count = 0;
            lWorkTotal = 0;
            lBreakTotal = 0;
            lActualStart = 0;
            lActualEnd = 0;
            m_Day = "";
            rows = new List<DisplayedColumns_Roster_Consolidated_Row>();
            //bioRows = new List<BioActivityRow>();
        }
    }
    public class DisplayedColumns_Roster_Consolidated_Row
    {
        public Int32 m_id { get; set; }
        public string m_RosterName { get; set; }
        public string m_ShiftName { get; set; }
        public long m_StaffsE { get; set; }
        public long m_StaffsA { get; set; }
        public long worktime { get; set; }
        public long shift_start { get; set; }
        public long shift_end { get; set; }
        public string m_StaffID { get; set; }
        public string m_StaffName { get; set; }
        //---------------------------------------Break report
        public string sHardwareID { get; set; }
        public string sActivity { get; set; }
        public long lActivityTime { get; set; }
        //public long lWorkTime { get; set; }
        public string sIP { get; set; }
        public string sTimeSpan { get; set; }
        public long lWorkTime { get; set; }
        public long lBreakTime { get; set; }
        public string ReasonHead { get; set; }
        public string ReasonNote { get; set; }
        public string m_Day { get; set; }
        public string sPreNote { get; set; }
        public string sPostNote { get; set; }
        public string m_RosterMarker { get; set; }
        public string m_Remarks { get; set; }
        public string m_Notes { get; set; }
        public DisplayedColumns_Roster_Consolidated_Row()
        {
            m_id = 0;
            m_RosterName = "";
            m_ShiftName = "";
            m_StaffsE = 0;
            m_StaffsA = 0;
            worktime = 0;
            shift_start = 0;
            shift_end = 0;
            m_StaffID = "";
            m_StaffName = "";
            sTimeSpan = "";
            sHardwareID = "";
            sActivity = "";
            lActivityTime = 0;
            lWorkTime = 0;
            sIP = "";
            ReasonHead = "";
            ReasonNote = "";
            m_Day = "";
            m_Notes = "";
            lBreakTime = 0;
            sPreNote = "";
            sPostNote = "";
            m_RosterMarker = "";
            m_Remarks = "";
        }
    }
    /*
    public class BioActivityRow
    {
        public Int32 m_id { get; set; }
        public string m_RosterName { get; set; }
        public string m_ShiftName { get; set; }
       
        
        //--------------------------------------
        public string m_HardwareID { get; set; } // MachineSlNo
        public string m_StaffName { get; set; }
        public string m_StaffID { get; set; }
        public string m_Activity { get; set; }   //  Finger/Card/Password
        public Int32 m_ActivityTime { get; set; }
        public Int32 m_ShiftStartTime { get; set; }
        public Int32 m_ShiftEndTime { get; set; }
        public string m_RosterMarker { get; set; }
        public string m_Remarks { get; set; }

        public BioActivityRow()
        {
            m_id = 0;
            m_HardwareID = "";
            m_StaffName = "";
            m_Activity = "";
            m_ActivityTime = 0;
            m_ShiftStartTime = 0;
            m_ShiftEndTime = 0;
            m_ShiftEndTime = 0;
            m_RosterMarker = "";
            m_Remarks = "";
            m_RosterName = "";
            m_ShiftName = "";
            m_StaffID = "";
            m_StaffName = "";
        }
    }
    */
    public class StaffShiftsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string staffID { get; set; }
        public List<StaffShiftsRow> rows { get; set; }
        public StaffShiftsResponse()
        {
            staffID = "";
            rows = new List<StaffShiftsRow>();
        }
    }
    public class StaffShiftsRow
    {
        public string id { get; set; }
        public string name { get; set; }
        public string m_Team { get; set; }
        public string email { get; set; }
        public string roster { get; set; }
        public string shift { get; set; }
        public string day1 { get; set; }
        public string day2 { get; set; }
        public string day3 { get; set; }
        public string day4 { get; set; }
        public string day5 { get; set; }
        public long lShiftStart { get; set; }
        public long lShiftEnd { get; set; }

        public StaffShiftsRow()
        {
            id = "";
            name = "";
            email = "";
            roster = "";
            shift = "";
            day5 = "";
            day4 = "";
            day3 = "";
            day2 = "";
            day1 = "";
            lShiftStart = 0;
            lShiftEnd = 0;
        }
    }
    //------------------------------------------
    public class PayslipMaster
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string name { get; set; }
        public string CTC { get; set; }
        public long m_id { get; set; }
        public int allowdelete { get; set; }
        public List<PayslipRow> rows_rate_earn { get; set; }
        public List<PayslipRow> rows_rate_earn_o { get; set; }
        public List<PayslipRow> rows_rate_deduct { get; set; }
        public List<PayslipRow> rows_rate_deduct_o { get; set; }
        public List<PayslipRow> rows_earn { get; set; }
        public List<PayslipRow> rows_deduct { get; set; }
        public List<PaySlipLedger> ledgers { get; set; }
        public PayslipMaster()
        {
            rows_rate_earn = new List<PayslipRow>();
            rows_rate_earn_o = new List<PayslipRow>();
            rows_rate_deduct = new List<PayslipRow>();
            rows_rate_deduct_o = new List<PayslipRow>();
            rows_earn = new List<PayslipRow>();
            rows_deduct = new List<PayslipRow>();
            ledgers = new List<PaySlipLedger>();
        }
    }
    public class PaySlipLedger
    {
        public string name { get; set; }
        public Int16 paymode { get; set; }
        public PaySlipLedger()
        {
            name = "";
            paymode = 0;
        }
    }
    public class PayslipRow
    {
        public long m_id { get; set; }
        public PaySlipLedger ledger { get; set; }
        public string basedon { get; set; }
        public string amount { get; set; }
        public PayslipRow()
        {
            m_id = 0;
            ledger = new PaySlipLedger();
            basedon = "";
            amount = "";
        }
    }
    public class LoadPayslip
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int original { get; set; }
        public string profile { get; set; }
        public string email { get; set; }
        public string m_PayscaleName { get; set; }
        public Int32 m_PayscaleKey { get; set; }
        public Int32 m_PayscaleStartDate { get; set; }
        public string staffid { get; set; }
        public string name { get; set; }
        public string band { get; set; }
        public string grade { get; set; }
        public string team { get; set; }
        public string designation { get; set; }
        public string epf_uan { get; set; }
        public string sb_acc { get; set; }
        public string CTC { get; set; }
        public double m_WorkingDays { get; set; }
        public double m_OFFs { get; set; }
        public double m_Leaves { get; set; }
        public double m_ALOPs { get; set; }
        public double m_LOPs { get; set; }
        public double m_LateSeconds { get; set; }
        public double m_LopBasedOnDelay { get; set; }
        public double m_ActualWorkingDays { get; set; }
        public double m_DaysToBePaidTotal { get; set; }
        public string m_RosterOptions { get; set; }
        public string m_RosterOptionsResult { get; set; }
        public int iYear { get; set; }
        public int iMonth { get; set; }
        public string sMonth { get; set; }
        public Int32 m_DateStart { get; set; }
        public Int32 m_DateEnd { get; set; }

        public string m_Bank { get; set; }
        public string m_Branch { get; set; }

        public double m_CrTot { get; set; }
        public double m_DrTot { get; set; }
        public double m_EarnsTot { get; set; }
        public double m_DeductsTot { get; set; }

        public string ledName { get; set; }   // additional ledgers
        public string ledType { get; set; }  // additional ledgers
        public string ledAmount { get; set; }  // additional ledgers

        public double m_GrossSalary { get; set; }
        public double m_BasicPay { get; set; }
        public double m_EPFContributionRemitted { get; set; }
        public double m_ESIC { get; set; }
        public double m_ProfessionalTax { get; set; }

        public int PageNo { get; set; }
        public int Pages { get; set; }
        public int m_VchNo { get; set; }
        

        public List<PayLedger> ratesCr { get; set; }
        public List<PayLedger> deductsDr { get; set; }
        public List<PayLedger> earns { get; set; }
        public List<PayLedger> deducts { get; set; }

        public string m_CreatedBy { get; set; }
        public string m_CreatedTime { get; set; }
        public string NetPayWords { get; set; }
        public LoadPayslip()
        {
            profile = "";
            email = "";
            original = 0;
            m_PayscaleName = "";
            m_PayscaleKey = 0;
            team = "";
            m_Bank = "";
            m_Branch = "";


            ratesCr = new List<PayLedger>();
            deductsDr = new List<PayLedger>();
            earns = new List<PayLedger>();
            deducts = new List<PayLedger>();

            m_WorkingDays = 0;
            m_OFFs = 0;
            m_Leaves = 0;
            m_ALOPs = 0;
            m_LOPs = 0;
            m_LateSeconds = 0;
            m_LopBasedOnDelay = 0;
            m_ActualWorkingDays = 0;
            m_DaysToBePaidTotal = 0;
            m_RosterOptions = "";
            m_RosterOptionsResult = "";


            m_DateStart = 0;
            m_DateEnd = 0;

            m_CrTot = 0;
            m_DrTot = 0;
            m_EarnsTot = 0;
            m_DeductsTot = 0;
            NetPayWords = "";
        }
    }

    public class PayLedger
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; }
        public int paymode { get; set; }
        public int m_Security { get; set; }
    }
    //----------------------------------
    public class PayslipSettlementPart
    {
        public Int32 m_DateStart { get; set; }
        public Int32 m_DateEnd { get; set; }

        public string m_PayscaleName { get; set; }
        public Int32 m_PayscaleKey { get; set; }
        public Int32 m_PayscaleStartDate { get; set; }

        public double m_WorkingDays { get; set; }
        public double m_OFFs { get; set; }
        public double m_Leaves { get; set; }
        public double m_ALOPs { get; set; }
        public double m_LOPs { get; set; }
        public double m_LateSeconds { get; set; }
        public double m_LOPBasedOnDelay { get; set; }
        public double m_ActualWorkingDays { get; set; }
        public double m_DaysToBePaidTotal { get; set; }
        public string m_RosterOptions { get; set; }
        public string m_RosterOptionsResult { get; set; }
        

        public PayslipSettlementPart()
        {
            m_PayscaleName = "";
            m_PayscaleKey = 0;
            m_PayscaleStartDate = 0;
            m_DateStart = 0;
            m_DateEnd = 0;
            m_WorkingDays = 0;
            m_OFFs = 0;
            m_Leaves = 0;
            m_ALOPs = 0;
            m_LOPs = 0;
            m_LateSeconds = 0;
            m_LOPBasedOnDelay = 0;
            m_ActualWorkingDays = 0;
            m_DaysToBePaidTotal = 0;
            m_RosterOptions = "";
            m_RosterOptionsResult = "";
            
        }
    }
    public class PayslipSettlement
    {
        public bool status { get; set; }
        public string result { get; set; }

        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedByTime1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedByTime2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string m_ApprovedByTime3 { get; set; }
        public string m_ApprovedBy4 { get; set; }
        public string m_ApprovedByTime4 { get; set; }

        //public PayslipSettlementPart part1 { get; set; }
        //public PayslipSettlementPart part2 { get; set; }
        public List<PayslipSettlementPart> parts { get; set; }


        public string ExistsInList { get; set; }

        public string m_FundsReleaseDate { get; set; }
        public List<PayLedger> addLedgers { get; set; }  // additional ledgers

        public int BonusMonths { get; set; }
        public double dblBonusFunds { get; set; }
        public double[] m_BonusTable { get; set; }
        public int[] m_BonusTableRelease { get; set; }
        public int m_BonusTableReleaseVoucher { get; set; }
        
        public PayslipSettlement()
        {
            m_ApprovedBy1 = "";
            m_ApprovedByTime1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedByTime2 = "";
            m_ApprovedBy3 = "";
            m_ApprovedByTime3 = "";
            m_ApprovedBy4 = "";
            m_ApprovedByTime4 = "";
            parts = new List<PayslipSettlementPart>();
            //part1 = new PayslipSettlementPart();
            //part2 = new PayslipSettlementPart();
            addLedgers = new List<PayLedger>();
            m_BonusTable = new double[12];
            m_BonusTableRelease = new int[12];
            ExistsInList = "";
            m_BonusTableReleaseVoucher=0;
            BonusMonths = 0;
            dblBonusFunds = 0;
            
        }
    }
    //-------------------------
    public class StaffSearchResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<StaffDetail> staffs { get; set; }
        public StaffSearchResponse()
        {
            staffs = new List<StaffDetail>();
        }
    }
    public class StaffDetail
    {
        public string Name { get; set; }
        public string StaffID { get; set; }
        public string m_Band { get; set; }
        public string m_Grade { get; set; }
        public string m_Team { get; set; }
        public string m_Base { get; set; }
        public string m_Designation { get; set; }
    }
    public class GetLedgersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<LedgerSearchItem> ledgers { get; set; }
        public GetLedgersResponse()
        {
            ledgers = new List<LedgerSearchItem>();
        }
    }
    public class LoadAccountsLedgersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public int page_size { get; set; }
        public List<LedgerItem> rows { get; set; }
        public LoadAccountsLedgersResponse()
        {
            rows = new List<LedgerItem>();
        }
    }
    public class LedgerItem
    {
        public Int32 m_id { get; set; }
        public double Amount { get; set; }
        public string Ledger { get; set; }
        public Int32 Time { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }
    public class LedgerSearchItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string StaffID { get; set; }
    }
    public class LoadAccountsDetailsResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public string ledger { get; set; }
        public string name { get; set; }
        public double CrTot { get; set; }
        public double DrTot { get; set; }
        public int page_size { get; set; }
        public List<DetailItem> rows { get; set; }
        public LoadAccountsDetailsResponse()
        {
            rows = new List<DetailItem>();
        }
    }
    public class DetailItem
    {
        public Int32 m_id { get; set; }
        public Int32 Time { get; set; }
        public string Head { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public double Dr { get; set; }
        public double Cr { get; set; }
        public bool rev { get; set; }
    }
    //-------------------------------
    public class StatementResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        //public Int32 dtBank { get; set; }
        public string list { get; set; }
        public string ApprovedBankReturn { get; set; }
        public Int16 total_count { get; set; }
        public double dblAmountSelected { get; set; }
        public List<string> sarTeams { get; set; }
        public List<string> sarBanks { get; set; }
        public List<StatementRow> rows { get; set; }
        public StatementResponse()
        {
            ApprovedBankReturn = "";
            list = "";
            sarTeams = new List<string>();
            sarBanks = new List<string>();
            rows = new List<StatementRow>();
        }
    }
    public class StatementRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public string staffid { get; set; }
        public double rate { get; set; }
        public double earns { get; set; }
        public double deducts { get; set; }
        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string m_ApprovedBy4 { get; set; }
        public string team { get; set; }
        public bool m_Selected { get; set; }
        public string m_Bank { get; set; }
        public string m_List { get; set; }
        public string m_sb_acc { get; set; }
        public string m_Email { get; set; }
        public StatementRow()
        {
            m_ApprovedBy1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedBy3 = "";
            m_ApprovedBy4 = "";
            m_List = "";
            m_Bank = "";
            m_Email = "";
        }
    }
    public class StatementAccountsExcelResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }
        public string bank { get; set; }
        public string list { get; set; }
        public string txtBankDate { get; set; }
        
        public List<Statement_Accounts_ExcelRow> rows { get; set; }
        public StatementAccountsExcelResponse()
        {
            rows = new List<Statement_Accounts_ExcelRow>();
        }
    }
    //-------------------------------
    public class Statement_Accounts_ExcelRow
    {
        //public Int32 m_id { get; set; }
        public string NAME { get; set; }
        public string StaffID { get; set; }
        public string SB_Acc { get; set; }
        public string Amount { get; set; }
    }
    public class StatementPFExcelResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }
        
        public List<Statement_PF_ExcelRow> rows { get; set; }
        public StatementPFExcelResponse()
        {
            rows = new List<Statement_PF_ExcelRow>();
        }
    }
    public class StatementPTExcelResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }

        public List<Statement_PT_ExcelRow> rows { get; set; }
        public StatementPTExcelResponse()
        {
            rows = new List<Statement_PT_ExcelRow>();
        }
    }
    //-------------------------------
    public class Statement_PF_ExcelRow
    {
        public string Name { get; set; }
        public string StaffID { get; set; }
        public string UAN { get; set; }
        public double GROSS_WAGES { get; set; }
        public double EPF_WAGES { get; set; }
        public double EPS_WAGES { get; set; }
        public double EDLI_WAGES { get; set; }
        public double EPF_CONTRI_REMITTED { get; set; }
        public double EPS_CONTRI_REMITTED { get; set; }
        public double EPF_EPS_DIFF_REMITTED { get; set; }
        public double NCP_DAYS { get; set; }
        public double REFUND_OF_ADVANCES { get; set; }
    }
    //-------------------------------
    public class Statement_PT_ExcelRow
    {
        public string Name { get; set; }
        public string StaffID { get; set; }
        public double m_GrossWages { get; set; }
        public double m_ProfessionalTax { get; set; }
    }
    public class StatementESICExcelResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }

        public List<Statement_ESIC_ExcelRow> rows { get; set; }
        public StatementESICExcelResponse()
        {
            rows = new List<Statement_ESIC_ExcelRow>();
        }
    }
    //-------------------------------
    public class Statement_ESIC_ExcelRow
    {
        public string Name { get; set; }
        public string StaffID { get; set; }
        public string IPNumber { get; set; }
        public double Paydays { get; set; }
        public double Total_Monthly_Wages { get; set; }
        public double ESIC { get; set; }
    }
    public class StatementRetentionExcelResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string ret_filename { get; set; }

        public List<Statement_RETENTION_ExcelRow> rows { get; set; }
        public StatementRetentionExcelResponse()
        {
            rows = new List<Statement_RETENTION_ExcelRow>();
        }
    }
    //-------------------------------
    public class Statement_RETENTION_ExcelRow
    {
        public string StaffID { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        //public string CCTNo { get; set; }
        //public string CCTCleardDate { get; set; }
        //public string RetentionBonusEffectiveDate { get; set; }
        //public double DaysTobePaidTotal { get; set; }

        public string m_Base { get; set; }
        public string m_Bank { get; set; }
        public string m_Branch { get; set; }
        public string m_IFSC { get; set; }
        public string m_AccountNo { get; set; }

        public double ActualWorkingDays { get; set; }
        public double Amount { get; set; }


    }
    //-------------------------------
    public class Statement_PFResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int32 dtBank { get; set; }
        public string list { get; set; }
        public Int16 total_count { get; set; }
        public double dblAmountSelected { get; set; }
        public List<string> sarTeams { get; set; }
        public List<string> sarBanks { get; set; }
        public List<Statement_PFRow> rows { get; set; }
        public Statement_PFResponse()
        {
            list = "";
            sarTeams = new List<string>();
            sarBanks = new List<string>();
            rows = new List<Statement_PFRow>();
        }
    }
    public class Statement_PFRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public string staffid { get; set; }
        public double rate { get; set; }
        public double earns { get; set; }
        public double deducts { get; set; }
        public string m_ApprovedBy1 { get; set; }
        public string m_ApprovedBy2 { get; set; }
        public string m_ApprovedBy3 { get; set; }
        public string team { get; set; }
        public bool m_Selected { get; set; }
        public string m_Bank { get; set; }
        public string m_List { get; set; }
        public string m_sb_acc { get; set; }

        public string m_UAN { get; set; }
        public double m_GrossWages { get; set; }
        public double m_BasicPay { get; set; }
        public double m_EPFWages { get; set; }
        public double m_EPSWages { get; set; }
        public double m_ELDIWages { get; set; }
        public double m_EPFContributionRemitted { get; set; }
        public double m_EPSContributionRemitted { get; set; }
        public double m_EPFEPSDifferenceRemitted { get; set; }
        public double m_NCPDays { get; set; }
        public double m_RefundOfAdvances { get; set; }
        public double m_ESIC { get; set; }

        public Statement_PFRow()
        {
            m_ApprovedBy1 = "";
            m_ApprovedBy2 = "";
            m_ApprovedBy3 = "";
            m_List = "";
            m_Bank = "";
        }
    }
    //-------------------------------
    public class StatementResponse_ProfessionalTax
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string list { get; set; }
        public Int32 dtBank { get; set; }
        public Int16 total_count { get; set; }
        public double dblAmountSelected { get; set; }
        public List<Statement_ProfessionalTaxRow> rows { get; set; }
        public StatementResponse_ProfessionalTax()
        {
            list = "";
            rows = new List<Statement_ProfessionalTaxRow>();
        }
    }
    public class Statement_ProfessionalTaxRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public string staffid { get; set; }
        public double m_GrossWages { get; set; }
        public double m_ProfessionalTax { get; set; }
        public bool m_Selected { get; set; }
        public string m_List { get; set; }
        public Statement_ProfessionalTaxRow()
        {

        }
    }
    public class Statement_ESICResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int32 dtBank { get; set; }
        public string list { get; set; }
        public Int16 total_count { get; set; }
        public double dblAmountSelected { get; set; }
        public List<string> sarTeams { get; set; }
        public List<string> sarBanks { get; set; }
        public List<Statement_ESICRow> rows { get; set; }
        public Statement_ESICResponse()
        {
            list = "";
            sarTeams = new List<string>();
            sarBanks = new List<string>();
            rows = new List<Statement_ESICRow>();
        }
    }
    public class Statement_ESICRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public string staffid { get; set; }
        public double rate { get; set; }
        public double earns { get; set; }
        public double deducts { get; set; }

        public string team { get; set; }
        public bool m_Selected { get; set; }
        public string m_Bank { get; set; }
        public string m_List { get; set; }
        public string m_sb_acc { get; set; }

        public string m_ESICNumber { get; set; }
        public double m_GrossWages { get; set; }
        public double m_BasicPay { get; set; }
        public double m_Paydays { get; set; }
        public double m_ESIC { get; set; }
        

    }
    public class Statement_PTResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int32 dtBank { get; set; }
        public string list { get; set; }
        public Int16 total_count { get; set; }
        public double dblAmountSelected { get; set; }
        //public List<string> sarTeams { get; set; }
        //public List<string> sarBanks { get; set; }
        public List<Statement_PTRow> rows { get; set; }
        public Statement_PTResponse()
        {
            list = "";
            //sarTeams = new List<string>();
            //sarBanks = new List<string>();
            rows = new List<Statement_PTRow>();
        }
    }
    public class Statement_PTRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public string staffid { get; set; }
        public bool m_Selected { get; set; }
        public string m_Bank { get; set; }
        public string m_List { get; set; }
        public double m_GrossWages { get; set; }
        public double m_ProfessionalTax { get; set; }
    }
    public class StatementToPDF
    {
        public bool status { get; set; }
        public string result { get; set; }

        public string txtBankName { get; set; }
        public string txtBankBranch { get; set; }
        public string txtListNo { get; set; }
        public string txtBankDate { get; set; }

        public List<string> cols { get; set; }
        public List<object> rows { get; set; }
        public StatementToPDF()
        {
            cols = new List<string>();
            rows = new List<object>();
        }
    }
    public class StatementToPDFRow
    {
        public List<string> cols { get; set; }
        public StatementToPDFRow()
        {
            cols = new List<string>();
        }
    }
    //-------------------------------
    public class PayscaleLedgersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }

        public List<PayLedger> sarLedgers { get; set; }
        public PayscaleLedgersResponse()
        {
            sarLedgers = new List<PayLedger>();
        }
    }
    public class PayscalDropdownItem
    {
        public string m_Name { get; set; }
        public double m_Amount { get; set; }
        public Int32 m_Key { get; set; }
        public Int32 m_StartDate { get; set; }
        public PayscalDropdownItem()
        {
            m_Name = "";
            m_Amount = 0;
            m_Key = 0;
            m_StartDate = 0;
        }
    }
    public class Bank
    {
        public string m_Name { get; set; }
        public string m_AccountNo { get; set; }
        public string m_Branch { get; set; }
        public string m_IFSC { get; set; }
        public Bank()
        {
            m_Name = "";
            m_AccountNo = "";
            m_Branch = "";
            m_IFSC = "";
        }
    }
    public class HoliGroup
    {
        public List<string> names { get; set; }
        public HoliGroup()
        {
            names = new List<string>();
        }
    }
    public class HolidayResponse
    {
        public bool status { get; set; }
        public string result { get; set; }

        public bool Updated { get; set; }
        public string StaffName { get; set; }
        public string StaffBase { get; set; }

        public List<HoliGroup> groups { get; set; }
        public List<HolidayCell> days { get; set; }
        public List<HolidayMonthRow> months { get; set; }

        public List<HolidayFixed> available_holidays_0_Chennai { get; set; }
        public List<HolidayFixed> available_holidays_1_Chennai { get; set; }
        public List<HolidayFixed> available_holidays_2_Chennai { get; set; }
        public List<HolidayFixed> available_holidays_3_Chennai { get; set; }
        public List<HolidayFixed> available_holidays_0_Delhi { get; set; }
        public List<HolidayFixed> available_holidays_1_Delhi { get; set; }
        public List<HolidayFixed> available_holidays_2_Delhi { get; set; }
        public List<HolidayFixed> available_holidays_3_Delhi { get; set; }

        public HolidayResponse()
        {
            groups = new List<HoliGroup>();
            days = new List<HolidayCell>();
            months = new List<HolidayMonthRow>();
            available_holidays_0_Chennai = new List<HolidayFixed>();
            available_holidays_1_Chennai = new List<HolidayFixed>();
            available_holidays_2_Chennai = new List<HolidayFixed>();
            available_holidays_3_Chennai = new List<HolidayFixed>();
            available_holidays_0_Delhi = new List<HolidayFixed>();
            available_holidays_1_Delhi = new List<HolidayFixed>();
            available_holidays_2_Delhi = new List<HolidayFixed>();
            available_holidays_3_Delhi = new List<HolidayFixed>();
        }
    }
    public class HolidayFixed
    {
        public string value { get; set; }
        public string desc { get; set; }

        public HolidayFixed()
        {
        }
    }
    public class HolidayMonthRow
    {
        public int month { get; set; }
        public List<HolidayCell> days { get; set; }
        public HolidayMonthRow()
        {
            days = new List<HolidayCell>();
        }
    }
    public class HolidayCell
    {
        public string name { get; set; }
        public int month { get; set; }
        public int date { get; set; }
        public int type { get; set; }
        public string positions { get; set; }
        public string desc { get; set; }
        public string group { get; set; }
    }
    public class PayscalesAssignedResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<PayscalesAssignedRow> rows { get; set; }
        public PayscalesAssignedResponse()
        {
            rows = new List<PayscalesAssignedRow>();
        }
    }
    public class PayscalesAssignedRow
    {
        public Int32 m_id { get; set; }
        public string name { get; set; }
        public Int32 effectivedate { get; set; }
        public Int32 startdate { get; set; }
        public int allowdelete { get; set; }
        public string ctc { get; set; }
        public string createdby { get; set; }
        public string createdtime { get; set; }
    }
    public class PRODuction
    {
        public string processName1 { get; set; }
        public int processTarget1 { get; set; }
        public int processAchived1 { get; set; }
        public string processName2 { get; set; }
        public int processTarget2 { get; set; }
        public int processAchived2 { get; set; }
        public PRODuction()
        {
            processName1 = "";
            processTarget1 = 0;
            processAchived1 = 0;
            processName2 = "";
            processTarget2 = 0;
            processAchived2 = 0;
        }
    }
    //------------------------------------------------------------
    public class ClassicRosterResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<string> sarDayHeaders { get; set; }
        public List<string> sarRosterOptions { get; set; }
        public List<RosterClassicRow> rows { get; set; }
        public ClassicRosterResponse()
        {
            sarRosterOptions = new List<string>();
            sarDayHeaders = new List<string>();
            rows = new List<RosterClassicRow>();
        }
    }
    public class RosterClassicRow
    {
        public string RosterName { get; set; }
        public string ShiftName { get; set; }
        public Int32 ShiftStart { get; set; }
        public Int32 ShiftEnd { get; set; }
        public List<RosterClassicCell> cells { get; set; }
        public RosterClassicRow()
        {
            ShiftStart = 0;
            ShiftEnd = 0;
            RosterName = "";
            ShiftName = "";
            cells = new List<RosterClassicCell>();
        }
    }
    public class RosterClassicCell
    {
        public int Day { get; set; }
        public int expired { get; set; }
        public List<RosterClassicCellRow> cellRows { get; set; }
        public RosterClassicCell()
        {
            expired = 1;
            Day = 0;
            cellRows = new List<RosterClassicCellRow>();
        }
    }
    public class RosterClassicCellRow
    {
        public string Name { get; set; }
        public string StaffID { get; set; }
        public string RosterOption { get; set; }
        public string Leave { get; set; }
        public Int16 LeaveStatus { get; set; }
        public long WorkHrs { get; set; }
        public RosterClassicCellRow()
        {
            WorkHrs = 0;
            Name = "";
            StaffID = "";
            RosterOption="";
            Leave = "";
            LeaveStatus = 0;
        }
    }
    //------------------------------------------------------------
    public class VoucherDetails
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string vchno { get; set; }
        public string staffid { get; set; }
        public string payto { get; set; }
        public string paytoname { get; set; }
        public string payfrom { get; set; }
        public double amount { get; set; }
        public string notes { get; set; }
        public List<string> sarLedgers { get; set; }
        public VoucherDetails()
        {
            sarLedgers = new List<string>();
        }
    }
    public class LoadPermissionData
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string sParam1 { get; set; }
        public List<TeamPermissions> sarTeamsPermissions { get; set; }
        public LoadPermissionData()
        {
            sarTeamsPermissions = new List<TeamPermissions>();
        }
    }
    public class TeamPermissions
    {
        public string Name { get; set; }
        public int state { get; set; }
        
    }
    public class QATableResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string staffID { get; set; }
        public string staffName { get; set; }
        public string staffIDQA { get; set; }
        public string staffNameQA { get; set; }
        public string total_count { get; set; }
        public List<QATableItem> items { get; set; }
        public QATableResponse()
        {
            items = new List<QATableItem>();
        }
    }
    public class QATableItem
    {
        public Int32 m_id { get; set; }
        public int m_QASlNo { get; set; }
        public int m_QAScore { get; set; }
        public string m_QAInitials { get; set; }
        public string m_QATime { get; set; }
        public string m_QAComments { get; set; }
        public string m_QATriggerType { get; set; }
        public string m_QAHR { get; set; }
        public string m_QAStripPosting { get; set; }
        public string m_QAStripCutting { get; set; }
        public string m_QAFindings { get; set; }
        public string m_QAMissedMDN { get; set; }
        public int m_QAFreeze { get; set; }
        public string m_QASavedBy { get; set; }
        public string m_QASavedTime { get; set; }
    }
    //-------------------------------------------
    public class ThisDayResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int32 m_ShiftStart { get; set; }
        public Int32 m_ShiftEnd { get; set; }
        public Int32 m_ActualStart { get; set; }
        public Int32 m_ActualEnd { get; set; }
        public string m_MarkRoster { get; set; }
        public string m_MarkLeave { get; set; }
        public ThisDayResponse()
        {
            m_ShiftStart = 0;
        }
    }
    //-------------------------------------------
    public class BiodeviceResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public bool reload { get; set; }
        public List<BiodeviceRow> items { get; set; }
        public BiodeviceResponse()
        {
            items = new List<BiodeviceRow>();
        }
    }
    public class BiodeviceRow
    {
        public long m_id { get; set; }
        public string m_MachineSlNo { get; set; }
        public string m_Make { get; set; }
        public string m_Model { get; set; }
        public string m_CreatedBy { get; set; }
        public string m_CreatedTime { get; set; }
    }
    //-------------------------------------------
    public class ProfileInfo
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Mobile1 { get; set; }
        public string Mobile2 { get; set; }
        public string Email { get; set; }
        public string StaffIDPrefix { get; set; }
        public string StaffIDLength { get; set; }
        public int AttnStartDate { get; set; }

        public ProfileInfo()
        {
            Name = "";
            Address1 = "";
            Address2 = "";
            Email = "";
            Mobile1 = "";
            Mobile2 = "";
            StaffIDPrefix = "";
            StaffIDLength = "3";
            AttnStartDate = 1;
        }
    }
    //-------------------------------------------
    public class MobileAccessLocation
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string Name { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public int m_Accuracy { get; set; }
        public MobileAccessLocation()
        {
            Name = "";
            m_Lat = 0;
            m_Lng = 0;
            m_Accuracy = 0;
        }
    }
    //-------------------------------------------
    public class RetentionBonusModel
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int16 total_count { get; set; }
        public string level { get; set; }
        public string list { get; set; }
        public double dblAmountSelected { get; set; }
        public List<RetentionBonusItem> items { get; set; }
        public RetentionBonusModel()
        {
            dblAmountSelected = 0;
            list = "";
            level = "";
            items = new List<RetentionBonusItem>();
        }
    }
    public class RetentionBonusItem
    {
        public Int32 m_id { get; set; }
        public string m_StaffID { get; set; }
        public string m_Name { get; set; }
        public Int32 dtBank { get; set; }
        public double m_ActualWorkingDays { get; set; }
        public double m_DaysTobePaidTotal { get; set; }
        public string m_Team { get; set; }
        public string m_CCTNo { get; set; }
        public string m_CCTCleardDate { get; set; }
        public string m_RetentionBonusEffectiveDate { get; set; }
        public double m_RetentionBonusAmount { get; set; }
        public bool m_SelectedHR { get; set; }
        public bool m_SelectedAccounts { get; set; }
        public string title { get; set; }
        public string m_FreezedOn { get; set; }
        public string m_FreezedBy { get; set; }
        
        public string m_ProcessedBy { get; set; }
        public string m_ProcessedOn { get; set; }
        public string m_ApprovalHR_by { get; set; }
        public string m_ApprovalHR_date { get; set; }
        public string m_ApprovalAccounts_by { get; set; }
        public string m_ApprovalAccounts_date { get; set; }

        public RetentionBonusItem()
        {
        }
    }
}