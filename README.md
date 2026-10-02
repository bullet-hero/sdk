# bullet-hero-sdk

The open level and save data model of the game Bullet Hero: models, serialization (JSON and
`.blob`), validation, generators and level packages. It builds inside Unity
and, without it, as a `netstandard2.1` DLL and the NuGet package `BulletHero.SDK`.

**Documentation: https://bullethero.space/en/docs/sdk** - installing, the level format, the blob
format, archives, versioning, validation, publishing profiles, writing generators, contributing.

## Install

As a git submodule of a Unity project:

```bash
git submodule add -f https://github.com/bullet-hero/sdk.git Assets/Plugins/BH.SDK
```

Without Unity:

```bash
dotnet build -c Release BH.SDK.csproj   # bin~/Release/BH.SDK.dll
dotnet pack  -c Release BH.SDK.csproj   # bin~/Release/BulletHero.SDK.<version>.nupkg
```

## License

MIT, see `LICENSE`
