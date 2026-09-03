using BH.SDK.Rules;

namespace BH.SDK.Validations
{
    // THE SEAM THE GENERATED WALK WILL ARRIVE THROUGH, and it is deliberately here before anything
    // implements it: the reflective walk is ported onto RuleWalk first, proven against the whole
    // existing suite, and only then does BH.SDK.Roslyn start writing bodies for this method.
    //
    // It exists as an interface rather than as a virtual on a base because the dispatch has to be on
    // the RUNTIME type. A member declared IVector2 holds any of four variants, one declared
    // RectObject holds any of five, and each one's own walk is the correct one - which is exactly
    // what RuleWalk.Node's type check buys, in place of the Dictionary<Type, bool> container lookup
    // the reflective path pays per node.
    //
    // Nothing may implement it by hand. A hand-written body bypasses both the [RuleContainer] check
    // and the reflective fallback and is answerable to nothing; the generator's own analyzer refuses
    // it (BHS1107). The one exception is a test fixture proving the seam works, which is what
    // RuleWalkSeamTests does.

    /// <summary> A model that knows how to walk itself, in place of being walked by reflection. </summary>
    public interface IValidatable
    {
        /// <summary> Report this object's own findings into <paramref name="walk"/>, then descend.
        /// The context is the scope this object was reached in, already rebased by every
        /// <c>IFrameScope</c> above it. </summary>
        void Validate(RuleWalk walk, RuleContext context);
    }
}
