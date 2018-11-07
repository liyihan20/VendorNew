using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorNew.Models;
using Newtonsoft.Json;

namespace VendorNew.Services
{
    public class DRSv:BaseSv
    {
        /// <summary>
        /// 获取k3中的po信息
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public List<K3POs> GetPOs(GetK3POParams p)
        {
            return db.ExecuteQuery<K3POs>(
                "exec GetK3POs @account = {0},@billType = {1},@poNumbers = {2},@itemInfo = {9},@beginDate = {3},@endDate = {4},@userNumber = {5},@userId = {6},@k3HasAudit = {7},@isFinished = {8}",
                p.account, p.billType, p.poNumbers, p.beginDate.ToString(), p.endDate.ToString(), p.userNumber, p.userId, p.k3HasAudit, p.isFinished,p.itemInfo).ToList();
        }

        /// <summary>
        /// 获取po的入库记录
        /// </summary>
        /// <param name="account"账套></param>
        /// <param name="billType">订单类型</param>
        /// <param name="poInterId">po id</param>
        /// <param name="poEntryId">po分录id</param>
        /// <param name="contractEntryId">合同分录id</param>
        /// <returns></returns>
        public List<POInstockRecord> GetInstockRecord(string account, string billType, int poInterId, int poEntryId, int contractEntryId)
        {
            return db.ExecuteQuery<POInstockRecord>(
                "exec GetInStockRecord @account = {0},@billType = {1},@poInterId = {2},@poEntryId = {3},@contractEntryId = {4}",
                account, billType, poInterId, poEntryId, contractEntryId).OrderBy(p => p.inDate).ToList();
        }
        
        public decimal GetInstockQty(string account, string billType, int poInterId, int poEntryId)
        {
            return db.ExecuteQuery<decimal>("exec GetInStockQty @account = {0},@billType = {1},@poInterId = {2},@poEntryId = {3}",
                account, billType, poInterId, poEntryId).FirstOrDefault();
        }
        
        public decimal GetPOTransitQty(int poId, int entryId)
        {
            var result = (from de in db.DRBillDetails
                          join d in db.DRBills on de.bill_id equals d.bill_id
                          where d.p_status != "已入库"
                          && de.po_id == poId && de.po_entry_id == entryId
                          select de.send_qty).Sum();
            return result ?? 0;
        }

        /// <summary>
        /// 提交之前验证
        /// </summary>
        /// <param name="h"></param>
        /// <param name="es"></param>
        /// <param name="boxes"></param>
        public void BeforeApply(DRBills h, List<DRBillDetails> es, List<IDModel> boxIds)
        {
            //1. 先验证可申请数量，需要获取最新的入库数量
            decimal stockQty, transitQty, canApplyQty;
            foreach (var e in es) {
                stockQty = GetInstockQty(h.account, h.bill_type, (int)e.po_id, (int)e.po_entry_id); // K3已入库数量
                transitQty = GetPOTransitQty((int)e.po_id, (int)e.po_entry_id); //在途数量
                canApplyQty = e.po_qty - stockQty - transitQty + e.send_qty ?? 0m; //可申请数量=订单数量-入库数量-在途数量+本次申请数量

                if (canApplyQty < e.send_qty) {
                    throw new Exception(string.Format("订单号【{0}】分录号【{1}】的可申请数量【{2:0.####}】少于申请数量【{3:0.####}】",e.po_number,e.po_entry_id,canApplyQty,e.send_qty));
                }
            }
                        
            if (boxIds.Count() > 0) {
                var outerBoxIds = boxIds.Select(b => b.interId).ToList();
                var oboxes = (from o in db.OuterBoxes
                              join pos in db.OuterBoxPOs on o.outer_box_id equals pos.out_box_id
                              join ib in db.InneBoxes on o.outer_box_id equals ib.outer_box_id into tib
                              from i in tib.DefaultIfEmpty()
                              select new
                              {
                                  box = o,
                                  po = pos,
                                  ibox = i
                              }).ToList();

                //2. 先验证箱内po数量是否等于送货数量
                foreach (var e in es) {
                    var inBoxQty = oboxes.Where(o => o.po.po_number == e.po_number && o.po.po_entry_id == e.po_entry_id)
                        .Select(o => new { bx = o.box, p = o.po }).Distinct()
                        .Sum(o => o.p.send_num * o.bx.pack_num);
                    if (e.send_qty != inBoxQty) {
                        throw new Exception(string.Format("订单号【{0}】分录号【{1}】的申请数量【{2:0.####}】不等于箱内数量【{3:0.####}】", e.po_number, e.po_entry_id, e.send_qty, inBoxQty));
                    }
                }

                //3. 验证有内箱的，内箱总数量是否等于外箱数量
                foreach (var box in oboxes.Where(o => o.ibox != null).Select(o => o.box).Distinct()) {
                    var innerBoxQty = oboxes.Where(o => o.box == box).Select(o => o.ibox).Distinct().Sum(i => i.every_qty * i.pack_num);
                    if (box.every_qty * box.pack_num != innerBoxQty) {
                        throw new Exception(string.Format("外箱箱号【{0}】的总数量【{1:0.####}】不等于内箱总数量【{2:0.####}】", box.box_number, box.every_qty * box.pack_num, innerBoxQty));
                    }
                }

            }

        }

