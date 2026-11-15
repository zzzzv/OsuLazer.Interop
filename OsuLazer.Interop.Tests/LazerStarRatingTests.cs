using osu.Game.Online.API;
using osu.Game.Rulesets.Mania.Mods;
using Xunit;

namespace OsuLazer.Interop.Tests;

/// <summary>需要本机安装 lazer 客户端。fixture 是 mania 谱面，仅作为具体 ruleset 的样本。</summary>
public sealed class LazerStarRatingTests
{
    private static readonly string SampleBeatmapPath = Path.Combine(AppContext.BaseDirectory, "fixtures", "pupa.osu");
    private static readonly string SampleBeatmapContent = File.ReadAllText(SampleBeatmapPath);

    private const double ExpectedNM = 5.390204871167372;
    private const double ExpectedHT = 4.395036522598568;
    private const double ExpectedDT = 7.186750686166602;

    static LazerStarRatingTests()
    {
        LazerHost.TryInitialize(out _);
    }

    [Fact]
    public void CalcSR_WithRulesetMods()
    {
        var nm = LazerStarRatingCalculator.Default.CalcSR(SampleBeatmapPath, []);
        var ht = LazerStarRatingCalculator.Default.CalcSR(SampleBeatmapPath, [new ManiaModHalfTime()]);
        var dt = LazerStarRatingCalculator.Default.CalcSR(SampleBeatmapPath, [new ManiaModDoubleTime()]);

        Assert.Equal(ExpectedNM, nm, 12);
        Assert.Equal(ExpectedHT, ht, 12);
        Assert.Equal(ExpectedDT, dt, 12);
    }

    [Fact]
    public void CalcSRFromContent_WithApiMods()
    {
        var nm = LazerStarRatingCalculator.Default.CalcSRFromContent(SampleBeatmapContent, []);
        var ht = LazerStarRatingCalculator.Default.CalcSRFromContent(SampleBeatmapContent, [new APIMod { Acronym = "HT", Settings = new Dictionary<string, object> { ["speed_change"] = 0.75 } }]);
        var dt = LazerStarRatingCalculator.Default.CalcSRFromContent(SampleBeatmapContent, [new APIMod { Acronym = "DT", Settings = new Dictionary<string, object> { ["speed_change"] = 1.5 } }]);

        Assert.Equal(ExpectedNM, nm, 12);
        Assert.Equal(ExpectedHT, ht, 12);
        Assert.Equal(ExpectedDT, dt, 12);
    }
}
