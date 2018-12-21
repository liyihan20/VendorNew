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

        [AllowAnonymous]
        public JsonResult IsUserAndEmailExisted(string userName)
        {
            var user = new UserSv().GetUserByUserName(userName);
            if (user == null) {
                return Json(new SRM(false,"用户名不存在"));
            }
            if (string.IsNullOrEmpty(user.email)) {
                return Json(new SRM(false, "贵司未在此平台登记过邮箱，不能处理；请联系管理员处理"));
            }
            return Json(new SRM());
        }

        [AllowAnonymous]
        public JsonResult SendValidateCodeForReset(string userName, string emailAddr)
        {
            bool isEmailValid;
            try {
                isEmailValid = new UserSv().HasEmailRegister(userName, emailAddr);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            if (!isEmailValid) {
                return Json(new SRM(false, "此邮箱地址与贵司在本平台登记的不匹配，请确认后重新输入再发送验证码"));
            }

            //验证通过，可以发送验证码
            var code = MyUtils.CreateValidateNumber(6);
            MyEmail.SendValidateCode(code, emailAddr, userName);

            Session["emailCode"] = code.ToUpper();

            return Json(new SRM(true,"验证码已发送，请到邮箱收取后复制到验证文本框"));
        }

        [AllowAnonymous]
        public JsonResult ValidateEmail4Password(string userName, string code, string opType)
        {
            if (Session["emailCode"] == null) {
                return Json(new SRM(false, "请先发送邮箱验证码后再操作"));
            }
            if (!code.Trim().ToUpper().Equals(Session["emailCode"])) {
                return Json(new SRM(false, "验证码不正确，请重新输入"));
            }
            string result = "";
            try {
                var uv = new UserSv();
                var user = uv.GetUserByUserName(userName);

                if (opType.Contains("R")) {
                    uv.ResetPassword(user.user_id);
                    result += "登录密码已成功重置为和用户名一致;";
                }
                if (opType.Contains("T")) {
                    if (user.is_forbit) {
                        uv.ToggleUser(user.user_id);
                    }
                    result += "用户已解禁";
                }
                WLog(TAG, userName + ":" + result);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            Session.Remove("emailCode");
            return Json(new SRM(true, result));
        }

    }
}
