Bullet Hero (`BH`) can't have _All in One_ build, each _platform_ and _distribution channel_ in combination
requires build with right configuration of packages, plugins, resources, code and everything related

Originally it's internal Bullet Hero client file, but this information must be public.
All of this information applied only to game itself, `BH.SDK` use different file for specification (`BUILD_SDK.md`)

### Platforms
- Windows (`WIN`)
- Linux (`LINUX`)
- macOS (`MACOS`)
- Android (`ANDROID`)
- iOS (`IOS`)

For now, consoles and servers is not required (and not reachable)

### Distribution Channels
- Independent (`FREE`) - BH site, GitHub, torrents
- Steam (`STEAM`)
- Google Play (`GOOGLEPLAY`)
- App Store (`APPSTORE`)
- VK Play (`VKPLAY`)
- RuStore (`RUSTORE`)

### Symbols
For each build need to define 3 symbols
- Only platform specific: `BH_BUILD_<Platform>`
- Only distribution channel specific: `BH_BUILD_<Distribution Channel>`
- Both specifics: `BH_BUILD_<Platform>_<Distribution Channel>`

What builds need to be created
- On Alpha
  - Windows, Independent (`BH_BUILD_WIN`, `BH_BUILD_FREE`, `BH_BUILD_WIN_FREE`)
  - Windows, Steam (`BH_BUILD_WIN`, `BH_BUILD_STEAM`, `BH_BUILD_WIN_STEAM`)
  - Windows, VK Play (`BH_BUILD_WIN`, `BH_BUILD_VKPLAY`, `BH_BUILD_WIN_VKPLAY`)
  - Android, Independent (`BH_BUILD_ANDROID`, `BH_BUILD_FREE`, `BH_BUILD_ANDROID_FREE`)
  - Android, Google Play (`BH_BUILD_ANDROID`, `BH_BUILD_GOOGLEPLAY`, `BH_BUILD_ANDROID_GOOGLEPLAY`)
  - Android, RuStore (`BH_BUILD_ANDROID`, `BH_BUILD_RUSTORE`, `BH_BUILD_ANDROID_RUSTORE`)
- On Release
  - Linux, Independent (`BH_BUILD_LINUX`, `BH_BUILD_FREE`, `BH_BUILD_LINUX_FREE`)
  - Linux, Steam (`BH_BUILD_LINUX`, `BH_BUILD_STEAM`, `BH_BUILD_LINUX_STEAM`)
  - iOS, App Store (`BH_BUILD_IOS`, `BH_BUILD_APPSTORE`, `BH_BUILD_IOS_APPSTORE`)
  - macOS, Steam (`BH_BUILD_MACOS`, `BH_BUILD_STEAM`, `BH_BUILD_MACOS_STEAM`)
- In Future
  - macOS, App Store (`BH_BUILD_MACOS`, `BH_BUILD_APPSTORE`, `BH_BUILD_MACOS_APPSTORE`)
- Questionable
  - macOS, Independent (`BH_BUILD_MACOS`, `BH_BUILD_FREE`, `BH_BUILD_MACOS_FREE`)
  - Any console platforms support
  - Any chinese distribution support

### Build variants

Each platform-channel pair ships **two** Build Profiles, `<PLATFORM>_<CHANNEL>_Release` and
`<PLATFORM>_<CHANNEL>_Debug`. The variant is not a third axis of the specification: it says how a
build was *compiled*, not which build it *is*, so there is deliberately **no fourth symbol** — Unity
already defines `DEVELOPMENT_BUILD`, and `Debug.isDebugBuild` already answers the only question
anyone asks at runtime.

A variant moves exactly two things:

|  | Release | Debug |
|---|---|---|
| `BH_USE_BURST` | defined | not defined |
| Development Build | off | on |

- **`BH_USE_BURST` is the load-bearing half.** Every `[BurstCompile]` in the project sits behind it,
  so a build without it runs the managed path throughout — the bot's clearance rasterizer costs
  22 ms a tick that way against 0.6 ms Bursted. Release defines it; that is what makes a shipped
  build a Bursted one. Debug does not, because Burst-compiled code is what a debugger and a profiler
  cannot step into.
- **It is not in Player Settings**, so the Editor, Play mode and the test suites all run Burst-off —
  the configuration the game is developed and debugged in. Only a build carries it, and only through
  its profile.
- **Every profile also repeats the project-wide symbols** (`UNITEXT`, `BHSDK_UNITY`) alongside its
  own three. Whether Unity appends a profile's defines to Player Settings' or replaces them is not
  documented for Unity 6.5; carrying the full set is correct either way, and it makes a profile
  readable on its own.

The profiles are generated, never hand-written — `BuildProfileGeneratorScriptable` in
`Assets/Settings/Build/` writes one pair per `BuildSpec` asset into `Assets/Settings/Build Profiles/`,
and re-running it rewrites only the defines and the Development Build flag, leaving every other
setting on a profile alone. A platform whose Editor module is not installed is skipped and named in
the report rather than failing the run.

Building **without** the three symbols is a warning on a development build and a hard failure on any
other: a development binary built straight off a classic platform is an ordinary thing to make while
working, a store binary compiled as another store's branch is not.

Export compliance
- The game ships encryption and every storefront asks about it. A level package can be written
  password-protected (`.tar.gz.gpg`) and a level's own document can sit encrypted on disk
  (`level.json.gpg`); both are OpenPGP symmetric - AES-256 keyed by PBKDF2-style S2K over SHA-256,
  from `BouncyCastle.Cryptography`. Nothing here is proprietary or export-restricted in itself, but
  it must be declared rather than left unanswered:
  - **App Store** - `ITSAppUsesNonExemptEncryption` in `Info.plist`. AES used only to protect the
    user's own content normally qualifies for the exemption; answering the question is still
    mandatory, and answering it wrong is what delays a review.
  - **Google Play** - the US export-law declaration on the app content page.
  - **Consoles** (Switch, PlayStation, Xbox) - each publishing portal asks the same question in its
    own form when those platforms are taken on.
  - **Chinese storefronts** - encryption rules there are their own subject and have to be checked
    per store before any of them is targeted.
