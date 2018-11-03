using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorNew.Models
{
    /// <summary>
    /// 获取po所需要的参数
    /// </summary>
    public class GetK3POParams
    {
        public string account { get; set; }
        public string billType { get; set; }
        public string poNumbers { get; set; }
        public DateTime beginDate { get; set; }
        public DateTime endDate { get; set; }
        public string userNumber { get; set; }
        public int userId { get; set; }
        public bool k3HasAudit { get; set; }
        public bool isFinished { get; set; }

        public GetK3POParams() { }
        public GetK3POParams(string account, string billType, string poNumbers, DateTime beginDate, DateTime endDate, string userNumber, int userId, bool k3HasAudit = true, bool isFinished = true)
        {
            this.account = account;
            this.billType = billType;
            this.poNumbers = poNumbers;
            this.beginDate = beginDate;
            this.endDate = endDate;
            this.userNumber = userNumber;
            this.userId = userId;
            this.k3HasAudit=k3HasAudit;
            this.isFinished = isFinished;
        }
    }

    /// <summary>
    /// 取得的po信息字段
    /// </summary>
    public class K3POs
    {
        public string account { get; set; }
        public string billType { get; set; }
        public int poId { get; set; }
        public int poEntryId { get; set; }
        public int contractEntryId { get; set; }
        public DateTime poDate { get; set; }
        public string poNo { get; set; }
        public string departmentName { get; set; }
        public string supplierNumber { get; set; }
        public string supplierName { get; set; }
        public string currencyNumber { get; set; }
        public string currencyName { get; set; }
        public string tradeTypeNumber { get; set; }
        public string tradeTypeName { get; set; }
        public string buyType { get; set; }
        public string buyerNumber { get; set; }
        public string buyerName { get; set; }
        public string matOrderName { get; set; }
        public string matOrderNumber { get; set; }
        public string prNo { get; set; }
        public string sourceBillNo { get; set; }
        public int sourceInterID { get; set; }
        public int sourceEntryID { get; set; }
        public string itemNumber { get; set; }
        public string itemName { get; set; }
        public string itemModel { get; set; }
        public string unitNumber { get; set; }
        public string unitName{get;set;}
        public decimal orderQty { get; set; }
        public decimal realteQty { get; set; }
        public string transitQty { get; set; } //不会一开始从k3服务器读取
    }

    /// <summary>
    /// po入库记录字段
    /// </summary>
    public class POInstockRecord
    {
        public string inDate { get; set; }
        public string billNo { get; set; }
        public int entryId { get; set; }
        public decimal inQty { get; set; }
        public string unitName { get; set; }
    }

    public class SearchMyApplyParams
    {
        public int page { get; set; }
        public int rows { get; set; }
        public DateTime beginDate { get; set; }
        public DateTime endDate { get; set; }
        public string billType { get; set; }
        public string billNo { get; set; }
        public string poNo { get; set; }
        public string itemInfo { get; set; }
        public string pStatus { get; set; }
        public string account { get; set; }
        public int userId { get; set; }
        public string userName { get; set; }
    }

    public class CheckApplyListModel
    {
        public int billId { get; set; }
        public string billNo { get; set; }
        public DateTime? sendDate { get; set; }
        public string pStatus { get; set; }
        public string supplierName { get; set; }
        public string billType { get; set; }
        public string buyType { get; set; }
        public string matOrderNumber { get; set; }
        public string matOrderName { get; set; }
        public string buyerNumber { get; set; }
        public string buyerName { get; set; }
        public string tradeTypeName { get; set; }
        public string inStockBillNo { get; set; }
        public DateTime? inStockBillDate { get; set; }

        public string poNo { get; set; }
        public int? poEntryId { get; set; }
        public string itemNumber { get; set; }
        public string itemName { get; set; }
        public string itemModel { get; set; }
        public decimal? poQty { get; set; }
        public decimal? sendQty { get; set; }
        public string prNo { get; set; }
        public string unitName { get; set; }
        public DateTime? poDate { get; set; }

        public int? userId { get; set; }
        public string supplierNumber { get; set; }

    }

}