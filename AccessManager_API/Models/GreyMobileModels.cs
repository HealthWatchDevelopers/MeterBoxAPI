using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace MyHub.Models
{
    public class SignalRObj
    {
        public string comm { get; set; }
    }
   
    public class MobileReqResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string profile { get; set; }
        public string mode { get; set; }
        public string m_StaffName { get; set; }
        public string email { get; set; }
        public string staffid { get; set; }
        public int regstatus { get; set; }  //  registered status
        public int signedin { get; set; }   //  SignedIn status
        public string roster { get; set; }
        public string shift { get; set; }
        
        public Int32 shiftstart { get; set; }
        public Int32 shiftend { get; set; }
        public Int32 actualstart { get; set; }
        public Int32 actualend { get; set; }
        public string rosteroption { get; set; }
        public string yesterday { get; set; }
        public string m_Team { get; set; }
        
        public List<Landmark> landmarks { get; set; } = new List<Landmark>();

        public MobileReqResponse()
        {
            status = false;
            profile = "";
            m_StaffName = "";
            email = "";
            staffid = "";
            result = "";
            regstatus = 0;
            roster = "";
            shift = "";
            shiftstart = 0;
            shiftend = 0;
            actualstart = 0;
            actualend = 0;
            rosteroption = "";
            yesterday = "";
            signedin = 0;

        }
    }
    public class Landmark
    {
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int Accuracy { get; set; }
    }
    //------------------------------------------------------------
    public class ShiftActivitiesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string yesterday { get; set; }
        public string roster { get; set; }
        public string shift { get; set; }
        public Int32 DayStart { get; set; }
        public Int32 shiftstart { get; set; }
        public Int32 shiftend { get; set; }
        public Int32 actualstart { get; set; }
        public Int32 actualend { get; set; }
        public int signedin { get; set; }
        public string rosteroption { get; set; }
        
        public List<ShiftActivity> activities { get; set; }
        public ShiftActivitiesResponse()
        {
            status = false;
            result = "";
            yesterday = "";
            DayStart = 0;
            activities = new List<ShiftActivity>();
        }
    }
    public class ShiftActivity
    {
        public string m_Activity { get; set; }
        public Int32 m_ActivityTime { get; set; }
        public string m_ReasonHead { get; set; }
        public string m_Notes { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
    }
    //------------------------------------------------------------
}