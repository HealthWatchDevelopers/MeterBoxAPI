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
using Microsoft.AspNet.SignalR.Hubs;

namespace MyHub.Hubs
{
    public partial class ChatHub : Hub
    {
        public static Dictionary<string, string> _StaffsLocation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public void LifeToHub(string sDeviceID, IAmOK iAmOK)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQL = "update " + MyGlobal.activeDB + ".tbl_terminals " +
                        "Set m_ActivityTime=UNIX_TIMESTAMP() where m_HardwareID='" + sDeviceID + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex1)
            {

            }
            HubObject hubObject = new HubObject();
            /*
             *  _StaffsLocation  STAFFID-DEVICEID
             * 
             */
             /*
            if (iAmOK.StaffID.Length > 0)
            {
                if (_StaffsLocation.ContainsKey(iAmOK.StaffID))
                {
                    if (!_StaffsLocation[iAmOK.StaffID].Equals(sDeviceID))
                    {   // Staff logged in from different hardware

                        hubObject.Mode = "logout";
                        SendHubObject_ToTerminal(_StaffsLocation[iAmOK.StaffID], hubObject);
                        MessageToDebugger("LifeToHub-Command to " + _StaffsLocation[iAmOK.StaffID] + " to logout[" + iAmOK.StaffID + "]");
                        _StaffsLocation.Add(iAmOK.StaffID, sDeviceID);
                        return;
                    }
                }
                else
                {
                    _StaffsLocation.Add(iAmOK.StaffID, sDeviceID);
                }
            }
            */
            hubObject.Mode = "thanks";
            SendHubObject_ToTerminal(sDeviceID, hubObject);

            MessageToDebugger("LifeToHub-"+sDeviceID+"__"+ iAmOK.StaffID);
        }
        private void SendHubObject_ToTerminal(string terminal, HubObject obj)
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            List<String> connections = hub.GetAccessManagerConnections(terminal);
            if (connections != null)
            {
                foreach (String connectionID in connections)
                {
                    hubContext.Clients.Client(connectionID).HubToAccessManager(obj);
                }
            }
        }
    }
    public class IAmOK
    {
        public string StaffID { get; set; }
    }
}