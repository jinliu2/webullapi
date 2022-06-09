using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Reflection;
using Newtonsoft.Json;
using System.Threading;


namespace WeBullAPI {

    static class Constants {
        public const string CREDENTIAL_FILE = @"webull_credentials.txt"; // create a text file with this inside: email@address[space]password
        public const string LOG_PATH = @"..\..\..\log\";
        public const bool DEBUG = true;
    }

    public static class HttpWebRequestExtensions {

        static string[] RestrictedHeaders = new string[] {
            "Accept",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Keep-Alive",
            "Proxy-Connection",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent"
        };

        static Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        static HttpWebRequestExtensions() {
            Type type = typeof(HttpWebRequest);
            foreach (string header in RestrictedHeaders) {
                string propertyName = header.Replace("-", "");
                PropertyInfo headerProperty = type.GetProperty(propertyName);
                HeaderProperties[header] = headerProperty;
            }
        }

        public static void SetRawHeader(this HttpWebRequest request, string name, string value) {
            if (HeaderProperties.ContainsKey(name)) {
                PropertyInfo property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                    property.SetValue(request, DateTime.Parse(value), null);
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(request, Boolean.Parse(value), null);
                else if (property.PropertyType == typeof(long))
                    property.SetValue(request, Int64.Parse(value), null);
                else
                    property.SetValue(request, value, null);
            } else {
                request.Headers[name] = value;
            }
        }
    }

    public class TablePrinter {

        private readonly string[] titles;
        private readonly List<int> lengths;
        private readonly List<string[]> rows = new List<string[]>();

        public TablePrinter(params string[] titles) {
            this.titles = titles;
            lengths = titles.Select(t => t.Length).ToList();
        }

        public void AddRow(params object[] row) {
            if (row.Length != titles.Length) {
                throw new Exception($"Added row length [{row.Length}] is not equal to title row length [{titles.Length}]");
            }
            rows.Add(row.Select(o => o.ToString()).ToArray());
            for (int i = 0; i < titles.Length; i++) {
                if (rows.Last()[i].Length > lengths[i]) {
                    lengths[i] = rows.Last()[i].Length;
                }
            }
        }

        public void Print() {
            lengths.ForEach(l => Console.Write("+-" + new string('-', l) + '-'));
            Console.WriteLine("+");

            string line = "";
            for (int i = 0; i < titles.Length; i++) {
                line += "| " + titles[i].PadRight(lengths[i]) + ' ';
            }
            Console.WriteLine(line + "|");
            lengths.ForEach(l => Console.Write("+-" + new string('-', l) + '-'));
            Console.WriteLine("+");

            foreach (var row in rows) {
                line = "";
                for (int i = 0; i < row.Length; i++) {
                    if (int.TryParse(row[i], out int n)) {
                        line += "| " + row[i].PadLeft(lengths[i]) + ' ';
                    } else {
                        line += "| " + row[i].PadRight(lengths[i]) + ' ';
                    }
                }
                Console.WriteLine(line + "|");
            }

            lengths.ForEach(l => Console.Write("+-" + new string('-', l) + '-'));
            Console.WriteLine("+");
        }
    }

    public class UserPass {
        public string user { get; set; }
        public string pass { get; set; }
    }


    public class webull {

        public static Dictionary<string, string> sess;
        public static WebHeaderCollection headers;
        public static Queue<double> movingAvg;
        public static string curtime;
        public static string logfile;
        public static double rt_rsi_12ma = 0;
        public static double rt_rsi = 0;
        public static double rt_macd = 0;
        public static double rt_signal = 0;
        public static double rt_histogram = 0;
        public static ConsoleColor dkgn = ConsoleColor.DarkGreen;
        public static ConsoleColor whte = ConsoleColor.White;
        public static ConsoleColor dkbl = ConsoleColor.DarkBlue;
        public static ConsoleColor dkmg = ConsoleColor.DarkMagenta;
        public static ConsoleColor dkrd = ConsoleColor.DarkRed;

