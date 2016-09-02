using System;
using System.Threading;
using System.Windows.Forms;
using Com.Lmax.Api;
using Com.Lmax.Api.OrderBook;
using System.Globalization;

namespace lmax_bot_v2
{
    public partial class Form1 : Form
    {

        public string DEMO_URL = "https://web-order.london-demo.lmax.com";
        public string USERNAME = "username";
        public string PASSWORD = "password";

        private LmaxApi lmaxApi = null;
        private ISession _session;
        private const long GBP_USD_INSTRUMENT_ID = 4002;
        private const long EUR_USD_INSTRUMENT_ID = 4001;

        private Thread nit = null;
        private BinaryWS bws = null;
        private long timer = 0;

        public Form1()
        {
            InitializeComponent();
            bws = new BinaryWS();
            textBox1.Text = USERNAME;
            textBox2.Text = PASSWORD;
            textBox3.Text = "0,00010";
            textBox4.Text = bws.API_KEY;
            textBox5.Text = bws.PAYMENT_AMNT.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bws.PAYMENT_AMNT = int.Parse(textBox5.Text);
            bws.API_KEY = textBox4.Text;
            button1.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            textBox5.Enabled = false;
            timer1.Start();
            nit = new Thread(startThread);
            nit.Start();

            HelperClass.addLog("Preparing to connect to: " + bws.url);
            bws.init();

            Console.ReadLine();
        }

        private void startThread()
        {
            Thread.CurrentThread.IsBackground = true;
            lmaxApi = new LmaxApi(DEMO_URL);
            LoginRequest loginRequest = new LoginRequest(textBox1.Text, textBox2.Text, ProductType.CFD_DEMO);
            lmaxApi.Login(loginRequest, LoginCallback, FailureCallback("log in"));
        }

        private void LoginCallback(ISession session)
        {
            Invoke((MethodInvoker)(() => listBox1.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + "Logged in, account ID: " + session.AccountDetails.AccountId)));
            _session = session;

