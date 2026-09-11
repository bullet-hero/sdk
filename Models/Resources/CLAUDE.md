# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Resources

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## Prefab system

Nesting is bounded by `ResourceSettings.Prefabs_MaxInheritanceLevel` — a template may hold a
placement of another template, and `PrefabMaterializer.Resync` re-propagates through every level of
that up to the limit.

The device-wide prefab library is the only cross-level sharing mechanism a prefab has, since prefabs
have no game-defined preset tier. The editor's prefab picker offers it as its own tier
(`SearchOrigin.Library`); choosing an entry imports the resource, creates the placement and
materializes the template as ONE undo step.

`Prefab` (`Models/Objects/Prefab.cs`, a `Level.Resources.Prefabs` entry) is the *template*: its own
`Objects`/`ObjectIdCounter`, plus its own authored `Name`/`FrameDuration`. `PrefabObject` (a
`RectObject` subclass) is the *placement*: `PrefabId` (which template) +
`Dictionary<ObjectId, ObjectId> ObjectIds` (template-inner id → this placement's own materialized
outer id) + `Dictionary<ModificationKey, Modification> Modifications` (per-instance field overrides,
below). Placements — whether at level scope or nested inside another `Prefab`'s own `Objects` — live
in the **same** `Objects` dictionary as everything else, discriminated only by
`GetModelType() == ObjectType.PrefabObject`; there's no separate placement list.

**Per-instance overrides (`PrefabObject.Modifications`) are live and load-bearing** — this is how a
placement diverges from its template without breaking the link. Three pieces:

- `ModificationKey` (`Models/Primitives/`) — the *address*: `ObjectId` (the **template's inner** id,
  not the materialized outer one, so the key survives re-materialization) + `int Field` (a stable
  `ModificationFields` number, NOT a JSON key spelling) + `int Index` (which element of a collection
  field, or `WholeField` = -1). Being the dictionary key is what makes "one override per (object,
  field) pair" a structural guarantee rather than a rule to enforce. It was a dotted string
  (`"pos[0].v"`) until a key rename's cost outgrew it - `Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md`
  carries why, including the three spellings that never resolved at all.
- `Modification` (`Models/Objects/`) — `Key` + an untyped `object Value`. The `Value` setter
  **normalizes integrals to `long` and floating-point to `double`** on assignment, deliberately
  matching what Newtonsoft always produces when deserializing a raw JSON number into an `object`
  property — without it an override built in code with a plain `int` stops `Equals`-ing itself after
  a round trip. Its file header also lists the design limits still in force: only `RectObject`/
  `Prefab` targets, no parenting a `RectObject` *into* a prefab's inner objects (only the reverse),
  and no deep inheritance — an override applies only within the prefab scope it lives in.
- `Utils/ModificationUtils.cs` + the generated `ModificationTable` — applying one. The table is a
  flat switch written by `BH.SDK.Roslyn`'s `ModificationTableGenerator` from every member carrying
  `[ModificationField]`, so the apply path is reflection-free; `ModificationValues.TryConvert`
  converts the untyped value back through the serializer that shaped it, which is what makes an int,
  an enum and an id override land at all. `ModificationUtils` keeps the rule-checked write
  (`IsValueAllowed`/`SetValueChecked`), the one place a `PropertyInfo` is still needed - an override
  writes past every rule the target property carries, and nothing else can judge it.

