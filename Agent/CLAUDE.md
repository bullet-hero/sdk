# CLAUDE.md - `Assets/Plugins/BH.SDK/Agent`

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

The pure half of the level-editing bridge an MCP client drives the running editor through. The plan is
`Docs/Plans/LEVEL_MCP_PLAN.md` in the game repo. Nothing here is a level or save model: no
`[GenerateModel]`, no `[ModelGeneration]`, no snapshot, no migrator, so nothing here bumps a version.

## Invariants

- **The game and the server (`Mcp~/`, net8) both compile this folder**, so it stays BCL + Newtonsoft (+ SharpZipLib
  for `Imaging/`'s zlib), netstandard2.1 and C# 9, no `UnityEngine`. The server project is excluded from `BH.SDK.csproj` by its tilde name.
- **DTOs in `Messages/` are plain Newtonsoft classes, each member under an explicit camelCase
  `[JsonProperty]`.** Not `Names.cs` and not the file's short keys: these never reach a level file, and a
  wire name is read by an AI, so it is spelled out.
- **A time is a number or a string, and `TimeAddress` is its only reader.** A frame counts from
  `FrameRules.MinFrame`; a duration is a count, with no origin. `s:` takes the same float arithmetic as
  Shared's `FrameUtils.ToFrame`/`ToFrameCount`; change one and the other moves with it.
- **Musical times resolve only through a `TimeAddressContext`** (a `MusicGrid` plus a marker lookup the
  caller builds); `MusicGrid` places beats exactly as `BeatMath` does. The convention: `Docs/MUSIC.md`.
- **The value codec finds implementations by reflection** (`AgentValueCodec.Families`), so a new value type
  is covered by the every-implementation round-trip test the day it is added. A NEW FAMILY is the one edit it
  needs: add the interface to `Families`.
- **Colours are 8 bits on the wire** (`"#RRGGBBAA"`, `"#RRGGBB"`), so a float colour that is not a multiple of
  1/255 does not round-trip exactly. Theme references are `{ "theme": n }`.
- **`Keyframe.Frame` is neither written nor read by the codec.** The AI speaks global frames, the file holds
  local ones, and the conversion belongs to the handler that knows the owning span.
- **Music DTOs (`Messages/MusicMessages.cs`) carry frames, never raw envelopes or onsets**; the analysis itself is
  `Services/AudioAnalysis/`'s `AudioMap`, a cache the game keeps, never sent.
- **A picture travels as `AgentImageResult`**: base64 PNG in `png`, plus `width`, `height` and one `legend` line saying what the colours mean (`AgentRules.HazardColor`, `CameraColor`). The server turns it into an image block; nothing else is sent as bytes.
- **`AiFolderRules` is the one decider for a level's `ai/` folder** (names, extensions, sizes, which files
  travel); `AiFolderManifest` is `ai/manifest.json`, a plain file. The archive builder, the level copy and the
  context tools all ask it. `AiContextDocument` is `CONTEXT.md`'s skeleton and the one way a section grows
  (`appendSection`); `AgentGuideManifest` maps guide topics to public `en` pages (`index` and `context` are
  generated, `mcp` is the public AI assistant page) - the game renders them, the SDK holds no docs.
- **`Imaging/` is golden-tested to the byte** (`PngEncoderTests`): a different zlib level or chunk set is a
  deliberate change of the pinned bytes, not noise.

## Traps

- **The endpoint's `DefaultPath` assumes Unity's default data path** for company `vertoker`, product
  `Bullet Hero` (`AgentEndpoint.Company`/`Product`); a renamed company or product in Player Settings silently
  moves the file, and the server then reports `not_connected`.
