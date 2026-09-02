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
| IDE-only assembly | `BH.SDK.Roslyn.Src.asmdef` | Suffixed `.Src`, referenced by nothing |
| Shipping artifact | `../BH.SDK.Roslyn.dll` | The SDK ROOT, and that is load-bearing |
| Tests | `Tests~/` | Tilde: Unity must not compile them into the asmdef above |

**The sources are compiled twice, and only one of the two results is used.** Unity compiles them
through the asmdef so the IDE resolves types, reports errors and navigates - that assembly is used
by nothing. `dotnet build` on the csproj produces the artifact Unity actually loads as an analyzer.

**The asmdef's assembly is suffixed `.Src` because Unity refuses the collision**: a plugin
`.dll` whose file name matches an asmdef's name gets *"Rename the assemblies to avoid hard to
diagnose issues and crashes"*. The shipped thing keeps the honest name.

**The artifact lives in the SDK ROOT, beside `BH.SDK.asmdef`, and that is what makes it run at
all.** Unity scopes an analyzer to the asmdef whose folder contains it **and to every assembly
referencing that asmdef** - measured, not assumed. Left inside `Roslyn/`, it belonged to
`BH.SDK.Roslyn.Src`, which nothing references, so it analyzed nothing. In the SDK root it
belongs to `BH.SDK`, i.e. the SDK and the ~25 assemblies built on it.

**`Refs/` is not committed.** They are 13 MB of Microsoft binaries every machine already has, and
their version must follow whichever Editor is open. `Tools/BH.SDK.Roslyn/Install Compiler References`
copies them out of `<Editor>/Data/Tools/BuildPipeline/Unity.Analyzers.Common`, which is by
definition the Roslyn the Editor's own compiler binds against; it also runs automatically on load
when they are missing. That tooling lives in **this repo**, at `../UnityExtensions/Editor/`
(`BH.SDK.UnityExtensions.Editor`), so the SDK carries its own build button wherever it is mounted -
it locates itself through its asmdef path and hardcodes nothing about the consuming project. Five files, not the fourteen next to them - the three an analyzer's surface
names, plus `System.Memory` and `System.Runtime.CompilerServices.Unsafe`, without which the
Editor's `System.Collections.Immutable` fails to bind with `CS0012`.

## Building

From the Editor: **Tools > BH.SDK.Roslyn > Build Analyzer**. It shells out to the `dotnet` Unity ships
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
- `Tests~/` - the components' own tests, see below.

## Testing

`Tests~/BH.SDK.Roslyn.Tests.csproj` (net8.0, NUnit) tests the components THEMSELVES - what they
emit, which diagnostics they report, and whether they re-run when they should not. It is a different
question from `../Tests/`, which tests what the generated code DOES once emitted; a generator can
emit something that behaves correctly on every fixture and still re-run on every keystroke.

```
# from Assets/Plugins/BulletHeroSDK/Roslyn/Tests~
dotnet test BH.SDK.Roslyn.Tests.csproj -c Release
```

**The folder is suffixed with a tilde, and that is load-bearing.** An asmdef governs its folder and
every subfolder under it until another one appears, so a plain `Tests/` would compile these fixtures
into `BH.SDK.Roslyn.Src` - which declares `overrideReferences` with five precompiled references and
no NUnit. The tilde is the only mechanism Unity offers for "a real folder the Editor must not look
into" (the same reason `../Directory.Build.props` redirects `bin`/`obj`), so this project has no
`.asmdef` and no `.meta` files and never runs in the Unity Test Runner.

**They could not be in the Unity Test Runner even with an asmdef of their own, and that is the
second reason for the tilde.** Running a generator means hosting a COMPILER: the fixtures need
`Microsoft.CodeAnalysis.CSharp` at runtime and the analyzer assembly itself as an ordinary
reference. `BH.SDK.Roslyn.dll` is imported with the `RoslynAnalyzer` label and NO platform enabled
- not even Editor, which is exactly what makes Unity treat it as a compiler plugin rather than as
code - and an asmdef cannot reference an assembly that is enabled nowhere. Putting them in the Test
Runner would mean shipping a SECOND copy of the same dll as a plain Editor plugin, plus making the
13 MB of `Refs/` Editor-referencable, and then keeping the two copies in step. `dotnet test` is
where they belong; `../Tests/` is the suite that runs in both places.

`GeneratorHarness` builds a compilation from source strings and runs a `CSharpGeneratorDriver` over
it. **The model API it compiles against is a STUB, not a reference to `BH.SDK`**, because of when
these tests have to work: the refactor that puts `partial`/`sealed`/`[GenerateModel]` on 205 model
files leaves the library uncompilable for long stretches, which is exactly when a broken generator
has to stay visible. The drift that buys is paid for on the other side - `BH.SDK.Tests` exercises
the generated code against the real types.
