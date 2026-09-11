using System.Collections.Generic;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Utils
{
    /// <summary> What went wrong while expanding one placement. </summary>
    public enum PrefabExpandProblem
    {
        /// <summary> The placement's PrefabId names no template in Level.Resources.Prefabs. </summary>
        TemplateMissing = 0,

        /// <summary> A template object has no entry in the placement's own id table. </summary>
        RemapMissing = 1,

        /// <summary> Expanding would nest deeper than LevelRules.MaxObjectDepth. </summary>
        TooDeep = 2,
    }

    /// <summary> One finding, addressed well enough to locate by hand in the file. </summary>
    public readonly struct PrefabExpandIssue
    {
        /// <summary> Which problem this is. </summary>
        public readonly PrefabExpandProblem Problem;

        /// <summary> Which scope it was found in: "Level", or "Prefab[id]" for a template. </summary>
        public readonly string Scope;

        /// <summary> The placement it was found on. </summary>
        public readonly ObjectId Placement;

        /// <summary> A sentence naming what was missing or refused. </summary>
        public readonly string Message;

        public PrefabExpandIssue(PrefabExpandProblem problem, string scope, ObjectId placement, string message)
        {
            Problem = problem;
            Scope = scope;
            Placement = placement;
            Message = message;
        }

        public override string ToString() => $"[{Problem}] {Scope}/{Placement.value}: {Message}";
    }

    // THE ENTRY LIST IS CAPPED AND THE COUNTS ARE NOT, for the reason LevelLoaderService caps its
    // own substitution log: one broken template is one finding per PLACEMENT of it, and a template
    // placed 340 times would otherwise hand the Editor 340 stack captures for a single authoring
    // mistake. The counts still say how big the problem really is.

    /// <summary> What a load-time prefab expansion did, and everything it could not do. </summary>
    public sealed class PrefabExpandReport
    {
        /// <summary> How many entries are kept; everything past this is counted only. </summary>
        public const int MaxEntries = 32;

        private readonly List<PrefabExpandIssue> _entries = new();

        /// <summary> Genuine placements the sweep visited. </summary>
        public int Placements { get; private set; }

        /// <summary> Objects written into a host scope. </summary>
        public int Expanded { get; private set; }

        /// <summary> Findings in total, including the ones past the cap. </summary>
        public int Count { get; private set; }

        /// <summary> The kept findings, oldest first. </summary>
        public IReadOnlyList<PrefabExpandIssue> Entries => _entries;

        /// <summary> Whether the expansion found nothing to report. </summary>
        public bool IsEmpty => Count == 0;

        /// <summary> Counts one visited placement. </summary>
        public void CountPlacement() => Placements++;

        /// <summary> Counts objects written for one placement. </summary>
        public void CountExpanded(int objects) => Expanded += objects;

        /// <summary> Records a finding, keeping at most MaxEntries of them. </summary>
        public void Report(PrefabExpandProblem problem, string scope, ObjectId placement, string message)
        {
            Count++;
            if (_entries.Count >= MaxEntries) return;
            _entries.Add(new PrefabExpandIssue(problem, scope, placement, message));
        }

        public override string ToString()
            => $"{Expanded} object(s) from {Placements} placement(s), {Count} issue(s)";
    }
}
