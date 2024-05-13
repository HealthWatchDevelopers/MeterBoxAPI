using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Net.Mail;
using MyHub.Controllers;

namespace MyHub.Hubs
{
    public partial class ChatHub : Hub
    {


        private void PrepareSendMail_swipe(String sProfile, String sClientName, String sLocation, String sKeyString, String sSwipeTime)
        {
            String sMails = GetToEmails("emailsSwipe");
            if (sMails.Length == 0) return;
            MailDoc mailDoc = new MailDoc();
            mailDoc.m_To = sMails;
            mailDoc.m_Subject = "Swipe alert at " + sClientName;
            mailDoc.m_Body = "<b>NFC Swipe alert</b><br><br>" +
                "<table>" +
                "<tr><td>Profile name </td><td style='background-color:#e0e0e0;'> " + sProfile + "</td></tr>" +
                "<tr><td>Client name </td><td style='background-color:#eeeeee;'> <span style='color:red;font-weight:bold;'>" + sClientName + "</span></td></tr>" +
                "<tr><td>Location </td><td style='background-color:#e0e0e0;'> <span style='color:blue;font-weight:bold;'>" + sLocation + "</span></td></tr>" +
                "<tr><td>Key </td><td style='background-color:#eeeeee;'> <span style='color:gray'>" + sKeyString + "</span></td></tr>" +
                "<tr><td>Access time </td><td style='background-color:#e0e0e0;'> " + sSwipeTime + "</td></tr>" +
                "</table>";
            Thread newThread = new Thread(SendEmail);
            newThread.Start(mailDoc);
            MessageToDebugger("Doom-Swipe alert email sent");
        }
        private void PrepareSendMail_shift(String s_m_id, String sIMEI, String sTimeIn, String sTimeInLat)
        {
            String sMails = GetToEmails("emailsSign");
            if (sMails.Length == 0) return;
            String sStaffID = "", sTimeOut = "", sClientName = "", sStaffName = "";
            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "SELECT m_StaffID,m_StaffName,m_ClientName,m_TimeIn,m_TimeOut from " + Controllers.MyGlobal.activeDB + ".tbl_attendancelog where m_id_device='" + s_m_id + "' " +
                    "and m_IMEI='" + sIMEI + "' and m_TimeIn='" + sTimeIn + "' and m_TimeInLat='" + sTimeInLat + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    sStaffID = GetPure(reader, 0);
                                    sStaffName = GetPure(reader, 1);
                                    sClientName = GetPure(reader, 2);
                                    sTimeIn = GetPure(reader, 3);
                                    sTimeOut = GetPure(reader, 4);
                                }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-GetToEmails" + ex.Message);

            }
            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 


            MailDoc mailDoc = new MailDoc();
            mailDoc.m_To = sMails;

            String ssss = "<b>Shift In alert</b>";
            if (sTimeOut.Length > 0) ssss = "<b>Shift Out alert</b>";
            mailDoc.m_Subject = "Shift alert at " + sClientName;
            mailDoc.m_Body = ssss + " <br><br>" +
                "<table>" +
                //"<tr><td>Profile name </td><td style='background-color:#eeeeee;'> " + sProfile + "</td></tr>" +
                "<tr><td>Client name </td><td style='background-color:#e0e0e0;'> <span style='color:red;font-weight:bold;'>" + sClientName + "</span></td></tr>" +
                "<tr><td>Staff name </td><td style='background-color:#eeeeee;'> <span style='color:blue;font-weight:bold;'>" + sStaffName + "</span></td></tr>" +
                "<tr><td>Staff ID </td><td style='background-color:#e0e0e0;'> <span style='color:gray'>" + sStaffID + "</span></td></tr>" +
                "<tr><td>Shift In </td><td style='background-color:#eeeeee;'> " +
                UnixTimeToDateTime(sTimeIn) +
                "</td></tr>" +
                "<tr><td>Shift Out </td><td style='background-color:#e0e0e0;'> " +
                UnixTimeToDateTime(sTimeOut) +
                "</td></tr>" +
                "</table>";
            Thread newThread = new Thread(SendEmail);
            newThread.Start(mailDoc);
            MessageToDebugger("Doom-Shift alert email sent");
        }
        private String GetToEmails(String sType)
        {
            String sEmails = "";
            try
            {
                using (MySqlConnection con = new MySqlConnection(MyGlobal.GetConnectionString()))
                {
                    con.Open();
                    String sSQL = "SELECT * from " + Controllers.MyGlobal.activeDB + ".tbl_alerts where m_Profile='iguard' and m_Type='" + sType + "'";
                    using (MySqlCommand mySqlCommand = new MySqlCommand(sSQL, con))
                    {
                        using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (!GetPure(reader, 4).Equals("1")) return "";  // m_Enabled
                                    sEmails = GetPure(reader, 6);
                                }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageToDebugger("Err-FromDoom-GetToEmails" + ex.Message);

            }
            return sEmails;
        }
        private static void SendEmail_iGuard(object data)
        {
            MailDoc mailDoc = (MailDoc)data;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(mailDoc.m_To);
                mail.From = new MailAddress("iGuard suite alert <alerts@iguardsuite.com>");
                mail.Subject = mailDoc.m_Subject;
                string Body = mailDoc.m_Body;
                mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail.iguardsuite.com";
                smtp.Port = 25;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("alerts@iguardsuite.com", "Po!Tr@w0yI827");
                smtp.EnableSsl = false;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
            }
        }

    }
}