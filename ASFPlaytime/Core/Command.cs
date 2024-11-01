using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using System.Text;

namespace ASFPlaytime.Core;

static class Command
{
    /// <summary>
    ///     获取游玩时间
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    static async Task<string?> ResponseDumpSingleBotPlayTime(Bot bot)
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
    ///     获取游玩时间 (多个Bot)
    /// </summary>
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

        return "Dump failed";
    }

    /// <summary>
    ///     读取账号消费历史
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    static async Task<string?> ResponseDumpPurchaseHistory(Bot bot)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        var sb = new StringBuilder();

        var login = bot.BotConfig.SteamLogin;
        var passwd = bot.BotConfig.SteamPassword;
        var email = await WebRequest.GetAccountEmail(bot).ConfigureAwait(false) ?? "[null]";
        sb.Append($"{login}:{passwd}:{email} - ");

        if (bot.IsConnectedAndLoggedOn)
        {
            var historyData = await WebRequest.GetAccountHistoryDetail(bot).ConfigureAwait(false);
            if (historyData == null)
            {
                sb.Append("[Network error]");
            }
            else
            {
                var myCurrency = bot.WalletCurrency.ToString();
                if (!Currency2Symbol.TryGetValue(myCurrency, out var symbol))
                {
                    symbol = myCurrency;
                }

                var totalSpend = historyData.StorePurchase + historyData.GiftPurchase + historyData.InGamePurchase -
                                 historyData.RefundPurchase + historyData.MarketPurchase;
                var totalTopped = historyData.WalletPurchase;

                sb.Append($"{totalSpend * 0.01:0.00}{symbol} / {totalTopped * 0.01:0.00}{symbol}");
            }
        }
        else
        {
            sb.Append("[Bot not connected]");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     读取账号消费历史 (多个Bot)
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseDumpPurchaseHistory(string? filename)
    {
        var bots = Bot.GetBots("ASF");
        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(Strings.BotNotFound, "ASF");
        }


        var results = await Utilities.InParallel(bots.Select(ResponseDumpPurchaseHistory)).ConfigureAwait(false);
        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        var fileContent = string.Join(Environment.NewLine, responses);

        if (!string.IsNullOrEmpty(fileContent) && !string.IsNullOrEmpty(filename))
        {
            var path = Path.Combine(MyDirectory, filename);
            await File.WriteAllTextAsync(path, fileContent).ConfigureAwait(false);
            return $"Dump to {path} success";
        }

        return "Dump failed";
    }
}