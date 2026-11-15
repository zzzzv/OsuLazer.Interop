# OsuLazer.Interop

Read the local osu!lazer client's Realm database and file store, and compute difficulty star ratings using lazer's own rulesets. Works with every ruleset — the ruleset is taken from each beatmap.

[中文文档](https://github.com/zzzzv/OsuLazer.Interop/blob/master/README.zh-CN.md)

## Requirements

- **osu!lazer must be installed locally.** The library references `osu.Game.dll`, `osu.Framework.dll` and `Realm.dll` from `%LocalAppData%\osulazer\current` at compile time; the build fails with `OSULAZER001` if they are missing. Other ruleset assemblies are loaded from that directory at runtime and need no reference.
- Target framework **net10.0 or later** (lazer is a net10 assembly, so a net8 project cannot reference it).
- Override the location if lazer is installed somewhere else:

```xml
<OsuLazerCurrentDir>D:\osulazer\current</OsuLazerCurrentDir>
```

## Installation

```powershell
dotnet add package OsuLazer.Interop
```

Or reference it directly:

```xml
<PackageReference Include="OsuLazer.Interop" Version="0.1.1" />
```

The package is **source-only**: it contains no dll, the sources are compiled into your own assembly. There is no binary compatibility problem as a result, but the build machine still needs lazer installed (see above).

When writing the `PackageReference` by hand, prefer adding `PrivateAssets="all"` so the package is not propagated to projects referencing yours. `dotnet add package` adds it automatically, because the package is marked as a development dependency:

```xml
<PackageReference Include="OsuLazer.Interop" Version="0.1.1" PrivateAssets="all" />
```

To hack on the library itself, reference the project instead (sibling directory on the same machine):

```xml
<Import Project="..\..\OsuLazer.Interop\build\OsuLazer.Interop.props" />

<ItemGroup>
  <ProjectReference Include="..\..\OsuLazer.Interop\OsuLazer.Interop\OsuLazer.Interop.csproj" />
</ItemGroup>
```

## Usage

```csharp
using OsuLazer.Interop;

var database = new LazerDatabase();                    // defaults to %AppData%\osu\client.realm
var beatmaps = database.Query(LazerEntity.Beatmaps, "Ruleset.ShortName == \"mania\"", depth: 1);

var store = new LazerFileStore(database.DataDirectory);
if (store.TryGetFilePath(hash, out var osuPath))
    Console.WriteLine(osuPath);

var starRating = LazerStarRatingCalculator.Default.CalcSR(osuPath, []);   // any ruleset; mods come from the caller
```

No initialization code is needed: the local lazer install is bound automatically when the assembly is loaded. Call `LazerHost.TryInitialize(out var error)` only when lazer is installed after startup; the binding is process-wide and one-shot, the first directory wins.

Star ratings (and anything else derived from lazer) change with the installed lazer version — record `LazerHost.ClientVersion` alongside generated fixtures or test expectations.

## API

| Type | Description |
|---|---|
| `LazerHost` | Install location, availability probing, initialization (`TryInitialize` / `IsInstalled` / `IsInitialized` / `ClientVersion`) |
| `LazerDatabase` | `Query` (RQL) / `Open` (raw Realm) / `AddToCollection` / `WriteStarRatings` |
| `LazerFileStore` | Path resolution and existence checks for `files/a/bc/<hash>` |
| `LazerStarRatingCalculator` | `CalcSR` (beatmap path + mod instances) / `CalcSRFromContent` (.osu content + APIMod) |
