using System;

namespace BH.SDK.Interop
{
    /// <summary>
    /// One thing a conversion could not carry across intact, aggregated over every place it
    /// happened. Deliberately not a <see cref="Validations.RuleIssue"/>: nothing here is wrong with
    /// the data, it is the two formats disagreeing.
    /// </summary>
    public class InteropIssue
    {
        /// <summary> How much was given up. </summary>
        public InteropSeverity Severity { get; }

        /// <summary> Stable slug naming what happened, e.g. "parent_time_offset". Meant for a
        /// localization lookup and for a test to assert on - never for display on its own. </summary>
        public string Code { get; }

        /// <summary> One sentence an author can act on, in English. </summary>
        public string Message { get; }

        /// <summary> How many times this happened. One line saying 4096 beats 4096 identical
        /// lines - which is the whole reason issues aggregate rather than accumulate. </summary>
        public int Count { get; private set; }

        /// <summary> Where the first occurrence was, in the SOURCE document's own terms
        /// ("objects[17].p_o"). Later occurrences do not overwrite it: the first one is the one an
        /// author can go and look at. </summary>
        public string FirstPath { get; }

        public InteropIssue(InteropSeverity severity, string code, string message, string firstPath)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            FirstPath = firstPath ?? string.Empty;
            Count = 1;
        }

        internal void Increment() => Count++;

        public override string ToString()
            => Count > 1
                ? $"[{Severity}] {Message} (x{Count}, first at {FirstPath})"
                : $"[{Severity}] {Message} ({FirstPath})";
    }
}
