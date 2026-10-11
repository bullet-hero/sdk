# Utils, Interop and the console sample: decisions behind the small pieces

## Model copying and merging (`ModelUtils`)

- **Reset keeps the collection instance.** A reset object is the same object, so whatever holds its lists
  still holds them; every hand-written `Reset` cleared in place for this reason. Defaults come from a fresh
  instance that is thrown away, so sharing its items costs nothing.
- **Two array copies, like three dictionary copies**: what an element IS decides whether copying the array
  is enough. One method constrained `where T : ICopyable<T>` blitted with `CopyTo`, which honours the
  constraint for struct elements and silently breaks it for class ones (`ThemeData.Matrix` is
  `Color4Value[64]` and `Color4Value` is a class, so a copied theme shared all 64 colours and editing
  either edited both; undo snapshots, autosave and prefab materialization all copy). `class` in the
  constraint makes the wrong call a compile error.
- **`PullFrom` is the one place a POLYMORPHIC field is merged**: a `Vector2Value` cannot become a
  `RandomVector2`, so the instance is keepable only while the concrete types agree. A bare
  `target.Pull(source)` through an interface is a no-op on a sibling implementation and would keep the old
  value silently; this returns a replacement instead. A field declared as a concrete model calls `Pull`.
- **The keyed counterpart exists for collections something HOLDS INTO.** Other collections are replaced
  wholesale by `Pull` (an element is addressed by index or key, the instance buys nothing), but in an object
  scope (`GameLevel`, `Prefab`) the reference IS what everything holds: the editor's selection, its
  operation buffer, every materialized prefab child. A `ClipboardData`'s five sections are the same case.
  `pullValue` is a delegate because the value type may be a polymorphic BASE, and `target.Pull(source)`
  through a base reference writes the base half and stops; dispatching to the concrete overload cannot live
  in a generic method, so object scopes pass the generated `<Base>ModelPull.PullValue`. A concretely typed
  value like `LevelTrack` needs no callback.
- **The dictionary hash is order-INDEPENDENT by construction**, never by sorting keys: sorting demanded
  `IComparable`, which most id structs are not (`ObjectId`, every `TypedResourceId`, every Guid id), so
  `PrefabObject.GetHashCode` threw the moment a placement went into a `HashSet`. Entry hashes are summed:
  enumeration order is not part of a dictionary's value and keys are unique.

## Overrides (`ModificationUtils`, `ModificationValues`)

- The generated `ModificationTable` answers structural questions only (which field, what type, where to
  write). Whether a value is ALLOWED there is a rules question living on the property as attributes no
  generator consumes; `PropertyOf` and `ModificationUtils` are the only caller. It replaced 377 lines of
  reflection that resolved a path per override and returned silently when it did not.
- **A per-instance override is the one write that reaches a model with nothing to judge it.** It bypasses
  every rule the target property carries, so an override can park a frame past the end of the timeline or
  a colour outside 0..1 while the level validates clean. The checked write closes it
  (`Tests/Rules/ModificationCheckedWriteTests`).
- **`ModificationValues` converts back through the serializer that shaped the value.** `Modification.Value`
  widens integrals to `long` and floats to `double` on assignment so an override still `Equals` itself after
  a round trip. The old apply path guarded the write with `typeof(int).IsAssignableFrom(typeof(long))`
  (false), so an int override never applied and an enum or id stopped applying after serialization (seven
  kinds landed, four did not; `ModificationApplyTests` holds the record). A long that was an int, a number
  that was an enum or id, a `JToken` that was a model or list - each has that shape BECAUSE this serializer
  gave it one, so converting back through it is a definition that already applies; a hand-written table
  would drift from the converters exactly where the format is most polymorphic. It allocates, which is fine:
  apply runs once per override per materialize, and the fast path covers a value already of the right type.

## Level measurement

