# CLAUDE.md - Assets/Plugins/BH.SDK/Validations

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

The rule engine, in two halves. *Declarative*: `RuleAnalyzer`/`RuleFixer` (+`RuleIssue`/`RulePath`)
walk `[RuleContainer]`-marked object graphs and check/auto-fix every `[RuleXxx]`-attributed property,
one property at a time. *Relational*: `LevelGraphAnalyzer` (+`GraphRule`/`GraphIssue`) checks the
cross-object invariants a per-property attribute cannot see. `ValidationFacade`/`ValidationReport`
run both and are what a consumer calls. **Opt-in tooling, not wired into save/load anywhere**, and
on the editor's load path only (`LevelValidation.Skip` for playback).

Everything below in full: `Docs/VALIDATIONS.md` ("Rules, severities and the attribute families",
"RuleAnalyzer and RuleWalk").

## Traps

- **Every rule states its `RuleGroup`** (criterion at the top of `RuleGroup.cs`), or is listed in
  `RuleSeverityTests`' `IntentionallyError` - a new rule cannot inherit Error by silence.
- **No collection rule on `CompositeShape.Vertices`/`Indices`** - every generic collection fix is
  index-destructive; `RuleShapeGeometry` owns the geometry.
- **A member that may be null needs `[RuleOptional]`**, or every bound on it reports an absent value.
- **`[Flags]` enums take `RuleEnumFlagsValid`**; never loosen `RuleEnumValid` (`Enum.IsDefined`
  rejects every flag combination).
- **A cross-object check is a graph rule, never a `[RuleXxx]` attribute.**
- **`RuleFixer` repairs in reverse trace order**; the analyzer logs nothing - policy is the caller's.
- **`[RuleContainer]` is inherited but the generator sees only DECLARED attributes** - declare it on
  the type itself (`RuleContainerCoverageTests`). A container needs a public parameterless
  constructor (`RuleContainerAnalyzer`).
- **The reflective walk stays**: `RuleAnalyzerSettings.useGeneratedWalk` is the parity proof
  (`Core.Tests.ValidationParityTests`). `IsValidType` is hoisted per type, never out of the rule loop.
- **Rule order is reflection's order** (`RuleTable` is deliberately not generated); a stale
  `BH.SDK.Roslyn.dll` throws at analyzer construction.
