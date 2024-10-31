namespace ASFPlaytime.Core;

internal static class CurrencyHelper
{
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

    /// <summary>
    /// 货币符号转成ISO名称
    /// </summary>
    internal static Dictionary<string, string> SymbolCurrency { get; } = new() {
        { "AED",  "AED" },
        { "ARS$", "ARS" },
        { "A$",   "AUD" },
        { "R$",   "BRL" },
        { "CDN$", "CAD" },
        { "CHF",  "CHF" },
        { "CLP$", "CLP" },
        { "¥",    "CNY" },
        { "COL$", "COP" },
        { "₡",    "CRC" },
        { "--€",    "EUR" },
        { "€",    "EUR" },
        { "£",    "GBP" },
        { "HK$",  "HKD" },
        { "Rp",   "IDR" },
        { "₪",    "ILS" },
        { "₹",    "INR" },
        { "₩",    "KRW" },
        { "KD",   "KWD" },
        { "₸",    "KZT" },
        { "Mex$", "MXN" },
        { "RM",   "MYR" },
        { "kr",   "NOK" },
        { "NZ$",  "NZD" },
        { "S/.",  "PEN" },
        { "₱",    "PHP" },
        { "zł",   "PLN" },
        { "QR",   "QAR" },
        { "руб.", "RUB" },
        { "ру6.", "RUB" },
        { "₽",    "RUB" },
        { "SR",   "SAR" },
        { "S$",   "SGD" },
        { "฿",    "THB" },
        { "TL",   "TRY" },
        { "₺",    "TRY" },
        { "NT$",  "TWD" },
        { "₴",    "UAH" },
        { "$",    "USD" },
        { "$U",   "UYU" },
        { "₫",    "VND" },
        { "R",    "ZAR" },
    };

    /// <summary>
    /// 使用逗号作为小数点的国家
    /// </summary>
    internal static HashSet<string> DotCurrency { get; } = [
        "TRY",
        "ARS",
        "BRL",
        "NOK",
        "EUR",
        "PLN",
        "VND",
        "RUB",
    ];
}
