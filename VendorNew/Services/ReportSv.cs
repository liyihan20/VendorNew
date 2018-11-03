using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VendorNew.Models;

namespace VendorNew.Services
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
            m.supplierInfo = new ItemSv().GetSupplierInfo(m.h.supplier_number,account);

            return m;
        }
    }
}