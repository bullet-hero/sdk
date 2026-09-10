# CLAUDE.md — Assets/Plugins/BulletHeroSDK/UnityExtensions

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## UnityExtensions/

everything that genuinely needs Unity, in four groups. Own asmdef,
  unconditionally requires `UnityEngine` (unlike the core SDK), plus `Unity.Mathematics` and
  `Unity.Collections`, which is what the last three of these brought in:
  - *Conversion glue* — `Pixel`↔`Color32`, `PixelTexture`↔`Texture2D` (`ResourceExtensions`),
    `IFrameable` framerate resolution reading `Screen.currentResolution` (`ModelExtensions`).
  - **`Avatars/`** — `AvatarMovement`, the avatar's whole movement mechanism as one `readonly
    struct`, plus `TimePoint`, `AvatarStepSpeeds` and `AvatarStepResult`. **It touches no Unity
    RUNTIME** — no `Time`, `Transform`, `Camera`, `Screen` or `UnityEngine.Random`; the clock arrives
    as a `float` and every direction arrives resolved. It is here rather than in the core assembly
    only because it reasons in `float2`. Its own header carries the design; the consuming project's
    `docs/issues/MOVEMENT_HISTORY.md` is the record of why it moved.
  - **`Transforms/`** — `Transform2D` and `RectTransform2D`, the project's 2D local-transform structs.
    They arrived from the consumer's `BH.Shared.Transforms`, and their `ApplyTo(Transform/
    RectTransform/Camera/TransformHandle/TransformAccess)` overloads are exactly the "Unity-type
    conversion glue" this folder is for.
  - **`Math2D` and `TransformDefaults`** — the bridge those two groups needed across the assembly
    boundary. `BH.Shared.BHMath` could not be referenced (`Shared` references the SDK, never the
    reverse), so the four rotation primitives moved down here as **non-extension statics** — a second
    `RotateVector(this float2, …)` would be ambiguous in every file with both namespaces in scope —
    and `BHMath` delegates to them. `TransformDefaults` is the eight transform fields of
    `BH.Shared.defaults`, which likewise delegates; the rest of `defaults` reaches for `alignment`
    and `color` and stays in the consumer.
