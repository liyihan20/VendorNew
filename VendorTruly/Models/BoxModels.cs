using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorTruly.Models
{
    public class BoxModels
    {
        /// <summary>
        /// 外箱实体
        /// </summary>
        public OuterBoxes oBox { get; set; }

        /// <summary>
        /// 关联的po信息
        /// </summary>
        public List<OuterBoxPOs> poList { get; set; }

        /// <summary>
        /// 关联的PO信息，整合成字符串，格式：#订单号[行号]:数量;#订单号[行号]:数量;
        /// </summary>
        public string poInfos { get; set; }

        /// <summary>
        /// 内箱信息
        /// </summary>
        public List<InneBoxes> children { get; set; }
    }

    public class SearchBoxParams
    {
        public int page { get; set; }
        public int rows { get; set; }
        public string id { get; set; }
        public DateTime beginDate { get; set; }
        public DateTime endDate { get; set; }
        public string hasUsed { get; set; }
        public string billNo { get; set; }
        public string poNo { get; set; }
        public string itemInfo { get; set; }
        public string boxNumber { get; set; }
        public string account { get; set; }
        public string userName { get; set; }
    }

    public class K3POs4BoxModel
    {
        public string account { get; set; }
        public string bill_type { get; set; }
        public int po_id { get; set; }
        public int po_entry_id { get; set; }
        public DateTime po_date { get; set; }
        public string po_number { get; set; }
        public string department_name { get; set; }
        public string supplier_number { get; set; }
        public string supplier_name { get; set; }
        public string trade_type_name { get; set; }
        public string mat_order_name { get; set; }
        public string mat_order_number { get; set; }
        public string pr_number { get; set; }
        public string item_number { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string unit_name { get; set; }
        public string unit_number { get; set; }
        public decimal po_qty { get; set; }
        public decimal realte_qty { get; set; }
        public decimal tax_price { get; set; }
        public decimal can_make_box_qty { get; set; } //不会一开始从k3服务器读取
        public string id_field { get; set; }
    }

    public class NotFinishBoxQty
    {
        public int poId { get; set; }
        public int poEntryId { get; set; }
        public decimal? qty { get; set; }
    }

    public class InnerBoxExtraModel
    {
        public InneBoxes ib { get; set; }
        public InnerBoxesExtra extra { get; set; }
        public string hasRelated { get; set; }
        public string outerBoxNumber { get; set; }
        public string hasUsed { get; set; }
    }

    public class SearchInnerBoxExtraParam
    {
        public int page { get; set; }
        public int rows { get; set; }
        public DateTime beginDate { get; set; }
        public DateTime endDate { get; set; }
        public string hasUsed { get; set; }
        public string hasRelated { get; set; }
        public string outerBoxNumber { get; set; }
        public string itemInfo { get; set; }
        public string innerBoxNumber { get; set; }
        public string account { get; set; }
        public string userName { get; set; }
    }

    public class NotRelatedInnerBox
    {
        public int inner_box_id { get; set; }
        public string box_number { get; set; }
        public int? pack_num { get; set; }
        public decimal? every_qty { get; set; }
        public string item_number { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string brand { get; set; }
        public string batch { get; set; }
        public string rohs { get; set; }
        public DateTime? produce_date { get; set; }
        public int? safe_period { get; set; }
        public string made_in { get; set; }
        public string made_by { get; set; }
        public DateTime? package_date { get; set; }
        public string keep_condition { get; set; }
        public string produce_circle { get; set; }
        public DateTime? expire_date { get; set; }

    }

}