using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Models;
using VendorNew.Services;
using VendorNew.Filters;
using VendorNew.Utils;
using Newtonsoft.Json;

namespace VendorNew.Controllers
{
    public class DeliveryController : BaseController
    {
        private string innerWebSite = "http://192.168.90.100/VendorNew/";
        private string outerWebSite = "http://59.37.42.23/VendorNew/";

        /// <summary>
        /// 进入po查询界面
        /// </summary>
        /// <returns></returns>
        [AuthorityFilter]
        public ActionResult CheckPosForApply()
        {
            ViewData["addDRPower"] = new UASv().hasGotPower(currentUser.userId, "add_dr_apply") ? 1 : 0;

            return View();
        }

        /// <summary>
        /// 获取符合条件的po数据
        /// </summary>
        /// <param name="fc"></param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult GetPOs(FormCollection fc)
        {
            GetK3POParams p = new GetK3POParams();
            MyUtils.SetFieldValueToModel(fc, p);

            if (p.beginDate == DateTime.MinValue) {
                return Json(new SRM(false, "必须输入正确的开始日期"));
            }
            if (p.endDate == DateTime.MinValue) {
                return Json(new SRM(false, "必须输入正确的结束日期"));
            }
            if (p.beginDate > p.endDate) {
                return Json(new SRM(false, "开始日期不能大于结束日期"));
            }

            p.poNumbers = p.poNumbers == null ? "" : p.poNumbers.Trim();//去掉前后空格
            p.itemInfo = p.itemInfo == null ? "" : p.itemInfo.Trim();
            p.account = currentAccount;
            p.userId = currentUser.userId;
            p.userNumber = currentUser.userName;
            p.k3HasAudit = true;

            if (p.poNumbers.Length > 800) {
                return Json(new SRM(false, "订单编号字符数不能大于800个"));
            }

            List<K3POs> result;
            try {
                result = new DRSv().GetPOs(p);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            if (result.Count() == 0) {
                return Json(new SRM(false, "查询不到任何符合条件的记录"));
            }

            return Json(new { suc = true, rows = result.OrderBy(r => r.poDate) });

        }

        /// <summary>
        /// 获取某po的入库记录
        /// </summary>
        /// <param name="billType">订单类型</param>
        /// <param name="poInterID">po id</param>
        /// <param name="poEntryID">po entry id</param>
        /// <param name="contractEntryID">合同id</param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult GetPOInstockRecord(string billType, int poInterID, int poEntryID, int contractEntryID)
        {
            List<POInstockRecord> result;
            try {
                result = new DRSv().GetInstockRecord(currentAccount, billType, poInterID, poEntryID, contractEntryID);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            if (result.Count() == 0) {
                return Json(new SRM(false, "查询不到入库记录"));
            }
            return Json(new { suc = true, rows = result });

        }

        public JsonResult GetPOTransitQty(string interIds, string entryIds)
        {
            string[] interIdArr = interIds.Split(new char[]{','});
            string[] entryIdArr = entryIds.Split(new char[] { ',' });
            string[] result = new string[interIdArr.Length];
            for (var i = 0; i < interIdArr.Length; i++) {
                result[i] = new DRSv().GetPOTransitQty(Int32.Parse(interIdArr[i]), Int32.Parse(entryIdArr[i])).ToString("0.##");
            }
            return Json(string.Join(",", result));
        }

        /// <summary>
        /// 进入申请界面之前的验证和保存临时数据
        /// </summary>
        /// <param name="poJson">选取的po数据，将保存在临时字典中</param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult BeforeAddDRApply(string poJson)
        {
            List<K3POs> list = JsonConvert.DeserializeObject<List<K3POs>>(poJson);
                        
            decimal transitQty;
            foreach (var l in list) {
                transitQty = new DRSv().GetPOTransitQty(l.poId, l.poEntryId);
                if (l.orderQty - l.realteQty - transitQty <= 0) {
                    return Json(new SRM(false, string.Format("订单号【{0}】,分录号【{1}】的可申请数量不大于0，不能申请送货", l.poNo, l.poEntryId)));
                }
                l.transitQty = transitQty.ToString();
            }

            try {
                //验证没问题之后，将获取最新的在途数量的list转化为json后，保存在临时字典表
                var dicId = new ItemSv().SaveTempDic("add_dr_apply", JsonConvert.SerializeObject(list), currentAccount, currentUser.userName);
                return Json(new SRM(true, "", dicId.ToString()));
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

        }

        /// <summary>
        /// 进入申请界面
        /// </summary>
        /// <param name="id">临时字典id，可以标识url，因为url不同，标签就会刷新</param>
        /// <returns></returns>
        [AuthorityFilter]
        public ActionResult AddDRApply(int id)
        {
            string poJsonStr;
            try {
                poJsonStr = new ItemSv().GetTempDicValue(id, currentAccount, currentUser.userName);
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            var poList = JsonConvert.DeserializeObject<List<K3POs>>(poJsonStr);
            var poHead=poList.First();

            string billNo = new ItemSv().GetDRBillNo(poHead.account, "C");
            var drHead = new DRBills();
            drHead.account = poHead.account;
            drHead.bill_no = billNo;
            drHead.bill_type = poHead.billType;
            drHead.buy_type = poHead.buyType;
            drHead.currency_name = poHead.currencyName;
            drHead.currency_number = poHead.currencyNumber;
            drHead.department_name = poHead.departmentName;
            drHead.mat_order_name = poHead.matOrderName;
            drHead.mat_order_number = poHead.matOrderNumber;
            drHead.p_status = "未提交";
            drHead.supplier_name = poHead.supplierName;
            drHead.supplier_number = poHead.supplierNumber;
            drHead.trade_type_name = poHead.tradeTypeName;
            drHead.trade_type_number = poHead.tradeTypeNumber;

            var drDetails = new List<DRBillDetails>();
            int entryId = 1;
            foreach (var e in poList) {
                drDetails.Add(new DRBillDetails()
                {
                    entry_id = entryId++,
                    item_number = e.itemNumber,
                    item_model = e.itemModel,
                    item_name = e.itemName,
                    unit_name = e.unitName,
                    unit_number = e.unitNumber,
                    po_qty = e.orderQty,
                    send_qty = e.orderQty - e.realteQty - decimal.Parse(e.transitQty),
                    can_send_qty = e.orderQty - e.realteQty - decimal.Parse(e.transitQty),
                    po_number = e.poNo,
                    po_id = e.poId,
                    po_entry_id = e.poEntryId,
                    po_date = e.poDate,
                    pr_number = e.prNo,
                    pr_entry_id = e.prEntryID,
                    buyer_name = e.buyerName,
                    buyer_number = e.buyerNumber,
                    contract_entry_id=e.contractEntryId
                });
            }

            ViewData["drHead"] = drHead;
            ViewData["drDetails"] = drDetails;

            WLog("新增送货单", "进入新增界面，系统分配流水号", billNo);

            return View();
        }
        
        [SessionTimeOutJsonFilter]
        public JsonResult SaveOuterBoxes(FormCollection fc)
        {
            OuterBoxes box = new OuterBoxes();
            MyUtils.SetFieldValueToModel(fc, box);

            if (box.package_date == DateTime.MinValue) {
                return Json(new SRM(false, "请填写正确的包装日期"));
            }
            if (box.produce_date == DateTime.MinValue) {
                return Json(new SRM(false, "请填写正确的生产日期"));
            }
            
            List<OuterBoxPOs> poList = JsonConvert.DeserializeObject<List<OuterBoxPOs>>(fc.Get("poRows"));

            try {
                string boxNumber = new BoxSv().SaveOuterBox(box, poList);
                WLog("新增外箱", "保存外箱，箱号：" + boxNumber);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }

            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveInnerBox(FormCollection fc)
        {
            InneBoxes ib = new InneBoxes();
            MyUtils.SetFieldValueToModel(fc, ib);

            try {
                string[] innerBox = new BoxSv().SaveInnerBox(ib);
                WLog("新增内箱", "保存内箱，箱号：" + innerBox[0]);

                return Json(new { suc = true, msg = "内箱保存成功", boxNumber = innerBox[0], boxId = innerBox[1] });
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
        }

        [SessionTimeOutJsonFilter]
        public JsonResult RemoveOuterBox(int outerBoxId)
        {
            try {
                string boxNumber = new BoxSv().RemoveOuterBox(outerBoxId);
                WLog("删除外箱", "箱号是：" + boxNumber);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult RemoveInnerBox(int innerBoxId)
        {
            try {
                string boxNumber = new BoxSv().RemoveInnerBox(innerBoxId);
                WLog("删除内箱", "箱号：" + boxNumber);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        /// <summary>
        /// 移出外箱
        /// </summary>
        /// <param name="outerBoxId"></param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult MoveOutBox(int outerBoxId)
        {
            try {
                new BoxSv().MoveOutBox(outerBoxId);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        /// <summary>
        /// 在申请页面获取所有关联PO的外箱树，部分关联的剔除
        /// </summary>
        /// <param name="poInfo">po id 和 entryid 信息</param>
        /// <param name="exceptBoxIds">移出外箱id信息</param>
        /// <param name="supplierNumber">供应商编码</param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult LoadBoxDatas(string poInfo, string exceptBoxIds, string supplierNumber, int billId)
        {
            List<IDModel> idList = JsonConvert.DeserializeObject<List<IDModel>>(poInfo);
            List<IDModel> excepteIdList = JsonConvert.DeserializeObject<List<IDModel>>(exceptBoxIds);

            try {
                var result = new BoxSv().GetOuterBoxesByPoInfo(currentAccount, supplierNumber, idList, excepteIdList,billId);
                return Json(new { suc = true, result = result });
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }            
        }

        /// <summary>
        /// 拆分外箱
        /// </summary>
        /// <param name="outerBoxId"></param>
        /// <param name="splitNum"></param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult SplitOuterBox(int outerBoxId, int splitNum)
        {
            try {
                string[] boxArr = new BoxSv().SplitOuterbox(outerBoxId, splitNum);
                WLog("拆分外箱", string.Format("外箱箱号为【{0}】拆分为【{1}】和【{2}】", boxArr[0], boxArr[1], boxArr[2]));
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        /// <summary>
        /// 保存送货申请
        /// </summary>
        /// <param name="drJson">表头json</param>
        /// <param name="detailJson">表体json</param>
        /// <param name="boxIdJson">箱子id json</param>
        /// <param name="aFlag">true表示保存且提交，false表示只保存不提交</param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult SaveApply(string drJson, string detailJson, string boxIdJson,bool aFlag)
        {
            DRBills bill;
            List<DRBillDetails> details;
            List<IDModel> boxIds;
            var sv = new DRSv();
            int billId;

            try {
                bill = JsonConvert.DeserializeObject<DRBills>(drJson);
                details = JsonConvert.DeserializeObject<List<DRBillDetails>>(detailJson);
                boxIds = JsonConvert.DeserializeObject<List<IDModel>>(boxIdJson);

                bill.user_id = currentUser.userId;
                bill.bill_date = DateTime.Now;
                billId = sv.SaveApply(bill, details, boxIds);

            }
            catch (Exception ex) {                
                return Json(new SRM(ex)); //保存失败的，suc=false，页面不跳转
            }

            //page为1跳转到修改编辑界面，page为2跳转到只读页面
            if (aFlag) {
                try {
                    sv.BeforeApply(bill, details, boxIds);
                    sv.beginApply(billId,currentUser.realName);

                    //发送待处理邮件给订料员
                    SendNotifyEmail(billId, "提交");
                    WLog("提交申请单", "提交成功", bill.bill_no);
                }
                catch (Exception ex) {
                    return Json(new { suc = true, msg = "保存申请成功，但是提交申请失败。<br/>原因：" + ex.Message, page = 1, id = billId });
                }                

                return Json(new { suc = true, msg = "已成功保存并提交申请", page = 2, id = billId, bill_no = bill.bill_no });
            }
            else {
                WLog("保存申请单", "保存成功", bill.bill_no);
                return Json(new { suc = true, msg = "已成功保存申请", page = 1, id = billId });
            }
        }

        /// <summary>
        /// 修改申请单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuthorityFilter]
        public ActionResult ModifyDRApply(int id)
        {
            var sv = new DRSv();
            var h = sv.GetDRBill(id);

            if (h == null) {
                ViewBag.tip = "单据不存在，可能已被删除";
                return View("Error");
            }

            if (!new string[] { "未提交", "已拒绝" }.Contains(h.p_status)) {
                ViewBag.tip = "当前申请单状态是：" + h.p_status + ",不能修改";
                return View("Error");
            }

            var details = sv.GetDRBillDetails(id);

            //重新再计算一次可申请数量
            decimal stockQty, transitQty;             
            foreach (var e in details) {
                stockQty = sv.GetInstockQty(h.account, h.bill_type, (int)e.po_id, (int)e.po_entry_id); // K3已入库数量
                transitQty = sv.GetPOTransitQty((int)e.po_id, (int)e.po_entry_id); //在途数量
                e.can_send_qty = e.po_qty - stockQty - transitQty + e.send_qty ?? 0m; //可申请数量=订单数量-入库数量-在途数量+本次申请数量
            }

            ViewData["drHead"] = h;
            ViewData["drDetails"] = details;

            WLog("修改申请单", "进入修改页面", h.bill_no);

            return View("AddDRApply");
        }


        [SessionTimeOutFilter]
        public ActionResult CheckDRApply(int id)
        {
            var sv = new DRSv();
            var dr=sv.GetDRBill(id);

            ViewData["drHead"] = dr;
            ViewData["drDetails"] = sv.GetDRBillDetails(id);

            WLog("查看申请单", "查看详情", dr == null ? "" : dr.bill_no);
            return View();
        }

        [AuthorityFilter]
        public ActionResult CheckMyApplies()
        {
            var sv = new UASv();
            ViewData["modifyPower"] = sv.hasGotPower(currentUser.userId, "modify_apply") ? "1" : "0";
            ViewData["undoPower"] = sv.hasGotPower(currentUser.userId, "undo_apply") ? "1" : "0";
            ViewData["deletePower"] = sv.hasGotPower(currentUser.userId, "delete_apply") ? "1" : "0";
            return View();
        }

        /// <summary>
        /// 获取我的申请记录
        /// </summary>
        /// <param name="fc"></param>
        /// <returns></returns>
        [SessionTimeOutJsonFilter]
        public JsonResult GetMyAppliesData(FormCollection fc)
        {
            SearchMyApplyParams p = new SearchMyApplyParams();
            MyUtils.SetFieldValueToModel(fc, p);

            p.account = currentAccount;
            p.userId = currentUser.userId;
            p.userName = currentUser.userName;            

            var result = new DRSv().SearchMyApplyList(p, canCheckAll);

            return Json(new { total = result.Count(), rows = result.Skip((p.page - 1) * p.rows).Take(p.rows).ToList() });
        }

        [AuthorityFilter]
        public ActionResult CheckMyAuditingList()
        {
            UASv sv = new UASv();
            ViewData["confirmPower"] = sv.hasGotPower(currentUser.userId, "confirm_apply") ? "1" : "0";
            ViewData["printApplyPower"] = sv.hasGotPower(currentUser.userId, "print_apply") ? "1" : "0";
            ViewData["printOuterBoxPower"] = sv.hasGotPower(currentUser.userId, "print_outer_qrcode") ? "1" : "0";
            ViewData["printInnerBoxPower"] = sv.hasGotPower(currentUser.userId, "print_inner_qrcode") ? "1" : "0";
            ViewData["exportExcelPower"] = sv.hasGotPower(currentUser.userId, "export_audit_list_excel") ? "1" : "0";

            return View();
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SearchMyAuditingList(FormCollection fc)
        {
            SearchMyApplyParams p = new SearchMyApplyParams();
            MyUtils.SetFieldValueToModel(fc, p);

            p.account = currentAccount;
            p.userId = currentUser.userId;
            p.userName = currentUser.userName;

            var result = new DRSv().SearchMyAuditList(p, canCheckAll);

            return Json(new { total = result.Count(), rows = result.Skip((p.page - 1) * p.rows).Take(p.rows).ToList() });
        }

        [SessionTimeOutJsonFilter]
        public JsonResult DeleteDR(int billId,bool alsoDeleteBox)
        {
            try {
                var billNo = new DRSv().DeleteDR(billId, alsoDeleteBox, currentUser.userId, currentUser.userName);
                WLog("删除申请单", alsoDeleteBox ? "同时删除关联箱子" : "不删除关联箱子", billNo);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult UndoDR(int billId,string pStatusNow)
        {
            try {
                new DRSv().UpdatePStatus(billId, currentUser.realName, pStatusNow, "未提交", "撤销申请");
                //发送通知邮件给订料员
                SendNotifyEmail(billId, "撤销");                
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }
        
        [SessionTimeOutJsonFilter]
        public JsonResult GetDROpRecord(string billNo)
        {
            var list = new DRSv().GetOpRecord(billNo);
            if (list.Count() == 0) {
                return Json(new SRM(false, "此申请没有任何操作记录"));
            }
            return Json(new { suc = true, result = list });
        }

        [AuthorityFilter]
        public ActionResult ConfirmApply(int billId)
        {
            var dr = new DRSv().GetDRBill(billId);
            if (dr == null) {
                ViewBag.tip = "单据不存在，可能已被删除";
                return View("Error");
            }
            if (!new string[] { "已提交" }.Contains(dr.p_status)) {
                ViewBag.tip = "只有申请状态是已提交的单据才能处理，当前单据申请状态是：" + dr.p_status;
                return View("Error");
            }

            if (!dr.mat_order_number.Equals(currentUser.userName)) {
                if (!new UASv().hasGotPower(currentUser.userId, "audit_all_bills")) {
                    if (new UASv().GetAuditGroupUsers(currentUser.userId).Where(u => u.user_name == dr.mat_order_number).Count() == 0) {
                        ViewBag.tip = "你没有此单据的处理权限";
                        return View("Error");
                    }
                }
            }
            
            ViewData["billId"] = billId;
            ViewData["billNo"] = dr.bill_no;

            return View();
        }

        [SessionTimeOutJsonFilter]
        public JsonResult BeginHandleApply(int billId, string opinion, bool isOk)
        {            
            try {
                new DRSv().UpdatePStatus(billId, currentUser.realName, "已提交", (isOk ? "已确认" : "已拒绝"), (isOk ? "确认申请单" : "拒绝申请单"), opinion);
                //发送邮件给供应商
                SendNotifyEmail(billId, isOk ? "确认" : "拒绝");
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());

        }

        [SessionTimeOutJsonFilter]
        public JsonResult TakeBackApply(int billId,string pStatus)
        {
            var dr = new DRSv().GetDRBill(billId);
            if (dr != null) {
                if (!dr.mat_order_number.Equals(currentUser.userName)) {
                    if (!new UASv().hasGotPower(currentUser.userId, "audit_all_bills")) {
                        if (new UASv().GetAuditGroupUsers(currentUser.userId).Where(u => u.user_name == dr.mat_order_number).Count() == 0) {
                            return Json(new SRM(false, "你没有此单据的反审核权限"));
                        }
                    }
                }
            }

            try {
                new DRSv().UpdatePStatus(billId, currentUser.realName, pStatus, "已提交", "反审核申请单");
                //发送邮件给供应商
                SendNotifyEmail(billId, "反审核");
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        /// <summary>
        /// 发送通知邮件
        /// </summary>
        /// <param name="billId"></param>
        /// <param name="opType"></param>
        private void SendNotifyEmail(int billId,string opType){            
            var dr = new DRSv().GetDRBill(billId);
            if (dr == null) return;

            string subject, content,emailAddr;
            switch (opType) {
                case "提交":
                    emailAddr = GetMatOrderEmail(dr);
                    subject="你有一张待审核的供应商送货申请单";
                    content = "<div style='font-family:Microsoft YaHei'><div>你好：</div>";
                    content += "<div style='margin-left:30px;'>";
                    content += string.Format("你有一张待处理的单号为【{0}】的送货申请单，来自【{3}】，供应商【{1}({2})】。请尽快登录平台处理。", dr.bill_no, dr.supplier_name, dr.supplier_number, dr.account == "S" ? "信利半导体有限公司" : "信利光电股份有限公司");
                    content += "</div>";
                    content += "<div style='clear:both'><br />单击以下链接可进入平台处理这张申请单：</div>";
                    content += string.Format("<div><a href='{0}{1}{2}' style='color:#337ab7;text-decoration:none;'>内网用户请点击此链接</a></div>", innerWebSite, "Delivery/ConfirmApply?billId=", billId);
                    content += string.Format("<div><a href='{0}{1}{2}' style='color:#337ab7;text-decoration:none;'>外网用户请点击此链接</a></div>", outerWebSite, "Delivery/ConfirmApply?billId=", billId);
                    content += "</div>";
                    break;
                case "确认":
                case "拒绝":
                case "反审核":
                    emailAddr = getSupplierEmail(dr);
                    subject = "你有一张送货申请单已被订料员" + opType;
                    content = "<div style='font-family:Microsoft YaHei'><div>你好：</div>";
                    content += "<div style='margin-left:30px;'>";
                    content += "你有一张单号为【" + dr.bill_no + "】的送货申请单已被订料员" + opType+"。";
                    content += "</div>";
                    content += "<div style='clear:both'><br />单击以下链接可进入平台查看申请单详情：</div>";
                    content += string.Format("<div><a href='{0}{1}{2}' style='color:#337ab7;text-decoration:none;'>请点击此链接查看详情</a></div>", outerWebSite, "Delivery/CheckDRApply?id=", billId);
                    content += "</div>";
                    break;
                case "撤销":
                    emailAddr = GetMatOrderEmail(dr);
                    subject = "供应商撤销了一张送货申请单";
                    content = "<div style='font-family:Microsoft YaHei'><div>你好：</div>";
                    content += "<div style='margin-left:30px;'>";
                    content += "单号为【" + dr.bill_no + "】的送货申请单已被供应商撤销。";
                    content += "</div>";
                    content += "<div style='clear:both'><br />单击以下链接可进入平台查看申请单详情：</div>";
                    content += string.Format("<div><a href='{0}{1}{2}' style='color:#337ab7;text-decoration:none;'>内网用户请点击此链接</a></div>", innerWebSite, "Delivery/CheckDRApply?id=", billId);
                    content += string.Format("<div><a href='{0}{1}{2}' style='color:#337ab7;text-decoration:none;'>外网用户请点击此链接</a></div>", outerWebSite, "Delivery/CheckDRApply?id=", billId);
                    content += "</div>";
                    break;
                default:
                    return;                   
            }
            WLog("发送通知邮件", opType, dr.bill_no);
            MyEmail.SendEmail(subject, emailAddr, content);
        }

        /// <summary>
        /// 订料员邮箱
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        private string getSupplierEmail(DRBills bill)
        {
            var user = new UserSv().GetUserByUserName(bill.supplier_number);
            if (user == null) return "";
            return user.email ?? "";            
        }

        /// <summary>
        /// 订料员邮箱
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        private string GetMatOrderEmail(DRBills bill)
        {            
            var mat = new UserSv().GetUserByUserName(bill.mat_order_number);
            if (mat == null) return "";

            List<string> emails = new List<string>();
            if (mat.email != null) {
                emails.Add(mat.email);
            }
            var matGroupUser = new UASv().GetAuditGroupUsers(mat.user_id);
            emails.AddRange(matGroupUser.Select(m => m.email).ToList());

            return string.Join(",", emails.Distinct().ToArray());
        }

    }
}
