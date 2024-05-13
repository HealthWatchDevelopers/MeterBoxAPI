using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Models
{
    public class onResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
    }
    public class ClientResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string email { get; set; }
        public string otp { get; set; }
        public int active { get; set; }
        public string name { get; set; }
    }

    public class MeterData
    {
        public string imei { get; set; }
        public string imeiclient { get; set; }
        public string jobid { get; set; }
        public string status { get; set; }
        public string tripno { get; set; }
        public string wait { get; set; }
        public string kms { get; set; }
        public string pay { get; set; }
    }
    public class ClassDispatchMessage
    {
        public string imeiClient { get; set; }
        public string src { get; set; }
        public string imeiDriver { get; set; }
        public LatLng pickloc { get; set; }
        public LatLng droploc { get; set; }
        public string pickadd { get; set; }
        public string dropadd { get; set; }
        public string TaxiType { get; set; }
        public string distance { get; set; }
        public string duration { get; set; }
        public string fare { get; set; }
        public string jobid { get; set; }
    }
    public class JobMessage
    {
        public string Mode { get; set; }
        public string JobID { get; set; }
        public string DriverIMEI { get; set; }
        public string StaffID { get; set; }
        public string DvrName { get; set; }
        public string RegNo { get; set; }
        public string TaxiType { get; set; }
        public string src { get; set; }
    }
    public class OnLoadResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string DriverID { get; set; }
        public string DriverName { get; set; }
        public string RegNo { get; set; }
        public string FleetID { get; set; }
        public MYTariff myTariff { get; set; }
        public List<string> tariffs { get; set; }
        public ActiveJob activeJob { get; set; }
        public OnLoadResponse()
        {
            status = false;
            result = "";
            DriverID = "";
            RegNo = "";
            FleetID = "";
            myTariff = new MYTariff();
            tariffs = new List<string>();
            activeJob = new ActiveJob();
        }
    }
    public class ActiveJob
    {
        public string imeiClient { get; set; }
        public string imeiDriver { get; set; }
        public string pickloclat { get; set; }
        public string pickloclng { get; set; }
        public string droploclat { get; set; }
        public string droploclng { get; set; }
        public string pickadd { get; set; }
        public string dropadd { get; set; }
        public string vehicletype { get; set; }
        public string distance { get; set; }
        public string duration { get; set; }
        public string fare { get; set; }
        public string jobid { get; set; }
        public string src { get; set; }
        public ActiveJob()
        {
            imeiClient = "";
            imeiDriver = "";
            pickloclat = "";
            pickloclng = "";
            droploclat = "";
            droploclng = "";
            pickadd = "";
            dropadd = "";
            vehicletype = "";
            distance = "";
            duration = "";
            fare = "";
            jobid = "";
            src = "";
        }
    }
    public class OnDriverInfoResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string dvrPhoto { get; set; }
        public string dvrName { get; set; }
        public string dvrDesignation { get; set; }
        public string staffID { get; set; }
        public string regNo { get; set; }
        public string taxiType { get; set; }
        public string country { get; set; }
        public string profile { get; set; }

        public OnDriverInfoResponse()
        {
            status = false;
            result = "";
            dvrPhoto = "";
            dvrName = "";
            dvrDesignation = "";
            staffID = "";
            regNo = "";
            taxiType = "";
            country = "";
            profile = "";
        }
    }

    public class MYTariff
    {
        public String m_Name;

        public String m_FlagFall;       //	10.00 (1000)AED
        public String m_DistanceFree;       //	4115 meters
        public String m_WaitingFree;        //	360 seconds

        public String m_DistanceSlab;       //	147 meters
        public String m_DistanceCharge; //	0.25 (25)AED

        public String m_WaitingSlab;        //	30 Seconds
        public String m_WaitingCharge;  //	0.25(25) AED

        public String m_WaitingSpeedLag;    // 0 Kms
        public String m_Surcharge;      //	2.50 (250)AED

        public MYTariff()
        {
            m_Name = "";
            m_FlagFall = "";
            m_DistanceFree = "";
            m_WaitingFree = "";
            m_DistanceSlab = "";
            m_DistanceCharge = "";
            m_WaitingSlab = "";
            m_WaitingCharge = "";
            m_WaitingSpeedLag = "";
            m_Surcharge = "";
        }
    }
    public class JobDecisionResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int responsemode { get; set; }
        public string mode { get; set; }
        public string jobid { get; set; }
        public string src { get; set; }
    }
    public class JobRequestResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public int waittime { get; set; }
        public long job_id { get; set; }
    }
    //_____________________________________________________________
    public class PickupResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<AboutAPickup> pickups = new List<AboutAPickup>();
    }
    public class AboutAPickup
    {
        public AboutAPickup(string idLoc, string typeLoc, LatLng latLngLoc, string labelLoc,
            string titleLoc)
        {
            id = idLoc;
            type = typeLoc;
            latLng = latLngLoc;
            label = labelLoc;
            title = titleLoc;
        }
        public string id { get; set; }
        public string type { get; set; }
        public LatLng latLng { get; set; }
        public string label { get; set; }
        public string title { get; set; }
    }
    //_____________________________________________________________
    public class VehicleAroundResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<CarGroup> carGroups = new List<CarGroup>();
    }
    public class CarGroup
    {
        public bool show { get; set; }
        public bool open { get; set; }
        public string name { get; set; }
        public int count { get; set; }
        public int countOn { get; set; }
        public int countOff { get; set; }
        public string information { get; set; }
        public string price { get; set; }
        public List<AboutACar> aboutACars = new List<AboutACar>();
        public List<CarGroup> children = new List<CarGroup>();
    }
    public class AboutACar
    {
        public AboutACar(string imeiLoc, string typeLoc, LatLng latLngLoc, long timeLoc, int headingLoc,
            string regnoLoc, string fleetidLoc, string driveridLoc, long lapseLoc,int speedLoc)
        {
            imei = imeiLoc;
            type = typeLoc;
            latLng = latLngLoc;
            time = timeLoc;
            heading = headingLoc;
            regno = regnoLoc;
            fleetid = fleetidLoc;
            driverid = driveridLoc;
            lapse = lapseLoc;
            speed = speedLoc;
        }
        public string imei { get; set; }
        public string type { get; set; }
        public LatLng latLng { get; set; }
        public long time { get; set; }
        public long lapse { get; set; }
        public int heading { get; set; }
        public string regno { get; set; }
        public string fleetid { get; set; }
        public string driverid { get; set; }
        public int speed { get; set; }
    }
    //_____________________________________________________________
    public class LatLng
    {
        public LatLng()
        {

        }
        public LatLng(double latLoc, double lngLoc)
        {
            lat = latLoc;
            lng = lngLoc;
        }
        public double lat { get; set; }
        public double lng { get; set; }
    }
    public class TariffResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string m_Name { get; set; }
        public int m_FlagFall { get; set; }
        public int m_DistanceFree { get; set; }
        public int m_WaitingFree { get; set; }
        public int m_DistanceSlab { get; set; }
        public int m_DistanceCharge { get; set; }
        public int m_WaitingSlab { get; set; }
        public int m_WaitingCharge { get; set; }
        public int m_WaitingSpeedLag { get; set; }
        public int m_Surcharge { get; set; }
    }
    public class OnLoadResponseToClient
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string JobID { get; set; }
        public string m_PickAddress { get; set; }
        public string m_DropAddress { get; set; }
        public string m_PickLat { get; set; }
        public string m_PickLng { get; set; }
        public string m_DropLat { get; set; }
        public string m_DropLng { get; set; }
        public string m_VehicleType { get; set; }
        public string m_AssignedTo { get; set; }
        public string m_AssignedToStaffID { get; set; }
        public string m_FName { get; set; }
        public string m_TaxiType { get; set; }
        public string m_RegNo { get; set; }
        public string m_ClientName { get; set; }
        public string m_ClientActive { get; set; }
        public string m_ClientEmail { get; set; }
        public OnLoadResponseToClient()
        {
            JobID = "";
            m_PickAddress = "";
            m_DropAddress = "";
            m_PickLat = "";
            m_PickLng = "";
            m_DropLat = "";
            m_DropLng = "";
            m_VehicleType = "";
            m_AssignedTo = "";
            m_AssignedToStaffID = "";
            m_FName = "";
            m_TaxiType = "";
            m_RegNo = "";
            m_ClientName = "";
            m_ClientActive = "";
            m_ClientEmail = "";

        }
    }
    //----------------------------------
    public class DeviceListResponse
    {
        public bool status { get; set; }
        public string result { get; set; }
        public List<CarGroup> groups = new List<CarGroup>();
    }
    /*
    public class DeviceGroup
    {
        public bool open { get; set; }
        public string name { get; set; }
        public string information { get; set; }
        public string price { get; set; }
        public int countOn { get; set; }
        public int countOff { get; set; }
        public List<AboutACar> aboutACars = new List<AboutACar>();
        public List<DeviceGroup> children = new List<DeviceGroup>();
    }
    */
    //----------------------------------
    public class InfoWindowData
    {
        public bool status { get; set; }
        public string result { get; set; }
        public string imei { get; set; }
        public string sRegNo { get; set; }
        public string sFleetID { get; set; }
        public string sDriverID { get; set; }
        public string sMobile { get; set; }
        public string sName { get; set; }
        public InfoWindowData()
        {
            imei = "";
            sRegNo = "";
            sFleetID = "";
            sDriverID = "";
            sMobile = "";
            sName = "";
        }
    }
}