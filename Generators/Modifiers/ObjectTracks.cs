using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;

namespace BH.SDK.Generators.Modifiers
{
    /// <summary> Which of an object's keyframe tracks a modifier should touch. </summary>
    [Flags]
    public enum ObjectTrackMask
    {
        None = 0,

        Positions = 1 << 0,
        Rotations = 1 << 1,
        Scales = 1 << 2,
        Sizes = 1 << 3,
        AnchorsMin = 1 << 4,
        AnchorsMax = 1 << 5,
        Pivots = 1 << 6,

        /// <summary> ShapeObject.Colors / TextObject.Colors - the type-specific tracks. </summary>
        Colors = 1 << 7,

        /// <summary> ShapeObject.UVs. </summary>
        UVs = 1 << 8,

        /// <summary> TextObject.FontSizes. </summary>
        FontSizes = 1 << 9,

        /// <summary> TextObject.Fillments. </summary>
        Fillments = 1 << 10,

        /// <summary> TextObject.Appearings. </summary>
        Appearings = 1 << 11,

        Transform = Positions | Rotations | Scales | Sizes,
        Layout = AnchorsMin | AnchorsMax | Pivots,
        All = Transform | Layout | Colors | UVs | FontSizes | Fillments | Appearings,
    }

    // Every track on a RectObject is a List<T> of something implementing IFrame, but they are ten
    // separate properties of four unrelated element types - so anything wanting to work "on the
    // object's tracks" generically either writes the same ten-branch switch (every modifier, once
    // each) or gets it from here, once.
    //
    // IList<IFrame> would have been the tidy signature and is not available: List<PosKey> is not
    // IList<IFrame>, and copying each track into a temporary list would break the whole point,
    // which is mutating the real keyframes in place.

    /// <summary>
    /// Enumerates an object's keyframe tracks by mask, so a modifier can read or rewrite frames
    /// without knowing which concrete keyframe types the object happens to carry.
    /// </summary>
    public static class ObjectTracks
    {
        /// <summary> Every selected track of this object, as (frame-reader, frame-writer) pairs over
        /// the live keyframes. </summary>
        public static IEnumerable<Track> Of(RectObject obj, ObjectTrackMask mask)
        {
            if (obj == null) yield break;

            if (Has(mask, ObjectTrackMask.Positions)) yield return Wrap(obj.Positions);
            if (Has(mask, ObjectTrackMask.Rotations)) yield return Wrap(obj.Rotations);
            if (Has(mask, ObjectTrackMask.Scales)) yield return Wrap(obj.Scales);
            if (Has(mask, ObjectTrackMask.Sizes)) yield return Wrap(obj.Sizes);
            if (Has(mask, ObjectTrackMask.AnchorsMin)) yield return Wrap(obj.AnchorsMin);
            if (Has(mask, ObjectTrackMask.AnchorsMax)) yield return Wrap(obj.AnchorsMax);
            if (Has(mask, ObjectTrackMask.Pivots)) yield return Wrap(obj.Pivots);

            switch (obj)
            {
                case ShapeObject texture:
                    if (Has(mask, ObjectTrackMask.Colors)) yield return Wrap(texture.Colors);
                    if (Has(mask, ObjectTrackMask.UVs)) yield return Wrap(texture.UVs);
                    break;
                case TextObject text:
                    if (Has(mask, ObjectTrackMask.Colors)) yield return Wrap(text.Colors);
                    if (Has(mask, ObjectTrackMask.FontSizes)) yield return Wrap(text.FontSizes);
                    if (Has(mask, ObjectTrackMask.Fillments)) yield return Wrap(text.Fillments);
                    if (Has(mask, ObjectTrackMask.Appearings)) yield return Wrap(text.Appearings);
                    break;
            }
        }

        /// <summary> One track, reduced to what a frame-level modifier needs: how many keys there
        /// are, what frame each sits on, how to move it, and how to drop it. </summary>
        public readonly struct Track
        {
            /// <summary> How many keyframes the track holds. </summary>
            public readonly int Count;
            private readonly Func<int, int> _frameAt;
            private readonly Action<int, int> _setFrameAt;
            private readonly Action<int> _removeAt;

            /// <summary> One track seen through two delegates, so a modifier retimes any of them without knowing its element type. </summary>
            public Track(int count, Func<int, int> frameAt, Action<int, int> setFrameAt, Action<int> removeAt)
            {
                Count = count;
                _frameAt = frameAt;
                _setFrameAt = setFrameAt;
                _removeAt = removeAt;
            }

            /// <summary> The frame one keyframe sits on. </summary>
            public int FrameAt(int index) => _frameAt(index);
            /// <summary> Moves it. </summary>
            public void SetFrameAt(int index, int frame) => _setFrameAt(index, frame);

            /// <summary> Drops one key. Count is a snapshot taken when the track was handed over, so
            /// a caller that removes must walk indices DOWNWARD and stop using Count afterwards. </summary>
            public void RemoveAt(int index) => _removeAt(index);
        }

        private static Track Wrap<TKey>(List<TKey> track) where TKey : IFrame
            => new(track?.Count ?? 0,
                index => track[index].Frame,
                (index, frame) => track[index].Frame = frame,
                index => track.RemoveAt(index));

        private static bool Has(ObjectTrackMask mask, ObjectTrackMask flag) => (mask & flag) != 0;
    }
}
