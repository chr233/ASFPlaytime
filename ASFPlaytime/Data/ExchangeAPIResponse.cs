using System.Text.Json.Serialization;

namespace ASFPlaytime.Data;

internal sealed record ExchangeAPIResponse
{
    [JsonPropertyName("base")]
    public string Base { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("time_last_updated")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = [];
}
