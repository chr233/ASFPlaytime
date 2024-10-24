using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using System.Text;

namespace ASFPlaytime.Core;

internal static class Command
{
    /// <summary>
    /// 获取游玩时间
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseDumpSingleBotPlayTime(Bot bot)
    {
        var sb = new StringBuilder();

        var login = bot.BotConfig.SteamLogin;
        var passwd = bot.BotConfig.SteamPassword;
        var email = await WebRequest.GetAccountEmail(bot).ConfigureAwait(false) ?? "[null]";
        sb.Append($"{login}:{passwd}:{email} - ");

        if (bot.IsConnectedAndLoggedOn)
        {
            var first = true;

            var result = await WebRequest.GetGamePlayTime(bot).ConfigureAwait(false);
            if (result == null)
            {
                sb.Append("[Network error]");
            }
            else
            {
                foreach (var game in result)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    else
                    {
                        first = false;
                    }

                    var playHours = game.PlayTimeForever / 60.0;
                    var gameName = game.Name?.Replace(",", "").Replace("(", "[").Replace(")", "]") ?? "[null]";
                    sb.Append($"{gameName} ({playHours:0.00})");
                }
            }
        }
        else
        {
            sb.Append("[Bot not connected]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取游玩时间 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseDumpPlayTime(string? filename)
    {
        var bots = Bot.GetBots("ASF");
        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(Strings.BotNotFound, "ASF");
        }

        var results = await Utilities.InParallel(bots.Select(ResponseDumpSingleBotPlayTime)).ConfigureAwait(false);
        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        var fileContent = string.Join(Environment.NewLine, responses);

        if (!string.IsNullOrEmpty(fileContent) && !string.IsNullOrEmpty(filename))
        {
            var path = Path.Combine(MyDirectory, filename);
            await File.WriteAllTextAsync(path, fileContent).ConfigureAwait(false);
            return $"Dump to {path} success";
        }
        else
        {
            return "Dump failed";
        }
    }

    /// <summary>
    /// 读取账号消费历史
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAccountHistory(Bot bot)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        string result = await WebRequest.GetAccountHistoryDetail(bot).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// 读取账号消费历史 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseAccountHistory(string botNames)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if ((bots == null) || (bots.Count == 0))
        {
            return FormatStaticResponse(Strings.BotNotFound, botNames);
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAccountHistory(bot))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }
}
