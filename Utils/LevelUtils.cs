using System;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.SettingGroups;

namespace BH.SDK.Utils
{
    /// <summary> Shorthands for building a level by hand - minting ids, parenting, setting lifetimes. Used by
    /// generators and tests, where spelling the same four assignments out every time obscures the intent. </summary>
    public static class LevelUtils
    {
        /// <summary> Mints the next id in that scope and puts it on the object. </summary>
        public static void SetObjectId(this RectObject obj, LevelSettings settings)
        {
            obj.ObjectId = settings.GetNextObjectId();
        }

        /// <summary> Parents one object to another by id. </summary>
        public static void SetParent(this RectObject obj, RectObject parentObj)
        {
            obj.ParentObjectId = parentObj.ObjectId;
        }

        /// <summary> Sets the lifetime from a half-open pair, where endFrame is the first frame the
        /// object is already gone. </summary>
        public static void SetBounds(this RectObject obj, int startFrame, int endFrame)
        {
            obj.Span = FrameSpan.FromBounds(startFrame, endFrame, obj.Span.Anchors);
        }
    }
}