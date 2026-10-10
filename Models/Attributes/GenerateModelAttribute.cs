using System;

namespace BH.SDK.Models.Attributes
{
    // Opt-in, not discovery: a hand-written model (FrameSpan, ModificationKey) must stay sayable, and
    // a compilation with no [GenerateModel] type is one the generator never touches.
    // Why: Docs/Issues/MODEL_CODEGEN_HISTORY.md

    /// <summary>
    /// Marks a model whose <see cref="Interfaces.IModel{T}"/> implementation is written by
    /// BH.SDK.Roslyn rather than by hand. The type must be <c>partial</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class GenerateModelAttribute : Attribute
    {
    }
}
