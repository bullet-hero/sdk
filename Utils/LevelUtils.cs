using System;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.SettingGroups;

namespace BH.SDK.Utils
{
    public static class LevelUtils
    {
        public static void SetObjectId(this RectObject obj, LevelSettings settings)
        {
            obj.ObjectId = settings.GetNextObjectId();
        }
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