using BH.SDK.Models.Primitives;

namespace BH.SDK.Generators.Modifiers
{
    // Shared by every modifier for which the run's window is an INSTRUCTION rather than a boundary -
    // "do this to what the window names" instead of "write new content in here". Two of them exist
    // (mod_content_remover, mod_span_fit) and the answer must read the same in both: a level cleaned
    // by one and fitted by the other should have talked about the same objects.
    //
    // The asymmetry is the whole point and is NOT an oversight: Invert selects what shares no frame
    // at all with the window, its opposite selects only what lies wholly inside it. Making both
    // "overlaps" would mean content hanging over the edge is taken whichever way the flag is set,
    // which is not a mode - it is a trap.

    /// <summary> Whether a lifetime is what a window-driven modifier is talking about. </summary>
    public static class WindowSelection
    {
        /// <summary> Whether a lifetime falls inside the run's window - or outside it, which is what the two modes of the content remover are. </summary>
        public static bool Selects(in FrameSpan span, in FrameSpan window, bool invert)
            => invert ? !window.Overlaps(span) : window.Contains(span);
    }
}
