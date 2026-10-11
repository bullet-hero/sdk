Design facts of the data resources in `Models/Data/` that the code cannot say by itself. A contributor
changing `ThemeData`, `CompositeShape` or `CachedFontText` reads this first.

## ThemeData

A theme is a fixed palette of `ValueRules.ThemeCount` colours (an 8x8 grid) that a level addresses by
slot index; the active theme is chosen over time by `ThemeKeyframe`. Slots start at 1 in the table below
(`ColorType.ThemeRef` stores a 0-based index, the bands are written 1-based):

| Slots | Meaning |
|---|---|
| 1 | fallback colour, used when an index is not found |
| 2 | GUI |
| 3 | background |
| 4-7 | players |
| 8 | tail |
| 17-25 | objects |
| 33-41 | parallax |
| 49-57 | effects |
| 9-16, 26-32, 42-48, 58-64 | free |

`ColorNames` is a different question from a slot's band: it is what an author calls a slot. It is
`null` when nothing was ever named and writes as one null rather than 64 empty strings (a collection
is never omitted whole, `Docs/NAMING.md`); a non-null list is exactly `ThemeCount` long and an empty or
null entry means that one slot is unnamed. The shipped Balanced preset names its slots by hue and
ignores the bands, so nothing derives a name from a band.

From `Models/CLAUDE.md` ("Theme"), which keeps the summary:

- `ThemeData` (`Level.Resources.Themes`) holds a fixed `Color4Value[64] Matrix` (the 8x8 grid above)
  and, beside it, the NULLABLE `List<string> ColorNames` (`clrn`) of the same length. **Null is the
  normal state.** It is `[RuleOptional, RuleCollectionCount]`, and it is the first member of the format
  that is BOTH a collection and legally absent - which is what forced `RuleWalk`'s three `Descend*` to
  answer null, since the generated walk calls those INSTEAD of `Check` and never reaches
  `[RuleOptional]`'s own short-circuit. The band comment inside `ThemeData.cs` says what a slot is FOR
  and is a different question from its name.
- `ThemeKeyframe` (a real animated track, unlike `Marker`/`Checkpoint`) selects which `ThemeId` is
  **active** over time. `ColorType.ThemeRef` (`Color3ThemeRef`/`Color4ThemeRef`) stores only a raw
  `int ThemeColorIndex` (0-63) - **not** a `ThemeId` - indexing into whichever theme is currently
  active. Two-level indirection: `ThemeKeyframe` picks the theme, `ThemeRef` picks a slot within it.

## CompositeShape

- **Stored indexed** (vertices + indices), not as a triangle soup. A render mesh wants shared vertices
  and collision wants contiguous triples; indexed converts to the other without guessing, while
  welding by epsilon goes the other way and two triangles meeting on an edge do not agree bit for bit
  once dragged. It is also what the shape editor edits: a corner shared by three triangles is one point.
- **A shape carries no pivot, by decision.** `RectObject.Pivot` is the one pivot (per object, keyframed)
  and the renderer and narrowphase both map through it (`CollisionUtils.LocalToGlobalFast`). A
  `ShapeObject` holds two independent ids into the collection (`ShapeId` drawn, `ColliderId` hit), so a
  pivot stored on the shape would give an object whose ids differ two answers for one transform, and a
  tie-break puts the hitbox away from the art. A shape is also shared: editing its pivot would move
  every object using it, in every level already written. "Rotate about where it balances" is derived:
  `ShapeGeometryUtils.GetCentroidPivot`, offered as a pivot preset in the editor.
- **No collection rule beyond `RuleNotNull` on either list**: every generic collection fix is
  index-destructive here. `RuleCollectionNoNullItems` would remove a null vertex and shift every later
  index onto the wrong point; `RuleCollectionMaxCount` would truncate the vertex list under the
  triangles still referencing its tail. `RuleShapeGeometry` owns it because only it sees both lists: it
  replaces nulls in place and reaches the vertex cap by trimming whole triangles, then dropping the
  vertices left unreferenced.

## CachedFontText

Carries its own `FontResourceId` so `LevelHints.FontCharacters` serializes like every resource
dictionary - a plain array with the key recovered from the value (`DictionaryAsListConverter`). A bare
id-to-`IString` map would need the `{k, v}` pair form, which reads differently from every other keyed
collection for no reason a reader of the file could guess.

## EffectData (Models/Effects)

The shape, from `Models/CLAUDE.md` ("Effects"), which keeps the summary: `EffectObject.EffectId` points
at an `EffectData` resource (`Models/Data/EffectData.cs`, `Level.Resources.Effects`) - the SDK-side
analog of Unity's `EffectGameStatic` index table. `EffectData` groups: `Core`
(render/loop/particle-count/lifetime/texture/pivot), `Forces` (gravity/velocity/orbital/linear-force),
plus 4 independently-polymorphic sub-shapes:

- `IEffectAngle`/`IEffectColor`/`IEffectScale` - each: Value / CurvesOverLife / CurvesBySpeed /
  RandomUniform / RandomPerComponent. The Random pair is structurally identical and differs only in
  evaluation semantics; `IEffectColor` names its curve pair `GradientOverLife`/`GradientBySpeed` and
  adds a sixth, `GradientRandom`.
- `IEffectShape` - Point/Circle/Rectangle/Line/Cone/Torus, with Circle/Line/Cone/Torus additionally
  nesting an `IEffectShapeSpread`: Random/Loop/PingPong/Sine. `Sine` and `Point` carry zero fields,
  pure enum-selected behavior.

- **No world/local switch in the format.** A level's particles are always local: they ride their object's
  transform, so a moving parent, a loop and a scrub backwards all behave. World-space emission exists
  only in the game's own engine (a host-side flag no authored data writes) and cannot be replayed by
  anything that writes one transform per frame and simulates a window of history under it - which is
  what a scrub, an edit and a jump all do. The format cannot guarantee the host honours this: a local
  graph with World-marked input slots emitted a world-fixed jet. The host pins it (`EffectGraphSpaceTests`);
  the measurement is `Docs/Issues/EFFECTS_HISTORY.md` of the consuming project.
- **`ParticleCount` is a rate, not a population.** A looping system feeds it to a constant spawn rate, so
  what is alive settles at count x average lifetime; a one-shot burst is handed that same product. Nothing
  in the SDK bounds the product (`ParticleCount_Max` caps the rate, `LimitHints` counts emitters), and the
  graph's own capacity clamps silently, so a legal rate with a long lifetime can ask for more than the
  runtime holds. A rename is a format change nobody asked for.
- **The eleven forces are born null** (a default `EffectData` was 45% polymorphic neutrals). Unlike
  `LevelTrackEffects` this is compression, not a third state: null reads back as the neutral
  (`EffectForcesState.Default`), and zero gravity is both the neutral and a legal value.
- **`EffectShapeCone` is flattened in 2D**, so its arc is a fan, not a sector: one of the two swept axes
  is kept and the arc sweeps edge to edge. A full turn is the whole fan; a partial one is decided by the
  phase, which starts at +X like the other two shapes.
