# bullet-hero-sdk
SDK for game Bullet Hero, for Unity on C#

### Dependencies
Depends only from Nuget, optional Unity-independent
- Newtonsoft.Json
- BouncyCastle.Cryptography (OpenPGP, for password-protected level packages)
- SharpZipLib (tar; gzip comes from the BCL)

### Level packages
A level is a folder of files, and a package is that folder made portable: `<name>.tar.gz`, or
`<name>.tar.gz.gpg` behind a passphrase, or a single `level.json.gpg` for a level protected where it
sits. Everything is an open standard on purpose - `tar -xzf` and `gpg -d` open all three, so a level
outlives the game that wrote it. `Services/Content` is what the whole layer addresses files through,
and it is rooted by construction, so nothing above it can reach outside the folder it was given.

### As a DLL, or a NuGet package
The same sources build without Unity, as `netstandard2.1`:

```
dotnet build -c Release BH.SDK.csproj   # bin~/Release/BH.SDK.dll (+ BH.SDK.xml docs)
dotnet pack  -c Release BH.SDK.csproj   # bin~/Release/BulletHero.SDK.<version>.nupkg
```

The package is `BulletHero.SDK`; its dependencies are the three NuGet packages above. Until the SDK
says otherwise, a version is the game's, not an API-stability promise - see `Docs/VERSIONING.md`.

`Samples~/ConsoleSmoke` is a `net8.0` console app that references the BUILT DLL, not the sources: it
reads a level folder, prints its name, object count and generation, round-trips the level through
JSON and through `.blob`, and exits non-zero on any mismatch (3) or on a file newer than the SDK (4).

```
dotnet build -c Release Samples~/ConsoleSmoke/ConsoleSmoke.csproj
dotnet Samples~/ConsoleSmoke/bin~/Release/net8.0/ConsoleSmoke.dll <level folder>
```

Against the game's builtin level `new-zero-demo` (2026-09-23, SDK 0.15.0):

```
BH.SDK 0.15.0, model generation 1
name:       New zero demo
objects:    782
generation: 1 (Blob)
round trip Json: equal (1505132 bytes)
round trip Blob: equal (680751 bytes)
exit 0
```

And against a copy of a level whose outermost `"g"` was raised to 2:

```
BH.SDK 0.15.0, model generation 1
refused: 'Level' is at generation 2, newer than this build's 1 (domain Level, file 2, this SDK 1) - update the SDK
exit 4
```

### Installation

How to install
```csharp
git submodule init
git submodule add -f https://github.com/bullet-hero/sdk.git Assets/Plugins/BulletHeroSDK
```

How to delete
```csharp
git rm -r -f Assets/Plugins/BulletHeroSDK
```
