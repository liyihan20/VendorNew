using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Models;
using VendorNew.Filters;
using VendorNew.Services;
using VendorNew.Utils;

namespace VendorNew.Controllers
{
    public class BoxController : BaseController
    {
        
        [AuthorityFilter]
        public ActionResult Boxes()
        {            
            ViewData["account"] = currentAccount;
            return View();
        }
        
        [SessionTimeOutJsonFilter]
        public JsonResult GetBoxNodes(FormCollection fc)
        {
            SearchBoxParams p = new SearchBoxParams();
            MyUtils.SetFieldValueToModel(fc, p);

            p.endDate = p.endDate.AddDays(1);
            p.account = currentAccount;            
            p.itemInfo = p.itemInfo ?? "";
            p.userName = currentUser.userName;

            BoxSv sv = new BoxSv();
            if (p.id == null) {
                //搜索外箱信息
                try {
                    var result = sv.GetOuterBoxes(p, canCheckAll).OrderByDescending(r => r.create_date);
                    if (result.Count() < p.rows && p.page > 1) {
                        p.page = 1; //应该是当前easyui的bug，翻页后，比如在第二页以后的页码中搜索箱子，结果有1行，但是page还是搜索时候的那个页码，导致页面加载不出箱子，在这里手动将页码调为1
                    }
                    var outerBoxes = result.Skip((p.page - 1) * p.rows).Take(p.rows).ToList(); //外箱信息
                    var obIds = outerBoxes.Select(o => o.outer_box_id).ToList(); //所有外箱id
                    var pos = sv.GetBoxPos(obIds); //po信息
                    var boxIdHasInner = sv.HasGotInnerBox(obIds); //有内箱的外箱id集合
                    var billNoInfo = new DRSv().GetBillIdAndNo(outerBoxes.Where(o => o.bill_id != null).Select(o => (int)o.bill_id).Distinct().ToList());

                    return Json(new { suc = true, total = result.Count(), box = outerBoxes, po = pos, boxIdHasInner = boxIdHasInner, billNoInfo=billNoInfo });
                }
                catch (Exception ex) {
                    return Json(new SRM(ex));
                }
            }
            else {
                //展开内箱信息
                try {
                    var innerboxes = sv.GetInnerBoxes(p.id);
                    return Json(new { suc = true, box = innerboxes });
                }
                catch (Exception ex) {
                    return Json(new SRM(ex));
                }
            }
        }

        [SessionTimeOutJsonFilter]
        public JsonResult GetPO4Box(string billType,string searchValue, int page,int rows)
        {
            var boxSv = new BoxSv();
            var drSv = new DRSv();

            List<K3POs4BoxModel> pos;
            string GetPO4Box_param = (string)Session["GetPO4Box_param"];
            if (GetPO4Box_param == null || !GetPO4Box_param.Equals(billType + ":" + searchValue)) {
                pos = boxSv.GetPos4Box(billType, searchValue, currentUser.userId, currentUser.userName, currentAccount);
                pos = pos.OrderByDescending(p => p.po_date).Take(1000).ToList(); //最多显示1000条记录
                Session["GetPO4Box_param"] = billType + ":" + searchValue;
                Session["GetPO4Box_list"] = pos; //为加快翻页速度，将数据放在session中
            }
            else {
                pos = (List<K3POs4BoxModel>)Session["GetPO4Box_list"];
            }
            var result = pos.OrderByDescending(p => p.po_date).Skip((page - 1) * rows).Take(rows).ToList();

            var noFinishBox = boxSv.GetNotFinishedBoxQty(result.Select(r => new IDModel() { interId = r.po_id, entryId = r.po_entry_id }).ToList());
            foreach (var p in result) {
                p.id_field = p.po_id + "-" + p.po_entry_id;
                p.can_make_box_qty = p.po_qty - p.realte_qty - (noFinishBox.Where(f => f.poId == p.po_id && f.poEntryId == p.po_entry_id).Sum(r => r.qty) ?? 0m);
            }

            return Json(new { suc = true, total = pos.Count(), rows = result });            
        }

