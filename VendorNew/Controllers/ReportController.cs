using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VendorNew.Filters;
using VendorNew.Services;
using VendorNew.Utils;
using org.in2bits.MyXls;
using VendorNew.Models;
using Newtonsoft.Json;

namespace VendorNew.Controllers
{
    public class ReportController : BaseController
    {
        [AuthorityFilter]
        public ActionResult PrintApply(int billId, string pageNumList = null)
        {
            //先验证权限
            if (!canCheckAll) {
                if (!new UASv().CanCheckTheDRBill(billId, currentUser.userName, currentUser.userId)) {
                    ViewBag.tip = "单据不存在或无权限查看";
                    WLog("打印送货单", "无权访问：" + billId.ToString(), "", false);
                    return View("Error");
                }
            }

            int defaultNumPerPage = 14;
            int totalNumToDisplay = 0;
            PrintApplyModels m;

            if (pageNumList == null) {
                pageNumList = defaultNumPerPage.ToString();//默认1页显示14行数据
            }

            try {
                m = new ReportSv().GetData4Print(billId, currentAccount);
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            totalNumToDisplay = m.boxAndPos.Count() == 0 ? m.es.Count() : m.boxAndPos.Count();
            List<int> pageNumArr = MyUtils.GetPageNumberList(defaultNumPerPage, pageNumList, totalNumToDisplay);

            ViewData["reportData"] = m;
            ViewData["pageNumList"] = pageNumList;
            ViewData["pageNumArr"] = pageNumArr;

            WLog("打印送货申请单", "进入打印界面", m.h.bill_no);

            return View();
        }

        [AuthorityFilter]
        public ActionResult PrintOuterQrcode(int billId,int numPerPage=1)
        {
            if (numPerPage < 1) numPerPage = 1;

            try {
                var dr=new DRSv().GetDRBill(billId);
                var result = new ReportSv().GetOuterBoxes4Print(billId);
                ViewData["outerData"] = result;
                ViewData["numPerPage"] = numPerPage;
                ViewData["billId"] = billId;
                WLog("打印外箱标签", "打印送货申请关联箱子标签", dr == null ? "" : dr.bill_no);
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult PrintSelectedOuterBox(string boxIds, int numPerPage = 1)
        {
            if (numPerPage < 1) numPerPage = 1;

            List<int> boxIdList = boxIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(b => Int32.Parse(b)).ToList();

            try {
                var result = new ReportSv().GetOuterBoxes4Print(boxIdList);
                ViewData["outerData"] = result;
                ViewData["numPerPage"] = numPerPage;
                ViewData["boxIds"] = boxIds;

                WLog("打印外箱", "打印所有选中的外箱：" + string.Join(",", result.Select(r => r.boxNumber).ToList()));
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }


            return View();
        }

        [AuthorityFilter]
        public ActionResult PrintInnerQrcode(int billId, int numPerPage = 1)
        {
            if (numPerPage < 1) numPerPage = 1;

            try {
                var dr = new DRSv().GetDRBill(billId);
                var result = new ReportSv().GetInnerBoxes4Print(billId);
                ViewData["innerData"] = result;
                ViewData["numPerPage"] = numPerPage;
                ViewData["billId"] = billId;
                WLog("打印内箱标签", "打印送货申请关联箱子标签", dr == null ? "" : dr.bill_no);
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            return View();
        }

        /// <summary>
        /// 打印所有所选外箱里面的所有内箱
        /// </summary>
        /// <param name="boxIds"></param>
        /// <param name="numPerPage"></param>
        /// <param name="inOrOut">in:打印所选内箱；out:打印所选外箱</param>
        /// <returns></returns>
        [SessionTimeOutFilter]
        public ActionResult PrintSelectedInnerBox(string boxIds,string inOrOut = "out", int numPerPage = 1)
        {
            if (numPerPage < 1) numPerPage = 1;

            List<int> boxIdList = boxIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(b => Int32.Parse(b)).ToList();

            try {
                List<PrintInnerBoxModel> result;
                if (inOrOut.Equals("out")) {
                    result = new ReportSv().GetInnerBoxes4Print(boxIdList);
                }
                else {
                    result = new ReportSv().GetInnerBoxesWithExtra4Print(boxIdList);
                }
                ViewData["innerData"] = result;
                ViewData["inOrOut"] = inOrOut;
                ViewData["numPerPage"] = numPerPage;
                ViewData["boxIds"] = boxIds;
                WLog("打印内箱", "打印所有选中的内箱：" + string.Join(",", result.Select(r => r.boxNumber).ToList()));
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }

            return View();
        }


        /// <summary>
        /// 导出申请单excel，包括我的申请和我的审批
        /// </summary>
        /// <param name="fc"></param>
        public void ExporDRData(string queryJson, string pageType)
        {
            SearchMyApplyParams p = JsonConvert.DeserializeObject<SearchMyApplyParams>(queryJson);            

            p.account = currentAccount;
            p.userId = currentUser.userId;
            p.userName = currentUser.userName;
            
            var result = new List<CheckApplyListModel>();
            string fileName = "";

            if (pageType.Equals("apply")) {
                fileName = "我的送货申请_";
                result = new DRSv().SearchMyApplyList(p, canCheckAll).ToList();
            }
            else if (pageType.Equals("audit")) {
                fileName = "审核送货申请_";
                result = new DRSv().SearchMyAuditList(p, canCheckAll).ToList();
            }

            ushort[] colWidth = new ushort[] { 16, 18, 12, 16, 10, 24,
                                               32, 16, 16, 12, 18,18, 16,
                                               16, 16, 16, 16, 18, 16, 18, 16 };

            string[] colName = new string[] { "发货日期", "送货单号", "申请状态", "订单编号", "分录号", "物料名称", 
                                              "规格型号", "订单数量", "申请数量", "单位", "物料编码","备注","订单类型",
                                              "采购方式","订料员","采购员","贸易类型","PR单号","采购日期","入库单号","入库日期" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = fileName + DateTime.Now.ToString("MMddHHmmss");
            Worksheet sheet = xls.Workbook.Worksheets.Add("申请列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in result) {
                colIndex = 1;

                //"发货日期", "送货单号", "申请状态", "订单编号", "分录号", "物料名称", 
                //"规格型号", "订单数量", "申请数量", "单位", "物料编码","订单类型",
                //"采购方式","订料员","采购员","贸易类型","PR单号","采购日期","入库单号","入库日期"
                cells.Add(++rowIndex, colIndex, ((DateTime)d.sendDate).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.billNo);
                cells.Add(rowIndex, ++colIndex, d.pStatus);
                cells.Add(rowIndex, ++colIndex, d.poNo);
                cells.Add(rowIndex, ++colIndex, d.poEntryId);
                cells.Add(rowIndex, ++colIndex, d.itemName);

                cells.Add(rowIndex, ++colIndex, d.itemModel);
                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}",d.poQty));
                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}",d.sendQty));
                cells.Add(rowIndex, ++colIndex, d.unitName);
                cells.Add(rowIndex, ++colIndex, d.itemNumber);
                cells.Add(rowIndex, ++colIndex, d.comment);
                cells.Add(rowIndex, ++colIndex, d.billType);

                cells.Add(rowIndex, ++colIndex, d.buyType);
                cells.Add(rowIndex, ++colIndex, d.matOrderName);
                cells.Add(rowIndex, ++colIndex, d.buyerName);
                cells.Add(rowIndex, ++colIndex, d.tradeTypeName);
                cells.Add(rowIndex, ++colIndex, d.prNo);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.poDate).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.inStockBillNo);
                cells.Add(rowIndex, ++colIndex, d.inStockBillDate==null?"":((DateTime)d.inStockBillDate).ToString("yyyy-MM-dd"));
            }

            xls.Send();

            WLog("导出申请单Excel", fileName + ":" + queryJson);
        }

        public void ExportPOData(string queryJson)
        {
            GetK3POParams p = JsonConvert.DeserializeObject<GetK3POParams>(queryJson);

            p.poNumbers = p.poNumbers == null ? "" : p.poNumbers.Trim();//去掉前后空格
            p.itemInfo = p.itemInfo == null ? "" : p.itemInfo.Trim();
            p.account = currentAccount;
            p.userId = currentUser.userId;
            p.userNumber = currentUser.userName;
            p.k3HasAudit = true;

            List<K3POs> result;
            List<decimal> qtyList;
            try {
                result = new DRSv().GetPOs(p).OrderBy(po => po.poDate).Take(1000).ToList();
                //一次性关联1000条到数据库查询会导致内存溢出，所以大于500条的，分2次获取
                var infos = result.Take(500).Select(r => new IDModel() { interId = r.poId, entryId = r.poEntryId }).ToList();
                qtyList = new DRSv().GetPOTransitQty(infos);
                if (result.Count() > 500) {
                    infos = result.Skip(500).Select(r => new IDModel() { interId = r.poId, entryId = r.poEntryId }).ToList();
                    qtyList.AddRange(new DRSv().GetPOTransitQty(infos));
                }
            }
            catch {
                return;
            }

            ushort[] colWidth = new ushort[] { 18, 10, 20, 24, 32, 16,
                                               16, 16, 16, 12, 24, 12, 16,
                                               12, 12, 16, 12, 12, 16 };

            string[] colName = new string[] { "订单编号", "分录号", "PR单号", "物料名称", "规格型号", "订单数量", 
                                              "入库数量", "申请数量", "可申请数量", "订料员", "申购部门","单位","物料编码",
                                              "订单类型","采购方式","订单日期","贸易类型","币别","采购员" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = "订单明细表_" + DateTime.Now.ToString("MMddHHmmss");
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            for (var i = 0; i < result.Count();i++ ) {
                var d = result[i];
                var q = qtyList[i];
                colIndex = 1;

                //"订单编号", "分录号", "PR单号", "物料名称", "规格型号", "订单数量", 
                //"入库数量", "申请数量", "可申请数量", "订料员", "申购部门","单位","物料编码",
                //"订单类型","采购方式","订单日期","贸易类型","币别","采购员"
                cells.Add(++rowIndex, colIndex, d.poNo);
                cells.Add(rowIndex, ++colIndex, d.poEntryId);
                cells.Add(rowIndex, ++colIndex, d.prNo);
                cells.Add(rowIndex, ++colIndex, d.itemName);
                cells.Add(rowIndex, ++colIndex, d.itemModel);
                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}",d.orderQty));

                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}",d.realteQty));
                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}", q));
                cells.Add(rowIndex, ++colIndex, string.Format("{0:0.####}", d.orderQty - d.realteQty - q));
                cells.Add(rowIndex, ++colIndex, d.matOrderName);
                cells.Add(rowIndex, ++colIndex, d.departmentName);
                cells.Add(rowIndex, ++colIndex, d.unitName);
                cells.Add(rowIndex, ++colIndex, d.itemNumber);

                cells.Add(rowIndex, ++colIndex, d.billType);
                cells.Add(rowIndex, ++colIndex, d.buyType);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.poDate).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.tradeTypeName);
                cells.Add(rowIndex, ++colIndex, d.unitName);
                cells.Add(rowIndex, ++colIndex, d.buyerName);
            }

            xls.Send();

            WLog("导出K3订单Excel", "行数：" + result.Count() + ";" + queryJson);

            //return "已成功导出行数："+result.Count();
        }

        [AuthorityFilter]
        public ActionResult StatisticsChart()
        {
            return View();
        }

        public JsonResult GetDayNumData4Chart()
        {
            try {
                var result = new ReportSv().GetDayNumChartData();
                return Json(new { suc = true, result = result });
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
        }

        /// <summary>
        /// 获取今天做的最多的前10个申请、外箱、内箱、小标签内箱对应的供应商和数量
        /// </summary>
        /// <returns></returns>
        public JsonResult GetTopTenSupplier4Chart()
        {
            try {
                var d1 = new ReportSv().GetTopTenSupplierApplyNumToday();
                var d2 = new ReportSv().GetTopTenSupplierOBoxNumToday();
                var d3 = new ReportSv().GetTopTenSupplierIBoxNumToday();
                var d4 = new ReportSv().GetTopTenSupplierIBoxExtraNumToday();
                return Json(new { suc = true, d1 = d1, d2 = d2, d3 = d3, d4 = d4 });
            }
            catch (Exception ex) {
                return Json(new SRM(ex));
            }
        }

    }
}
