using System.Collections.Generic;
using System.Text;

namespace BH.SDK.Interop
{
    // Issues aggregate by Code rather than accumulating, and that is the difference between a
    // usable report and a wall of text: a level whose every object uses a feature this format has
    // no equivalent for produces one line saying so, with a count, not one line per object. The
    // first path is kept because an author needs somewhere to go and look; the rest are noise once
    // the first has been seen.

    /// <summary>
    /// Everything a conversion had to give up, in one object. Produced by both directions and
    /// meant to be shown to the author - a silent lossy import is the failure mode this exists to
    /// prevent.
    /// </summary>
    public class InteropReport
    {
        private readonly Dictionary<string, InteropIssue> _byCode = new();
        private readonly List<InteropIssue> _issues = new();

        /// <summary> Every distinct issue, in the order it was first hit. </summary>
        public IReadOnlyList<InteropIssue> Issues => _issues;

        /// <summary> The worst thing that happened, or <see cref="InteropSeverity.Info"/> when
        /// nothing did. </summary>
        public InteropSeverity Worst { get; private set; } = InteropSeverity.Info;

        /// <summary> True when nothing was lost or approximated. </summary>
        public bool IsClean => Worst <= InteropSeverity.Info;

        /// <summary> True when something could not be read at all. </summary>
        public bool HasFailure => Worst >= InteropSeverity.Failed;

        /// <summary> Records one finding, folding it into an identical earlier one rather than repeating it. </summary>
        public void Add(InteropSeverity severity, string code, string message, string path = null)
        {
            if (_byCode.TryGetValue(code, out var existing))
            {
                existing.Increment(path);
                return;
            }

            var issue = new InteropIssue(severity, code, message, path);
            _byCode.Add(issue.Code, issue);
            _issues.Add(issue);
            if (severity > Worst) Worst = severity;
        }

        /// <summary> Something the author should know that cost nothing. </summary>
        public void Info(string code, string message, string path = null)
            => Add(InteropSeverity.Info, code, message, path);

        /// <summary> Something that crossed, but not exactly. </summary>
        public void Approximated(string code, string message, string path = null)
            => Add(InteropSeverity.Approximated, code, message, path);

        /// <summary> Something this build cannot convert yet, but could. </summary>
        public void Deferred(string code, string message, string path = null)
            => Add(InteropSeverity.Deferred, code, message, path);

        /// <summary> Something that did not cross at all. </summary>
        public void Dropped(string code, string message, string path = null)
            => Add(InteropSeverity.Dropped, code, message, path);

        /// <summary> Something that stopped the conversion. </summary>
        public void Failed(string code, string message, string path = null)
            => Add(InteropSeverity.Failed, code, message, path);

        /// <summary> How many distinct issues reached at least <paramref name="severity"/>. </summary>
        public int CountAtLeast(InteropSeverity severity)
        {
            var count = 0;
            foreach (var issue in _issues)
                if (issue.Severity >= severity)
                    count++;
            return count;
        }

        /// <summary> Merges another report into this one, keeping the aggregation. </summary>
        public void Absorb(InteropReport other)
        {
            if (other == null) return;

            // The counts and the PATHS both have to survive a merge, and they are different
            // lengths: an issue hit 4000 times keeps at most MaxTrackedPaths of them, so replaying
            // one path per count would either lose paths or invent them. Each is replayed from the
            // half that actually holds it - the paths from the list, the remaining count as bare
            // increments carrying no path at all.
            foreach (var issue in other._issues)
            {
                if (!_byCode.TryGetValue(issue.Code, out var existing))
                {
                    existing = new InteropIssue(issue.Severity, issue.Code, issue.Message, null);
                    existing.Decrement();
                    _byCode.Add(existing.Code, existing);
                    _issues.Add(existing);
                    if (existing.Severity > Worst) Worst = existing.Severity;
                }

                var replayed = 0;
                foreach (var path in issue.Paths)
                {
                    if (replayed >= issue.Count) break;
                    existing.Increment(path);
                    replayed++;
                }

                for (var i = replayed; i < issue.Count; i++) existing.Increment(null);
                if (issue.HasMorePaths) existing.MarkMorePaths();
            }
        }

        /// <summary> Every finding, worst first - what a host shows the author after a conversion. </summary>
        public override string ToString()
        {
            if (_issues.Count == 0) return "Conversion clean - nothing lost.";

            var builder = new StringBuilder();
            builder.Append("Conversion finished with ").Append(_issues.Count).AppendLine(" note(s):");
            foreach (var issue in _issues) builder.Append("  ").AppendLine(issue.ToString());
            return builder.ToString();
        }
    }
}