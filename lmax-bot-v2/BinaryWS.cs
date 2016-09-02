using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocket4Net;

namespace lmax_bot_v2
{
    class BinaryWS
    {

        public string url = "wss://ws.binaryws.com/websockets/v3";
        public WebSocket websocket = null;
        public string API_KEY = "CHANGEME";
        public int PAYMENT_AMNT = 20;

        public DataGridView grid = null;
        public bool active_trading = true;

        public int[] lastEPOCH = null;
        public decimal[] lastQUOTE = null;
        public string[] lastID = null;
        public DateTime lastTICK;

        public TextBox put = null;
        public TextBox put2 = null;
        public TextBox call = null;
        public TextBox call2 = null;

        public void init()
        {
            websocket = new WebSocket(url);
            websocket.Opened += new EventHandler(websocket_Opened);
            websocket.Error += websocket_Error;
            websocket.Closed += new EventHandler(websocket_Closed);
            websocket.MessageReceived += websocket_MessageReceived;
            websocket.Open();
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            HelperClass.addLog("Connection established!");
            string auth_key = "{\"authorize\": \"" + API_KEY + "\"}";
            string dataPUT = "{\"subscribe\": 1, \"proposal\": 1,\"amount\": \"" + PAYMENT_AMNT + "\", \"basis\": \"stake\", \"contract_type\": \"PUT\", \"currency\": \"USD\", \"duration\": \"5\", \"duration_unit\": \"t\", \"symbol\": \"frxEURUSD\"}";
            string dataCALL = "{\"subscribe\": 1, \"proposal\": 1,\"amount\": \"" + PAYMENT_AMNT + "\", \"basis\": \"stake\", \"contract_type\": \"CALL\", \"currency\": \"USD\", \"duration\": \"5\", \"duration_unit\": \"t\", \"symbol\": \"frxEURUSD\"}";
            string dataTICK = "{\"ticks\": \"frxEURUSD\"}";

            websocket.Send(auth_key);
            websocket.Send(dataTICK);
            Console.WriteLine("Sent request: " + auth_key);
            Console.WriteLine("Sent request: " + dataTICK);
            Execute();
        }

        public async void Execute()
        {
            string balance = "{\"balance\": \"1\", \"subscribe\": \"1\"}";
            await Task.Delay(3000);
            websocket.Send(balance);
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs msg)
        {
            //HelperClass.addLog(msg.Message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg.Message);

            var json = (JObject)JsonConvert.DeserializeObject(msg.Message);
            var e = json["echo_req"];
            var b = json["buy"];
            var p = json["proposal"];
            var t = json["tick"];
            if (json["authorize"] != null)
            {
                HelperClass.addLog("Authenticated! Delaying 15s");
                HelperClass.setBalance((double)json["authorize"]["balance"]);
                HelperClass.addDelay(15);
                return;
            }
            else if (json["balance"] != null)
            {
                HelperClass.setBalance((double)json["balance"]["balance"]);
            }
            else if (json["error"] != null)
            {
                HelperClass.addLog("Error retrieving data... Delaying 60s");
                HelperClass.addLog("Message:" + (string)json["error"]["message"]);
                HelperClass.addDelay(60);
                //active_trading = false;
                return;
            }
            else if (json["msg_type"] != null && json["buy"] != null)
            {
                int placed_order = (int)JsonConvert.DeserializeObject(b["start_time"].ToString(), typeof(int));
                TimeSpan placed = (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(placed_order).ToLocalTime().TimeOfDay;

                HelperClass.addToGrid((string)e["price"], (string)e["buy"], (string)json["msg_type"], (string)b["balance_after"], (string)b["shortcode"], (string)b["contract_id"], placed.ToString(), (string)b["transaction_id"], placed.ToString(), (string)b["buy_price"], (string)b["payout"]);
            }

            if (lastEPOCH == null || lastQUOTE == null || lastID == null)
            {
                lastEPOCH = new int[2];
                lastQUOTE = new decimal[2];
                lastID = new string[2];
            }

            if ((string)e["ticks"] == "frxEURUSD")
            {
                lastEPOCH[1] = lastEPOCH[0];
                lastEPOCH[0] = (int)t["epoch"];
                lastQUOTE[1] = lastQUOTE[0];
                lastQUOTE[0] = (decimal)t["quote"];
                lastID[1] = lastID[0];
                lastID[0] = (string)t["id"];
                lastTICK = DateTime.Now;
                HelperClass.BINARY_TIME = DateTime.Now;
                HelperClass.BINARY_TICKS++;
            }

        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            HelperClass.addLog("Websocket closed");
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Websocket closed");
        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            HelperClass.addLog("Error thrown trying to open websocket : " + e.Exception.Message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Error thrown trying to open websocket : " + e.Exception.Message);
        }

    }
}
