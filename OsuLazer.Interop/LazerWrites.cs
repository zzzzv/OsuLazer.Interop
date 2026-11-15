using Realms;
using osu.Game.Beatmaps;
using osu.Game.Collections;

namespace OsuLazer.Interop;

/// <summary>
/// 写入 lazer 数据库。与 <see cref="LazerQuery"/> 同理，集中在引用 osu.Game 类型的地方，
/// 由 <see cref="LazerDatabase"/> 先完成初始化再转发。
/// </summary>
internal static class LazerWrites
{
    public static CollectionUpdateResult AddToCollection(
        string clientRealmPath,
        string name,
        IReadOnlyList<string> beatmapMd5Hashes,
        bool overwrite)
    {
        using var realm = LazerRealm.Open(clientRealmPath, readOnly: false);

        bool wasNew = false;
        realm.Write(() =>
        {
            var existing = realm.All<BeatmapCollection>().FirstOrDefault(c => c.Name == name);

            if (existing is not null)
            {
                wasNew = false;

                if (overwrite)
                {
                    existing.BeatmapMD5Hashes.Clear();
                    foreach (var hash in beatmapMd5Hashes)
                        existing.BeatmapMD5Hashes.Add(hash);
                }
                else
                {
                    // 追加模式：合并去重
                    foreach (var hash in beatmapMd5Hashes)
                    {
                        if (!existing.BeatmapMD5Hashes.Contains(hash))
                            existing.BeatmapMD5Hashes.Add(hash);
                    }
                }

                existing.LastModified = DateTimeOffset.UtcNow;
            }
            else
            {
                wasNew = true;
                realm.Add(new BeatmapCollection(name, beatmapMd5Hashes.ToList()));
            }
        });

        var created = realm.All<BeatmapCollection>().First(c => c.Name == name);
        return new CollectionUpdateResult(name, created.BeatmapMD5Hashes.Count, wasNew);
    }

    public static int WriteStarRatings(string clientRealmPath, IReadOnlyDictionary<string, double> starRatings)
    {
        using var realm = LazerRealm.Open(clientRealmPath, readOnly: false);

        int updated = 0;
        realm.Write(() =>
        {
            foreach (var beatmap in realm.All<BeatmapInfo>())
            {
                if (starRatings.TryGetValue(beatmap.MD5Hash, out var starRating))
                {
                    beatmap.StarRating = starRating;
                    updated++;
                }
            }
        });

        return updated;
    }
}
