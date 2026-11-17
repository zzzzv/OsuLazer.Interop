# OsuLazer.Interop

读取本机 osu!lazer 客户端的 Realm 数据库与文件存储，并用 lazer 自己的 ruleset 计算难度星级。所有模式通用：ruleset 由谱面自身决定。

[English](README.md)

## 前置条件

- **本机必须安装 lazer 客户端**：编译期直接引用 `%LocalAppData%\osulazer\current` 下的 `osu.Game.dll` / `osu.Framework.dll` / `Realm.dll`，缺失时构建报 `OSULAZER001`。其他 ruleset 程序集在运行时按需加载，无需在此引用。
- 目标框架 **net10.0 或更高**（lazer 是 net10 程序集，net8 项目引用不了）。
- 非默认安装目录时覆盖路径：

```xml
<OsuLazerCurrentDir>D:\osulazer\current</OsuLazerCurrentDir>
```

## 安装

```powershell
dotnet add package OsuLazer.Interop
```

或直接写进工程：

```xml
<PackageReference Include="OsuLazer.Interop" Version="0.1.2" />
```

包是 **source-only** 的：不含 dll，源码会编进你自己的程序集。因此不存在二进制版本兼容问题，但要求构建机装好 lazer（见上）。

手写 `PackageReference` 时建议带上 `PrivateAssets="all"`，避免它继续作为依赖传递给引用本项目的工程。用 `dotnet add package` 安装时 NuGet 会自动加上（包在 nuspec 里标了 `developmentDependency`）：

```xml
<PackageReference Include="OsuLazer.Interop" Version="0.1.2" PrivateAssets="all" />
```

本地改库调试时可直接引用项目（同机相邻目录）：

```xml
<Import Project="..\..\OsuLazer.Interop\build\OsuLazer.Interop.props" />

<ItemGroup>
  <ProjectReference Include="..\..\OsuLazer.Interop\OsuLazer.Interop\OsuLazer.Interop.csproj" />
</ItemGroup>
```

## 用法

```csharp
using OsuLazer.Interop;

var database = new LazerDatabase();                    // 默认 %AppData%\osu\client.realm
var beatmaps = database.Query(LazerEntity.Beatmaps, "Ruleset.ShortName == \"mania\"", depth: 1);

var store = new LazerFileStore(database.DataDirectory);
if (store.TryGetFilePath(hash, out var osuPath))
    Console.WriteLine(osuPath);

var starRating = LazerStarRatingCalculator.Default.CalcSR(osuPath, []);   // 任意模式，mod 由调用方给
```

不需要写初始化代码：程序集加载时会自动绑定本机 lazer。只有「启动时没有 lazer、之后才安装」这种情况才需要 `LazerHost.TryInitialize(out var error)`；该绑定是进程级、一次性的，先绑定的目录生效。

结果（尤其是 SR）随本机 lazer 版本变化，生成 fixture 或写断言时请用 `LazerHost.ClientVersion` 一并记录版本。

## API

| 类型 | 说明 |
|---|---|
| `LazerHost` | 安装路径、可用性探测、初始化（`TryInitialize` / `IsInstalled` / `IsInitialized` / `ClientVersion`） |
| `LazerDatabase` | `Query`（RQL）/ `Open`（原始 Realm）/ `AddToCollection` / `WriteStarRatings` |
| `LazerFileStore` | `files/a/bc/<hash>` 路径解析与存在性判断 |
| `LazerStarRatingCalculator` | `CalcSR`（谱面路径 + mod 实例）/ `CalcSRFromContent`（.osu 内容 + APIMod） |
