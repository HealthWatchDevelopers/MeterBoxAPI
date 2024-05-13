using MyHub.Models;
using MySql.Data.MySqlClient;
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
        public ActionResult ManageTeams(string key, string profile, string selected, string mode, 
            string name, string head, string description, string locktime, 
            string payindex, string physicalpresence,
            string shiftstarttime, string shiftendtime,string ifsc,string bankbranch,
            string type, string positions,string holidaygroup,string lat,string lng,string accuracy,string geolocation)
        {
            if (MyGlobal.activeDB.Length == 0) MyGlobal.GetDomain();
            if (key == null) key = "";
            if (key.Equals("Team")) return ManageTeams_Sub(profile, selected, mode, name, head, description, locktime, geolocation);
            //if (key.Equals("Roll")) return ManageRolls_Sub(profile, selected, mode, name, head, description,locktime);
            if (key.Equals("Base")) return ManageBases_Sub(profile, selected, mode, name, head, description);
            if (key.Equals("Band")) return ManageBands_Sub(profile, selected, mode, name, description);
            //if (key.Equals("Grade")) return ManageGrades_Sub(profile, selected, mode, name, description);
            if (key.Equals("Title")) return ManageDesignations_Sub(profile, selected, mode, name, head, description);
            if (key.Equals("Base")) return ManageBases_Sub(profile, selected, mode, name, head, description);
            if (key.Equals("Breaks")) return ManageBreaks_Sub(profile, selected, mode, name, head, description);
            //if (key.Equals("Payscales")) return ManagePayscales_Sub(profile, selected, mode, name, head, description);
            if (key.Equals("RosterOptions")) return ManageRosterOptions_Sub(profile, selected, mode, name, head, description, payindex, physicalpresence);
            if (key.Equals("ShiftNames")) return ManageShiftNames_Sub(profile, selected, mode, name, head, description, shiftstarttime,shiftendtime);
            if (key.Equals("StaffBanks")) return ManageStaffBanks_Sub(profile, selected, mode, name, head, description, ifsc, bankbranch);
            if (key.Equals("Holidays")) return ManageHolidays_Sub(profile, selected, mode, name, head, description, type, positions, holidaygroup);
            if (key.Equals("GeoLocations")) return ManageGeoLocations_Sub(profile, selected, mode, name, head, description, lat, lng, accuracy);

            //return ManageTeams_Sub(profile, selected, mode, name, head, description);

            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            manageTeamsResponse.mode = mode;
            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManageBands(string profile,string selected, string selectedsub, string mode)
        {
            var manageBandsResponse = new ManageBandsResponse();
            manageBandsResponse.status = false;
            manageBandsResponse.result = "";
            string sSQL = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    string sSQLComm = "";
                    bool bExists = false;
                    if (mode.Equals("newband"))
                    {
                        sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) bExists = reader.HasRows;
                        selected = "new";
                        if (!bExists) sSQLComm = "insert into " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name,m_Order) values ('" + profile + "','new',9999);";
                    }
                    else if (mode.Equals("newgrade"))
                    {
                        if (selected.Length > 0)
                        {
                            sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_grades where m_Profile='" + profile + "' and m_Name='new' and m_Band='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) bExists = reader.HasRows;
                            selectedsub = "new";
                            if (!bExists) sSQLComm = "insert into " + MyGlobal.activeDB + ".tbl_misc_grades (m_Profile,m_Name,m_Band,m_Order) values ('" + profile + "','new','" + selected + "',9999);";
                        }
                    }
                    else if (mode.Equals("deleteband"))
                    {
                        if (selected.Length > 0)
                        {
                            sSQL = "delete FROM " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) bExists = reader.HasRows;
                            selectedsub = "";
                            selected = "";
                        }
                    }
                    else if (mode.Equals("saveband"))
                    {
                        if (selected.Length > 0)
                        {
                            sSQLComm = "update " + MyGlobal.activeDB + ".tbl_misc_bands Set m_Name='"+selected+"' where m_Profile='" + profile + "' and m_Name='new';";
                            selectedsub = "";
                        }
                    }
                    else if (mode.Equals("deletegrade"))
                    {
                        if (selected.Length > 0)
                        {
                            sSQL = "delete FROM " + MyGlobal.activeDB + ".tbl_misc_grades where m_Profile='" + profile + "' and m_Name='" + selectedsub + "' and m_Band='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con)) using (MySqlDataReader reader = mySqlCommand.ExecuteReader()) bExists = reader.HasRows;
                            selectedsub = "";
                            selected = "";
                        }
                    }
                    else if (mode.Equals("savegrade"))
                    {
                        if (selectedsub.Length > 0)
                        {
                            sSQLComm = "update " + MyGlobal.activeDB + ".tbl_misc_grades Set m_Name='" + selectedsub + "' where m_Profile='" + profile + "' and m_Name='new';";
                            selectedsub = "";
                        }
                    }
                    if (sSQLComm.Length > 0)
                    {
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQLComm, con)) mySqlCommand.ExecuteNonQuery();
                    }
                    sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_bands " +
                        "where m_Profile='" + profile + "' order by m_Order;";
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
                                        manageBandsResponse.bands.Add(reader.GetString(0));
                                    }
                                }
                                manageBandsResponse.status = true;
                            }
                            else
                            {

                            }
                        }
                    }
                    //------------------------Load Grader
                    if (selected.Length > 0)
                    {
                        //sSQL = "SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_grades " +
                          //  "where m_Profile='" + profile + "' and m_Band='" + selected + "'" +
                            //"order by m_Order;";
                        sSQL = "SELECT grades.m_Name,payslip.m_Name FROM " + MyGlobal.activeDB + ".tbl_misc_grades grades " +
"left join(select m_Name from " + MyGlobal.activeDB + ".tbl_payscale_master where m_Profile= '" + profile + "' group by m_Name) payslip on payslip.m_Name = grades.m_Name " +
"where grades.m_Profile = '" + profile + "' and m_Band = '" + selected + "' " +
"order by grades.m_Order;";
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
                                            manageBandsResponse.grades.Add(reader.GetString(0));
                                            if (reader.IsDBNull(1))
                                            {
                                                manageBandsResponse.payscales.Add("");
                                            }
                                            else
                                            {
                                                manageBandsResponse.payscales.Add(reader.GetString(1));
                                            }
                                        }
                                    }
                                    manageBandsResponse.status = true;
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                }

            }
            catch (MySqlException ex)
            {
            }
            manageBandsResponse.selected = selected;
            manageBandsResponse.selectedsub = selectedsub;
            return Json(manageBandsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManageTeams_Sub(string profile, string selected, string mode, string name,  string head, string description,string locktime,string geolocation)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Team name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_teams " +
                                "Set " +
                                "m_Head='" + head + "'," +
                                "m_LockTime='" + locktime + "'," +
                                "m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_teams (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Team deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    else if (mode.Equals("remove"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_mobile_team_locations " +
                            "where m_Profile='" + profile + "' " +
                            "and m_TeamName='" + selected + "' " +
                            "and m_LocationName='" + geolocation + "' ";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.result = "Geo Location deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    else if (mode.Equals("move"))
                    {
                        sSQL = "insert into " + MyGlobal.activeDB + ".tbl_mobile_team_locations " +
                            "(m_Profile,m_TeamName,m_LocationName) values " +
                            "('" + profile + "','"+ selected + "','" + geolocation + "');";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.result = "Geo Location added";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_teams where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                            if (!reader.IsDBNull(5))
                                            {
                                                manageTeamsResponse.m_LockTime = reader[5].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Teams";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }

                    //-------------------------------------------Get Landmarks bind to this team
                    bool bGlobalExists = false;
                    sSQL = "select m_LocationName from " + MyGlobal.activeDB + ".tbl_mobile_team_locations " +
                        "where m_Profile='" + profile + "' and m_TeamName='" + selected + "' " +
                        "order by m_LocationName;";
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
                                        manageTeamsResponse.landmarks.Add(reader.GetString(0));
                                        if (reader.GetString(0).Equals("global")) bGlobalExists = true;
                                    }
                                }
                            }
                        }
                    }
                    //-------------------------------------------Get Landmarks
                    sSQL = "select m_Name from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                        "where m_Profile = '" + profile + "' " +
                        "and m_Name not in (select m_LocationName from " + MyGlobal.activeDB + ".tbl_mobile_team_locations " +
                        "where m_Profile = '" + profile + "' and m_TeamName = '" + selected + "') " +
                        "order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) manageTeamsResponse.landmarksAll.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    if(!bGlobalExists) manageTeamsResponse.landmarksAll.Add("global");
                    //---------------------------------------
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult ManageHolidays_Sub(string profile, string selected, string mode, string name, string head, string description, string type, string positions,string holidaygroup)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_holidays where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Holiday '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_holidays " +
                                "Set " +
                                "m_Type='" + type + "'," +
                                "m_Positions='" + positions + "'," +
                                "m_HolidayGroup='" + holidaygroup + "'," +
                                "m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                            //------------------------------
                            if (holidaygroup.Length == 0)
                            {

                                

                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_holidays where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_holidays (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_holidays where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Holiday deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_holidays where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name")))
                                    {
                                        manageTeamsResponse.teams.Add(reader.GetString(reader.GetOrdinal("m_Name")));
                                        if (selected.Equals(reader.GetString(reader.GetOrdinal("m_Name"))))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Description")))
                                            {
                                                manageTeamsResponse.m_Description = reader.GetString(reader.GetOrdinal("m_Description"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Type")))
                                            {
                                                manageTeamsResponse.m_Type = reader.GetString(reader.GetOrdinal("m_Type"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Positions")))
                                            {
                                                manageTeamsResponse.m_Positions = reader.GetString(reader.GetOrdinal("m_Positions"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_HolidayGroup")))
                                            {
                                                manageTeamsResponse.m_HolidayGroup = reader.GetString(reader.GetOrdinal("m_HolidayGroup"));
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Holidays";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManageStaffBanks_Sub(string profile, string selected, string mode, string name, string head, string description, string ifsc,string bankbranch)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Bank name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_staffbanks " +
                                "Set " +
                                "m_Branch='" + bankbranch + "'," +
                                "m_IFSC='" + ifsc + "'," +
                                "m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";

                            if (!selected.Equals(name)){ // New Name. So add ledger
                                sSQL +=
                                "INSERT INTO " + MyGlobal.activeDB + ".tbl_accounts_ledgers (m_Profile,m_Name,m_Type) " +
                                "select * FROM (select '" + profile + "', '" + name + "','Bank') AS tmp " +
                                "where NOT EXISTS(SELECT m_Name FROM " + MyGlobal.activeDB + ".tbl_accounts_ledgers " +
                                "where m_Name = '" + name + "') LIMIT 1;";
                            }
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_staffbanks (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        MySqlTransaction myTrans = con.BeginTransaction();
                        MySqlCommand myCommand = con.CreateCommand();
                        myCommand.Connection = con;
                        myCommand.Transaction = myTrans;
                        try
                        {
                            myCommand.CommandText = "delete from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            myCommand.ExecuteNonQuery();
                            manageTeamsResponse.result = "Bank deleted. ";

                            myCommand.CommandText =  "delete from " + MyGlobal.activeDB + ".tbl_accounts_ledgers where m_Profile='" + profile + "' and m_Name='" + selected + "' " +
                                "and m_Name not in (select m_Ledger from " + MyGlobal.activeDB + ".tbl_accounts where m_Profile='" + profile + "' group by m_Ledger)";
                            int iRet=myCommand.ExecuteNonQuery();
                            if (iRet > 0)
                            {
                                manageTeamsResponse.result += "Removed from Ledger";
                                myTrans.Commit();
                                manageTeamsResponse.status = true;
                            }
                            else
                            {
                                manageTeamsResponse.result = "Can't remove Ledger. Used by a voucher";
                                myTrans.Rollback();
                            }
                           
                        }
                        catch (System.Exception ex)
                        {
                            myTrans.Rollback();
                            manageTeamsResponse.result = ex.Message;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_staffbanks where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_Branch = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                            if (!reader.IsDBNull(5))
                                            {
                                                manageTeamsResponse.m_IFSC = reader[5].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Bank names";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ManageShiftNames_Sub(string profile, string selected, string mode, string name, string head, string description, string shiftstarttime, string shiftendtime)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_shiftnames where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Shift name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_shiftnames " +
                                "Set " +
                                "m_ShiftStartTime='" + shiftstarttime + "'," +
                                "m_ShiftEndTime='" + shiftendtime + "'," +
                                "m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                                manageTeamsResponse.result = "Updated";
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_shiftnames where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_shiftnames (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                                manageTeamsResponse.result = "Created a new name";
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_shiftnames where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Shift Name deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_shiftnames where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("m_Name")))
                                    {
                                        manageTeamsResponse.teams.Add(reader.GetString(reader.GetOrdinal("m_Name")));
                                        if (selected.Equals(reader.GetString(reader.GetOrdinal("m_Name"))))
                                        { 
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftStartTime")))
                                            {
                                                manageTeamsResponse.m_ShiftStartTime = reader.GetString(reader.GetOrdinal("m_ShiftStartTime"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_ShiftEndTime")))
                                            {
                                                manageTeamsResponse.m_ShiftEndTime = reader.GetString(reader.GetOrdinal("m_ShiftEndTime"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Description")))
                                            {
                                                manageTeamsResponse.m_Description = reader.GetString(reader.GetOrdinal("m_Description"));
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Shift Names";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManageRosterOptions_Sub(string profile, string selected, string mode, string name, string head, string description, string payindex,string physicalpresence)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Rroster options '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            if (payindex.Length == 0) payindex = "1";
                            if (physicalpresence.Length == 0) physicalpresence = "1";
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_rosteroptions " +
                                "Set " +
                                "m_Head='" + head + "'," +
                                "m_PayIndex='" + payindex + "'," +
                                "m_PhysicalPresence='" + physicalpresence + "'," +
                                "m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_rosteroptions (m_Profile,m_Name,m_PayIndex,m_PhysicalPresence) values ('" + profile + "','new','1','1');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Roster options deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    bool bDoesHasAnyItems = false;
                    xxx:
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rosteroptions where m_Profile='" + profile + "';";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        bDoesHasAnyItems = true;
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }

                                            if (!reader.IsDBNull(5))
                                            {
                                                manageTeamsResponse.m_PayIndex = reader[5].ToString();
                                            }
                                            if (!reader.IsDBNull(6))
                                            {
                                                manageTeamsResponse.m_PhysicalPresence = reader[6].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Roster Items";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                    if (!bDoesHasAnyItems)
                    {
                        sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_rosteroptions (m_Profile,m_Name,m_PayIndex,m_PhysicalPresence) values ('" + profile + "','void','0','0');" +
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_rosteroptions (m_Profile,m_Name,m_PayIndex,m_PhysicalPresence) values ('" + profile + "','" + MyGlobal.WORKDAY_MARKER + "','1','1');" +
                            "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_rosteroptions (m_Profile,m_Name,m_PayIndex,m_PhysicalPresence) values ('" + profile + "','OFF','1','0');";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            bDoesHasAnyItems = true;
                            goto xxx;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ManageBands_Sub(string profile, string selected, string mode, string name, string description)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Band name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_bands " +
                                "Set m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_bands (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Band deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bands where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            /*
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                            */
                                            if (!reader.IsDBNull(reader.GetOrdinal("m_Description")))
                                                manageTeamsResponse.m_Description = reader.GetString(reader.GetOrdinal("m_Description"));
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Bands";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        /*********************************Rolls*****************************************/
        [HttpPost]
        public ActionResult ManageRolls_Sub(string profile, string selected, string mode, string name, string head, string description,string locktime)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Roll name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            int iLockTime = MyGlobal.GetInt16(locktime);
                            if (iLockTime > 300 || iLockTime==0) {
                                manageTeamsResponse.result = "Lock time in mintes and only numbers";
                            }
                            else
                            {
                                sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_rolls " +
                                    "Set m_Head='" + head + "',m_LockTime='" + locktime + "',m_Description='" + description + "' ";
                                if (!selected.Equals(name))
                                {
                                    sSQL += ",m_Name='" + name + "' ";
                                }
                                sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                                using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                                {
                                    mySqlCommand.ExecuteNonQuery();
                                    if (!selected.Equals(name))
                                    {
                                        manageTeamsResponse.selected = name;
                                    }
                                    manageTeamsResponse.status = true;
                                    manageTeamsResponse.result = "Updated";
                                }
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_rolls (m_Profile,m_Name,m_LockTime) values ('" + profile + "','new','5');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                                manageTeamsResponse.result = "New roll created";
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Team deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_rolls where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                            if (!reader.IsDBNull(5))
                                            {
                                                manageTeamsResponse.m_LockTime = reader[5].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Rolls";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        /*********************************Designation*****************************************/
        [HttpPost]
        public ActionResult ManageDesignations_Sub(string profile, string selected, string mode, string name, string head, string description)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Team name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_titles " +
                                "Set m_Head='" + head + "',m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_titles (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Team deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_titles where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Designations";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        /******************************ManageGeoLocations***********************************/
        [HttpPost]
        public ActionResult ManageGeoLocations_Sub(
            string profile, string selected, string mode, string name, string head, 
            string description, string lat, string lng, string accuracy
            )
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Location name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations " +
                                "Set m_Description='" + description + "' " +
                                ",m_Lat='" + lat + "',m_Lng='" + lng + "',m_Accuracy='" + accuracy + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Base deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select m_Name,m_Lat,m_Lng,m_Accuracy,m_Description from " + MyGlobal.activeDB + ".tbl_mobile_allowed_locations where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string x = MyGlobal.GetPureString(reader, "m_Name");
                                    if (x.Length>0)
                                    {
                                        manageTeamsResponse.teams.Add(x);
                                        if (selected.Equals(x))
                                        {
                                            bSelectedIdentified = true;
                                            /*
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            */
                                            manageTeamsResponse.m_Lat = MyGlobal.GetPureString(reader, "m_Lat");
                                            manageTeamsResponse.m_Lng = MyGlobal.GetPureString(reader, "m_Lng");
                                            manageTeamsResponse.m_Accuracy = MyGlobal.GetPureString(reader, "m_Accuracy");
                                            manageTeamsResponse.m_Description = MyGlobal.GetPureString(reader, "m_Description");
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Geo Locations";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        /*********************************Bases*****************************************/
        [HttpPost]
        public ActionResult ManageBases_Sub(string profile, string selected, string mode, string name, string head, string description)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Base name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_bases " +
                                "Set m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_bases (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Base deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_bases where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            /*
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            */
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_Description = reader[3].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Bases";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
        /*********************************PAYSCALES IN MASTER TABLE> NOT USED*****************************************/
        /*
        [HttpPost]
        public ActionResult ManagePayscales_Sub(string profile, string selected, string mode, string name, string head, string description)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_payscales where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Payslip name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_payscales " +
                                "Set m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_payscales where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_payscales (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_payscales where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        sSQL +="delete from " + MyGlobal.activeDB + ".tbl_payscale_master where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Base deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_payscales where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            
                                            //if (!reader.IsDBNull(3))
                                            //{
                                              //  manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            //}

                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_Description = reader[3].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Bases";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
*/
        /*********************************Breaks*****************************************/
        [HttpPost]
        public ActionResult ManageBreaks_Sub(string profile, string selected, string mode, string name, string head, string description)
        {
            var manageTeamsResponse = new ManageTeamsResponse();
            manageTeamsResponse.status = false;
            manageTeamsResponse.result = "";
            manageTeamsResponse.selected = selected;
            try
            {
                string sSQL = "";
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    //------------------------------
                    if (mode.Equals("update"))
                    {
                        bool bDoesNewExists = false;
                        if (!selected.Equals(name)) // Team name changed. Its unique
                        {
                            sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks where m_Profile='" + profile + "' and m_Name='" + name + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        bDoesNewExists = true;
                                        manageTeamsResponse.result = "Break name '" + name + "' already exists";
                                    }
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "update " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks " +
                                "Set m_Head='" + head + "',m_Description='" + description + "' ";
                            if (!selected.Equals(name))
                            {
                                sSQL += ",m_Name='" + name + "' ";
                            }
                            sSQL += "where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                if (!selected.Equals(name))
                                {
                                    manageTeamsResponse.selected = name;
                                }
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("new"))
                    {
                        bool bDoesNewExists = false;
                        sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks where m_Profile='" + profile + "' and m_Name='new';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    bDoesNewExists = true;
                                    manageTeamsResponse.result = "New already exists";
                                    manageTeamsResponse.selected = "new";
                                }
                            }
                        }
                        if (!bDoesNewExists)
                        {
                            sSQL = "INSERT INTO " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks (m_Profile,m_Name) values ('" + profile + "','new');";
                            using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                            {
                                mySqlCommand.ExecuteNonQuery();
                                manageTeamsResponse.selected = "new";
                                manageTeamsResponse.status = true;
                            }
                        }
                    }
                    else if (mode.Equals("delete"))
                    {
                        sSQL = "delete from " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks where m_Profile='" + profile + "' and m_Name='" + selected + "';";
                        using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                        {
                            mySqlCommand.ExecuteNonQuery();
                            manageTeamsResponse.selected = "";
                            manageTeamsResponse.result = "Team deleted";
                            manageTeamsResponse.status = true;
                        }
                    }
                    //------------------------------
                    bool bSelectedIdentified = false;
                    sSQL = "select * from " + MyGlobal.activeDB + ".tbl_misc_meterbox_breaks where m_Profile='" + profile + "' order by m_Name;";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(2))
                                    {
                                        manageTeamsResponse.teams.Add(reader[2].ToString());
                                        if (selected.Equals(reader[2].ToString()))
                                        {
                                            bSelectedIdentified = true;
                                            if (!reader.IsDBNull(3))
                                            {
                                                manageTeamsResponse.m_TeamHead = reader[3].ToString();
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                manageTeamsResponse.m_Description = reader[4].ToString();
                                            }
                                        }
                                    }
                                }
                                if (mode.Length == 0)
                                {
                                    manageTeamsResponse.status = true;
                                }
                                //manageTeamsResponse.result = "Done";
                            }
                            else
                            {
                                manageTeamsResponse.result = "Sorry!!! No Breaks";
                            }
                        }
                    }
                    if (!bSelectedIdentified)
                    {
                        if (!manageTeamsResponse.selected.Equals("new"))
                            manageTeamsResponse.selected = "";
                    }
                }
            }
            catch (MySqlException ex)
            {
                manageTeamsResponse.result = ex.Message;
            }

            return Json(manageTeamsResponse, JsonRequestBehavior.AllowGet);
        }
    }
}