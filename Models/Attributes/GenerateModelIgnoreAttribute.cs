using System;

namespace BH.SDK.Models.Attributes
{
    // THE ONE THING A HOOK CANNOT DO IS UN-EMIT A LINE. Everything else about a generated member is
    // adjustable after the fact - OnCopied, OnReset and their siblings run last and may overwrite
    // whatever the generator wrote. A member the generator cannot express at all is the exception:
    // Modification.Value is an `object` whose setter normalizes integrals to long and floats to
    // double, and no mechanical read of its type says any of that.
    //
    // So the escape valve is explicit and narrow. Without it the generator REFUSES the type with a
    // diagnostic rather than skipping the member, because a member quietly missing from Copy or
    // Equals is precisely the class of bug this generator exists to end.

    /// <summary>
    /// Excludes one property from every generated body. The declaring type is then responsible for
    /// it through the partial hooks - the generator neither reads nor writes it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelIgnoreAttribute : Attribute
    {
    }
}
