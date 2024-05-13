using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Net;
using System.Net.Mime;
using MyHub.Controllers;

namespace MyHub.Hubs
{
    public partial class ChatHub : Hub
    {
        public void FromDoom(string sIMEI, string sJSONData)
        {
            if (sJSONData.Length < 3) return;// Just safty
            char sReturnKey = 'X';
            sReturnKey = sJSONData[1];
            MessageToDebugger("FromDoom-->" + sReturnKey + "___" + sIMEI + "___" + sJSONData);
            if (sReturnKey == 'C')  // GPS Data
            {
                TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
                int unixTime = (int)span.TotalSeconds;
                MyLatest myLatest = new MyLatest();
                myLatest.iTime = unixTime;
                myLatest.sData = "";
                //int iLatestTime = unixTime-1180; // 3 minutes
                
                String sResponse = UpdateDatabase_C(unixTime, sIMEI, sJSONData, myLatest);
                if (sResponse.Length > 0)
                {
                    sResponse = "{c" + sResponse + "}"; // Note small 'c' is reply
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                }
                //_____________Do this last to make the clients refresh with latest data
                // Echo to all clients subscribed for this
                if (myLatest.sData.Length > 0)
                {
                    //PushFromDispatch(sIMEI, "{C" + myLatest.sData + "}");
                    ///SendToAllDevicesInThisProfile(sIMEI, "{C" + myLatest.sData + "}");
                    ///


                    SendToSubscribedList(sIMEI, "{C" + myLatest.sData + "}");
                    SendToAllBrowsersExceptBlock("{C" + myLatest.sData + "}");
                    
                }
                return;

            }
            else if (sReturnKey == 'B')    // Attendance data
            {
                String sResponse = UpdateDatabase_B(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                    //PushFromDispatch(sIMEI, "{B}"); // This is to reload the data on web
                }
                return;
            }
            else if (sReturnKey == 'G')    // Doom trips data
            {
                String sResponse = UpdateDatabase_G(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                    //PushFromDispatch(sIMEI, "{G}"); // This is to reload the data on web
                    SendToAllBrowsers("{G}");
                }
                return;
            }
            else if (sReturnKey == 'D')    // Job activities
            {
                String sResponse = UpdateDatabase_D(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                    //PushFromDispatch(sIMEI, sResponse);// "{D}"); // This is to reload the data on web
                }
                return;
            }
            else if (sReturnKey == 'E')    // NFC Swipe data
            {
                String sResponse = UpdateDatabase_E(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                    //PushFromDispatch(sIMEI, "k}"); // This is to reload the data on web
                }
                return;
            }
            else if (sReturnKey == 'H')    // Notes data
            {
                String sResponse = UpdateDatabase_H(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                    //PushFromDispatch(sIMEI, "{H}"); // This is to reload the data on web
                }
                return;
            }
            else if (sReturnKey == 'Z')    // Profile Info
            {
                String sResponse = UpdateDatabase_Z(sIMEI, sJSONData);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                }
                return;
            }
            else if (sReturnKey == 'Y')    // Doom client requesting for last trip details
            {
                String sResponse = GetLastTripDetails(sIMEI);
                if (sResponse.Length > 0)
                {
                    sResponse = "{" + sReturnKey + sResponse + "}";
                    AckBackToDoomDriver(sIMEI, sResponse);   // Push response back to the device
                }
                MessageToDebugger("FromDoom-LastTripDetailsRequested-Y-" + sIMEI);
                return;
            }
        }
        //_________________________________________________________
        public string AckBackToDoomDriver(string sIMEI, string sJSONData)
        {
            string sRet = "Unable to send";
            List<string> li = connObj_DoomDriver.GetList(sIMEI);
            if (li != null)
            {
                li.ForEach(delegate (String connectionid)
                {
                    Clients.Client(connectionid).broadcastMessage(sIMEI, sJSONData);
                    sRet = "";
                });
            }
            MessageToDebugger("AckBackToDoomDriver-" + sIMEI + "=" + sJSONData);
            return sRet;
        }
        public void SetSubscribeList(string sIMEIClient,List<string> arDriverIMEIList)
        {
            RemoveFromSubscribeList(sIMEIClient);
            arDriverIMEIList.ForEach((driverIMEI) =>
            {
                if (_subscriptions.ContainsKey(driverIMEI))
                {
                    List<string> li = _subscriptions[driverIMEI];
                    if (!li.Contains(sIMEIClient)) li.Add(sIMEIClient);
                }
                else
                {
                    List<string> li = new List<string>();
                    li.Add(sIMEIClient);
                    _subscriptions.Add(driverIMEI, li);
                }
            });
        }
        public void RemoveFromSubscribeList(string sIMEIClient)
        {
            foreach (var item in _subscriptions)
            {
                List<string> li = item.Value;
                if (li.Contains(sIMEIClient)) li.Remove(sIMEIClient);
            }
        }
        public void SetTaxiDataBlockList(string imei,int mode)
        {
            if (mode == 1)
            {
                if (!_TaxiDataBlockList.ContainsKey(imei)) // Add, if not exists
                {
                    _TaxiDataBlockList.Add(imei, mode);
                }
            }
            else
            {
                if (_TaxiDataBlockList.ContainsKey(imei))   // Remove, if exists
                {
                    _TaxiDataBlockList.Remove(imei);
                }
            }
        }
        /******************************************************************************************************/
        /*      0            1   2    3    4   5     6
         * {AIMEI(15)Time()^lat^lng^speed^acc^alt^heading^
         * DispatchData 351558073167217={C35155807316721776136^1508204704^13.0895799^80.2254123^114^0^0^-90^|76135^1508204699^13.0895799^80.2254123^102^0^0^-90^|76134^1508204694^13.0895799^80.2254123^96^0^0^-90^|76133^1508204689^13.0895799^80.2254123^86^0^0^-90^|76132^1508204685^13.0895799^80.2254123^117^0^0^-90^|76131^1508204677^13.0895799^80.2254123^75^0^0^-90^|76130^1508204673^13.0895799^80.2254123^85^0^0^-90^|76129^1508204669^13.0895799^80.2254123^140^0^0^-90^|76128^1508204667^13.089587^80.2253454^23.37^0^0^0^|76127^1508204659^13.0895799^80.2254123^84^0^0^-90^|76126^1508204655^13.0895799^80.2254123^136^0^0^-90^|76125^1508204652^13.089587^80.2253454^23.125^0^0^0^|76124^1508204644^13.0895799^80.2254123^100^0^0^-90^|76123^1508204640^13.0895799^80.2254123^140^0^0^-90^|76122^1508204631^13.0895799^80.2254123^146^0^0^-90^|76121^1508204626^13.0895799^80.2254123^147^0^0^-90^|76120^1508204619^13.0895799^80.2254123^73^0^0^-90^|76119^1508204615^13.0895799^80.2254123^113^0^0^-90^|76118^1508204610^13.0895799^80.2254123^134^0^0^-90^|76117^1508204605^13.0895799^80.2254123^124^0^0^-90^|}
         * PushToDispatch-351558073167217={C76136-76135-76134-76133-76132-76131-76130-76129-76128-76127-76126-76125-76124-76123-76122-76121-76120-76119-76118-76117-}
         */
        public String UpdateDatabase_C(Int32 unixTime, string sIMEI, string sJSONData, MyLatest myLatest)  // Tracking data from device
        {
            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "";

            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_C(unixTime, sIMEI, arData[i], sRes, myLatest);
                if (iStatus == 2) break;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            if (iStatus == 2)
            {
                MessageToDebugger("Unable to insert into tables IMEI=" + sIMEI);
                sReturn = "0-";
            }
            return sReturn;
        }
        /*
         * Profile info
         * DispatchData 351558073167217={Z351558073167217mark^^^^^^^^|}
         */
        public String UpdateDatabase_Z(string sIMEI, string sJSONData)  // Tracking data from device
        {
            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "";
            if (iPacketLength > 0)
            {
                char[] delimiterCharsSub = { '^' };
                string[] arDataSub = arData[0].Split(delimiterCharsSub);
                if (arDataSub.Length >= 5)
                {
                    MessageToDebugger("Trying to create table for " + sIMEI + " as profile " + arDataSub[0]);
                    if (arDataSub[0].Length > 0)
                    {
                        CreateNewDBEntries_Device(sIMEI, arDataSub[0]);
                        sReturn = "OK";
                    }
                }
            }
            return sReturn;
        }
        /*
DispatchData 358187078395418={B35818707839541823^1506504827^13.0895862^80.2253236^23.532^1506504827^^^^|22^1506503993^13.0895862^80.2253236^23.41^1506503993^^^^|21^1506503983^13.0895862^80.2253236^23.41^1506503993^^^^|20^1506503968^13.0895862^80.2253236^23.588^1506503973^^^^|19^1506503539^13.0895834^80.2253322^20^1506503539^^^^|18^1506503534^13.0895862^80.2253236^23.65^1506503534^^^^|17^1506503529^13.0895862^80.2253236^23.65^1506503534^^^^|16^1506503509^13.0895862^80.2253236^23.438^1506503514^^^^|15^1506503354^13.0895862^80.2253236^23.421^1506503354^^^^|14^1506503308^13.0895862^80.2253236^23.523^1506503308^^^^|13^1506502982^13.0895862^80.2253236^23.593^1506503303^^^^|12^1506502982^13.0895862^80.2253236^23.526^1506502982^^^^|11^1506502893^13.0895862^80.2253236^23.519^1506502908^^^^|10^1506502893^13.0895862^80.2253236^23.437^1506502893^^^^|9^1506502888^13.0895862^80.2253236^23.437^1506502893^^^^|8^1506502888^13.0895862^80.2253236^23.437^1506502888^^^^|7^1506502877^13.0895862^80.2253236^23.437^1506502888^^^^|6^1506502602^13.0895862^80.2253236^23.533^1506502877^^^^|5^1506502597^13.0895862^80.2253236^23.424^1506502602^^^^|4^1506502533^13.0895862^80.2253236^23.404^1506502572^^^^|}
         
         */
        public String UpdateDatabase_B(string sIMEI, string sJSONData)
        {
            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "";

            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_B(sIMEI, arData[i], sRes);
                if (iStatus == 2) continue;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            return sReturn;
        }
        /*
    {G3581870783954182^1510640549^^^^^^^^^^^^^|1^123^1510640549^^^^1510640549^^^^0^0^0^0^^|}

 */
        public String UpdateDatabase_G(string sIMEI, string sJSONData)
        {
            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "";
            String sProfile = GetProfileOfThisIMEI(sIMEI);
            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_G(sProfile, sIMEI, arData[i], sRes);
                if (iStatus == 2) continue;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            return sReturn;
        }
        private String GetNullOnNonInt(String sIn)
        {
            if (sIn.Length == 0) return "null";
            int i = 0;
            if (!Int32.TryParse(sIn, out i))
            {
                return "null";
            }
            return sIn;
        }
        private String GetHighOnNonInt(String sIn)
        {
            if (sIn.Length == 0) return "9999";
            int i = 0;
            if (!Int32.TryParse(sIn, out i))
            {
                return "9999";
            }
            return sIn;
        }
        private static Int32 GetInt32(String sIn)
        {
            Int32 i = 0;
            if (Int32.TryParse(sIn, out i))
            {
                return i;
            }
            return i;
        }
        public int UpdateDatabaseSub_C(Int32 unixTime, string sIMEI, string sJSONData, StringBuilder sRes, MyLatest myLatest)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length < 5) return 0;

            string sModel = "";
            string s_m_id = arData[0];
            Int32 iTime = GetInt32(arData[1]);
            string sLat = arData[2];
            string sLng = arData[3];
            string sAcc = arData[4];
            string sSpeed = arData[5];
            string sHeading = arData[6];

            string sAlt = "0";
            string sBits = "0";
            string sTripD = "0";
            string sFirmwareVersion = "";
            if (arData.Length >9)
            {
                sAlt = arData[7];
                sBits = arData[8];
                sTripD = arData[9];
            }
            if (arData.Length > 10) // Versionn string
            {
                // 01.000profilename, 01.000svs etc... 6+profilename
                if (arData[10].Length >= 6)
                {
                    sFirmwareVersion = arData[10].Substring(0,6);
                    String sProfileReceived= arData[10].Substring(6);
                    if (sProfileReceived.Length > 0)
                    {
                        GetProfileOfThisIMEI_UpdateIfNeeded(sIMEI, sProfileReceived);  // If profile is different, update

                    }
                }
            }
            string sReason = "1";
            string sGPS = "11";
            string sGSM = "27";
            string sVoltage = "0";
            string sAnalog = "0";
            //__________________________Get best value
            //if (iTime > myLatest.iTime)
            if (iTime > (unixTime - 180))
            {
                myLatest.iTime = iTime;
                myLatest.sData =
                    sIMEI + "^" + iTime + "^" + sLat + "^" + sLng + "^" + sAcc + "^" + sSpeed + "^" + sHeading + "^|";
            }
            else
            {
                //myLatest.sData = "I am here-iTime,myLatest.iTime" + iTime +","+ myLatest.iTime;
                myLatest.sData =
                    sIMEI + "^Lapsed^" + (unixTime - iTime) + "^|";
            }
            //_________________________________________
            sAcc = GetHighOnNonInt(sAcc);
            sHeading = GetNullOnNonInt(sHeading);
            string sSQL = "";
            try
            {

                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    int iRet = 0;
                    sSQL = "INSERT INTO dispatch_log.log_" + sIMEI + " (";
                    sSQL += @"m_id_device,m_Type,m_Lat,m_Lng,m_Speed,m_Alt,m_Time,m_TimeReceived,m_Accuracy,m_Heading,m_GUI,m_GSM,m_GPS,";
                    sSQL += @"m_Voltage,m_Reason,m_Bits,m_Firmware,m_Model,m_Analog,m_MeterStatus,m_id_trip) values (";
                    sSQL += "'" + s_m_id + "',";   //  Remote device ID's m_id
                    sSQL += "'" + "1" + "',";   //  Type
                    sSQL += "'" + sLat + "',"; //  Lat
                    sSQL += "'" + sLng + "',"; //  Lng
                    sSQL += "'" + sSpeed + "',"; //  Speed
                    sSQL += "'" + sAlt + "',"; //  Altitude
                    sSQL += "'" + iTime + "',";   //m_Time
                    sSQL += "'" + unixTime + "',";
                    sSQL += "" + sAcc + ","; //  Accuracy
                    sSQL += "" + sHeading + ","; //  Heading
                    sSQL += "'1',";
                    sSQL += "'" + sGSM + "',";  //  GSM
                    sSQL += "'" + sGPS + "',";  //  GPS
                    sSQL += "'" + "1" + "',";   // Voltage
                    sSQL += "'" + "1" + "',";   // Reason for trigger
                    sSQL += "" + sBits + ",";   // Bits
                    sSQL += "'" + sFirmwareVersion + "',";   //  sFirmwareVersion
                    sSQL += "'" + sModel + "',";  //  sModel
                    sSQL += "'" + "-1" + "',";  //iAnalog
                    sSQL += "'" + "0" + "',"; //iMeterStatus
                    sSQL += "'" + sTripD + "'"; //m_id_trip
                    sSQL += ");";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                    sSQL = "";
                    if ((sLat.Length < 4) || (sLng.Length < 4))
                    {
                        sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_Devices Set m_TimeReceived='" + unixTime + "',m_GPS='" + sGPS + "',m_GSM='" + sGSM + "',m_Accuracy=" + sAcc + ",m_Reason='" + sReason + "',m_Bits=" + sBits + ",m_Voltage='" + sVoltage + "' ";
                        if (sFirmwareVersion.Length > 0) sSQL += ",m_Firmware='" + sFirmwareVersion + "' ";
                        sSQL += "where m_IMEI='" + sIMEI + "';";
                    }
                    else
                    {
                        sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_Devices Set m_Lat='" + sLat + "',m_Lng='" + sLng + "',m_Time='" + iTime + "',m_TimeReceived='";
                        sSQL += unixTime + "',m_GPS='" + sGPS + "',m_GSM='" + sGSM + "',m_Accuracy=" + sAcc + ",m_Reason='" + sReason + "',m_Bits=" + sBits + ",m_Voltage='" + sVoltage + "' ";
                        if (sFirmwareVersion.Length > 0) sSQL += ",m_Firmware='" + sFirmwareVersion + "' ";
                        sSQL += "where m_IMEI='" + sIMEI + "';";
                    }
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        iRet=mySqlCommand.ExecuteNonQuery();
                    }
                    if (iRet == 0)
                    {
                        MessageToDebugger("Device table does not exists.");
                        return 2;   // DB Error
                    }
                    sRes.Append(s_m_id);
                    return 1;
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("UpdateDatabaseSub_C Exception - " + ex.Message);
                return 2;   // DB Error
            }

        }
        
        private void GetProfileOfThisIMEI_UpdateIfNeeded(String sIMEI,String sProfileReceived)
        {
            String sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bModifyProfile = false;
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!GetPure(reader, 0).Equals(sProfileReceived)) bModifyProfile=true;
                                }
                            }
                        }
                    }
                    if (bModifyProfile)
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_devices Set m_Profile='" + sProfileReceived + "' where m_IMEI='" + sIMEI + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_authorized Set m_Profile='" + sProfileReceived + "' where m_IMEI='" + sIMEI + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            
                        }
                        MessageToDebugger("GetProfileOfThisIMEI_UpdateIfNeeded Profile modified for " + sIMEI + " to " + sProfileReceived);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("GetProfileOfThisIMEI_UpdateIfNeeded Exception - " + ex.Message + "[" + sSQL + "]");
            }
        }
        private String GetProfileOfThisIMEI(String sIMEI)
        {
            String sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    return GetPure(reader, 0);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("UpdateDatabaseSub_D Exception - " + ex.Message + "[" + sSQL + "]");
            }
            return "";
        }
        //{D35155807316721711^4^Accept^1508078077202^^|10^4^Accept^1508078067241^^|9^4^Accept^1508078067241^^|8^4^Accept^1508078067241^^|7^4^Accept^1508078062151^^|6^4^Accept^1508078062151^^|5^3^Accept^1508078034000^^|4^3^Accept^1508078031131^^|3^3^Accept^1508078026112^^|2^3^Accept^1508078021101^^|1^3^Accept^1508078016042^^|}
        //11^4^Accept^1508078077202^^|
        public int UpdateDatabaseSub_D(string sIMEI, string sJSONData, StringBuilder sRes, String sFleetID, String sRegNo, String sDriverID)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length < 5) return 0;
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            int unixTime = (int)span.TotalSeconds;

            string s_m_id = arData[0];
            string s_m_id_job = arData[1];
            string sKeyString = arData[2];
            string sAccessTime = arData[3];
            string sParam = arData[4];

            if (sAccessTime.Length < 3) sAccessTime = "null"; else sAccessTime = "'" + sAccessTime + "'";

            string sKeyFiledName = "";
            if (sKeyString.Equals("Accept")) sKeyFiledName = "m_TimeAccept";
            else if (sKeyString.Equals("Reached")) sKeyFiledName = "m_TimeReach";
            else if (sKeyString.Equals("Started")) sKeyFiledName = "m_TimeStart";
            else if (sKeyString.Equals("Hospital")) sKeyFiledName = "m_TimeHospital";
            else if (sKeyString.Equals("End Job")) sKeyFiledName = "m_TimeClosed";
            else if (sKeyString.Equals("Deny")) sKeyFiledName = "m_Deny";
            else if (sKeyString.Equals("Received")) sKeyFiledName = "m_Received";
            else if (sKeyString.Equals("Assigned")) sKeyFiledName = "m_TimeAssgnedConfirmed";
            if( (sKeyFiledName.Length == 0)|| (s_m_id_job.Length == 0))
            {
                sRes.Append(s_m_id);
                return 1;
            }
            string sSQL = "", sProfile = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sProfile = GetPure(reader, 0);
                                }
                            }
                        }
                    }
                    //______________________________________
                    bool bRecordExists = false;
                    sSQL = "SELECT m_id from " + MyGlobal.activeDB + ".tbl_jobactivities where m_id_job='" +
                        s_m_id_job + "' and m_IMEI='" + sIMEI + "' and m_Profile='" + sProfile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            bRecordExists = reader.HasRows;
                            reader.Close();
                        }
                    }
                    if (bRecordExists)
                    {
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_jobactivities Set " + sKeyFiledName + "=" + sAccessTime + " where m_id_job='" + s_m_id_job + "' and m_Profile='" + sProfile + "'";
                    }
                    else
                    {
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_jobactivities (";
                        sSQL += "m_id_job,m_IMEI,m_FleetID,m_DriverID,m_Profile," + sKeyFiledName;
                        sSQL += ") values (";
                        sSQL += "'" + s_m_id_job + "',";   //  JobID
                        sSQL += "'" + sIMEI + "',";
                        sSQL += "'" + sFleetID + "',";
                        sSQL += "'" + sDriverID + "',";
                        sSQL += "'" + sProfile + "',";
                        sSQL += "" + sAccessTime + "";
                        sSQL += ");";
                    }
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                    if (sKeyString.Equals("Accept"))    // Try to assign the job
                    {
                        bool bJobAlreadAssigned = false;
                        sSQL = "SELECT m_IMEIAssigned from " + MyGlobal.activeDB + ".tbl_jobs_doom where "+
                            "m_id_seq='" +  s_m_id_job + "' and m_Profile='" + sProfile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (GetPure(reader, 0).Length > 5) bJobAlreadAssigned = true;
                                    }
                                }
                                reader.Close();
                            }
                        }
                        if (bJobAlreadAssigned)
                        {
                            sRes.Append("B" + s_m_id);
                        }
                        else
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_jobs_doom Set m_IMEIAssigned='" + sIMEI + "' where m_id_seq='" + s_m_id_job + "' and m_Profile='" + sProfile + "';";
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_jobactivities Set m_TimeAssigned='" + unixTime + "',m_Assigned='" + sIMEI + "' where m_id_job='" + s_m_id_job + "' and m_Profile='" + sProfile + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                            sRes.Append("A" + s_m_id);
                        }
                    }
                    else
                    {
                        sRes.Append(s_m_id);
                    }
                    PushFromDispatch(sIMEI, "{D" + sIMEI + "," + sKeyString + "," + "}");// Date to user web pages
                    return 1;
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("UpdateDatabaseSub_D Exception - " + ex.Message + "[" + sSQL + "]");
                return 2;   // DB Error
            }
        }

        public int UpdateDatabaseSub_E(string sIMEI, string sJSONData, StringBuilder sRes, String sFleetID, String sRegNo, String sDriverID)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length < 8) return 0;
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            int unixTime = (int)span.TotalSeconds;

            string s_m_id = arData[0];
            string s_m_id_job = arData[1];
            string sKeyString = arData[2];
            string sAccessTime = arData[3];
            string sParam = arData[4];
            string sLat = arData[5];
            string sLng = arData[6];
            string sAcc = arData[7];

            String sSwipeTime = UnixTimeToDateTime(sAccessTime);

            if (sAccessTime.Length < 3) sAccessTime = "null"; else sAccessTime = "'" + sAccessTime + "'";


            string sSQL = "", sProfile = "", sClientName = "", sLocation = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sProfile = GetPure(reader, 0);
                                }
                            }
                        }
                    }
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Client,m_Location FROM " + MyGlobal.activeDB + ".tbl_nfctags where m_TagId = '" + sKeyString + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sClientName = GetPure(reader, 0);
                                    sLocation = GetPure(reader, 1);
                                }
                            }
                        }
                    }


                    sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_jobs_doomwipeevents (";
                    sSQL += "m_IMEI,m_KeyString,m_Time,m_Lat,m_Lng,m_Acc,m_Profile,m_ClientName,m_Location";
                    sSQL += ") values (";
                    sSQL += "'" + sIMEI + "',";
                    sSQL += "'" + sKeyString + "',";
                    sSQL += "" + sAccessTime + ",";
                    sSQL += "'" + sLat + "',";
                    sSQL += "'" + sLng + "',";
                    sSQL += "'" + sAcc + "',";
                    sSQL += "'" + sProfile + "',";
                    sSQL += "'" + sClientName + "',";
                    sSQL += "'" + sLocation + "'";
                    sSQL += ");";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                        sRes.Append(s_m_id);
                    }
                    if (sKeyString.Substring(0, 4).Equals("ffff")) return 1;
                    //______________________________Send mail
                    PrepareSendMail_swipe(sProfile, sClientName, sLocation, sKeyString, sSwipeTime);
                    //______________________________Send mail End
                    return 1;
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("UpdateDatabaseSub_D Exception - " + ex.Message + "[" + sSQL + "]");
                return 2;   // DB Error
            }
        }
        //{D351558073167217id^time^notes^|...}
        //{D351558073167217id^1508078077202^notes^|...}
        public int UpdateDatabaseSub_H(string sIMEI, string sJSONData, StringBuilder sRes, string sClientName,string sStaffID,string sStaffName)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length < 3) return 0;
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            int unixTime = (int)span.TotalSeconds;

            string s_m_id_client = arData[0];
            string sAccessTime = arData[1];
            string sNotes = arData[2];


            if (sAccessTime.Length < 3) sAccessTime = "null"; else sAccessTime = "'" + sAccessTime + "'";


            string sSQL = "", sProfile = "";//, sClientName = "", sLocation = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //_______________________Get profile of this IMEI
                    sSQL = "SELECT m_Profile FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {

                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sProfile = GetPure(reader, 0);
                                }
                            }
                        }
                    }


                    sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_notes (";
                    sSQL += "m_id_client,m_Time,m_Notes,m_StaffID,m_StaffName,m_ClientName";
                    sSQL += ") values (";
                    sSQL += "'" + s_m_id_client + "',";
                    sSQL += "" + sAccessTime + ",";
                    sSQL += "'" + sNotes + "',";
                    sSQL += "'" + sStaffID + "',";
                    sSQL += "'" + sStaffName + "',";
                    sSQL += "'" + sClientName + "'";
                    sSQL += ");";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                        sRes.Append(s_m_id_client);
                        return 1;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("UpdateDatabaseSub_H Exception - " + ex.Message + "[" + sSQL + "]");
                return 2;   // DB Error
            }

        }
        /*
DispatchData 358187078395418={B35818707839541823^1506504827^13.0895862^80.2253236^23.532^1506504827^^^^|22^1506503993^13.0895862^80.2253236^23.41^1506503993^^^^|21^1506503983^13.0895862^80.2253236^23.41^1506503993^^^^|20^1506503968^13.0895862^80.2253236^23.588^1506503973^^^^|19^1506503539^13.0895834^80.2253322^20^1506503539^^^^|18^1506503534^13.0895862^80.2253236^23.65^1506503534^^^^|17^1506503529^13.0895862^80.2253236^23.65^1506503534^^^^|16^1506503509^13.0895862^80.2253236^23.438^1506503514^^^^|15^1506503354^13.0895862^80.2253236^23.421^1506503354^^^^|14^1506503308^13.0895862^80.2253236^23.523^1506503308^^^^|13^1506502982^13.0895862^80.2253236^23.593^1506503303^^^^|12^1506502982^13.0895862^80.2253236^23.526^1506502982^^^^|11^1506502893^13.0895862^80.2253236^23.519^1506502908^^^^|10^1506502893^13.0895862^80.2253236^23.437^1506502893^^^^|9^1506502888^13.0895862^80.2253236^23.437^1506502893^^^^|8^1506502888^13.0895862^80.2253236^23.437^1506502888^^^^|7^1506502877^13.0895862^80.2253236^23.437^1506502888^^^^|6^1506502602^13.0895862^80.2253236^23.533^1506502877^^^^|5^1506502597^13.0895862^80.2253236^23.424^1506502602^^^^|4^1506502533^13.0895862^80.2253236^23.404^1506502572^^^^|}
          
         */
        public int UpdateDatabaseSub_B(string sIMEI, string sJSONData, StringBuilder sRes)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length < 9) return 0;
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            int unixTime = (int)span.TotalSeconds;
            string sFirmwareVersion = "";
            string sModel = "";
            string s_m_id = arData[0];
            string sTimeIn = arData[1];
            string sTimeInLat = arData[2];
            string sTimeInLng = arData[3];
            string sTimeInAcc = arData[4];
            string sTimeOut = arData[5];
            string sTimeOutLat = arData[6];
            string sTimeOutLng = arData[7];
            string sTimeOutAcc = arData[8];

            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bRecordExists = false;
                    sSQL = "SELECT m_id from " + MyGlobal.activeDB + ".tbl_attendancelog where m_id_device='" +
                        s_m_id + "' and m_IMEI='" + sIMEI + "' and m_TimeIn='" + sTimeIn + "' and m_TimeInLat='" + sTimeInLat + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            bRecordExists = reader.HasRows;
                            reader.Close();
                        }
                    }
                    String sReturnID = "", m_StaffID = "", sStaffName = "", sClientName = "";
                    if (bRecordExists)
                    {
                        if (sTimeOut.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_attendancelog Set ";
                            sSQL += "m_TimeOut='" + sTimeOut + "',";
                            sSQL += "m_TimeOutLat='" + sTimeOutLat + "',";
                            sSQL += "m_TimeOutLng='" + sTimeOutLng + "',";
                            sSQL += "m_TimeOutAcc='" + sTimeOutAcc + "' ";
                            sSQL += "where m_id_device = '" +
                            s_m_id + "' and m_IMEI='" + sIMEI + "' and m_TimeIn='" + sTimeIn +
                            "' and m_TimeInLat='" + sTimeInLat + "'";
                            //sReturnID = "O";
                            sReturnID = "A";
                        }
                        else
                        {
                            MessageToDebugger("Err-FromDoom-UpdateDatabaseSub_B" + " Ignored sql=" + sSQL);
                            sSQL = "";
                            sReturnID = "I";
                        }
                    }
                    else
                    {
                        
                        //_________________________________Get Staff details
                        sSQL = "SELECT m_DriverID1, concat(staff.m_FName,' ', m_MName,' ', m_LName) as Name,assignstaff.m_ClientName from " + MyGlobal.activeDB + ".tbl_assignment ass " +
                        "left join " + MyGlobal.activeDB + ".tbl_staffs staff on staff.m_StaffID = ass.m_DriverID1  " +
                        "left join " + MyGlobal.activeDB + ".tbl_assignment_staff assignstaff on assignstaff.m_StaffID = ass.m_DriverID1 "+
                        "where ass.m_DeviceIMEI = '" + sIMEI + "' ";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_StaffID = GetPure(reader, 0);
                                        sStaffName = GetPure(reader, 1);
                                        sClientName = GetPure(reader, 2);
                                    }
                                }
                                reader.Close();
                            }
                        }

                        /*
                        sSQL = "SELECT m_StaffID from " + MyGlobal.activeDB + ".tbl_staffs where m_DeviceIMEI='" + sIMEI + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_StaffID = GetPure(reader, 0);
                                    }
                                }
                                reader.Close();
                            }
                        }
                        if (m_StaffID.Length == 0)  // No staff record available
                        {
                            CreateNewDBEntries_Staff(sIMEI, "mark");
                        }
                        */
                        //___________________________________________________
                        if (sTimeOut.Length == 0)
                        {
                            sTimeOut = "null";
                            sReturnID = "I";
                        }
                        else
                        {
                            sTimeOut = "'" + sTimeOut + "'";
                            sReturnID = "A";
                        }
                        if (sTimeOutLat.Length == 0) sTimeOutLat = "null"; else sTimeOutLat = "'" + sTimeOutLat + "'";
                        if (sTimeOutLng.Length == 0) sTimeOutLng = "null"; else sTimeOutLng = "'" + sTimeOutLng + "'";
                        if (sTimeOutAcc.Length == 0) sTimeOutAcc = "null"; else sTimeOutAcc = "'" + sTimeOutAcc + "'";
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_attendancelog (";
                        sSQL += "m_id_device,m_StaffID,m_IMEI,m_TimeIn,m_TimeOut,m_TimeInLat,m_TimeInLng,m_TimeInAcc,";
                        sSQL += "m_TimeOutLat,m_TimeOutLng,m_TimeOutAcc,m_StaffName,m_ClientName) values (";
                        sSQL += "'" + s_m_id + "',";
                        sSQL += "'" + m_StaffID + "',";
                        sSQL += "'" + sIMEI + "',";
                        sSQL += "'" + sTimeIn + "',";
                        sSQL += "" + sTimeOut + ",";
                        sSQL += "'" + sTimeInLat + "',";
                        sSQL += "'" + sTimeInLng + "',";
                        sSQL += "'" + sTimeInAcc + "',";
                        sSQL += "" + sTimeOutLat + ",";
                        sSQL += "" + sTimeOutLng + ",";
                        sSQL += "" + sTimeOutAcc + ",";
                        sSQL += "'" + sStaffName + "',";
                        sSQL += "'" + sClientName + "'";
                        sSQL += ");";
                    }
                    int iRet = 2;
                    if (sSQL.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            sRes.Append(sReturnID + s_m_id);
                            iRet= 1;
                        }
                    }
                    else
                    {
                        if (sReturnID.Length > 0 && s_m_id.Length > 0)
                        {
                            sRes.Append(sReturnID + s_m_id);
                            iRet= 1;
                        }
                        else
                        {
                            iRet= 2;
                        }
                    }
                    //______________________________Send mail
                    PrepareSendMail_shift(s_m_id, sIMEI, sTimeIn, sTimeInLat);
                    //______________________________Send mail END
                    return iRet;
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-UpdateDatabaseSub_B" + ex.Message);
                return 2;   // DB Error
            }
        }
        /*
{G3581870783954182^1510640549^^^^^^^^^^^^^|1^123^1510640549^^^^1510640549^^^^0^0^0^0^^|}
          
         */
        public int UpdateDatabaseSub_G(string sProfile, string sIMEI, string sJSONData, StringBuilder sRes)
        {
            sRes.Clear();
            char[] delimiterChars = { '^' };
            string[] arData = sJSONData.Split(delimiterChars);
            if (arData.Length <= 14) return 0;
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            int unixTime = (int)span.TotalSeconds;
            string sFirmwareVersion = "";
            string sModel = "";
            string s_m_id = arData[0];
            string s_TripNo = arData[1];
            string m_Start = arData[2];
            string m_TimeStartLat = arData[3];
            string m_TimeStartLng = arData[4];
            string m_TimeStartAcc = arData[5];
            string m_End = arData[6];
            string m_TimeEndLat = arData[7];
            string m_TimeEndLng = arData[8];
            string m_TimeEndAcc = arData[9];

            string m_Distance = arData[10];
            string m_Waiting = arData[11];
            string m_Duration = arData[12];
            string m_Amount = arData[13];
            string m_Tariff = arData[14];
            string m_JobID = "";
            string m_DriverID = "";
            string m_RegNo = "";
            string m_FleetID = "";
            string m_DriverName = "";
            if (arData.Length > 15) m_JobID = arData[15];
            if (arData.Length > 19)
            {
                m_DriverName = arData[16];
                m_DriverID = arData[17];
                m_FleetID = arData[18];
                m_RegNo = arData[19];
            }
            //-------------------------------------------
            string m_Stage1="null", m_Stage2 = "null", m_Stage3 = "null", m_Stage4 = "null", m_Stage5 = "null";
            if (arData.Length > 24)
            {
                if (MyGlobal.GetInt64(arData[20]) > 0) m_Stage1 = "'" + arData[20] + "'";
                if (MyGlobal.GetInt64(arData[21]) > 0) m_Stage2 = "'" + arData[21] + "'";
                if (MyGlobal.GetInt64(arData[22]) > 0) m_Stage3 = "'" + arData[22] + "'";
                if (MyGlobal.GetInt64(arData[23]) > 0) m_Stage4 = "'" + arData[23] + "'";
                if (MyGlobal.GetInt64(arData[24]) > 0) m_Stage5 = "'" + arData[24] + "'";
            }

            //-------------------------------------------
            if (m_Amount.Length == 0) m_Amount = "0";
            if (m_Distance.Length == 0) m_Distance = "0";
            if (m_Waiting.Length == 0) m_Waiting = "0";
            //float dblDistance =(float) GetInt32(m_Distance) / 100f;

            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    bool bRecordExists = false;
                    sSQL = "SELECT m_id from " + MyGlobal.activeDB + ".tbl_trips where m_id_device='" + s_m_id + "' " +
                        "and m_DeviceIMEI='" + sIMEI + "' " +
                        "and m_TripSequentialNumber='" + s_TripNo + "' " +
                        "and m_Profile='" + sProfile + "';";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            bRecordExists = reader.HasRows;
                            reader.Close();
                        }
                    }

                    String sReturnID = "";
                    if (bRecordExists)
                    {
                        //if (sTimeOut.Length > 0)
                        //{
                        sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_trips Set ";
                        sSQL += "m_TripEndLatitude='" + m_TimeEndLat + "',";
                        sSQL += "m_TripEndLongitude='" + m_TimeEndLng + "',";
                        //sSQL += "m_TimeEndAcc='" + m_TimeEndAcc + "',";
                        sSQL += "m_DistanceHired='" + m_Distance + "', ";
                        sSQL += "m_DistanceTotal='" + m_Distance + "', ";
                        sSQL += "m_WaitingTime='" + m_Waiting + "', ";
                        if (m_End.Length > 0)
                        {
                            sSQL += "m_TripEndTime='" + m_End + "', ";
                        }
                        sSQL += "m_JobID='" + m_JobID + "', ";
                        sSQL += "m_DriverID='" + m_DriverID + "', ";
                        sSQL += "m_RegNo='" + m_RegNo + "', ";
                        sSQL += "m_FleetID='" + m_FleetID + "', ";
                        sSQL += "m_DriverName='" + m_DriverName + "', ";
                        sSQL += "m_AmountTotal='" + m_Amount + "', ";

                        sSQL += "m_Stage1=" + m_Stage1 + ", ";
                        sSQL += "m_Stage2=" + m_Stage2 + ", ";
                        sSQL += "m_Stage3=" + m_Stage3 + ", ";
                        sSQL += "m_Stage4=" + m_Stage4 + ", ";
                        sSQL += "m_Stage5=" + m_Stage5 + " ";

                        sSQL += "where m_id_device = '" + s_m_id + "' ";
                        sSQL += "and m_TripSequentialNumber = '" + s_TripNo + "' ";
                        sSQL += "and m_DeviceIMEI='" + sIMEI + "' and m_Profile='" + sProfile + "';";

                        //sReturnID = "O";
                        if (m_End.Length > 0)
                        {
                            sReturnID = "A";
                        }
                        else
                        {
                            sReturnID = "I";
                        }
                        /*
                        }
                        else
                        {
                            MessageToDebugger("Err-FromDoom-UpdateDatabaseSub_B" + " Ignored sql=" + sSQL);
                            sSQL = "";
                            sReturnID = "I";
                        }
                        */
                    }
                    else
                    {
                        /*
                        String m_StaffID = "0", sStaffName = "", sRegNo = "", sFleetID = "";
                        sSQL = "select m_StaffID,m_FName,m_RegNo from " + MyGlobal.activeDB + ".tbl_drivers " +
                            "where m_DeviceIMEI='" + sIMEI + "';";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_StaffID = GetPure(reader, 0);
                                        sStaffName = GetPure(reader, 1);
                                        sRegNo = GetPure(reader, 2);
                                        //sFleetID = GetPure(reader, 3);
                                    }
                                }
                                reader.Close();
                            }
                        }
                        if (m_StaffID.Length == 0) m_StaffID = "0";
                        */
                        /*
                        sSQL = "SELECT m_StaffID from " + MyGlobal.activeDB + ".tbl_staffs where m_DeviceIMEI='" + sIMEI + "'";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        m_StaffID = GetPure(reader, 0);
                                    }
                                }
                                reader.Close();
                            }
                        }
                        if (m_StaffID.Length == 0)  // No staff record available
                        {
                            CreateNewDBEntries_Staff(sIMEI, "mark");
                        }
                        */
                        //___________________________________________________
                        if (m_End.Length == 0)
                        {
                            m_End = "null";
                            sReturnID = "I";
                        }
                        else
                        {
                            m_End = "'" + m_End + "'";
                            sReturnID = "A";
                        }
                        if (m_TimeEndLat.Length == 0) m_TimeEndLat = "null"; else m_TimeEndLat = "'" + m_TimeEndLat + "'";
                        if (m_TimeEndLng.Length == 0) m_TimeEndLng = "null"; else m_TimeEndLng = "'" + m_TimeEndLng + "'";
                        if (m_TimeEndAcc.Length == 0) m_TimeEndAcc = "null"; else m_TimeEndAcc = "'" + m_TimeEndAcc + "'";
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_trips (";
                        sSQL += "m_id_device,m_DriverID,m_DeviceIMEI,m_Profile,";
                        sSQL += "m_TripSequentialNumber,";
                        sSQL += "m_TripType,";
                        sSQL += "m_VehicleID,";
                        sSQL += "m_RegNo,";
                        sSQL += "m_TripStartTime,";
                        sSQL += "m_TimeReceived,";
                        sSQL += "m_TripEndLatitude,";
                        sSQL += "m_TripEndLongitude,";
                        sSQL += "m_TripEndTime,"; 
                        sSQL += "m_AmountTotal,";
                        sSQL += "m_DistanceHired,";
                        sSQL += "m_DistanceTotal,";
                        sSQL += "m_JobID,";
                        sSQL += "m_DriverName,";
                        sSQL += "m_Stage1,";
                        sSQL += "m_Stage2,";
                        sSQL += "m_Stage3,";
                        sSQL += "m_Stage4,";
                        sSQL += "m_Stage5";
                        sSQL += ") values (";
                        sSQL += "'" + s_m_id + "',";
                        sSQL += "'" + m_DriverID + "',";
                        sSQL += "'" + sIMEI + "',";
                        sSQL += "'" + sProfile + "',";
                        sSQL += "'" + s_TripNo + "',";
                        sSQL += "'" + "2" + "',";
                        sSQL += "'" + m_FleetID + "',";
                        sSQL += "'" + m_RegNo + "',";
                        sSQL += "'" + m_Start + "',";
                        sSQL += "Now(),";
                        sSQL += "" + m_TimeEndLat + ",";
                        sSQL += "" + m_TimeEndLng + ",";
                        sSQL += "" + m_End + ",";
                        sSQL += "'" + m_Amount + "',";
                        sSQL += "'" + m_Distance + "',";
                        sSQL += "'" + m_Distance + "',";
                        sSQL += "'" + m_JobID + "',";
                        sSQL += "'" + m_DriverName + "',";
                        sSQL += "" + m_Stage1 + ",";
                        sSQL += "" + m_Stage2 + ",";
                        sSQL += "" + m_Stage3 + ",";
                        sSQL += "" + m_Stage4 + ",";
                        sSQL += "" + m_Stage5 + "";
                        sSQL += ");";

                    }
                    if (sSQL.Length > 0)
                    {
                        //___Trip end. So, close any associated Jobs also
                        if (!m_End.Equals("null"))
                        {
                            sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_jobs_doom Set m_TimeClosed=Now(),m_Status='closed'," +
                                "m_TripNo='" + s_TripNo + "' where m_AssignedTo='" + sIMEI + "' and " +
                                "m_TimeClosed is null;";
                        }
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            sRes.Append(sReturnID + s_m_id);
                            if (!m_End.Equals("null"))
                            {
                                ReceiptRequestObj receiptRequestObj = new ReceiptRequestObj();
                                receiptRequestObj.tripno = s_TripNo;
                                receiptRequestObj.imei = sIMEI;
                                Thread newThread = new Thread(ChatHub.SendReceiptForThisTripNo);
                                newThread.Start(receiptRequestObj);
                            }
                            return 1;
                        }
                    }
                    else
                    {
                        if (sReturnID.Length > 0 && s_m_id.Length > 0)
                        {
                            sRes.Append(sReturnID + s_m_id);
                            return 1;
                        }
                        else
                        {
                            return 2;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err1-FromDoom-UpdateDatabaseSub_G" + ex.Message+"["+sSQL+"]");
                return 2;   // DB Error
            }
        }
        
        private void GetAdditionalInfo(String sIMEI, ref string sClientName, ref string sStaffID, ref string sStaffName)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //String sSQL = "SELECT m_FleetID,m_RegNo,m_DriverID1 from " + MyGlobal.activeDB + ".tbl_assignment where m_DeviceIMEI='" + sIMEI + "'";
                    String sSQL = "" +
                    "SELECT m_FleetID, m_RegNo, m_DriverID1, concat(m_FName,' ', m_MName, ' ', m_LName),m_ClientName from " + MyGlobal.activeDB + ".tbl_assignment as aa " +
