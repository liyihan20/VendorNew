using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VendorNew.Models;

namespace VendorNew.Utils
{
    public class MyUtils
    {
        //生成随机数列
        public static string CreateValidateNumber(int length)
        {
            //去掉数字0和字母o，小写l和大写I，因为不容易区分
            string Vchar = "1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,m,n,p" +
            ",q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,J,K,L,M,N,P,Q" +
            ",R,S,T,U,V,W,X,Y,Z";

            string[] VcArray = Vchar.Split(new Char[] { ',' });//拆分成数组
            string num = "";

            int temp = -1;//记录上次随机数值，尽量避避免生产几个一样的随机数

            Random rand = new Random();
            //采用一个简单的算法以保证生成随机数的不同
            for (int i = 1; i < length + 1; i++) {
                if (temp != -1) {
                    rand = new Random(i * temp * unchecked((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(VcArray.Length - 1);
                if (temp != -1 && temp == t) {
                    return CreateValidateNumber(length);

                }
                temp = t;
                num += VcArray[t];
            }
            return num;
        }

        public static byte[] CreateValidateGraphic(string validateCode, int width = 72, int height = 26)
        {
            Bitmap image = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(image);
            try {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 50; i++) {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }


                Font font = new Font("Arial", 16, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height),
                 Color.Green, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, random.Next(width / 2 - 10), random.Next(height / 2 - 8));
                //画图片的前景干扰点
                for (int i = 0; i < 200; i++) {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);
                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Jpeg);
                //输出图片流
                return stream.ToArray();
            }
            finally {
                g.Dispose();
                image.Dispose();
            }
        }

        public static string getMD5(string str)
        {
            if (str.Length > 2) {
                str = "Who" + str.Substring(2) + "Are" + str.Substring(0, 2) + "You";
            }
            else {
                str = "Who" + str + "Are" + str + "You";
            }
            return getNormalMD5(str);

        }

        public static string getNormalMD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.Default.GetBytes(str);
            byte[] result = md5.ComputeHash(data);
            String ret = "";
            for (int i = 0; i < result.Length; i++) {
                ret += result[i].ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        //将中文编码为utf-8
        public static string EncodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlEncode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        //将utf-8解码
        public static string DecodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlDecode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        /// <summary>
        /// 使用反射将表单的值设置到数据库对象中，根据字段名
        /// </summary>
        /// <param name="col">表单</param>
        /// <param name="obj">数据库对象</param>
        public static void SetFieldValueToModel(System.Web.Mvc.FormCollection col, object obj)
        {
            foreach (var p in obj.GetType().GetProperties()) {
                string val = col.Get(p.Name);//字段值
                string pType = p.PropertyType.FullName;//数据类型
                if (string.IsNullOrEmpty(val) || val.Equals("null")) continue;
                if (pType.Contains("DateTime")) {
                    DateTime dt;
                    if (DateTime.TryParse(val, out dt)) {
                        p.SetValue(obj, dt, null);
                    }
                }
                else if (pType.Contains("Int32")) {
                    int i;
                    if (int.TryParse(val, out i)) {
                        p.SetValue(obj, i, null);
                    }
                }
                else if (pType.Contains("Decimal")) {
                    decimal dm;
                    if (decimal.TryParse(val, out dm)) {
                        p.SetValue(obj, dm, null);
                    }
                }
                else if (pType.Contains("String")) {
                    p.SetValue(obj, val.Trim(), null);
                }
                else if (pType.Contains("Bool")) {
                    bool bl;
                    if (bool.TryParse(val, out bl)) {
                        p.SetValue(obj, bl, null);
                    }
                }
            }
        }

        public static string GetAppSetting(string key)
        {
            return System.Configuration.ConfigurationManager.AppSettings[key];
        }

        //获取cookie名称
        public static string GetCookieName()
        {
            return GetAppSetting("cookieName");
        }

        //箱号的周和日字符串
        public static string GetBoxDayStr()
        {
            GregorianCalendar gc = new GregorianCalendar();
            int weekOfYear = gc.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            string dow;
            switch (DateTime.Now.DayOfWeek) {
                case DayOfWeek.Monday:
                    dow = "A";
                    break;
                case DayOfWeek.Tuesday:
                    dow = "B";
                    break;
                case DayOfWeek.Wednesday:
                    dow = "C";
                    break;
                case DayOfWeek.Thursday:
                    dow = "D";
                    break;
                case DayOfWeek.Friday:
                    dow = "E";
                    break;
                case DayOfWeek.Saturday:
                    dow = "F";
                    break;
                case DayOfWeek.Sunday:
                    dow = "G";
                    break;
                default:
                    dow = "H";
                    break;
            }
            return weekOfYear.ToString() + dow;
        }

        /// <summary>
        /// 将页码字符串转换成页码数字数组，例如字符串：“8，8，10”，页数：50，即结果为：8，8，10，10，10，4
        /// </summary>
        /// <param name="defaultNumber">不能转换为数字的，就用这个默认页数</param>
        /// <param name="pageNumStr">页码字符串</param>
        /// <param name="totalNum">总页数</param>
        /// <returns></returns>
        public static List<int> GetPageNumberList(int defaultNumber, string pageNumStr, int totalNum)
        {
            List<int> result = new List<int>();
            var numArr = pageNumStr.Split(new char[] { ',', '，' }); //页码字符串数组
            int lastNumber = defaultNumber; //最后一页，页码字符串数字不够用的话，后面的都按照最后一页的页数设定
            for (var i = 0; i < numArr.Length && totalNum > 0; i++) {
                int temp;
                if (!Int32.TryParse(numArr[i], out temp)) {
                    temp = defaultNumber;
                }
                if (totalNum >= temp) {
                    result.Add(temp);
                    totalNum -= temp;
                }
                else {
                    result.Add(totalNum);
                    totalNum = 0;
                }
                if (i == numArr.Length - 1) {
                    lastNumber = temp;
                }
            }
            while (totalNum > 0) {
                if (totalNum >= lastNumber) {
                    result.Add(lastNumber);
                    totalNum -= lastNumber;
                }
                else {
                    result.Add(totalNum);
                    totalNum = 0;
                }
            }

            return result;

        }

        public static CompanyModel GetCurrentCompany(string account)
        {
            return GetAllCompany().Find(c => c.account == account);
        }

        public static List<CompanyModel> GetAllCompany()
        {
            return new List<CompanyModel>()
            {
                new CompanyModel(){
                    account="S",
                    accountName = "信利半导体有限公司",
                    addr = "广东省汕尾市区工业大道信利电子工业城物流总仓",
                    phone = "0660-336788-1234",
                    shortName = "半导体"
                },
                new CompanyModel(){
                    account="O",
                    accountName = "信利光电股份有限公司",
                    addr = "广东省汕尾市区工业大道信利电子工业城物流总仓",
                    phone = "0660-336788-1234",
                    shortName = "光电"
                },
                new CompanyModel(){
                    account="R",
                    accountName = "信利光电仁寿有限公司",
                    addr = "四川省眉山市仁寿县文林工业园陵州大道信利厂区仓运部",
                    phone = "15023771749",
                    shortName = "光电仁寿",
                }
            };
        }

    }
}