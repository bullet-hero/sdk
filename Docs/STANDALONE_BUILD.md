# Building the SDK without Unity

Split out of the SDK's root `CLAUDE.md`, which keeps the summary and links here.

The main `BH.SDK` asmdef is deliberately Unity-independent: `noEngineReferences: true`, depends only
on Newtonsoft.Json/Newtonsoft.Json.Bson (NuGet). Everything that touches `UnityEngine` lives in
separate, opt-in asmdefs - `UnityIntegration/` (**dual**: every file compiles with and without an
engine, `#if BHSDK_UNITY` choosing, so it ships in both builds) and `UnityExtensions/`
(**engine-only**, no such branch) - so the core model/serialization/rules code can be reused
standalone (external tooling, a future companion website, a server) without dragging Unity along.

## The independence is checked, not stated

Four `.csproj` files build this repo with no Unity anywhere, and every one of them is the same
sources Unity compiles - never a copy (the fourth is the exception that proves it: Unity must NOT
compile the Roslyn tests, hence the tilde):

| Project | Target | What it builds |
|---|---|---|
| `BH.SDK.csproj` | netstandard2.1 | The library: everything except `Roslyn/`, `Tests/` and `UnityExtensions/`. **`UnityIntegration/` is included WHOLE** - every file there is dual by contract (`#if BHSDK_UNITY`), and this build is what enforces it; see its `README.md` |
| `Tests/BH.SDK.Tests.csproj` | net8.0 | Every fixture the Unity Test Runner runs, under `dotnet test`, outside Unity. Its `Compile` include is RECURSIVE; while it was the folder root alone, `Tests/Rules` and `Tests/Services` were silently absent and the run reported a green 454 |
| `Roslyn/BH.SDK.Roslyn.csproj` | netstandard2.0 | The analyzers and generators - see `Roslyn/README.md` |
| `Roslyn/Tests~/BH.SDK.Roslyn.Tests.csproj` | net8.0 | Tests for the components themselves. **Invisible to Unity by the tilde**, and has to be - the asmdef above it would otherwise swallow the fixtures |

## Two numbers that are not choices

`netstandard2.1` and `LangVersion 9.0` are what the Unity project compiles with
(`apiCompatibilityLevel: 6`). Raising either would let in code that Unity then refuses.

`Directory.Build.props` redirects every project's output to `bin~`/`obj~`, because **Unity imports
any folder under `Assets/` that is not suffixed with a tilde** - a plain `bin/` hands the Editor a
second copy of every assembly here.
