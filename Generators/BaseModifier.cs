namespace BH.SDK.Generators
{
    /// <summary> Base for a generator that EDITS existing content. Requirements default to Selection;
    /// a modifier that works on the whole scope regardless of selection overrides it explicitly.
    /// </summary>
    public abstract class BaseModifier<TParams> : BaseScopeGenerator<TParams> where TParams : class, new()
    {
        /// <summary> Fixed here, so a modifier cannot end up in the content list. </summary>
        public sealed override GeneratorKind Kind => GeneratorKind.Modifier;

        /// <summary> Needs a selection: this run edits what the author pointed at. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.Selection;
    }
}