        public static void initialize() {

            headers = new WebHeaderCollection();
            headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:99.0) Gecko/20100101 Firefox/99.0");
            headers.Add("Accept", "*/*");
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Accept-Language", "en-US,en;q=0.5");
            headers.Add("Content-Type", "application/json");
            headers.Add("platform", "web");
            headers.Add("hl", "en");
            headers.Add("os", "web");
            headers.Add("osv", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:99.0) Gecko/20100101 Firefox/99.0");
            headers.Add("app", "global");
            headers.Add("appid", "webull-webapp");
            headers.Add("ver", "3.39.18");
            headers.Add("lzone", "dc_core_r001");
            headers.Add("ph", "MacOS Firefox");
            headers.Add("locale", "eng");
            headers.Add("device-type", "Web");
            headers.Add("did", _get_did(""));

            //sessions
            webull.sess = new Dictionary<string, string>();
            sess.Add("account_id", "");
            sess.Add("trade_token", "");
            sess.Add("access_token", "");
            sess.Add("refresh_token", "");
            sess.Add("token_expire", "");
            sess.Add("uuid", "");

            //miscellaenous
            sess.Add("did", _get_did(""));
            sess.Add("region_code", "6");
            sess.Add("zone_var", "dc_core_r001");
            sess.Add("timeout", "15");
        }

        public static UserPass GetCredential() {
            UserPass up = new UserPass();
            string cred = String.Empty;
            string filename = Constants.CREDENTIAL_FILE;
            if (File.Exists(filename)) {
                cred = File.ReadAllText(filename);
            }
            if (cred.Contains(" ")) {
                up.user = cred.Split(' ')[0];
                up.pass = cred.Split(' ')[1];
            } else {
                Console.WriteLine("Wrong credential format.");
                return up;
            }
            return up;
        }

