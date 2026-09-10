# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Rules

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Rules/

`public const` numeric/enum clamp tables (`FrameRules`, `ValueRules`, `LevelRules`,
  `AudioRules`, `EffectRules`, `PostProcessingRules`, `ResourceRules`, `TextRules`) plus
  **`AvatarRules`, which is the odd one out**: every other file here bounds what an author or a player
  may set, while nothing in that one is settable at all. It is the avatar's balance — speeds, dash and
  knockback windows, scale and hitbox — frozen as constants because it used to be serialized fields
  with a ScriptableObject overriding them, and the two copies had drifted (the asset played
  `dashTime` 0.15 against a field initializer of 0.2, `knockoutSpeed` 50 against 2). `AvatarRulesTests`
  spells every value out a second time, so changing one fails there rather than in a level. Plus
  `Rules/Attributes/` (declarative `[RuleXxx]` property attributes consumed by `Validations/`).
