# CLAUDE.md — Assets/Plugins/BulletHeroSDK/UnityIntegration

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## UnityIntegration/

`Cat.cs`, a tiny `Debug.Log`-style logging façade (`Meow`/`MeowWarn`/
  `MeowError`/...) gated by `#if BHSDK_UNITY` per call, falling back to `Console.WriteLine`. Own
  asmdef, distinct purpose from `UnityExtensions/` (logging, not data conversion) — don't conflate
  the two folders.
