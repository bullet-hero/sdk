using System.Collections.Generic;

namespace BH.SDK.Interop
{
    /// <summary>
    /// One thing a conversion could not carry across intact, aggregated over every place it
    /// happened. Deliberately not a <see cref="Validations.RuleIssue"/>: nothing here is wrong with
    /// the data, it is the two formats disagreeing.
    /// </summary>
    public class InteropIssue
    {
        // An issue used to keep ONE path and a count, and that is what made a report unusable at
        // the size a real level produces: a code firing on eight thousand objects surfaced a single
        // example and a number, so an author could not tell "this happened once, harmlessly" from
        // "this is most of my level", and could reach exactly one of the objects it named.
        //
        // WHY PATHS RATHER THAN OBJECT IDS. An id would have to be threaded through every call site
        // in the converter, and it would be the WRONG address twice over: the ids are minted per
        // scope, so the same number means different objects inside a prefab template and at level
        // scope, and an id is not what an author is looking at. A path is the source document's own
        // address, it is already passed to every report call, and it names the thing the author can
        // go and open.
        //
        // The list is BOUNDED and says when it was cut. An unbounded one is the wall of text this
        // aggregation exists to avoid, and it would grow with the level rather than with the
        // finding.

        /// <summary> How many distinct places one finding names before it stops collecting them. </summary>
        public const int MaxTrackedPaths = 32;

        private readonly List<string> _paths = new();
        private readonly HashSet<string> _seen = new();

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

        /// <summary> The distinct places this finding was hit, in the order they were reached, up
        /// to <see cref="MaxTrackedPaths"/> of them - each in the SOURCE document's own terms
        /// ("objects[17].p_o"). </summary>
        public IReadOnlyList<string> Paths => _paths;

        /// <summary> Whether the finding reached more distinct places than it kept. </summary>
        public bool HasMorePaths { get; private set; }

        /// <summary> Where the first occurrence was. The one an author can go and look at first,
        /// and what a one-line summary shows. </summary>
        public string FirstPath => _paths.Count > 0 ? _paths[0] : string.Empty;

        /// <summary> One finding, before any identical ones are folded into it. </summary>
        public InteropIssue(InteropSeverity severity, string code, string message, string firstPath)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Count = 1;
            Track(firstPath);
        }

        /// <summary> Counts one more occurrence of the same finding, so a level with 4000 of them
        /// prints one line - remembering where it happened while there is room. </summary>
        internal void Increment(string path)
        {
            Count++;
            Track(path);
        }

        /// <summary> Undoes the count the constructor starts at, for a merge that is about to
        /// replay every occurrence of an absorbed issue rather than add one more. </summary>
        internal void Decrement() => Count--;

        /// <summary> Carries an absorbed issue's own "there were more than these" over a merge. </summary>
        internal void MarkMorePaths() => HasMorePaths = true;

        private void Track(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!_seen.Add(path)) return;

            if (_paths.Count >= MaxTrackedPaths)
            {
                HasMorePaths = true;
                return;
            }

            _paths.Add(path);
        }

        /// <summary> One line for the report, carrying the count when the finding repeated. </summary>
        public override string ToString()
            => Count > 1
                ? $"[{Severity}] {Message} (x{Count}, first at {FirstPath})"
                : $"[{Severity}] {Message} ({FirstPath})";
    }
}