- **`LevelCapacityUtils`** measures the peak of simultaneously alive objects, family by family: an object
  occupies an instance slot always, a texture/effect/text slot only when it is that type (thousands of
  plain anchors beside a handful of effects must not size the far more expensive effect buffers). It is
  O(n log n), a sweep over half-open `FrameSpan`s - an end is already the first frame the object is gone,
  so end-before-start ordering needs no correction. Placed prefabs need no special handling (materialized
  into the same `Objects`). `isOpaque` exists because `ShaderType.Auto` is not decidable here (it needs the
  texture's alpha, and this library has no idea what a texture is): a consumer passes a resolver, everyone
  else gets Auto counted transparent, which is the consumer's own fallback and the harmless direction.
- **`LevelStatsUtils`** counts what a level HOLDS, a different question from what it plays: one O(n) pass
  with no allocation, so a diagnostics readout can poll it; buffer sizing still uses `LevelCapacityUtils`.
  Advisory like `LevelHints`: nothing decides from these numbers. Null-tolerant on purpose - a half-built
  or hand-edited level must produce a number, not an exception.
- `BeatMath` lives in the SDK because a generator resolves beats without a Unity project and the editor
  draws and snaps to what that generator will act on. Every beat frame is rounded from the segment's start
  (`Start + round(Offset + i * fpb)`), never accumulated: accumulation drifts up to half a frame per beat
  over thousands of beats, rounding from the base keeps the error at half a frame, once.

## Hashing, text, keys

- **`ContentHashUtils`**: identity of the bytes behind a resource for `ResourceMeta.ResourceHashes`. A
  takedown names a work and finding every level that carries it by title is a guess, by digest a lookup.
  The algorithm is part of the stored value (`sha256:ab12...`), so a future digest can be introduced
  without invalidating what is written and two different algorithms compare unequal instead of silently
  matching. Lowercase hex, no separators (what every other tool prints). Not cryptography in the
  `CryptographyService` sense; SHA-256 is simply collision-resistant enough and universally available.
- **`XxHash64`** checks CORRUPTION, not forgery: a `.blob` is a player's level (interrupted download, file
  half-written when the process died, failing SD card). Tampering is answered where it belongs - a
  protected level goes through OpenPGP SEIPD, which authenticates its own payload - so a cryptographic
  digest would cost several times as much for a guarantee the format does not need. 64 bits rather than
  CRC32's 32: levels reach 18 MB and sit on disk for years, and 32 bits leaves a one-in-four-billion
  chance of accepting a damaged one, multiplied by every level of every player. Hand-written because
  netstandard2.1 has no `System.IO.Hashing` and one algorithm in ~80 lines is not worth a dependency.
- **`ByteSizeUtils`**: sizes are plain `long` BYTES everywhere in the format and publish profiles; the BCL
  has no size type and a struct would make every stored limit custom-serialized. This file only makes them
  READABLE ("48.2 MB", not "50529730"): binary units (1 KiB = 1024 B) printed with the decimal
  abbreviations a file manager shows on every target platform.
