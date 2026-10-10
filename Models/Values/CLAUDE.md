# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Values

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## The polymorphic Value system

Every "authorable value" field goes through one of these interfaces (`Models/Interfaces/Values/`),
each `: IModel<TSelf>` plus a single discriminator method returning a `*Type` enum:

| Interface | discriminator enum | concrete variants |
|---|---|---|
| `IFloat` / `IInt` | `FloatType`/`IntType` | Value / RandomMinMax / RandomMinMaxStep |
| `IVector2/3/4` | `VectorType` (shared) | Value / RandomRect / RandomRectStep / RandomCircle / Random**Lerp** / Random**LerpStep** |
| `IColor3` (RGB) / `IColor4` (RGBA) | `ColorType` (shared) | Value / ThemeRef / RandomMinMax / Random**Lerp** |
| `IString` | `StringType` | Value / Localized (`List<StringLanguage>`) |
| `IScreenLimit` | `ScreenLimitType` | None / Fixed / Bounds |
| `ILicense` | `LicenseType` | NoSpecified / Typical / Custom |

**Discriminator mechanism - a 2-element JSON array, not Newtonsoft `$type`/`TypeNameHandling`.** Each
interface has its own `JsonConverter<TInterface, TType> : JsonConverterCustomType<T, TType>`
(`Serialization/Converters/CustomTypes/*.cs`) writing `[typeEnum, payload]`. The base class
(`Serialization/Converters/Base/JsonConverterCustomType.cs`) needs a *second*, private "default"
serializer (containing every other converter except itself) to deserialize the resolved concrete
type's own plain members without recursively re-wrapping them - see `IRequiresDefaultSerializer`
(`Serialization/Converters/Base/`, **not** under `Models/Interfaces` despite the SDK's own TODO.md
implying otherwise). `SerializationService.GetConverters` auto-wires this for any converter
implementing the interface - adding a new polymorphic-value converter needs no other bookkeeping.

**What a serializer actually holds is two converters, not thirty-five.** Newtonsoft resolves a
settings-level converter by walking `JsonSerializer.Converters` and calling `CanConvert` on each,
once per **value**, caching nothing - so a long list is paid for on every value in the file, and
`JsonConverter<T>.CanConvert` is `sealed`, so a converter cannot memoize its own answer. The list in
`GetConverters` is therefore handed to a `ConverterRouter` (`Converters/Base/`), which resolves
`Type → converter` once and answers from a cache afterwards; first match wins over the same list in
the same order, so **order still decides which converter handles a type, and adding one works
exactly as before**. `VersionedEnvelopeConverter` is the one that cannot be routed and sits in the
list beside the router: its `CanConvert` answers differently depending on which domain is currently
being written (the `_activeDomains` guard, which is what stops it re-wrapping its own payload), and a
per-type cache cannot express that. Each `IRequiresDefaultSerializer`'s private serializer gets its
own router, since each excludes a different converter.

Same 2-element-array mechanism backs several other polymorphic families beyond the table above:
`EffectAngleConverter`/`EffectColorConverter`/`EffectScaleConverter`/`EffectShapeConverter`/
`EffectShapeSpreadConverter` (effect emitter sub-shapes, see "Effects" below), `Color4X4KeyConverter`
(4-corner keyframe color - `Color4X4KeyType.Value/Horizontal/Vertical/BariCentrical`), and
`ObjectConverter` (`RectObject` hierarchy, see "Object model" above).

**Dictionaries with a self-describing value** (`ObjectId→RectObject`, `AudioId→LevelTrack`,
`ThemeId→ThemeData`, `EffectId→EffectData`, `PrefabId→Prefab`,
`ModificationKey→Modification`, resource-id dicts) serialize as a
**plain array with the key dropped** (`Serialization/Converters/Base/DictionaryAsListConverter`,
key recovered from the value on read). Dictionaries where the key *can't* be derived from the value
(id→id remap tables) use `DictionaryAsPairListConverter` instead (array of `{K,V}` structs) - plain
Newtonsoft dictionary serialization needs a `TypeConverter` on the key type to use it as a JSON
property name, which value-type ids like `ObjectId` don't have; this sidesteps that entirely.

**The `*Lerp` variants are SEGMENTS, not boxes.** One roll picks a position along the line `From`-`To` and every
component is read at it, so the value never leaves the line; pinning one axis is a degenerate range on that axis
alone (the same number in both ends). They carry NO `RulePropertyOrder` on purpose: a segment has no smaller end,
and ordering the pair would swap `From` and `To`, making a line that falls as it advances (x up, y down)
unrepresentable - the case correlation exists for. `CurveKeyframeValue`'s broken-tangent flag is a separate bit
rather than a `CurveTangentMode` member (in Unity brokenness is independent of how each side's tangent is
derived) and is authored intent: it records whether the author MEANT the two tangents to differ, which an editor
dragging one handle otherwise has to guess. `CustomLicense` carries no permission flags (a ticked box is a claim
nobody can check): a name, an address and the wording, never auto-approved by a publish profile; its body is the
one uncapped string in the format, since a truncated licence is a different legal document that still reads as
whole. `NoSpecifiedLicense.Source` names where a work came from and says nothing about what may be done with it.
