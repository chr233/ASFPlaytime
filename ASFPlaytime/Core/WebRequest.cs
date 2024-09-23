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
}
