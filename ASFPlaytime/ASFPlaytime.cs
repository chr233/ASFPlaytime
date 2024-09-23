using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFPlaytime.Core;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ASFPlaytime;

[Export(typeof(IPlugin))]
internal sealed class ASFPlaytime : IASF, IBotCommand2
{
    public string Name => nameof(ASFPlaytime);

    public Version Version => MyVersion;

    private bool ASFEBridge;

    private Timer? StatisticTimer { get; set; }


    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null)
    {
        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach (var (configProperty, configValue) in additionalConfigProperties)
            {
                if (configProperty == "ASFEnhance" && configValue.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        config = configValue.Deserialize<PluginConfig>();
                        if (config != null)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ASFLogger.LogGenericException(ex);
                    }
                }
            }
        }

        Config = config ?? new();

        var warnings = new StringBuilder("\n");

        //使用协议
        if (!Config.EULA)
        {
            warnings.AppendLine(Static.Line);
            warnings.AppendLineFormat(Langs.EulaWarning, Name);
            warnings.AppendLine(Static.Line);
        }

        if (warnings.Length > 1)
        {
            ASFLogger.LogGenericWarning(warnings.ToString());
        }

        //统计
        if (Config.Statistic && !ASFEBridge)
        {
            var request = new Uri("https://asfe.chrxw.com/asflevelupbot");
            StatisticTimer = new Timer(
                async (_) =>
                {
                    await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false);
                },
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        ASFLogger.LogGenericInfo(Langs.PluginContact);
        ASFLogger.LogGenericInfo(Langs.PluginInfo);

        const BindingFlags flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var handler = typeof(ASFPlaytime).GetMethod(nameof(ResponseCommand), flag);

        const string pluginId = nameof(ASFPlaytime);
        const string cmdPrefix = "ALU";
        const string? repoName = null;

        ASFEBridge = AdapterBridge.InitAdapter(Name, pluginId, cmdPrefix, repoName, handler);

        if (ASFEBridge)
        {
            ASFLogger.LogGenericDebug(Langs.ASFEnhanceRegisterSuccess);
        }
        else
        {
            ASFLogger.LogGenericInfo(Langs.ASFEnhanceRegisterFailed);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取插件信息
    /// </summary>
    private static string? PluginInfo => string.Format("{0} {1}", nameof(ASFPlaytime), MyVersion);

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="cmd"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Task<string?>? ResponseCommand(Bot bot, EAccess access, string cmd, string message, string[] args)
    {
        var argLength = args.Length;

        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch //不带参数
            {
                //Plugin Info
                "ASFPLAYTIME" or
                "ASFP" when access >= EAccess.FamilySharing =>
                    Task.FromResult(PluginInfo),

                "DUMPPLAYTIME" when access >= EAccess.Master =>
                    Command.ResponseDumpPlayTime( "playtime.txt"),
                _ => null,
            },
            _ => cmd switch //带参数
            {
                "DUMPPLAYTIME" when  access >= EAccess.Master =>
                    Command.ResponseDumpPlayTime( Utilities.GetArgsAsText(message,1)),
                
                _ => null,
            }
        };
    }

    /// <summary>
    /// 处理命令事件
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId = 0)
    {
        if (ASFEBridge)
        {
            return null;
        }

        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var cmd = args[0].ToUpperInvariant();

            if (cmd.StartsWith("ASFP."))
            {
                cmd = cmd[5..];
            }

            var task = ResponseCommand(bot, access, cmd, message,args);
            if (task != null)
            {
                return await task.ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                ASFLogger.LogGenericException(ex);
            }).ConfigureAwait(false);

            return ex.StackTrace;
        }
    }
}
