using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MyHub.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Chat()
        {
            return View();
        }
        /*
        public ActionResult GetCurrentTime()
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            return Content(hub.GetListTable() +  DateTime.Now.ToLongTimeString() + "[" + unixTimestamp + "]");
        }
        */
        public ActionResult GetGreyOfficeMobileList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetGreyOfficeMobileList_todebug());
        }
        public ActionResult GetDoomDriverList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetDriverList_todebug());
        }
        public ActionResult GetDoomClientList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetClientList_todebug());
        }
        public ActionResult GetLogisticsClientsList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetLogisticsClientsList_todebug());
        }
        public ActionResult GetAccessManagerList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetAccessManagerList_todebug());
        }
        public ActionResult GetDispatchersList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetDispatchersList_todebug());
        }
        
        public ActionResult GetDoomBrowserList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetBrowserList_todebug());
        }
        /* robin
        public ActionResult GetDoomUnknownList_todebug()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            return Content(hub.GetUnknownList_todebug());
        }
        */
        public ActionResult GetSubscriptionList()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            return Content(hub.GetSubList() + "-" + DateTime.Now.ToLongTimeString());
        }
        public ActionResult GetBlockedList()
        {
            DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
            var hub = hd.ResolveHub("ChatHub") as ChatHub;
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            return Content(hub.GetBlockedList() + "-" + DateTime.Now.ToLongTimeString());
        }
    }
}