using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ASFPlaytime.Data;
using System.Reflection;
using System.Text;

namespace ASFPlaytime;

internal static class Utils
{
    internal static PluginConfig Config { get; set; } = new();

    /// <summary>
    /// 获取版本号
    /// </summary>
    internal static Version MyVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
    
    /// <summary>
    /// 获取插件所在路径
    /// </summary>
    internal static string MyLocation => Assembly.GetExecutingAssembly().Location;

    /// <summary>
    /// 获取插件所在文件夹路径
    /// </summary>
    internal static string MyDirectory => Path.GetDirectoryName(MyLocation) ?? ".";
    
    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message)
    {
        return $"<ASFE> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message, params object?[] args)
    {
        return FormatStaticResponse(string.Format(message, args));
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message)
    {
        return $"<{bot.BotName}> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message, params object?[] args)
    {
        return bot.FormatBotResponse(string.Format(message, args));
    }

    internal static StringBuilder AppendLineFormat(this StringBuilder sb, string format, params object?[] args)
    {
        return sb.AppendLine(string.Format(format, args));
    }
    
    /// <summary>
    /// Steam商店链接
    /// </summary>
    internal static Uri SteamStoreURL => ArchiWebHandler.SteamStoreURL;
    
    /// <summary>
    /// SteamAPI链接
    /// </summary>
    internal static Uri SteamApiURL => new("https://api.steampowered.com");
    
    /// <summary>
    /// 日志
    /// </summary>
    internal static ArchiLogger ASFLogger => ASF.ArchiLogger;

    /// <summary>
    /// 默认语言设置
    /// </summary>
    internal static string DefaultOrCurrentLanguage => Config.DefaultLanguage ?? Langs.Language;
    
    /// <summary>
    /// 货币ISO名称转成符号
    /// </summary>
    internal static Dictionary<string, string> Currency2Symbol { get; } = new() {
        { "AED", "AED" },
        { "ARS", "ARS$" },
        { "AUD", "A$" },
        { "BRL", "R$" },
        { "CAD", "CDN$" },
        { "CHF", "CHF" },
        { "CLP", "CLP$" },
        { "CNY", "¥" },
        { "COP", "COL$" },
        { "CRC", "₡" },
        { "EUR", "€" },
        { "GBP", "£" },
        { "HKD", "HK$" },
        { "IDR", "Rp" },
        { "ILS", "₪" },
        { "INR", "₹" },
        { "JPY", "¥" },
        { "KRW", "₩" },
        { "KWD", "KD" },
        { "KZT", "₸" },
        { "MXN", "Mex$" },
        { "MYR", "RM" },
        { "NOK", "kr" },
        { "NZD", "NZ$" },
        { "PEN", "S/." },
        { "PHP", "₱" },
        { "PLN", "zł" },
        { "QAR", "QR" },
        { "RUB", "₽" },
        { "SAR", "SR" },
        { "SGD", "S$" },
        { "THB", "฿" },
        { "TRY", "₺" },
        { "TWD", "NT$" },
        { "UAH", "₴" },
        { "USD", "$" },
        { "UYU", "$U" },
        { "VND", "₫" },
        { "ZAR", "R" },
    };
}
