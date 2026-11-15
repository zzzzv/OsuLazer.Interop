using System.Diagnostics;

namespace OsuLazer.Interop;

/// <summary>
/// lazer 客户端在本机的位置约定。全部为只读探测，不涉及任何 osu.Game 类型，
/// 因此可以在程序集解析器注册之前安全调用。
/// </summary>
internal static class LazerPaths
{
    /// <summary>缺少其中任意一个就视为 lazer 不可用。</summary>
    private static readonly string[] RequiredAssemblies = ["osu.Game.dll", "Realm.dll"];

    /// <summary>lazer 客户端安装目录（<c>%LocalAppData%\osulazer\current</c>）。</summary>
    public static string LazerCurrentDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "osulazer",
        "current");

    /// <summary>lazer 用户数据目录（<c>%AppData%\osu</c>），realm 与 files 都在这里。</summary>
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "osu");

    /// <summary>Realm 打开只读库时使用的管道临时目录。</summary>
    public static string TempDirectory => Path.Combine(Path.GetTempPath(), "lazer");

    public static string DefaultClientRealmPath => Path.Combine(DataDirectory, "client.realm");

    public static bool IsInstalled => IsInstalledIn(LazerCurrentDirectory);

    public static bool IsInstalledIn(string directory) =>
        Directory.Exists(directory) && RequiredAssemblies.All(dll => File.Exists(Path.Combine(directory, dll)));

    /// <summary>返回第一个缺失的必需程序集；全部存在时返回 null。</summary>
    public static string? FindMissingAssembly(string directory) =>
        RequiredAssemblies.FirstOrDefault(dll => !File.Exists(Path.Combine(directory, dll)));

    /// <summary>
    /// 从 <c>osu.Game.dll</c> 的文件版本读取 lazer 客户端版本，不加载程序集。
    /// 用于让测试/fixture 记录“这份期望值是由哪个 lazer 版本产生的”。
    /// </summary>
    public static string? GetClientVersion(string directory)
    {
        try
        {
            var path = Path.Combine(directory, "osu.Game.dll");
            return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : null;
        }
        catch
        {
            return null;
        }
    }
}
