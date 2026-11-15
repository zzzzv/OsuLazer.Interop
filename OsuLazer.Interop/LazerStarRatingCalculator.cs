using System.Text;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace OsuLazer.Interop;

/// <summary>
/// 用 lazer 自己的 ruleset 计算难度星级，结果与 lazer 客户端一致。
/// 与具体模式无关：ruleset 由谱面自身的 <c>Ruleset</c> 决定，mod 由调用方提供。
/// </summary>
public sealed class LazerStarRatingCalculator
{
    /// <summary>使用默认 lazer 安装目录的共享实例。</summary>
    public static LazerStarRatingCalculator Default { get; } = new();

    private readonly Lazy<AssemblyRulesetStore> rulesetStore;

    /// <param name="lazerDirectory">ruleset 程序集所在目录，null 表示默认 lazer 安装目录。</param>
    public LazerStarRatingCalculator(string? lazerDirectory = null)
    {
        var directory = lazerDirectory ?? LazerHost.LazerDirectory;
        rulesetStore = new Lazy<AssemblyRulesetStore>(() => new AssemblyRulesetStore(directory));
    }

    /// <summary>按谱面文件路径计算星级，mod 由调用方给（任意 ruleset 的 mod 实例均可）。</summary>
    public double CalcSR(string beatmapPath, Mod[] mods)
    {
        LazerHost.EnsureInitialized();

        var working = CreateWorkingBeatmap(File.ReadAllText(beatmapPath));
        var ruleset = ResolveRuleset(working.BeatmapInfo);
        return ruleset.CreateDifficultyCalculator(working).Calculate(mods).StarRating;
    }

    /// <summary>按 .osu 文件内容计算星级。</summary>
    public double CalcSRFromContent(string beatmapContent, APIMod[] apiMods)
    {
        LazerHost.EnsureInitialized();

        var working = CreateWorkingBeatmap(beatmapContent);
        var ruleset = ResolveRuleset(working.BeatmapInfo);
        var mods = apiMods.Select(m => m.ToMod(ruleset)).ToArray();
        return ruleset.CreateDifficultyCalculator(working).Calculate(mods).StarRating;
    }

    private static FlatWorkingBeatmap CreateWorkingBeatmap(string beatmapContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(beatmapContent));
        using var reader = new LineBufferedReader(stream);
        var beatmap = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        return new FlatWorkingBeatmap(beatmap);
    }

    private Ruleset ResolveRuleset(BeatmapInfo beatmapInfo)
    {
        var rulesetInfo = beatmapInfo.Ruleset ?? throw new InvalidOperationException("谱面无 ruleset 信息");
        var info = rulesetStore.Value.GetRuleset(rulesetInfo.ShortName)
            ?? throw new InvalidOperationException($"找不到 ruleset: {rulesetInfo.ShortName}");

        return info.CreateInstance() ?? throw new InvalidOperationException($"无法创建 ruleset 实例: {rulesetInfo.Name}");
    }
}
