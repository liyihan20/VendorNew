using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Filters;
using VendorNew.Services;
using VendorNew.Utils;

namespace VendorNew.Controllers
{
    public class ReportController : BaseController
    {
        [AuthorityFilter]
        public ActionResult PrintApply(int billId, string pageNumList = null)
        {
            int defaultNumPerPage = 14;
            int totalNumToDisplay = 0;

            if (pageNumList == null) {
                pageNumList = defaultNumPerPage.ToString();//默认1页显示14行数据
            }

            var m = new ReportSv().GetData4Print(billId, currentAccount);
            totalNumToDisplay = m.boxAndPos.Count() == 0 ? m.es.Count() : m.boxAndPos.Count();
            List<int> pageNumArr = MyUtils.GetPageNumberList(defaultNumPerPage, pageNumList, totalNumToDisplay);

            ViewData["reportData"] = m;
            ViewData["pageNumList"] = pageNumList;
            ViewData["pageNumArr"] = pageNumArr;

            return View();
        }

        [AuthorityFilter]
        public ActionResult PrintOuterQrcode(int billId,int numPerPage=1)
        {
            if (numPerPage < 1) numPerPage = 1;

            try {
                var result = new ReportSv().GetOuterBoxes4Print(billId);
                ViewData["outerData"] = result;
                ViewData["numPerPage"] = numPerPage;
                ViewData["billId"] = billId;
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            return View();
        }

        [AuthorityFilter]
        public ActionResult PrintInnerQrcode(int billId, int numPerPage = 1)
        {
            if (numPerPage < 1) numPerPage = 1;

            try {
                var result = new ReportSv().GetInnerBoxes4Print(billId);
                ViewData["innerData"] = result;
                ViewData["numPerPage"] = numPerPage;
                ViewData["billId"] = billId;
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            return View();
        }

    }
}
