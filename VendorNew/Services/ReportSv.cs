using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorNew.Models;

namespace VendorNew.Services
{
    public class ReportSv:BaseSv
    {
        public PrintApplyModels GetData4Print(int billId,string account)
        {
            DRSv sv = new DRSv();
            PrintApplyModels m = new PrintApplyModels();
            m.h = sv.GetDRBill(billId);
            m.es = sv.GetDRBillDetails(billId);
            m.boxAndPos = sv.GetBoxAndPo(billId);

            m.supplierInfo = new ItemSv().GetSupplierInfo(m.h.supplier_number,account);

            return m;
        }

        /// <summary>
        /// 打印某张申请单的外箱
        /// </summary>
        /// <param name="billId"></param>
        /// <returns></returns>
        public List<PrintOuterBoxModels> GetOuterBoxes4Print(int billId)
        {
            List<PrintOuterBoxModels> boxes = new List<PrintOuterBoxModels>();
            var dr = new DRSv().GetDRBill(billId);
            if (dr == null) {
                throw new Exception("单据不存在");
            }
            string supplierName = dr.supplier_name;
            string supplierNumber = dr.supplier_number;
            foreach (var bp in new DRSv().GetBoxAndPo(billId)) {
                foreach (var boxNumber in bp.box.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    boxes.Add(new PrintOuterBoxModels()
                    {
                        batchNo = bp.box.batch,
                        boxNumber = boxNumber,
                        brand = bp.box.brand,
                        companyName = bp.box.account == "S" ? "信利半导体有限公司" : "信利光电股份有限公司",
                        grossWeight = string.Format("{0:0.####}", bp.box.every_gross_weight),
                        itemModel = bp.box.item_model,
                        itemName = bp.box.item_name,
                        itemNumber = bp.box.item_number,
                        keepCondition = bp.box.keep_condition,
                        expireDate = ((DateTime)bp.box.produce_date).AddMonths((int)bp.box.safe_period).AddDays(-1).ToString("yyyy-MM-dd"),
                        madeBy = bp.box.made_by,
                        madeIn = bp.box.made_in,
                        netWeight = string.Format("{0:0.####}", bp.box.every_net_weight),
                        poNumber = bp.po.po_number,
                        produceCircle = bp.box.produce_circle,
                        produceDate = bp.box.produce_date == null ? "" : ((DateTime)bp.box.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit =  string.Format("{0:0.####}", bp.po.send_num) + bp.box.unit_name + (bp.box.backup_number == null || bp.box.backup_number == 0 ? "" : (";备件:" + string.Format("{0:0.####}", bp.box.backup_number))),
                        rohs = bp.box.rohs,
                        supplierName = supplierName,
                        tradeTypeName = bp.box.trade_type_name,
                        qrcodeContent = string.Format("{11};{0};{1};{2};{3};{4:0.####};{5:0.####};{6};{7};{8};{9:yyyy-MM-dd};{10};",
                        boxNumber, bp.po.po_number, bp.po.po_entry_id, bp.box.item_number, bp.po.send_num,
                        bp.box.backup_number, bp.box.unit_number, supplierNumber, "", "", bp.box.account == "S" ? "zb" : "gd", bp.box.outer_box_id
                        )
                        //qrcodeContent = bp.box.outer_box_id + ";" + boxNumber
                    });
                }

            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }


        /// <summary>
        /// 打印所有选中的外箱
        /// </summary>
        /// <param name="boxIds"></param>
        /// <returns></returns>
        public List<PrintOuterBoxModels> GetOuterBoxes4Print(List<int> boxIds)
        {
            List<PrintOuterBoxModels> boxes = new List<PrintOuterBoxModels>();

            var selectedboxes = (from o in db.OuterBoxes
                                 join po in db.OuterBoxPOs on o.outer_box_id equals po.out_box_id
                                 where boxIds.Contains(o.outer_box_id)
                                 select new BoxAndPoModels()
                                 {
                                     box = o,
                                     po = po
                                 }).ToList();

            if (selectedboxes.Count() == 0) {
                throw new Exception("箱子不存在");
            }

            string supplierNumber = selectedboxes.First().box.user_name;
            string supplierName = new ItemSv().GetSupplierNameByNumber(supplierNumber);
            foreach (var bp in selectedboxes) {
                foreach (var boxNumber in bp.box.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    boxes.Add(new PrintOuterBoxModels()
                    {
                        batchNo = bp.box.batch,
                        boxNumber = boxNumber,
                        brand = bp.box.brand,
                        companyName = bp.box.account == "S" ? "信利半导体有限公司" : "信利光电股份有限公司",
                        grossWeight = string.Format("{0:0.####}", bp.box.every_gross_weight),
                        itemModel = bp.box.item_model,
                        itemName = bp.box.item_name,
                        itemNumber = bp.box.item_number,
                        keepCondition = bp.box.keep_condition,
                        expireDate = ((DateTime)bp.box.produce_date).AddMonths((int)bp.box.safe_period).AddDays(-1).ToString("yyyy-MM-dd"),
                        madeBy = bp.box.made_by,
                        madeIn = bp.box.made_in,
                        netWeight = string.Format("{0:0.####}", bp.box.every_net_weight),
                        poNumber = bp.po.po_number,
                        produceCircle = bp.box.produce_circle,
                        produceDate = bp.box.produce_date == null ? "" : ((DateTime)bp.box.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit = string.Format("{0:0.####}", bp.po.send_num) + bp.box.unit_name + (bp.box.backup_number == null || bp.box.backup_number == 0 ? "" : (";备件:" + string.Format("{0:0.####}", bp.box.backup_number))),
                        rohs = bp.box.rohs,
                        supplierName = supplierName,
                        tradeTypeName = bp.box.trade_type_name,
                        qrcodeContent = string.Format("{11};{0};{1};{2};{3};{4:0.####};{5:0.####};{6};{7};{8};{9:yyyy-MM-dd};{10};",
                        boxNumber, bp.po.po_number, bp.po.po_entry_id, bp.box.item_number, bp.po.send_num,
                        bp.box.backup_number, bp.box.unit_number, supplierNumber, "", "", bp.box.account == "S" ? "zb" : "gd",bp.box.outer_box_id
                        )
                        //qrcodeContent = bp.box.outer_box_id + ";" + boxNumber
                    });
                }

            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }

        /// <summary>
        /// 打印某张申请单的内箱
        /// </summary>
        /// <param name="billId"></param>
        /// <returns></returns>
        public List<PrintInnerBoxModel> GetInnerBoxes4Print(int billId)
        {
            List<PrintInnerBoxModel> boxes = new List<PrintInnerBoxModel>();
            var dr = new DRSv().GetDRBill(billId);
            if (dr == null) {
                throw new Exception("单据不存在");
            }
            string supplierName = dr.supplier_name;
            string supplierNumber = dr.supplier_number;

            var m = (from o in db.OuterBoxes
                     join i in db.InneBoxes on o.outer_box_id equals i.outer_box_id
                     where o.bill_id == billId
                     select new
                     {
                         o,
                         i
                     }).ToList();

            foreach (var b in m) {
                var outerBoxes = b.o.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var innerBoxes = b.i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i=0;i<innerBoxes.Count();i++) {
                    var outerBoxNum = outerBoxes[i / ((int)b.i.pack_num / (int)b.o.pack_num)];
                    boxes.Add(new PrintInnerBoxModel()
                    {
                        batchNo = b.o.batch,
                        boxNumber = innerBoxes[i],
                        brand = b.o.brand,
                        expireDate = ((DateTime)b.o.produce_date).AddMonths((int)b.o.safe_period).AddDays(-1).ToString("yyyy-MM-dd"),
                        itemModel = b.o.item_model,
                        itemName = b.o.item_name,
                        itemNumber = b.o.item_number,
                        keepCondition = b.o.keep_condition,
                        madeBy = b.o.made_by,
                        produceDate = b.o.produce_date == null ? "" : ((DateTime)b.o.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit = string.Format("{0:0.####}{1}", b.i.every_qty, b.o.unit_name),
                        rohs = b.o.rohs,
                        supplierName = supplierName,
                        tradeTypeName = b.o.trade_type_name,
                        outerBoxNumber = outerBoxNum,
                        //qrcodeContent = string.Format("{9};{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};{7};{8}",
                        //innerBoxes[i], b.o.item_number, b.i.every_qty, b.o.unit_number, supplierNumber, b.o.batch, b.o.produce_date, outerBoxNum, b.o.account == "S" ? "zb" : "gd",b.i.inner_box_id
                        //)
                        qrcodeContent = b.i.inner_box_id + ";" + innerBoxes[i] + ";" + outerBoxNum
                    });
                }
            }

            return boxes.OrderBy(b=>b.boxNumber).ToList();
        }

        /// <summary>
        /// 打印所有选中的外箱包含的内箱
        /// </summary>
        /// <param name="boxIds"></param>
        /// <returns></returns>
        public List<PrintInnerBoxModel> GetInnerBoxes4Print(List<int> boxIds)
        {
            List<PrintInnerBoxModel> boxes = new List<PrintInnerBoxModel>();

            var m = (from o in db.OuterBoxes
                     join i in db.InneBoxes on o.outer_box_id equals i.outer_box_id
                     where boxIds.Contains(o.outer_box_id)
                     select new
                     {
                         o,
                         i
                     }).ToList();

            if (m.Count() == 0) {
                throw new Exception("不存在可打印的内箱");
            }

            string supplierNumber = m.First().o.user_name;
            string supplierName = new ItemSv().GetSupplierNameByNumber(supplierNumber);            

            foreach (var b in m) {
                var outerBoxes = b.o.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var innerBoxes = b.i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < innerBoxes.Count(); i++) {
                    var outerBoxNum = outerBoxes[i / ((int)b.i.pack_num / (int)b.o.pack_num)];
                    boxes.Add(new PrintInnerBoxModel()
                    {
                        batchNo = b.o.batch,
                        boxNumber = innerBoxes[i],
                        brand = b.o.brand,
                        expireDate = ((DateTime)b.o.produce_date).AddMonths((int)b.o.safe_period).AddDays(-1).ToString("yyyy-MM-dd"),
                        itemModel = b.o.item_model,
                        itemName = b.o.item_name,
                        itemNumber = b.o.item_number,
                        keepCondition = b.o.keep_condition,
                        madeBy = b.o.made_by,
                        produceDate = b.o.produce_date == null ? "" : ((DateTime)b.o.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit = string.Format("{0:0.####}{1}", b.i.every_qty, b.o.unit_name),
                        rohs = b.o.rohs,
                        supplierName = supplierName,
                        tradeTypeName = b.o.trade_type_name,
                        outerBoxNumber = outerBoxNum,
                        //qrcodeContent = string.Format("{9};{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};{7};{8}",
                        //innerBoxes[i], b.o.item_number, b.i.every_qty, b.o.unit_number, supplierNumber, b.o.batch, b.o.produce_date, outerBoxNum, b.o.account == "S" ? "zb" : "gd",b.i.inner_box_id
                        //)
                        qrcodeContent = b.i.inner_box_id + ";" + innerBoxes[i] + ";" + outerBoxNum
                    });
                }
            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }

    }
}