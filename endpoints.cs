using System;

namespace WeBullAPI {

    static class urls {

        public const string base_info_url = "https://infoapi.webull.com/api";
        public const string base_options_url = "https://quoteapi.webullbroker.com/api";
        public const string base_options_url_new = "https://quotes-gw.webullfintech.com/api";
        public const string base_options_gw_url = "https://quotes-gw.webullbroker.com/api";
        public const string base_paper_url = "https://act.webullbroker.com/webull-paper-center/api";
        public const string base_quote_url = "https://quoteapi.webullbroker.com/api";
        public const string base_securities_url = "https://securitiesapi.webullbroker.com/api";
        public const string base_trade_url = "https://tradeapi.webullbroker.com/api/trade";
        public const string base_user_url = "https://userapi.webull.com/api";
        public const string base_userbroker_url = "https://userapi.webullbroker.com/api";
        public const string base_ustrade_url = "https://ustrade.webullfinance.com/api";
        public const string base_paperfintech_url = "https://act.webullfintech.com/webull-paper-center/api";
        public const string base_fintech_gw_url = "https://quotes-gw.webullfintech.com/api";
        public const string base_userfintech_url = "https://userapi.webullfintech.com/api";
        public const string base_new_trade_url = "https://trade.webullfintech.com/api";
        public const string base_ustradebroker_url = "https://ustrade.webullbroker.com/api";

    }

    public class endpoints {
        
        public static string account(string account_id) {
            return urls.base_trade_url + @"/v3/home/" + account_id;
        }
        
        public static string account_id() {
            return urls.base_trade_url + @"/account/getSecAccountList/v5";
        }
        
        public static string account_activities(string account_id) {
            return urls.base_ustrade_url + @"/trade/v2/funds/" + account_id + @"/activities";
        }
        
        public static string active_gainers_losers(string direction, string region_code, string rank_type, string num) {
            string url = String.Empty;
            if (direction == "gainer") {
                url = "topGainers";
            } else if (direction == "loser") {
                url = "dropGainers";
            } else {
                url = "topActive";
            }
            return urls.base_fintech_gw_url + @"/wlas/ranking/" + url + "?regionId=" + region_code + " & rankType=" + rank_type + "&pageIndex=1&pageSize=" + num;
        }
        
        public static string add_alert() {
            return urls.base_userbroker_url + @"/user/warning/v2/manage/overlap";
        }
        
        public static string analysis(string stock) {
            return urls.base_securities_url + @"/securities/ticker/v5/analysis/" + stock;
        }
        
        public static string analysis_shortinterest(string stock) {
            return urls.base_securities_url + @"/securities/stock/" + stock + @"/shortInterest";
        }
        
        public static string analysis_institutional_holding(string stock) {
            return urls.base_securities_url + @"/securities/stock/v5/" + stock + @"/institutionalHolding";
        }
        
        public static string analysis_etf_holding(string stock, string has_num, string page_size) {
            return urls.base_securities_url + @"/securities/stock/v5/" + stock + @"/belongEtf?hasNum=" + has_num + "&pageSize=" + page_size;
        }
        
        public static string analysis_capital_flow(string stock, string show_hist) {
            return urls.base_securities_url + @"/wlas/capitalflow/ticker?tickerId=" + stock + "&showHis=" + show_hist;
        }
        
        public static string bars(string stock) {
            return urls.base_quote_url + @"/quote/tickerChartDatas/v5/" + stock;
        }
        
        public static string bars_crypto(string stock) {
            return urls.base_fintech_gw_url + @"/crypto/charts/query?tickerIds=" + stock;
        }
        
        public static string cancel_order(string account_id) {
            return urls.base_ustrade_url + @"/trade/order/" + account_id + @"/cancelStockOrder/";
        }
        
        public static string modify_otoco_orders(string account_id) {
            return urls.base_ustrade_url + @"/trade/v2/corder/stock/modify/" + account_id;
        }
        
        public static string cancel_otoco_orders(string account_id, string combo_id) {
            return urls.base_ustrade_url + @"/trade/v2/corder/stock/cancel/" + account_id + @"/" + combo_id;
        }
        
        public static string check_otoco_orders(string account_id) {
            return urls.base_ustrade_url + @"/trade/v2/corder/stock/check/" + account_id;
        }
        
        public static string place_otoco_orders(string account_id) {
            return urls.base_ustrade_url + @"/trade/v2/corder/stock/place/" + account_id;
        }
        
        public static string dividends(string account_id) {
            return urls.base_trade_url + @"/v2/account/" + account_id + @"/dividends?direct=in";
        }
        
        public static string fundamentals(string stock) {
            return urls.base_securities_url + @"/securities/financial/index/" + stock;
        }
        
        public static string is_tradable(string stock) {
            return urls.base_trade_url + @"/ticker/broker/permissionV2?tickerId=" + stock;
        }
        
        public static string list_alerts() {
            return urls.base_userbroker_url + @"/user/warning/v2/query/tickers";
        }
        
        public static string login() {
            return urls.base_user_url + @"/passport/login/v5/account";
        }
        
        public static string get_mfa() {
            return urls.base_userfintech_url + @"/passport/v2/verificationCode/send";
        }
        
        public static string check_mfa() {
            return urls.base_userfintech_url + @"/passport/v2/verificationCode/checkCode";
        }
        
        public static string get_security (string username, string account_type, string region_code, string @event, string time, string url) {
            if (String.IsNullOrEmpty(url)) { url = "0"; }
            if (url == "1") {
                url = "getPrivacyQuestion";
            } else {
                url = "getSecurityQuestion";
            }
            return urls.base_userfintech_url + @"/user/risk/" + url + "?account=" + username + "&accountType=" + account_type + "&regionId=" + region_code + "&event=" + @event + "&v=" + time;
        }
        
