namespace BH.SDK.Models.Enums.Values
{
    // Authored intent only - nothing evaluates it. Every producer writes Free (the constructors
    // here, the AfterBeat import, the Unity round trip), and the one reader casts it into Unity's
    // obsolete Keyframe.tangentMode. It is kept for a curve editor that lets an author touch
    // tangents at all; until that exists, no member but Free is reachable.
    //
    // A "Broken" member deliberately does NOT belong here: in Unity brokenness is an independent
    // BIT beside a per-side mode, so Auto-and-broken is a real state that a fifth enum value cannot
    // express. Whoever implements tangent modes adds a separate flag, not a member.

    /// <summary> How a CurveKeyframeValue's tangents were derived when it was authored. </summary>
    public enum CurveTangentMode : byte
    {
        Free = 0,
        Auto = 1,
        Linear = 2,
        Constant = 3,
        ClampedAuto = 4,
    }
}