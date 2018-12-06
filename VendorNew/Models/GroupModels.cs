using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VendorNew.Models
{
    public class GroupUsersModel
    {
        public int group_user_id { get; set; }
        public string user_name { get; set; }
        public string real_name { get; set; }
    }

    public class GroupAuthModel
    {
        public int group_auth_id { get; set; }
        public decimal? auth_number { get; set; }
        public string auth_name { get; set; }
    }

    public class GroupAndAllMembers
    {
        public int group_id { get; set; }
        public string name { get; set; }
        public IQueryable<string> allMembers { get; set; }
    }

}