using System.Runtime.CompilerServices;

namespace OsuLazer.Interop;

/// <summary>
/// lazer 客户端的位置与初始化入口。
/// 不涉及任何 osu.Game 类型，可在程序启动最早期安全调用。
/// </summary>
public static class LazerHost
{
    /// <summary>lazer 客户端安装目录。</summary>
    public static string LazerDirectory => LazerPaths.LazerCurrentDirectory;

    /// <summary>lazer 用户数据目录（realm、files 所在处）。</summary>
    public static string DataDirectory => LazerPaths.DataDirectory;

    /// <summary>Realm 只读打开时使用的临时目录。</summary>
    public static string TempDirectory => LazerPaths.TempDirectory;

    /// <summary>默认的 <c>client.realm</c> 路径。</summary>
    public static string DefaultClientRealmPath => LazerPaths.DefaultClientRealmPath;

    /// <summary>默认安装目录下 lazer 是否可用（只探测文件，不做初始化）。</summary>
    public static bool IsInstalled => LazerPaths.IsInstalled;

    /// <summary>
    /// lazer 客户端版本（取自本机 <c>osu.Game.dll</c> 的文件版本），读不到时为 null。
    /// 测试/fixture 建议把它一起记录下来：SR 等结果会随 lazer 版本变化。
    /// </summary>
    public static string? ClientVersion => LazerPaths.GetClientVersion(LazerPaths.LazerCurrentDirectory);

    /// <summary>程序集解析器是否已绑定。</summary>
    public static bool IsInitialized => LazerAssemblyResolver.RegisteredDirectory is not null;

    /// <summary>已绑定的 lazer 目录；未初始化时为 null。</summary>
    public static string? InitializedLazerDirectory => LazerAssemblyResolver.RegisteredDirectory;

    public static bool IsInstalledIn(string? lazerDirectory) =>
        LazerPaths.IsInstalledIn(lazerDirectory ?? LazerPaths.LazerCurrentDirectory);

    /// <summary>绑定默认安装目录下的 lazer。重复调用安全。</summary>
    public static bool TryInitialize(out string? error) => TryInitialize(null, out error);

    /// <summary>绑定指定目录下的 lazer。重复调用同一目录安全；绑定到另一目录会失败。</summary>
    public static bool TryInitialize(string? lazerDirectory, out string? error) =>
        LazerAssemblyResolver.TryRegister(lazerDirectory, out error);

    /// <summary>供后续 API 兜底调用：未初始化则尝试初始化，失败抛出。</summary>
    internal static void EnsureInitialized()
    {
        if (IsInitialized)
            return;

        if (!TryInitialize(out var error))
            throw new InvalidOperationException($"osu!lazer 不可用：{error}", new FileNotFoundException(error));
    }
}

/// <summary>
/// 程序集加载时自动绑定本机 lazer（若存在），这样使用方无需任何显式初始化代码。
/// 必须早于任何 osu.Game 类型的 JIT 才有效，故用模块初始化器而不是懒加载。
/// </summary>
internal static class LazerAutoInitializer
{
    // 本库以源码形式编进使用方程序集，此时它就是"application code"，CA2255 不适用
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            if (LazerPaths.IsInstalled)
                LazerAssemblyResolver.TryRegister(null, out _);
        }
        catch
        {
            // 自动初始化失败不应影响宿主启动，后续调用会给出明确错误
        }
    }
#pragma warning restore CA2255
}
