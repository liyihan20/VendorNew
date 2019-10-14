using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorNew.Models;
using VendorNew.Utils;

namespace VendorNew.Services
{
    public class ItemSv:BaseSv
    {
        /// <summary>
        /// 保存数据到临时字典表，例如选择了PO之后，跳转到新标签的临时数据
        /// 因为在url中直接带参数传递的话，值太长且不安全
        /// </summary>
        /// <param name="key">字典key</param>
        /// <param name="value">字典value</param>
        /// <param name="account">账套</param>
        /// <param name="userName">用户名</param>
        /// <returns>临时字典id</returns>
        public int SaveTempDic(string key, string value, string account, string userName)
        {
            var td = new TempDic()
            {
                account = account,
                user_name = userName,
                t_key = key,
                t_value = value
            };
            db.TempDic.InsertOnSubmit(td);
            db.SubmitChanges();

            return td.id;
        }

        /// <summary>
        /// 获取临时字典数据
        /// </summary>
        /// <param name="id">字典id</param>
        /// <param name="userName">用户名，用于验证</param>
        /// <returns>字典value</returns>
        public string GetTempDicValue(int id,string account, string userName)
        {
            var dic = db.TempDic.FirstOrDefault(d => d.id == id);
            if (dic == null) {
                throw new Exception("数据获取失败");
            }
            if (!userName.Equals(dic.user_name)) {
                throw new Exception("无权限获取数据");
            }
            if (!account.Equals(dic.account)) {
                throw new Exception("登录公司名不匹配");
            }
            return dic.t_value;
        }

        /// <summary>
        /// 获取送货流水编号
        /// </summary>
        /// <param name="account">账套，S为半导体，O为光电</param>
        /// <param name="local">送货地点，C为中国大陆，H为香港</param>
        /// <returns></returns>
        public string GetDRBillNo(string account,string local)
        {
            string prefix1 = account + local + "DR";
            string prefix2 = DateTime.Now.ToString("yyMMdd");

            return GetSystemNo(prefix1, prefix2, 3);
        }
                

        /// <summary>
        /// 获取流水号
        /// </summary>
        /// <param name="prefix1">前缀1</param>
        /// <param name="prefix2">前缀2</param>
        /// <param name="digitPerDay">每日位数，3表示001~999</param>
        /// <param name="num">需要获取的序号个数，默认是1个</param>
        /// <returns></returns>
        private string GetSystemNo(string prefix1, string prefix2, int digitPerDay,int num = 1)
        {
            var nowYear = DateTime.Now.ToString("yyyy");
            var currentRecord = db.SystemNos.Where(s => s.prefix1 == prefix1 && s.prefix2 == prefix2 && s.year_str == nowYear).FirstOrDefault();
            int currentNumber;
            if (currentRecord == null) {
                db.SystemNos.InsertOnSubmit(new SystemNos()
                {
                    prefix1 = prefix1,
                    prefix2 = prefix2,
                    year_str = nowYear,
                    current_num = num
                });
                currentNumber = num;
            }
            else {
                currentNumber = currentRecord.current_num + num;
                currentRecord.current_num = currentNumber;
            }
            db.SubmitChanges();

            string result = string.Format("{0}{1}{2:D" + digitPerDay + "}", prefix1, prefix2, currentNumber);
            
            return result;
        }

        /// <summary>
        /// 获取箱号，可以一次性获取多个
        /// </summary>
        /// <param name="boxType">O表示外箱，I表示内箱</param>
        /// <param name="packNum">件数</param>
        /// <returns>2个字符串，第一个是箱号简写（例如42A001~42A003），第二个是箱号明细（例如42A001,42A002,42A003）</returns>
        public string[] GetBoxNumber(string boxType, int packNum)
        {
            int digitPerDay = 4;
            string dateStr = MyUtils.GetBoxDayStr(); //获取周和日编码
            string maxBoxNumber = GetSystemNo(boxType, dateStr, digitPerDay, packNum); 

            if (packNum == 1) return new string[] { maxBoxNumber, maxBoxNumber };

            string shortBoxNumber = "", longBoxNumber = "", currentBoxNumber = "";
            int maxNum = int.Parse(maxBoxNumber.Substring(boxType.Length + dateStr.Length));
            for (int i = maxNum - packNum + 1; i <= maxNum; i++) {
                currentBoxNumber = string.Format("{0}{1}{2:D" + digitPerDay + "}", boxType, dateStr, i);

                //箱号简写
                if (i == maxNum - packNum + 1) shortBoxNumber = currentBoxNumber + "~"; //第一个
                if (i == maxNum) shortBoxNumber += currentBoxNumber; //最后一个

                //箱号全写
                longBoxNumber += currentBoxNumber + ",";
            }

            return new string[] { shortBoxNumber, longBoxNumber };
        }

