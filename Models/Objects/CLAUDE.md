# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Objects

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## Object model (`Models/Objects/`)

**`TextObject` carries two per-character effect tracks** beyond the usual transform ones:
`Fillments` (how much of the text is written) and `Appearings` (how much of it hides behind
`AppearingMask`), both plain `List<FloatKey>`, plus the non-keyframed `FillDirection`
(`Forward`/`Backward`/`FromCenter`/`ToCenter`), `AppearingMode` (`Random`=0/`Forward`/`Backward`) and
`AppearingMask` (an author-set string, default `"X"`, capped by `TextRules.MaxAppearingMask`). They
are resolved over the string itself by the consumer's text job rather than by the keyframe→transform
path. **Both fallbacks in `TextRules` mean "effect off"** (`Fillment_Fallback` = 1,
`Appearing_Fallback` = 0) — an empty track has to read as unchanged, or every text authored before
these existed would vanish. `Services/FontCharacterService` folds the mask into the font's character
set, since a mask character needs a glyph exactly like the text it replaces.

`RectObject` is the base of every placeable scene object: `ObjectId`, `ParentObjectId`, `Name`,
`Active`, `Span` (a half-open `FrameSpan`), `Layer`, plus the shared keyframe tracks
(`Positions`/`Rotations`/`Scales`/`Sizes`/`AnchorsMin`/`AnchorsMax`/`Pivots`). Empty keyframe lists
are valid (mirrors Unity project's `defaults.xxx` fallback convention). Subclasses, each overriding
`GetModelType() : ObjectType`: `ShapeObject`, `EffectObject` (thin — just an `EffectId` pointing
into `Level.Resources.Effects`, the actual payload lives in `EffectData`), `TextObject`,
`PrefabObject` (see "Prefab system" below).

**`ShapeObject` carries TWO `ShapeId` fields and neither derives from the other**: `ShapeId` is what
is drawn, `ColliderId` is what is hit, and a level routinely wants them to disagree — a telegraph
beam that is drawn but harmless, a hitbox simpler than the art it guards, an invisible wall. One id
type serves both because a shape and a hitbox are the same data: triangles inside `[-0.5, 0.5]`.
Both resolve against the same two collections (the game's own shape presets, or
`Level.Resources.CompositeShapes`), so a user-authored shape is usable for either or both.

**A built-in `ShapeId` is its parameters packed**, and the constants naming them live in the
generated half of the type (`Models/Primitives/ShapeId.g.cs`, one nested class per form —
`ShapeId.Hexagon.S4_2_T8_I`). `Services/Shapes/ShapeCatalogService` owns the layout and is the only
thing that may write that file. The previous library numbered its 78 shapes 1..78 by their position
in the consumer's array, so inserting a form renumbered everything after it; form code 0 is now
reserved and never issued, which is what makes every one of those retired ids decode to nothing
rather than to some other shape.

**`RectObject.Active` replaced `Visible`, and the change is semantic, not cosmetic.** `Visible`
gated rendering only, so an invisible object still hit the player — a trap, since nothing in the
name said so. `Active` gates *both* paths and applies down the hierarchy. "Not drawn but still
solid" moved to where it belongs: a Null `ShapeId` with a real `ColliderId`.

`ShapeObject.ShaderType` (`Models/Enum/ShaderType.cs`, byte: `Auto = 0`/`Opaque`/`Transparent`) is
authored intent about the render path, not a shader id — the format deliberately has no
user-defined shaders. `Auto = 0` is what an older file deserializes to, so adding it needed no
migration and the domain stayed at generation 1. What `Auto` actually resolves to is a *consumer*
question and lives in the Unity project (`Core`'s `ShapeShaderResolver`); the format only stores the
three-way choice. It is one of the hand-written-boilerplate fields, so it must appear in
`CopyImpl`/`Update`/`EqualsShapeObject`/`GetHashCode` alike.

**Polymorphism mechanism** (applies throughout the whole model tree, not just objects — see "Value
system"): `ObjectType` (byte enum) is resolved by `Serialization/Converters/CustomTypes/
ObjectConverter.cs`, registered globally in `SerializationService`, **not** via a `[JsonConverter]`
attribute on `RectObject` itself and **not** via Newtonsoft `TypeNameHandling`/`$type`. Adding a new
`RectObject` subtype means extending `ObjectConverter.GetType`'s switch (throws
`ArgumentOutOfRangeException` otherwise) — there's no attribute-based auto-discovery.

## `IObjectScope` / `IObjectIdCounter` — the split every consumer must get right

`IObjectScope` (`Dictionary<ObjectId, RectObject> Objects`) and `IObjectIdCounter`
(`ObjectId GetNextObjectId()`) are two separate, narrow interfaces. **`Prefab` implements both on
one class** (its own `Objects` + its own int counter). **At level scope the two are split across
different classes**: `Level.Game` (`GameLevel`) is the `IObjectScope`, but `Level.Settings`
(`LevelSettings`) is the `IObjectIdCounter` — `GameLevel` does *not* implement `IObjectIdCounter`,
`LevelSettings` does *not* implement `IObjectScope`. Anything that needs "the scope+counter pair"
generically (the Unity project's `PrefabMaterializer`, `LevelEditorOperation.TargetScope`/
`TargetCounter`) must combine `Level.Game`+`Level.Settings` explicitly for the level case, unlike the
`Prefab` case where one object satisfies both. Don't assume symmetry here.

`ObjectId` has reserved negative constants beyond plain user ids: `Camera = -1` (player-runtime-only,
invalid as an actual `ObjectId`), `LocalPlayer = -2` (a valid parent target), `PrefabRoot = -3`
(only meaningful *inside* a `Prefab` template — same effect as an unset/`Null` `ParentObjectId`
there). `RuleParentObjectIdValidAttribute` accepts all three everywhere, even where semantically
meaningless (e.g. `PrefabRoot` at level scope) — a known leniency gap, not yet context-checked.
