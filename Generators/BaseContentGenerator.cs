namespace BH.SDK.Generators
{
    /// <summary>
    /// Base for a generator that ADDS content to the active scope. Kind is fixed here so a content
    /// generator can't accidentally advertise itself as a modifier and end up in the wrong list.
    /// </summary>
    public abstract class BaseContentGenerator<TParams> : BaseScopeGenerator<TParams> where TParams : class, new()
    {
        /// <summary> Fixed here, so a content generator cannot end up in the modifier list. </summary>
        public sealed override GeneratorKind Kind => GeneratorKind.Content;
    }
}
