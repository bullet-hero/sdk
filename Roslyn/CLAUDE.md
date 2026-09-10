# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Roslyn

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Roslyn/

the compile-time half: analyzers and incremental source generators, **running**
  since 2026-09-02 (they never had before: the gate `#if BHSDK_ROSLYN` was defined nowhere, and the
  `RoslynAnalyzer` label sat on the asmdef, which Unity ignores — it honours that label on a
  precompiled `.dll` alone). Its own `README.md` is the record; the shape in one line: the sources
  are compiled TWICE, by `BH.SDK.Roslyn.Src.asmdef` (Editor-only, referenced by nothing, purely
  so the IDE resolves them) and by `BH.SDK.Roslyn.csproj`, whose output — `BH.SDK.Roslyn.dll` in
  this folder's PARENT, i.e. the SDK root — is the only one Unity loads. **The artifact sits in the
  SDK root rather than in `Roslyn/`** because Unity scopes an analyzer to the asmdef owning its
  folder plus every assembly referencing it: inside `Roslyn/` it reached nothing, in the root it
  reaches `BH.SDK` and the ~25 assemblies built on it. The asmdef takes the `.Src` suffix
  because Unity refuses a plugin whose file name equals an asmdef's. `Refs/` holds the Editor's own
  Roslyn assemblies, installed rather than committed. **The build tooling ships with the SDK**, in
  `UnityExtensions/Editor/` (`BH.SDK.UnityExtensions.Editor`, Editor-only): `RoslynLayout` (every
  path, derived from this assembly's own asmdef location - nothing about the consuming project is
  hardcoded), `RoslynBuilder` (Tools > BH.SDK.Roslyn > Build Analyzer, shelling out to the `dotnet` Unity
  ships), `RoslynCompilerReferences` and `RoslynImportSettings`. It is deliberately specialised to
  `BH.SDK.Roslyn` rather than generic - one project, one artifact, one folder of references.
  **The components have their own tests, in `Roslyn/Tests~/`** (`BH.SDK.Roslyn.Tests`, net8.0,
  `dotnet test`) - what a generator EMITS and which diagnostics it reports, as opposed to `Tests/`,
  which covers what the generated code DOES. The tilde is load-bearing rather than decorative: an
  asmdef governs every subfolder under it, so a plain `Tests/` would compile NUnit fixtures into
  `BH.SDK.Roslyn.Src`, which declares `overrideReferences` with five precompiled references. Its
  harness compiles against a STUBBED model API rather than referencing `BH.SDK`, so a broken
  generator stays visible while the library itself is mid-refactor.
