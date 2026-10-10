using BH.SDK.Rules;

namespace BH.SDK.Validations
{
    // An interface, not a virtual on a base: dispatch must be on the RUNTIME type. Nothing may implement
    // it by hand - that bypasses the [RuleContainer] check and the reflective fallback (analyzer
    // BHS1107); the one exception is the fixture RuleWalkSeamTests.

    /// <summary> A model that knows how to walk itself, in place of being walked by reflection. </summary>
    public interface IValidatable
    {
        /// <summary> Report this object's own findings into <paramref name="walk"/>, then descend.
        /// The context is the scope this object was reached in, already rebased by every
        /// <c>Prefab</c> above it. </summary>
        void Validate(RuleWalk walk, RuleContext context);
    }
}
