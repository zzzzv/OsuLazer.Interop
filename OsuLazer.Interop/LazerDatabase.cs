using Realms;

namespace OsuLazer.Interop;

/// <summary>
/// lazer 的 <c>client.realm</c> 访问入口。
/// 本类型刻意不引用任何 osu.Game 类型，以便在程序集解析器注册前也能安全被 JIT。
/// </summary>
public sealed class LazerDatabase
{
    public LazerDatabase(string? clientRealmPath = null)
    {
        ClientRealmPath = clientRealmPath ?? LazerHost.DefaultClientRealmPath;
    }

    /// <summary>正在访问的 realm 文件路径。</summary>
    public string ClientRealmPath { get; }

    /// <summary>realm 文件是否存在。</summary>
    public bool Exists => File.Exists(ClientRealmPath);

    /// <summary>lazer 用户数据目录（realm 所在目录，<c>files/</c> 也在这里）。</summary>
    public string DataDirectory => Path.GetDirectoryName(ClientRealmPath) ?? LazerHost.DataDirectory;

    /// <summary>
    /// 打开原始 Realm 实例，用于库未封装的查询。实例为线程内独占，用完请 dispose。
    /// </summary>
    public Realm Open(bool readOnly = true)
    {
        LazerHost.EnsureInitialized();
        return LazerRealm.Open(ClientRealmPath, readOnly);
    }

    /// <summary>
    /// 以 RQL 查询指定实体，返回可序列化的对象列表。
    /// </summary>
    /// <param name="entity">实体类型。</param>
    /// <param name="rql">Realm 查询字符串，例如 <c>"Ruleset.ShortName == \"mania\""</c>。</param>
    /// <param name="depth">展开深度，0 表示不展开引用对象。</param>
    /// <param name="noExpandFields">即使 depth 足够也不展开的字段名。</param>
    public IReadOnlyList<object> Query(LazerEntity entity, string rql, int depth = 0, IReadOnlySet<string>? noExpandFields = null)
    {
        LazerHost.EnsureInitialized();
        return LazerQuery.Execute(ClientRealmPath, entity, rql, depth, noExpandFields);
    }

    /// <summary>创建或更新收藏夹。</summary>
    public CollectionUpdateResult AddToCollection(string name, IReadOnlyList<string> beatmapMd5Hashes, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("收藏夹名称不能为空。", nameof(name));

        if (!Exists)
            throw new FileNotFoundException($"未找到 client.realm: {ClientRealmPath}", ClientRealmPath);

        LazerHost.EnsureInitialized();
        return LazerWrites.AddToCollection(ClientRealmPath, name, beatmapMd5Hashes, overwrite);
    }

    /// <summary>按 MD5 批量写入星级（lazer 每个谱面只有一个 StarRating 字段）。</summary>
    public int WriteStarRatings(IReadOnlyDictionary<string, double> starRatings)
    {
        ArgumentNullException.ThrowIfNull(starRatings);

        if (!Exists)
            throw new FileNotFoundException($"未找到 client.realm: {ClientRealmPath}", ClientRealmPath);

        LazerHost.EnsureInitialized();
        return LazerWrites.WriteStarRatings(ClientRealmPath, starRatings);
    }
}
