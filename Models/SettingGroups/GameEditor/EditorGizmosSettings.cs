using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // One property, and it stays its own group rather than joining Selection: a gizmo is what an
    // author DRAGS, a selection is what they PICK, and the numbers that will join this one (grab
    // margins, arrow reach) all describe the former. Everything else about a gizmo - its colours,
    // its snap steps, the shapes themselves - is either the UI's theme or tuning nobody edits per
    // session, and stays in the project's own asset.

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

        public EditorGizmosSettings()
        {
            ResetOwn();
        }
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
