# BH.SDK.UnityIntegration

The thin layer where the SDK is *allowed* to touch `UnityEngine` — and the only folder in this repo
that ships in **both** builds of the library.

## The contract

**Every script in this folder is DUAL, and that is a guarantee rather than a habit.** Each file
compiles two ways:

- **inside Unity**, as the `BH.SDK.UnityIntegration` assembly, with `BHSDK_UNITY` defined (Player
  Settings define it) and `UnityEngine` available;
- **outside Unity**, compiled straight into `BH.SDK.dll` by `../BH.SDK.csproj`, with `BHSDK_UNITY`
  undefined and no engine anywhere.

So a file here **may** call into `UnityEngine`, but only behind `#if BHSDK_UNITY`, and it **must**
answer the same question the other way when the symbol is absent. `Cat` is the worked example: one
logger, `Debug.Log` under Unity and `Console.WriteLine` without it.

Two rules follow from that, both mechanical:

1. **`using UnityEngine;` goes inside the `#if` too.** A using directive at the top of the file is
   itself engine-dependent code — `Cat.cs` carried both branches for its bodies and an unguarded
   using above them, which meant it had never once compiled outside Unity despite looking like it
   could.
2. **Nothing here may appear in a public signature that the core SDK exposes.** The core is
   `noEngineReferences`, so a `Vector2` on a public member would be a type the engine-free build
   cannot even name.

**The contract is CHECKED, not promised.** `BH.SDK.csproj` compiles this folder without
`BHSDK_UNITY`, so a file that breaks the rule fails that build — and `Tests/BH.SDK.Tests.csproj`
runs the suite against the result.

## What is NOT here

`UnityExtensions/` is the other half and makes the opposite promise: it needs `UnityEngine`,
`Unity.Mathematics` and `Unity.Collections` unconditionally, has no engine-free branch, and is
excluded from `BH.SDK.csproj` entirely. Transform maths, `RectTransform2D`, `AvatarMovement` and the
`Texture2D` conversions live there. When a thing *could* be written either way, it belongs here;
when it is only meaningful with an engine, it belongs there.

## Files

- `Cat.cs` — the SDK's logger. Named for what it does to the console.
- `AssemblyInfo.cs` — `InternalsVisibleTo` for the Unity assemblies. **Excluded** from
  `BH.SDK.csproj`: merging two assemblies' AssemblyInfo into one would declare the same
  `InternalsVisibleTo` twice (`CS0579`), and the root `AssemblyInfo.cs` already names everyone.
