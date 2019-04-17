using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorTruly.Models;
using VendorTruly.Services;
using VendorTruly.Utils;

namespace VendorTruly.Filters
{
    public class SessionTimeOutFilterAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            if (ctx.Session != null) {
                var cookie = ctx.Request.Cookies[MyUtils.GetCookieName()];
                if (cookie != null) {
                    var id = cookie.Values.Get("user_id");
                    var code = cookie.Values.Get("code");
                    if (code.Equals(MyUtils.getMD5(id))) {
                        base.OnActionExecuting(filterContext);
                        return;
                    }
                }
            }
            //将访问的url作为参数保存起来，登陆后直接跳转到此url
            string returnUrl = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName + "/" + filterContext.ActionDescriptor.ActionName;
            if (returnUrl.ToUpper().EndsWith("HOME/INDEX")) {
                filterContext.Result = new RedirectResult("~/Account/Login");
                return;
            }
            if (filterContext.ActionParameters.Keys.Count() > 0) {
                returnUrl += "?";
                foreach (var k in filterContext.ActionParameters.Keys) {
                    var v = filterContext.ActionParameters[k];
                    if (v != null) {
                        returnUrl += k + "=" + v.ToString() + "&";
                    }
                }
                returnUrl = returnUrl.Substring(0, returnUrl.Length - 1); // 将最后的&去掉
            }
            
            filterContext.Result = new RedirectResult("~/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        }

    }

    public class SessionTimeOutJsonFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            if (ctx.Session != null) {
                var cookie = ctx.Request.Cookies[MyUtils.GetCookieName()];
                if (cookie != null) {
                    var id = cookie.Values.Get("user_id");
                    var code = cookie.Values.Get("code");
                    if (code.Equals(MyUtils.getMD5(id))) {
                        base.OnActionExecuting(filterContext);
                        return;
                    }
                }
            }

            filterContext.Result = new JsonResult()
            {
                Data = new SRM() { suc = false, msg = "操作失败！原因：会话已过期，请重新登陆系统" }
            };
        }
    }

    //权限过滤器，验证用户是否有访问该控制器和方法的权限。
    public class AuthorityFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //先执行session拦截器，没有登陆则跳转到登陆界面
            new SessionTimeOutFilterAttribute().OnActionExecuting(filterContext);
            if (filterContext.Result != null) return;

            //如果已登陆，再判断是否有权限
            string actionName = filterContext.ActionDescriptor.ActionName;
            string controlerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;

            HttpContextBase ctx = filterContext.HttpContext;
            var cookie = ctx.Request.Cookies[MyUtils.GetCookieName()];
            var id = cookie.Values.Get("user_id");
            if (new UASv().hasGotPower(int.Parse(id), controlerName, actionName)) {
                base.OnActionExecuting(filterContext);
                return;
            }
            else {
                filterContext.Result = new RedirectResult("~/Account/NoPowerToVisit?controlerName=" + controlerName + "&actionName=" + actionName);                
            }

        }
    }

}