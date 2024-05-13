using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyHub.Controllers
{
    public class TemplatesController : Controller
    {
        // GET: Templates
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult TripMail()
        {
            return View();
        }

    }
}