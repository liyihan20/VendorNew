using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorNew.Models;

namespace VendorNew.Services
{
    public class UASv : BaseSv
    {

        public Authorities GetAutById(int id)
        {
            return db.Authorities.Where(a => a.auth_id == id).FirstOrDefault();
        }

        public Groups GetGroupById(int id)
        {
            return db.Groups.Where(g => g.group_id == id).FirstOrDefault();
        }

        /// <summary>
        /// 获取主页菜单项
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<MenuItems> GetMenuItems(int userId)
        {
            var list = (from gu in db.GroupUsers
                        join g in db.Groups on gu.group_id equals g.group_id
                        join ga in db.GroupAuthorities on g.group_id equals ga.group_id
                        join a in db.Authorities on ga.auth_id equals a.auth_id
                        where gu.user_id == userId
                        && a.number * 10 % 1 == 0
                        orderby a.number
                        select (new MenuItems()
                       {
                           number = a.number,
                           name = a.name,
                           icon = a.fa_name,
                           color = a.btn_color,
                           url = a.number % 1 == 0 ? "" : "/VendorNew/" + a.controller_name + "/" + a.action_name
                       })).Distinct().ToList();

            return list;            
        }

        /// <summary>
        /// 判断某用户是否有执行某个controller和action的权限
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public bool hasGotPower(int userId, string controllerName, string actionName)
        {
            return (from gu in db.GroupUsers
                    join g in db.Groups on gu.group_id equals g.group_id
                    join ga in db.GroupAuthorities on g.group_id equals ga.group_id
                    join a in db.Authorities on ga.auth_id equals a.auth_id
                    where gu.user_id == userId
                    && a.controller_name == controllerName
                    && a.action_name == actionName
                    select a).Count() > 0;
        }

        /// <summary>
        /// 判断某用户是否拥有某权限
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="authEnName"></param>
        /// <returns></returns>
        public bool hasGotPower(int userId, string authEnName)
        {
            return (from gu in db.GroupUsers
                    join g in db.Groups on gu.group_id equals g.group_id
                    join ga in db.GroupAuthorities on g.group_id equals ga.group_id
                    join a in db.Authorities on ga.auth_id equals a.auth_id
                    where gu.user_id == userId
                    && a.en_name==authEnName       
                    select a).Count() > 0;
        }
        
        /// <summary>
        /// 获取和自己在同一个审核组内的所有成员登录名
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<Users> GetAuditGroupUsers(int userId)
        {
            return (from ga in db.GroupUsers
                    join g in db.Groups on ga.group_id equals g.group_id
                    join ga2 in db.GroupUsers on g.group_id equals ga2.group_id
                    join u in db.Users on ga2.user_id equals u.user_id
                    where g.group_type == "audit"
                    && ga.user_id == userId
                    select u).Distinct().ToList();
        }


        public bool CanCheckTheDRBill(int billId, string userName,int userId)
        {
            var bill = (from d in db.DRBills
                        join e in db.DRBillDetails on d.bill_id equals e.bill_id
                        where d.bill_id == billId
                        select new
                        {
                            d,
                            e
                        }).ToList();
            if (bill.Count() == 0) {
                return false;
            }
            if (userName.StartsWith(bill.First().d.supplier_number)) {
                return true;
            }
            if (bill.First().d.mat_order_number.Equals(userName) || bill.Where(b => b.e.buyer_number.Equals(userName)).Count() > 0) {
                return true;
            }
            var auditGroupUsers = GetAuditGroupUsers(userId).Select(u => u.user_name).ToList(); 
            if (auditGroupUsers.Contains(bill.First().d.mat_order_number)) {
                return true;
            }
            if (bill.Where(b => auditGroupUsers.Contains(b.e.buyer_number)).Count() > 0) {
                return true;
            }
            return false;
        }

        public List<Authorities> GetAuthorities(string searchValue)
        {
            return db.Authorities.Where(a => a.name.Contains(searchValue) || a.en_name.Contains(searchValue)).OrderBy(a => a.number).ToList();
        }

        public void SaveAuthority(Authorities aut)
        {
            if (db.Authorities.Where(a => a.auth_id != aut.auth_id && (a.number == aut.number || a.en_name == aut.en_name)).Count() > 0) {
                throw new Exception("编号或EN名称已存在，保存失败");
            }

            if (aut.auth_id > 0) {
                //修改
                var au = GetAutById(aut.auth_id);
                if (au == null) {
                    throw new Exception("权限id不存在");
                }

                au.number = aut.number;
                au.name = aut.name;
                au.en_name = aut.en_name;
                au.controller_name = aut.controller_name;
                au.action_name = aut.action_name;
                au.fa_name = aut.fa_name;
                au.btn_color = aut.btn_color;
                au.comment = aut.comment;
            }
            else {
                //新增
                db.Authorities.InsertOnSubmit(aut);
            }

            db.SubmitChanges();

        }

