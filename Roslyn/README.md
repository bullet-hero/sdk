# BH.SDK.Roslyn

The SDK's compile-time half: Roslyn **analyzers** (rules checked while your code compiles) and
**incremental source generators** (code written while your code compiles). One assembly, shipped as
`../BH.SDK.Roslyn.dll`.

## Why the layout looks the way it does

| Thing | Where | Why |
|---|---|---|
| Sources | `Analyzers/`, `Generators/` | Compiled twice on purpose - see below |
| Standalone build | `BH.SDK.Roslyn.csproj` | The ONLY producer of the shipping artifact |
| Compiler references | `Refs/` (gitignored) | Copied out of the installed Unity Editor |
| IDE-only assembly | `BH.SDK.Roslyn.Sources.asmdef` | Suffixed `.Sources`, referenced by nothing |
| Shipping artifact | `../BH.SDK.Roslyn.dll` | The SDK ROOT, and that is load-bearing |

**The sources are compiled twice, and only one of the two results is used.** Unity compiles them
through the asmdef so the IDE resolves types, reports errors and navigates - that assembly is used
by nothing. `dotnet build` on the csproj produces the artifact Unity actually loads as an analyzer.

**The asmdef's assembly is suffixed `.Sources` because Unity refuses the collision**: a plugin
`.dll` whose file name matches an asmdef's name gets *"Rename the assemblies to avoid hard to
diagnose issues and crashes"*. The shipped thing keeps the honest name.

**The artifact lives in the SDK ROOT, beside `BH.SDK.asmdef`, and that is what makes it run at
all.** Unity scopes an analyzer to the asmdef whose folder contains it **and to every assembly
referencing that asmdef** - measured, not assumed. Left inside `Roslyn/`, it belonged to
`BH.SDK.Roslyn.Sources`, which nothing references, so it analyzed nothing. In the SDK root it
belongs to `BH.SDK`, i.e. the SDK and the ~25 assemblies built on it.

**`Refs/` is not committed.** They are 13 MB of Microsoft binaries every machine already has, and
their version must follow whichever Editor is open. `Tools/Roslyn/Install Compiler References`
copies them out of `<Editor>/Data/Tools/BuildPipeline/Unity.Analyzers.Common`, which is by
definition the Roslyn the Editor's own compiler binds against; it also runs automatically on load
when they are missing. That tooling lives in **this repo**, at `../UnityExtensions/Editor/`
(`BH.SDK.UnityExtensions.Editor`), so the SDK carries its own build button wherever it is mounted -
it locates itself through its asmdef path and hardcodes nothing about the consuming project. Five files, not the fourteen next to them - the three an analyzer's surface
names, plus `System.Memory` and `System.Runtime.CompilerServices.Unsafe`, without which the
Editor's `System.Collections.Immutable` fails to bind with `CS0012`.

## Building

From the Editor: **Tools > Roslyn > Build Analyzer**. It shells out to the `dotnet` Unity ships
(`<Editor>/Data/DotNetSdk/dotnet.exe`), so nothing has to be installed, then imports the result and
applies the import settings (no platforms, `RoslynAnalyzer` label). Unity then recompiles every
assembly in scope - that is the point, not overhead.

Without an Editor - a build server, the standalone SDK repo:

```
# from Assets/Plugins/BulletHeroSDK/Roslyn
dotnet build BH.SDK.Roslyn.csproj -c Release
cp bin~/Release/BH.SDK.Roslyn.dll ../BH.SDK.Roslyn.dll
```

That is the whole recipe, and the two lines are what the menu item does - the second one is not
optional: `bin~/` is invisible to Unity, so a build that is not copied changes nothing. Inside the
Editor the copy is followed by an `ImportAsset` and the import settings (no platforms, the
`RoslynAnalyzer` label); by hand, the AssetPostprocessor applies the same on the next refresh.

With no Unity installed, the same command works with `dotnet` from anywhere - only the reference
resolution differs, see below.

With `Refs/` present the csproj binds against them; without, it restores
`Microsoft.CodeAnalysis.CSharp` from NuGet. Either way the output lands in `bin~/`, hidden from
Unity by the tilde (see `../Directory.Build.props`).

## Versions

Unity **6000.5.3f1** compiles with **Roslyn 4.10.0**. An analyzer may be built against that or
anything older, never newer - a component built against a newer Roslyn is silently not loaded.

## What is here now

- `Analyzers/RuleContainerAnalyzer.cs` - the real one: every `[RuleContainer]` class must be
  instantiatable, because `RuleNotNull` and the `RuleIPrimitiveXxx` family call
  `Activator.CreateInstance` on property types at runtime. It was written long ago and **had never
  run once** - it was `#if BHSDK_ROSLYN`-gated with that symbol defined nowhere, and its
  `RoslynAnalyzer` label sat on the asmdef, where Unity ignores it.
- `Analyzers/SandboxProbeAnalyzer.cs`, `Generators/SandboxProbeGenerator.cs` - a probe kept
  deliberately: `BHS0001` on any type named `*RoslynProbe`, and a generated `RoslynSandboxStamp`
  in every assembly in scope. Between them they answer "is Roslyn working right now" in one glance.
  Delete both once the real generators land.
