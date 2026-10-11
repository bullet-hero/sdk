using System;
using System.Collections.Generic;
using System.Text;

namespace BH.SDK.Agent
{
    /// <summary> The text of a level's <c>ai/CONTEXT.md</c>: the sections it starts with, and how a section
    /// grows when the AI appends to it. Plain markdown, no format of its own. </summary>
    public static class AiContextDocument
    {
        /// <summary> The top-level sections of the skeleton, in order, each with what it is for. </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Sections = new[]
        {
            new KeyValuePair<string, string>("Brief", "what the level is: the track, the mood, the length, who it is for"),
            new KeyValuePair<string, string>("Music", "tempo, beat grid, named sections and markers, what the analysis was unsure of"),
            new KeyValuePair<string, string>("Sections", "the plan per part of the track: frames or bars, what happens, how hard"),
            new KeyValuePair<string, string>("Decisions", "what was decided and why, so a later session does not undo it"),
            new KeyValuePair<string, string>("Author remarks", "what the author asked for or rejected, in their words"),
            new KeyValuePair<string, string>("Open questions", "what to ask the author next"),
        };

        /// <summary> Every section heading, empty, as a new <c>CONTEXT.md</c> starts. </summary>
        public static string Skeleton { get; } = BuildSkeleton();

        /// <summary> The heading's text without its leading <c>#</c> marks and surrounding spaces. </summary>
        public static string NormalizeHeading(string heading)
            => (heading ?? string.Empty).Trim().TrimStart('#').Trim();

        /// <summary> The document with <paramref name="text"/> added at the end of the first section titled
        /// <paramref name="heading"/> (any level, case-insensitive), or as a new top-level section at the end
        /// when there is none. Headings inside code fences are not sections. Ends with a newline. </summary>
        public static string AppendSection(string document, string heading, string text)
        {
            var title = NormalizeHeading(heading);
            var addition = SplitLines(TrimBlankEdges(text ?? string.Empty));
            var lines = SplitLines(document ?? string.Empty);

            var start = -1;
            var level = 0;
            var fenced = false;
            for (var i = 0; i < lines.Count; i++)
            {
                if (IsFence(lines[i])) fenced = !fenced;
                if (fenced || !TryHeading(lines[i], out var lineLevel, out var lineTitle)) continue;
                if (!string.Equals(lineTitle, title, StringComparison.OrdinalIgnoreCase)) continue;

                start = i;
                level = lineLevel;
                break;
            }

            var result = new List<string>();
            if (start < 0)
            {
                result.AddRange(lines);
                TrimTrailingBlanks(result);
                if (result.Count > 0) result.Add(string.Empty);
                result.Add("# " + title);
                result.Add(string.Empty);
                result.AddRange(addition);
                return Join(result);
            }

            var end = lines.Count;
            fenced = false;
            for (var i = start + 1; i < lines.Count; i++)
            {
                if (IsFence(lines[i])) fenced = !fenced;
                if (fenced || !TryHeading(lines[i], out var lineLevel, out _) || lineLevel > level) continue;

                end = i;
                break;
            }

            for (var i = 0; i <= start; i++) result.Add(lines[i]);
            for (var i = start + 1; i < end; i++) result.Add(lines[i]);
            TrimTrailingBlanks(result);
            result.Add(string.Empty);
            result.AddRange(addition);

            if (end < lines.Count)
            {
                result.Add(string.Empty);
                for (var i = end; i < lines.Count; i++) result.Add(lines[i]);
                TrimTrailingBlanks(result);
            }

            return Join(result);
        }

        private static string BuildSkeleton()
        {
            var lines = new List<string>();
            foreach (var section in Sections)
            {
                if (lines.Count > 0) lines.Add(string.Empty);
                lines.Add("# " + section.Key);
            }

            return Join(lines);
        }

        private static bool TryHeading(string line, out int level, out string title)
        {
            level = 0;
            while (level < line.Length && line[level] == '#') level++;

            title = null;
            if (level == 0 || level > 6 || level == line.Length || (line[level] != ' ' && line[level] != '\t'))
                return false;

            title = line.Substring(level).Trim().TrimEnd('#').Trim();
            return true;
        }

        private static bool IsFence(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal);
        }

        private static List<string> SplitLines(string text)
        {
            var lines = new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        private static string TrimBlankEdges(string text)
        {
            var lines = SplitLines(text);
            while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
            TrimTrailingBlanks(lines);
            return string.Join("\n", lines);
        }

        private static void TrimTrailingBlanks(List<string> lines)
        {
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
        }

        private static string Join(List<string> lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines) builder.Append(line).Append('\n');
            return builder.ToString();
        }
    }
}
