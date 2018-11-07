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
                        expireDate = ((DateTime)bp.box.produce_date).AddMonths((int)bp.box.safe_period).ToString("yyyy-MM-dd"),
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
                        qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####};{5:0.####};{6};{7};{8};{9:yyyy-MM-dd};",
                        boxNumber, bp.po.po_number, bp.po.po_entry_id, bp.box.item_number, bp.po.send_num,
                        bp.box.backup_number, bp.box.unit_name, supplierNumber, bp.box.batch, bp.box.create_date
                        )
                    });
                }

            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }

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
                foreach (var boxNumber in b.i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    boxes.Add(new PrintInnerBoxModel()
                    {
                        batchNo=b.o.batch,
                        boxNumber=boxNumber,
                        brand=b.o.brand,
                        expireDate = ((DateTime)b.o.create_date).AddMonths((int)b.o.safe_period).ToString("yyyy-MM-dd"),
                        itemModel=b.o.item_model,
                        itemName=b.o.item_name,
                        itemNumber=b.o.item_number,
                        keepCondition=b.o.keep_condition,
                        madeBy=b.o.made_by,
                        produceDate = b.o.produce_date == null ? "" : ((DateTime)b.o.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit=string.Format("{0:0.####}{1}",b.i.every_qty,b.o.unit_name),
                        rohs=b.o.rohs,
                        supplierName=supplierName,
                        tradeTypeName=b.o.trade_type_name,
                        qrcodeContent = string.Format("{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};",
                        boxNumber, b.o.item_number, b.i.every_qty,b.o.unit_name, supplierNumber, b.o.batch, b.o.produce_date
                        )
                    });
                }
            }

            return boxes.OrderBy(b=>b.boxNumber).ToList();
        }

    }
}