        public static string CreateRequest(string type, string url, WebHeaderCollection hdrCollections, object data = null, List<string> param = null) {

            string result = String.Empty;
            if (param != null) {
                url += ("?" + String.Join("&", param.ToArray()));
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            foreach (string key in hdrCollections) {
                request.SetRawHeader(key, hdrCollections[key]);
            }
            request.ContentType = "application/json";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = type;
            if (type.Equals("POST")) {
                using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                    string json = new JavaScriptSerializer().Serialize(data);
                    streamWriter.Write(json);
                }
            }
            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        public static dynamic get_quote(string stock = null, string tId = null) {
            dynamic result;
            var headers = build_req_headers(false, false, true);
            if (String.IsNullOrEmpty(stock) && String.IsNullOrEmpty(tId)) {
                Console.WriteLine("Must provide a stock symbol or a stock id");
                return null;
            }
            if (!String.IsNullOrEmpty(stock) && String.IsNullOrEmpty(tId)) {
                try {
                    tId = get_ticker(stock);
                } catch (Exception ex) {
                    Console.WriteLine("Could not find ticker for stock " + stock + ": " + ex.ToString());
                }
            }
            result = JsonConvert.DeserializeObject(CreateRequest("GET", endpoints.quotes(tId), headers));
            return result;
        }

        public static string get_ticker(string stock = "") {
            var headers = build_req_headers(false, false, true);
            string ticker_id = String.Empty;
            if (!String.IsNullOrEmpty(stock)) {
                dynamic result = JsonConvert.DeserializeObject(CreateRequest("GET", endpoints.stock_id(stock, sess["region_code"]), headers));
                if (result.ContainsKey("data")) {
                    if (result.data[0].ContainsKey("symbol")) {
                        if (result.data[0].symbol.ToString().Equals(stock)) {
                            ticker_id = result.data[0].tickerId.ToString();
                        }
                    }
                } else {
                    Console.WriteLine("TickerId could not be found for stock " + stock);
                    return null;
                }
            } else {
                Console.WriteLine("Stock symbol is required");
                return null;
            }
            return ticker_id;
        }

        public static string CreateMD5(string input) {
            using (MD5 md5 = MD5.Create()) {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        //
        //  Makes a unique device id from a random uuid (uuid.uuid4).
        //  if the pickle file doesn't exist, this func will generate a random 32 character hex string
        //  uuid and save it in a pickle file for future use. if the file already exists it will
        //  load the pickle file to reuse the did. Having a unique did appears to be very important
        //  for the MQTT web socket protocol
        //
        //  path: path to did.bin. For example _get_did('cache') will search for cache/did.bin instead.
        //  :return: hex string of a 32 digit uuid
        //
        public static string _get_did(string path) {
            string did = String.Empty;
            string filename = "did.bin";
            if (!String.IsNullOrEmpty(path)) {
                filename = Path.Combine(path, filename);
            }
            if (File.Exists(filename)) {
                did = File.ReadAllText(filename);
            } else {
                did = Guid.NewGuid().ToString("N");
                File.WriteAllText(filename, did);
            }
            return did;
        }

        //
        //  Build default set of header params
        //
        public static WebHeaderCollection build_req_headers(bool include_trade_token, bool include_time, bool include_zone_var) {
            WebHeaderCollection headers = webull.headers;
            headers["did"] = sess["did"];
            headers["access_token"] = sess["access_token"];
            if (include_trade_token) {
                headers["t_token"] = sess["trade_token"];
            }
            if (include_time) {
                headers["t_time"] = Math.Round(((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds * 1000), 0).ToString();
            }
            if (include_zone_var) {
                headers["lzone"] = sess["zone_var"];
            }
            return headers;
        }

        public static string get_mfa(string username) {
            string account_type = "2";
            var data = new Dictionary<object, object> {
                    { "account", username },
                    { "accountType", account_type },
                    { "codeType", Convert.ToInt32(5) }
                };
            var response = CreateRequest("POST", endpoints.get_mfa(), headers, data);
            return response;
        }


        public static object get_security(string username) {
            var account_type = "2";
            // seems like webull has a bug/stability issue here:
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var response = CreateRequest("GET", endpoints.get_security(username, account_type, sess["region_code"], "PRODUCT_LOGIN", time.ToString(), "0"), headers);
            return response;
        }


        public static string get_account_id() {
            var headers = build_req_headers(false, false, true);
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("GET", endpoints.account_id(), headers));
            if (result.ContainsKey("success") && result.ContainsKey("data")) {
                if (result["data"].Count > 0) {
                    if (result["data"][0].ContainsKey("rzone") &&
                        result["data"][0].ContainsKey("secAccountId")) {
                        sess["zone_var"] = result["data"][0]["rzone"];
                        return result["data"][0]["secAccountId"].ToString();
                    } else return null;
                } else return null;
            } else {
                return null;
            }
        }


        //
        //  Login with email or phone number
        //
        //  phone numbers must be a str in the following form
        //  US '+1-XXXXXXX'
        //  CH '+86-XXXXXXXXXXX'
        //
        public static dynamic login(string username = "", string password = "", string device_name = "", string mfa = "", string question_id = "",
                             string question_answer = "", bool save_token = false, string token_path = null) {

            WebHeaderCollection hdrs;
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)) {
                Console.WriteLine("username or password is empty");
            }

            // with webull md5 hash salted
            password = Encoding.Default.GetString(Encoding.ASCII.GetBytes("wl_app-a&b@!423^" + password));
            string md5_hash = CreateMD5(password);
            //string account_type = get_account_type(username);
            string account_type = "2";
            if (device_name == "") {
                device_name = "default_string";
            }
            var data = new Dictionary<string, object> {
                { "account", username },
                { "accountType", account_type.ToString() },
                { "deviceId", sess["did"] },
                { "deviceName", device_name },
                { "grade", 1 },
                { "pwd", md5_hash },
                { "regionId", sess["region_code"] }
            };
            if (!String.IsNullOrEmpty(mfa)) {
                if (!data.ContainsKey("extInfo")) {
                    data.Add("extInfo", new Dictionary<string, object> {
                                        { "codeAccountType", account_type },
                                        { "verificationCode", mfa } });
                } else {
                    data["extInfo"] = new Dictionary<string, object> {
                                        { "codeAccountType", account_type },
                                        { "verificationCode", mfa } };
                }
                hdrs = build_req_headers(false, false, true);
            } else {
                hdrs = headers;
            }
            if (!String.IsNullOrEmpty(question_id) && !String.IsNullOrEmpty(question_answer)) {
                data["accessQuestions"] = "[{\"questionId\":\"" + question_id.ToString() + "\", \"answer\":\"" + question_answer.ToString() + "\"}]";
            }
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("POST", endpoints.login(), hdrs, data));
            if (result.ContainsKey("accessToken")) {
                sess["access_token"] = result["accessToken"].ToString();
                sess["refresh_token"] = result["refreshToken"].ToString();
                sess["token_expire"] = result["tokenExpireTime"].ToString();
                sess["uuid"] = result["uuid"].ToString();
                sess["account_id"] = get_account_id();
            }
            return result;
        }


