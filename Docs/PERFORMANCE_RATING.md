# The level performance rating: the SDK half

A level's demand (`LevelMeta.LevelDemand`) and a device's profile (`DeviceProfile`) combine into one
score, 100 being exactly the frame budget at the target frame rate. This file is what the SDK owns of
it; the client's half (sampling the area, the stress test, the screens) is the game repository's
`Docs/Architecture/PERFORMANCE_RATING.md`.

## Dimensions and windows

- **Twelve dimensions** (`DemandDimension`), each a MEAN over a window: alive objects, keyframes in
  their tracks, ancestor links, colliders while player collision is on, texts, glyphs, effects,
  transparent shapes, distinct opaque (texture, mesh) batches, the two screen areas and active
  post-processing passes. The enum's order is the index of every stored vector and every coefficient,
  so changing it raises `PerformanceRules.AnalyzerVersion`, which turns every stored demand into
  "unknown" rather than into a wrong number.
- **Windows** are `PerformanceRules.WindowSeconds` long, one every `PerformanceRules.WindowHopSeconds`
  (`DemandWindows`). A lifetime is integrated exactly: `|span ∩ window| / WindowSeconds`, so an object
  alive for two frames counts as two frames, which sampling would miss.
- **The counts are SDK-only** (`LevelDemandCounter`) and use the AUTHORED span, which can only be wider
  than the resolved one: a server's recount is an upper bound of the client's. The area dimensions need
  transforms and the camera and are left zero here.
- `ShapeId` Null draws nothing, so it is counted under Instances and under no draw dimension. A batch
  is alive while any of its shapes is (the union of their spans). Glyphs are non-whitespace code points
  of the longest language. A post-processing track plays its bounding START key, as the client's job
  does; an empty track is off, and the events' own `Active` switches all of them.

## Why storing only the front loses nothing

`T(w)` - for every device and every setting - is non-decreasing in every dimension (non-negative
coefficients and multipliers, a max of two sums). A dominated window can therefore never be the
maximum, so the maximum over the Pareto front (`ParetoFront`) equals the maximum over all windows, for
any profile. Above `PerformanceRules.MaxDemandWindows` the two closest windows (L1) are replaced by
their component-wise maximum, which dominates both: the stored maximum can only over-read, and
`LevelDemand.Exact` says whether that happened. A merged window keeps the earliest start of its
sources. `ParetoFrontTests` pins both claims over random sets.

## The score

`PerformanceScore.Evaluate`: per window `max(cpuBase + Σ c_cpu·s·x, gpuBase + Σ c_gpu·s·x)`, the peak
over windows, as a percentage of `1000 / targetFps`. The settings multipliers:

- render scale `r`: both areas and the post passes times `r²`
- anti-aliasing: both areas times the profile's factor for the mode (`PerformanceRules.AreaFactorIndex`)
- effects off: zero; effects at a fixed rate `e`: `min(1, e / targetFps)`
- post-processing off: the passes are zero, and one pass comes back under FXAA

A null demand, a demand of another analyzer version, a null profile or a target of zero fps is
`Unknown`, never zero. A known score reads in one of five `PerformanceBand`s, split at
`PerformanceRules.ScoreWarning`, `ScoreLimit`, `ScoreHeavy` and `ScoreSevere`; a level holds in the
first two (`PerformanceBands.Holds`). A band is never stored, so its enum may be renumbered.

## The stress levels