        /// <summary>
        /// 保存申请
        /// </summary>
        /// <param name="h">表头</param>
        /// <param name="es">表体</param>
        /// <param name="boxes">箱子id</param>
        /// <returns>申请单id</returns>
        public int SaveApply(DRBills h, List<DRBillDetails> es, List<IDModel> boxes)
        {
            if (h.bill_id == 0) {
                //新增
                //先保存表头            
                try {
                    h.p_status = "未提交";
                    db.DRBills.InsertOnSubmit(h);
                    db.SubmitChanges();
                }
                catch (Exception ex) {
                    throw new Exception("抬头信息保存失败，原因：" + ex.Message);
                }

                foreach (var e in es) {
                    e.bill_id = h.bill_id;
                }
                foreach (var b in boxes) {
                    var box = db.OuterBoxes.Where(o => o.outer_box_id == b.interId).FirstOrDefault();
                    if (box != null) {
                        box.bill_id = h.bill_id;
                    }
                }

                try {
                    db.DRBillDetails.InsertAllOnSubmit(es);
                    db.SubmitChanges();
                }
                catch (Exception ex) {
                    //将已经保存的表头删除
                    db.DRBills.DeleteOnSubmit(h);
                    db.SubmitChanges();
                    throw new Exception("送货明细和包装信息保存失败，原因：" + ex.Message);
                }

                return h.bill_id;
            }
            else {
                //修改
                var bill = db.DRBills.Where(d => d.bill_id == h.bill_id).FirstOrDefault();
                if (bill == null) {
                    throw new Exception("找不到此申请，可能已被删除");
                }
                bill.p_status = "未提交";
                bill.send_date = h.send_date;
                bill.supplier_dr_number = h.supplier_dr_number;
                bill.supplier_invoice_number = h.supplier_invoice_number;
                bill.comment = h.comment;
                bill.user_id = h.user_id;

                var details = db.DRBillDetails.Where(d => d.bill_id == h.bill_id).ToList();
                foreach (var d in details) {
                    d.send_qty = es.Where(e => e.bill_detail_id == d.bill_detail_id).Select(e => e.send_qty).FirstOrDefault();
                    d.comment = es.Where(e => e.bill_detail_id == d.bill_detail_id).Select(e => e.comment).FirstOrDefault();
                }

                db.SubmitChanges();

                return bill.bill_id;
            }
        }

        /// <summary>
        /// 提交申请
        /// </summary>
        /// <param name="billId"></param>
        public void beginApply(int billId,string opName)
        {
            var bill = db.DRBills.Where(d => d.bill_id == billId).FirstOrDefault();
            if (bill == null) {
                throw new Exception("申请id不存在：" + billId);
            }

            bill.p_status = "已提交";

            db.OpRecord.InsertOnSubmit(new OpRecord()
            {
                op_date = DateTime.Now,
                op_user_name = opName,
                account = bill.account,
                bill_no = bill.bill_no,
                do_what = "提交申请单"
            });            

            db.SubmitChanges();
        }

        public DRBills GetDRBill(int billId)
        {
            return db.DRBills.Where(d => d.bill_id == billId).FirstOrDefault();
        }

        public List<DRBillDetails> GetDRBillDetails(int billId)
        {
            return db.DRBillDetails.Where(d => d.bill_id == billId).ToList();
        }

        public List<BoxAndPoModels> GetBoxAndPo(int billId)
        {            
            var result = (from b in db.OuterBoxes
                          join p in db.OuterBoxPOs on b.outer_box_id equals p.out_box_id into ptemp
                          from po in ptemp.DefaultIfEmpty()
                          where b.bill_id == billId
                          select new BoxAndPoModels()
                          {
                              box = b,
                              po = po
                          }).ToList();
            return result;
        }

        /// <summary>
        /// 查看我的送货申请列表，供应商用
        /// </summary>
        /// <param name="p"></param>
        /// <param name="canCheckAll"></param>
        /// <returns></returns>
        public IQueryable<CheckApplyListModel> SearchMyApplyList(SearchMyApplyParams p,bool canCheckAll)
        {
            return SearchApplyListBase(p)
                .Where(s => (canCheckAll 
                    || s.userId == p.userId 
                    || (s.supplierNumber + "A").Contains(p.userName))
                    )
                .OrderBy(s => s.sendDate);
        }

        /// <summary>
        /// 查看我的审核申请，订料员和采购员用
        /// </summary>
        /// <param name="p"></param>
        /// <param name="canCheckAll"></param>
        /// <returns></returns>
        public IQueryable<CheckApplyListModel> SearchMyAuditList(SearchMyApplyParams p, bool canCheckAll)
        {
            var groupUser = new UASv().GetAuditGroupUsers(p.userId);
            return SearchApplyListBase(p)
                .Where(s => (canCheckAll
                    || s.matOrderNumber==p.userName
                    || s.buyerNumber==p.userName
                    || groupUser.Contains(s.matOrderNumber)
                    || groupUser.Contains(s.buyerName))
                    )
                .OrderBy(s => s.sendDate);
        }

