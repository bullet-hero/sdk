using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Named for the EDITOR's interface, and unrelated to UserSettings.Interface, which is the game's
    // own overlays. The two share the "iface" key and can never co-occur - one is a group of
    // UserSettings, the other a group of its GameEditor group - which is exactly the reuse Names' own
    // header allows.

    /// <summary>
    /// How the editor's own panels read and behave: how eagerly a field commits, which unit an angle
    /// is shown in, and how much the console is told.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorInterfaceSettings : IModel<EditorInterfaceSettings>, IMoveable<EditorInterfaceSettings>
    {
        // The debounce every inspector field commits through. Zero is a legitimate choice - it means
        // "commit on every keystroke", which is what a slow-typing author wants and what an author
        // typing a four-digit frame number very much does not, since each intermediate number is an
        // undoable edit.
        //
        // Read when a view is CONSTRUCTED, not per keystroke, so a change reaches the editor the next
        // time it opens rather than mid-session. That is a real limitation and the reason it is
        // stated here: the alternative is threading a live reference through thirty-odd views for a
        // number nobody changes twice.

        /// <summary> Quiet time after a keystroke before an inspector field commits its edit. </summary>
        [RuleInRange(0f, 5f)]
        [JsonProperty(Names.DirtyFieldDelay)]
        public float DirtyFieldDelay { get; set; }

        /// <summary> Which unit the editor's rotation fields are read and typed in. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.RotationUnit)]
        public AngleDisplayUnit RotationDisplayUnit { get; set; }

        // The clamp itself is not optional; only the telling is. A value pushed back into its rule is
        // a thing the author asked for and did not get, so the default is to say so - but an author
        // dragging a slider against its own ceiling does not need to be told thirty times.

        /// <summary> Whether a value clamped back into its rule is reported to the editor console. </summary>
        [JsonProperty(Names.LogClamps)]
        public bool LogValueClamps { get; set; }

        // Off by default: inframes are what an effect SPAWNS while it plays - engine-owned rows that
        // cannot be selected, edited or addressed, appearing and vanishing on their own as the
        // playhead moves. That is a diagnostic view of the simulation rather than the content the
        // tree exists to navigate, so it is the author who asks for it.

        /// <summary> Whether the editor's frame hierarchy lists the objects effects spawn at runtime. </summary>
        [JsonProperty(Names.RenderInframes)]
        public bool RenderInframes { get; set; }

        // Off by default, and it is a MODIFIER of one action rather than a state of anything: while
        // it is on, explicitly picking a shape writes that same id into the object's ColliderId, so
        // the hitbox follows the silhouette without a second trip through the collider picker. It is
        // a preference of the person editing rather than data about a level, which is why it lives
        // here and why ShapeObject grew no field for it - a link is not a third answer beside the two
        // ids, it is how one of them is typed in.
        //
        // Picking None never reaches the collider: a Null ShapeId beside a real ColliderId is how an
        // invisible hitbox is authored, and a mode that erased it would take an authoring idiom away.

        /// <summary> Whether picking a shape also writes it as that object's collider. </summary>
        [JsonProperty(Names.LinkColliderShape)]
        public bool LinkColliderToShape { get; set; }

        public EditorInterfaceSettings()
        {
            ResetOwn();
        }
        public EditorInterfaceSettings(float dirtyFieldDelay, AngleDisplayUnit rotationDisplayUnit,
            bool logValueClamps, bool renderInframes, bool linkColliderToShape)
        {
            DirtyFieldDelay = dirtyFieldDelay;
            RotationDisplayUnit = rotationDisplayUnit;
            LogValueClamps = logValueClamps;
            RenderInframes = renderInframes;
            LinkColliderToShape = linkColliderToShape;
        }
        private void ResetOwn()
        {
            DirtyFieldDelay = 0.05f;
            RotationDisplayUnit = AngleDisplayUnit.Degrees;
            LogValueClamps = true;
            RenderInframes = false;
            LinkColliderToShape = false;
        }
    }
}