            session.MarketDataChanged += MarketDataUpdate;
            session.Subscribe(new OrderBookSubscriptionRequest(EUR_USD_INSTRUMENT_ID),
                    () => Invoke((MethodInvoker)(() => listBox1.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + "Successful subscription"))),
                    failureResponse => Invoke((MethodInvoker)(() => listBox1.Items.Add(string.Format("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + "Failed to subscribe: {0}", failureResponse)))));
            session.Start();
        }

        private static OnFailure FailureCallback(string failedFunction)
        {
            return failureResponse => Console.Error.WriteLine("Failed to " + failedFunction + " due to: " + failureResponse.Message);
        }

        public void MarketDataUpdate(OrderBookEvent orderBookEvent)
        {
            Invoke((MethodInvoker)(() => label18.Text = "$ " + HelperClass.balance.ToString()));

            if (bws.lastID != null && bws.lastEPOCH != null && bws.lastQUOTE != null && bws.lastQUOTE[0] > 0 && bws.active_trading)
            {
                decimal lmax_diff = (orderBookEvent.ValuationAskPrice + orderBookEvent.ValuationBidPrice) / 2; // LMAX(ASK+BID)/2  
                decimal binary = bws.lastQUOTE[0]; // 1.11111
                decimal diff_input = decimal.Parse(textBox3.Text); // 0.00010
                decimal lmax_bin = (lmax_diff - binary); // 
                decimal bin_lmax = (binary - lmax_diff); //

                Invoke((MethodInvoker)(() => label8.Text = lmax_diff.ToString()));
                Invoke((MethodInvoker)(() => label10.Text = binary.ToString()));
                Invoke((MethodInvoker)(() => label11.Text = lmax_bin.ToString()));

                if (DateTime.Now > HelperClass.delayTime)
                {
                    if (lmax_diff > diff_input) // 
                    {
                        var buyOrder = "{\"parameters\": { \"amount\": \"" + bws.PAYMENT_AMNT + "\", \"basis\": \"stake\", \"contract_type\": \"CALL\", \"currency\": \"USD\", \"duration\": \"5\", \"duration_unit\": \"t\", \"symbol\": \"frxEURUSD\" }, \"buy\":\"" + bws.lastID[0] + "\", \"price\":\"" + int.Parse(textBox5.Text).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "\"}";
                        var sellOrder = "{\"parameters\": { \"amount\": \"" + bws.PAYMENT_AMNT + "\", \"basis\": \"stake\", \"contract_type\": \"PUT\", \"currency\": \"USD\", \"duration\": \"5\", \"duration_unit\": \"t\", \"symbol\": \"frxEURUSD\" }, \"buy\":\"" + bws.lastID[0] + "\", \"price\":\"" + int.Parse(textBox5.Text).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "\"}";

                        if (lmax_bin > diff_input) // 0.00020 > 0.00010
                        {
                            bws.websocket.Send(buyOrder);
                            HelperClass.addDelay(60);
                            HelperClass.addLog("ATTEMPTING CALL ORDER @ " + bws.lastQUOTE[0] + ", Delaying 60s...");
                            Invoke((MethodInvoker)(() => dataGridView1.Rows.Add(lmax_diff, binary, lmax_bin, "CALL", "$" + bws.PAYMENT_AMNT, DateTime.Now, bws.lastID[0])));
                            Invoke((MethodInvoker)(() => dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1));
                            Console.WriteLine("CALL @ LMAX: " + lmax_diff + " Binary: " + binary + " Diff: " + lmax_bin);
                        }
                        else if (bin_lmax > diff_input) // -0.00020 > 0.00010
                        {
                            bws.websocket.Send(sellOrder);
                            HelperClass.addDelay(60);
                            HelperClass.addLog("ATTEMPTING PUT ORDER @ " + bws.lastQUOTE[0] + ", Delaying 60s...");
                            Invoke((MethodInvoker)(() => dataGridView1.Rows.Add(lmax_diff, binary, lmax_bin, "PUT", "$" + bws.PAYMENT_AMNT, DateTime.Now, bws.lastID[0])));
                            Invoke((MethodInvoker)(() => dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1));
                            Console.WriteLine("PUT @ LMAX: " + lmax_diff + " Binary: " + binary + " Diff: " + bin_lmax);
                        }
                    }
                }
            }

            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + orderBookEvent);
            Invoke((MethodInvoker)(() => listBox1.Refresh()));
            Invoke((MethodInvoker)(() => listBox1.TopIndex = listBox1.Items.Count - 1));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer++;
            TimeSpan t = TimeSpan.FromSeconds(timer);
            label14.Text = new DateTime(t.Ticks).ToString("HH:mm:ss");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (HelperClass.logs.Count > 0)
            {
                foreach (string item in HelperClass.logs)
                {
                    listBox2.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + item);
                }
                listBox2.TopIndex = listBox2.Items.Count - 1;
                HelperClass.logs.Clear();
            }

            if (HelperClass.gridlogs.Count > 0)
            {
                foreach (string[] item in HelperClass.gridlogs)
                {
                    dataGridView2.Rows.Add(item[0], item[1], item[2], item[3], item[4], item[5], item[6], item[7], item[8], item[9], item[10]);
                }
                dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.RowCount - 1;
                HelperClass.gridlogs.Clear();
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            int secondsToWait = HelperClass.delayTimeInt;
            DateTime startTime = HelperClass.delayTime;

            int elapsedSeconds = ((int)(DateTime.Now - startTime).TotalSeconds);
            int remainingSeconds = secondsToWait - elapsedSeconds;
            if (HelperClass.delayTime > DateTime.Now)
            {
                label16.Text = string.Format("{0}s", remainingSeconds - secondsToWait);
            }
            else
            {
                label16.Text = "0s";
            }
        }

    }
}
