using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyHub.Models;

namespace MyHub.Models
{
    public class ParcelInit
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string usertype { get; set; }
        public int userStatus { get; set; }
        public string username { get; set; }
        //public string companyname { get; set; }
        public NewUser newUser { get; set; } = new NewUser();
        public List<LocationInfo> picklist { get; set; }
        public List<PickList> picklists { get; set; }
        public ParcelInit()
        {
            userStatus = 0;
        }
    }
    public class LocationInfo
    {
        public string name { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
    }
    public class OnParcelPicklistInit
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string usertype { get; set; }
        public List<LocationInfo> picklist { get; set; }
    }
    public class Input
    {
        public string Pickup { get; set; }
        public string Weight { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string AmPm { get; set; }
    }
    public class SlaveUsers
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<SlaveUser> slaveUser { get; set; }
        public SlaveUsers()
        {
            slaveUser = new List<SlaveUser>();
        }
    }
    public class SlaveUser
    {
        public long m_id { get; set; }
        public string imei { get; set; }
        public string name { get; set; }
        public string mobile { get; set; }
        public string type { get; set; }
    }
    public class PickList
    {
        public long m_id { get; set; }
        public string m_PickLocation { get; set; }
        public string m_PickTime { get; set; }
        public string m_PickWeight { get; set; }
        public string m_ActivityTime { get; set; }
        public string m_ActivityIMEI { get; set; }
        public string m_NameCompany { get; set; }
        public string m_ActivityBy { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public PickList()
        {
            m_NameCompany = "";
            m_ActivityBy = "";
            m_Lat = 0;
            m_Lng = 0;
        }
    }
    public class DailyCount
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<CountSet> countSet { get; set; }
    }
        
    public class CountSet
    {
        public string key { get; set; }
        public string value { get; set; }
    }
    public class UpdateStatus
    {
        public bool status { get; set; }
        public string result { get; set; }
    }
    //_____________________________________________
    public class LogisticClientProfilesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<LogisticsClientItem> items { get; set; }
        public LogisticClientProfilesResponse()
        {
            items = new List<LogisticsClientItem>();
        }
    }
    public class LogisticsClientItem
    {
        public int m_id { get; set; }
        public string m_Name { get; set; }
        public string m_NameCompany { get; set; }
        public string m_IMEI { get; set; }
        public string m_Type { get; set; }
        public string m_Mobile { get; set; }
    }
    //_________________________________________
    public class LoadActiveBookings
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<LoadActiveBookings_row> items { get; set; }
        public LoadActiveBookings()
        {
            //items = new List<LoadActiveBookings_row>();
        }
    }
    public class LoadActiveBookings_row
    {
        public Int16 m_id { get; set; }
        public string m_NameCompany { get; set; }
        public string m_NameUser { get; set; }
        public string not_picked { get; set; }
        public string picked { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public LoadActiveBookings_row()
        {
            m_Lat = 0;
            m_Lng = 0;
        }
    }
    public class LoadActiveBookingsDetails
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<LoadActiveBookingsDetails_row> items { get; set; }

    }
    public class LoadActiveBookingsDetails_row
    {
        public string m_id { get; set; }
        public string m_IMEI { get; set; }
        public string m_PickLocation { get; set; }
        public string m_PickTime { get; set; }
        public string m_PickWeight { get; set; }
        public string m_Activity { get; set; }
        public string m_ActivityTime { get; set; }
        public string m_ActivityBy { get; set; }
        public double m_Lat { get; set; }
        public double m_Lng { get; set; }
        public LoadActiveBookingsDetails_row()
        {
            m_Lat = 0;
            m_Lng = 0;
        }
    }
    //________________________________________
    public class BookingHistoryResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<BookingHistoryItem> items { get; set; }
        public BookingHistoryResponse()
        {
            items = new List<BookingHistoryItem>();
        }
    }
    public class BookingHistoryItem
    {
        public long m_id { get; set; }
        public string m_NameCompany { get; set; }
        public string m_NameUser { get; set; }
        public string m_PickTime { get; set; }
        public string m_PickWeight { get; set; }
        public string m_PickLocation { get; set; }
        public string m_ActivityIMEI { get; set; }
        public string m_ActivityTime { get; set; }
        public string m_WaybillNo { get; set; }
        public string m_Activity { get; set; }
        public BookingHistoryItem()
        {
            m_id = 0;
            m_NameCompany = "";
            m_NameUser = "";
            m_PickTime = "";
            m_PickWeight = "";
            m_PickLocation = "";
            m_ActivityIMEI = "";
            m_ActivityTime = "";
            m_WaybillNo = "";
            m_Activity = "";
        }
    }
    //___________________________
    public class NewUser
    {
        public string Name { get; set; }
        public string NameCompany { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PIN { get; set; }
        public string Mobile { get; set; }
        public string OTP { get; set; }
        public string Key { get; set; }
        public NewUser()
        {
            Name = "";
            NameCompany = "";
            Address = "";
            City = "";
            PIN = "";
            Mobile = "";
            OTP = "";
            Key = "";
        }
    }
}