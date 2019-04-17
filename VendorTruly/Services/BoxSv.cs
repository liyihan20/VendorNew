using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using VendorTruly.Models;

namespace VendorTruly.Services
{
    public class BoxSv:BaseSv
    {
        /// <summary>
        /// 保存外箱
        /// </summary>
        /// <param name="box">外箱信息</param>
        /// <param name="poList">外箱关联的PO信息</param>
        /// <returns>箱号</returns>
        public string SaveOuterBox(OuterBoxes box, List<OuterBoxPOs> poList, List<int> innerBoxIds)
        {
            //验证可做外箱数量
            decimal everyBoxQty = 0;
            foreach (var po in poList) {
                //所有未关联的数量加上此张申请已关联的数量+本次新增的数量不能大于可申请数量
                var notRelatedQty = (from op in db.OuterBoxPOs
                                     join o in db.OuterBoxes on op.out_box_id equals o.outer_box_id
                                     where op.po_id == po.po_id && op.po_entry_id == po.po_entry_id
                                     && (o.bill_id == null || o.bill_id == box.bill_id)
                                     select op.send_num * o.pack_num).Sum();
                var sendNum = po.send_num * box.pack_num; //本次制作数量
                if (notRelatedQty + sendNum > po.can_send_qty) {
                    throw new Exception(string.Format("订单号[{0}]分录号[{1}]的可制作标签数量不足：已制作标签数量[{2}],本次制作数量[{3}],可申请数量[{4}]",
                        po.po_number, po.po_entry_id, notRelatedQty, sendNum, po.can_send_qty));
                }
                everyBoxQty += (decimal)po.send_num;
            }

            //数量没问题之后，就保存
            var boxNumber = new ItemSv().GetBoxNumber("O", (int)box.pack_num);
            box.box_number = boxNumber[0];
            box.box_number_long = boxNumber[1];
            box.create_date = DateTime.Now;
            box.every_qty = everyBoxQty;
            box.bill_id = box.bill_id == 0 ? null : box.bill_id;

            db.OuterBoxes.InsertOnSubmit(box);
            db.OuterBoxPOs.InsertAllOnSubmit(poList);
            db.SubmitChanges();

            //因为取消了外键，保存后还需要再保存两者的关系
            foreach (var po in poList) {
                po.out_box_id = box.outer_box_id;
            }
            //保存外箱明细 2018-12-04
            foreach (var num in box.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )) {
                db.OuterBoxesDetail.InsertOnSubmit(new OuterBoxesDetail()
                {
                    outer_box_id = box.outer_box_id,
                    box_number = num
                });
            }

            //关联小标签信息 2018-12-12
            if (innerBoxIds.Count() > 0) {
                var innerBoxes = db.InneBoxes.Where(i => innerBoxIds.Contains(i.inner_box_id)).ToList();
                foreach (var ib in innerBoxes) {
                    ib.outer_box_id = box.outer_box_id; //关联到外箱
                    //添加内箱明细
                    var innerBoxArr = ib.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (var j = 0; j < innerBoxArr.Count(); j++) {
                        db.InnerBoxesDetail.InsertOnSubmit(new InnerBoxesDetail()
                        {
                            inner_box_id = ib.inner_box_id,
                            outer_box_id = box.outer_box_id,
                            inner_box_number = innerBoxArr[j],
                            outer_box_number = box.box_number
                        });
                    }
                }
            }

            db.SubmitChanges();

            return box.box_number;
        }