Overrides are **re-applied on top of a fresh template copy after every materialize/resync**
(Unity-side: `Core`'s `PrefabMaterializer.ApplyModifications`; recorded by `GameEditor`'s
`ModificationRecorder`/`EditObjectOperation.RecordModification` — see those folders' `CLAUDE.md`s).
`Modifications` serializes through its own `Serialization/Converters/Dict/
DictionaryModificationsConverter` (the key is recoverable from the value's own `Key` property, so it
writes as a plain array — same family as `DictionaryAsListConverter`, see "Value system" below).

## `Models/Resources/`

`LevelResources` (`Level.Resources`): seven dictionaries — `Textures`/`Fonts`/`Audios` (by their
typed resource id), `CompositeShapes` (by `ShapeId`), `Themes`/`Effects`/`Prefabs` (by their own
Guid-based id). **Every dictionary here can only ever contain user-defined (negative-id) resources**
— each concrete `Resource` subtype's id property is rule-capped to the negative range; game-defined
resources are baked into the game/its own registries and never appear in a level's own `Resources`.
`TypedResourceId` (`Models/Primitives/Resources/`) is the shared convention: `0`=Null,
`[1,MaxInt]`=game-defined (permanent), `[MinInt,-1]`=user-defined (needs `Resource.Sources`, up to
`Resource.MaxSourcesCount`=4 fallback URIs per resource). `TextureResourceId`/`FontResourceId`/
`AudioResourceId`/`BytesResourceId`/`TextResourceId` are narrow per-category wrappers sharing this
range, freely convertible to/from the untyped `TypedResourceId`.

**`TextureResource` is the one resource carrying authored fields beyond its id, UV and sources**, and
there are six of them - `Kind` (`TextureKind`: `Auto`/`Photo`/`Graphic`/`PixelArt`/`Gradient`),
`Alpha` (`TextureAlpha`: `Auto`/`Opaque`), `Sampling` (`TextureSampling`: `Auto`/`Smooth`/`Sharp`),
`Compression` (`TextureCompressionKind`: `Auto`/`Allow`/`Refuse`) and `WrapU`/`WrapV`
(`TextureWrapKind`: `Clamp`/`Repeat`/`Mirror`, one per axis).
**Six independent axes, deliberately not one richer enum**: an opaque pixel-art tile repeating
horizontally and clamped vertically is five answers, and every pair that was ever folded together
made a real case unsayable. None of them is a format, a size or a memory budget - all of that belongs
to the player's own `UserSettings.Graphics.Textures`.

**`Sampling` and `Compression` are the two statements `Kind` used to carry on its own behalf**, split
out because they are not the same claim as the content: a photograph an author refuses to have
compressed is not a `Gradient`, a crisp hand-drawn sprite is not pixel art, and calling it one to get
the look also threw its mip-maps away. A kind still SEEDS both (`PixelArt` seeds `Sharp` plus a
compression refusal, `Gradient` a refusal), so nothing authored before them changed - and `Allow` is
the direction that was previously inexpressible, an author handing a device its memory back. The
consumer's rules for each are in `Core`'s `TextureLoadPlanner`: a memory axis may only be REFUSED,
never demanded, and never touches the size cap; a look axis wins outright.

`Alpha` is the one whose motive is worth restating: `ImageHeaderReader` proves a file CANNOT be
transparent from 33 bytes, but proving an alpha channel that EXISTS is 255 everywhere needs every
pixel read, which the consumer refuses to pay - so the author is the only one who can say it, and
nothing verifies the claim. It is an enum rather than a bool for the reason `ShapeObject.ShaderType`
is one: `Cutout` is the obvious next member, and two booleans that can contradict each other is what
this shape avoids.

`WrapU`/`WrapV` are the fields that finally make `TextureResourceUV`'s tiling half mean something -
the data always reached the shader, and the consumer hard-coded Clamp, so a tiling UV only ever
stretched one row of pixels. **Per axis rather than one field**, because the content a single one
could not describe is ordinary: a strip that tiles along X and is clamped on Y.

Every one is additive with a zero default except the wrap pair, which REPLACED a single `Wrap` under
new keys (`wrap_u`/`wrap_v`) - so a level written before them reads back as
`Auto`/`Auto`/`Auto`/`Auto`/`Clamp`/`Clamp`, one that had authored a repeat needs a re-save (the
consumer's Rule 11: the format breaks in place before release), and `LevelResources` stays at
generation 1 either way.

**`FontCharacters` used to live here and no longer does** — it was never a resource, only a fact
*about* the resources, so it moved to `Level.Hints` with the rest of the advisory data. Don't look
for it under `Level.Resources`.

`Guid`-based ids (`ShapeId`/`EffectId`/`LevelId`/`PrefabId`/`ThemeId`) deliberately have **no**
positive/negative split — a `Guid` has no natural sign, so game-defined vs. user-defined is
determined by *which collection* an id is found in (game registry vs. level `Resources`), not by the
id's own value, unlike the int-based `TypedResourceId` family. `Guid.Empty` is the reserved Null for
all of them.

**Three `IPrimitiveGuid` properties deliberately allow Null and must NOT carry
`[RuleIPrimitiveGuidNotNull]`** — for them Null is a real authored state, not an unset reference:
`ShapeObject.ShapeId` (Null = drawn as nothing, which combined with a real `ColliderId` is how an
invisible hitbox is authored), `ShapeObject.ColliderId` (Null = decoration, drawn but never collided
with — the runtime collision jobs skip on `!IsEnabled()` and the editor's collision toggle writes
Null) and `PrefabObject.PrefabId` (Null = empty placement, materializes nothing —
`OpLevelCreatePrefabObject` creates every placement this way before the author picks a template).
`ColliderId`/`PrefabId` default to Null straight from their constructors, so adding the rule makes a
freshly-constructed object fail validation; worse, its `Fix` assigns a random `Guid`, silently
inventing a nonexistent shape (giving decoration real damage) or a dangling prefab reference. This
bit the SDK once already — the rule was on `ColliderId` and broke
`ValidatorTests`/`SerializationTests` against `MockData`. Don't re-add any of the three.

**Two int-backed ids join them, for one reason: `ShapeObject.TextureResourceId` and
`EffectObjectCore.TextureResourceId`.** Both carry `[RuleReferenceExists(Texture, allowNull: true)]`
and **no** `[RuleIPrimitiveIntNotNull]`, and both default to `Null` rather than `Square`/`Circle`.
Geometry moved out of the texture and into `ShapeId` — an object's own, and (since
`EffectObjectCore.ParticleShapeId`) a particle's too — so the ordinary object and the ordinary
particle are both a bare tinted silhouette, and an image painted on top of one is the exception. On
the object side the old default also cost every freshly created object the opaque render path, since
a texture that exists cannot be *proven* alpha-1 while one that does not exist trivially can
(`Core`'s `ShapeShaderResolver`); on the effect side it is the graph's own authored state, so the
common effect pushes no texture property at all. The other two int-backed references
(`LevelTrack.AudioResourceId`, `TextObject.FontResourceId`) keep the rule: nothing plays or renders
without them.
