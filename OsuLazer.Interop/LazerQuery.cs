using System.Collections;
using Realms;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Scoring;

namespace OsuLazer.Interop;

/// <summary>
/// 所有引用 osu.Game 类型的查询逻辑集中在这里，
/// 由 <see cref="LazerDatabase"/>（不含 osu.Game 类型的方法）先完成初始化后再转发进来。
/// </summary>
internal static class LazerQuery
{
    public static IReadOnlyList<object> Execute(
        string clientRealmPath,
        LazerEntity entity,
        string rql,
        int depth,
        IReadOnlySet<string>? noExpandFields)
    {
        if (string.IsNullOrWhiteSpace(rql))
            throw new ArgumentException("RQL 查询字符串不能为空。", nameof(rql));

        using var realm = LazerRealm.Open(clientRealmPath);

        IEnumerable items = entity switch
        {
            LazerEntity.Scores => realm.All<ScoreInfo>().Filter(rql),
            LazerEntity.Beatmaps => realm.All<BeatmapInfo>().Filter(rql),
            LazerEntity.BeatmapSets => realm.All<BeatmapSetInfo>().Filter(rql),
            LazerEntity.Collections => realm.All<BeatmapCollection>().Filter(rql),
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "未知的 lazer 实体。"),
        };

        return LazerRealmConverter.ToList(items, depth, noExpandFields);
    }
}
