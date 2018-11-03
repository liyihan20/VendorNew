using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Filters;
using VendorNew.Services;

namespace VendorNew.Controllers
{
    public class ReportController : BaseController
    {
        [AuthorityFilter]
        public ActionResult PrintApply(int billId)
        {
            var m = new ReportSv().GetData4Print(billId, currentAccount);
            ViewData["reportData"] = m;
            return View();
        }

    }
}