"left join " + MyGlobal.activeDB + ".tbl_staffs as ss on ss.m_StaffID = aa.m_DriverID1 " +
"left join " + MyGlobal.activeDB + ".tbl_assignment_staff as asignstaff on asignstaff.m_StaffID = aa.m_DriverID1 " +
"where aa.m_DeviceIMEI = '" + sIMEI + "' ";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    //sFleetID = GetPure(reader, 0);
                                    //sRegNo = GetPure(reader, 1);
                                    sStaffID = GetPure(reader, 2);
                                    sStaffName = GetPure(reader, 3);
                                    sClientName = GetPure(reader, 4);
                                }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-UpdateDatabaseSub_B" + ex.Message);

            }
        }
        private void GetFleetAndDriver(String sIMEI, ref string sFleetID, ref string sRegNo, ref string sDriverID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "SELECT m_FleetID,m_RegNo,m_DriverID1 from " + MyGlobal.activeDB + ".tbl_assignment where m_DeviceIMEI='" + sIMEI + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sFleetID = GetPure(reader, 0);
                                    sRegNo = GetPure(reader, 1);
                                    sDriverID = GetPure(reader, 2);
                                }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-UpdateDatabaseSub_B" + ex.Message);

            }
        }
        //{D35155807316721711^4^Accept^1508078077202^^|10^4^Accept^1508078067241^^|9^4^Accept^1508078067241^^|8^4^Accept^1508078067241^^|7^4^Accept^1508078062151^^|6^4^Accept^1508078062151^^|5^3^Accept^1508078034000^^|4^3^Accept^1508078031131^^|3^3^Accept^1508078026112^^|2^3^Accept^1508078021101^^|1^3^Accept^1508078016042^^|}
        public String UpdateDatabase_D(string sIMEI, string sJSONData) // Ambulance key data
        {
            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "", sFleetID = "", sRegNo = "", sDriverID = "";
            GetFleetAndDriver(sIMEI, ref sFleetID, ref sRegNo, ref sDriverID);
            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_D(sIMEI, arData[i], sRes, sFleetID, sRegNo, sDriverID);
                if (iStatus == 2) continue;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            return sReturn;

        }
        
        public String UpdateDatabase_E(string sIMEI, string sJSONData) // NFC
        {

            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "", sFleetID = "", sRegNo = "", sDriverID = "";
            GetFleetAndDriver(sIMEI, ref sFleetID, ref sRegNo, ref sDriverID);
            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_E(sIMEI, arData[i], sRes, sFleetID, sRegNo, sDriverID);
                if (iStatus == 2) continue;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            return sReturn;

        }
        //{D351558073167217id^time^notes^|...}
        //{D351558073167217id^1508078077202^notes^|...}
        public String UpdateDatabase_H(string sIMEI, string sJSONData) // Ambulance key data
        {

            int iLength = sJSONData.Length;
            if (iLength > 0)
            {
                if (sJSONData[0] != '{') return "";
            }
            if (sJSONData[iLength - 1] != '}') return "";
            if (sJSONData.Length < 25) return "";
            String sData = sJSONData.Substring(17);
            char[] delimiterChars = { '|' };
            string[] arData = sData.Split(delimiterChars);
            int iPacketLength = arData.Length;
            int iStatus = 0;
            String sReturn = "", sClientName = "", sStaffID = "", sStaffName = "";
            GetAdditionalInfo(sIMEI, ref sClientName, ref sStaffID, ref sStaffName);
            for (int i = 0; i < iPacketLength; i++)
            {
                StringBuilder sRes = new StringBuilder();
                iStatus = UpdateDatabaseSub_H(sIMEI, arData[i], sRes, sClientName, sStaffID, sStaffName);
                if (iStatus == 2) continue;
                if (sRes.Length > 0)
                {
                    sReturn += sRes.ToString() + "-";
                }
            }
            return sReturn;

        }
        private void CreateNewDBEntries_Device(String sIMEI, String sProfile)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    try
                    {
                        string sSQL1 = "";
                        TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
                        int unixTime = (int)span.TotalSeconds;
                        string sSQL = "SELECT m_id from " + MyGlobal.activeDB + ".tbl_Devices where " +
                            "m_IMEI='" + sIMEI + "' and m_Profile='" + sProfile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    sSQL1 = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_Devices (";
                                    sSQL1 += @"m_IMEI,m_Name,m_Status,m_CreatedTime,m_UpdatedTime,m_VeriCode,";
                                    sSQL1 += @"m_TimeReceived,m_Type,m_Security,";
                                    sSQL1 += "m_FriendlyName,m_Profile) values (";
                                    sSQL1 += "'" + sIMEI + "',";
                                    sSQL1 += "'" + "New" + "',";
                                    sSQL1 += "'" + "Verified" + "',";
                                    sSQL1 += "Now(),";
                                    sSQL1 += "Now(),";
                                    sSQL1 += "'" + GetRandomNo(1000, 9999) + "',";
                                    sSQL1 += "'" + unixTime + "',";
                                    sSQL1 += "'New device',";
                                    sSQL1 += "'9',";
                                    sSQL1 += "'New',";
                                    sSQL1 += "'" + sProfile + "'";
                                    sSQL1 += ");";
                                }
                            }
                        }
                        sSQL = "SELECT m_id from " + MyGlobal.activeDB + ".tbl_authorized where " +
                            "m_IMEI='" + sIMEI + "' and m_Profile='" + sProfile + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    sSQL1 += "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (";
                                    sSQL1 += "m_IMEI,m_Profile,m_Status";
                                    sSQL1 += ") values (";
                                    sSQL1 += "'" + sIMEI + "',";
                                    sSQL1 += "'" + sProfile + "',";
                                    sSQL1 += "'" + "1" + "'";
                                    sSQL1 += ");";
                                }
                            }
                        }
                        if (sSQL1.Length > 0)
                        {
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL1, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (MySqlException ex1)
                    {

                    }
                    //_____________________________________________________________
                    string sSQLCreateLogTable = @"CREATE TABLE IF NOT EXISTS dispatch_log.log_" + sIMEI + " LIKE " + MyGlobal.activeDB + ".log_000000000000000";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLCreateLogTable, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex1)
            {

            }
        }
        private void CreateNewDBEntries_Staff(String sIMEI, String sProfile)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    try
                    {
                        TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
                        int unixTime = (int)span.TotalSeconds;

                        string sSQL1 = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_staffs (";
                        sSQL1 += @"m_Profile,m_FName,m_Vericode";
                        sSQL1 += ") values (";
                        sSQL1 += "'" + sProfile + "',";
                        sSQL1 += "'" + "New" + "',";
                        sSQL1 += "'" + GetRandomNo(1000, 9999) + "'";
                        sSQL1 += ");";
                        /*
                                                string sSQL1 = @"INSERT INTO " + MyGlobal.activeDB + ".tbl_devices (";
                                                sSQL1 += @"m_Profile,m_FName,m_DeviceIMEI,m_TimeReceived,m_Vericode";
                                                sSQL1 += ") values (";
                                                sSQL1 += "'" + sProfile + "',";
                                                sSQL1 += "'" + "New" + "',";
                                                sSQL1 += "'" + sIMEI + "',";
                                                sSQL1 += "'" + unixTime + "',";
                                                sSQL1 += "'" + GetRandomNo(1000, 9999) + "'";
                                                sSQL1 += ");";
                                                */
                        sSQL1 += "INSERT INTO " + MyGlobal.activeDB + ".tbl_authorized (";
                        sSQL1 += "m_IMEI,m_Profile,m_Status";
                        sSQL1 += ") values (";
                        sSQL1 += "'" + sIMEI + "',";
                        sSQL1 += "'" + sProfile + "',";
                        sSQL1 += "'" + "1" + "'";
                        sSQL1 += ");";

                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL1, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    catch (MySqlException ex1)
                    {

                    }
                    //_____________________________________________________________
                    /*
                    string sSQLCreateLogTable = @"CREATE TABLE IF NOT EXISTS dispatch_log.log_" + sIMEI + " LIKE " + MyGlobal.activeDB + ".log_000000000000000";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLCreateLogTable, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                    */
                }
            }
            catch (MySqlException ex1)
            {

            }
        }
        private static string GetRandomNo(int min, int max)
        {
            Random r = new Random();
            return r.Next(min, max).ToString(); //for ints
        }
        /*
        public void test(ref Customer i)
        {
            i.Name = "1000";
            //customer = new Customer();
            //customer.Name = "John";
        }
        */
        public class MyLatest
        {
            public Int32 iTime { get; set; }
            public String sData { get; set; }
        }
        private string GetLastTripDetails(string sIMEI)
        {
            string sReturn = "";
            String sProfile = GetProfileOfThisIMEI(sIMEI);
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "SELECT m_TripSequentialNumber, "+
                        "m_TripStartTime,m_TripEndTime "+
                        "from " + MyGlobal.activeDB + ".tbl_trips where " +
                        "m_DeviceIMEI='" + sIMEI + "' and m_Profile='" + sProfile + "' " +
                        "order by m_TripSequentialNumber desc limit 1;";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sReturn = "^";
                                    sReturn += GetPure(reader, 0)+"^";       //  m_TripSequentialNumber
                                    sReturn += GetPure(reader, 1) + "^";    //  m_TripStartTime
                                    sReturn += GetPure(reader, 2) + "^";    //  m_TripEndTime
                                    sReturn += "keycheck" + "^|";
                                }
                            }else
                            {
                                sReturn = "^";
                                sReturn += "0" + "^";    //  m_TripSequentialNumber
                                sReturn += "0" + "^";   //  m_TripStartTime
                                sReturn += "0" + "^";   //  m_TripEndTime
                                sReturn += "keycheck" + "^|";
                            }
                            reader.Close();
                            MessageToDebugger("Err-FromDoom-GetLastTripDetails by IMEI-" + sIMEI +
                                ", profile- " + sProfile);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-GetLastTripDetails" + ex.Message);

            }
            return sReturn;
        }

        public static String UnixTimeToDateTime(String sUnixTime)
        {
            if (sUnixTime.Length < 5) return "";
            long unixtime = GetInt32(sUnixTime);
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return sTime.AddSeconds(unixtime).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    public class ReceiptRequestObj
    {
        public string imei { get; set; }
        public string tripno { get; set; }
        public string profile { get; set; }
    }
}