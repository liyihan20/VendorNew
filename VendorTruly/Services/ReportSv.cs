using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorTruly.Models;

namespace VendorTruly.Services
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

            m.boxAndPos = UnionBoxAndPo(m.boxAndPos); //2018-12-24 同一PO，同一分录，同一数量，没有合并箱的情况下，将行合并起来，件数汇总

            m.supplierInfo = new ItemSv().GetSupplierInfo(m.h.supplier_number,account);

            return m;
        }

        /// <summary>
        /// 2018-12-24 同一PO，同一分录，同一数量，没有合并箱的情况下，将行合并起来，件数汇总
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<BoxAndPoModels> UnionBoxAndPo(List<BoxAndPoModels> list)
        {
            List<BoxAndPoModels> result = new List<BoxAndPoModels>();
            //var sameBoxId = list.Where(li => li.po.entry_id > 1).Select(li => li.box.outer_box_id).Distinct().ToArray(); //合并的箱子id，不参与合并行

            var sameBoxId = (from li in list
                         group li by li.box into lbox
                         where lbox.Count() > 1
                         select lbox.Key.outer_box_id).Distinct().ToArray();

            foreach (var l in list) {
                if (sameBoxId.Contains(l.box.outer_box_id)) {
                    //合并箱子的，不再合并行
                    result.Add(l);
                    continue;
                }
                if (result.Where(r => r.box.box_number_long.Contains(l.box.box_number_long)).Count() > 0) {
                    //已经合并过了的，跳过
                    continue;
                }
                var samePoAndQtyList = list.Where(li => li.po.po_id == l.po.po_id 
                    && li.po.po_entry_id == l.po.po_entry_id 
                    && li.po.send_num == l.po.send_num 
                    && !sameBoxId.Contains(li.box.outer_box_id)
                    ).ToList();
                if (samePoAndQtyList.Count() == 1) {
                    //不存在相同po可合并的行，直接输出
                    result.Add(l);
                    continue;
                }
                var box = l.box;
                box.pack_num = samePoAndQtyList.Sum(s => s.box.pack_num);
                if (samePoAndQtyList.Select(s => s.box.box_number).Count() <= 2) {
                    box.box_number = string.Join(",", samePoAndQtyList.Select(s => s.box.box_number).ToArray());
                }
                else {
                    box.box_number = string.Join(",", samePoAndQtyList.Select(s => s.box.box_number).ToArray().Take(2)) + "...";
                }
                box.box_number_long = string.Join(",", samePoAndQtyList.Select(s => s.box.box_number_long).ToArray());
                result.Add(new BoxAndPoModels()
                {
                    box = box,
                    po = samePoAndQtyList.First().po
                });
            }

            return result;
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
                        expireDate = bp.box.expire_date == null ? ((DateTime)bp.box.produce_date).AddMonths((int)bp.box.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)bp.box.expire_date).ToString("yyyy-MM-dd"),
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
                        expireDate = bp.box.expire_date == null ? ((DateTime)bp.box.produce_date).AddMonths((int)bp.box.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)bp.box.expire_date).ToString("yyyy-MM-dd"),
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
                     join et in db.InnerBoxesExtra on i.inner_box_id equals et.inner_box_id into etp
                     from e in etp.DefaultIfEmpty()
                     where o.bill_id == billId
                     select new
                     {
                         o,
                         i,
                         e
                     }).ToList();

            foreach (var b in m) {
                var outerBoxes = b.o.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var innerBoxes = b.i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i=0;i<innerBoxes.Count();i++) {
                    var outerBoxNum = outerBoxes[i / ((int)b.i.pack_num / (int)b.o.pack_num)];
                    if (b.e == null) {
                        boxes.Add(new PrintInnerBoxModel()
                        {
                            batchNo = b.o.batch,
                            boxNumber = innerBoxes[i],
                            brand = b.o.brand,
                            expireDate = b.o.expire_date == null ? ((DateTime)b.o.produce_date).AddMonths((int)b.o.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)b.o.expire_date).ToString("yyyy-MM-dd"),
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
                            qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####}",b.i.inner_box_id,innerBoxes[i],outerBoxNum,b.o.item_number,b.i.every_qty)
                        });
                    }
                    else {
                        boxes.Add(new PrintInnerBoxModel()
                        {
                            batchNo = b.e.batch,
                            boxNumber = innerBoxes[i],
                            brand = b.e.brand,
                            expireDate = b.e.expire_date == null ? ((DateTime)b.e.produce_date).AddMonths((int)b.e.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)b.e.expire_date).ToString("yyyy-MM-dd"),
                            itemModel = b.e.item_model,
                            itemName = b.e.item_name,
                            itemNumber = b.e.item_number,
                            keepCondition = b.e.keep_condition,
                            madeBy = b.e.made_by,
                            produceDate = b.e.produce_date == null ? "" : ((DateTime)b.e.produce_date).ToString("yyyy-MM-dd"),
                            qtyAndUnit = string.Format("{0:0.####}{1}", b.i.every_qty, b.e.unit_name),
                            rohs = b.e.rohs,
                            supplierName = supplierName,
                            tradeTypeName = b.e.trade_type_name,
                            outerBoxNumber = outerBoxNum,
                            //qrcodeContent = string.Format("{9};{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};{7};{8}",
                            //innerBoxes[i], b.o.item_number, b.i.every_qty, b.o.unit_number, supplierNumber, b.o.batch, b.o.produce_date, outerBoxNum, b.o.account == "S" ? "zb" : "gd",b.i.inner_box_id
                            //)
                            qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####}",b.i.inner_box_id,innerBoxes[i],outerBoxNum,b.e.item_number,b.i.every_qty)
                        });
                    }
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
                     join et in db.InnerBoxesExtra on i.inner_box_id equals et.inner_box_id into etp
                     from e in etp.DefaultIfEmpty()
                     where boxIds.Contains(o.outer_box_id)
                     select new
                     {
                         o,
                         i,
                         e
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
                    if (b.e == null) {
                        boxes.Add(new PrintInnerBoxModel()
                        {
                            batchNo = b.o.batch,
                            boxNumber = innerBoxes[i],
                            brand = b.o.brand,
                            expireDate = b.o.expire_date == null ? ((DateTime)b.o.produce_date).AddMonths((int)b.o.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)b.o.expire_date).ToString("yyyy-MM-dd"),
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
                            qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####}",b.i.inner_box_id,innerBoxes[i],outerBoxNum,b.o.item_number,b.i.every_qty)
                        });
                    }
                    else {
                        boxes.Add(new PrintInnerBoxModel()
                        {
                            batchNo = b.e.batch,
                            boxNumber = innerBoxes[i],
                            brand = b.e.brand,
                            expireDate = b.e.expire_date == null ? ((DateTime)b.e.produce_date).AddMonths((int)b.e.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)b.e.expire_date).ToString("yyyy-MM-dd"),
                            itemModel = b.e.item_model,
                            itemName = b.e.item_name,
                            itemNumber = b.e.item_number,
                            keepCondition = b.e.keep_condition,
                            madeBy = b.e.made_by,
                            produceDate = b.e.produce_date == null ? "" : ((DateTime)b.e.produce_date).ToString("yyyy-MM-dd"),
                            qtyAndUnit = string.Format("{0:0.####}{1}", b.i.every_qty, b.e.unit_name),
                            rohs = b.e.rohs,
                            supplierName = supplierName,
                            tradeTypeName = b.e.trade_type_name,
                            outerBoxNumber = outerBoxNum,
                            //qrcodeContent = string.Format("{9};{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};{7};{8}",
                            //innerBoxes[i], b.o.item_number, b.i.every_qty, b.o.unit_number, supplierNumber, b.o.batch, b.o.produce_date, outerBoxNum, b.o.account == "S" ? "zb" : "gd",b.i.inner_box_id
                            //)
                            qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####}",b.i.inner_box_id,innerBoxes[i],outerBoxNum,b.e.item_number,b.i.every_qty)
                        });
                    }
                }
            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }

        /// <summary>
        /// 打印小标签管理里面所有选中的内箱
        /// </summary>
        /// <param name="boxIds"></param>
        /// <returns></returns>
        public List<PrintInnerBoxModel> GetInnerBoxesWithExtra4Print(List<int> boxIds)
        {
            List<PrintInnerBoxModel> boxes = new List<PrintInnerBoxModel>();

            var m = (from i in db.InneBoxes
                     join et in db.InnerBoxesExtra on i.inner_box_id equals et.inner_box_id into etp
                     from e in etp.DefaultIfEmpty()
                     where boxIds.Contains(i.inner_box_id)
                     select new
                     {
                         i,
                         e
                     }).ToList();

            if (m.Count() == 0) {
                throw new Exception("不存在可打印的内箱");
            }

            string supplierNumber = m.First().e.user_name;
            string supplierName = new ItemSv().GetSupplierNameByNumber(supplierNumber);
            if (string.IsNullOrEmpty(supplierName)) {
                supplierName = "测试供应商名";
            }

            foreach (var b in m) {                
                var innerBoxes = b.i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string outerBoxNumber = "";
                if (b.i.outer_box_id != null) {
                    var outerBox = db.OuterBoxes.Where(o => o.outer_box_id == b.i.outer_box_id).FirstOrDefault();
                    if (outerBox != null) {
                        outerBoxNumber = outerBox.box_number;
                    }
                }
                for (var i = 0; i < innerBoxes.Count(); i++) {                    
                    boxes.Add(new PrintInnerBoxModel()
                    {
                        batchNo = b.e.batch,
                        boxNumber = innerBoxes[i],
                        brand = b.e.brand,
                        expireDate = b.e.expire_date == null ? ((DateTime)b.e.produce_date).AddMonths((int)b.e.safe_period).AddDays(-1).ToString("yyyy-MM-dd") : ((DateTime)b.e.expire_date).ToString("yyyy-MM-dd"),
                        itemModel = b.e.item_model,
                        itemName = b.e.item_name,
                        itemNumber = b.e.item_number,
                        keepCondition = b.e.keep_condition,
                        madeBy = b.e.made_by,
                        produceDate = b.e.produce_date == null ? "" : ((DateTime)b.e.produce_date).ToString("yyyy-MM-dd"),
                        qtyAndUnit = string.Format("{0:0.####}{1}", b.i.every_qty, b.e.unit_name),
                        rohs = b.e.rohs,
                        supplierName = supplierName,
                        tradeTypeName = b.e.trade_type_name,
                        outerBoxNumber = outerBoxNumber,
                        //qrcodeContent = string.Format("{9};{0};{1};{2:0.####};{3};{4};{5};{6:yyyy-MM-dd};{7};{8}",
                        //innerBoxes[i], b.o.item_number, b.i.every_qty, b.o.unit_number, supplierNumber, b.o.batch, b.o.produce_date, outerBoxNum, b.o.account == "S" ? "zb" : "gd",b.i.inner_box_id
                        //)
                        qrcodeContent = string.Format("{0};{1};{2};{3};{4:0.####}",b.i.inner_box_id,innerBoxes[i],outerBoxNumber,b.e.item_number,b.i.every_qty)
                    });
                    
                }
            }

            return boxes.OrderBy(b => b.boxNumber).ToList();
        }


        public DayNumChartModel GetDayNumChartData(string currentAccount)
        {
            DateTime lastMonth = DateTime.Parse(DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd"));
            DateTime now = DateTime.Now;
            DayNumChartModel m = new DayNumChartModel();

            //送货申请
            var applys=(from d in db.DRBills
                   join e in db.DRBillDetails on d.bill_id equals e.bill_id
                   where d.bill_date >= lastMonth && d.bill_date <= now && d.account==currentAccount
                   select d.bill_date).ToList();

            //外箱
            var oBoxes = db.OuterBoxes.Where(o => o.create_date >= lastMonth && o.create_date <= now && o.account==currentAccount)
                .Select(o => new { createDay = o.create_date, packNum = o.pack_num }).ToList();

            //内箱
            var iBoxes = (from i in db.InneBoxes
                          join o in db.OuterBoxes on i.outer_box_id equals o.outer_box_id
                          where o.create_date >= lastMonth && o.create_date <= now && o.account == currentAccount
                          select new
                          {
                              createDay = o.create_date,
                              packNum = i.pack_num
                          }).ToList();

            //先做内箱，并且未关联外箱的那部分
            var iBoxesExtra = (from ie in db.InnerBoxesExtra
                               join i in db.InneBoxes on ie.inner_box_id equals i.inner_box_id
                               where i.outer_box_id == null
                               && ie.create_date >= lastMonth
                               && ie.create_date <= now
                               && ie.account==currentAccount
                               select new
                               {
                                   createDay = ie.create_date,
                                   packNum = i.pack_num
                               }).ToList();

            //对应到日期
            DateTime aDayLater;
            while (lastMonth < now) {
                aDayLater = lastMonth.AddDays(1);
                m.dayList.Add(lastMonth.ToString("MM-dd"));

                m.applyNumList.Add(applys.Where(a => a >= lastMonth && a < aDayLater).Count());
                m.oBoxNumList.Add(oBoxes.Where(o => o.createDay >= lastMonth && o.createDay < aDayLater).Select(o => o.packNum).Sum() ?? 0);
                m.iBoxNumList.Add(iBoxes.Where(i => i.createDay >= lastMonth && i.createDay < aDayLater).Select(i => i.packNum).Sum() ?? 0
                    + iBoxesExtra.Where(ie => ie.createDay >= lastMonth && ie.createDay < aDayLater).Select(ie => ie.packNum).Sum() ?? 0
                    );
                lastMonth = aDayLater;
            }

            return m;
        }

        /// <summary>
        /// 今天申请送货数量最多的前10个供应商
        /// </summary>
        /// <returns></returns>
        public List<SupplierAndApplyNumModel> GetTopTenSupplierApplyNumToday(string currentAccount)
        {
            DateTime today = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            var result = (from d in db.DRBills
                          join e in db.DRBillDetails on d.bill_id equals e.bill_id
                          where d.bill_date >= today && d.account==currentAccount
                          group d by d.supplier_name into dg
                          orderby dg.Count() descending
                          select new SupplierAndApplyNumModel()
                          {
                              supplierName = dg.Key,
                              num = dg.Count()
                          }).Take(10).ToList();

            return result;
        }

        /// <summary>
        /// 今天制作外箱数量最多的前10个供应商
        /// </summary>
        /// <returns></returns>
        public List<SupplierAndApplyNumModel> GetTopTenSupplierOBoxNumToday(string currentAccount)
        {
            DateTime today = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            var result = (from o in db.OuterBoxes
                          join u in db.Users on o.user_name equals u.user_name
                          where o.create_date >= today && o.account== currentAccount
                          group o by u.real_name into dg
                          orderby dg.Sum(d => d.pack_num) descending
                          select new SupplierAndApplyNumModel()
                          {
                              supplierName = dg.Key,
                              num = dg.Sum(d => d.pack_num)
                          }).Take(10).ToList();

            return result;
        }

        /// <summary>
        /// 今天制作内箱数量最多的前10个供应商，外箱流程
        /// </summary>
        /// <returns></returns>
        public List<SupplierAndApplyNumModel> GetTopTenSupplierIBoxNumToday(string currentAccount)
        {
            DateTime today = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            var result = (from i in db.InneBoxes
                          join o in db.OuterBoxes on i.outer_box_id equals o.outer_box_id
                          join u in db.Users on o.user_name equals u.user_name
                          join ie in db.InnerBoxesExtra on i.inner_box_id equals ie.inner_box_id into iet
                          from it in iet.DefaultIfEmpty()
                          where o.create_date >= today
                          && it == null
                          && o.account==currentAccount
                          group i by u.real_name into ig
                          orderby ig.Sum(s => s.pack_num) descending
                          select new SupplierAndApplyNumModel()
                          {
                              supplierName = ig.Key,
                              num = ig.Sum(s => s.pack_num)
                          }).Take(10).ToList();
            return result;
        }

        /// <summary>
        /// 今天制作内箱数量最多的前10个供应商，小标签流程
        /// </summary>
        /// <returns></returns>
        public List<SupplierAndApplyNumModel> GetTopTenSupplierIBoxExtraNumToday(string currentAccount)
        {
            DateTime today = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            var result = (from i in db.InneBoxes
                          join ie in db.InnerBoxesExtra on i.inner_box_id equals ie.inner_box_id
                          join u in db.Users on ie.user_name equals u.user_name
                          where ie.create_date >= today && ie.account==currentAccount
                          group i by u.real_name into ig
                          orderby ig.Sum(s => s.pack_num) descending
                          select new SupplierAndApplyNumModel()
                          {
                              supplierName = ig.Key,
                              num = ig.Sum(s => s.pack_num)
                          }).Take(10).ToList();
            return result;
        }


    }
}