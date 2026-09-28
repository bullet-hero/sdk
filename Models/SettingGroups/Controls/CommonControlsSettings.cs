using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Controls
{
    /// <summary>
    /// Settings above any single device: how the shared in-world cursor looks and behaves.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class CommonControlsSettings : IModel<CommonControlsSettings>, IMoveable<CommonControlsSettings>
    {
        /// <summary> Whether the in-world cursor is drawn at all. It still exists and still steers the
        /// avatar when hidden. </summary>
        [JsonProperty(Names.CursorVisible)]
        public bool CursorVisible { get; set; }

        /// <summary> How large the inframe cursor is drawn - it is a real world-space object, so it letterboxes
        /// and scales with the level. </summary>
        [RuleInRange(ControlsRules.MinCursorScale, ControlsRules.MaxCursorScale)]
        [JsonProperty(Names.CursorScale)]
        public float CursorScale { get; set; }

        /// <summary> The cursor is born on the avatar rather than where it was left. </summary>
        [JsonProperty(Names.CursorRecenter)]
        public bool CursorRecenter { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CommonControlsSettings()
        {
            CursorVisible = true;
            CursorScale = ControlsRules.DefaultCursorScale;
            CursorRecenter = true;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public CommonControlsSettings(bool cursorVisible, float cursorScale, bool cursorRecenter)
        {
            CursorVisible = cursorVisible;
            CursorScale = cursorScale;
            CursorRecenter = cursorRecenter;
        }
    }
}
