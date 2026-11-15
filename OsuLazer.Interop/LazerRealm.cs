using System.Reflection;
using Realms;
using osu.Game.Scoring;

namespace OsuLazer.Interop;

/// <summary>
/// 以与 lazer 客户端一致的 schema 版本打开 Realm。
/// schema 版本直接取自 lazer 自己的 <c>RealmAccess.schema_version</c>，避免硬编码。
/// </summary>
internal static class LazerRealm
{
    private static readonly Lazy<ulong> LazerSchemaVersion = new(ResolveLazerSchemaVersion);

    public static Realm Open(string clientRealmPath, bool readOnly = true)
    {
        var tempDir = LazerPaths.TempDirectory;
        Directory.CreateDirectory(tempDir);

        var config = new RealmConfiguration(clientRealmPath)
        {
            IsReadOnly = readOnly,
            SchemaVersion = LazerSchemaVersion.Value,
            FallbackPipePath = tempDir,
        };

        return Realm.GetInstance(config);
    }

    private static ulong ResolveLazerSchemaVersion()
    {
        var realmAccessType = typeof(ScoreInfo).Assembly.GetType("osu.Game.Database.RealmAccess")
            ?? throw new InvalidOperationException("找不到 osu.Game.Database.RealmAccess 类型，无法解析 lazer schema 版本。");

        var schemaVersionField = realmAccessType.GetField("schema_version", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("找不到 osu.Game.Database.RealmAccess.schema_version 字段，无法解析 lazer schema 版本。");

        var value = schemaVersionField.IsLiteral
            ? schemaVersionField.GetRawConstantValue()
            : schemaVersionField.GetValue(null);

        if (value is null)
            throw new InvalidOperationException("osu.Game.Database.RealmAccess.schema_version 值为 null。");

        return Convert.ToUInt64(value);
    }
}