        /// <summary>
        /// 保存内箱
        /// </summary>
        /// <param name="outerBoxId">外箱id</param>
        /// <param name="everyQty">内箱每件数量</param>
        /// <param name="packNum">内箱件数</param>
        /// <returns>内箱箱号</returns>
        public string[] SaveInnerBox(InneBoxes ib)
        {
            var outerBox = db.OuterBoxes.Where(o => o.outer_box_id == ib.outer_box_id).FirstOrDefault();
            if (outerBox == null) {
                throw new Exception("外箱不存在，操作失败");
            }

            if (outerBox.bill_id != null) {
                var dr = new DRSv().GetDRBill((int)outerBox.bill_id);
                if (dr != null && dr.p_status != "未提交" && dr.p_status != "已拒绝") {
                    throw new Exception("已使用并提交的箱子不能再添加内箱");
                }
            }

            //此外箱已有的内箱数量
            var outerNum = outerBox.every_qty * outerBox.pack_num;
            var existedInnerNum = db.InneBoxes.Where(i => i.outer_box_id == outerBox.outer_box_id).Sum(i => i.every_qty * i.pack_num) ?? 0m;

            if (ib.every_qty > outerBox.every_qty) {
                throw new Exception("内箱每件数量不能大于外箱每件数量");
            }

            if (ib.pack_num % outerBox.pack_num != 0) {
                throw new Exception("内箱件数必须是外箱件数的整数倍，否则不能被平均分摊到每个外箱中");
            }

            if (ib.every_qty * ib.pack_num > outerNum - existedInnerNum) {
                throw new Exception(string.Format("内箱每件数量*内箱件数不能大于可做数量:{0:0.####}", outerNum - existedInnerNum));
            }

            var innerBoxNumber = new ItemSv().GetBoxNumber("I", (int)ib.pack_num);
            ib.box_number = innerBoxNumber[0];
            ib.box_number_long = innerBoxNumber[1];

            db.InneBoxes.InsertOnSubmit(ib);
            db.SubmitChanges();

            //保存内箱明细
            var innerBoxArr = ib.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var outerBoxArr = outerBox.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (var j = 0; j < innerBoxArr.Count(); j++) {
                db.InnerBoxesDetail.InsertOnSubmit(new InnerBoxesDetail()
                {
                    inner_box_id = ib.inner_box_id,
                    outer_box_id = outerBox.outer_box_id,
                    inner_box_number = innerBoxArr[j],
                    outer_box_number = outerBoxArr[j / (innerBoxArr.Count() / outerBoxArr.Count())]
                });
            }
            db.SubmitChanges();

            return new string[] { ib.box_number, ib.inner_box_id.ToString() };
        }

