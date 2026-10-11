# Level resources: the seven dictionaries, their ids, and the references that may be null

Split out of `Models/Resources/CLAUDE.md`, which keeps the summary and links here. Design facts of
`Models/Resources/` that the code cannot say by itself. The id criterion (`Guid` vs `int`, and the sign
table) is `IDENTIFIERS.md`; the texture enums' own reasoning is `ENUMS.md`, "Texture axes"; credit
records are `RESOURCE_META.md`; collections on disk are `ARCHIVE.md`, "Collections".

## `LevelResources` (`Level.Resources`)

Seven dictionaries - `Textures`/`Fonts`/`Audios` (by their typed resource id), `CompositeShapes` (by
`ShapeId`), `Themes`/`Effects`/`Prefabs` (by their own Guid-based id).

**Every dictionary here can only ever contain user-defined (negative-id) resources** - each concrete
`Resource` subtype's id property is rule-capped to the negative range; game-defined resources are
baked into the game/its own registries and never appear in a level's own `Resources`.

`TypedResourceId` (`Models/Primitives/Resources/`) is the shared convention: `0`=Null,
`[1,MaxInt]`=game-defined (permanent), `[MinInt,-1]`=user-defined (needs `Resource.Sources`, up to
`Resource.MaxSourcesCount`=4 fallback URIs per resource). `TextureResourceId`/`FontResourceId`/
`AudioResourceId`/`BytesResourceId`/`TextResourceId` are narrow per-category wrappers sharing this
range, freely convertible to/from the untyped `TypedResourceId`.

`Guid`-based ids (`ShapeId`/`EffectId`/`LevelId`/`PrefabId`/`ThemeId`) deliberately have **no**
positive/negative split - a `Guid` has no natural sign, so game-defined vs. user-defined is determined
by *which collection* an id is found in (game registry vs. level `Resources`), not by the id's own
value, unlike the int-based `TypedResourceId` family. `Guid.Empty` is the reserved Null for all of
them.

**`FontCharacters` is not here** - it was never a resource, only a fact *about* the resources, so it
lives in `Level.Hints` with the rest of the advisory data. Don't look for it under `Level.Resources`.

## `TextureResource`

**The one resource carrying authored fields beyond its id, UV and sources**, and there are six of them
- `Kind` (`TextureKind`: `Auto`/`Photo`/`Graphic`/`PixelArt`/`Gradient`), `Alpha` (`TextureAlpha`:
`Auto`/`Opaque`), `Sampling` (`TextureSampling`: `Auto`/`Smooth`/`Sharp`), `Compression`
(`TextureCompressionKind`: `Auto`/`Allow`/`Refuse`) and `WrapU`/`WrapV` (`TextureWrapKind`:
`Clamp`/`Repeat`/`Mirror`, one per axis).

- **Six independent axes, deliberately not one richer enum**: an opaque pixel-art tile repeating
  horizontally and clamped vertically is five answers, and every pair that was ever folded together
  made a real case unsayable. None of them is a format, a size or a memory budget - all of that
  belongs to the player's own `UserSettings.Graphics.Textures`.
- **`Sampling` and `Compression` are the two statements `Kind` carried on its own behalf before them**,
  split out because they are not the same claim as the content: a photograph an author refuses to have
  compressed is not a `Gradient`, a crisp hand-drawn sprite is not pixel art, and calling it one to get
  the look also threw its mip-maps away. A kind still SEEDS both (`PixelArt` seeds `Sharp` plus a
  compression refusal, `Gradient` a refusal), so nothing authored before them changed - and `Allow` is
  the direction that was previously inexpressible, an author handing a device its memory back. The
  consumer's rules for each are in `Core`'s `TextureLoadPlanner`: a memory axis may only be REFUSED,
  never demanded, and never touches the size cap; a look axis wins outright.
- **`Alpha`'s motive is worth restating**: `ImageHeaderReader` proves a file CANNOT be transparent from
  33 bytes, but proving an alpha channel that EXISTS is 255 everywhere needs every pixel read, which
  the consumer refuses to pay - so the author is the only one who can say it, and nothing verifies the
  claim. It is an enum rather than a bool for the reason `ShapeObject.ShaderType` is one: `Cutout` is
  the obvious next member, and two booleans that can contradict each other is what this shape avoids.
