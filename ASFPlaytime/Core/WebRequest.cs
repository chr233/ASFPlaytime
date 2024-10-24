using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using ASFPlaytime.Data;
using System.Text;

namespace ASFPlaytime.Core;

internal static class WebRequest
{
    /// <summary>
    /// 获取游戏游玩时间
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<List<GetOwnedGamesResponse.GameData>?> GetGamePlayTime(Bot bot)
    {
        var token = bot.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var request = new Uri(SteamApiURL, $"/IPlayerService/GetOwnedGames/v1/?access_token={token}&steamid={bot.SteamID}&include_appinfo=true&include_played_free_games=true&include_free_sub=true&skip_unvetted_apps=true&language={DefaultOrCurrentLanguage}&include_extended_appinfo=true");
        var response = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<GetOwnedGamesResponse>(request, referer: SteamStoreURL).ConfigureAwait(false);

        return response?.Content?.Response?.Games;
    }

    /// <summary>
    /// 获取账号邮箱
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> GetAccountEmail(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/account");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL).ConfigureAwait(false);

        if (response?.Content == null)
        {
            return null;
        }

        var eleEmail = response.Content.QuerySelector("#main_content div.account_setting_sub_block:nth-child(1) > div:nth-child(2) span.account_data_field");
        return eleEmail?.TextContent;
    }

    /// <summary>
    /// 加载账户历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="cursorData"></param>
    /// <returns></returns>
    private static async Task<AccountHistoryResponse?> AjaxLoadMoreHistory(Bot bot, AccountHistoryResponse.CursorData cursorData)
    {
        var request = new Uri(SteamStoreURL, "/account/AjaxLoadMoreHistory/?l=schinese");

        var data = new Dictionary<string, string>(5, StringComparer.Ordinal) {
            { "cursor[wallet_txnid]", cursorData.WalletTxnid },
            { "cursor[timestamp_newest]", cursorData.TimestampNewest.ToString() },
            { "cursor[balance]", cursorData.Balance },
            { "cursor[currency]", cursorData.Currency.ToString() },
        };

        var response = await bot.ArchiWebHandler!.UrlPostToJsonObjectWithSession<AccountHistoryResponse>(request, referer: SteamStoreURL, data: data).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 获取在线汇率
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="currency"></param>
    /// <returns></returns>
    private static async Task<ExchangeAPIResponse?> GetExchangeRatio(string currency)
    {
        var request = new Uri($"https://api.exchangerate-api.com/v4/latest/{currency}");
        var response = await ASF.WebBrowser!.UrlGetToJsonObject<ExchangeAPIResponse>(request).ConfigureAwait(false);
        return response?.Content;
    }

    /// <summary>
    /// 获取更多历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    private static async Task<HtmlDocumentResponse?> GetAccountHistoryAjax(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/account/history?l=schinese");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// 获取账号消费历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string> GetAccountHistoryDetail(Bot bot)
    {
        // 读取在线汇率
        string myCurrency = bot.WalletCurrency.ToString();
        ExchangeAPIResponse? exchangeRate = await GetExchangeRatio(myCurrency).ConfigureAwait(false);
        if (exchangeRate == null)
        {
            return bot.FormatBotResponse(Langs.GetExchangeRateFailed);
        }

        // 获取货币符号
        if (!CurrencyHelper.Currency2Symbol.TryGetValue(myCurrency, out var symbol))
        {
            symbol = myCurrency;
        }

        var result = new StringBuilder();
        result.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

        int giftedSpend = 0;
        int totalSpend = 0;
        int totalExternalSpend = 0;

        // 读取账户消费历史
        result.AppendLine(Langs.PurchaseHistorySummary);
        var accountHistory = await GetAccountHistoryAjax(bot).ConfigureAwait(false);
        if (accountHistory == null)
        {
            return Langs.NetworkError;
        }

        // 解析表格元素
        var tbodyElement = accountHistory?.Content?.QuerySelector("table>tbody");
        if (tbodyElement == null)
        {
            return Langs.ParseHtmlFailed;
        }

        // 获取下一页指针(为null代表没有下一页)
        var cursor = HtmlParser.ParseCursorData(accountHistory);

        var historyData = HtmlParser.ParseHistory(tbodyElement, exchangeRate.Rates, myCurrency);

        while (cursor != null)
        {
            AccountHistoryResponse? ajaxHistoryResponse = await AjaxLoadMoreHistory(bot, cursor).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(ajaxHistoryResponse?.HtmlContent))
            {
                tbodyElement.InnerHtml = ajaxHistoryResponse.HtmlContent;
                cursor = ajaxHistoryResponse.Cursor;
                historyData += HtmlParser.ParseHistory(tbodyElement, exchangeRate.Rates, myCurrency);
            }
            else
            {
                cursor = null;
            }
        }

        giftedSpend = historyData.GiftPurchase;
        totalSpend = historyData.StorePurchase + historyData.InGamePurchase;
        totalExternalSpend = historyData.StorePurchase - historyData.StorePurchaseWallet + historyData.GiftPurchase - historyData.GiftPurchaseWallet;

        result.AppendLine(Langs.PurchaseHistoryGroupType);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeStorePurchase, historyData.StorePurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeExternal, (historyData.StorePurchase - historyData.StorePurchaseWallet) / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeWallet, historyData.StorePurchaseWallet / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeGiftPurchase, historyData.GiftPurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeExternal, (historyData.GiftPurchase - historyData.GiftPurchaseWallet) / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeWallet, historyData.GiftPurchaseWallet / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeInGamePurchase, historyData.InGamePurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeMarketPurchase, historyData.MarketPurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeMarketSelling, historyData.MarketSelling / 100.0, symbol);

        result.AppendLine(Langs.PurchaseHistoryGroupOther);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeWalletPurchase, historyData.WalletPurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeOther, historyData.Other / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeRefunded, historyData.RefundPurchase / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeExternal, (historyData.RefundPurchase - historyData.RefundPurchaseWallet) / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryTypeWallet, historyData.RefundPurchaseWallet / 100.0, symbol);

        result.AppendLine(Langs.PurchaseHistoryGroupStatus);
        result.AppendLineFormat(Langs.PurchaseHistoryStatusTotalPurchase, totalSpend / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryStatusTotalExternalPurchase, totalExternalSpend / 100.0, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryStatusTotalGift, giftedSpend / 100.0, symbol);
        result.AppendLine(Langs.PurchaseHistoryGroupGiftCredit);
        result.AppendLineFormat(Langs.PurchaseHistoryCreditMin, (totalSpend - giftedSpend) / 100, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryCreditMax, (totalSpend * 1.8 - giftedSpend) / 100, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryExternalMin, (totalExternalSpend - giftedSpend) / 100, symbol);
        result.AppendLineFormat(Langs.PurchaseHistoryExternalMax, (totalExternalSpend * 1.8 - giftedSpend) / 100, symbol);

        var updateTime = DateTimeOffset.FromUnixTimeSeconds(exchangeRate.UpdateTime).UtcDateTime;

        result.AppendLine(Langs.PurchaseHistoryGroupAbout);
        result.AppendLineFormat(Langs.PurchaseHistoryAboutBaseRate, exchangeRate.Base);
        result.AppendLineFormat(Langs.PurchaseHistoryAboutPlugin, nameof(ASFPlaytime));
        result.AppendLineFormat(Langs.PurchaseHistoryAboutUpdateTime, updateTime);
        result.AppendLine(Langs.PurchaseHistoryAboutRateSource);

        return result.ToString();
    }

}
