using AngleSharp.Dom;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Web.Responses;
using ASFPlaytime.Data;
using System.Globalization;

namespace ASFPlaytime.Core;

internal static class HtmlParser
{
    /// <summary>
    /// 获取Cursor对象
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static AccountHistoryResponse.CursorData? ParseCursorData(HtmlDocumentResponse? response)
    {
        if (response?.Content?.Body == null)
            return null;

        var content = response.Content.Body.InnerHtml;
        var match = RegexUtils.MatchHistortyCursor().Match(content);
        if (!match.Success)
            return null;

        content = match.Groups[1].Value;
        try
        {
            var cursorData = content.ToJsonObject<AccountHistoryResponse.CursorData>();
            return cursorData;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析历史记录条目
    /// </summary>
    /// <param name="tableElement"></param>
    /// <param name="currencyRates"></param>
    /// <param name="defaultCurrency"></param>
    /// <returns></returns>
    internal static HistoryParseResponse ParseHistory(IElement tableElement, Dictionary<string, decimal> currencyRates, string defaultCurrency)
    {
        var pattern = RegexUtils.MatchHistoryItem();

        // 识别货币符号
        string ParseSymbol(string symbol1, string symbol2)
        {
            const char USD = '$';
            const char RMB = '¥';

            var currency = string.Empty;

            if (!string.IsNullOrEmpty(symbol1))
                if (CurrencyHelper.SymbolCurrency.TryGetValue(symbol1, out var c))
                    currency = c;

            if (!string.IsNullOrEmpty(symbol2))
                if (CurrencyHelper.SymbolCurrency.TryGetValue(symbol2, out var c))
                    currency = c;

            if (string.IsNullOrEmpty(currency))
                if (symbol1.Contains(USD) || symbol2.Contains(USD))
                    return "USD";
                else if (symbol1.Contains(RMB) || symbol2.Contains(RMB))
                    return defaultCurrency;
                else
                {
                    ASFLogger.LogGenericWarning(string.Format("检测货币符号失败, 使用默认货币单位 {0}", defaultCurrency));
                    return defaultCurrency;
                }
            else
                return currency;
        }

        // 识别货币数值
        decimal ParseMoneyString(string strMoney)
        {
            var match = pattern.Match(strMoney);

            if (!match.Success)
                return 0;
            else
            {
                var negative = match.Groups[1].Value == "-";
                string symbol1 = match.Groups[2].Value.Trim();
                string strPrice = match.Groups[3].Value;
                string symbol2 = match.Groups[4].Value.Trim();

                var currency = ParseSymbol(symbol1, symbol2);

                var useDot = CurrencyHelper.DotCurrency.Contains(currency);

                if (useDot)
                    strPrice = strPrice.Replace(".", ";").Replace(',', '.').Replace(';', ',');

                if (decimal.TryParse(strPrice, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, null, out var price))
                    if (currencyRates.TryGetValue(currency, out var rate))
                        return (negative ? -1 : 1) * (price / rate);
                    else
                    {
                        ASFLogger.LogGenericWarning(string.Format("无 {0} 货币的汇率", currency));
                        return (negative ? -1 : 1) * price;
                    }
                else
                {
                    ASFLogger.LogGenericWarning(string.Format("解析价格 {0} 失败", match.Groups[3].Value));
                    return 0;
                }
            }
        }

        HistoryParseResponse result = new();

        var rows = tableElement.QuerySelectorAll("tr");

        foreach (var row in rows)
        {
            if (!row.HasChildNodes)
                continue;

            var whtItem = row?.QuerySelector("td.wht_items");
            var whtType = row?.QuerySelector("td.wht_type");
            var whtTotal = row?.QuerySelector("td.wht_total");
            var whtChange = row?.QuerySelector("td.wht_wallet_change.wallet_column");

            var isRefund = whtType?.ClassName?.Contains("wht_refunded") ?? false;

            var strItem = whtItem?.Text().Trim().Replace("\t", "") ?? "";
            var strType = whtType?.Text().Trim().Replace("\t", "") ?? "";
            var strTotal = whtTotal?.Text().Replace("资金", "").Trim().Replace("\t", "") ?? "";
            var strChange = whtChange?.Text().Trim().Replace("\t", "") ?? "";

            if (!string.IsNullOrEmpty(strType))
                // 排除退款和转换货币
                if (!string.IsNullOrEmpty(strType) && !strType.StartsWith("转换") && !strType.StartsWith("退款"))
                {
                    var total = (int)(ParseMoneyString(strTotal) * 100);

                    int walletChange;
                    int walletChangeAbs;

                    if (string.IsNullOrEmpty(strChange))
                        walletChange = 0;
                    else
                        walletChange = (int)(ParseMoneyString(strChange) * 100);
                    walletChangeAbs = Math.Abs(walletChange);

                    if (total == 0)
                        continue;

                    if (strType.StartsWith("购买"))
                        if (!strItem.Contains("钱包资金"))
                            if (!isRefund)
                            {
                                result.StorePurchase += total;
                                result.StorePurchaseWallet += walletChangeAbs;
                            }
                            else
                            {
                                result.RefundPurchase += total;
                                result.RefundPurchaseWallet += walletChangeAbs;
                            }
                        else
                            result.WalletPurchase += total;
                    else if (strType.StartsWith("礼物购买"))
                        if (!isRefund)
                        {
                            result.GiftPurchase += total;
                            result.GiftPurchaseWallet += walletChangeAbs;
                        }
                        else
                        {
                            result.RefundPurchase += total;
                            result.RefundPurchaseWallet += walletChangeAbs;
                        }
                    else if (strType.StartsWith("游戏内购买"))
                        if (!isRefund)
                            result.InGamePurchase += walletChangeAbs;
                        else
                        {
                            result.RefundPurchase += total;
                            result.RefundPurchaseWallet += walletChangeAbs;
                        }
                    else if (strType.Contains("市场交易"))
                        if (!isRefund)
                            if (walletChange >= 0)
                                result.MarketSelling += total;
                            else
                                result.MarketPurchase += total;
                        else
                            result.RefundPurchase += total;
                    else
                        if (!isRefund)
                        result.Other += total;
                    else
                        result.RefundPurchase -= total;
                }
        }

        return result;
    }
}
