using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Its own group: a gizmo is what an author DRAGS, a selection is what they PICK.

    /// <summary>
    /// The editor's viewport drag handles.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorGizmosSettings : IModel<EditorGizmosSettings>, IMoveable<EditorGizmosSettings>
    {
        // A handle keeps a roughly constant ON-SCREEN size across zoom levels, the same way Unity's
        // own move/rotate/scale gizmos do; this scales that screen size. It is a preference because
        // the size that reads as precise under a mouse reads as unhittable under a thumb.

        /// <summary> Multiplier on every gizmo handle's on-screen size. </summary>
        [RuleInRange(0.1f, 10f)]
        [JsonProperty(Names.Scale)]
        public float Scale { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorGizmosSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from its scale. </summary>
        public EditorGizmosSettings(float scale)
        {
            Scale = scale;
        }
        private void ResetOwn()
        {
            Scale = 1f;
        }
    }
}
