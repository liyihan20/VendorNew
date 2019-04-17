using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorTruly.Models
{
    public class BoxAndPoModels
    {
        public OuterBoxes box { get; set; }
        public OuterBoxPOs po { get; set; }
    }

    public class supplierInfo
    {
        public string supplierNumber { get; set; }
        public string supplierName { get; set; }
        public string supplierAddr { get; set; }
        public string supplierAttn { get; set; }
        public string supplierPhone { get; set; }
    }

    public class PrintApplyModels
    {
        public DRBills h { get; set; }
        public List<DRBillDetails> es { get; set; }
        public List<BoxAndPoModels> boxAndPos { get; set; }
        public supplierInfo supplierInfo { get; set; }

    }

    public class PrintOuterBoxModels
    {
        public string companyName { get; set; }
        public string rohs { get; set; }
        public string qrcodeContent { get; set; }
        public string poNumber { get; set; }
        public string itemNumber { get; set; }
        public string qtyAndUnit { get; set; }
        public string batchNo { get; set; }
        public string madeBy { get; set; }
        public string tradeTypeName { get; set; }
        public string boxNumber { get; set; }
        public string itemName { get; set; }
        public string madeIn { get; set; }
        public string itemModel { get; set; }
        public string produceDate { get; set; }
        public string expireDate { get; set; }
        public string produceCircle { get; set; }
        public string brand { get; set; }
        public string grossWeight { get; set; }
        public string netWeight { get; set; }
        public string keepCondition { get; set; }
        public string supplierName { get; set; }
    }

    public class PrintInnerBoxModel
    {
        public string qrcodeContent { get; set; }
        public string itemNumber { get; set; }
        public string qtyAndUnit { get; set; }
        public string rohs { get; set; }
        public string batchNo { get; set; }
        public string madeBy { get; set; }
        public string tradeTypeName { get; set; }
        public string boxNumber { get; set; }
        public string itemName { get; set; }
        public string brand { get; set; }
        public string produceDate { get; set; }
        public string expireDate { get; set; }
        public string keepCondition { get; set; }
        public string supplierName { get; set; }
        public string itemModel { get; set; }
        public string outerBoxNumber { get; set; }

    }

    public class DayNumChartModel
    {
        public DayNumChartModel()
        {
            dayList = new List<string>();
            applyNumList = new List<int>();
            oBoxNumList = new List<int>();
            iBoxNumList = new List<int>();
        }

        public List<string> dayList { get; set; }
        public List<int> applyNumList { get; set; }
        public List<int> oBoxNumList { get; set; }
        public List<int> iBoxNumList { get; set; }
    }

    public class SupplierAndApplyNumModel
    {
        public string supplierName { get; set; }
        public int? num { get; set; }
    }

    


}