using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;


using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;
using System.Text;
using System.IO;
using MyHub.Controllers;

//https://docs.microsoft.com/en-us/aspnet/signalr/overview/getting-started/tutorial-getting-started-with-signalr-and-mvc
namespace MyHub.Hubs
{

    public partial class ChatHub : Hub
    {

        //private readonly static ConnectionMapping<string> connObj = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_GreyOfficeMobile = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_DoomDriver = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_LogisticsClients = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_DoomClient = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_AccessManager = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_Browser = new ConnectionMapping<string>();
        private readonly static ConnectionMapping<string> connObj_Dispatchers = new ConnectionMapping<string>();
        private static Dictionary<string, List<string>> _subscriptions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, int> _TaxiDataBlockList = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void Hello()
        {
            Clients.All.hello();
        }
        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            //Clients.All.broadcastMessage(name, message);
            //Console.WriteLine("name=" + name + ",message=" + message);
            //Log("name=" + name + ",message=" + message);
            Clients.All.addNewMessageToPage(name, message);
        }
        public void Broadcast(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message);
        }
        // Life checking
        public void helloclient(string imei, string message)
        {
            List<string> li = connObj_DoomClient.GetList(imei);
            if (li != null)
            {
                li.ForEach(delegate (String connectionid)
                {
                    Clients.Client(connectionid).helloclient(message);
                });
            }
        }
        public void greymobile(string imei, Object obj)
        {
            try
            {
                List<string> li = connObj_GreyOfficeMobile.GetList(imei);
                if (li != null)
                {
                    li.ForEach(delegate (String connectionid)
                    {
                        Clients.Client(connectionid).greymobile(obj);
                    });
                }
            }
            catch (InvalidOperationException)
            {

            }
        }
        public static void Log(String txt)
        {
            try
            {
                using (var file = new StreamWriter("c:\\temp\\temp\\log.txt", true))
                {
                    file.WriteLine(txt);
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static double GetDouble(String sDbl)
        {
            double dblRet = 0;
            if(double.TryParse(sDbl,out dblRet))
            {

            }
            return dblRet;
        }
        public void dispatchJsonToDriver(object o)
        {
            // Call the broadcastMessage method to update clients.
            //Clients.All.broadcastMessage(name, message);
            Clients.All.dispatchJsonToDriver(o);
        }
        public void DataToClient(object o)
        {
            Clients.All.dispatchJsonToDriver(o);
        }
        public void MeterStatusToClient(object o)
        {
            Clients.All.MeterStatusToClient(o);
        }
        public void jobMessageToClient(object o)
        {
            // Call the broadcastMessage method to update clients.
            //Clients.All.broadcastMessage(name, message);
            Clients.All.jobMessageToClient(o);
        }
        public void HubToBrowser(object o)
        {
            Clients.All.HubToBrowser(o);
        }
        public void HubToAccessManager(object o)
        {
            Clients.All.HubToAccessManager(o);
        }
        public void Setkey(string key, string value)
        {
            if ((key.Length > 0) && (key.Length > 0))
            {
                if (_subscriptions.ContainsKey(key)) {
                    List<string> li = _subscriptions[key];
                    if (!li.Contains(value)) li.Add(value);
                    //_subscriptions[key] = li;
                    Clients.All.addNewMessageToPage("value added to existing list", key + "-->" + value);
                }
                else
                {
                    List<string> li = new List<string>();
                    li.Add(value);
                    _subscriptions.Add(key, li);
                    Clients.All.addNewMessageToPage("Key created and value added", key + "-->" + value);
                }
            }
            else
            {
                Clients.All.addNewMessageToPage("Key or Value empty", key + "-->" + value);
            }
        }
        public void Removekey(string key, string value)
        {
            if ((key.Length > 0) && (key.Length > 0))
            {
                if (_subscriptions.ContainsKey(key))
                {
                    List<string> li = _subscriptions[key];
                    if (li.Contains(value))
                    {
                        li.Remove(value);
                        Clients.All.addNewMessageToPage("value removed from key", key + "-->" + value);
                        if (li.Count == 0) _subscriptions.Remove(key);
                    }
                    else
                    {
                        Clients.All.addNewMessageToPage("Matching value not exists", key + "-->" + value);
                    }
                }
                else
                {
                    Clients.All.addNewMessageToPage("No key to remove this value", key + "-->" + value);
                }
                //___________________________________________
                connObj_GreyOfficeMobile.Remove(key, value);
                connObj_LogisticsClients.Remove(key, value);
                connObj_DoomDriver.Remove(key, value);
                connObj_DoomClient.Remove(key, value);
                connObj_AccessManager.Remove(key, value);
                connObj_Browser.Remove(key, value);
                connObj_Dispatchers.Remove(key, value);
            }
            else
            {
                Clients.All.addNewMessageToPage("Key or Value empty", key + "-->" + value);
            }
        }

        public void Send1(string mess)
        {
            List<TestData> testList = new List<TestData>();
            testList.Add(new TestData { p1 = "eugene", p2 = "shiffin" });
            string xx = testList.ToString().Replace("[", "").Replace("]", "");
            Clients.All.receiveNotification(xx);
        }
        /* robin
        public void SendTo(string sToKey, string sMess)
        {
            List<string> li = connObj.GetList(sToKey);
            if (li != null)
            {
                li.ForEach(delegate (String connectionid)
                {
                    Clients.Client(connectionid).broadcastMessage(sToKey, sMess);
                });
            }
        }
        */
        
        public void FromDispatchApp(string sIMEI, string sJSONData)// Message from sIMEI
        {
            PushFromDispatch(sIMEI, sJSONData);
            //SendToAllDevicesInThisProfile(sIMEI, sJSONData);
            //SendToSubscribedList(sIMEI, sJSONData); //  This is done from dispatcher itself
        }
        
        public void PushFromDispatch(string sIMEI, string sJSONData)// Message from sIMEI
        {
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                //___________________Broadcast the packets to the requested clients
                string sSQL = "select m_IMEI,m_Profile,m_User from " + Controllers.MyGlobal.activeDB + ".tbl_Authorized where m_IMEI='" + sIMEI + "'";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string sKey = GetPure(reader, 1);
                                /*
                                if (reader["m_User"] != null)
                                {
                                    if (reader["m_User"].ToString().Length > 0)
                                    {
                                        sKey += "_" + reader["m_User"].ToString();
                                    }
                                }
                                */
                                List<string> li = connObj_Browser.GetList(sKey); // robin It was connObj.GetList(sKey);
                                if (li != null)
                                {
                                    li.ForEach(delegate (String connectionid)
                                    {
                                        Clients.Client(connectionid).broadcastMessage("DispatchData", sJSONData);
                                    });
                                }
                            }
                        }
                        reader.Close();
                    }
                }
                //______________________________________________________debugger
                MessageToDebugger("DispatchData " + sIMEI + "=" + sJSONData);
                con.Close();
            }
        }
        /* robin
        public void SendToAllDevicesInThisProfile(string sIMEI, string sJSONData)// Message from sIMEI
        {
            String sProfile = "";// GetProfileOfThisIMEI(sIMEI);
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                //_______________________Get profile of this IMEI
                string sSQL = "SELECT m_Profile,m_OperationMode FROM " + MyGlobal.activeDB + ".tbl_devices where m_IMEI = '" + sIMEI + "' limit 1;";
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {

                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                sProfile = GetPure(reader, 0);
                                //String m_OperationMode = GetPure(reader, 1);
                                //if (m_OperationMode.Length == 4)
                                //{
                                //    if(m_OperationMode.Substring(0,1).Equals("2"))return;
                                //}
                            }
                        }
                    }
                }
                //___________________Broadcast the packets to the requested clients
                //sSQL = "select m_IMEI from " + MyGlobal.activeDB + ".tbl_Authorized where m_Profile='" + sProfile + "'";
                // Send to all devices under the profile & to the devices subscribed
                sSQL = "select imei from(" +
                  "select m_Profile as imei from " + MyGlobal.activeDB + ".tbl_Authorized where m_IMEI = '" + sIMEI + "' and (m_Mode is null or m_Mode<>2) " +
                  "union " +
                  "select m_IMEI as imei from " + MyGlobal.activeDB + ".tbl_Authorized where m_Profile = '" + sProfile + "' and (m_Mode is null or m_Mode<>2) " + // Driver mode don't send
                        ")as x GROUP BY imei;";

                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string sKey = GetPure(reader, 0);
                                List<string> li = connObj.GetList(sKey);
                                if (li != null)
                                {
                                    li.ForEach(delegate (String connectionid)
                                    {
                                        Clients.Client(connectionid).broadcastMessage("DispatchData", sJSONData);
                                    });
                                }
                            }
                        }
                        reader.Close();
                    }
                }
                //________________________And send to the concern profile also
                List<string> lii = connObj.GetList(sProfile);
                if (lii != null)
                {
                    lii.ForEach(delegate (String connectionid)
                    {
                        Clients.Client(connectionid).broadcastMessage("DispatchData", sJSONData);
                    });
                }
                //______________________________________________________debugger
                MessageToDebugger("SendToAllDevicesInThisProfile " + sProfile + "=" + sJSONData);
                con.Close();
            }
        }
        */
        public void SendToAllBrowsersExceptBlock( string sJSONData)// Message from sIMEI
        {
            Dictionary<string, List<string>> _connections = connObj_Browser.GetList();
            foreach (var key in _connections.Keys)
            {
                if (!_TaxiDataBlockList.ContainsKey(key)) // Send, if not in this block list
                {
                    List<string> lii = connObj_Browser.GetList(key);
                    if (lii != null)
                    {
                        lii.ForEach(delegate (String connectionid)
                        {
                            Clients.Client(connectionid).broadcastMessage("DispatchData", sJSONData);
                        });
                    }
                }
            }
        }
        public void SendToAllBrowsers(string sJSONData)// Message from sIMEI
        {
            Dictionary<string, List<string>> _connections = connObj_Browser.GetList();
            foreach (var key in _connections.Keys)
            {
                
                    List<string> lii = connObj_Browser.GetList(key);
                    if (lii != null)
                    {
                        lii.ForEach(delegate (String connectionid)
                        {
                            Clients.Client(connectionid).broadcastMessage("DispatchData", sJSONData);
                        });
                    }
                
            }
        }
        /*
         If  _subscriptions contains entries, send only those, or send all
         */
        public bool SendToSubscribedList(String sIMEI_DataSource,String sData)
        {
            if (_subscriptions.ContainsKey(sIMEI_DataSource))
            {
                List<string> lii = _subscriptions[sIMEI_DataSource]; // Get all clients subscribed for this driver
                if (lii != null)
                {
                    lii.ForEach(delegate (String sRequestedClient)   // Loopall clients 
                    {
                        List<string> li_DoomClients = connObj_DoomClient.GetList(sRequestedClient); // Loopall connections of the client
                        if (li_DoomClients != null)
                        {
                            li_DoomClients.ForEach(delegate (String connectionid)
                            {
                                Clients.Client(connectionid).DataToClient(sData);
                            });
                        }
                        List<string> li_Browser = connObj_Browser.GetList(sRequestedClient); // Loopall connections of the client
                        if (li_Browser != null)
                        {
                            li_Browser.ForEach(delegate (String connectionid)
                            {
                                Clients.Client(connectionid).DataToClient(sData);
                            });
                        }
                    });
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /* robin
        public string PushToDispatch(string sIMEI, string sJSONData)
        {
            string sRet = "Unable to send";
            List<string> li = connObj.GetList(sIMEI);
            if (li != null)
            {
                li.ForEach(delegate (String connectionid)
                {
                    Clients.Client(connectionid).broadcastMessage(sIMEI, sJSONData);
                    sRet = "";
                });
            }
            MessageToDebugger("PushToDispatch-" + sIMEI + "=" + sJSONData);
            return sRet;
        }
        */
        /* ********************************************************************************* */
        /*
        public string GetListTable()
        {
            return connObj.GetListTable();
        }
        */
        public int GetLogisticsClientsOnline()
        {
            return connObj_LogisticsClients.GetCount();
        }
        public int GetGreyOfficeMobileOnline()
        {
            return connObj_GreyOfficeMobile.GetCount();
        }
        public int GetTaxiesOnline()
        {
            return connObj_DoomDriver.GetCount();
        }
        
        public int GetAccessManagerTerminalsOnline()
        {
            return connObj_AccessManager.GetCount();
        }
        public string GetLogisticsClientsList_todebug() { return connObj_LogisticsClients.GetListTable(); }
        public string GetDriverList_todebug(){return connObj_DoomDriver.GetListTable();}
        public string GetGreyOfficeMobileList_todebug() { return connObj_GreyOfficeMobile.GetListTable(); }
        public string GetClientList_todebug(){ return connObj_DoomClient.GetListTable();}
        public string GetAccessManagerList_todebug() { return connObj_AccessManager.GetListTable(); }
        public string GetDispatchersList_todebug() { return connObj_Dispatchers.GetListTable(); }
        
        public string GetBrowserList_todebug(){return connObj_Browser.GetListTable();}
        //public string GetUnknownList_todebug() { return connObj.GetListTable(); }
        
        public List<string> GetGreyOfficeMobileConnections(string sToKey) { return connObj_GreyOfficeMobile.GetList(sToKey); }
        public List<string> GetLogisticsClientsConnections(string sToKey) { return connObj_LogisticsClients.GetList(sToKey); }
        public List<string> GetClientConnections(string sToKey){return connObj_DoomClient.GetList(sToKey);}
        public List<string> GetDriverConnections(string sToKey) { return connObj_DoomDriver.GetList(sToKey);}
        public List<string> GetAccessManagerConnections(string sToKey) { return connObj_AccessManager.GetList(sToKey); }
        public List<string> GetBrowserConnections(string sToKey) { return connObj_Browser.GetList(sToKey); }
        //- - - - - - - - - - - - - - - - - - - - 
        /* robin
        public List<string> GetConList(string sToKey)
        {
            return connObj.GetList(sToKey);
        }
        */
        public string GetSubList()
        {
            String sRet = "<table style='margin: 0px; padding: 0px; border-spacing: 0px; background-color:#ffd;'>";
            foreach (var item in _subscriptions)
            {
                sRet += "<tr style='border:1px solid gray;'>" +
                        "<td style='vertical-align:top;border:1px solid gray;'>" + item.Key + "</td>";
                sRet += "<td style='border:1px solid gray;'>";
                //_______________________________________________
                List<string> li = item.Value;
                if (li != null)
                {
                    li.ForEach(delegate (String value)
                    {
                        sRet += "<div style='border-bottom:1px solid gray;'>"+value + "</div>";
                    });
                }
                //_______________________________________________
                sRet += "</td>";
                sRet += "</tr>";
            }
            sRet += "</table>-_subscriptions-" + _subscriptions.Count();
            return sRet;
        }
        public string GetBlockedList()
        {
            String sRet = "<table style='margin: 0px; padding: 0px; border-spacing: 0px; background-color:#fdf;'>";
            foreach (var item in _TaxiDataBlockList)
            {
                sRet += "<tr style='border:1px solid gray;'>" +
                        "<td style='vertical-align:top;border:1px solid gray;'>" + item.Key + "</td>";
                sRet += "<td style='border:1px solid gray;'>";
                //_______________________________________________
                sRet += "<div style='border-bottom:1px solid gray;'>" + item.Value + "</div>";
                //_______________________________________________
                sRet += "</td>";
                sRet += "</tr>";
            }
            sRet += "</table>-_TaxiDataBlockList-" + _TaxiDataBlockList.Count();
            return sRet;
        }
        public override Task OnConnected()
        {
            String ssIMEI = "", ssProfile = "", ssSession = "", ssUser = "",sType="";
            if (Context.QueryString["imei"] != null) ssIMEI = Context.QueryString["imei"];
            if (Context.QueryString["type"] != null) sType = Context.QueryString["type"];
            if (Context.QueryString["profile"] != null) ssProfile = Context.QueryString["profile"];
            if (Context.QueryString["user"] != null) ssUser = Context.QueryString["user"];
            if (Context.QueryString["session"] != null) ssSession = Context.QueryString["session"];

            if (sType.Equals("doomdriver"))
            {
                connObj_DoomDriver.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("doomclient"))
            {
                connObj_DoomClient.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("logisticsclient"))
            {
                connObj_LogisticsClients.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("accessmanager"))
            {
                connObj_AccessManager.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("browser"))
            { 
                connObj_Browser.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("greyofficemobile"))
            {
                connObj_GreyOfficeMobile.Add(ssIMEI, Context.ConnectionId);
            }
            else
            {
                if (ssIMEI.IndexOf("dispatcher_") > -1)
                {
                    connObj_Dispatchers.Add(ssIMEI, Context.ConnectionId);
                }
                //connObj.Add(ssIMEI, Context.ConnectionId);
            }
            SetTaxiDataBlockList(ssIMEI, 0);
            MessageToDebugger("Connected-" + ssIMEI + "-" + sType + "-" + ssProfile + "-" + ssUser + "-" + ssSession + "-[" + Context.ConnectionId + "]");
            /*
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sPacket = "2" + "^^0^" + unixTimestamp + "^^^^^^^0^90^0^|";  // 90 - On connection
            }
            */
            return base.OnConnected();
        }
        protected void MessageToDebugger(string sMess)
        {
            List<string> li = connObj_Browser.GetList("debugger");
            if (li != null)
            {
                li.ForEach(delegate(String connectionid)
                {
                    Clients.Client(connectionid).broadcastDebug("debugger", sMess);
                });
            }
        }
        public override Task OnDisconnected(bool stopCalled)
        {
            String ssIMEI = "", ssProfile = "", ssSession = "", ssUser = "", sType = "";
            if (Context.QueryString["imei"] != null) ssIMEI = Context.QueryString["imei"];
            if (Context.QueryString["type"] != null) sType = Context.QueryString["type"];
            if (Context.QueryString["profile"] != null) ssProfile = Context.QueryString["profile"];
            if (Context.QueryString["user"] != null) ssUser = Context.QueryString["user"];
            if (Context.QueryString["session"] != null) ssSession = Context.QueryString["session"];

            MessageToDebugger("OnDisconnected-" + ssIMEI + "-" + sType + "_" + ssProfile + "-" + ssUser + "-" + ssSession + "-[" + Context.ConnectionId + "]");

            if (sType.Equals("doomdriver"))
            {
                if (connObj_DoomDriver.Exists(ssIMEI)) connObj_DoomDriver.Remove(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("greyofficemobile"))
            {
                if (connObj_GreyOfficeMobile.Exists(ssIMEI)) connObj_GreyOfficeMobile.Remove(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("doomclient"))
            {
                if (connObj_DoomClient.Exists(ssIMEI)) connObj_DoomClient.Remove(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("logisticsclient"))
            {
                if (connObj_LogisticsClients.Exists(ssIMEI)) connObj_LogisticsClients.Remove(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("accessmanager"))
            {
                if (connObj_AccessManager.Exists(ssIMEI)) connObj_AccessManager.Remove(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("browser"))
            {
                if (connObj_Browser.Exists(ssIMEI)) connObj_Browser.Remove(ssIMEI, Context.ConnectionId);
            }else
            {
                if (ssIMEI.IndexOf("dispatcher_") > -1)
                {
                    connObj_Dispatchers.Remove(ssIMEI, Context.ConnectionId);
                }
                //if (connObj.Exists(ssIMEI)) connObj.Remove(ssIMEI, Context.ConnectionId);
            }
            /*
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sPacket = "2" + "^^0^" + unixTimestamp + "^^^^^^^0^91^0^|";  // 91 -  OnDisconnected
            }
            */
            return base.OnDisconnected(stopCalled);
        }
        public override Task OnReconnected()
        {
            String ssIMEI = "", ssProfile = "", ssSession = "", ssUser = "",sType="";
            if (Context.QueryString["imei"] != null) ssIMEI = Context.QueryString["imei"];
            if (Context.QueryString["type"] != null) sType = Context.QueryString["type"];

            if (Context.QueryString["profile"] != null) ssProfile = Context.QueryString["profile"];
            if (Context.QueryString["user"] != null) ssUser = Context.QueryString["user"];
            if (Context.QueryString["session"] != null) ssSession = Context.QueryString["session"];

            MessageToDebugger("OnReconnected-" + ssIMEI + "-" + sType + "-" + ssProfile + "-" + ssUser + "-" + ssSession + "-[" + Context.ConnectionId + "]");

            if (sType.Equals("doomdriver"))
            {
                connObj_DoomDriver.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("doomclient"))
            {
                connObj_DoomClient.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("greyofficemobile"))
            {
                connObj_GreyOfficeMobile.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("logisticsclient"))
            {
                connObj_LogisticsClients.Add(ssIMEI, Context.ConnectionId);
            }
            else if (sType.Equals("accessmanager"))
            {
                connObj_AccessManager.Add(ssIMEI, Context.ConnectionId);
            }
            if (sType.Equals("browser"))
            {
                connObj_Browser.Add(ssIMEI, Context.ConnectionId);
            }else
            {
                //connObj.Add(ssIMEI, Context.ConnectionId);
            }
            /*
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sPacket = "2" + "^^0^" + unixTimestamp + "^^^^^^^0^92^0^|";  // 91 -  OnReconnected
            }
            */
            return base.OnReconnected();
        }
        protected static string GetPure(MySqlDataReader reader, int iIndex)
        {
            if (reader.IsDBNull(iIndex))
            {
                return "";
            }
            else
            {
                return reader.GetString(iIndex);
            }
        }
    }
    /* ************************** ConnectionMapping CLASS  ******************************************** */
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<string, List<T>> _connections = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);
        public void Add(string key, T connectionId)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(key))
                {
                    List<T> li = _connections[key];
                    if (!li.Contains(connectionId)) li.Add(connectionId);
                    //_connections[key] = connectionId;
                }
                else
                {
                    List<T> li = new List<T>();
                    li.Add(connectionId);
                    _connections.Add(key, li);
                }
            }
        }
        public void Remove(string key, T connectionId)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(key))
                {
                    //_connections.Remove(key);
                    List<T> li = _connections[key];
                    if (li.Contains(connectionId)) li.Remove(connectionId);
                    if (li.Count == 0) _connections.Remove(key);
                }
            }
        }
        public int GetCount()
        {
            return _connections.Count();
        }
        public DataTable GetDataTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("key", typeof(string));
            table.Columns.Add("value", typeof(string));
            foreach (var key in _connections.Keys)
            {
                List<T> li = _connections[key];
                table.Rows.Add(key, string.Join(",", li.ToArray()));
            }
            return table;
        }
        public bool Exists(string key)
        {
            return _connections.ContainsKey(key);
        }
        public List<T> GetList(string key)
        {
            if (_connections.ContainsKey(key)) return _connections[key];
            return null;
        }
        public Dictionary<string,List<T>> GetList()
        {
            return _connections;
        }
        /*
        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }
        public string GetValue(T key)
        {
            if (_connections.ContainsKey(key))
            {
                return _connections[key].ToString();
            }
            return "";
        }
         * */
        public string GetListTable()
        {
            String sRet="<table style='margin: 0px; padding: 0px; border-spacing: 0px; width: 100%;'>";
            foreach (var item in _connections)
            {
                sRet += "<tr style='border:1px solid gray;'><td style='vertical-align:top;border:1px solid gray;'>" + item.Key + "</td>";
                List<T> li = item.Value;
                sRet += "<td style='border:1px solid gray;'>";
                //_______________________________________________
                if (li != null)
                {
                    li.ForEach(delegate (T value)
                    {
                        sRet += value + "<br>";
                    });
                }
                //_______________________________________________
                //+string.Join(",", li.ToArray()) +
                sRet += "</td>";
                sRet += "</tr>";
            }
            sRet += "</table>";
            return sRet;
        }
    }

    public class List<T1, T2>
    {
    }

    public class TestData
    {
        public string p1 { get; set; }
        public string p2 { get; set; }
    }
    public class HubObject
    {
        public string Mode { get; set; }
        public string sData { get; set; }
        public string sMess { get; set; }
        public long lData { get; set; }
        public HubObject()
        {
            sData = "";
            lData = 0;
            sMess = "";
        }
    }
}