using MyHub.Hubs;
using MyHub.Models;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public partial class AccessmanagerController : Controller
    {
        [HttpPost]
        public ActionResult LoadMessagesTimes(string profile, string email, string mode, string mess,
            string selectedemailfrom, string selectedemailto, string session, string showall,
            string leavestatus, string mins, string adminview, string page, string pagesize,
            string priority,string search)
        {
            if (page == null) page = "0";
            if (pagesize == null) pagesize = "8";
            if (priority == null) priority = "";
            int iPage = MyGlobal.GetInt16(page);
            int iPageSize = MyGlobal.GetInt16(pagesize);
            if (iPageSize < 8) iPageSize = 8;
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            //leavestatus is empty for OT
            var loadMessagesResponse = new LoadMessagesResponse();
            loadMessagesResponse.status = false;
            loadMessagesResponse.result = "";
            loadMessagesResponse.selectedLeaveStatus = 0;
            loadMessagesResponse.selectedOTStatus = 0;
            loadMessagesResponse.SenderDetails = "";

            string sSQL = "";
            email = email.ToLower();
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------
                    if (adminview.Length > 0) // Admin trying to monitor other's messages from his login
                    {
                        sSQL = "select m_Email from " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + adminview + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            email = reader.GetString(0).ToLower();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------Get sender details
                    string FromEmail = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        FromEmail = selectedemailfrom;
                    else
                        FromEmail = selectedemailto;

                    sSQL = "select m_FName,m_StaffID,m_Status,m_Team,m_Base from " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile = '" + profile + "' and m_Email='" + FromEmail + "' limit 1";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    //Reshwin, CHC0045, Team <b>MSA</b> of <b>Delhi</b> (Active)
                                    loadMessagesResponse.SenderDetails =
                                        GetFldVaue(reader, 0) + ", " + GetFldVaue(reader, 1) +
                                        ", Team<b> " + GetFldVaue(reader, 3) +
                                        "</b> of <b> " + GetFldVaue(reader, 4) +
                                        " </b> (" + GetFldVaue(reader, 2) + ")";

                                }
                            }
                        }
                    }
                    //-----------------------------------
                    string toWhom = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        toWhom = selectedemailfrom;
                    else
                        toWhom = selectedemailto;
                    //------------------------------------------Mark seen fields.....
                    if (session != null && adminview.Length == 0)
                    {
                        if (session.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_messages_clubs Set m_Seen='1' " +
                            "where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                            "and m_Member='" + email + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }

                    //------------------------New test
                    //------------------------------------Before CLUB--------------------------
                    sSQL = "SELECT sessions.*,unseencounts.Counts as UnSeenCounts," +
                        "UNIX_TIMESTAMP(sessions.m_Time) as tmCreated, " +
                        "UNIX_TIMESTAMP(sessions.m_TimeUpdated) as tmUpdated,m_Priority " +
                        "FROM " + MyGlobal.activeDB + ".tbl_messages_sessions sessions ";

                    sSQL += "left join (" +
                    "select m_Session, sum(Case When m_Seen is null Then 1 Else 0 End) as Counts from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Profile = '" + profile + "' and m_Member='" + email + "' group by m_Session " +
                    ") unseencounts on unseencounts.m_Session = sessions.m_Session ";

                    sSQL += "left join (select m_id,m_Session from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Member='" + email + "' and m_Profile = '" + profile + "' group by m_Session) as club on club.m_Session=sessions.m_Session ";

                    sSQL += "where m_Profile='" + profile + "' and m_Type=1 ";

                    if (search != null && search.Length > 0)
                    {
                        sSQL += "and (m_From like '%" + search + "%' " +
                            "or m_FromStaffID like '%" + search + "%' " +
                        "or m_To like '%" + search + "%') ";
                    }
                    if (priority.Equals("1"))
                    {
                        sSQL += "and m_Priority is not null and m_Priority='1' ";
                    }
                    else
                    {
                        sSQL += "and (m_Priority is null or m_Priority<>'1') ";
                    }
                    if (!showall.Equals("true")) sSQL += "and (unseencounts.Counts > 0) ";
                    sSQL += "and club.m_id is not null ";

                    if (session != null) if (session.Length > 0) sSQL += "or sessions.m_Session = '" + session + "' ";

                    sSQL += "order by sessions.m_TimeUpdated desc " +
                        "limit " + iPageSize + " offset " + (iPage * iPageSize) + ";";

                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        ListItem item = new ListItem();
                                        item.EmailFrom = MyGlobal.GetPureString( reader,"m_From");
                                        item.EmailTo = MyGlobal.GetPureString(reader, "m_To");
                                        string locName = "", m_ReportToFunctional = "", 
                                            m_ReportToAdministrative = "";
                                        GetStaffName(profile, item.EmailFrom,out locName,
                                            out m_ReportToFunctional,out m_ReportToAdministrative);
                                        item.NameFrom = locName;
                                        item.staffidFrom = MyGlobal.GetPureString(reader, "m_FromStaffID");

                                        item.NameTo = item.EmailTo;
                                        item.staffidTo = "";
                                        //-----------------------------------------------
                                        item.counts = MyGlobal.GetPureInt16(reader, "UnSeenCounts");
                                        item.tmCreated = MyGlobal.GetPureInt32(reader, "tmCreated") + 19800;
                                        item.tmUpdated = MyGlobal.GetPureInt32(reader, "tmUpdated") + 19800;
                                        item.session = MyGlobal.GetPureString(reader, "m_Session");
                                        int locOTStatus = 0, locMins = 0;
                                        GetOTStatus( out locOTStatus, out locMins,
                                             profile,item.session
                                             );
                                        item.otStatus = locOTStatus;
                                        item.mins = locMins;
                                        //----------------------------Leave status & Code
                                        //----------------------------OT
                                        if (item.session.Equals(session))
                                        {
                                            loadMessagesResponse.selectedOTStatus = item.otStatus;
                                        }
                                        //if (!reader.IsDBNull(14)) item.time = GetDisplayTime(reader.GetDateTime(14));
                                        item.IsAdmin = 0;


                                        if (email.Equals(m_ReportToFunctional, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        if (email.Equals(m_ReportToAdministrative, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        if (email.Equals(item.EmailTo, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        //if (!reader.IsDBNull(18)) item.Days = reader.GetInt16(18);
                                        item.Priority = 0;
                                        if (!reader.IsDBNull(reader.GetOrdinal("m_Priority")))
                                            item.Priority = reader.GetInt16(reader.GetOrdinal("m_Priority"));
                                        //--------------------------------------------
                                        loadMessagesResponse.listItems.Add(item);
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                loadMessagesResponse.result = ex.Message;
                MyGlobal.Error("LoadMessages-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                loadMessagesResponse.result = ex.Message;
                MyGlobal.Error("LoadMessages-Exception-" + ex.Message);
            }
            loadMessagesResponse.selectedSession = session;

            return Json(loadMessagesResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------
        [HttpPost]
        public ActionResult LoadMessagesLeaves(string profile, string email, string mode, string mess,
            string selectedemailfrom, string selectedemailto, string session, string showall,
            string leavestatus, string mins, string adminview, string page, string pagesize,string search)
        {
            if (page == null) page = "0";
            if (pagesize == null) pagesize = "8";
            int iPage = MyGlobal.GetInt16(page);
            int iPageSize = MyGlobal.GetInt16(pagesize);
            if (iPageSize < 8) iPageSize = 8;
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            //leavestatus is empty for OT
            var loadMessagesResponse = new LoadMessagesResponse();
            loadMessagesResponse.status = false;
            loadMessagesResponse.result = "";
            loadMessagesResponse.selectedLeaveStatus = 0;
            loadMessagesResponse.selectedOTStatus = 0;
            loadMessagesResponse.SenderDetails = "";

            string sSQL = "";
            email = email.ToLower();
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------
                    if (adminview.Length > 0) // Admin trying to monitor other's messages from his login
                    {
                        sSQL = "select m_Email from " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + adminview + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            email = reader.GetString(0).ToLower();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------Get sender details
                    string FromEmail = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        FromEmail = selectedemailfrom;
                    else
                        FromEmail = selectedemailto;

                    sSQL = "select m_FName,m_StaffID,m_Status,m_Team,m_Base from " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile = '" + profile + "' and m_Email='" + FromEmail + "' limit 1";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    //Reshwin, CHC0045, Team <b>MSA</b> of <b>Delhi</b> (Active)
                                    loadMessagesResponse.SenderDetails =
                                        GetFldVaue(reader, 0) + ", " + GetFldVaue(reader, 1) +
                                        ", Team<b> " + GetFldVaue(reader, 3) +
                                        "</b> of <b> " + GetFldVaue(reader, 4) +
                                        " </b> (" + GetFldVaue(reader, 2) + ")";

                                }
                            }
                        }
                    }
                    //-----------------------------------
                    string toWhom = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        toWhom = selectedemailfrom;
                    else
                        toWhom = selectedemailto;
                    //------------------------------------------Mark seen fields.....
                    if (session != null && adminview.Length == 0)
                    {
                        if (session.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_messages_clubs Set m_Seen='1' " +
                            "where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                            "and m_Member='" + email + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    //-------------------------------------------------------------------------
                    /*
                    sSQL = "SELECT message.m_From,message.m_To,'staffFrom.m_FName','staffTo.m_FName'," +    //0-3
                    "unseencounts.Counts,''," +     // 4,5
                    "message.m_Year,message.m_Month,message.m_Day,message.m_Session,message.m_Time," + // 6-10
                    "'ot.m_OTStatus','staffFrom.m_StaffID','staffTo.m_StaffID',message.m_Time," +  // 11-14
                    "'staffFrom.m_ReportToFunctional','staffFrom.m_ReportToAdministrative'," + // 15,16
                    "'lev.stat',message.m_Days,'lev.type','ot.m_OTDuration', " + // 17-20
                    "'staffTo.m_ReportToFunctional',message.m_StaffID,message.m_LeaveType,message.m_LeaveStatus,'lev.*' " + // 21,22,23,24 
                    "FROM " + MyGlobal.activeDB + ".tbl_messages message ";
                    //------------------------------------Before CLUB--------------------------

                    sSQL += "left join (" +
                    "select m_Session, sum(Case When m_Seen is null Then 1 Else 0 End) as Counts from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Profile = '" + profile + "' and m_Member='" + email + "' group by m_Session " +
                    ") unseencounts on unseencounts.m_Session = message.m_Session ";
                    //--------------------------------------CLUB END---------------------------
                    sSQL += "left join (select m_id,m_Session from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Member='" + email + "' and m_Profile = '" + profile + "') as club on club.m_Session=message.m_Session ";

                    //-------------------------------------------------------------------------
                    sSQL += "where message.m_Year is not null and message.m_Month is not null " +
                        "and message.m_Day is not null and message.m_StaffID is not null " +
                        "and message.m_Session is not null and m_LeaveType is not null ";
                    if (!showall.Equals("true")) sSQL += "and (unseencounts.Counts > 0) ";
                    sSQL += "and club.m_id is not null ";
                    if (session != null) if (session.Length > 0) sSQL += "or message.m_Session = '" + session + "' ";
                    sSQL += "group by m_Session order by message.m_Time desc " +
                        "limit " + iPageSize + " offset " + (iPage * iPageSize) + ";";
                    //-------------------------------------------------------------------------
                    */
                    sSQL = "SELECT sessions.*,unseencounts.Counts as UnSeenCounts," +
                        "UNIX_TIMESTAMP(sessions.m_Time) as tmCreated, " +
                        "UNIX_TIMESTAMP(sessions.m_TimeUpdated) as tmUpdated " +
                        "FROM " + MyGlobal.activeDB + ".tbl_messages_sessions sessions ";

                    sSQL += "left join (" +
                    "select m_Session, sum(Case When m_Seen is null Then 1 Else 0 End) as Counts from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Profile = '" + profile + "' and m_Member='" + email + "' group by m_Session " +
                    ") unseencounts on unseencounts.m_Session = sessions.m_Session ";

                    sSQL += "left join (select m_id,m_Session from " + MyGlobal.activeDB + ".tbl_messages_clubs  where m_Member='" + email + "' and m_Profile = '" + profile + "' group by m_Session) as club on club.m_Session=sessions.m_Session ";

                    sSQL += "where m_Profile='" + profile + "' and m_Type=2 ";
                    if (search != null && search.Length > 0)
                    {
                        sSQL += "and (m_From like '%" + search + "%' " +
                            "or m_FromStaffID like '%" + search + "%' "+
                        "or m_To like '%" + search + "%') ";
                    }
                    if (!showall.Equals("true")) sSQL += "and (unseencounts.Counts > 0) ";
                    sSQL += "and club.m_id is not null ";

                    if (session != null) if (session.Length > 0) sSQL += "or sessions.m_Session = '" + session + "' ";

                    sSQL += "order by sessions.m_TimeUpdated desc " +
                        "limit " + iPageSize + " offset " + (iPage * iPageSize) + ";";


                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        ListItem item = new ListItem();
                                        item.EmailFrom = MyGlobal.GetPureString(reader, "m_From");
                                        item.EmailTo = MyGlobal.GetPureString(reader, "m_To");
                                        string locName = "", m_ReportToFunctional = "",
                                            m_ReportToAdministrative = "";
                                        GetStaffName(profile, item.EmailFrom, out locName,
                                            out m_ReportToFunctional, out m_ReportToAdministrative);
                                        item.NameFrom = locName;
                                        item.staffidFrom = MyGlobal.GetPureString(reader, "m_FromStaffID");

                                        item.NameTo = item.EmailTo;
                                        item.staffidTo = "";
                                        //-----------------------------------------------
                                        item.counts = MyGlobal.GetPureInt16(reader, "UnSeenCounts");
                                        item.tmCreated = MyGlobal.GetPureInt32(reader, "tmCreated") + 19800;
                                        item.tmUpdated = MyGlobal.GetPureInt32(reader, "tmUpdated") + 19800;
                                        item.session = MyGlobal.GetPureString(reader, "m_Session");

                                        item.param1 = MyGlobal.GetPureString(reader, "m_Param1"); // Selected Date
                                        item.param2 = MyGlobal.GetPureString(reader, "m_Param2"); // Days
                                        

                                        int locLeaveStatus = 0;
                                        string locLeaveType = "";
                                        GetLeaveStatus(out locLeaveStatus,out locLeaveType,
                                             profile, item.staffidFrom, item.param1
                                             );
                                        item.otStatus = locLeaveStatus;
                                        //item.mins = locMins;
                                        // Take status from leave table than from message field
                                        item.param3 = locLeaveType;// MyGlobal.GetPureString(reader, "m_Param3"); // Leave Type
                                        //----------------------------Leave status & Code
                                        if (item.session.Equals(session))
                                        {
                                            loadMessagesResponse.selectedOTStatus = item.otStatus;
                                        }
                                        //if (!reader.IsDBNull(14)) item.time = GetDisplayTime(reader.GetDateTime(14));
                                        item.IsAdmin = 0;


                                        if (email.Equals(m_ReportToFunctional, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        if (email.Equals(m_ReportToAdministrative, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        if (email.Equals(item.EmailTo, StringComparison.CurrentCultureIgnoreCase))
                                        { // If ToStaff  is self, IsAdmin=1
                                            item.IsAdmin = 1;
                                        }

                                        //if (!reader.IsDBNull(18)) item.Days = reader.GetInt16(18);
                                        //--------------------------------------------
                                        loadMessagesResponse.listItems.Add(item);
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }


                }
            }
            catch (MySqlException ex)
            {
                loadMessagesResponse.result = ex.Message;
                MyGlobal.Error("LoadMessages-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                loadMessagesResponse.result = ex.Message;
                MyGlobal.Error("LoadMessages-Exception-" + ex.Message);
            }
            loadMessagesResponse.selectedSession = session;

            return Json(loadMessagesResponse, JsonRequestBehavior.AllowGet);
        }
        //----------------------------------------


        //----------------------------------------                
        private void GetStaffName(string profile, string email, out string locName,
            out string m_ReportToFunctional, out string m_ReportToAdministrative)
        {
            locName = email;
            m_ReportToFunctional = "";
            m_ReportToAdministrative = "";
            string sSQL = "select m_FName,m_ReportToFunctional,m_ReportToAdministrative " +
                "from " + MyGlobal.activeDB + ".tbl_staffs " +
                "where m_Profile = '" + profile + "' and m_Email='" + email + "'";

            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) locName = reader.GetString(0);
                                if (!reader.IsDBNull(1)) m_ReportToFunctional = reader.GetString(1);
                                if (!reader.IsDBNull(2)) m_ReportToAdministrative = reader.GetString(2);
                            }
                        }
                    }

                }
            }
        }
        private void GetOTStatus(out int locOTStatus, out int locMins,
            string profile, string m_Session)
        {
            locOTStatus = 0;
            locMins = 0;

            string sSQL = "select m_OTStatus,m_OTDuration from " + MyGlobal.activeDB + ".tbl_ot " +
                "where m_Profile = '" + profile + "' " +
                "and m_Session='" + m_Session + "' ";
                //"and m_OTStatus is not null " +
                //"and m_OTStatus <> 9 and m_OTStatus <> 2 "+
                //"limit 1";
            
            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) locOTStatus = reader.GetInt16(0);
                                if (!reader.IsDBNull(1)) locMins = reader.GetInt32(1);
                            }
                        }
                    }

                }
            }
        }
        private void GetLeaveStatus(out int locLeaveStatus,out string locLeaveType,
    string profile, string staffid, string date)
        {
            locLeaveType = "";
            locLeaveStatus = 0;

            string[] arDate = date.Split('-');
            if (arDate.Length != 3) return;

            string sSQL = "select m_DayL" + arDate[0] + ",m_Status" + arDate[0] + " from " + MyGlobal.activeDB + ".tbl_leaves " +
                "where m_Profile = '" + profile + "' " +
                "and m_StaffID='" + staffid + "' " +
                "and m_Year='" + arDate[2] + "' " +
                "and m_Month='" + (MyGlobal.GetInt16(arDate[1]) - 1) + "' ";


            using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                con.Open();
                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                {
                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) locLeaveType = reader.GetString(0);
                                if (!reader.IsDBNull(1)) locLeaveStatus = reader.GetInt16(1);
                            }
                        }
                    }

                }
            }
        }
        //----------------------------------------
        [HttpPost]
        public ActionResult LoadChat(string profile, string email, string mode, string session,
            string selectedemailfrom, string selectedemailto, string mess, string leavestatus,
            string mins, string adminview,string reqfrom) /* reqfrom = leave or time*/
        {
            var loadChatResponse = new LoadChatResponse();
            loadChatResponse.status = false;
            loadChatResponse.result = "";
            if (selectedemailfrom == null) selectedemailfrom = email;
            if (selectedemailto == null) selectedemailto = email;
            string sSQL = "";
            email = email.ToLower();
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //-----------------------------------
                    if (adminview.Length > 0) // Admin trying to monitor other's messages from his login
                    {
                        sSQL = "select m_Email from " + MyGlobal.activeDB + ".tbl_staffs " +
                            "where m_Profile = '" + profile + "' and m_StaffID='" + adminview + "' limit 1";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    if (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            email = reader.GetString(0).ToLower();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //-----------------------------------Get sender details
                    string FromEmail = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        FromEmail = selectedemailfrom;
                    else
                        FromEmail = selectedemailto;

                    sSQL = "select m_FName,m_StaffID,m_Status,m_Team,m_Base from " + MyGlobal.activeDB + ".tbl_staffs " +
                    "where m_Profile = '" + profile + "' and m_Email='" + FromEmail + "' limit 1";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    //Reshwin, CHC0045, Team <b>MSA</b> of <b>Delhi</b> (Active)
                                    loadChatResponse.SenderDetails =
                                        GetFldVaue(reader, 0) + ", " + GetFldVaue(reader, 1) +
                                        ", Team<b> " + GetFldVaue(reader, 3) +
                                        "</b> of <b> " + GetFldVaue(reader, 4) +
                                        " </b> (" + GetFldVaue(reader, 2) + ")";

                                }
                            }
                        }
                    }
                    //-----------------------------------
                    string toWhom = "";
                    if (email.Equals(selectedemailto, StringComparison.CurrentCultureIgnoreCase))
                        toWhom = selectedemailfrom;
                    else
                        toWhom = selectedemailto;
                    //------------------------------------------
                    if (mode.Equals("newmessage"))
                    {
                        sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_From,m_To,m_Message,m_Time,m_Session) " +
                            "values ('" + profile + "','" + email + "'," +
                            "'" + toWhom + "'," +
                            "'" + mess + "',Now(),'" + session + "');";

                        sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
"Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
"and m_Session='" + session + "';";

                        sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_clubs Set m_Seen=null " +
                            "where m_Profile='" + session + "' and m_Session='" + session + "';";

                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
        "values ('" + profile + "','" + session + "','" + email + "');";
                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
"values ('" + profile + "','" + session + "','" + toWhom + "');";


                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            loadChatResponse.bReload = true;
                        }
                        //--------------------------------------
                        MyGlobal.SendHubObject(toWhom, GetPendingMessagesObject(con, profile, "times", toWhom));
                    }
                    else if (mode.Equals("approve"))
                    {
                        if (reqfrom.Equals("time"))
                        {   // OT
                            //MyGlobal.Log("point 1 >"+ watch.ElapsedMilliseconds);

                            if (MyGlobal.GetInt16(mins) == 0)
                            {
                                SetOTStatus(con, profile, session, C_ACCEPTED, email, toWhom, mins);
                            }
                            else
                            {
                                SetOTStatus(con, profile, session, C_APPROVED, email, toWhom, mins);
                            }
                            //MyGlobal.Log("point 2 >" + watch.ElapsedMilliseconds);
                        }
                        else
                        {   // Leave
                            char[] delimiterChars = { '_' };    //20002_2019_2_23
                            string[] arData = session.Split(delimiterChars);
                            String year = "", month = "", staffid = "";
                            int iSelectedDay = 0;
                            if (arData.Length >= 4)
                            {
                                year = arData[1];
                                month = arData[2];
                                iSelectedDay = MyGlobal.GetInt16(arData[3]);
                                staffid = arData[0];
                                //--------------------------Get No of days from message head field
                                int dblSelectedDays = GetNoOfDays(con, profile, session);
                                //--------------------------Apply in leave table
                                int iMonthForDB = (MyGlobal.GetInt16(month) - 1);
                                int iYearForDB = MyGlobal.GetInt16(year);
                                bool bContinueToNextMonth = false;
                                int iDaysExhausted = 0;
                                //----------Get leave type from session-------------------------
                                string sLeaveType = "";
                                sSQL = "select m_LeaveType from " + MyGlobal.activeDB + ".tbl_messages " +
                                    "where m_Profile = '" + profile + "' and m_Year='" + iYearForDB + "' and m_Month='" + iMonthForDB + "' " +
                                    "and m_Day='" + iSelectedDay + "' and m_StaffID='" + staffid + "' and m_Session='" + session + "' " +
                                    "limit 1";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            if (reader.Read())
                                            {
                                                if (!reader.IsDBNull(reader.GetOrdinal("m_LeaveType")))
                                                {
                                                    sLeaveType = reader.GetString(reader.GetOrdinal("m_LeaveType"));
                                                }
                                            }
                                        }
                                    }
                                }
                                //--------------------------------------------------------------
                                //----Set leaves table
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                                string sBit = "";
                                for (int i = iSelectedDay; i < (iSelectedDay + dblSelectedDays); i++)
                                {
                                    if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                                    {
                                        if (sBit.Length > 0) sBit += ",";
                                        sBit += "m_Status" + i + "='9'";
                                        iDaysExhausted++;
                                    }
                                    else
                                    {
                                        bContinueToNextMonth = true;
                                        break;
                                    }
                                }
                                sSQL += sBit;
                                sSQL += " where " +
                                "m_Profile = '" + profile + "' " +
                                "and m_Year='" + year + "' and m_Month='" + iMonthForDB + "' " +
                                "and m_StaffID='" + staffid + "';";
                                //----Set leaves table, month cross over
                                if (bContinueToNextMonth)
                                {
                                    iMonthForDB++;
                                    if (iMonthForDB > 11)
                                    {
                                        iMonthForDB = 0;
                                        iYearForDB++;
                                    }
                                    sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                                    sBit = "";
                                    for (int i = 1; i <= (dblSelectedDays - iDaysExhausted); i++)
                                    {
                                        if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                                        {
                                            if (sBit.Length > 0) sBit += ",";
                                            sBit += "m_Status" + i + "='9'";
                                        }
                                        else
                                        {
                                            bContinueToNextMonth = true;
                                            break;
                                        }
                                    }
                                    sSQL += sBit;
                                    sSQL += " where " +
                                    "m_Profile = '" + profile + "' " +
                                    "and m_Year='" + year + "' and m_Month='" + iMonthForDB + "' " +
                                    "and m_StaffID='" + staffid + "';";
                                }
                                //----------------------------
                                if (sLeaveType.IndexOf('/') == -1) // Make roster table NULL, except half day leaves
                                {
                                    //----Set tbl_rosters table NULL, because, leave is confirmed
                                    iMonthForDB = (MyGlobal.GetInt16(month) - 1);
                                    iYearForDB = MyGlobal.GetInt16(year);
                                    bContinueToNextMonth = false;
                                    iDaysExhausted = 0;

                                    sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set ";
                                    sBit = "";
                                    for (int i = iSelectedDay; i < (iSelectedDay + dblSelectedDays); i++)
                                    {
                                        if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                                        {
                                            if (sBit.Length > 0) sBit += ",";
                                            sBit += "m_Day" + i + "=null";
                                        }
                                        else
                                        {
                                            bContinueToNextMonth = true;
                                            break;
                                        }
                                    }
                                    sSQL += sBit;
                                    sSQL += " where " +
                                    "m_Profile = '" + profile + "' " +
                                    "and m_Year='" + year + "' and m_Month='" + iMonthForDB + "' " +
                                    "and m_StaffID='" + staffid + "';";
                                    //----Set tbl_rosters table, cross over
                                    if (bContinueToNextMonth)
                                    {

                                        iMonthForDB++;
                                        if (iMonthForDB > 11)
                                        {
                                            iMonthForDB = 0;
                                            iYearForDB++;
                                        }
                                        sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set ";
                                        sBit = "";
                                        for (int i = 1; i < (dblSelectedDays - iDaysExhausted); i++)
                                        {
                                            if (IsThisDateValid(iYearForDB, iMonthForDB, i))
                                            {
                                                if (sBit.Length > 0) sBit += ",";
                                                sBit += "m_Day" + i + "=null";
                                            }
                                            else
                                            {
                                                bContinueToNextMonth = true;
                                                break;
                                            }
                                        }
                                        sSQL += sBit;
                                        sSQL += " where " +
                                        "m_Profile = '" + profile + "' " +
                                        "and m_Year='" + year + "' and m_Month='" + iMonthForDB + "' " +
                                        "and m_StaffID='" + staffid + "';";
                                    }
                                }
                                String sErrMessage = "";
                                MySqlTransaction myTrans = con.BeginTransaction();
                                MySqlCommand myCommand = con.CreateCommand();
                                myCommand.Connection = con;
                                myCommand.Transaction = myTrans;
                                try
                                {
                                    
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();

                                    //------------------Update message head. NOt much used
                                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_messages Set m_LeaveStatus='9' where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                                    "and m_Year is not null and m_Month is not null and m_Day is not null and m_StaffID is not null and m_Session is not null;";
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();
                                    //-----------------Create message
                                    string sFName, sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                                    GetStaffDetails_FromStaffID(
                                        con, profile, staffid,
                                        out sFName, out sStaffEmail,
                                        out sReportAdminEmail, out sReportFuncEmail, out sErrMessage);
                                    if (sErrMessage.Length == 0)
                                    {
                                        string sEmailName = "";
                                        GetStaffDetails_FromEmail(con, profile, email, out sEmailName);
                                        string message = "<span style=''color:darkgreen;''><b>Approved by " + sEmailName + "</b></span>";
                                        sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message,m_Time,m_Session) " +
                                            "values ('" + profile + "','" + "" + "','" + email + "','" + toWhom + "'," +
                                            "'" + message + "',Now(),'" + session + "');";

                                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                                        if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                                        if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                                        myCommand.CommandText = sSQL;
                                        myCommand.ExecuteNonQuery();
                                        //--------------------------------------
                                        myTrans.Commit();
                                        HubObject hub = GetPendingMessagesObject(con, profile, "leaves", toWhom);
                                        SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                                        loadChatResponse.bReload = true;
                                    }
                                    else
                                    {
                                        myTrans.Rollback();
                                        loadChatResponse.result = sErrMessage;
                                    }
                                }
                                catch (Exception e)
                                {
                                    try
                                    {
                                        myTrans.Rollback();
                                        sErrMessage = "Failed. (" + e.Message + ")";
                                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                                    }
                                    catch (MySqlException ex)
                                    {
                                        if (myTrans.Connection != null)
                                        {
                                            Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                                        }
                                    }
                                    Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                                    Console.WriteLine("Neither record was written to database.");
                                }
                                finally
                                {
                                    //myConnection.Close();
                                }
                            }
                            else
                            {
                                loadChatResponse.result = "Invalid Leave Session";
                            }
                        }
                        
                    }
                    else if (mode.Equals("revoke"))
                    {
                        char[] delimiterChars = { '_' };    //20002_2019_2_23
                        string[] arData = session.Split(delimiterChars);
                        String year = "", month = "", staffid = "", sErrMessage = "";
                        int day = 0;
                        if (arData.Length >= 4)
                        {
                            year = arData[1];
                            month = arData[2];
                            day = MyGlobal.GetInt16(arData[3]);
                            staffid = arData[0];
                            //--------------------------Get No of days from message head field
                            int iNoOfDays = GetNoOfDays(con, profile, session);
                            //--------------------------Apply in leave table
                            MySqlTransaction myTrans = con.BeginTransaction();
                            MySqlCommand myCommand = con.CreateCommand();
                            myCommand.Connection = con;
                            myCommand.Transaction = myTrans;
                            try
                            {
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                                string sBit = "";
                                for (int i = day; i < day + iNoOfDays; i++)
                                {
                                    if (sBit.Length > 0) sBit += ",";
                                    sBit += "m_Status" + i + "=null,m_DayL" + i + "=null";
                                }
                                sSQL += sBit;
                                sSQL += " where " +
                                "m_Profile = '" + profile + "' " +
                                "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                                "and m_StaffID='" + staffid + "';";
                                //-------------------------Marke roster
                                sSQL += "UPDATE " + MyGlobal.activeDB + ".tbl_rosters Set ";
                                sBit = "";
                                for (int i = day; i < day + iNoOfDays; i++)
                                {
                                    if (sBit.Length > 0) sBit += ",";
                                    sBit += "m_Day" + i + "='" + MyGlobal.WORKDAY_MARKER + "'";
                                }
                                sSQL += sBit;
                                sSQL += " where " +
                                "m_Profile = '" + profile + "' " +
                                "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                                "and m_StaffID='" + staffid + "';";

                                myCommand.CommandText = sSQL;
                                myCommand.ExecuteNonQuery();

                                string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                                GetStaffDetails_FromStaffID(
                                    con, profile, staffid,
                                    out sFName, out sStaffEmail, out sReportAdminEmail,
                                    out sReportFuncEmail, out sErrMessage);
                                if (sErrMessage.Length == 0)
                                {
                                    string sEmailName = "";
                                    GetStaffDetails_FromEmail(con, profile, email, out sEmailName);
                                    string message = "<span style=''font-weight:bold;color:red;''>Leave Cancelled by " + sEmailName + "</span>";
                                    sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message,m_Time,m_Session) " +
                                        "values ('" + profile + "','" + "" + "','" + email + "','" + toWhom + "'," +
                                        "'" + message + "',Now(),'" + session + "');";

                                    sSQL += "update " + MyGlobal.activeDB + ".tbl_messages_sessions " +
    "Set m_TimeUpdated=Now() where m_Profile='" + profile + "' " +
    "and m_Session='" + session + "';";


                                    sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                            "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                                    if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                            "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                                    if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                            "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();
                                    myTrans.Commit();
                                    loadChatResponse.bReload = true;
                                    //--------------------------------------
                                    //SendHubObject(toWhom, GetPendingMessagesObject(con, profile, toWhom));
                                    HubObject hub = GetPendingMessagesObject(con, profile, "leaves", toWhom);
                                    SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                                }
                                else
                                {
                                    myTrans.Rollback();
                                    loadChatResponse.result = sErrMessage;
                                }
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    myTrans.Rollback();
                                    sErrMessage = "Failed. (" + e.Message + ")";
                                    return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                                }
                                catch (MySqlException ex)
                                {
                                    if (myTrans.Connection != null)
                                    {
                                        Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                                    }
                                }
                                Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                                Console.WriteLine("Neither record was written to database.");
                            }
                            finally
                            {
                                //myConnection.Close();
                            }
                        }
                        else
                        {
                            loadChatResponse.result = "Invalid Leave Session";
                        }
                    }
                    else if (mode.Equals("reject"))
                    {
                        if (reqfrom.Equals("time"))
                        {   // OT
                            SetOTStatus(con, profile, session, C_REJECTED, email, toWhom, mins);
                        }
                        else
                        {   // Leave
                            char[] delimiterChars = { '_' };    //20002_2019_2_23
                            string[] arData = session.Split(delimiterChars);
                            String year = "", month = "", staffid = "", sErrMessage="";
                            int day = 0;
                            if (arData.Length >= 4)
                            {
                                year = arData[1];
                                month = arData[2];
                                day = MyGlobal.GetInt16(arData[3]);
                                staffid = arData[0];
                                //--------------------------Get No of days from message head field
                                int iNoOfDays = GetNoOfDays(con, profile, session);
                                //--------------------------Reject
                                sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_leaves Set ";
                                string sBit = "";
                                for (int i = day; i < day + iNoOfDays; i++)
                                {
                                    if (sBit.Length > 0) sBit += ",";
                                    sBit += "m_Status" + i + "='2'";
                                }
                                sSQL += sBit;
                                sSQL += " where " +
                                "m_Profile = '" + profile + "' " +
                                "and m_Year='" + year + "' and m_Month='" + (MyGlobal.GetInt16(month) - 1) + "' " +
                                "and m_StaffID='" + staffid + "';";
                                MySqlTransaction myTrans = con.BeginTransaction();
                                MySqlCommand myCommand = con.CreateCommand();
                                myCommand.Connection = con;
                                myCommand.Transaction = myTrans;
                                try
                                {
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();

                                    sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_messages Set m_LeaveStatus='2' where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                                    "and m_Year is not null and m_Month is not null and m_Day is not null and m_StaffID is not null and m_Session is not null;";
                                    myCommand.CommandText = sSQL;
                                    myCommand.ExecuteNonQuery();

                                    string sFName = "", sStaffEmail = "", sReportAdminEmail = "", sReportFuncEmail = "";
                                    GetStaffDetails_FromStaffID(con, profile,
                                        staffid, out sFName, out sStaffEmail,
                                        out sReportAdminEmail, out sReportFuncEmail, out sErrMessage);
                                    if (sErrMessage.Length == 0)
                                    {
                                        string sEmailName = "";
                                        GetStaffDetails_FromEmail(con, profile, email, out sEmailName);
                                        string message = "<span style=''font-weight:bold;color:red;''>Rejected by " + sEmailName + "</span>";
                                        sSQL = "INSERT into " + MyGlobal.activeDB + ".tbl_messages (m_Profile,m_StaffID,m_From,m_To,m_Message,m_Time,m_Session) " +
                                            "values ('" + profile + "','" + "" + "','" + email + "','" + toWhom + "'," +
                                            "'" + message + "',Now(),'" + session + "');";

                                        sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sStaffEmail + "');";
                                        if (sReportAdminEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sReportAdminEmail + "');";
                                        if (sReportFuncEmail.Length > 5) sSQL += "INSERT into " + MyGlobal.activeDB + ".tbl_messages_clubs (m_Profile,m_Session,m_Member) " +
                                                "values ('" + profile + "','" + session + "','" + sReportFuncEmail + "');";
                                        myCommand.CommandText = sSQL;
                                        myCommand.ExecuteNonQuery();
                                        myTrans.Commit();
                                        //--------------------------------------
                                        //SendHubObject(toWhom, GetPendingMessagesObject(con, profile, toWhom));
                                        HubObject hub = GetPendingMessagesObject(con, profile, "leaves", toWhom);
                                        SendHubObjects(new string[] { sStaffEmail, sReportAdminEmail, sReportFuncEmail }, hub);
                                        loadChatResponse.bReload = true;
                                    }
                                    else
                                    {
                                        loadChatResponse.result = sErrMessage;
                                    }
                                }
                                catch (Exception e)
                                {
                                    try
                                    {
                                        myTrans.Rollback();
                                        sErrMessage = "Failed. (" + e.Message + ")";
                                        return LoadLeaveData(profile, year, month, staffid, sErrMessage);
                                    }
                                    catch (MySqlException ex)
                                    {
                                        if (myTrans.Connection != null)
                                        {
                                            Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while attempting to roll back the transaction.");
                                        }
                                    }
                                    Console.WriteLine("An exception of type " + e.GetType() + " was encountered while inserting the data.");
                                    Console.WriteLine("Neither record was written to database.");
                                }
                                finally
                                {
                                    //myConnection.Close();
                                }

                            }
                            else
                            {
                                loadChatResponse.result = "Invalid Leave Session";
                            }
                        }
                        
                    }
                    else if (mode.Equals("accept"))
                    {
                        if (reqfrom.Equals("time"))
                        {   // OT
                            SetOTStatus(con, profile, session, C_ACCEPTED, email, toWhom, mins);
                        }
                        else
                        {   // Leave
                        }
                        loadChatResponse.bReload = true;
                    }
                    //------------------------------------------Mark seen fields.....

                    if (session != null && adminview.Length == 0)
                    {
                        if (session.Length > 0)
                        {
                            sSQL = "UPDATE " + MyGlobal.activeDB + ".tbl_messages_clubs Set m_Seen='1' " +
                            "where m_Profile='" + profile + "' and m_Session='" + session + "' " +
                            "and m_Member='" + email + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) mySqlCommand.ExecuteNonQuery();
                        }
                    }
                    //MyGlobal.Log("point 3 >" + watch.ElapsedMilliseconds);
                    //-----------------------------------
                    sSQL = "SELECT * FROM " + MyGlobal.activeDB + ".tbl_messages where m_Profile='" + profile + "' " +
                            "and m_Session='" + session + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    int ordinal = reader.GetOrdinal("m_Message");
                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        Message message = new Message();
                                        ordinal = reader.GetOrdinal("m_From");
                                        //int ordinalTo= reader.GetOrdinal("m_To");
                                        string emailFrom = reader.GetString(ordinal).ToLower();

                                        //if(!reader.IsDBNull(ordinalTo)) emailTo = reader.GetString(ordinalTo).ToLower();
                                        if (emailFrom.Equals(email))
                                        {
                                            message.MySelf = 1;
                                            message.image = profile + "_" + emailFrom;
                                        }
                                        else
                                        {
                                            message.MySelf = 0;
                                            message.image = profile + "_" + emailFrom;
                                        }
                                        //, StringComparison.CurrentCultureIgnoreCase
                                        ordinal = reader.GetOrdinal("m_Message");
                                        message.sMessage = reader.GetString(ordinal);
                                        message.sTime = Convert.ToDateTime(reader["m_Time"]).ToString("yyyy-MM-dd HH:mm:ss");
                                        message.By = MyGlobal.GetPureString(reader, "m_From");
                                        loadChatResponse.messages.Add(message);
                                    }
                                }
                                loadChatResponse.status = true;
                            }
                            else
                            {

                            }
                        }
                    }
                    //MyGlobal.Log("point 4 >" + watch.ElapsedMilliseconds);
                }
            }
            catch (MySqlException ex)
            {
                loadChatResponse.result = ex.Message;
                MyGlobal.Error("LoadChat-MySqlException-" + ex.Message);
            }
            catch (Exception ex)
            {
                loadChatResponse.result = ex.Message;
                MyGlobal.Error("LoadChat-Exception-" + ex.Message);
            }
            loadChatResponse.selectedSession = session;

            return Json(loadChatResponse, JsonRequestBehavior.AllowGet);
        }
    }
}