namespace ASFPlaytime;

/// <summary>
/// 应用配置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 是否同意使用协议
    /// </summary>
    public bool EULA { get; set; }
    /// <summary>
    /// 启用统计信息
    /// </summary>
    public bool Statistic { get; set; } = true;

    /// <summary>
    /// 默认语言, 影响 PUBLISCRECOMMAND 命令
    /// </summary>
    public string? DefaultLanguage { get; set; }
}
