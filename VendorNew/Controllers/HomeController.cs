using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Filters;
using VendorNew.Services;
using VendorNew.Models;
using System.Text.RegularExpressions;
using VendorNew.Utils;

namespace VendorNew.Controllers
{
    public class HomeController : BaseController
    {
        [SessionTimeOutFilter]
        public ActionResult Index(string url)
        {
            ViewBag.userName = currentUser.userName;
            ViewBag.realName = currentUser.realName;
            ViewBag.account = currentAccount;
            ViewBag.url = url;

            if (new UserSv().IsPasswordSameWithLoginName(currentUser.userId)) {
                ViewBag.needToChangePassword = 1;
            }

            return View();
        }
        
        public JsonResult GetMyMenuItems()
        {
            return Json(new UASv().GetMenuItems(currentUser.userId));
        }

        [SessionTimeOutFilter]
        public ActionResult Main()
        {
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult Test()
        {
            return View();
        }

        //public string EmailTest()
        //{
        //    Utils.MyEmail.SendValidateCode("abc", "liyihan.ic@truly.com.cn,,", "liyihan");
        //    return "ok";
        //}

        [SessionTimeOutFilter]
        public ActionResult PasswordSetting()
        {
            return View();
        }

        [SessionTimeOutJsonFilter]
        public JsonResult ResetPassword(string oldP,string newP){
            try {
                new UserSv().ResetPassword(currentUser.userId, oldP, newP);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("重置密码", "密码重置成功");
            return Json(new SRM(true, "密码已重置成功"));
        }

        [SessionTimeOutFilter]
        public ActionResult EmailSetting()
        {
            ViewData["email"] = new UserSv().GetEmailAddr(currentUser.userId);
            return View();
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SendValidateCode(string emailAddr,int index)
        {
            var emailR = new Regex(@"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
            if (!emailR.IsMatch(emailAddr)) {
                return Json(new SRM(false, "邮箱地址不合法"));
            }

            var code = MyUtils.CreateValidateNumber(6);
            MyEmail.SendValidateCode(code, emailAddr, currentUser.realName);

            Session["email" + index] = code.ToUpper();
            return Json(new SRM());

        }

        [SessionTimeOutJsonFilter]
        public JsonResult UpdateEmailAddr(string emailAddr, int index, string code)
        {
            if (Session["email" + index] == null) {
                return Json(new SRM(false, "请先发送邮箱验证码后再操作"));
            }
            if (!code.Trim().ToUpper().Equals((string)Session["email" + index])) {
                return Json(new SRM(false, "邮箱验证码不正确"));
            }

            try {
                new UserSv().UpdateEmailAddr(currentUser.userId, emailAddr, index);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            Session.Remove("email" + index);

            WLog("邮箱设置", index + ":" + emailAddr);

            return Json(new SRM());
        }
        

    }
}
