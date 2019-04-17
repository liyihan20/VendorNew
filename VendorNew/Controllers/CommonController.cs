using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Models;

namespace VendorNew.Controllers
{
    public class CommonController : Controller
    {
        public JsonResult DateCal(string bDate, string months, string eDate)
        {
            DateTime bDateDt,eDateDt;
            int monthsIn;
            if (!DateTime.TryParse(bDate, out bDateDt)) {
                return Json(new SRM(false));
            }
            if (!DateTime.TryParse(eDate, out eDateDt)) {
                eDateDt = bDateDt;
            }
            if (!int.TryParse(months, out monthsIn)) {
                return Json(new SRM(false));
            }
            DateTime result = bDateDt.AddMonths(monthsIn).AddDays(-1);
            if (Math.Abs((result - eDateDt).TotalDays) <= 10) {
                result = eDateDt; //如果计算结果与传进来的有效期相差少于10天，即返回原始有效期
            }
            return Json(new SRM(true, "", result.ToString("yyyy-MM-dd")));
        }

    }
}