        // 
        //         get important details of account, positions, portfolio stance...etc
        //         
        public static dynamic get_account() {
            var headers = build_req_headers(false, false, true);
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("GET", endpoints.account(get_account_id()), headers));
            return result;
        }


        // 
        //         get options and returns a dict of options contracts
        //         params:
        //             stock: symbol
        //             count: -1
        //             includeWeekly: 0 or 1
        //             direction: all, calls, puts
        //             expireDate: contract expire date
        //             queryAll: 0
        //         
        public static dynamic get_options(string stock = null, int count = -1, int includeWeekly = 1,
                                            string direction = "all", string expireDate = null, int queryAll = 0) {
            var headers = build_req_headers(false, false, true);
            var data = new Dictionary<string, object> {
                       { "count", count },
                       { "direction", direction },
                       { "tickerId", get_ticker(stock) }};
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("POST", endpoints.options_exp_dat_new(), headers, data));
            foreach (var entry in result["expireDateList"]) {
                if (entry.ContainsKey("from") && entry.ContainsKey("data")) {
                    if (entry.from.ContainsKey("date")) {
                        if (expireDate != null) {
                            if (entry.from.date.ToString().Equals(expireDate)) {
                                return entry["data"];
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static dynamic get_option_quote(string stock = null, string tId = null, string optionId = null) {
            if (String.IsNullOrEmpty(stock) && String.IsNullOrEmpty(tId)) {
                Console.WriteLine("Must provide a stock symbol or a stock id");
            }
            if (!String.IsNullOrEmpty(stock) && String.IsNullOrEmpty(tId)) {
                try {
                    tId = get_ticker(stock).ToString();
                } catch (Exception ex) {
                    Console.WriteLine("Could not find ticker for stock " + ex.ToString());
                }
            }
            var headers = build_req_headers(false, false, true);
            var p = new Dictionary<string, object> {
                   { "tickerId", tId },
                   { "derivativeIds", optionId }};
            List<string> queryString = new List<string>();
            foreach (var q in p) {
                queryString.Add(q.Key + "=" + q.Value);
            }

            return JsonConvert.DeserializeObject( CreateRequest("GET", endpoints.option_quotes(), headers, null, queryString) );
        }

        public static List<string> get_options_expiration_dates(string stock = null, int count = -1) {
            List<string> ex_dates = new List<string>();
            var headers = build_req_headers(false, false, true);
            var data = new Dictionary<string, object> {
                            { "count", count },
                            { "direction", "all" },
                            { "tickerId", get_ticker(stock) }};
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("POST", endpoints.options_exp_dat_new(), headers, data));
            foreach (var r in result["expireDateList"]) {
                if (r.ContainsKey("from")) {
                    if (r.from.ContainsKey("date")) {
                        ex_dates.Add(r.from.date.ToString());
                    }
                }
            }
            return ex_dates;
        }


        // 
        //         get bars returns a pandas dataframe
        //         params:
        //             interval: m1, m5, m15, m30, h1, h2, h4, d1, w1
        //             count: number of bars to return
        //             extendTrading: change to 1 for pre-market and afterhours bars
        //             timeStamp: If epoc timestamp is provided, return bar count up to timestamp. If not set default to current time.
        //         
        public static dynamic get_bars(string stock = null, string tId = null, string interval = "m1",
                                       int count = 1, int extendTrading = 0, string timeStamp = null) {
            var headers = build_req_headers(false, false, true);
            if (String.IsNullOrEmpty(tId) && String.IsNullOrEmpty(stock)) {
                Console.WriteLine("Must provide a stock symbol or a stock id");
            } else if (String.IsNullOrEmpty(tId) && !String.IsNullOrEmpty(stock)) {
                tId = get_ticker(stock);
            }
            var p = new Dictionary<string, object> {
                   { "type", interval },
                   { "count", count },
                   { "extendTrading", extendTrading },
                   { "timestamp", timeStamp }};
            List<string> queryString = new List<string>();
            foreach (var q in p) {
                queryString.Add(q.Key + "=" + q.Value);
            }
            dynamic result = JsonConvert.DeserializeObject(CreateRequest("GET", endpoints.bars(tId), headers, null, queryString));
            return result;
        }

        public static double RSI(double[] closePrices, double interval = 14) {
            double current_rsi, cur_avgGain, cur_gain, cur_avgLoss, cur_loss, sumGain, sumLoss;
            current_rsi = cur_avgGain = cur_gain = cur_avgLoss = cur_loss = sumGain = sumLoss = 0;
            if (closePrices.Count() < interval) return 0;
            bool firstcalc = false;
            for (int i = 1; i < closePrices.Length; i++) {
                double diff = closePrices[i] - closePrices[i - 1];
                if (diff >= 0) {
                    cur_loss = 0;
                    sumGain += diff;
                    cur_gain = diff;
                } else {
                    cur_gain = 0;
                    sumLoss -= diff;
                    cur_loss = -1 * diff;
                }
                if (i >= interval) {
                    if (!firstcalc) {
                        cur_avgGain = sumGain / interval;
                        cur_avgLoss = sumLoss / interval;
                        firstcalc = true;
                    } else {
                        cur_avgGain = (cur_avgGain * (interval - 1) + cur_gain) / interval;
                        cur_avgLoss = (cur_avgLoss * (interval - 1) + cur_loss) / interval;
                    }
                    current_rsi = 100 - (100 / (1 + cur_avgGain / cur_avgLoss));
                }
            }
            return current_rsi;
        }

        public static Dictionary<string, double> MACD(double[] closePrices, double p1 = 12, double p2 = 26, double sig = 9) {
            Dictionary<string, double> result = new Dictionary<string, double>() { { "MACD", 0 }, { "Signal", 0 }, { "Histogram", 0 } };
            if (closePrices.Count() < p2 || p1 >= p2) return result;
            bool firstcalc_p1, firstcalc_p2, firstcalc_sig;
            firstcalc_p1 = firstcalc_p2 = firstcalc_sig = false;
            double cur_total, cur_total_macd, cur_ema_p1, cur_ema_p2;
            cur_total = cur_total_macd = cur_ema_p1 = cur_ema_p2 = 0;
            for (int i = 0; i < closePrices.Length; i++) {
                cur_total += closePrices[i];
                if (i >= p1 - 1) {
                    if (!firstcalc_p1) {
                        cur_ema_p1 = cur_total / p1;
                        firstcalc_p1 = true;
                    } else {
                        cur_ema_p1 = closePrices[i] * (2 / (p1 + 1)) + cur_ema_p1 * (1 - 2 / (p1 + 1));
                    }
                }
                if (i >= p2 - 1) {
                    if (!firstcalc_p2) {
                        cur_ema_p2 = cur_total / p2;
                        firstcalc_p2 = true;
                    } else {
                        cur_ema_p2 = closePrices[i] * (2 / (p2 + 1)) + cur_ema_p2 * (1 - 2 / (p2 + 1));
                    }
                    result["MACD"] = cur_ema_p1 - cur_ema_p2;
                    cur_total_macd += result["MACD"];
                }
                if (i >= (p2 + sig - 1)) {
                    if (!firstcalc_sig) {
                        result["Signal"] = cur_total_macd / sig;
                        firstcalc_sig = true;
                    } else {
                        result["Signal"] = result["MACD"] * (2 / (sig + 1)) + result["Signal"] * (1 - 2 / (sig + 1));
                    }
                    result["Histogram"] = result["MACD"] - result["Signal"];
                }
            }

            return result;
        }

        public static int Main(string[] args) {
            Stopwatch sw;
            logfile = Constants.LOG_PATH + @"webullapi_" + DateTime.Now.ToString("yyyy-MM-dd") + @"_Log.txt";
            sw = Stopwatch.StartNew();
            initialize();
            sw.Stop();
            functions.cc_logstatus("Initialized. Getting user credential.", dkbl, whte, sw.ElapsedTicks / 10000.0);
            sw = Stopwatch.StartNew();
            UserPass cred = GetCredential();
            sw.Stop();
            functions.cc_logstatus("Logging in to Webull API.", dkbl, whte, sw.ElapsedTicks / 10000.0);
            sw = Stopwatch.StartNew();
            if (String.IsNullOrEmpty(cred.user) || String.IsNullOrEmpty(cred.pass)) {
                Console.WriteLine("No login information.");
                return 1;
            }
            dynamic account = login(cred.user, cred.pass);
            dynamic acct_info = get_account();
            // CashBalance: acct_info.accountMembers[1].value
            // Day Buying Power: acct_info.accountMembers[2].value
            // Settled Funds: acct_info.accountMembers[4].value
            // Option Buying Power: acct_info.accountMembers[7].value
            sw.Stop();
            functions.cc_logstatus("Successfully logged in.", dkgn, whte, sw.ElapsedTicks / 10000.0);
            sw = Stopwatch.StartNew();
            string symb, stock_tid, opt_tid, opt_symb;
            symb = stock_tid = opt_tid = opt_symb = String.Empty;
            symb = "AAPL";
            double budget, budget_diff, opt_strike, opt_bid, opt_ask, opt_delta, opt_gamma, opt_impvol, opt_rho, opt_theta, opt_vega;
            budget = budget_diff = opt_strike = opt_bid = opt_ask = opt_delta = opt_gamma = opt_impvol = opt_rho = opt_theta = opt_vega = 0;
            budget = 1.10;
            stock_tid = get_ticker(symb);
            sw.Stop();
            functions.cc_logstatus("AAPL TickerID: " + stock_tid, dkbl, whte, sw.ElapsedTicks / 10000.0);
            movingAvg = new Queue<double>();

            while (true) {
                sw = Stopwatch.StartNew();
                dynamic ohlc = get_bars(symb, stock_tid, "m1", 90);
                sw.Stop();
                functions.cc_logstatus("Retrieved 90 minutes of trading data.", dkbl, whte, sw.ElapsedTicks / 10000.0);
                sw = Stopwatch.StartNew();
                Dictionary<int, double> closedPrices_dict = new Dictionary<int, double>();
                List<double> closePrices = new List<double>();
            
                // reconstruct a dictionary with the key being the timestamp from get_bars
                foreach (var bar in ohlc[0].data) {
                    var o = bar.ToString().Split(',');
                    closedPrices_dict.Add(Convert.ToInt32(o[0]), Convert.ToDouble(o[2]));
                }
            
                // get_bars data from Webull is ordered descending with the most recent data on top
                // to prep the data for stats calculation, order must be reversed to ascending by timestamp
                foreach (var od in closedPrices_dict.OrderBy(t => t.Key)) {
                    closePrices.Add(od.Value);
                }

                sw.Stop();
                functions.cc_logstatus("Reconstructed dictionary order by timestamp.", dkbl, whte, sw.ElapsedTicks / 10000.0);
                sw = Stopwatch.StartNew();
                // get_bars data does not update as frequently as get_quote
                // this step is to replace the most recent data point with the data from get_quote
                dynamic quote = get_quote(symb, stock_tid);
                closePrices[ closePrices.Count-1 ] = quote.close;
                sw.Stop();
                functions.cc_logstatus("Retrieved latest quote on ticker " + symb + ".", dkbl, whte, sw.ElapsedTicks / 10000.0);
                sw = Stopwatch.StartNew();
                double cur_rsi = RSI(closePrices.ToArray());
                rt_rsi = cur_rsi;
                sw.Stop();
                functions.cc_logstatus("AAPL: $" + quote.close + ", Current RSI: " + Math.Round(cur_rsi, 4), dkmg, whte, sw.ElapsedTicks / 10000.0);

                sw = Stopwatch.StartNew();
                movingAvg.Enqueue(rt_rsi);
                if (movingAvg.Count > 12) movingAvg.Dequeue();
                if (movingAvg.Count == 12) {
                    double cur_rsi_12ma = movingAvg.Average();
                    if (cur_rsi_12ma > rt_rsi_12ma) {
                        functions.cc_logstatus("12-Period MA RSI trend up.", dkgn, whte);
                    } else {
                        functions.cc_logstatus("12-Period MA RSI trend down.", dkrd, whte);
                    }
                    rt_rsi_12ma = cur_rsi_12ma;
                }
                sw.Stop();
                functions.cc_logstatus("Current 12-Period 10s RSI MA: " + Math.Round(rt_rsi_12ma, 4), dkmg, whte, sw.ElapsedTicks / 10000.0);
                
                sw = Stopwatch.StartNew();
                Dictionary<string, double> M = MACD(closePrices.ToArray());
                rt_macd = M["MACD"];
                rt_signal = M["Signal"];
                rt_histogram = M["Histogram"];
                sw.Stop();
                functions.cc_logstatus("MACD: " + Math.Round(rt_macd, 4) + ", Signal: " + Math.Round(rt_signal, 4) + ", Histogram: " + Math.Round(rt_histogram, 4), dkmg, whte, sw.ElapsedTicks / 10000.0);
                budget_diff = 999;

                if (String.IsNullOrEmpty(opt_tid)) {
                    sw = Stopwatch.StartNew();
                    dynamic expdate = get_options_expiration_dates(symb);
                    sw.Stop();
                    functions.cc_logstatus("Get options expiration dates list.", dkbl, whte, sw.ElapsedTicks / 10000.0);
                    sw = Stopwatch.StartNew();
                    dynamic opt = get_options(symb, -1, 1, "call", expdate[0]);
                    sw.Stop();
                    functions.cc_logstatus("Get details on all options chain for the soonest expiry date.", dkbl, whte, sw.ElapsedTicks / 10000.0);

                    sw = Stopwatch.StartNew();
                    foreach (var o in opt) {
                        opt_strike = Convert.ToDouble(o.strikePrice);
                        opt_ask = o.ContainsKey("askList") ? Convert.ToDouble(o.askList[0].price) : 0;
                        if (opt_strike < Convert.ToDouble(quote.close) + 10) {
                            if (opt_ask < budget) {
                                if ((budget - opt_ask) < budget_diff) {
                                    budget_diff = budget - opt_ask;
                                    opt_symb = o["symbol"].ToString();
                                    opt_tid = o["tickerId"].ToString();
                                }
                            }
                        }
                    }
                    sw.Stop();
                    functions.cc_logstatus("Found a suitable contract to monitor. (" + opt_symb + ", " + opt_tid + ")", dkbl, whte, sw.ElapsedTicks / 10000.0);
                } else {
                    sw = Stopwatch.StartNew();
                    dynamic oq = get_option_quote(symb, stock_tid, opt_tid).data;
                    if (oq.Count > 0) {
                        oq = oq[0];
                        opt_strike = oq.ContainsKey("strikePrice") ? Convert.ToDouble(oq.strikePrice) : 0;
                        opt_bid = oq.ContainsKey("bidList") ? Convert.ToDouble(oq.bidList[0].price) : 0;
                        opt_ask = oq.ContainsKey("askList") ? Convert.ToDouble(oq.askList[0].price) : 0;
                        opt_delta = oq.ContainsKey("delta") ? Convert.ToDouble(oq.delta) : 0;
                        opt_gamma = oq.ContainsKey("gamma") ? Convert.ToDouble(oq.gamma) : 0;
                        opt_impvol = oq.ContainsKey("impVol") ? Convert.ToDouble(oq.impVol) : 0;
                        opt_rho = oq.ContainsKey("rho") ? Convert.ToDouble(oq.rho) : 0;
                        opt_theta = oq.ContainsKey("theta") ? Convert.ToDouble(oq.theta) : 0;
                        opt_vega = oq.ContainsKey("vega") ? Convert.ToDouble(oq.vega) : 0;
                        var t = new TablePrinter("Strike", "Symbol", "Bid", "Ask", "impVol", "delta", "gamma", "rho", "theta", "vega");
                        t.AddRow(opt_strike, opt_symb, opt_bid, opt_ask, opt_impvol, opt_delta, opt_gamma, opt_rho, opt_theta, opt_vega);
                        t.Print();
                        Console.WriteLine();
                    }
                }
                Thread.Sleep(9000);
            }
            return 0;
        }
    }
}