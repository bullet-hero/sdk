using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    /// <summary>
    /// How an object is picked, how several are picked at once, and what a picked object shows about
    /// itself.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorSelectionSettings : IModel<EditorSelectionSettings>, IMoveable<EditorSelectionSettings>
    {
        // Whether multi-selection is a MODIFIER or a MODE is a real preference, not a constant: on a
        // desktop the modifier is the familiar answer and an ordinary click should still replace the
        // selection, while on touch there is no key to hold, so the mode has to apply on its own.
        // Turning this off makes every click additive for as long as the mode is on.
        //
        // A mode entered by LONG PRESS ignores this either way, and has to: a finger has no modifier
        // to hold, so gating it there would make the touch entry point reach a mode that never applies.

        /// <summary> Whether multi-selection applies only while the modifier is held down. </summary>
        [JsonProperty(Names.RequiresHold)]
        public bool MultiRequiresHold { get; set; }

        // The two numbers below are what makes that long press reachable at all, and they are here
        // rather than in the project's asset because the gesture is an accessibility one: half a
        // second and eight points are a steady hand's numbers, and the author whose hand is not
        // steady is exactly the one who cannot change them from a ScriptableObject.

        /// <summary> How long a hierarchy row or timeline clip must be held to enter multi-select
        /// mode, in seconds. </summary>
        [RuleInRange(0.05f, 5f)]
        [JsonProperty(Names.LongPressDelay)]
        public float LongPressDelay { get; set; }

        /// <summary> Panel-space travel that turns that hold into a drag and cancels it. </summary>
        [RuleInRange(1f, 200f)]
        [JsonProperty(Names.LongPressThreshold)]
        public float LongPressMoveThreshold { get; set; }

        // Off by default, unlike almost everything else here, and the asymmetry is the point: the fill
        // covers the object an author has just selected and is about to work on, so it is in the way
        // far more often than it answers a question. The global collider view (a session toggle, not a
        // preference) is what an author reaches for when the question is actually about hitboxes; this
        // one exists for the case where they want the answer without leaving that view on.

        /// <summary> Whether selecting an object draws its collider. </summary>
        [JsonProperty(Names.PreviewCollider)]
        public bool PreviewColliderOnSelect { get; set; }

        // Only the two ALPHAS are authored, exactly as the grid's colour is: a hitbox reads as one
        // thing regardless of taste, so its hue stays a constant in the consumer rather than a field
        // somebody can tune into invisibility.

        /// <summary> Fill opacity for the collider of a SELECTED object - the louder of the two,
        /// since it is answering a question the author just asked. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.ColliderOpacity)]
        public float ColliderOpacitySelection { get; set; }

        /// <summary> Fill opacity in the global collider view, where hundreds of fills can overlap
        /// and each one has to stay readable through the others. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.ColliderOpacityView)]
        public float ColliderOpacityView { get; set; }

        // Off by default, which is the picker's own answer rather than a taste: a shape is clicked by
        // what it DRAWS (or by its collider), and the empty padding around a slice or a ring belongs
        // to whatever sits behind it. Turning this on gives every object its whole rect back, for an
        // author who would rather have a generous target than an exact one. Either way an object
        // carrying no geometry at all is picked by its rect - it has nothing else to be clicked by.

        /// <summary> Whether a click picks an object by its whole rect instead of its own shape. </summary>
        [JsonProperty(Names.PickInvisibleAABB)]
        public bool PickInvisibleAABB { get; set; }

        public EditorSelectionSettings()
        {
            ResetOwn();
        }
        public EditorSelectionSettings(bool multiRequiresHold, float longPressDelay,
            float longPressMoveThreshold, bool previewColliderOnSelect, float colliderOpacitySelection,
            float colliderOpacityView, bool pickInvisibleAABB)
        {
            MultiRequiresHold = multiRequiresHold;
            LongPressDelay = longPressDelay;
            LongPressMoveThreshold = longPressMoveThreshold;
            PreviewColliderOnSelect = previewColliderOnSelect;
            ColliderOpacitySelection = colliderOpacitySelection;
            ColliderOpacityView = colliderOpacityView;
            PickInvisibleAABB = pickInvisibleAABB;
        }
        private void ResetOwn()
        {
            MultiRequiresHold = true;
            LongPressDelay = 0.5f;
            LongPressMoveThreshold = 8f;
            PreviewColliderOnSelect = false;
            ColliderOpacitySelection = 0.5f;
            ColliderOpacityView = 0.25f;
            PickInvisibleAABB = false;
        }
    }
}
