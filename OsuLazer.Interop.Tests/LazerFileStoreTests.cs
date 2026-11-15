using Xunit;

namespace OsuLazer.Interop.Tests;

/// <summary>纯路径推导逻辑，不依赖 lazer 是否安装。</summary>
public sealed class LazerFileStoreTests
{
    private const string Hash = "378cc43278110713a4df5ef802184ecc195aeab29235a6c64b1e51c24bb2c461";

    private static string NewTempRoot() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetFilePath_UsesTwoLevelSharding()
    {
        var root = NewTempRoot();
        var store = new LazerFileStore(root);

        var expected = Path.Combine(root, "files", "3", "37", Hash);
        Assert.Equal(expected, store.GetFilePath(Hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("zzzzzz")]
    public void GetFilePath_RejectsInvalidHash(string hash)
    {
        var store = new LazerFileStore(NewTempRoot());

        Assert.Throws<ArgumentException>(() => store.GetFilePath(hash));
    }

    [Fact]
    public void TryGetFilePath_ReturnsFalseWhenFileMissing()
    {
        var store = new LazerFileStore(NewTempRoot());

        Assert.False(store.TryGetFilePath(Hash, out var path));
        Assert.Null(path);
    }

    [Fact]
    public void TryGetFilePath_ReturnsFalseForInvalidHash()
    {
        var store = new LazerFileStore(NewTempRoot());

        Assert.False(store.TryGetFilePath(null, out _));
        Assert.False(store.TryGetFilePath("ab", out _));
    }

    [Fact]
    public void TryGetFilePath_FindsExistingFile()
    {
        var root = NewTempRoot();
        var store = new LazerFileStore(root);
        var path = store.GetFilePath(Hash);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "x");

            Assert.True(store.TryGetFilePath(Hash, out var found));
            Assert.Equal(path, found);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
