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
                    var result = sv.GetOuterBoxes(p, canCheckAll);
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

    }
}
