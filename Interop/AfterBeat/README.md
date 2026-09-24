# Afterbeat interop

Two-way conversion between this format and **Afterbeat** (formerly *Project Arrhythmia*, by Vitamin
Games): its level (`.vgd`), metadata (`.vgm`), theme (`.vgt`) and prefab (`.vgp`) documents.

`ABInterop` is the entry point — one method per thing a host converts. Everything here takes
and returns **text**, never a path: this LAYER reads no files - whatever the rest of the SDK does,
and it does now (`Services/Content`) - and where a document came from is
the host's business.

## Where the format description came from

These models were transcribed from the official Afterbeat wiki. Every page below was read at
transcription time (August 2026).

**The wiki is no longer the authority, though, and where the two disagree the game wins.** Three
other sources have since been read, in this order of trust:

1. **The game's own decompiled code** — `BeatmapObject`, `DataManager`, `EventManager`,
   `ObjectManager`, `LSEffectsManager`, `ObjectHelpers`. This is what settled every enum's real
   numbering, every post-processing scale, what `er` on a keyframe actually means, and the fact that
   `game_version` is parsed rather than displayed.
2. **The game's own shipped data** — `Afterbeat_Data/level2`, which holds the Inspector-authored
   lists the code only references: the 21 default themes, and the 23 easing names a curve may carry.
3. **Real levels** — measured, not read. Key frequencies, value ranges and start-time distributions
   over **five** workshop levels and their autosaves (20 415 root objects, 4 774 template objects,
   March 2024 to August 2026), which is what tells a value that does not exist from one that merely
   never came up. It was one level for a long time, and widening it to five found ten more losses a
   player can see — see `docs/issues/AFTERBEAT_ISSUE.md` §11.

The wiki was wrong or silent on, among others: `ShapeType` 3 (Misc, not Arrow), `AutoKillType` 0,
randomization type 2, the fact that `er` is a range END rather than an offset, the default `p_t`,
the object-type numbering real files use, and every one of the post-processing ranges.

