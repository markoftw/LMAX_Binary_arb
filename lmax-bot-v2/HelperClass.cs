using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lmax_bot_v2
{
    class HelperClass
    {
        //public static ListBox info = null;
        public static List<string> logs = new List<string>();
        public static List<string[]> gridlogs = new List<string[]>();
        public static DateTime delayTime;
        public static int delayTimeInt;
        public static double balance = 0.00;

        public static int LMAX_TICKS = 0;
        public static int BINARY_TICKS = 0;
        public static DateTime LMAX_TIME;
        public static DateTime BINARY_TIME;

        public static void addLog(string msg)
        {
            logs.Add(msg);
            //Console.WriteLine(msg);
            //info.Items.Add(msg);
            //info.TopIndex = info.Items.Count - 1;
        }

        public static void addDelay(int seconds)
        {
            delayTime = DateTime.Now.AddSeconds(seconds);
            delayTimeInt = seconds;
        }

        public static void setBalance(double b)
        {
            balance = b;
        }

        public static void addToGrid(string price, string order, string type, string balance, string shortcode, string c_id, string start, string trans_id, string purch, string buy_price, string payout)
        {
            gridlogs.Add(new string[] { price, order, type, balance, shortcode, c_id, start, trans_id, purch, buy_price, payout });
        }
    }
}
