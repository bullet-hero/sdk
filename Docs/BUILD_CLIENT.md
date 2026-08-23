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
