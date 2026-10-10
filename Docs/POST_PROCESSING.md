Design facts of the post-processing keys in `Models/PostProcessing/` that the code cannot say by itself.
A contributor changing a key reads this first; the consumer side is the Unity project's `PostProcessing`.

## ColorCurvesKey: URP's own eight curves

Each curve is a nullable `CurveValue`; null means "never touched" and is neutral. What each one means is
URP's `LutBuilder` formula:

| Curves | Formula | Neutral |
|---|---|---|
| the three saturation curves (`HueVsSat`, `SatVsSat`, `LumVsSat`) | `satMult *= curve(x) * 2` | flat 0.5 (a multiply by one) |
| `HueVsHue` | `hue += curve(hue) - 0.5` (0..1 is a whole turn) | flat 0.5 |
| `Master`, `Red`, `Green`, `Blue` (YRGB) | `c = curve(c)`, an absolute mapping | the IDENTITY line, not a constant |

Every axis is 0..1 on both sides; anything outside is what the 128x1 curve texture clamps away.

- **Why not scalars beside the curves.** The key once held a `HueVsHue` and a `SatVsSat` scalar. A scalar
  IS a flat curve, and two answers for one knob is what this format refuses. They could not be extended
  either: three constants multiplying `satMult` are one constant, and a constant YRGB curve maps every
  input to one output, i.e. paints the frame one colour. Only a curve reshapes anything.
- **A global hue/saturation slider is a real authoring want and stays unserved here on purpose.** It is
  what URP's `ColorAdjustments` component is for (not wired up yet) and belongs there, not as a second
  answer on this key.
- **Both wrap modes are `ClampForever`, and that is load bearing.** A colour curve is sampled on exactly
  0..1, so the wrap mode means nothing to the sampler, but `CurveWrapMode.Default` maps to
  `UnityEngine.WrapMode.Default`, which behaves as `Once`, and `Once` answers a sample at EXACTLY the last
  key time by wrapping back to the first - an identity curve read 0 at x = 1 and painted the top of the
  ramp black. Core's `ColorCurvesGroup` forces the same two modes onto an authored curve.

## The grading colours carry three channels

`LiftGammaGainKey` and `ShadowsMidtonesHighlightsKey` colours are `IColor3`. URP takes each as a `Vector4`
whose w is a signed OFFSET, not opacity: `PrepareLiftGammaGain` adds it to every channel after removing the
luminance (gamma also does `w += 1` and inverts), and `PrepareShadowsMidtonesHighlights` weights it - times
one when negative, times FOUR when positive - and adds that. Its neutral is zero.

A four-component colour defaulting to white sent w = 1, so enabling a range with an untouched colour pushed
+4 into every channel of a whole tonal band; the picker called it Alpha and the neutral was reachable only
by guessing. The component is therefore removed, not documented: the provider sends URP the zero the effect
is neutral at. **No version moved**: an older payload carries one property more than the type has, Newtonsoft
drops it and the colour survives (`PostProcessingColorTests` checks it); what does not survive is whatever
the old alpha did to the picture, which is the point.

## VignetteKey

No alpha and no HDR part, both URP's own call: the component declares its colour as
`new ColorParameter(Color.black, hdr: false, showAlpha: false, ...)`, so a value above one changes nothing
and a fourth component is not read.
