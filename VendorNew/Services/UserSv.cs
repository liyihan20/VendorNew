using System;
using System.Collections.Generic;
using System.Linq;
using VendorNew.Models;
using VendorNew.Utils;

namespace VendorNew.Services
{
    public class UserSv:BaseSv
    {
        public Users GetUserByUserName(string userName)
        {
            return db.Users.Where(u => u.user_name == userName).FirstOrDefault();
        }

        public Users GetUserByUserId(int userId)
        {
            return db.Users.Where(u => u.user_id == userId).FirstOrDefault();
        }

        /// <summary>
        /// 用户的登录验证
        /// </summary>
        /// <param name="userName">登录用户名</param>
        /// <param name="password">登录密码</param>
        /// <returns>users对象，如果验证不通过则抛出异常</returns>
        public Users StartLogin(string userName,string password)
        {
            int maxErrorTimes = 5;
            Users user = GetUserByUserName(userName);
            //Users user = db.Users.Where(u => u.user_name == userName).FirstOrDefault();
            if (user == null) {
                throw new Exception("用户不存在");
            }
            if (user.is_forbit) {
                throw new Exception("用户已被禁用，原因：" + user.forbit_reason);
            }
            if (!user.password.Equals(MyUtils.getMD5(password))) {
                string msg = "";
                user.continual_error_times = user.continual_error_times == null ? 1 : user.continual_error_times + 1;
                if (user.continual_error_times >= maxErrorTimes) {
                    user.is_forbit = true;
                    user.forbit_date = DateTime.Now;
                    user.forbit_reason = msg = "密码连续输入错误次数达到" + maxErrorTimes + "次，被禁用";
                }
                else {
                    msg = "密码错误，还剩下" + (maxErrorTimes - user.continual_error_times) + "次尝试机会";
                }
                db.SubmitChanges();
                throw new Exception(msg);
            }
            //成功登录
            user.continual_error_times = 0;
            user.last_login_date = DateTime.Now;
            db.SubmitChanges();

            return user;
        }

        public IQueryable<Users> GetUsers(string searchValue)
        {
            return db.Users.Where(u => u.user_name.Contains(searchValue) || u.real_name.Contains(searchValue));
        }

        public void SaveUser(Users user)
        {
            if (db.Users.Where(u => u.user_id != user.user_id && u.user_name == user.user_name).Count() > 0) {
                throw new Exception("登录名已存在，保存失败");
            }

            if (user.user_id > 0) {
                //修改
                var us = GetUserByUserId(user.user_id);
                if (us == null) {
                    throw new Exception("用户不存在，不能编辑");
                }

                us.user_name = user.user_name;
                us.real_name = user.real_name;
                us.email = user.email;
                us.comment = user.comment;
                us.user_role = user.user_role;
            }
            else {
                //新增
                user.is_forbit = false;
                user.continual_error_times = 0;
                user.in_date = DateTime.Now;
                user.password = MyUtils.getMD5(user.user_name);

                db.Users.InsertOnSubmit(user);

                db.SubmitChanges();

                //根据角色增加到权限组中
                int groupId = new UASv().GetGroupIdByName("auth",user.user_role + "组");
                if (groupId > 0) {
                    new UASv().SaveGroupUser(groupId, user.user_id);
                }

            }
            
            db.SubmitChanges();
        }

        public void ResetPassword(int userId)
        {
            var user = GetUserByUserId(userId);
            if (user == null) {
                throw new Exception("用户id不存在");
            }
            user.password = MyUtils.getMD5(user.user_name);
            db.SubmitChanges();
        }

        public void ToggleUser(int userId)
        {
            var user = GetUserByUserId(userId);
            if (user == null) {
                throw new Exception("用户id不存在");
            }
            if (user.is_forbit) {
                user.is_forbit = false;
                user.forbit_reason = null;
                user.forbit_date = null;
            }
            else {
                user.is_forbit = true;
                user.forbit_reason = "后台管理员禁用";
                user.forbit_date = DateTime.Now;
            }
            db.SubmitChanges();
        }


        public string GetEmailAddr(int userId)
        {
            return GetUserByUserId(userId).email ?? "";
        }

        /// <summary>
        /// 修改邮箱地址，最多可保存3个邮箱，邮箱地址之间用逗号隔开
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="emailAddr">邮箱地址</param>
        /// <param name="index">0表示第一个，1表示第二个，2表示第三个</param>
        public void UpdateEmailAddr(int userId, string emailAddr, int index)
        {
            var user = GetUserByUserId(userId);
            var email = user.email;
            if (string.IsNullOrWhiteSpace(email)) email = "";
            var emailArr = new string[3]; //最多只能保存3个邮箱地址
            email.Split(',').CopyTo(emailArr, 0);

            emailArr[index] = emailAddr;
            user.email = string.Join(",", emailArr);
            db.SubmitChanges();
        }

        public void ResetPassword(int userId, string oldP, string newP)
        {
            var user = GetUserByUserId(userId);

            if (user.user_name.Equals(newP)) {
                throw new Exception("新密码不能与用户名一致，请重新设置");
            }

            oldP = MyUtils.getMD5(oldP);
            newP = MyUtils.getMD5(newP);
            if (user.password != oldP) {
                throw new Exception("当前密码不正确，重置失败");
            }
            user.password = newP;
            db.SubmitChanges();
        }

        public bool IsPasswordSameWithLoginName(int userId)
        {
            var user = GetUserByUserId(userId);
            return user.password == MyUtils.getMD5(user.user_name);
        }
    }
}