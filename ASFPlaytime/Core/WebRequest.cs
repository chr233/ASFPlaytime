using ArchiSteamFarm.Steam;

namespace ASFPlaytime.Core;

internal static class WebRequest
{
    /// <summary>
    /// 获取游戏游玩时间
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<List< GetOwnedGamesResponse.GameData>?> GetGamePlayTime(Bot bot)
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
}