| Page | What it covers |
|---|---|
| [VGD format](https://afterbeat.wiki.gg/wiki/VGD_format) | the level document: editor block, triggers, parallax, checkpoints, objects, prefab placements, prefabs, themes, markers, and the fourteen `events` arrays |
| [Object Data](https://afterbeat.wiki.gg/wiki/Object_Data) | one object's fields and its four keyframe tracks; the shape, object-type, autokill, gradient and randomization tables |
| [VGP format](https://afterbeat.wiki.gg/wiki/VGP_format) | the prefab document and its type table |
| [VGT format](https://afterbeat.wiki.gg/wiki/VGT_format) | the theme document and its 34 colours |
| [VGM format](https://afterbeat.wiki.gg/wiki/VGM_format) | the metadata document; difficulty, artist link and game-reference tables |
| [Level folder](https://afterbeat.wiki.gg/wiki/Level_folder) | what a level folder holds — **a stub at the time of writing**, so the file names below were recovered from the changelog pages instead |

Two caveats the wiki states about itself, both load-bearing here:

- **`Object Data` is marked `{{Proofread}}`** ("mostly copied directly") and its shape table is
  behind the game: the editor has custom polygon shapes (sides, roundness, thickness, slices,
  inverted) whose JSON keys are undocumented. This is the direct reason every model in `Models/`
  derives from `ABNode` and carries `[JsonExtensionData]` — unknown keys survive a round trip
  instead of being silently deleted on the first export.
- **`Level folder` is a stub.** File names (`level.vgd`, `cover.jpg`, the song as `.ogg`/`.mp3`/
  `.wav`) come from the version-history pages rather than from a specification, so a host should
  probe for them rather than assume them.

The format carries **no version field** at all. Compatibility is expressed per key as "optional,
defaults to X", which is why every property in `Models/` is initialised to the documented default
rather than to the CLR's.

## Layout

```
ABNames.cs          every JSON key, deliberately separate from Models/Names.cs
ABEnums.cs          every enum table, with the format's own numbering and its gaps
ABOptions.cs        the choices a conversion cannot make on its own
ABSerialization.cs  its own JsonSerializerSettings - see below
ABInterop.cs        the entry point
Models/                    the wire models, one file per document
Maps/                      Time, Ease, Shape, Theme, Colour, Value(random), Id
Import/                    Afterbeat -> this format
Export/                    this format -> Afterbeat
```

`Generators/Interop/ABLevelGenerator.cs` wraps the import as `gen_level_afterbeat`, so the
editor gets a form, an estimate and a preset system for free. The export is **not** a generator: a
generator produces content, an export consumes a level.

## Three decisions worth knowing before changing anything here

**These documents do not go through `SerializationService`.** That service wraps every
`[ModelGeneration]` aggregate in a `{"g", "v"}` envelope and installs two dozen converters
implementing *this* format's polymorphism. Both would corrupt a foreign document. `Interop/` is also
not `Versions/`: that machinery upgrades this format's own domains between generations, and a
foreign format is not a generation of it.

**Unknown keys are preserved** (`[JsonExtensionData]` on `ABNode`) — see the caveat above.

**Ids are derived, never freshly generated.** Afterbeat names themes and prefabs with arbitrary
strings; `ABIdMap` hashes those into stable Guids, so re-importing a level, or importing a
`.vgt` and then a `.vgd` that references it, produce the same id both times.

## What crosses, and what does not

Everything lost or approximated goes into an `InteropReport` — aggregated by cause, with a count and
the first place it happened. A silent lossy import is the failure mode the whole class exists to
prevent.

> The summary below is the short version. The **full mapping standard** — every enum table, every
> slot index, every conversion law, and the complete ledger of what is skipped in each direction —
> lives in the consuming project at `docs/issues/AFTERBEAT_ISSUE.md`. Read that before changing any
> number in `Maps/`.

**Exact, both ways:** themes (Afterbeat's 34 colours are exactly the Project Arrhythmia slot layout
`ThemeData` already uses, minus alpha, which `.vgt` has no channel for) — including the 21 the game
ships, which a level references by index without storing, and which are materialized into the
converted level as ordinary custom themes (`Maps/ABDefaultThemes`) — markers, checkpoints
including their respawn position, object hierarchy, and the object/prefab structure.

**Converted, with a rule worth remembering:**

| Thing | There | Here |
|---|---|---|
| time | seconds | frames at the level's framerate |
| object lifetime | start + an autokill rule | a half-open `FrameSpan` |
| keyframe time | relative to the object's start | the same — **not** rebased |
| rotation | degrees, each key relative to the previous | radians, absolute |
| draw order | absolute depth 0–60 (smaller in front) inside one of three render bands | parent-relative `Layer`, higher in front, four import modes — see below |
| object colour | theme slot + an opacity in **percent** | `ThemeRef` at full opacity, a literal below it |
| whether an object hurts the player | its type **and** its current opacity — the damage check refuses anything whose alpha is below 1 | its type decides `ColliderId`; the opacity half becomes WHEN that collider exists — see below |
| object gradient | a per-pixel ramp: two theme slots plus a type, a direction in degrees and a length (`gt`/`gr`/`gs`) | the ramp **sampled at the four corners** of the object's box, landing in the narrowest colour keyframe those four samples fit — see below |
| camera zoom | the camera's orthographic size (half-height), default 20 | `Zoom`, the whole visible height — so **doubled** |
| camera-parented objects | hang off a node scaled by `zoom / 20`, so they keep a constant screen size | that node is rebuilt as an ordinary object and they hang off it; the export flattens it away again |
| background | a subsystem of its own, plus the theme's background colour | the theme's background slot referenced on the `Backgrounds` track; the parallax becomes objects |
| text | a scale and no font size, no bounds at all; the authored string reaches TextMeshPro untouched, so its markup vocabulary is TMP's own | `Scale` carries the source scale; `Size` is estimated at one cell per character and per line; the tag names this format shares with TMP are parsed here too, while `<rotate>` — the one with no counterpart — is removed on import (`text_rotate_tag`), because an unparsed tag is drawn as its own literal characters |
| a text's typeface | no field at all: a `<font>` tag inside the string itself, resolved by name against the ten font assets the game ships in `Resources/Fonts & Materials` | a property of the object, so the tag is removed and `FontResourceId` is set from it (`ABFontMap`, pairing by role — monospace onto monospace, comic onto comic); a string naming several typefaces keeps the one covering most of its characters (`text_font_mixed`), a name the source game itself resolves to nothing is no loss and is reported as none. The export writes the tag back, naming the nearest font that game ships (`text_font_tag`); only a font the level brings along itself has no name over there (`text_font`) |
| post-processing | every effect is keyframed whether used or not | each keyframe arrives switched **on exactly when its intensity is non-zero**, which is the rule the source game itself applies before writing to the volume |
| parent inheritance | position / scale / rotation switchable per child (`p_t`, default `101` — no scale) | one transform, always all three — except the **scale** bit, which crosses exactly as the choice of `Size` (does not propagate) vs `Scale` (does); `000` becomes a root |
| the frame | nothing in the file, and nothing enforced at play time — but every window resolution the game offers is 16:9, so that is what every level was authored inside | a `Fixed` 16:9 screen limit on the first frame. **Import only** — the export writes none, since the target format has no field and always runs at that aspect anyway |
| shapes | 25 `(shape, option)` pairs | 78 presets, plus synthesized geometry for the seven combinations no preset covers |
| parallax | a background subsystem | ordinary collider-less objects with the loop baked into keyframes |
| a rotated child under a non-uniformly scaled parent | a matrix product, so the child is genuinely **skewed** | the nearest rotation and scale there is (`ABLinearFit`, least squares in closed form) — exact at every quarter and straight angle, an approximation between them, and reported as `parent_scale_shear` only where a residue is actually left |

### Opacity decides damage over there (`ABOpacityHitGate`)

The rule no document states and every level relies on: Afterbeat never disables an object's collider,
it vetoes the damage. `VGPlayer.CheckForObjectCollision` reads the object's material and returns
false whenever `_BaseColor.a < 1` (or `_Alpha < 1` on the gradient shader), and opacity is the only
thing that reaches either property. Three consequences, in the order they bite:

- **The threshold is below 1, not zero.** An object authored at a constant 35% is decoration for its
  whole life; one at 99% is already harmless.
- **A fade is intangible for its whole length**, not only at the transparent end. This is *why*
  authors fade things out — a splash, a shockwave, a telegraph — instead of killing them: the game
  hands them intangibility for free, so the collider outliving the visible object costs nothing.
- **Nothing on screen explains it.** An object faded to zero that keeps its hitbox kills the player
  with no visible cause, which is exactly what an import that ignores this produces. Measured on one
  real level: 964 objects carrying ~56 000 object-seconds of invisible lethal collider, plus 222 that
  are lethal for their entire life and were never meant to touch anybody.

`ColliderId` here is a per-object constant with no keyframe track, so "hits between these frames and
not those" cannot be said on one object. The import therefore splits it: an object whose opacity
crosses the boundary keeps drawing and gives up its own collider, and gains one invisible child
(`ShapeId` Null + a real `ColliderId`) per fully-opaque stretch, anchor-stretched over its parent's
whole rect so it inherits the motion, the size, the rotation and the `Active` flag for free. An
object opaque for its whole life — the overwhelming majority of every level — is untouched, so only
the levels using the rule pay for it.

**An author can overrule the rule: `ABOptions.OpacityHitThreshold`** (the import form's *Opacity hit
threshold*, in alpha). One — the default — is Afterbeat's own boundary and the only value that
reproduces the source level. Lowering it arms each collider for every stretch drawn at or above it,
so a shockwave stays lethal further into its fade; zero switches the pass off outright, leaving each
object holding its own collider for its whole life. It exists because the rule it relaxes is
*invisible* in this editor — a ring correctly lethal for a tenth of a second and inert for the next
three looks exactly like a hitbox lost in conversion — and below the default the report says the
level was deliberately made to differ rather than claiming fidelity.

**The stretches are resolved from the curve, frame by frame, not from the keyframes bounding a
segment.** The damage check re-reads the material every time it runs, so the easing between two keys
decides as much as the keys do: `Instant` holds the old opacity across its whole segment and drops
only on the key itself, `Back`/`Elastic` overshoot through 1 on their way to a value below it, and a
gentle fade sits at full alpha for exactly as long as its own shape says. A frame counts only when
both boundaries of its cell are at full alpha, which is what keeps a fade from ever arming a hitbox
the player has already seen start to disappear — at most half a frame earlier than the source game,
and in the only direction that cannot kill somebody unfairly.

**The export cannot undo this, and does not pretend to.** An object that draws nothing is an empty
over there and an empty cannot hit the player, so an invisible hitbox — whether this pass made it or
an author did — exports as an empty object and is reported as `collider_invisible`. That asymmetry is
the format's, not the converter's: the only invisible object Afterbeat has is a transparent one, and a
transparent one is precisely what its damage check refuses.

### Object gradients

Afterbeat evaluates an object's ramp per pixel; this format has four corner colours blended
bilinearly across the object's box. So the ramp is **sampled** at those four corners
(`Maps/ABGradientMap.cs`) rather than translated, and the result is the narrowest keyframe the four
samples fit into — a `Color4Key` when they agree, a `ColorHorizontalKey`/`ColorVerticalKey` when
they agree in pairs, a `Color4X4Key` only when the ramp is genuinely diagonal.

A bilinear field reproduces any affine function exactly, so the sampling is **pixel-exact whenever
`gs >= |cos gr| + |sin gr|`** — always at `gs >= sqrt(2)`, and at `gs >= 1` for an axis-aligned
angle. Below that the real ramp has saturated flat bands no bilinear field holds: the corner
colours stay right and the distribution comes out softer. A **radial** gradient is rotationally
symmetric and bilinear at no parameters at all, so its four corners always agree and it arrives
flat — at the colour of its *edge*, which is what its area is made of, rather than at the colour of
the single point at its centre (`gradient_radial`).

The cost is theme references. A corner sampled strictly between the two ends is a **blend** of two
theme colours, and no `Color4ThemeRef` expresses a blend, so such a corner has to become a literal
and that object stops following a theme change (`gradient_theme_flattened`). `ABOptions
.BakeGradientCorners` is the choice: on (default) keeps the ramp's shape and pays in references;
off snaps such a corner to its nearer end instead — a hard edge in place of a blend, every
reference alive (`gradient_corners_snapped`).

`gr` and `gs` do **not** survive a round trip, since this format stores no ramp of its own to hold
them: the export re-derives the direction from which keyframe type it is writing (`0` for
horizontal, `90` for vertical) and always writes `gs = 1`. **Parallax gradients are not imported at
all** — the source declares the same three fields on `ParallaxObject.ShapeData`, but a parallax
object carries only one colour index (`c`, clamped 0–9) and `ParallaxManager` never sets the
shader's second colour, so there is no second end to run a ramp between.

### Draw order

The one conversion with no single right answer, and `ABOptions.LayerImport` is where an
author picks:

| Mode | What decides draw order |
|---|---|
| `Packed` (default) | Depth orders anything alive at the same time, strictly; TIME decides the row - `ABLayoutPacker` packs intervals, so objects that never share the screen share a row. Children take own layers 1..N, a placement is one row whose template draws above it. Measured: weathergirl's worst row went from 128 simultaneous clips to 2. `CollidersAbovePlayer` (off) moves everything that can hurt the player to 1 and up - this project's convention rather than Afterbeat's. |
| `Auto` | Depth alone, packed into consecutive layers inside each band. Costs one layer per depth the level uses; every object sharing a depth shares a row. |
| `OnlyDepth` | Depth alone, one layer per depth the *format* has. The exact inverse of the export, so a level converted under it round-trips unchanged. |
| `OnlyEditor` | The source editor's own layers and bins alone (bin 0 of layer 1 furthest back); what the level *drew* in front is discarded. |
| `DepthAndEditor` | Both, each editor group given a fixed `EditorGroupStride`-wide band. Nothing is packed, so a finely organised level runs out of layers and is clamped. |

Three things every mode holds to.

**The player line.** Afterbeat sorts by `sortingOrder` in one sorting layer: an ordinary object gets
0, the player gets 61 (`VGPlayer.Init`), an `AbovePlayer` object gets `62 + (60 - depth)`, and a
`Background` object is drawn by a different camera. So the whole Default band is *behind* the player,
depth 0 included — depth only separates it by draw distance (`z = 0.1 * depth`). This format draws
its avatar at layer -0.5, so the Default band lands at layer <= -1 and `AbovePlayer` at layer >= 0.

**The three bands** — `rl`, i.e. Background / Default / AbovePlayer — never interleave; the
background objects the parallax importer creates sit below all three. Their boundaries are fixed
(`AbovePlayer` at 0 and up, `Default` at -1 down to -61, `Background` below that) and they are a
**contract with the export**, not an implementation detail: nothing on an object here records which
band it came from, so the export infers it from where the layer falls. `Auto` therefore packs each
band against its own edge of the player line rather than packing all three into one run — the cost
is a gap between `Default`'s last used layer and `Background`'s first, paid only by a level that
uses the `Background` band at all.

**One plan orders the whole level.** The level's own objects and every prefab template's are ranked
against a single table of depths, because that is what the source game does with them: a template's
objects are copied into the level and sorted against its own by depth alone. Under the legacy modes a
placement takes no layer of its own (`PlacementLayerOffset` defaults to 0), and the range a converted level
occupies is bounded by the source *format* — three bands of 61 depths — rather than by how large or
how finely organised the level is. Resolving each list separately and stacking the results with
offsets is what this replaced, and it spread one real level over 894 layers reaching -520.

**Going back out.** A placement Afterbeat can express - static, untrimmed, no overrides - is written
as a `prefab_objects` entry and its copies are NOT written (`ABPlacementExporter`); any other is
written as an EMPTY object with the copies it materialized into hanging off it, and the reason is
reported as `placement_flattened:<reason>`. Draw order and editor rows come from `ABDrawOrderMap`: a
level whose layers fit `[-121, 61]` keeps the fixed boundaries below, anything wider is mapped by rank
(band by sign, Background not inferred), and the editor row is the layer's rank from the top.
Under the fixed boundaries a level
imported under `OnlyDepth` returns with every depth and band unchanged, and one imported under `Auto`
returns with its depths *packed* — the order the source game drew them in survives, the exact numbers
do not. For a level authored **here** the same boundaries have a consequence worth knowing: layer 0
is the default for a new object and draws in front of this format's avatar, and the only way
Afterbeat can express that is the `AbovePlayer` band — so a level built entirely on layer 0 arrives
over there entirely above the player. That is faithful (content that covers the player here covers it
there), but it means an author who wants ordinary content should spend the layers below -1.

### Prefab structure

Decided on the parsed `.vgd` before anything is minted (`Import/ABPrefabExtractor`), because the
layout above needs the final set of entries:

| Option | Default | What it does |
|---|---|---|
| `FlattenNestedPrefabs` | on | a template's own placements (`pobjs`) are inlined under an empty carrying their transform - nothing this converter produces nests |
| `RestoreExpandedInstances` | on | objects Afterbeat's editor expanded out of a placement (`pre_iid`) become a placement again - of their template when untouched, of a new `<name> (edited)` one otherwise; a group tied to a live object outside itself stays expanded |
| `ExtractRepeatedSubtrees` | off | identical root subtrees become one template and a placement each |
| `MergeDuplicateTemplates` | off | templates with identical contents merge |

Created templates compete for `LevelRules.MaxPrefabs`. The facts behind the matching (what the
source's expand bakes and what it does not) are `Docs/Issues/AFTERBEAT_ISSUE.md` section 10.12.1.

**Not imported:** triggers, the screen-gradient event track, depth of field, per-axis parent
inheritance and parent time offsets, and prefab preview images and lead times.
**Two things are reported as deferred rather than dropped**, i.e. waiting on work rather than on a
decision: player force (`PlayerEvents.Velocities`/`VelocityPoints` exist in the model, commented
out) and the hue track, whose mapping onto colour curves is settled but is temporarily not written
while this project's own colour curves are being fixed.

**Not exported:** audio (an Afterbeat level is one song file in a folder — no
track list, offsets, speeds or effects), level-authored geometry, anchors, per-corner colours,
per-character text effects, random values, beat segments past the first, checkpoint spaces other
than World, several post-processing effects, per-instance prefab overrides, and — worth naming
separately — **licensing, age rating and attribution**, which `.vgm` has no field for at all.

### Particle emitters

`ot = 7` is an `EffectObject` in both directions, and the object it replaces is the point: an
Afterbeat emitter **draws no shape of its own** (`ObjectManager.InitVisual` spawns the particle
prefab and returns), so its `(shape, shapeOption)` is the particle's MESH rather than a quad. The
import used to build an ordinary `ShapeObject` out of it, which drew a static shape the level never
drew, in place of a stream of small ones.

The eight parameters are **not in `csp`**, whatever `BeatmapObject`'s field order suggests — they
live on the first position keyframe's own value array, `e[0].k[0].ev[4..11]`, each with its own
default and clamp (`Maps/ABParticleMap.cs`). Beyond them, an emitter's four tracks do **two jobs at
once**: values 0/1 keep their ordinary meaning and animate the emitter, while values 2/3 are a
hidden channel describing one particle over its own life. A particle lives exactly as long as the
object's own animation — the largest keyframe time across its four tracks, which is deliberately
*not* `ABTimeMap.GetLastKeyframeTime` (that one skips single-keyframe tracks, because it answers a
different question: how long the OBJECT lives).

**A circle emitter over there is an ellipse, and its arc starts at +X going counter-clockwise.**
`shape.radius` is never assigned, so it keeps the particle prefab's own value while `shape.scale`
multiplies it per axis — the authored scale IS the pair of semi-axes, and the horizontal one crosses
as `EffectShapeCircle.Radius` with the vertical one as its `Aspect`. Nothing is halved. Both facts
come from the source game's own editor gizmo rather than from the wiki
(`EditorEmptyRendering.cs:423-425`, `:444-449`), and the second one is why the shipped VFX graphs
were rotated to match: theirs used to start the arc at +Y and sweep clockwise, which turned an
authored dome into a sideways half-disc.

One `EffectData` is written per distinct definition, keyed by a canonical signature through
`ABIdMap.ToEffectId`, so re-importing a level produces the same ids and two emitters authored the
same way share one resource. The signature names the emitter's spawn volume too — it did not, and
two emitters differing only in the size particles spawn inside collapsed onto one definition. The stop frame is part of that signature rather than of the placement:
`EffectData` is shared, so two emitters agreeing on every parameter but not on how long they run are
genuinely two definitions.

An emitter that does **not** despawn on end outlives its own emission — its span is extended by one
particle lifetime and `HasStopLocalFrame`/`StopLocalFrame` end the emission where the object used to
end, which is exactly `length = logicalLength + particleMaxLifetime` over there.

**Easing has to be baked.** An Afterbeat keyframe carries an easing NAME and the source game samples
each eased segment at 16 points; a `CurveKeyframeValue` carries TANGENTS and no easing at all, while
`ValueRules.MaxCurveKeys` bounds the whole curve at 16. So `Maps/ABCurveMap.cs` keeps every authored
keyframe and shares what is left of the budget among the segments that actually bend — a Linear or
Instant segment spends nothing. `ABEaseMap.Evaluate` is the sampling half, and it exists here rather
than in `Utils/` because the format itself never evaluates an ease: a level keyframe stores one and
the consumer resolves it, and a particle curve is the one thing that has to be baked at conversion
time or lost.

**Named losses on import** — each fires only when the source actually used the thing:
`particle_world_space` (effects here always simulate in their own space, so those particles are
dragged along instead of left behind), `particle_velocity_curve` (the largest approximation in the
whole conversion — the channel is a POSITION over the particle's life whose derivative the source
game feeds to `velocityOverLifetime`, and only one start velocity crosses, so the travel is
flattened to its average), `particle_emitter_volume_animated` (an `EffectShape*` field is a value,
not a track, so only the first keyframe crosses), `particle_color_theme_lost` (a gradient stop is a
literal `Color4Value` by design, so the ramp is resolved once against the reference theme),
`particle_spawn_per_unit` (no distance-based emission exists here), `particle_start_speed` (no
radial-outward force), `particle_gradient_material`, plus `particle_curve_flattened` and
`particle_color_stops_capped` when a track carries more keyframes than a curve or a ramp can hold.

**Named losses on export** — an effect is a much larger thing than an emitter, so an effect authored
here rather than imported loses most of what it is: `particle_shape_unsupported` (Point/Line/Cone/
Torus), `particle_shape_spread`, `particle_scale_variant`/`particle_angle_variant`/
`particle_color_variant` (every Random and BySpeed form), `particle_forces` (the whole group past
the start velocity), `particle_texture`, `particle_render_off`, `particle_burst` (Afterbeat clears
the burst list unconditionally and only emits at a rate), `particle_lifetime_spread` (one lifetime,
no range), `particle_start_velocity` and `effect_unresolved`. `effect_resources` now means only what
it says: a definition **nothing places** has nowhere to go, since over there an emitter IS the
object.

## Calibration

Measured out of the shipped game rather than guessed — the decompiled source for the behaviour, the
build's own serialized assets (`sharedassets2.assets`) for the numbers the source does not carry.
These are what a 1:1 port has to match:

| Thing | Value | Where it was read |
|---|---|---|
| camera | `orthographicSize = camZoom`, default `20` | `EventManager.Update`; confirmed by `RenderBounds` drawing its frame at `orthoSize * 2` |
| visible area | **40 units tall**, 71.11 wide at 16:9 | derived from the above |
| aspect | **not forced in code** — read from `Screen.width/height` | no `camera.aspect` assignment, no letterbox anywhere |
| object at scale 1 | exactly **1 unit** | all shape radii are `0.5` (`ObjectManager`), the scale keyframe goes straight into `localScale` |
| player body | a **1×1 square**, drawn at scale 1 | prefab `new-player` → `Player` → `core`, mesh `square`, local AABB extent `0.5` |
| player hitbox | **circle, radius 0.25** (trigger), centred | `hit-collider`, `CircleCollider2D.m_Radius`, `CollisionType.Standard`, no scale anywhere up the chain |
| close-call radius | **1.5** | `close-call-collider`, `CollisionType.CloseCall` |
| player clamp | **1.2 units** inset from every edge, aspect-independent | `VGPlayer.EDGE_OFFSET = 0.03` of the viewport, × 40 |
| speeds | `22` normal, `80` boost, `720`°/s turn | `VGPlayer.DEFAULT_*` |

So the hitbox is **half the width of the drawn body** — 0.5 across against 1 — and the whole player
is 1/40 of the screen height at the neutral zoom. Nothing scales the player at runtime: the boost
and hit reactions deform mesh vertices (`MeshDeformation`) and shake the transform's position, never
its scale, and no event track, difficulty modifier or zen mode touches it.

## Testing

`Tests/Interop/AfterBeat/` covers each map on its own plus a round trip. `ABCorpusTests`
additionally runs every real level found under the folder named by the `BH_AFTERBEAT_CORPUS`
environment variable; with the variable unset, or the folder empty, it passes having checked nothing
and says so. No level files live in this repository — this is somebody else's user content.