        /// <summary>
        /// 搜索送货申请单据的底层方法
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private IQueryable<CheckApplyListModel> SearchApplyListBase(SearchMyApplyParams p)
        {
            var result = from d in db.DRBills
                         join e in db.DRBillDetails on d.bill_id equals e.bill_id
                         where d.send_date >= p.beginDate
                         && d.send_date <= p.endDate
                         && d.bill_no.Contains(p.billNo)
                         && (p.pStatus == "所有" || d.p_status == p.pStatus)
                         && d.account == p.account
                         && (p.billType == "所有" || d.bill_type == p.billType)
                         && e.po_number.Contains(p.poNo)
                         && (e.item_model.Contains(p.itemInfo) || e.item_name.Contains(p.itemInfo))
                         select new CheckApplyListModel()
                         {
                             billId = d.bill_id,
                             billType = d.bill_type,
                             billNo = d.bill_no,
                             buyerName = e.buyer_name,
                             buyerNumber = e.buyer_number,
                             buyType = d.buy_type,
                             inStockBillDate = d.in_stock_bill_date,
                             inStockBillNo = d.in_stock_bill_number,
                             itemModel = e.item_model,
                             itemName = e.item_name,
                             itemNumber = e.item_number,
                             matOrderNumber = d.mat_order_number,
                             matOrderName = d.mat_order_name,
                             poDate = e.po_date,
                             poEntryId = e.po_entry_id,
                             poNo = e.po_number,
                             poQty = e.po_qty,
                             prNo = e.pr_number,
                             pStatus = d.p_status,
                             sendDate = d.send_date,
                             sendQty = e.send_qty,
                             supplierName = d.supplier_name,
                             tradeTypeName = d.trade_type_name,
                             unitName = e.unit_name,
                             userId = d.user_id,
                             supplierNumber = d.supplier_number
                         };
            return result;
        }

        public void DeleteDR(int billId,bool alsoDeleteBox,int userId,string userName)
        {
            var dr = db.DRBills.Where(d => d.bill_id == billId).FirstOrDefault();
            var details = db.DRBillDetails.Where(d => d.bill_id == billId).ToList();

            if (dr == null) {
                throw new Exception("此申请单不存在，可能已被删除");
            }
            if (!new string[] { "未提交", "已拒绝" }.Contains(dr.p_status)) {
                throw new Exception("当前申请单状态是：" + dr.p_status + ",不能删除");
            }

            //先备份
            var backup = new BackupData();
            backup.account = dr.account;
            backup.user_id = userId;
            backup.user_name = userName;
            backup.op_date = DateTime.Now;
            backup.bill_no = dr.bill_no;
            backup.head = JsonConvert.SerializeObject(dr);
            backup.details = JsonConvert.SerializeObject(details);
            db.BackupData.InsertOnSubmit(backup);

            //删除单据
            db.DRBills.DeleteOnSubmit(dr);
            db.DRBillDetails.DeleteAllOnSubmit(details);

            //删除关联箱子
            foreach (var box in db.OuterBoxes.Where(o => o.bill_id == billId)) {
                if (alsoDeleteBox) {
                    db.OuterBoxPOs.DeleteAllOnSubmit(db.OuterBoxPOs.Where(p => p.out_box_id == box.outer_box_id));
                    db.InneBoxes.DeleteAllOnSubmit(db.InneBoxes.Where(i => i.outer_box_id == box.outer_box_id));
                    db.OuterBoxes.DeleteOnSubmit(box);
                }
                else {
                    box.bill_id = null;
                }
            }
            db.SubmitChanges();
        }

        public void UpdatePStatus(int billId, string userName, string beforeStatus, string afterStatus,string doWhat=null, string comment=null)
        {
            var dr = db.DRBills.Where(d => d.bill_id == billId).FirstOrDefault();
            if (dr == null) {
                throw new Exception("单据不存在");
            }
            if (!beforeStatus.Equals(dr.p_status)) {
                throw new Exception("当前申请状态是：[" + dr.p_status + "],不是[" + beforeStatus + "],操作失败");
            }

            doWhat = doWhat ?? beforeStatus + " ---> " + afterStatus;
            if (!string.IsNullOrWhiteSpace(comment)) {
                doWhat += ":" + comment;
            }

            dr.p_status = afterStatus;

            //加入到审批记录
            OpRecord rec = new OpRecord();
            rec.account = dr.account;
            rec.bill_no = dr.bill_no;
            rec.op_date = DateTime.Now;
            rec.op_user_name = userName;
            rec.do_what = doWhat;
            db.OpRecord.InsertOnSubmit(rec);

            db.SubmitChanges();
        }

        public List<OpRecord> GetOpRecord(string billNo)
        {
            return db.OpRecord.Where(o => o.bill_no == billNo).ToList();
        }

    }
}