using System;

namespace BH.SDK.Models.Attributes
{
    // OPT-IN, NOT DISCOVERY, and the choice is deliberate. The generator could just as well take
    // every type implementing IModel<T>, and that would even be correct for the 200-odd types that
    // want it - but then "this type is hand-written on purpose" would have no way to be said.
    // FrameSpan and ModificationKey are exactly that case: two structs whose state is packed into
    // private ints, carrying no [JsonProperty] at all and assigning `this = src` in Update. A
    // member-driven generator would produce something semantically different for them, silently.
    //
    // The attribute is also what bounds the generator's reach. It ships inside BH.SDK.Roslyn.dll,
    // which Unity scopes to BH.SDK plus every assembly referencing it - roughly the whole project.
    // A compilation holding no [GenerateModel] type is one the generator declines to touch, and
    // that decision is free: the incremental predicate never matches.

    /// <summary>
    /// Marks a model whose <see cref="Interfaces.IModel{T}"/> implementation is written by
    /// BH.SDK.Roslyn rather than by hand. The type must be <c>partial</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class GenerateModelAttribute : Attribute
    {
    }
}
