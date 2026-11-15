namespace OsuLazer.Interop;

/// <summary>可查询的 lazer 实体。</summary>
public enum LazerEntity
{
    Scores,
    Beatmaps,
    BeatmapSets,
    Collections,
}

/// <summary>收藏夹写入结果。</summary>
public sealed record CollectionUpdateResult(string Name, int BeatmapCount, bool Created);