`StressLevelGenerator.Create(k, n, baseline)` is a level two windows long in which only dimension `k`
moves with `n`: every level of one sweep carries the same pool of slots (`StressBaseline.Pool`), each
alive the whole level with one size key, and a unit only changes what a slot IS - a hidden collider, an
empty text, a tiny transparent square, an opaque square of a distinct catalog mesh, a square sized to
`StressLevelGenerator.UnitArea` of the pinned 16:9 screen. Keyframes and parent links are spread over
the slots (a shared chain gives the depth), glyphs over pool-wide texts, passes over the twelve tracks.
`StressLevelGeneratorTests` proves, for every dimension, that the counter sees only that dimension
move. `PoolFor` picks the smallest power-of-two pool that holds a count, stops where a bigger pool holds
no more (the opaque batches end at the catalog's size), and never passes `MaxPool`, the most slots a
level holds beside the spine; `Create` refuses a bigger pool rather than allocate it.

`CreateMix(parts)` puts several dimensions in one level for the additivity check: each `StressPart`
gets its own block of `PoolFor` slots behind the one spine, so the mix counts exactly the sum of its
parts' own levels with the spine counted once (`CreateMix_CountsAreTheSumOfItsParts`). That is what
makes a mismatch on a device a property of the device, not of the level.

## Checking the model

`PerformanceValidation` is the arithmetic of the spec's experiments, pure so a device and the tests
compute it alike:

- `Spearman`: rank correlation, ties at their mean rank; NaN for a series that does not vary (a
  level whose every window predicts the same has no order to check).
- `RelativeError(predicted, measured)`: against the measured value.
- `JensenGap(cpu, gpu)`: `mean(max(cpu, gpu)) - max(mean cpu, mean gpu)` over frames, what the max of
  the two sides' means misses when the bound side flips (check 8).
- `Compare(predicted, measured, targetFps)`: both peaks, their error, both bands, and a **false hold**
  - predicted at or below `PerformanceRules.ScoreLimit`, measured above it, the one error check 9
  allows none of. `Passed` is no false hold, peak error within `PerformanceRules.MaxPeakError` and
  Spearman at least `PerformanceRules.MinRankCorrelation` (or NaN) - unless the level is flat
  (`IsFlat`: the measured windows' 90th percentile less their 10th, `MeasuredSpread`, is under
  `PerformanceRules.NoiseFloorMs`), where the order of the windows is noise and only the peak and the
  bands decide.
- `PassesAdditivity(predicted, measured)`: check 7, within `PerformanceRules.MaxAdditivityError` or
  under `PerformanceRules.NoiseFloorMs` in milliseconds.
- `PerformanceScore.WindowSides` is one window's predicted time, both sides.

## The device profile

A new domain in its own file (`device_profile.json`, `ModelDomains.DeviceProfile`), so no bump. It
holds the coefficients both sides, the base frame time, each fit's R², the anti-aliasing factors, the
thermal drift and whether every gate passed (`Trusted`); `DeviceProfile.IsUsable` is the one test of
whether a score may use it, and it does not read `Trusted`: an untrusted profile is still the device's
own measurement, closer than a reference, and the flag is advice to measure again.

## Reference devices

`ReferenceDeviceProfiles` are the profiles a device scores with until it measures itself, the same
constants a server scores with. Two are stress test runs (2026-10-17): `MidAndroid` a Redmi Note 12
Pro (Adreno 618, `Trusted` false: Instances missed check 6 by 0.02 of R²), `Pc` an RTX 4060 Ti desktop.
Two are **estimates** until such a device is measured (`IsEstimate`): `WeakAndroid` is the Redmi with
the CPU side and its base doubled, the GPU count dimensions times 2.5 and the screen-sized ones (both
areas, the passes, the GPU base) times 1.2 for a 720p screen; `LaptopIGpu` is the PC with the CPU times
1.6 and the GPU times 8, MSAA costing more ([1, 1.9, 2.8, 4.2, 1.4]). Replacing an estimate is pasting a
measured profile over it.

`BenchmarkMath` is the stress test's
arithmetic (least squares with R² and the fitted rise, median, drift, the sweep's unit counts). A sweep
passes linearity when its fit is linear or flat - rising less than `PerformanceRules.NoiseFloorMs`, where
R² is only the noise of the medians - and the thermal check when the drift is small or both passes are
flat.
