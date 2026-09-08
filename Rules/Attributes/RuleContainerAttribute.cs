using System;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> Marks a class whose properties the rule walk descends into. INHERITED, which is why the
    /// generator has to look past declared attributes to find every one of them. </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RuleContainerAttribute : Attribute
    {
        
    }
}