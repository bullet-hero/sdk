using System;

namespace BH.SDK.Models.Attributes
{
    // The one thing a hook cannot do is un-emit a line; without this the generator REFUSES the type
    // instead of skipping the member, because a member missing from Copy or Equals is the bug it ends.

    /// <summary>
    /// Excludes one property from every generated body. The declaring type is then responsible for
    /// it through the partial hooks - the generator neither reads nor writes it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelIgnoreAttribute : Attribute
    {
    }
}
