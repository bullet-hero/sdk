# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Tests

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Tests/

`BH.SDK.Tests.asmdef`, NUnit. **There is a SECOND test assembly**,
  `UnityExtensions/Tests/` (`BH.SDK.UnityExtensions.Tests`), and the split is forced rather
  than stylistic: this one is `noEngineReferences: true` and therefore cannot reference
  `UnityExtensions` at all. It holds `AvatarMovementTests`/`AvatarMovementStateTests`,
  `Transform2DTests`/`RectTransform2DTests`, and `Approx.cs` — a fixture helper, not a test, that
  delegates to `BHSDKMath.Approximately` so the tolerance still lives in one place. `MockData.cs` is the shared fixture factory and
  `Metadata.cs` the author/category constants (neither is a test) — read `MockData.cs`'s header
  comment before writing new tests that need a `Level`/`Prefab`/etc. Root-level files cover
  serialization (`SerializationTests`, `SerializationTypeExtensionsTests`), modification
  (`ModificationTests`), validation (`ValidatorTests`), capacity (`LevelCapacityUtilsTests`),
  cryptography, text formatting, `ShapeIdTests`, `ShapeGeometryUtilsTests` and `AvatarRulesTests`, plus
`Tests/Services/ShapeCatalogServiceTests` (the built-in shape library — id round trip, retired and
future-axis ids refused, and the two geometric invariants a person cannot eyeball across five
hundred entries: a shape and its inverse tile the sector they were cut from, and slices tile the
whole). **`Tests/Rules/` is the bulk** — 54 files,
  roughly one per `[RuleXxx]` attribute on top of `BaseRuleTests` (the shared analyze/fix harness),
  `RuleCoverageTests` (fails if a rule has no test file), `RuleContextTests`, `RulesConsistencyTests`,
  `LevelGraphAnalyzerTests`, `ValidationFacadeTests`, `ModificationCheckedWriteTests`. Five of them
  guard the GENERATED walk rather than a rule, and are what any change to it answers to:
  `ValidationParityTests` (three reports stated in full), `RuleWalkSeamTests` (a hand-written
  `IValidatable` compared against reflection - also the executable spec of what the generator must
  emit), `RuleContainerCoverageTests` (every container declares its marker and has a generated
  walk), `RuleSeverityTests` (a rule states its `Group` or is named as deliberately Error) and
  `ValidationFacadePublishTests`.
