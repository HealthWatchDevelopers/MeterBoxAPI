using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Models
{
    public class MyDash
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int trips_completed { get; set; }
        public int trips_open { get; set; }
        public int trips_amount { get; set; }
        public int taxies_online { get; set; }
        public int terminals_online { get; set; }
        public MyDash()
        {
            status = false;
            result = "";
            trips_completed = 0;
            trips_open = 0;
            trips_amount = 0;
            taxies_online = 0;
            terminals_online = 0;
        }
    }
    public class TripResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<TripItem> trips { get; set; }

        public TripResponse()
        {
            status = false;
            result = "";
            total_count = "";
            trips = new List<TripItem>();
        }
    }
    public class TripItem
    {
        public Int32 m_id;
        public String m_TripSequentialNumber;
        public String m_TripType;
        public String m_DriverID; 
        public String m_TimeReceived;
        public String m_AmountTotal;
        public String m_DistanceTotal;
        public String m_TripStartTime;
        public String m_TripEndTime;
        public String m_WaitingTime;
        public String m_DeviceIMEI;
        public String m_RegNo;
        public String m_FleetID;
        public String m_JobID;
        public String m_DriverName;
        public Int64 m_Stage1;
        public Int64 m_Stage2;
        public Int64 m_Stage3;
        public Int64 m_Stage4;
        public Int64 m_Stage5;

        public TripItem()
        {
            m_id = 0;
            m_TripSequentialNumber = "";
            m_TripType = "";
            m_DriverID = "";
            m_TimeReceived = "";
            m_AmountTotal = "";
            m_DistanceTotal = "";
            m_TripStartTime = "";
            m_TripEndTime = "";
            m_WaitingTime = "";
            m_DeviceIMEI = "";
            m_RegNo = "";
            m_JobID = "";
            m_DriverName = "";
            m_Stage1 = 0;
            m_Stage2 = 0;
            m_Stage3 = 0;
            m_Stage4 = 0;
            m_Stage5 = 0;
        }
    }
    /*
    public class MYTrip
    {
        public String m_TripSequentialNumber;
        public String m_TripType;
        public String m_TimeReceived;
        public String m_AmountTotal;
        public String m_DistanceTotal;
        public String m_TripStartTime;
        public String m_TripEndTime;
        public String m_WaitingTime;
        public String m_DeviceIMEI;
        public String m_JobID;
        public String m_StaffID;
        public String m_DriverName;

        public MYTrip()
        {
            m_TripSequentialNumber = "";
            m_TripType = "";
            m_TimeReceived = "";
            m_AmountTotal = "";
            m_DistanceTotal = "";
            m_TripStartTime = "";
            m_TripEndTime = "";
            m_WaitingTime = "";
            m_DeviceIMEI = "";
            m_JobID = "";
            m_StaffID = "";
            m_DriverName = "";
        }
    }
    */
    //________________________________
    public class OnProfileResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public SelectedProfile selectedProfile { get; set; }
        public List<ListProfile> profiles { get; set; }

        public OnProfileResponse()
        {
            status = false;
            result = "";
            selectedProfile = new SelectedProfile();
            profiles = new List<ListProfile>();
        }
    }
    public class ListProfile
    {
        public String m_id;
        public String m_Name;
        public String m_StaffID;
        public ListProfile()
        {
            m_id = "0";
            m_Name = "";
            m_StaffID = "";
        }
    }
    public class SelectedProfile
    {
        public String m_id;
        public String m_FName;
        public String m_AddressLocal;
        public String m_AddressHome;
        public String m_Country;
        public String m_StaffID;
        public String m_DeviceIMEI;
        public String m_RegNo;
        public String m_TaxiType;
        public SelectedProfile()
        {
            m_id = "0";
            m_FName = "";
            m_AddressLocal = "";
            m_AddressHome = "";
            m_Country = "";
            m_StaffID = "";
            m_DeviceIMEI = "";
            m_RegNo = "";
            m_TaxiType = "";
        }
    }
    public class LoginResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string birthdayResult { get; set; }
        public string m_Firstname { get; set; }
        public string m_Email { get; set; }
        public string m_Username { get; set; }
        public string m_Profile { get; set; }
        public string m_StaffID { get; set; }
        public string m_MenuKey { get; set; }
        public string m_CompName { get; set; }
        public int m_AttnStartDate { get; set; }
        public LoginResponse()
        {
            status = false;
            result = "";
            birthdayResult = "";
            m_Firstname = "";
            m_Email = "";
            m_Profile = "";
            m_Username = "";
            m_StaffID = "";
            m_MenuKey = "";
            m_CompName = "";
            m_AttnStartDate = 1;
        }
    }
    public class DownloadDetails
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string download_client_version { get; set; }
        public string download_client_time { get; set; }
        public string download_driver_version { get; set; }
        public string download_driver_time { get; set; }
        public string download_parcelbooking_version { get; set; }
        public string download_parcelbooking_time { get; set; }
        public string download_meterbox_version { get; set; }
        public string download_meterbox_time { get; set; }
        public DownloadDetails()
        {
            status = false;
            result = "";
            download_client_version = "";
            download_client_time = "";
            download_driver_version = "";
            download_driver_time = "";
            download_parcelbooking_version = "";
            download_parcelbooking_time = "";
            download_meterbox_version = "";
            download_meterbox_time = "";
        }
    }
    public class ProfileResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string m_id { get; set; }
        public string m_Username { get; set; }
        public string m_FirstName { get; set; }
        public string m_MiddleName { get; set; }
        public string m_LastName { get; set; }
        public string m_Status { get; set; }
        public string m_UserType { get; set; }
        public string m_Mobile { get; set; }
        public string m_Email { get; set; }
        public string m_Address { get; set; }
        public string m_City { get; set; }
        public string m_Country { get; set; }
        public string m_PIN { get; set; }
        public string m_AboutMe { get; set; }
        public string m_StaffID { get; set; }
        public string m_ReportToFunctional { get; set; }
        public string m_Base { get; set; }
        public string m_Team { get; set; }
    }
    //_________________________________________MobileUsersResponse
    public class MobileUsersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<MobileUser> items { get; set; }
        public MobileUsersResponse()
        {
            items = new List<MobileUser>();
        }
    }
    public class MobileUser
    {
        public long m_id { get; set; }
        public string m_IMEI { get; set; }
        public string m_Name { get; set; }
        public string m_Mobile { get; set; }
        public string m_CreatedTime { get; set; }
        public string m_Version { get; set; }
        public string m_Status { get; set; }
        public string m_LinkedProfile { get; set; }
        public MobileUser()
        {
            m_id = 0;
            m_IMEI = "";
            m_Name = "";
            m_Mobile = "";
            m_Version = "";
            m_Status = "";
            m_LinkedProfile = "";
        }
    }
    //__________________________________________Vehicle response
    public class VehiclesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<VehicleItem> items { get; set; }
        public VehiclesResponse()
        {
            items = new List<VehicleItem>();
        }
    }
    public class VehicleItem
    {
        public long m_id { get; set; }
        public string m_Make { get; set; }
        public string m_Model { get; set; }
        public string m_DOR { get; set; }
        public string m_DriverID1 { get; set; }
        public string m_FName { get; set; }
        public string m_Mobile { get; set; }
        public string m_DeviceIMEI { get; set; }
        public string m_SIMMobileNo { get; set; }
        public string m_Make_device { get; set; }
        public string m_Model_device { get; set; }
        public string m_RegNo { get; set; }
        public string m_FleetID { get; set; }
        public string m_Group { get; set; }
        public List<Group> groupList { get; set; }
        public VehicleItem()
        {
            m_id = 0;
            m_Make = "";
            m_Model = "";
            m_DOR = "";
            m_DriverID1 = "";
            m_FName = "";
            m_DeviceIMEI = "";
            m_SIMMobileNo = "";
            m_Make_device = "";
            m_Model_device = "";
            m_RegNo = "";
            m_FleetID = "";
            m_Group = "";
            groupList = new List<Group>();
        }
    }
    public class Group
    {
        public string value { get; set; }
        public string viewValue { get; set; }
        public Group(string value_,string viewValue_)
        {
            value = value_;
            viewValue = viewValue_;
        }
    }
    //__________________________________________LinkedProfileResponse
    public class LinkedProfileResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<LinkedProfile> items { get; set; }
        public LinkedProfileResponse()
        {
            items = new List<LinkedProfile>();
        }
    }
    public class LinkedProfile
    {
        public long m_id { get; set; }
        public string m_Email { get; set; }

        public LinkedProfile()
        {
            m_id = 0;
            m_Email = "";
        }
    }
    //__________________________________________Devices response
    public class DevicesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<DeviceItem> items { get; set; }
        public DevicesResponse()
        {
            items = new List<DeviceItem>();
        }
    }
    public class DeviceItem
    {
        public long m_id { get; set; }
        public string m_IMEI { get; set; }
        public string m_SIMMobileNo { get; set; }
        public string m_Time { get; set; }
        public string m_Make { get; set; }
        public string m_Model { get; set; }
        
        public DeviceItem()
        {
            m_id = 0;
            m_IMEI = "";
            m_SIMMobileNo = "";
            m_Time = "";
            m_Make = "";
            m_Model = "";
        }
    }
    //__________________________________________Vehicle response
    public class ProfilesResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<ProfileItem> items { get; set; }
        public ProfilesResponse()
        {
            items = new List<ProfileItem>();
        }
    }
    public class ProfileItem
    {
        public long m_id { get; set; }
        public string m_FName { get; set; }
        public string m_StaffID { get; set; }
        public string m_Country { get; set; }
        public string m_Mobile { get; set; }
        public ProfileItem()
        {
            m_id = 0;
            m_FName = "";
            m_StaffID = "";
            m_Country = "";
            m_Mobile = "";
        }
    }
    //__________________________________________Vehicle response
    public class DriversResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<DriverItem> items { get; set; }
        public DriversResponse()
        {
            items = new List<DriverItem>();
        }
    }
    public class DriverItem
    {
        public long m_id { get; set; }
        public string m_FName { get; set; }
        public string m_StaffID { get; set; }
        public DriverItem()
        {
            m_id = 0;
            m_FName = "";
            m_StaffID = "";
        }
    }
    //__________________________________________Vehicle response
    public class UsersResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string total_count { get; set; }
        public List<UserItem> items { get; set; }
        public UsersResponse()
        {
            items = new List<UserItem>();
        }
    }
    public class UserItem
    {
        public string m_id { get; set; }
        public string m_FirstName { get; set; }
        public string m_MiddleName { get; set; }
        public string m_LastName { get; set; }
        public string m_Status { get; set; }
        public string m_Email { get; set; }
        public string m_Mobile { get; set; }
        public string m_Address { get; set; }
        public string m_City { get; set; }
        public string m_Country { get; set; }
        public string m_Pin { get; set; }
        public string m_AboutMe { get; set; }
        public string m_Username { get; set; }
        public string m_MenuKey { get; set; }
        public UserItem()
        {
            m_id = "";
            m_FirstName = "";
            m_LastName = "";
            m_Status = "";
            m_Email = "";
            m_Mobile = "";
            m_Address = "";
            m_City = "";
            m_Country = "";
            m_Pin = "";
            m_AboutMe = "";
            m_Username = "";
            m_MenuKey = "";
        }
    }
    //_____________________________________
    
    public class AllowedListResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<string> groups { get; set; }
        public List<ListRow> rows { get; set; }
        public AllowedListResponse()
        {
            status = false;
            result = "";
            groups = new List<string>();
            rows = new List<ListRow>();
        }
    }
    public class ListRow
    {
        public string imei { get; set; }
        public string regno { get; set; }
        public string group { get; set; }
        public bool check { get; set; }
        public ListRow()
        {
            imei = "";
            regno = "";
            check = false;
        }
    }
    //_____________________________________
    public class PostResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public Int32 iParam1 { get; set; }
        public double dblParam1 { get; set; }
        public string sParam1 { get; set; }
        public PostResponse()
        {
            status = false;
            result = "";
            iParam1 = 0;
            dblParam1 = 0;
            sParam1 = "";
        }
    }
    public class GroupResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string m_Name { get; set; }
        public string m_Description { get; set; }
        public MYGroup myGroup { get; set; }
        public List<string> groups { get; set; }
        public GroupResponse()
        {
            status = false;
            result = "";
            myGroup = new MYGroup();
            groups = new List<string>();
       }
    }
    public class MYGroup
    {
        public String m_Name;

        public String m_Description;       //	10.00 (1000)AED

        public MYGroup()
        {
            m_Name = "";
            m_Description = "";
        }
    }
}