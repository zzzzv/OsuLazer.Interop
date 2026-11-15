using Xunit;

namespace OsuLazer.Interop.Tests;

/// <summary>需要本机安装 lazer 客户端并且有 client.realm。</summary>
public sealed class LazerDatabaseTests
{
    [Fact]
    public void Host_IsInstalledAndInitialized()
    {
        Assert.True(LazerHost.IsInstalled, $"未找到 lazer: {LazerHost.LazerDirectory}");
        Assert.True(LazerHost.TryInitialize(out var error), error);
        Assert.True(LazerHost.IsInitialized);
        Assert.Equal(Path.GetFullPath(LazerHost.LazerDirectory), LazerHost.InitializedLazerDirectory);
        Assert.False(string.IsNullOrWhiteSpace(LazerHost.ClientVersion));
    }

    [Fact]
    public void Database_PointsAtExistingRealm()
    {
        var database = new LazerDatabase();

        Assert.True(database.Exists, $"未找到 client.realm: {database.ClientRealmPath}");
        Assert.Equal(Path.GetDirectoryName(database.ClientRealmPath), database.DataDirectory);
    }

    [Fact]
    public void Query_Collections_ReturnsPlainObjects()
    {
        var database = new LazerDatabase();
        Assert.True(database.Exists, $"未找到 client.realm: {database.ClientRealmPath}");

        var items = database.Query(LazerEntity.Collections, "TRUEPREDICATE", depth: 0);

        Assert.All(items, item => Assert.IsAssignableFrom<IDictionary<string, object?>>(item));
    }

    [Fact]
    public void Query_EmptyRql_Throws()
    {
        var database = new LazerDatabase();

        Assert.Throws<ArgumentException>(() => database.Query(LazerEntity.Beatmaps, "  "));
    }
}
