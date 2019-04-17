using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorTruly.Models
{    
    /// <summary>
    /// SimpleResultModel 的简写
    /// </summary>
    public class SRM
    {
        public bool suc { get; set; }
        public string msg { get; set; }
        public string extra { get; set; }

        public SRM()
        {
            this.suc = true;
        }
        public SRM(bool _suc, string _msg = "", string _extra = "")
        {
            this.suc = _suc;
            this.msg = _msg;
            this.extra = _extra;
        }

        public SRM(Exception ex)
        {
            this.suc = false;
            this.msg = ex.Message;
        }
    }


    public class MenuItems
    {
        public decimal? number { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string icon { get; set; }
        public string color { get; set; }

    }

    public class IDModel
    {
        public int? interId { get; set; }
        public int? entryId { get; set; }
    }

    public class IntStringModel
    {
        public int id { get; set; }
        public string text { get; set; }
    }

}