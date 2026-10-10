using BH.SDK.Models.Primitives;

namespace BH.SDK.Generators.Modifiers
{
    // Shared by the modifiers (mod_content_remover, mod_span_fit) for which the window is an INSTRUCTION.
    // Asymmetric on purpose: Invert selects what shares no frame with the window, its opposite only what
    // lies wholly inside it, so an edge-overhanging object is not taken either way.

    /// <summary> Whether a lifetime is what a window-driven modifier is talking about. </summary>
    public static class WindowSelection
    {
        /// <summary> Whether a lifetime falls inside the run's window - or outside it, which is what the two modes of the content remover are. </summary>
        public static bool Selects(in FrameSpan span, in FrameSpan window, bool invert)
            => invert ? !window.Overlaps(span) : window.Contains(span);
    }
}
