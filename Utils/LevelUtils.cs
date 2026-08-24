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

        // The switch IS the point. Update/Pull are overloads rather than overrides (CLAUDE.md's
        // "IModel<T> pattern" says what an override would cost), so pulling through a RectObject writes the
        // RectObject half and silently drops whatever the subclass adds - a ShapeObject would keep
        // its old shape and colours while its transform moved. Dispatching on the concrete type costs
        // one method here instead of three per subclass, and it is the same hand-kept switch
        // ObjectConverter.GetType already is: a new RectObject subtype extends BOTH, and the throw
        // below is what says so out loud instead of losing that subtype's fields on every pull.

        /// <summary> Merges source into target in place while their concrete types agree, and returns
        /// what the scope must now hold - the same instance, or a fresh copy of source. </summary>
        public static RectObject PullObject(RectObject target, RectObject source)
        {
            if (source is null) return null;
            if (target is null || target.GetType() != source.GetType()) return source.Copy();

            switch (target)
            {
                case ShapeObject shape: shape.Pull((ShapeObject)source); break;
                case EffectObject effect: effect.Pull((EffectObject)source); break;
                case TextObject text: text.Pull((TextObject)source); break;
                case PrefabObject prefab: prefab.Pull((PrefabObject)source); break;
                default:
                    if (target.GetType() != typeof(RectObject))
                        throw new ArgumentOutOfRangeException(nameof(target), target.GetType(),
                            "RectObject subtype is missing from LevelUtils.PullObject");
                    target.Pull(source);
                    break;
            }
            return target;
        }
    }
}