using System;
using System.Linq;
using System.Web.Mvc;
using VendorNew.Filters;
using VendorNew.Services;
using VendorNew.Utils;
using VendorNew.Models;

namespace VendorNew.Controllers
{
    public class AdminController : BaseController
    {

        #region 用户管理

        [AuthorityFilter]
        public ActionResult Users()
        {
            return View();
        }
        
        public JsonResult GetUsers(int page, int rows, string searchValue = "")
        {
            var users = new UserSv().GetUsers(searchValue);
            var total = users.Count();
            var result = users.Skip((page - 1) * rows).Take(rows)
                .Select(u => new
                {
                    u.comment,
                    u.continual_error_times,
                    u.email,
                    u.forbit_date,
                    u.forbit_reason,
                    u.in_date,
                    u.is_forbit,
                    u.last_login_date,
                    u.real_name,
                    u.user_id,
                    u.user_name,
                    u.user_role
                })
                .ToList();

            return Json(new { rows = result, total = total });
        }

        public JsonResult GetComboUsers(string searchValue = "", int maxCount=30)
        {
            var users = new UserSv().GetUsers(searchValue);
            var result = users.Select(u => new
                {
                    u.real_name,
                    u.user_id,
                    u.user_name
                })
                .Take(maxCount)
                .ToList();

            return Json(result);
        }

        [SessionTimeOutJsonFilter]
        public JsonResult GetSupplierNameByNumber(string number)
        {
            return Json(new SRM(true, "", new ItemSv().GetSupplierNameByNumber(number)));
        }