        /// <summary>
        /// 获取供应商名称
        /// </summary>
        /// <param name="supplierNumber">供应商编码</param>
        /// <returns></returns>
        public string GetSupplierNameByNumber(string supplierNumber)
        {
            var supplierName = db.Users.Where(u => u.user_name == supplierNumber).Select(u => u.real_name).FirstOrDefault();
            if (supplierName == null) {
                if (supplierNumber.EndsWith("A")) {
                    supplierNumber = supplierNumber.Replace("A", "");
                }
                var supplier = db.GetSupplierNameByNumber(supplierNumber).FirstOrDefault();
                if (supplier != null) {
                    supplierName = supplier.FName;
                }
            }
            return supplierName ?? "";
        }

        /// <summary>
        /// 取得k3职员姓名
        /// </summary>
        /// <param name="empNumber">职员厂牌</param>
        /// <returns></returns>
        public string GetEmpNameByNumber(string empNumber)
        {
            return db.GetEmpNameByNumber(empNumber).ToList().Select(e => e.FName).FirstOrDefault() ?? "";            
        }

        public supplierInfo GetSupplierInfo(string supplierNumber,string account)
        {
            var sInfo = db.GetSupplierInfo(supplierNumber, account).ToList();

            if (sInfo.Count() == 0) {
                throw new Exception("供应商基础资料不存在");
            }

            var info = sInfo.First();
            return new supplierInfo()
                {
                    supplierNumber = info.suppier_number,
                    supplierName = info.supplier_name,
                    supplierAddr = info.supplier_addr,
                    supplierAttn = info.supplier_attn,
                    supplierPhone = info.supplier_phone
                };
        }

        public void SaveSupplierInfo(supplierInfo info,string account)
        {
            var inf = db.SupplierInfo.Where(s => s.supplier_number == info.supplierNumber && s.account == account).FirstOrDefault();
            if (inf == null) {
                inf = new SupplierInfo();
                inf.supplier_number = info.supplierNumber;
                inf.name = info.supplierName;
                inf.account = account;
                db.SupplierInfo.InsertOnSubmit(inf);
            }
            inf.attn_name = info.supplierAttn;
            inf.address = info.supplierAddr;
            inf.phone = info.supplierPhone;
            inf.update_date = DateTime.Now;

            db.SubmitChanges();
        }

        public List<UpdateLog> GetUpdateLogs(string searchValue)
        {
            return db.UpdateLog.Where(u => u.update_content.Contains(searchValue)).OrderByDescending(u=>u.update_date).ToList();
        }

        public void SaveUpdateLog(UpdateLog log)
        {
            if (log.id == 0) {
                db.UpdateLog.InsertOnSubmit(log);
            }
            else {
                var l = db.UpdateLog.Where(u => u.id == log.id).FirstOrDefault();
                if (l != null) {
                    l.update_date = log.update_date;
                    l.update_content = log.update_content;
                }
            }
            db.SubmitChanges();
        }

        public void RemoveUpdateLog(int id)
        {
            db.UpdateLog.DeleteAllOnSubmit(db.UpdateLog.Where(u => u.id == id).ToList());
            db.SubmitChanges();
        }

        public List<Vw_K3_Item> SearchK3Item(string itemModel,string account)
        {
            return db.Vw_K3_Item.Where(v => v.item_model == itemModel && v.account == account).ToList();
        }

    }
}