- **`SurrogateUtils`**: a `char` is a UTF-16 code unit; everything past the BMP (every emoji) is a PAIR and
  either half alone is not a character. Anything that cuts a string BY LENGTH or addresses it BY INDEX uses
  this one implementation (each hand-rolled `Substring` produced the same broken half, rendered as U+FFFD).
  It does NOT do grapheme clustering (UAX #29): a combining mark is its own index, "e" + U+0301 is two
  characters. Full clustering needs a Unicode table plus a version to keep current
  (`Docs/Issues/PRE_RELEASE_STRUCTURAL_ANALYSIS.md` section 7 prices it). Only the cut is inspected: input
  that arrives already broken keeps its damage; repairing it would mean deciding what a character the
  caller never wrote should have been.
- **`ShortcutSyntax`** is the whole grammar of a keybinding, stored as a STRING because that is the form a
  person editing `settings.json`, a diff and a screenshot all read. It does not know which keys a
  keyboard has (a full key enum would be a second source of truth against the engine's, maintained against
  every key Unity adds): the SDK validates SHAPE (lowercase, canonical modifier order, alternate count,
  length), the engine side resolves the name against `UnityEngine.InputSystem.Key`, and an unrecognized
  name is shape-valid and resolves to the catalog default with one warning. `mouse.middle` and
  `gamepad.south` parse for the same reason: later pointer or pad bindings are a consumer change, not a
  format change. Canonical order is fixed so equality is string equality ("Shift+Ctrl+P" and
  "ctrl+shift+p" are one binding and the map cannot hold both).

## `UnityExtensions` leftovers

- `Math2D` holds the four rotation primitives as NON-extension statics, and `defaults`/`TransformDefaults`
  split by meaning - both in `UnityExtensions/CLAUDE.md`. `defaults.size` is zero because an empty size
  track is a group node with no extent; a type wanting the unit square gets an explicit keyframe at
  creation (the game's `SizeSeed`), since a seed can be deleted by the author and a fallback never can.
  `TransformDefaults` are NOT clamps and do not belong in `Rules/`: a clamp says what a value may be, these
  say what a transform IS before anything is authored onto it.
- `RectTransform2D`'s bounding box is the AABB, not the rect: a rotated rect no longer fits inside
  `FullSize`, and every caller that must CONTAIN it (camera framing, bounds tests) needs the box that does.
  Scale is taken absolute: a mirrored object covers exactly as much screen as an unmirrored one.
- `Tests/Approx` delegates vector comparison to `BHSDKMath` for scalars, because the consumer's `BHMath`
  cannot be referenced from here; it sits in tests since nothing shipping compares vectors.
- `SharedConstantsTests` pins the STRUCTURE of the constant tables (`alignment`, `color`, `color2`), not
  their values: a table cannot prove itself, and a wrong entry is never a compile error. `color2` is ~150
  hand-written `(color.x, color.x)` pairs, which can drift in three silent ways (name in one table only,
  either half disagreeing). Every named colour is opaque except the deliberate `clear`: a transparent alpha
  silently puts its user on the transparent render path.
- `RectTransform2DTests` worked examples compute parent centre, anchor, self offset and rotation by hand;
  the arithmetic is the assertion's expected value and is not repeated in comments.

## Interop (`Interop/`)

- An `InteropReport` is everything a conversion had to give up, for both directions, meant to be SHOWN: a
  silent lossy import is the failure it prevents. It is not a `RuleIssue`: nothing is wrong with the data,
  the two formats disagree.
- **Issues aggregate by `Code`**, not one per occurrence: a level whose every object uses an unsupported
  feature yields one line with a count. The issue used to keep one path and a count, which was unusable at
  real size (a code firing on 8000 objects surfaced one example and a number). It keeps a BOUNDED list of
  paths (`MaxTrackedPaths`), and says when it was cut: unbounded is the wall of text the aggregation exists
  to avoid and grows with the level, not the finding.
- **Paths, not object ids**: an id would be threaded through every converter call site and is the wrong
  address twice (ids are minted per scope, so one number names different objects inside a template and at
  level scope; and an id is not what an author looks at). A path is the source document's own address,
  already passed to every report call, naming what the author can open.
- **Merging replays each half from the half that holds it**: counts and paths are different lengths (an
  issue hit 4000 times keeps at most `MaxTrackedPaths` of them), so replaying one path per count would lose
  paths or invent them. Paths come from the list, the remaining count as bare increments with no path.

## `Samples~/ConsoleSmoke`

The SDK as a third party sees it: a DLL, a level folder, nothing else. Exit codes: 0 ok, 1 usage, 2 missing
file, 3 round-trip mismatch, 4 newer than this SDK (so a script can run it).

## The folder at a glance (moved from `Utils/CLAUDE.md`)

The folder file keeps the traps; this is its catalogue, with the facts not stated elsewhere.

- `BHSDKMath` - Unity-independent math, since the core assembly can't reference `UnityEngine.Mathf`.
  `DimensionalIndexer2` - flat-array to 2D-grid indexing. `LevelUtils` - `RectObject`
  id/parent/bounds setters. `ModelUtils` - deep-copy/equality/hash helpers for
  `List`/`Dictionary`/`Array` of `ICopyable<T>`. `ModificationUtils`/`TypeExtensions`.
- `LevelCapacityUtils` (sweep-line "peak simultaneous objects per type", backing `LevelHints.Limits`)
  and `LevelStatsUtils` (`LevelStats`/`LevelObjectStats`/`LevelResourceStats` - what a level *holds*,
  one O(n) allocation-free pass, for showing rather than sizing) answer different questions; don't
  use either for the other's.
- **`PrefabVirtualizationUtils`** (+`PrefabExpandReport`) keeps a placement's materialized copies OUT
  of the file: `Thin` drops them before a write, `Expand` rebuilds them from `pfid` + `ids` + `mod`
  after a read, and the level every other system sees is flat either way (`Docs/PREFAB_PLACEMENT.md`;
  the Unity project's `Docs/Issues/PREFAB_VIRTUALIZATION_HISTORY.md` is the record). **It never mints
  an outer id** (ids are born at edit time, in the consumer's `PrefabMaterializer`; a missing one is
  reported), and **`Expand` writes by INDEXER**, which is what lets a level saved with copies AND table
  both present open without doubling them (a change that landed before 1.0.0, when Rule 11 still
  meant no migrations).
- **`ObjectDepthUtils`** - parent-chain depth over an `IObjectScope`, bounded by
  `LevelRules.MaxObjectDepth`. It lives in the SDK because `Expand` has to refuse the same chains and
  cannot reach `BH.Core`; the consumer's `BH.Core.Utils.HierarchyMath` is a forwarding facade over it.
  **The template root costs no depth** only while `Prefab.Root` stays a field: `GetTemplateDepth`
  walks `Objects.Values` alone and stops on `ObjectId.PrefabRoot` without incrementing.
- **`PrefabRootUtils`** - the `Prefab.Root` ownership table as code, in the SDK because its two
  consumers (the edit-time `PrefabMaterializer` and the load-time `Expand`) only stay correct
  together. `ApplyRoot` writes the template-owned half onto the placement (`Name`, the seven
  positional tracks copied not aliased, and the span's DURATION) and leaves the span's start, `Active`
  and `Layer` alone - the last two are pinned on the template by `RulePrefabRootFixed` because nothing
  copies them. `SetTemplateLength` is the one writer of that duration, spelling the pinned half of
  `Root.Span` (start at `FrameRules.MinFrame`, no anchors) once; a template's whole timeline is that
  span, `Prefab` carrying no `FrameDuration`. `TryGetModificationTarget` is what makes a root-keyed
  `Modification` land at all: the root is never a key in `placement.ObjectIds`, so without it such an
  override is recorded, saved, shown and silently never applied (Unity project's
  `Docs/Issues/PREFAB_ROOT_HISTORY.md`).
- **`ObjectScopeExtensions`** - the ONE place an `ObjectId` becomes a `RectObject`
  (`TryGetObject`/`GetObject`): nothing that resolves an id through `scope.Objects` can see a
  template's root. Deliberately NOT applied to loops that ENUMERATE a scope - those ask what a
  template HOLDS, and the root is what content hangs off, not content.
- `ShapeLoopUtils` - radial loops, the two emitters every built-in shape is built from (a fan and a
  resampled annulus) plus sector clipping; `Docs/SHAPES.md` explains relative angles and the
  resampled rims. `ShapeSynthUtils` - procedural geometry for shapes the library cannot name; `Build`
  is the sanitizing wrap `ShapeCatalogService` goes through. `ShapeGeometryUtils`
  (+`ShapeGeometryReport`) - the single "what a valid shape is / how to make one valid", shared by
  `RuleShapeGeometry.Fix` and the consumer's shape editor on Save; its `Sanitize` order (clamp → weld
  → drop malformed → drop degenerate → trim → drop orphans → fix winding) is load-bearing - every step
  can reintroduce a problem an earlier one fixed, and this is the order in which none does.
- **`ResourceReferenceUtils`** - what still points at a resource, as a COUNT per family: the question
  in front of a delete (`LevelGraphAnalyzer.UnresolvedReference` answers the opposite direction, the
  state a delete creates). EVERY scope is walked, `Prefab.Root` included, and a NULL id counts nothing.
  A `ThemeRef` colour is not a theme reference (it stores a slot INDEX); only the theme track names a
  `ThemeId`.
- **`SurrogateUtils`** - where a string may be cut. Every length ceiling counts UTF-16 CODE UNITS; a
  cut between a surrogate pair renders U+FFFD and reports nothing. `ClampLength`/`Truncate` give the
  shorter answer; `IsLead`/`IsTrail`/`IsPair` are plain `char` arithmetic, so the consumer's Burst text
  job shares them. Used by `RuleStringMax`/`RuleIStringMax`' fixes, `FontCharacterService`, and
  `GamePlayer`'s `TextsProvider`/`TextComposeJob`. **Not grapheme clustering** (UAX #29), deliberately
  (Unity project's `Docs/Issues/PRE_RELEASE_STRUCTURAL_ANALYSIS.md` §7).
- **`ResourceGraph` is the ONE list of reference edges**, including `EffectObjectCore.ParticleShapeId`
  and id-valued `Modification`s (through `ModificationValues.TryConvert`). `ResourceReferenceUtils`,
  `ResourceClosure` and `ResourceRemap` all walk it, so a new reference field is added in ONE place.
  `ResourceRef` is a typed reference to any of the seven kinds (`Guid` data kinds, `int` file kinds -
  `ResourceTypeExtensions.IsGuidAddressed`), `ResourceSet` a set of them; `ResourceNaming` allocates
  the next user id and a unique file name. Full record: `Docs/RESOURCE_GRAPH.md`.
