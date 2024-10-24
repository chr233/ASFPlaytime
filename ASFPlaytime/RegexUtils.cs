using System.Text.RegularExpressions;

namespace ASFPlaytime;
internal static partial class RegexUtils
{
    [GeneratedRegex("g_historyCursor = ([^;]+)")]
    public static partial Regex MatchHistortyCursor();

    [GeneratedRegex(@"^([-+])?([^\d,.]*)([\d,.]+)\s*([^\d,.]*|[pуб.]*)$")]
    public static partial Regex MatchHistoryItem();
}
