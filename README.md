# bullet-hero-sdk
SDK for game Bullet Hero, for Unity on C#

### Dependencies
Depends only from Nuget, optional Unity-independent
- Newtonsoft.Json
- Newtonsoft.Json.Bson
- BouncyCastle.Cryptography (OpenPGP, for password-protected level packages)
- SharpZipLib (tar; gzip comes from the BCL)

### Level packages
A level is a folder of files, and a package is that folder made portable: `<name>.tar.gz`, or
`<name>.tar.gz.gpg` behind a passphrase, or a single `level.json.gpg` for a level protected where it
sits. Everything is an open standard on purpose - `tar -xzf` and `gpg -d` open all three, so a level
outlives the game that wrote it. `Services/Content` is what the whole layer addresses files through,
and it is rooted by construction, so nothing above it can reach outside the folder it was given.

### Installation

How to install
```csharp
git submodule init
git submodule add -f https://github.com/vertoker/bullet-hero-sdk.git Assets/Plugins/BulletHeroSDK
```

How to delete
```csharp
git rm -r -f Assets/Plugins/BulletHeroSDK
```
