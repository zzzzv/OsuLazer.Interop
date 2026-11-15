using System.Diagnostics.CodeAnalysis;

namespace OsuLazer.Interop;

/// <summary>
/// lazer 的文件存储（<c>&lt;data&gt;\files\a\bc\abcdef...</c>）。
/// </summary>
public sealed class LazerFileStore
{
    public LazerFileStore(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? LazerHost.DataDirectory;
    }

    /// <summary>lazer 用户数据目录。</summary>
    public string DataDirectory { get; }

    /// <summary>由哈希推导文件路径（不检查存在性）。</summary>
    public string GetFilePath(string hash)
    {
        if (!IsValidHash(hash))
            throw new ArgumentException("无效的文件哈希。", nameof(hash));

        return Path.Combine(DataDirectory, "files", hash[..1], hash[..2], hash);
    }

    /// <summary>尝试获取哈希对应的文件路径，文件不存在时返回 false。</summary>
    public bool TryGetFilePath(string? hash, [NotNullWhen(true)] out string? filePath)
    {
        filePath = null;

        if (!IsValidHash(hash))
            return false;

        var path = Path.Combine(DataDirectory, "files", hash![..1], hash[..2], hash);
        if (!File.Exists(path))
            return false;

        filePath = path;
        return true;
    }

    private static bool IsValidHash([NotNullWhen(true)] string? hash) =>
        !string.IsNullOrWhiteSpace(hash) && hash.Length >= 3 && hash.All(Uri.IsHexDigit);
}
