using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorNew.Models
{
    public class BoxAndPoModels
    {
        public OuterBoxes box { get; set; }
        public List<OuterBoxPOs> pos { get; set; }
    }

    public class supplierInfo
    {
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
}