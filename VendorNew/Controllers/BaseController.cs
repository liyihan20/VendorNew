using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Models;
using VendorNew.Utils;
using VendorNew.Services;

namespace VendorNew.Controllers
{
    public class BaseController : Controller
    {
        private UserInfoModel _currentUser;
        private string _currentAccount;
        private bool? _canCheckAll, _canAuditAll;
        private Companies _currentCompany;

        public UserInfoModel currentUser
        {
            get
            {
                _currentUser = (UserInfoModel)Session["userInfo"];
                if (_currentUser == null) {
                    var cookie = Request.Cookies[MyUtils.GetCookieName()];
                    if (cookie != null) {
                        _currentUser = new UserInfoModel();
                        _currentUser.userId = Int32.Parse(cookie.Values.Get("user_id"));
                        _currentUser.userName = MyUtils.DecodeToUTF8(cookie.Values.Get("user_name"));
                        _currentUser.realName = MyUtils.DecodeToUTF8(cookie.Values.Get("real_name"));
                    }
                    Session["userInfo"] = _currentUser;
                }
                return _currentUser;
            }
        }

        /// <summary>
        /// 当前登录账套，O表示光电，S表示半导体，R是仁寿
        /// </summary>
        public string currentAccount
        {
            get
            {
                _currentAccount = (string)Session["currentAccount"];
                if (_currentAccount == null) {
                    var cookie = Request.Cookies[MyUtils.GetCookieName()];
                    if (cookie != null) {
                        _currentAccount = cookie.Values.Get("account");
                    }
                    Session["currentAccount"] = _currentAccount;
                }
                return _currentAccount;
            }
        }

        /// <summary>
        /// 可查看所有单据
        /// </summary>
        public bool canCheckAll
        {
            get
            {
                _canCheckAll = (bool?)Session["canCheckAll"];
                if (_canCheckAll == null) {
                    _canCheckAll = new UASv().hasGotPower(currentUser.userId, "check_all_bills");
                    Session["canCheckAll"] = _canCheckAll;
                }
                return (bool)_canCheckAll;
            }
        }

        /// <summary>
        /// 可审批所有送货申请
        /// </summary>
        public bool canAuditAll
        {
            get
            {
                _canAuditAll = (bool?)Session["canAuditAll"];
                if (_canAuditAll == null) {
                    _canAuditAll = new UASv().hasGotPower(currentUser.userId, "audit_all_bills");
                    Session["canAuditAll"] = _canAuditAll;
                }
                return (bool)_canAuditAll;
            }
        }

        public Companies currentCompany
        {
            get
            {
                _currentCompany = (Companies)Session["currentcompany"];
                if (_currentCompany == null) {
                    _currentCompany = new ItemSv().GetCertainCompany(currentAccount);
                    Session["currentcompany"] = _currentCompany;
                }
                return _currentCompany;
            }
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="module">模块</param>
        /// <param name="doWhat">操作事项</param>
        /// <param name="sysNum">流水编号</param>
        /// <param name="isNormal">是否正常操作</param>
        public void WLog(string module, string doWhat, string sysNum = "", bool isNormal = true)
        {
            new BaseSv().Wlog(new EventLog()
            {
                module = module,
                do_what = doWhat,
                sys_num = sysNum,
                is_normal = isNormal,
                ip = Request.UserHostAddress,
                op_time = DateTime.Now,
                user_name = currentUser == null ? "" : currentUser.realName + "(" + currentUser.userName + ")",
                account = currentAccount
            });
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }



    }
}
