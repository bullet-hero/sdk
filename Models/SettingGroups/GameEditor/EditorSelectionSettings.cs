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
        // Multi-selection as a modifier or a mode is a preference (desktop vs touch); a long-press mode
        // ignores this either way.

        /// <summary> Whether multi-selection applies only while the modifier is held down. </summary>
        [JsonProperty(Names.RequiresHold)]
        public bool MultiRequiresHold { get; set; }

        // The long-press numbers are an accessibility gesture, so they live here, not in an asset.

        /// <summary> How long a hierarchy row or timeline clip must be held to enter multi-select
        /// mode, in seconds. </summary>
        [RuleInRange(0.05f, 5f)]
        [JsonProperty(Names.LongPressDelay)]
        public float LongPressDelay { get; set; }

        /// <summary> Panel-space travel that turns that hold into a drag and cancels it. </summary>
        [RuleInRange(1f, 200f)]
        [JsonProperty(Names.LongPressThreshold)]
        public float LongPressMoveThreshold { get; set; }

        // Off: the fill covers the object just selected. The global collider view is a session toggle.

        /// <summary> Whether selecting an object draws its collider. </summary>
        [JsonProperty(Names.PreviewCollider)]
        public bool PreviewColliderOnSelect { get; set; }

        // Only the two ALPHAS live here. The hue is GraphicsSettings.CollidersMode.Color, shared with the
        // game's Colliders Only mode; these two opacities stay the editor's own.

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

        // Off: a shape is clicked by what it draws. No escape hatch for a size-less node (an empty Sizes
        // track reads zero, so it has no rect). Docs/SETTING_GROUPS.md

        /// <summary> Whether a click picks an object by its whole rect instead of its own shape. </summary>
        [JsonProperty(Names.PickInvisibleAABB)]
        public bool PickInvisibleAABB { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorSelectionSettings()
        {
            ResetOwn();
        }
        /// <summary> Every member at once, in declaration order. </summary>
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