        public void RemoveAuth(int authId)
        {
            db.Authorities.DeleteAllOnSubmit(db.Authorities.Where(a => a.auth_id == authId));
            db.SubmitChanges();
        }

        public IQueryable<Groups> GetGroups(string groupType, string searchValue)
        {
            return db.Groups.Where(g => g.group_type == groupType && g.name.Contains(searchValue));
        }

        public List<GroupAndAllMembers> GetGroupAndAllMembers(string groupType, string searchValue)
        {
            var result = (from g in db.Groups
                          join gus in db.GroupUsers on g.group_id equals gus.group_id into gut
                          from gu in gut.DefaultIfEmpty()
                          join us in db.Users on gu.user_id equals us.user_id into ut
                          from u in ut.DefaultIfEmpty()
                          where g.group_type == groupType
                          select new
                          {
                              group_id = g.group_id,
                              name = g.name,
                              user_name = (u == null ? "" : u.real_name)
                          }).ToList();
            var gms = new List<GroupAndAllMembers>();
            foreach (var r in result.Where(re=>re.name.Contains(searchValue) || re.user_name.Contains(searchValue)).Select(re => new { re.group_id, re.name }).Distinct()) {
                gms.Add(new GroupAndAllMembers()
                {
                    group_id = r.group_id,
                    name = r.name,
                    allMembers = result.Where(re => re.group_id == r.group_id).Select(re => re.user_name).ToList()
                });
            }
            return gms;
        }

        public IQueryable<GroupUsersModel> GetGroupUsers(int groupId,string searchValue)
        {
            return (db.GroupUsers.Where(gu => gu.group_id == groupId && (gu.Users.user_name.Contains(searchValue) || gu.Users.real_name.Contains(searchValue)))
                .OrderBy(gu => gu.Users.user_name)
                .Select(gu => new GroupUsersModel()
            {
                group_user_id = gu.group_user_id,
                user_name = gu.Users.user_name,
                real_name = gu.Users.real_name
            }));
        }

        public IQueryable<GroupAuthModel> GetGroupAuts(int groupId, string searchValue)
        {            
            return (db.GroupAuthorities.Where(ga => ga.group_id == groupId && ga.Authorities.name.Contains(searchValue))
                .OrderBy(ga => ga.Authorities.number)                
                .Select(ga => new GroupAuthModel()
                {
                    group_auth_id = ga.group_auth_id,
                    auth_number = ga.Authorities.number,
                    auth_name = ga.Authorities.name
                }));
        }

        public void SaveGroup(Groups group)
        {
            if (db.Groups.Where(g => g.group_id != group.group_id && g.name == group.name).Count() > 0) {
                throw new Exception("组名已存在");
            }
            if (group.group_id > 0) {
                //修改
                Groups gr = db.Groups.Where(g => g.group_id == group.group_id).FirstOrDefault();
                if (gr == null) {
                    throw new Exception("组别不存在");
                }
                gr.name = group.name;
                gr.comment = group.comment;
            }
            else {
                db.Groups.InsertOnSubmit(group);
            }
            db.SubmitChanges();
        }

        public void RemoveGroup(int groupId)
        {
            var group = GetGroupById(groupId);

            db.GroupAuthorities.DeleteAllOnSubmit(group.GroupAuthorities);
            db.GroupUsers.DeleteAllOnSubmit(group.GroupUsers);
            db.Groups.DeleteOnSubmit(group);
            db.SubmitChanges();
        }

        public void SaveGroupAut(int groupId, int[] authIds)
        {
            foreach (var authId in authIds) {
                if (db.GroupAuthorities.Where(ga => ga.group_id == groupId && ga.auth_id == authId).Count() > 0) continue;
                db.GroupAuthorities.InsertOnSubmit(new GroupAuthorities()
                {
                    group_id = groupId,
                    auth_id = authId
                });
            }

            db.SubmitChanges();
        }

        public void RemoveGroupAut(int groupAutId)
        {
            db.GroupAuthorities.DeleteAllOnSubmit(db.GroupAuthorities.Where(ga => ga.group_auth_id == groupAutId));
            db.SubmitChanges();
        }

        public void SaveGroupUser(int groupId, int userId)
        {
            if (db.GroupUsers.Where(gu => gu.group_id == groupId && gu.user_id == userId).Count() > 0) {
                throw new Exception("该组别用户已存在，不能重复添加");
            }
            db.GroupUsers.InsertOnSubmit(new GroupUsers()
            {
                group_id = groupId,
                user_id = userId
            });
            db.SubmitChanges();
        }

        public void RemoveGroupUser(int groupUserId)
        {
            db.GroupUsers.DeleteAllOnSubmit(db.GroupUsers.Where(gu => gu.group_user_id == groupUserId));
            db.SubmitChanges();
        }

        public int GetGroupIdByName(string groupType,string groupName)
        {
            var gr = db.Groups.Where(g => g.group_type == groupType && g.name == groupName).FirstOrDefault();
            if (gr == null) return 0;
            return gr.group_id;
        }
    }
}