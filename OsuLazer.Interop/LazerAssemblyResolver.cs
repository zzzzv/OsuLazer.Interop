using System.Reflection;
using System.Runtime.Loader;

namespace OsuLazer.Interop;

/// <summary>
/// 把 lazer 安装目录接进默认 <see cref="AssemblyLoadContext"/> 的解析链。
/// 这是进程级、一次性的副作用：先注册者决定目录，后续注册不会改绑。
/// </summary>
internal static class LazerAssemblyResolver
{
    private static readonly object Gate = new();
    private static string? registeredDirectory;

    /// <summary>当前已绑定的 lazer 目录，未注册时为 null。</summary>
    public static string? RegisteredDirectory => Volatile.Read(ref registeredDirectory);

    /// <summary>
    /// 注册 lazer 目录。<paramref name="directory"/> 为 null 时使用默认安装路径。
    /// 重复注册同一目录视为成功。
    /// </summary>
    public static bool TryRegister(string? directory, out string? error)
    {
        directory = Path.GetFullPath(directory ?? LazerPaths.LazerCurrentDirectory);

        lock (Gate)
        {
            if (registeredDirectory is not null)
            {
                if (string.Equals(registeredDirectory, directory, StringComparison.OrdinalIgnoreCase))
                {
                    error = null;
                    return true;
                }

                error = $"程序集解析器已绑定到 {registeredDirectory}，无法改绑到 {directory}。";
                return false;
            }

            if (!Directory.Exists(directory))
            {
                error = $"lazer 目录不存在: {directory}";
                return false;
            }

            var missing = LazerPaths.FindMissingAssembly(directory);
            if (missing is not null)
            {
                error = $"lazer 目录缺少 {missing}: {directory}";
                return false;
            }

            registeredDirectory = directory;
            AssemblyLoadContext.Default.Resolving += ResolveFromLazerDirectory;
            PreloadRulesetAssemblies(directory);

            error = null;
            return true;
        }
    }

    private static void PreloadRulesetAssemblies(string directory)
    {
        foreach (var path in Directory.EnumerateFiles(directory, "osu.Game.Rulesets*.dll", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(path);

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                    string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            try
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            catch
            {
                // 版本不匹配的 ruleset 跳过即可，不影响主流程
            }
        }
    }

    private static Assembly? ResolveFromLazerDirectory(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        var directory = registeredDirectory;
        if (directory is null || !Directory.Exists(directory))
            return null;

        // 1) 优先用 lazer 目录里的版本
        var dllPath = TryFindDll(directory, assemblyName.Name);
        if (dllPath is not null)
        {
            try
            {
                return context.LoadFromAssemblyPath(dllPath);
            }
            catch
            {
                // 版本不匹配等错误 — 继续尝试回退
            }
        }

        // 2) 回退：已有同名程序集已加载（例如来自 NuGet 包的不同版本）
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }

        return null;
    }

    private static string? TryFindDll(string directory, string? assemblyName)
    {
        if (assemblyName is null)
            return null;

        var directPath = Path.Combine(directory, $"{assemblyName}.dll");
        if (File.Exists(directPath))
            return directPath;

        return Directory
            .EnumerateFiles(directory, $"{assemblyName}.dll", SearchOption.AllDirectories)
            .FirstOrDefault();
    }
}
