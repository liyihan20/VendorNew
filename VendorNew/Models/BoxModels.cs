using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorNew.Models
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
}