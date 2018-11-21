using System;
using System.Web;
using System.Web.Mvc;
using VendorNew.Models;
using VendorNew.Services;
using VendorNew.Utils;

namespace VendorNew.Controllers
{
    public class AccountController : BaseController
    {
        private const string TAG = "登录模块";
        public ActionResult Login()
        {
            var cookie = Request.Cookies[MyUtils.GetCookieName() + "_login"];
            if (cookie != null) {
                ViewBag.user_name = MyUtils.DecodeToUTF8(cookie.Values.Get("user_name"));
                ViewBag.account = cookie.Values.Get("account");
            }
            return View();
        }

        [AllowAnonymous]
        public FileContentResult getValidateCodeImg()
        {
            string code = MyUtils.CreateValidateNumber(4);
            Session["code"] = code.ToLower();
            byte[] bytes = MyUtils.CreateValidateGraphic(code, 120, 38);
            return File(bytes, @"image/jpeg");
        }

        [AllowAnonymous]
        public JsonResult StartLogin(FormCollection fc)
        {
            string account = fc.Get("account");
            string userName = fc.Get("user_name");
            string password = fc.Get("password");
            string validateCode = fc.Get("validate_code");

            if (!validateCode.ToLower().Equals((string)Session["code"])) {
                return Json(new SRM(false, "验证码不正确"));
            }

            Users user;
            try {
                user = new UserSv().StartLogin(userName, password);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            //写入cookie，保留24小时登录状态
            var cookie = new HttpCookie(MyUtils.GetCookieName());
            cookie.Values.Add("user_id", user.user_id.ToString());
            cookie.Values.Add("user_name", MyUtils.EncodeToUTF8(user.user_name));
            cookie.Values.Add("real_name", MyUtils.EncodeToUTF8(user.real_name));
            cookie.Values.Add("account", account);
            cookie.Values.Add("code", MyUtils.getMD5(user.user_id.ToString()));//用于filter验证
            cookie.Expires = DateTime.Now.AddDays(1);
            Response.AppendCookie(cookie);

            //再写入登录用的cookie，用于记住公司名和登录名
            cookie = new HttpCookie(MyUtils.GetCookieName() + "_login");
            cookie.Values.Add("user_name", MyUtils.EncodeToUTF8(user.user_name));
            cookie.Values.Add("account", account);
            cookie.Expires = DateTime.Now.AddYears(1);
            Response.AppendCookie(cookie);

            WLog(TAG, "登录成功:" + userName + "," + account);

            return Json(new SRM());
        }                

        public ActionResult LogOut()
        {
            WLog(TAG, "安全退出");

            Session.Clear();
            var cookie = new HttpCookie(MyUtils.GetCookieName());
            cookie.Expires = DateTime.Now.AddHours(-1);
            Response.AppendCookie(cookie);

            return RedirectToAction("Login");
        }

        public ActionResult NoPowerToVisit(string controlerName, string actionName)
        {
            WLog("无权访问", controlerName + "/" + actionName, "", false);
            return View();
        }

    }
}
