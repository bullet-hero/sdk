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

        public void Add(InteropSeverity severity, string code, string message, string path = null)
        {
            if (_byCode.TryGetValue(code, out var existing))
            {
                existing.Increment();
                return;
            }

            var issue = new InteropIssue(severity, code, message, path);
            _byCode.Add(issue.Code, issue);
            _issues.Add(issue);
            if (severity > Worst) Worst = severity;
        }

        public void Info(string code, string message, string path = null)
            => Add(InteropSeverity.Info, code, message, path);

        public void Approximated(string code, string message, string path = null)
            => Add(InteropSeverity.Approximated, code, message, path);

        public void Deferred(string code, string message, string path = null)
            => Add(InteropSeverity.Deferred, code, message, path);

        public void Dropped(string code, string message, string path = null)
            => Add(InteropSeverity.Dropped, code, message, path);

        public void Failed(string code, string message, string path = null)
            => Add(InteropSeverity.Failed, code, message, path);

        /// <summary> How many distinct issues reached at least <paramref name="severity"/>. </summary>
        public int CountAtLeast(InteropSeverity severity)
        {
            var count = 0;
            foreach (var issue in _issues)
                if (issue.Severity >= severity) count++;
            return count;
        }

        /// <summary> Merges another report into this one, keeping the aggregation. </summary>
        public void Absorb(InteropReport other)
        {
            if (other == null) return;
            foreach (var issue in other._issues)
            {
                if (_byCode.TryGetValue(issue.Code, out var existing))
                {
                    for (var i = 0; i < issue.Count; i++) existing.Increment();
                    continue;
                }

                var copy = new InteropIssue(issue.Severity, issue.Code, issue.Message, issue.FirstPath);
                for (var i = 1; i < issue.Count; i++) copy.Increment();
                _byCode.Add(copy.Code, copy);
                _issues.Add(copy);
                if (copy.Severity > Worst) Worst = copy.Severity;
            }
        }

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
