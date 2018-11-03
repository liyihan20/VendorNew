﻿using VendorNew.Models;

namespace VendorNew.Services
{
    public class BaseSv
    {
        protected VendorNDBDataContext db;

        public BaseSv()
        {
            if (db == null) db = new VendorNDBDataContext();
        }

        public void Wlog(EventLog log)
        {
            db.EventLog.InsertOnSubmit(log);
            db.SubmitChanges();            
        }
    }
}