        /// <summary>
        /// 在申请界面获取关联PO单的未使用外箱，如果是合并箱且部分关联当前po信息，也不加载
        /// </summary>
        /// <param name="account">账套</param>
        /// <param name="userName">供应商编码</param>
        /// <param name="poIds">关联po</param>
        /// <param name="exceptIds">不加载的外箱id，里面只有interid有效</param>
        /// <returns></returns>
        public List<BoxModels> GetOuterBoxesByPoInfo(string account, string userName, List<IDModel> poIds, List<IDModel> exceptIds, int billId)
        {
            List<BoxModels> list = new List<BoxModels>();

            if (billId > 0) {
                //已申请的，和申请单已经有了关联，只加载关联部分
                var boxes = (from o in db.OuterBoxes
                             join p in db.OuterBoxPOs on o.outer_box_id equals p.out_box_id
                             join i in db.InneBoxes on o.outer_box_id equals i.outer_box_id into tpi
                             from ti in tpi.DefaultIfEmpty()
                             where o.account == account
                             && o.bill_id == billId
                             select new
                             {
                                 box = o,
                                 pos = p,
                                 ibox = ti
                             }).ToList();

                string poInfoString;
                foreach (var box in boxes.Select(b => b.box).Distinct()) {
                    poInfoString = "";
                    foreach (var p in boxes.Where(b => b.box == box).Select(b => b.pos).Distinct()) {
                        poInfoString += string.Format("{0}[{1}]：{2:0.####}；", p.po_number, p.po_entry_id, p.send_num);
                    }
                    list.Add(new BoxModels()
                    {
                        oBox = box,
                        poInfos = poInfoString,
                        poList = boxes.Where(b => b.box == box).Select(b => b.pos).Distinct().ToList(),
                        children = boxes.Where(b => b.box == box && b.ibox != null).Select(b => b.ibox).Distinct().ToList()
                    });
                }
            }
            else { 
                //新申请的，billId=0，和申请单没关联，加载所有符合条件的未关联箱子
                //先获取这个供应商这个账套所有的未使用外箱
                var boxes1 = (from o in db.OuterBoxes
                              join p in db.OuterBoxPOs on o.outer_box_id equals p.out_box_id
                              join i in db.InneBoxes on o.outer_box_id equals i.outer_box_id into tpi
                              from ti in tpi.DefaultIfEmpty()
                              where o.account == account
                              && (o.user_name + "A").Contains(userName)
                              && o.bill_id == null
                              select new
                              {
                                  box = o,
                                  pos = p,
                                  ibox = ti
                              }).ToList();

                //与当前po关联的外箱，包括部分关联
                var boxes2 = from b1 in boxes1
                             join pid in poIds on new { interid = b1.pos.po_id, entryid = b1.pos.po_entry_id } equals new { interid = pid.interId, entryid = pid.entryId }
                             select b1;

                        
                List<int> tempBoxId = new List<int>(); //保存可用的box id
                foreach (var id in boxes2.Select(b => b.box.outer_box_id).Distinct()) {
                    //1. 例外的（手动移出的），跳过
                    if (exceptIds.Where(e => e.interId == id).Count() > 0) continue;

                    //2. 部分关联的（合并箱，但是关联的PO没有都在PoIds里面的），跳过
                    var boxes1PoId = boxes1.Where(b => b.box.outer_box_id == id).Select(b => new { interid = b.pos.po_id, entryid = b.pos.po_entry_id }).Distinct().ToList();
                    bool jumpNext = false;
                    foreach (var bp in boxes1PoId) {
                        if (poIds.Where(p => p.interId == bp.interid && p.entryId == bp.entryid).Count() == 0) {
                            jumpNext = true;
                            break;
                        }
                    }
                    if (jumpNext) continue;
                    //符合条件的外箱id，加入到临时list
                    tempBoxId.Add(id);                                    
                }

                string poInfoString;
                foreach (var id in tempBoxId) {
                    poInfoString = "";
                    foreach (var p in boxes2.Where(b => b.box.outer_box_id == id).Select(b => b.pos).Distinct()) {
                        poInfoString += string.Format("{0}[{1}]：{2:0.####}；", p.po_number, p.po_entry_id, p.send_num);
                    }
                    list.Add(new BoxModels()
                    {
                        oBox = boxes2.Where(b => b.box.outer_box_id == id).FirstOrDefault().box,
                        poInfos = poInfoString,
                        poList = boxes2.Where(b => b.box.outer_box_id == id).Select(b => b.pos).Distinct().ToList(),
                        children = boxes2.Where(b => b.box.outer_box_id == id && b.ibox != null).Select(b => b.ibox).Distinct().ToList()
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// 删除外箱，还包括关联的所有内箱和外箱PO
        /// </summary>
        /// <param name="outerBoxId">外箱id</param>
        public string RemoveOuterBox(int outerBoxId)
        {
            string boxNumber = "";
            var box = db.OuterBoxes.Where(o => o.outer_box_id == outerBoxId).FirstOrDefault();
            if (box != null) {
                boxNumber = box.box_number;
            }
            db.OuterBoxes.DeleteAllOnSubmit(db.OuterBoxes.Where(o => o.outer_box_id == outerBoxId));
            db.InneBoxes.DeleteAllOnSubmit(db.InneBoxes.Where(i => i.outer_box_id == outerBoxId));
            db.OuterBoxPOs.DeleteAllOnSubmit(db.OuterBoxPOs.Where(o => o.out_box_id == outerBoxId));
            db.OuterBoxesDetail.DeleteAllOnSubmit(db.OuterBoxesDetail.Where(o => o.outer_box_id == outerBoxId));
            db.InnerBoxesDetail.DeleteAllOnSubmit(db.InnerBoxesDetail.Where(i => i.outer_box_id == outerBoxId));
            db.SubmitChanges();

            return boxNumber;
        }

        /// <summary>
        /// 删除内箱
        /// </summary>
        /// <param name="innerBoxId">内箱id</param>
        public string RemoveInnerBox(int innerBoxId)
        {
            var box = db.InneBoxes.Where(i => i.inner_box_id == innerBoxId).FirstOrDefault();
            if (box == null) return "";
            string boxNumber = box.box_number;

            db.InneBoxes.DeleteAllOnSubmit(db.InneBoxes.Where(i => i.inner_box_id == innerBoxId));
            db.InnerBoxesDetail.DeleteAllOnSubmit(db.InnerBoxesDetail.Where(i => i.inner_box_id == innerBoxId));
            db.InnerBoxesExtra.DeleteAllOnSubmit(db.InnerBoxesExtra.Where(i => i.inner_box_id == innerBoxId));
            db.SubmitChanges();

            return boxNumber;
        }

        /// <summary>
        /// 移出外箱，如果此外箱有bill_id,即将bill_id设置为null
        /// </summary>
        /// <param name="outerBoxId">外箱id</param>
        public void MoveOutBox(int outerBoxId)
        {
            var box = db.OuterBoxes.Where(o => o.outer_box_id == outerBoxId).FirstOrDefault();
            if (box == null) {
                throw new Exception("此外箱不存在");
            }
            if (box.bill_id != null) {
                box.bill_id = null;
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// 拆分外箱
        /// </summary>
        /// <param name="outerBoxId">外箱id</param>
        /// <param name="splitNum">拆分出来的件数</param>
        public string[] SplitOuterbox(int outerBoxId, int splitNum)
        {
            var b1 = db.OuterBoxes.Where(o => o.outer_box_id == outerBoxId).FirstOrDefault();
            if (b1 == null) {
                throw new Exception("此外箱不存在");
            }
            if (b1.pack_num <= splitNum) {
                throw new Exception("外箱件数必须大于拆分件数");
            }

            string boxNumberBefore = b1.box_number;

            //判断关联的内箱能否按照外箱的拆分按比例分割,因为内箱件数必须是外箱整数倍，所以不必再验证
            //foreach (var ib in db.InneBoxes.Where(i => i.outer_box_id == outerBoxId).ToList()) {
            //    if (ib.pack_num * 1.0m * splitNum / b1.pack_num - ib.pack_num * splitNum / b1.pack_num != 0) {
            //        throw new Exception("内箱箱号[" + ib.box_number + "]不能根据拆分件数按比例平均分割，拆分失败");
            //    }
            //}


            //通过序列化和反序列化的方式拷贝外箱对象
            OuterBoxes b2 = JsonConvert.DeserializeObject<OuterBoxes>(JsonConvert.SerializeObject(b1));

            string longBoxNum = b1.box_number_long;
            int packNum = (int)b1.pack_num;
            string[] boxNumArr = longBoxNum.Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
                        
            var num1 = boxNumArr.Take(boxNumArr.Length - splitNum);
            var num2 = boxNumArr.Skip(boxNumArr.Length - splitNum);
            
            //更新原始外箱
            if (num1.Count() == 1) {
                b1.box_number = num1.First();
            }
            else {
                b1.box_number = num1.First() + "~" + num1.Last();
            }
            b1.box_number_long = string.Join(",", num1);
            b1.pack_num = packNum - splitNum;

            //更新第二个外箱
            b2.outer_box_id = 0;
            if (num2.Count() == 1) {
                b2.box_number = num2.First();
            }
            else {
                b2.box_number = num2.First() + "~" + num2.Last();
            }
            b2.box_number_long = string.Join(",", num2);
            b2.pack_num = splitNum;
            db.OuterBoxes.InsertOnSubmit(b2);                       

            db.SubmitChanges();
                        
            try {
                //第二个外箱关联的PO信息也从第一个外箱拷贝
                var pos = db.OuterBoxPOs.Where(o => o.out_box_id == outerBoxId).ToList();
                if (pos.Count() > 0) {
                    var posList = JsonConvert.DeserializeObject<List<OuterBoxPOs>>(JsonConvert.SerializeObject(pos));
                    posList.ForEach(p => { p.out_box_id = b2.outer_box_id; p.out_box_po_id = 0; });
                    db.OuterBoxPOs.InsertAllOnSubmit(posList);
                }

                //第二个外箱关联的内箱也要跟着拆分
                var ins = db.InneBoxes.Where(i => i.outer_box_id == outerBoxId).ToList();
                if (ins.Count() > 0) {
                    foreach (var i in ins) {
                        var i1 = i;
                        var i2 = JsonConvert.DeserializeObject<InneBoxes>(JsonConvert.SerializeObject(i));

                        int iPackNum = (int)i.pack_num;
                        string iLongNum = i.box_number_long;
                        string[] iNumArr = iLongNum.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        var iNum1 = iNumArr.Take(iPackNum * (packNum - splitNum) / packNum);
                        var iNum2 = iNumArr.Skip(iPackNum * (packNum - splitNum) / packNum);

                        i1.pack_num = iNum1.Count();
                        if (iNum1.Count() == 1) {
                            i.box_number = iNum1.First();
                        }
                        else { 
                            i1.box_number = iNum1.First() + "~" + iNum1.Last();
                        }
                        i1.box_number_long = string.Join(",", iNum1);

                        i2.inner_box_id = 0;
                        i2.outer_box_id = b2.outer_box_id;
                        i2.pack_num = iNum2.Count();
                        if (iNum2.Count() == 1) {
                            i2.box_number = iNum2.First();
                        }
                        else {
                            i2.box_number = iNum2.First() + "~" + iNum2.Last();
                        }                        
                        i2.box_number_long = string.Join(",", iNum2);
                        db.InneBoxes.InsertOnSubmit(i2);

                        
                    }
                    
                }
                db.SubmitChanges();

                //更新箱子明细
                UpdateBoxDetaiId(b2.outer_box_id);

                return new string[] { boxNumberBefore, b1.box_number, b2.box_number };
            }
            catch (Exception ex) {
                //保存失败，回滚操作
                db.OuterBoxes.DeleteOnSubmit(b2);
                b1.box_number = boxNumArr[0] + "~" + boxNumArr[boxNumArr.Length - 1];
                b1.box_number_long = longBoxNum;
                b1.pack_num = packNum;
                db.SubmitChanges();
                throw new Exception("保存拆分外箱关联的PO信息或内箱信息失败，原因："+ex.Message);
            }

            

        }

        /// <summary>
        /// 拆分箱子后，在同步修改一下箱子明细
        /// </summary>
        /// <param name="newOuterBoxNumber"></param>
        private void UpdateBoxDetaiId(int newOuterBoxId)
        {
            var outerBox = db.OuterBoxes.Where(o => o.outer_box_id == newOuterBoxId).FirstOrDefault();
            if (outerBox == null) return;
            var innerboxes = db.InneBoxes.Where(i => i.outer_box_id == outerBox.outer_box_id).ToList();

            //更新外箱明细
            var outerNumberArr = outerBox.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var oNum in outerNumberArr) {
                foreach (var od in db.OuterBoxesDetail.Where(o => o.box_number == oNum)) {
                    od.outer_box_id = outerBox.outer_box_id;
                }
            }

            //更新内箱明细
            foreach (var ib in innerboxes) {
                var innerBoxNumberArr = ib.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var iNum in innerBoxNumberArr) {
                    foreach (var id in db.InnerBoxesDetail.Where(i => i.inner_box_number == iNum)) {
                        id.outer_box_id = newOuterBoxId;
                        id.inner_box_id = ib.inner_box_id;
                    }
                }
            }

            db.SubmitChanges();

        }

        public IQueryable<OuterBoxes> GetOuterBoxes(SearchBoxParams p,bool canCheckAll)
        {
            var result = from b in db.OuterBoxes
                         where b.create_date >= p.beginDate && b.create_date <= p.endDate
                         && b.account == p.account
                         && (b.item_name.Contains(p.itemInfo) || b.item_model.Contains(p.itemInfo))
                         && (p.hasUsed == "所有" || (p.hasUsed == "已使用" && b.bill_id != null) || (p.hasUsed == "未使用" && b.bill_id == null))
                         && (canCheckAll || (b.user_name + "A").Contains(p.userName))                         
                         select b;

            if (!string.IsNullOrWhiteSpace(p.boxNumber)) {
                result = from b in result
                         join i in db.InneBoxes on b.outer_box_id equals i.outer_box_id into ti
                         from ib in ti.DefaultIfEmpty()
                         where (b.box_number_long.Contains(p.boxNumber) || ib.box_number_long.Contains(p.boxNumber))
                         select b;
            }

            if (!string.IsNullOrWhiteSpace(p.poNo)) {
                result = from b in result
                         join po in db.OuterBoxPOs on b.outer_box_id equals po.out_box_id
                         where po.po_number.Contains(p.poNo)
                         select b;
            }
            if (!string.IsNullOrWhiteSpace(p.billNo)) {
                result = from b in result
                         join bill in db.DRBills on b.bill_id equals bill.bill_id
                         where bill.bill_no.Contains(p.billNo)
                         select b;
            }

            return result.Distinct().OrderBy(r => r.create_date);
        }

        public List<OuterBoxPOs> GetBoxPos(List<int> boxIds)
        {
            return db.OuterBoxPOs.Where(o => boxIds.Contains((int)o.out_box_id)).ToList();
        }

        public List<InneBoxes> GetInnerBoxes(string outerBoxNumber)
        {
            return (from i in db.InneBoxes
                    join o in db.OuterBoxes on i.outer_box_id equals o.outer_box_id
                    where o.box_number == outerBoxNumber
                    orderby i.box_number
                    select i).ToList();
        }

        public List<IDModel> HasGotInnerBox(List<int> boxIds)
        {
            return (from o in db.OuterBoxes
                    join i in db.InneBoxes on o.outer_box_id equals i.outer_box_id
                    where boxIds.Contains(o.outer_box_id)
                    select new IDModel()
                    {
                        interId = o.outer_box_id
                    }
                    ).Distinct().ToList();
        }

        public List<K3POs4BoxModel> GetPos4Box(string billType, string searchValue, int userId, string userName, string account)
        {
            return db.ExecuteQuery<K3POs4BoxModel>(
                "exec GetK3POs4Box @account = {0},@billType = {1},@searchValue = {2},@userId = {3},@userNumber = {4}",
                account, billType, searchValue, userId, userName).ToList();
        }
        
        public List<NotFinishBoxQty> GetNotFinishedBoxQty(List<IDModel> ids)
        {
            return (from op in db.OuterBoxPOs
                    join o in db.OuterBoxes on op.out_box_id equals o.outer_box_id
                    join b in db.DRBills on o.bill_id equals b.bill_id into tb
                    from bill in tb.DefaultIfEmpty()
                    where (bill == null || bill.p_status != "已入库")
                    && ids.Contains(new IDModel() { interId = op.po_id, entryId = op.po_entry_id })
                    select new NotFinishBoxQty()
                    {
                        poId = (int)op.po_id,
                        poEntryId = (int)op.po_entry_id,
                        qty = op.send_num * o.pack_num
                    }).ToList();
        }

        public OuterBoxes GetBoxBySupplierAndItem(string supplierNumber, string itemNumber)
        {
            return db.OuterBoxes.Where(o => o.user_name == supplierNumber && o.item_number == itemNumber).OrderByDescending(o=>o.outer_box_id).FirstOrDefault();
        }

        public IQueryable<InnerBoxExtraModel> GetInnerBoxesExtra(SearchInnerBoxExtraParam p,bool canCheckAll)
        {
            var result = from ibe in db.InnerBoxesExtra
                         join ib in db.InneBoxes on ibe.inner_box_id equals ib.inner_box_id 
                         join o in db.OuterBoxes on ib.outer_box_id equals o.outer_box_id into ot
                         from ob in ot.DefaultIfEmpty()
                         where ibe.create_date >= p.beginDate && ibe.create_date <= p.endDate
                         && ibe.account == p.account
                         && (ibe.item_model.Contains(p.itemInfo) || ibe.item_name.Contains(p.itemInfo))
                         && (canCheckAll || (ibe.user_name + "A").Contains(p.userName))
                         && ((p.hasUsed == "所有") || (p.hasUsed == "已使用" && ob.bill_id != null) || (p.hasUsed == "未使用" && ob.bill_id == null))
                         && ((p.hasRelated == "所有") || (p.hasRelated == "已关联" && ib.outer_box_id != null) || (p.hasRelated == "未关联" && ib.outer_box_id == null))
                         && ib.box_number_long.Contains(p.innerBoxNumber)
                         &&(p.outerBoxNumber=="" || ob.box_number_long.Contains(p.outerBoxNumber))
                         orderby ibe.create_date descending
                         select new InnerBoxExtraModel()
                         {
                             ib = ib,
                             extra = ibe,
                             hasRelated = ib.outer_box_id == null ? "未关联" : "已关联",
                             hasUsed = ob == null ? "未使用" : (ob.bill_id == null ? "未使用" : "已使用"),
                             outerBoxNumber = ob == null ? "" : ob.box_number
                         };
            
            return result;
                       
        }

        public InnerBoxesExtra GetInnerBoxExtraBefore(string supplierNumber, string itemNumber)
        {
            return db.InnerBoxesExtra.Where(i => i.user_name == supplierNumber && i.item_number == itemNumber).OrderByDescending(i => i.inner_box_extra_id).FirstOrDefault();
        }

        public string SaveInnerBoxWithExtra(InnerBoxesExtra extra, int packNum, decimal everyQty)
        {
            //首先增加内箱表
            InneBoxes ib = new InneBoxes();           

            var innerBoxNumber = new ItemSv().GetBoxNumber("I", packNum);
            ib.box_number = innerBoxNumber[0];
            ib.box_number_long = innerBoxNumber[1];
            ib.pack_num = packNum;
            ib.every_qty = everyQty;

            db.InneBoxes.InsertOnSubmit(ib);
            db.SubmitChanges();

            //增加到extra表
            try {
                extra.inner_box_id = ib.inner_box_id;
                db.InnerBoxesExtra.InsertOnSubmit(extra);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                //extra新增失败，删除掉内箱
                db.InneBoxes.DeleteOnSubmit(ib);
                db.SubmitChanges();

                throw ex;
            }

            return ib.box_number;
        }


        public string[] SplitInnerBoxesWithExtra(int innerBoxId, int splitNum)
        {
            var innerBox = db.InneBoxes.Where(i => i.inner_box_id == innerBoxId).FirstOrDefault();
            if (innerBox == null) {
                throw new Exception("内箱不存在");
            }
            if (innerBox.pack_num <= splitNum) {
                throw new Exception("内箱件数必须大于拆分件数");
            }
            if (innerBox.outer_box_id != null) {
                throw new Exception("只能拆分未关联外箱的内箱");
            }

            var boxNumberBefore = innerBox.box_number_long;
            var b2 = JsonConvert.DeserializeObject<InneBoxes>(JsonConvert.SerializeObject(innerBox));
            var boxNumberArr = boxNumberBefore.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var packNum = (int)innerBox.pack_num;            
            var num1 = boxNumberArr.Take(packNum - splitNum);
            var num2 = boxNumberArr.Skip(packNum - splitNum);

            //更新原始内箱
            innerBox.pack_num = packNum - splitNum;
            if (num1.Count() == 1) {
                innerBox.box_number = num1.First();
            }
            else {
                innerBox.box_number = num1.First() + "~" + num1.Last();
            }
            innerBox.box_number_long = string.Join(",", num1);

            //添加拆分箱
            b2.inner_box_id = 0;
            b2.pack_num = splitNum;
            if (num2.Count() == 1) {
                b2.box_number = num2.First();
            }
            else {
                b2.box_number = num2.First() + "~" + num2.Last();
            }
            b2.box_number_long = string.Join(",", num2);
            db.InneBoxes.InsertOnSubmit(b2);

            db.SubmitChanges();

            try {
                var e1 = db.InnerBoxesExtra.Where(e => e.inner_box_id == innerBoxId).FirstOrDefault();
                if (e1 != null) {
                    var e2 = JsonConvert.DeserializeObject<InnerBoxesExtra>(JsonConvert.SerializeObject(e1));
                    e2.inner_box_id = b2.inner_box_id;
                    e2.inner_box_extra_id = 0;
                    e2.create_date = DateTime.Now;
                    db.InnerBoxesExtra.InsertOnSubmit(e2);
                    db.SubmitChanges();
                }
            }
            catch (Exception ex) {
                //失败回滚
                innerBox.pack_num = packNum;
                innerBox.box_number = boxNumberArr.First() + "~" + boxNumberArr.Last();
                innerBox.box_number_long = string.Join(",", boxNumberArr);
                db.InneBoxes.DeleteOnSubmit(b2);
                db.SubmitChanges();
                throw new Exception("保存内箱其他信息时失败，原因：" + ex.Message);
            }

            return new string[] { boxNumberBefore, innerBox.box_number, b2.box_number };

        }

        public List<NotRelatedInnerBox> GetNoRelatedInnerBox(string userName, string itemNumber,string tradeTypeName,string account, bool canCheckAll)
        {
            return (from e in db.InnerBoxesExtra
                    join i in db.InneBoxes on e.inner_box_id equals i.inner_box_id
                    where i.outer_box_id == null
                    && ((e.user_name + "A").StartsWith(userName) || canCheckAll)
                    && e.item_number == itemNumber
                    && e.account == account
                    && e.trade_type_name == tradeTypeName
                    orderby i.inner_box_id descending
                    select new NotRelatedInnerBox()
                    {
                        inner_box_id = i.inner_box_id,
                        box_number = i.box_number,
                        every_qty = i.every_qty,
                        pack_num = i.pack_num,
                        batch = e.batch,
                        brand = e.brand,
                        item_model = e.item_model,
                        item_name = e.item_name,
                        item_number = e.item_number,
                        keep_condition = e.keep_condition,
                        made_by = e.made_by,
                        made_in = e.made_in,
                        package_date = e.package_date,
                        produce_circle = e.produce_circle,
                        produce_date = e.produce_date,
                        safe_period = e.safe_period,
                        rohs = e.rohs,
                        expire_date = e.expire_date
                    }).ToList();
        }

        public void CancelIOBoxRelation(string outerBoxNumber)
        {
            var ob = db.OuterBoxes.Where(o => o.box_number == outerBoxNumber).FirstOrDefault();
            if (ob == null) {
                throw new Exception("外箱不存在");
            }
            //取消和内箱关联
            foreach (var ib in db.InneBoxes.Where(i => i.outer_box_id == ob.outer_box_id).ToList()) {
                ib.outer_box_id = null;

            }
            //删除内箱明细信息
            db.InnerBoxesDetail.DeleteAllOnSubmit(db.InnerBoxesDetail.Where(i => i.outer_box_id==ob.outer_box_id));

            db.SubmitChanges();
        }

        //public void GenerateOuterBoxDetail()
        //{
        //    foreach (var o in db.OuterBoxes.Where(o=>o.outer_box_id<1413).ToList()) {
        //        var longNumberArr = o.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (var num in longNumberArr) {
        //            db.OuterBoxesDetail.InsertOnSubmit(new OuterBoxesDetail()
        //            {
        //                box_number = num,
        //                outer_box_id = o.outer_box_id
        //            });
        //        }
        //    }
        //    db.SubmitChanges();
        //}

        //public void GenerateInnerBoxDetail()
        //{
        //    foreach (var i in db.InneBoxes.Where(i=>i.inner_box_id<1070).ToList()) {
        //        var innerBoxArr = i.box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        //        var outerBoxArr = db.OuterBoxes.Where(ob => ob.outer_box_id == i.outer_box_id).FirstOrDefault()
        //                            .box_number_long.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        //        for (var j = 0; j < innerBoxArr.Count(); j++) {                    
        //            db.InnerBoxesDetail.InsertOnSubmit(new InnerBoxesDetail(){
        //                inner_box_id=i.inner_box_id,
        //                outer_box_id=(int)i.outer_box_id,
        //                inner_box_number=innerBoxArr[j],
        //                outer_box_number=outerBoxArr[j / (innerBoxArr.Count() / outerBoxArr.Count())]
        //            });
        //        }
        //    }
        //    db.SubmitChanges();
        //}
    }
}