        public static string next_security (string username, string account_type, string region_code, string @event, string time, string url) {
            if (String.IsNullOrEmpty(url)) { url = "0"; }
            if (url == "1") {
                url = "nextPrivacyQuestion";
            } else {
                url = "nextSecurityQuestion";
            }
            return urls.base_userfintech_url + @"/user/risk/" + url + "?account=" + username + "&accountType=" + account_type + "&regionId=" + region_code + "&event=" + @event + "&v= " + time;
        }
        
        public static string check_security() {
            return urls.base_userfintech_url + @"/user/risk/checkAnswer";
        }
        
        public static string logout() {
            return urls.base_userbroker_url + @"/passport/login/logout";
        }
        
        public static string news(string stock, string Id, string items) {
            return urls.base_fintech_gw_url + @"/information/news/tickerNews?tickerId=" + stock + " & currentNewsId=" + Id + "&pageSize=" + items;
        }
        
        public static string option_quotes() {
            return urls.base_options_gw_url + @"/quote/option/query/list";
        }
        
        public static string options(string stock) {
            return urls.base_options_url + @"/quote/option/" + stock + @"/list";
        }
        
        public static string options_exp_date(string stock) {
            return urls.base_options_url + @"/quote/option/" + stock + @"/list";
        }

        public static string options_exp_dat_new() {
            return urls.base_options_url_new + @"/quote/option/strategy/list";
        }
        
        public static string options_bars(string derivativeId) {
            return urls.base_options_gw_url + @"/quote/option/chart/query?derivativeId=" + derivativeId;
        }
        
        public static string orders(string account_id, string page_size) {
            return urls.base_ustradebroker_url + @"/trade/v2/option/list?secAccountId=" + account_id + "&startTime=1970-0-1&dateType=ORDER&pageSize=" + page_size + "&status=";
        }
        
        public static string history(string account_id) {
            return urls.base_ustrade_url + @"/trading/v1/webull/order/list?secAccountId=" + account_id;
        }
        
        public static string paper_orders(string paper_account_id, string page_size) {
            return urls.base_paper_url + @"/paper/1/acc/" + paper_account_id + @"/order?&startTime=1970-0-1&dateType=ORDER&pageSize=" + page_size + "&status=";
        }
        
        public static string paper_account(string paper_account_id) {
            return urls.base_paperfintech_url + @"/paper/1/acc/" + paper_account_id;
        }
        
        public static string paper_account_id() {
            return urls.base_paperfintech_url + @"/myaccounts/true";
        }
        
        public static string paper_cancel_order(string paper_account_id, string order_id) {
            return urls.base_paper_url + @"/paper/1/acc/" + paper_account_id + @"/orderop/cancel/" + order_id;
        }
        
        public static string paper_modify_order(string paper_account_id, string order_id) {
            return urls.base_paper_url + @"/paper/1/acc/" + paper_account_id + @"/orderop/modify/" + order_id;
        }
        
        public static string paper_place_order(string paper_account_id, string stock) {
            return urls.base_paper_url + @"/paper/1/acc/" + paper_account_id + @"/orderop/place/" + stock;
        }
        
        public static string place_option_orders(string account_id) {
            return urls.base_ustrade_url + @"/trade/v2/option/placeOrder/" + account_id;
        }
        
        public static string place_orders(string account_id) {
            return urls.base_ustrade_url + @"/trade/order/" + account_id + @"/placeStockOrder";
        }
        
        public static string modify_order(string account_id, string order_id) {
            return urls.base_ustrade_url + @"/trading/v1/webull/order/stockOrderModify?secAccountId=" + account_id;
        }
        
        public static string quotes(string stock) {
            return urls.base_options_gw_url + @"/quotes/ticker/getTickerRealTime?tickerId=" + stock + "&includeSecu=1&includeQuote=1";
        }
        
        public static string rankings() {
            return urls.base_securities_url + @"/securities/market/v5/6/portal";
        }
        
        public static string refresh_login() {
            return urls.base_user_url + @"/passport/refreshToken?refreshToken=";
        }
        
        public static string remove_alert() {
            return urls.base_userbroker_url + @"/user/warning/v2/manage/overlap";
        }
        
        public static string replace_option_orders(string account_id) {
            return urls.base_trade_url + @"/v2/option/replaceOrder/" + account_id;
        }
        
        public static string stock_id(string stock, string region_code) {
            return urls.base_options_gw_url + @"/search/pc/tickers?keyword=" + stock + "&pageIndex=1&pageSize=20&regionId=" + region_code;
        }
        
        public static string trade_token() {
            return urls.base_new_trade_url + @"/trading/v1/global/trade/login";
        }
        
        public static string user() {
            return urls.base_user_url + @"/user";
        }
        
        public static string screener() {
            return urls.base_userbroker_url + @"/wlas/screener/ng/query";
        }
        
        public static string social_posts(string topic, string num) {
            if (String.IsNullOrEmpty(num)) { num = "100"; }
            return urls.base_user_url + @"/social/feed/topic/" + topic + @"/posts?size= " + num;
        }
        
        public static string social_home(string topic, string num) {
            if (String.IsNullOrEmpty(num)) { num = "100"; }
            return urls.base_user_url + @"/social/feed/topic/" + topic + @"/home?size=" + num;
        }
        
        public static string portfolio_lists() {
            return urls.base_options_gw_url + @"/personal/portfolio/v2/check";
        }
    }
}