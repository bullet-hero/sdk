# Versions

Historical snapshots and migrators for the model versioning system. See `VERSIONING.md` in the SDK's own
`Docs/` folder (`Assets/Plugins/BulletHeroSDK/Docs/VERSIONING.md` from the game project) for the
full design — what a generation is, which of the project's six versions this one is,
and why a nested envelope at another generation is refused rather than migrated. Short version: a
generation lives only on aggregate roots (types carrying `[ModelGeneration]`), not on every class,
so most models never appear here at all.

## Folder convention: generation-first, not domain-first

Organize by **format generation** (`V{n}/`), not by domain. A single content update usually touches
several related domains at once (e.g. `Level`, `GameLevel`, `GameEvents` together), so keeping
everything that was true "as of generation n" in one folder makes a release's full diff visible in
one place, instead of scattering it across parallel per-domain trees.

```
Versions/
  V0/                          # one folder per historical generation, named after its own number
    LevelV0.cs                 # frozen snapshot of Level's own shape at this generation
    LevelSettingsV0.cs         # ...only for domains that actually differ from current at this generation
    GameLevelV0.cs
    GameEventsV0.cs
    LevelResourcesV0.cs
    AudioLevelV0.cs            # intentionally has NO [ModelGeneration] - see note below
    Migrations/
      LevelV0ToV1.cs           # LevelV0 -> Level (current)
      LevelSettingsV0ToV1.cs   # LevelSettingsV0 -> LevelSettings (current)
      GameLevelV0ToV1.cs
      GameEventsV0ToV1.cs
      LevelResourcesV0ToV1.cs
  V1/                          # next generation, whenever it exists, same shape
    ...
```

**A generation is one number, and it is assigned forward.** A domain that changes shape takes the
next free number for ITSELF; the others stay where they are. So the folders are not a global ladder
every domain climbs together — `V3/` may hold nothing but `LevelV3.cs` while every other domain is
still at 1. `ModelGenerations` names the ones that exist, and `ModelGenerations.Current` is the
maximum across the live domains.

Rules that keep this from turning into a mess as more generations pile up:

- Only create a snapshot class for a domain if **that domain's own shape** actually changed at this
  generation. If `Theme` never changes, it never gets a `ThemeVn.cs` file, ever.
- A domain's own frozen snapshot classes ALSO carry `[ModelGeneration(Domain, <their own old
  generation>)]` — `VersionedTypeRegistry` needs this to resolve generation -> Type. Classes that
  exist only as another snapshot's implementation detail (a nested leaf type frozen alongside its
  container) don't need the attribute.
- **Important, easy to get wrong**: a container's property holding a nested `[ModelGeneration]`-tagged
  value must be typed using that domain's **current** class, not the old `Vn`-suffixed one —
  `VersionedEnvelopeConverter` always resolves the nested envelope's own generation tag and upgrades
  it to the domain's current type before returning, regardless of what generation the *outer*
  container belongs to. So e.g. `LevelV0.Settings` is typed `LevelSettings` (current), never
  `LevelSettingsV0`, even though `LevelSettingsV0` still exists and is still registered.
- A domain that had **no** independent envelope yet at some generation (i.e. it was introduced later)
  gets a snapshot class with **no** `[ModelGeneration]` attribute at all, exactly like `AudioLevelV0`
  here — Newtonsoft then deserializes it as a plain nested object with no envelope unwrapping, and the
  *containing* domain's own migrator is responsible for constructing the current type from those raw
  fields by hand.
- Migrators for a generation live in that generation's own `Migrations/` subfolder, one class per
  domain, always named `<Domain>V{from}ToV{to}.cs` (e.g. `LevelV0ToV1.cs`) — spelling out both ends in
  the name even for a single-step chain, so it stays unambiguous once a domain accumulates more than
  one historical generation.
- **A snapshot IS a generated model**: `[GenerateModel] public sealed partial class Xv0 : IModel<XV0>`,
  exactly like a live one, and it gets all seven contract bodies plus both codecs. They are dead
  weight on a transient type, and that is the price of the thing having codecs at all — which is what
  lets `ReadEnveloped` and the generated `.blob` root read a snapshot with no serializer and no
  reflection. Keys stay literal strings, not the shared `Names.Xxx` constants, since those track
  *current* naming.
- **A snapshot holds only what `ModelGenerator` can encode.** `LevelResourcesV0.Resources` was a
  `Dictionary<int, object>` and could not stay one; it maps to nothing, so retyping it cost only
  keystrokes. A member the generator cannot name is a `BHS1003`, not a member quietly skipped.
- **A snapshot constructs its members**, like every live model: the constructor is the single source
  of every default the generated `Reset`/`Copy`/`Update` read from, so a member left null is a
  `NullReferenceException` in bodies a free-form deserialization target never had.
- A nested leaf frozen alongside its container (`AudioLevelV0`) still needs no `[ModelGeneration]` —
  but it **does** need `[GenerateModel]`, because the generator refuses a member type it does not own,
  so everything reachable from a snapshot is generated or the snapshot does not compile.

## V0 is a scaffold, and that is why it is kept

`V0` is not real shipped format history: its `NamesV0` keys are placeholders (`"test_settings"`,
`"test_fps"`) and its snapshot classes are near-identical to today's. It stopped being a thing the
suite merely TOUCHES once the snapshots gained codecs — it is now what the suite MEASURES migration
with, in three ways it could not before: a `.blob` half, a nested half, and a cross-path identity
(the same V0 bytes through JSON and through `.blob` must arrive at the same instance). It is kept
because it is the **only** thing that still proves `VersionedTypeRegistry` and `IMigration` work at all — the two real
snapshots this repo once had were deleted along with the bumps that needed them (pre-release, nothing
on anyone's disk is worth migrating; the main project's `CLAUDE.md` Rule 11 says when that stops being
true). `VersionedTypeRegistryTests` and `SerializationTests.TestLevelV0Migration` are what exercise it.