- **`WrapU`/`WrapV` are the fields that make `TextureResourceUV`'s tiling half mean something** - the
  data always reached the shader, and the consumer hard-coded Clamp, so a tiling UV only ever stretched
  one row of pixels. **Per axis rather than one field**, because the content a single one could not
  describe is ordinary: a strip that tiles along X and is clamped on Y.
- **Every one is additive with a zero default except the wrap pair**, which REPLACED a single `Wrap`
  under new keys (`wrap_u`/`wrap_v`) - so a level written before them reads back as
  `Auto`/`Auto`/`Auto`/`Auto`/`Clamp`/`Clamp`, one that had authored a repeat needs a re-save (the
  consumer's Rule 11: the format still broke in place before 1.0.0, when this landed), and
  `LevelResources` stayed at generation 1 either way.

## References where Null is an authored state

**Three `IPrimitiveGuid` properties deliberately allow Null and must NOT carry
`[RuleIPrimitiveGuidNotNull]`** - for them Null is a real authored state, not an unset reference:

- `ShapeObject.ShapeId` - Null = drawn as nothing, which combined with a real `ColliderId` is how an
  invisible hitbox is authored;
- `ShapeObject.ColliderId` - Null = decoration, drawn but never collided with (the runtime collision
  jobs skip on `!IsEnabled()` and the editor's collision toggle writes Null);
- `PrefabObject.PrefabId` - Null = empty placement, materializes nothing (`OpLevelCreatePrefabObject`
  creates every placement this way before the author picks a template).

`ColliderId`/`PrefabId` default to Null straight from their constructors, so adding the rule makes a
freshly-constructed object fail validation; worse, its `Fix` assigns a random `Guid`, silently
inventing a nonexistent shape (giving decoration real damage) or a dangling prefab reference. This bit
the SDK once already - the rule was on `ColliderId` and broke `ValidatorTests`/`SerializationTests`
against `MockData`. Don't re-add any of the three.

**Two int-backed ids join them, for one reason: `ShapeObject.TextureResourceId` and
`EffectObjectCore.TextureResourceId`.** Both carry `[RuleReferenceExists(Texture, allowNull: true)]`
and **no** `[RuleIPrimitiveIntNotNull]`, and both default to `Null` rather than `Square`/`Circle`.
Geometry moved out of the texture and into `ShapeId` - an object's own, and (since
`EffectObjectCore.ParticleShapeId`) a particle's too - so the ordinary object and the ordinary particle
are both a bare tinted silhouette, and an image painted on top of one is the exception. On the object
side the old default also cost every freshly created object the opaque render path, since a texture
that exists cannot be *proven* alpha-1 while one that does not exist trivially can (`Core`'s
`ShapeShaderResolver`); on the effect side it is the graph's own authored state, so the common effect
pushes no texture property at all. The other two int-backed references (`LevelTrack.AudioResourceId`,
`TextObject.FontResourceId`) keep the rule: nothing plays or renders without them.

## Credits and collections

**Resource credits cover all seven kinds since generation 2 of `LevelMeta`.** `ResourceType` gained
`Theme`, `Effect`, `Shape`, `Prefab`, and `ResourceMeta.ResourceGuid` addresses them (the four kinds are
guid-addressed, the three file kinds keep `ResourceId`); `RuleResourceMetaId` keeps the two slots
exclusive. A V1 file migrates through `Versions/V1/Migrations/LevelMetaV1ToV2` with every guid empty.

The same records travel inside a collection's manifest (`Models/Collections/ResourceCollection`), and
an import copies the records of what it brings (`GameEditor`'s `OpMetaMergeResourceRecords`). The
manifest lists only the FILE resources (a texture is bytes, so its record must live somewhere); the
data resources (prefabs, themes, shapes, effects) are envelope files of their own, so the folder is the
table. File-resource ids are collection-local negative ints, renumbered by every import
(`ResourceRemap`). **`ResourceCollection` is a new domain, not a generation bump**: no level refers to
a collection, it copies.
