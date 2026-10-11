using System;
using System.Collections.Generic;

namespace BH.SDK.Agent
{
    /// <summary> What the musical time forms resolve against: the level's <see cref="MusicGrid"/> and its
    /// markers by name. The caller builds it from the level, so this side never reads a level model.
    /// </summary>
    public sealed class TimeAddressContext
    {
        private readonly Func<string, int?> _markers;

        /// <summary> The beat grid; null or empty when the level has none. </summary>
        public MusicGrid Grid { get; }

        /// <summary> A grid and a marker lookup returning a global frame, or null for an unknown name.
        /// </summary>
        public TimeAddressContext(MusicGrid grid, Func<string, int?> markers = null)
        {
            Grid = grid;
            _markers = markers;
        }

        /// <summary> A grid and markers as a name to global frame map (its comparer decides the matching).
        /// </summary>
        public TimeAddressContext(MusicGrid grid, IReadOnlyDictionary<string, int> markers)
            : this(grid, markers == null ? null : name => markers.TryGetValue(name, out var f) ? f : (int?)null)
        {
        }

        /// <summary> Whether a grid with at least one beat is present. </summary>
        public bool HasGrid => Grid != null && !Grid.IsEmpty;

        /// <summary> Whether markers can be looked up at all. </summary>
        public bool HasMarkers => _markers != null;

        /// <summary> The global frame of the marker named <paramref name="name"/>. </summary>
        public bool TryGetMarker(string name, out int frame)
        {
            frame = 0;
            var found = _markers?.Invoke(name);
            if (!found.HasValue) return false;
            frame = found.Value;
            return true;
        }
    }
}