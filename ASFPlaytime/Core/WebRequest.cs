using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using ASFPlaytime.Data;
using System.Text;

namespace ASFPlaytime.Core;

static class WebRequest
{
    /// <summary>
    ///     获取游戏游玩时间
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    public static async Task<List<GetOwnedGamesResponse.GameData>?> GetGamePlayTime(Bot bot)
    {
        var token = bot.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var request = new Uri(SteamApiURL,
            $"/IPlayerService/GetOwnedGames/v1/?access_token={token}&steamid={bot.SteamID}&include_appinfo=true&include_played_free_games=true&include_free_sub=true&skip_unvetted_apps=true&language={DefaultOrCurrentLanguage}&include_extended_appinfo=true");
        var response = await bot.ArchiWebHandler
            .UrlGetToJsonObjectWithSession<GetOwnedGamesResponse>(request, referer: SteamStoreURL)
            .ConfigureAwait(false);

        return response?.Content?.Response?.Games;
    }

    /// <summary>
    ///     获取账号邮箱
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    public static async Task<string?> GetAccountEmail(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/account");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL)
            .ConfigureAwait(false);

        if (response?.Content == null)
        {
            return null;
        }

        var eleEmail = response.Content.QuerySelector(
            "#main_content div.account_setting_sub_block:nth-child(1) > div:nth-child(2) span.account_data_field");
        return eleEmail?.TextContent;
    }

    /// <summary>
    ///     加载账户历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="cursorData"></param>
    /// <returns></returns>
    static async Task<AccountHistoryResponse?> AjaxLoadMoreHistory(Bot bot,
        AccountHistoryResponse.CursorData cursorData)
    {
        var request = new Uri(SteamStoreURL, "/account/AjaxLoadMoreHistory/?l=schinese");

        var data = new Dictionary<string, string>(5, StringComparer.Ordinal)
        {
            { "cursor[wallet_txnid]", cursorData.WalletTxnid },
            { "cursor[timestamp_newest]", cursorData.TimestampNewest.ToString() },
            { "cursor[balance]", cursorData.Balance },
            { "cursor[currency]", cursorData.Currency.ToString() }
        };

        var response = await bot.ArchiWebHandler!
            .UrlPostToJsonObjectWithSession<AccountHistoryResponse>(request, referer: SteamStoreURL, data: data)
            .ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    ///     获取在线汇率
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="currency"></param>
    /// <returns></returns>
    public static async Task<ExchangeAPIResponse?> GetExchangeRatio(string currency)
    {
        var request = new Uri($"https://api.exchangerate-api.com/v4/latest/{currency}");
        var response = await ASF.WebBrowser!.UrlGetToJsonObject<ExchangeAPIResponse>(request).ConfigureAwait(false);
        return response?.Content;
    }

    /// <summary>
    ///     获取更多历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    static async Task<HtmlDocumentResponse?> GetAccountHistoryAjax(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/account/history?l=schinese");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL)
            .ConfigureAwait(false);
        return response;
    }

    /// <summary>
    ///     获取账号消费历史记录
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="exchangeRate"></param>
    /// <returns></returns>
    internal static async Task<HistoryParseResponse?> GetAccountHistoryDetail(Bot bot)
    {
        // 读取在线汇率
        var myCurrency = bot.WalletCurrency.ToString();

        var exchangeRate = await GetExchangeRatio(myCurrency).ConfigureAwait(false);

        if (exchangeRate == null)
        {
            return null;
        }

        // 获取货币符号
        if (!CurrencyHelper.Currency2Symbol.TryGetValue(myCurrency, out var symbol))
        {
            symbol = myCurrency;
        }

        var result = new StringBuilder();
        result.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

        // 读取账户消费历史
        var accountHistory = await GetAccountHistoryAjax(bot).ConfigureAwait(false);
        if (accountHistory == null)
        {
            return null;
        }

        // 解析表格元素
        var tbodyElement = accountHistory?.Content?.QuerySelector("table>tbody");
        if (tbodyElement == null)
        {
            return null;
        }

        // 获取下一页指针(为null代表没有下一页)
        var cursor = HtmlParser.ParseCursorData(accountHistory);

        var historyData = HtmlParser.ParseHistory(tbodyElement, exchangeRate.Rates, myCurrency);

        while (cursor != null)
        {
            var ajaxHistoryResponse = await AjaxLoadMoreHistory(bot, cursor).ConfigureAwait(false);

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

        return historyData;
    }
}