        public JsonResult GetBoxBySupplierAndItem(string supplierNumber, string itemNumber)
        {
            var result = new BoxSv().GetBoxBySupplierAndItem(supplierNumber, itemNumber);
            if (result == null) {
                return Json(new SRM(false));
            }
            result.bill_id = 0;//2018-11-29 if not 0, the relation will be faulty
            result.account = currentAccount;
            return Json(new { suc = true, boxInfo = result });
        }

        [AuthorityFilter]
        public ActionResult InnerBoxesExtra()
        {
            ViewData["account"] = currentAccount;
            return View();
        }


        [SessionTimeOutJsonFilter]
        public JsonResult GetInnerBoxexExtra(FormCollection fc)
        {
            SearchInnerBoxExtraParam p = new SearchInnerBoxExtraParam();
            MyUtils.SetFieldValueToModel(fc, p);

            p.endDate = p.endDate.AddDays(1);
            p.account = currentAccount;
            p.itemInfo = p.itemInfo ?? "";
            p.userName = currentUser.userName;
            p.innerBoxNumber = p.innerBoxNumber ?? "";
            p.outerBoxNumber = p.outerBoxNumber ?? "";

            var result = new BoxSv().GetInnerBoxesExtra(p, canCheckAll);
            int total = result.Count();

            return Json(new { suc = true, total = total, result = result.Skip((p.page - 1) * p.rows).Take(p.rows).ToList() });

        }
                
        public JsonResult SearchK3Item(string searchValue)
        {
            return Json(new ItemSv().SearchK3Item(searchValue, currentAccount));
        }

        [SessionTimeOutJsonFilter]
        public JsonResult GetInnerBoxExtraBefore(string itemNumber)
        {
            var extra = new BoxSv().GetInnerBoxExtraBefore(currentUser.userName, itemNumber);
            if (extra == null) {
                return Json(new SRM(false));
            }
            extra.account = currentAccount;
            return Json(new { suc = true, boxInfo = extra });
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SaveInnerBoxWithExtra(FormCollection fc)
        {
            InnerBoxesExtra extra = new InnerBoxesExtra();
            MyUtils.SetFieldValueToModel(fc, extra);

            if (string.IsNullOrEmpty(extra.item_number)) {
                return Json(new SRM(false, "请先选择物料信息后再保存"));
            }

            try {
                extra.create_date = DateTime.Now;
                extra.user_name = currentUser.userName;
                int packNum = Int32.Parse(fc.Get("pack_num"));
                decimal everyQty = decimal.Parse(fc.Get("every_qty"));

                string innerBoxNumber = new BoxSv().SaveInnerBoxWithExtra(extra, packNum, everyQty);
                WLog("保存Extra内箱", "箱号：" + innerBoxNumber);
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
            return Json(new SRM());
        }

        [SessionTimeOutJsonFilter]
        public JsonResult SplitInnerBoxWithExtra(int innerBoxId, int splitNum)
        {
            try {
                var result = new BoxSv().SplitInnerBoxesWithExtra(innerBoxId, splitNum);
                WLog("拆分内箱Extra", string.Format("{0}-->{1};{2}", result[0], result[1], result[2]));
                return Json(new SRM());
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
        }

        [SessionTimeOutJsonFilter]
        public JsonResult GetNoRelatedInnerBoxes(string itemNumber, string tradeTypeName)
        {
            return Json(new { suc = true, boxInfo = new BoxSv().GetNoRelatedInnerBox(currentUser.userName, itemNumber, tradeTypeName,currentAccount, canCheckAll) });
        }

        [SessionTimeOutJsonFilter]
        public JsonResult CancelIOBoxRelation(string outerBoxNumber)
        {
            new BoxSv().CancelIOBoxRelation(outerBoxNumber);
            WLog("取消关联内外箱", outerBoxNumber);
            return Json(new SRM());
        }
    }
}
