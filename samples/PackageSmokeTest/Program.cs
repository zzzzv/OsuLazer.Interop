using System.Text.Json;
using OsuLazer.Interop;

Console.WriteLine($"lazer 安装: {LazerHost.IsInstalled} ({LazerHost.LazerDirectory}) 版本: {LazerHost.ClientVersion}");

if (!LazerHost.TryInitialize(out var error))
{
    Console.WriteLine($"初始化失败: {error}");
    return;
}

var database = new LazerDatabase();
Console.WriteLine($"realm: {database.ClientRealmPath} (存在: {database.Exists})");

if (!database.Exists)
    return;

var beatmaps = database.Query(LazerEntity.Beatmaps, "Ruleset.ShortName == \"mania\"", depth: 0);
Console.WriteLine($"mania 谱面: {beatmaps.Count}");

using (var realm = database.Open())
    Console.WriteLine($"Open() 直接查询: 共 {realm.All<osu.Game.Beatmaps.BeatmapInfo>().Count()} 张谱面");
Console.WriteLine(JsonSerializer.Serialize(beatmaps.FirstOrDefault(), new JsonSerializerOptions { WriteIndented = true }));

// 各模式通用性验证：只靠运行时解析出来的 ruleset，不需要引用对应的 ruleset 程序集
var fileStore = new LazerFileStore(database.DataDirectory);
Console.WriteLine();
foreach (var ruleset in new[] { "osu", "taiko", "fruits", "mania" })
{
    var items = database.Query(LazerEntity.Beatmaps, $"Ruleset.ShortName == \"{ruleset}\"", depth: 0);
    if (items.Count == 0)
    {
        Console.WriteLine($"{ruleset,-7} 本机没有该模式的谱面");
        continue;
    }

    var hash = ((IDictionary<string, object?>)items[0])["Hash"] as string;
    if (!fileStore.TryGetFilePath(hash, out var osuPath))
    {
        Console.WriteLine($"{ruleset,-7} 谱面文件缺失");
        continue;
    }

    var starRating = LazerStarRatingCalculator.Default.CalcSR(osuPath, []);
    Console.WriteLine($"{ruleset,-7} 共 {items.Count,6} 张  NM 星级={starRating,7:F3}");
}