        [SessionTimeOutJsonFilter]
        public JsonResult GetEmpNameByNumber(string number)
        {
            return Json(new SRM(true, "", new ItemSv().GetEmpNameByNumber(number)));
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveUser(FormCollection fc)
        {
            Users user = new Users();
            MyUtils.SetFieldValueToModel(fc, user);

            if (string.IsNullOrEmpty(user.user_name)) {
                return Json(new SRM(false, "登录名不能为空"));
            }
            if (string.IsNullOrEmpty(user.real_name)) {
                return Json(new SRM(false, "用户名不能为空"));
            }
            if (string.IsNullOrEmpty(user.user_role)) {
                return Json(new SRM(false, "角色必须选择"));
            }

            try {
                new UserSv().SaveUser(user);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("用户管理", "保存用户：" + user.user_name);

            return Json(new SRM());

        }

        [SessionTimeOutJsonFilter]
        public JsonResult ResetPassword(int userId)
        {
            try {
                new UserSv().ResetPassword(userId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("用户管理", "重置密码：" + userId);

            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult ToggleUser(int userId)
        {
            try {
                new UserSv().ToggleUser(userId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("用户管理", "禁用/解禁用户：" + userId);

            return Json(new SRM());
        }

        #endregion
        
        #region 权限管理

        [AuthorityFilter]
        public ActionResult Authorities()
        {
            return View();
        }

        public JsonResult GetAuthorities(string searchValue = "")
        {
            var list = new UASv().GetAuthorities(searchValue)
                .Select(a => new
                {
                    a.action_name,
                    a.auth_id,
                    a.btn_color,
                    a.comment,
                    a.controller_name,
                    a.en_name,
                    a.fa_name,
                    a.name,
                    a.number
                }).ToList();

            return Json(list);
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveAuthority(FormCollection fc)
        {
            Authorities auth = new Authorities();
            MyUtils.SetFieldValueToModel(fc, auth);

            if (auth.number == null) {
                return Json(new SRM(false, "编号不能为空"));
            }
            if (string.IsNullOrEmpty(auth.name)) {
                return Json(new SRM(false, "权限名不能为空"));
            }
            if (string.IsNullOrEmpty(auth.en_name)) {
                return Json(new SRM(false, "EN名称不能为空"));
            }

            try {
                new UASv().SaveAuthority(auth);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("权限管理", "保存权限：" + auth.name);

            return Json(new SRM());

        }

        public JsonResult RemoveAuthority(int authId)
        {
            try {
                new UASv().RemoveAuth(authId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("权限管理", "删除权限：" + authId);

            return Json(new SRM());
        }

        #endregion

        #region 权限组管理

        /// <summary>
        /// 权限组界面
        /// </summary>
        /// <returns></returns>
        [AuthorityFilter]
        public ActionResult AutGroups()
        {
            return View();
        }

        /// <summary>
        /// 审核组界面
        /// </summary>
        /// <returns></returns>
        [AuthorityFilter]
        public ActionResult AuditGroups()
        {
            return View();
        }

        public JsonResult GetGroups(int page, int rows, string groupType = "auth", string searchGroup = "")
        {
            var groups = new UASv().GetGroups(groupType,searchGroup);
            int total = groups.Count();

            var result = groups.Skip((page - 1) * rows).Take(rows)
                .Select(g => new
                {
                    g.comment,
                    g.group_id,
                    g.name
                }).ToList();

            return Json(new { rows = result, total = total });
        }

        public JsonResult GetGroupAndAllMembers(int page, int rows, string groupType = "auth", string searchGroup = "")
        {
            var groups = new UASv().GetGroupAndAllMembers(groupType, searchGroup);
            int total = groups.Count();

            var result = groups.Skip((page - 1) * rows).Take(rows)
                .Select(g => new
                {
                    g.group_id,
                    g.name,
                    members = string.Join("; ", g.allMembers.ToArray())
                }).OrderBy(g => g.name).ToList();

            return Json(new { rows = result, total = total });
        }


        public JsonResult GetGroupUsers(int page, int rows,int groupId, string searchGroupUser="")
        {
            var groupUsers = new UASv().GetGroupUsers(groupId, searchGroupUser);
            int total = groupUsers.Count();
            return Json(new { rows = groupUsers.Skip((page - 1) * rows).Take(rows).ToList(), total = total });
        }

        public JsonResult GetGroupAuts(int page, int rows, int groupId, string searchGroupAut = "")
        {
            var groupAuts = new UASv().GetGroupAuts(groupId, searchGroupAut);
            int total = groupAuts.Count();
            return Json(new { rows = groupAuts.Skip((page - 1) * rows).Take(rows).ToList(), total = total });
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveGroup(FormCollection fc)
        {
            var group = new Groups();
            MyUtils.SetFieldValueToModel(fc, group);
            if (string.IsNullOrEmpty(group.name)) {
                return Json(new SRM(false, "组名不能为空"));
            }
            try {
                new UASv().SaveGroup(group);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "保存分组：" + group.name);

            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult RemoveGroup(int groupId)
        {
            try {
                new UASv().RemoveGroup(groupId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "删除分组：" + groupId);

            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveGroupAut(int groupId, string autIds)
        {
            try {
                int[] autIdsInt = autIds.Split(',').Select(a => Int32.Parse(a)).ToArray();
                new UASv().SaveGroupAut(groupId, autIdsInt);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "组内增加权限：" + groupId + "+=" + autIds);

            return Json(new SRM());

        }
        
        [SessionTimeOutJsonFilter]
        public JsonResult RemoveGroupAut(int groupAutId)
        {
            try {
                new UASv().RemoveGroupAut(groupAutId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "组内删除权限：" + groupAutId);

            return Json(new SRM());
        }
        
        [SessionTimeOutJsonFilter]
        public JsonResult SaveGroupUser(int groupId, int userId)
        {
            try {
                new UASv().SaveGroupUser(groupId, userId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "组内增加用户：" + groupId + "+=" + userId);

            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult RemoveGroupUser(int groupUserId)
        {
            try {
                new UASv().RemoveGroupUser(groupUserId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            WLog("分组管理", "组内删除用户：" + groupUserId);

            return Json(new SRM());
        }

        #endregion

        #region 更新日志

        [AuthorityFilter]
        public ActionResult CheckUpdateLog()
        {
            return View();
        }

        public JsonResult GetUpdateLog(string searchValue = "")
        {
            return Json(new ItemSv().GetUpdateLogs(searchValue));
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveUpdateLog(UpdateLog log)
        {
            try {
                new ItemSv().SaveUpdateLog(log);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult RemoveUpdateLog(int id)
        {
            try {
                new ItemSv().RemoveUpdateLog(id);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        #endregion

    }
}
