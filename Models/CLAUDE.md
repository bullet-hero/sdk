# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

**Documentation for this layer is per-subfolder.** Each file below loads only when you touch
a file in that folder.

## Folder index

| Folder | File | Size |
|---|---|---|
| `Clipboard/` | `Clipboard/CLAUDE.md` | 474 tok |
| `Game/` | `Game/CLAUDE.md` | 667 tok |
| `Hints/` | `Hints/CLAUDE.md` | 1052 tok |
| `Keyframes/` | `Keyframes/CLAUDE.md` | 619 tok |
| `Objects/` | `Objects/CLAUDE.md` | 1485 tok |
| `Resources/` | `Resources/CLAUDE.md` | 2525 tok |
| `SettingGroups/` | `SettingGroups/CLAUDE.md` | 2179 tok |
| `Statistics/` | `Statistics/CLAUDE.md` | 681 tok |
| `Values/` | `Values/CLAUDE.md` | 1039 tok |

## Models/

the level format itself (see "Object model" / "Value system" below for the
  architecturally important parts). Subfolders: `Objects/` (scene-object hierarchy), `Values/`
  (polymorphic Value implementations), `Keyframes/` (keyframe wrapper types), `Effects/`+`Audio/`+
  `AudioEffects/`+`PostProcessing/` (per-domain payload data), `Game/` (`GameLevel` + level-global
  event tracks), `Events/` (one-shot markers/checkpoints, not animated tracks), `Data/` (`ThemeData`/
  `EffectData`/`CompositeShape` — resource-level aggregate payloads), `Resources/` (level resource
  dictionaries + `TypedResourceId` family), `SettingGroups/` (⚠️ two unrelated aggregates share this
  folder — `LevelSettings`, per-level, vs. `UserSettings`' sub-groups, per-device — see below),
  `Hints/` (`LevelHints` + `LimitHints` — everything advisory, see its own section below),
  `Meta/` (`Author` — name, credit and url, where the credit is a localizable free-text line
  rather than a role enum, since the vocabulary of a credits list is open and an empty one is an
  ordinary record; `ResourceMeta` — consumed by `LevelMeta`, itself NOT in this folder; note
  `ResourceMeta` carries licensing/attribution only — **age rating and content descriptors live on
  `LevelMeta` alone**, since a rating describes the finished experience, not an asset in isolation),
  `Interfaces/`, `Enum/`, `Primitives/` (id structs — **`Assets/Plugins/BulletHeroSDK/Docs/IDENTIFIERS.md` is the criterion that
  decides `Guid` vs `int`, and the answer to "will these ever have to be unified"; read it before
  adding an id**). `Models/Names.cs` is the single source of truth
  for every `[JsonProperty]` name (short/abbreviated on purpose — see Serialization).

## Effects, Audio, PostProcessing, Theme

- **Effects**: `EffectObject.EffectId` points at an `EffectData` resource (`Models/Data/
  EffectData.cs`, `Level.Resources.Effects`) — the SDK-side analog of Unity's `EffectGameStatic`
  index table. `EffectData` groups: `Core` (render/loop/particle-count/lifetime/texture/pivot),
  `Forces` (gravity/velocity/orbital/linear-force), plus 4 independently-polymorphic sub-shapes:
  `IEffectAngle`/`IEffectColor`/`IEffectScale` (each: Value / CurvesOverLife / CurvesBySpeed /
  RandomUniform / RandomPerComponent — the Random pair is structurally identical, differs only in
  evaluation semantics) and `IEffectShape` (Point/Circle/Rectangle/Line/Cone/Torus, with
  Circle/Line/Cone/Torus additionally nesting an `IEffectShapeSpread`: Random/Loop/PingPong/Sine —
  `Sine` and `Point` carry zero fields, pure enum-selected behavior).
- **Audio**: `AudioLevel.Tracks : Dictionary<AudioId, LevelTrack>` (one flat dict per level;
  `AudioId` bans negative values — no game/user-defined split unlike other ids). `LevelTrackEffects`
  is a **flat class with one slot per DSP effect** (Lowpass/Highpass/Echo/Reverb/Chorus/
  PitchShifter/Distortion/Flange/Compressor/Normalize/ParamEQ), not a dictionary/flags/list.
  **A slot is NULL until an author touches it**, and null means "this effect is not in the chain" —
  the eleven used to be always-present objects sitting at the disabled floor, which wrote 1.1 KB of
  pure defaults into every track of every level. They are constructed null because a member that may
  be null has to be (see `docs/NAMING.md`), which is what lets the writer skip it: an untouched
  track's whole chain is now `"eff":{"vlm":[],"stpan":[],"a":false}`, and dialling ONE effect in
  writes that one.
  `MixLevel` did NOT change meaning: it is a real wet level in dB whose floor is silence, so
  `AudioRules.IsActiveMixLevel` still answers "is this audible". Null answers "is it here at all",
  and the two are different questions — an effect an author dialled in and then switched off keeps
  its object and its settings. Every consumer treats a null slot as the default instance, which IS
  the off state field for field (`MixLevel_Default` is `MixLevel_Disabled`).
  `LevelTrackEffects.Active` (track-level, default `false`) is the master switch for the whole
  chain — a third thing again, don't confuse it with either.
  **`LevelTrack.Volume` (`[0, 1]`, default `1`) is the track's own fader and the SECOND thing on it
  called volume** - the first being `Effects.Volumes`, the keyframed curve. They MULTIPLY at playback
  rather than compete (the consumer does it in `BuildAudioJob`): the fader is what the whole track
  sits behind, the curve is what fades it in and out inside that, and an author wanting "this track,
  quieter" should not have to rewrite every key on it. Additive with a constructor default, so it
  needed no migration and `AudioLevel` stays at generation 1 - a pre-fader file reads back at full
  volume, which is exactly how it used to sound. Note this is the opposite call from `Speed`
  below, whose pre-Speed default deserialized to a silent `0`.
  `LevelTrack.Speed` (`[-2, 2]`, default `1`) is the track's own resample rate — faster is also
  higher-pitched, negative **reverses** the track (it starts at the clip's END and plays back to its
  start, with `OffsetTime` skipping the tail rather than the head), `0` freezes it silent — and it
  is deliberately **not** keyframed: an animated rate would make the clip position the integral of
  that curve, which no consumer can evaluate from one frame's data. It shipped **without a migration**
  on purpose (`AudioLevel` stays at generation 1): a pre-Speed file deserializes to `0f`, i.e. silent
  tracks, and the levels that existed at the time were the author's own to re-save. Don't add a
  `NullSpeed`-style sentinel after the fact — `0` is a legal authored value here.
- **PostProcessing**: `GameLevel.PostProcessingEvents` (generation 1 like every other domain - it had moved
  while the ColorCurves migrator existed; see `docs/issues/COLOR_CURVES_HISTORY.md` in the
  consuming project) - top-level `Active` (default `true`,
  opposite default from audio's `Active`) + 12 keyframe-track lists, one per URP effect (Bloom,
  ChromaticAberration, Vignette, LensDistortion, FilmGrain, MotionBlur, ColorCurves, LiftGammaGain,
  ShadowsMidtonesHighlights, WhiteBalance, AnalogGlitch, DigitalGlitch — matches Unity's
  `Core/Models/Groups/PostProcessingGroup.cs` field-for-field). `PostProcessingKeyframe` implements
  `IKeyframe` directly (not `: Keyframe`) and adds its own per-keyframe `Active` bool — every effect
  is independently toggleable per-keyframe *in addition to* the track-level switch. Several fields
  are commented `HEAVY, PHONES DON'T LIKE IT` (Bloom, MotionBlur, AnalogGlitch, DigitalGlitch).
  **The grading colours carry THREE channels, and the fourth one they used to carry was never an
  alpha**: URP reads it as a signed OFFSET (`PrepareLiftGammaGain` adds it per channel,
  `PrepareShadowsMidtonesHighlights` weights it x4 when positive), so an `IColor4` defaulting to
  white sent w = 1 and pushed a whole tonal band four stops up the moment its range was enabled.
  `LiftGammaGainKey`, `ShadowsMidtonesHighlightsKey` and `VignetteKey` are `IColor3` now - the
  vignette's for URP's own reason, it declares that colour `hdr: false, showAlpha: false` - and the
  consumer writes URP's neutral zero itself. **No version moved with it**, deliberately: an older
  payload carries one property more than the type has and Newtonsoft drops it, which
  `PostProcessingColorTests` checks rather than assumes.

  **`ColorCurvesKey` is URP's own eight curves, each a NULLABLE `CurveValue`** — `Master`/`Red`/
  `Green`/`Blue` (an absolute mapping, neutral at the IDENTITY line) and `HueVsHue`/`HueVsSat`/
  `SatVsSat`/`LumVsSat` (an offset or a multiplier, neutral at a flat
  `PostProcessingRules.ColorCurves.CurveNeutral`). Null means "the author never touched this", which
  is a real state rather than missing data. It replaced two scalars, and its own header carries the
  LutBuilder formula that is the reason six MORE scalars would have been worthless. Both wrap modes
  are `ClampForever` on purpose — `CurveWrapMode.Default` behaves as `Once`, which answers a sample
  at exactly the last key time by wrapping to the first.
- **Theme**: `ThemeData` (`Level.Resources.Themes`) holds a fixed `Color4Value[64] Matrix` (an
  "8×8 grid", index layout documented in-file, mirrors *Project Arrhythmya*'s convention).
  `ThemeKeyframe` (a real animated track, unlike `Marker`/`Checkpoint`) selects which `ThemeId` is
  **active** over time. `ColorType.ThemeRef` (`Color3ThemeRef`/`Color4ThemeRef`) stores only a raw
  `int ThemeColorIndex` (0-63) — **not** a `ThemeId` — indexing into whichever theme is currently
  active. Two-level indirection: `ThemeKeyframe` picks the theme, `ThemeRef` picks a slot within it.

## `IModel<T>` pattern

`IModel<T> : ICopyable<T>, IEquatable<T>, IResetable, IUpdatable<T>, IMoveable<T>` — every live
domain model implements: `Copy()` (new instance), `Equals(T)`/`Equals(object)`/`GetHashCode()`,
`Reset()` (back to defaults, in place), and the two become-another-instance operations,
`Update(src)` and `Pull(src)`.

**ALL OF IT IS GENERATED, AND SO ARE BOTH CODECS.** `BH.SDK.Roslyn`'s `ModelGenerator` writes every
one of those bodies for every type carrying `[GenerateModel]` — 203 of them — plus that type's
`.blob` codec (`IBinaryModel`) and its JSON codec (`IJsonModel`); the model file itself holds only
its members, its constructors and whatever is genuinely its own. It used to be hand-written per class:
208 `Copy` bodies, 279 typed `Equals`, 206 `Update`, 206 `Pull`, and the failure mode was never a
compile error but a member quietly missing from one of them. `Equals(object obj) => obj is T value
&& Equals(value)` was named here as the single easiest place in this codebase to introduce a silent
bug, because pasting it from a sibling class compiles and only makes the boxed comparison always
false. That whole class of defect is gone by construction.

**Three models are still hand-written, each for its own reason.** `FrameSpan` and `ModificationKey`
are structs: C# 9 gives a struct no parameterless constructor body, so "back to what the constructor
built" means nothing for them and `Update` is `this = src`. `Modification` holds an untyped `object`
whose copy is not an assignment (a whole-track override stores a `List<TKeyframe>`, which would
alias) and whose equality is not `object.Equals` (a `List` has no value equality). All three are
simply not marked `[GenerateModel]`, which is what the attribute being opt-in is for.

**What the generator will NOT do is fail quietly.** A member it has no encoding for is an ERROR
naming the member (`BHS1003`), never a member skipped; so is a type that forgot `partial`, one with
no parameterless constructor, and one whose base is a model the generator does not also own. The
escape valve is explicit: `[GenerateModelIgnore]` on the property, and the partial hooks
(`OnReset`, `OnCopied`, `OnUpdated`, `OnPulled`, `OnWriteBlob`, `OnReadBlob`) to handle it by hand.

**`[GenerateModelMerge]` is the one thing about `Pull` a type cannot say for itself.** Nine
dictionaries are merged key by key instead of being replaced - the three object scopes
(`GameLevel`, `Prefab`, `ClipboardData`) and the two statistics maps - and nothing in the TYPE of a
`Dictionary<ObjectId, RectObject>` distinguishes them from the seven that are replaced wholesale.
The dispatcher that merges one polymorphic value is generated too (`RectObjectModelPull.PullValue`),
which is what removed `LevelUtils.PullObject` - a hand-kept switch whose own header called out that
a new `RectObject` subtype had to remember to extend it.

**`Update` and `Pull` are what a model offers when its reference is already held somewhere else** —
`GameData` hands `UserSettings` out live, the editor's operation buffer holds an object across an
undo, and for both of them replacing the reference is the one thing that must not happen. They differ
in exactly one way, and it is not which fields they write: **how deep the identity promise reaches**.
`Update` replaces every nested model with a fresh copy, so only the receiver itself survives; `Pull`
writes into the nested instances it already has, so a reference anyone holds anywhere in that subtree
stays live — which is why the `UserSettings` groups are pulled, being handed out one sub-group at a
time. On a model carrying no nested model of its own the two bodies coincide; that is the contract
agreeing rather than duplication to collapse, and a nested model added later changes `Pull` alone.
**Collections are replaced wholesale by both, except the keyed ones something holds into**: an
element is normally addressed by index or key rather than by reference, so keeping the instance buys
nothing. An **object scope** is where the reference *is* the address — the editor's selection, its
operation buffer and every materialized prefab child hold `RectObject`s — and a **clipboard's**
sections are held one per timeline, so `GameLevel`, `Prefab` and `ClipboardData` merge theirs key by
key with `ModelUtils.PullDictionary`: keys the source no longer has are dropped, a key whose concrete
type changed is replaced by a copy, everything else is pulled into the instance already there, and
the dictionary object itself is never swapped. What merges one **value** is the caller's choice —
`LevelUtils.PullObject` for a `RectObject`, the constrained `PullDictionary` overload (i.e.
`PullFrom`) for a concretely typed value like `LevelTrack`. `PullObject` is a hand-kept switch over
the five `ObjectType`s for the same reason `ObjectConverter.GetType` is one — pulling through a
`RectObject` reference would write the base half and drop the subclass's own, so **a new `RectObject`
subtype extends both switches**, and `PullObject` throws rather than truncate if it does not.
`AudioLevel.Tracks` and the seven `LevelResources` dictionaries are still replaced; nothing is known
to hold into them across a pull, and they take the same two lines when something does.

A **polymorphic** field (`IVector2`, `IFloat`, `IEffectShape` …) cannot always keep its instance — a
`Vector2Value` cannot become a `RandomVector2` — so it goes through `ModelUtils.PullFrom`, which
pulls in place while the concrete types match and copies when they do not. Each concrete type also
implements the *interface's* own `Update`/`Pull` explicitly, and those do **nothing** when handed a
sibling implementation; `PullFrom` is the correct path, the no-op is what stops a wrong call writing
one variant's fields out of another's.

Neither is **virtual**, and `Copy()` being virtual is the contrast worth noticing: a subclass adds an
*overload* (`Update(ShapeObject)` beside the inherited `Update(RectObject)`), so `Update` called
through the base type writes the base half and leaves the rest — exactly what `Equals(RectObject)`
already does. Address a model by its own type and it is total. `Tests/ModelHierarchyTests` pins both
halves. **Frozen historical snapshot classes under `Versions/V<n>/` are generated models too** — they
used to skip all of this as one-shot deserialization targets, and stopped being able to the moment a
migrating reader needed to read one with a codec rather than with reflection. They carry
`[GenerateModel]`, declare `IModel<T>`, construct their members, and `ModelContractTests` sweeps them
with everything else.
