using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The preview player's toggle is also what decides who owns the viewport's touches, so its
    // starting state is a real preference rather than a constant: a desktop author wants the player
    // there from the first frame (a mouse loses nothing to it), a phone author does not, since the
    // whole viewport goes to the avatar the moment it exists. The platform only picks the value a
    // FRESH settings file is born with - after that it is the author's own.

    /// <summary>
    /// How the editor's preview player behaves when it is switched on.
    /// </summary>
    [RuleContainer]
    public class EditorPlayerSettings : IModel<EditorPlayerSettings>, IMoveable<EditorPlayerSettings>
    {
        /// <summary> Whether the editor's preview player starts switched on. </summary>
        [JsonProperty(Names.ActiveDefault)]
        public bool ActiveDefault { get; set; }

        /// <summary> Whether switching the preview player on drops the gizmo mode back to None. </summary>
        [JsonProperty(Names.ResetGizmos)]
        public bool ResetGizmos { get; set; }

        public EditorPlayerSettings()
        {
            ResetOwn();
        }
        public EditorPlayerSettings(bool activeDefault, bool resetGizmos)
        {
            ActiveDefault = activeDefault;
            ResetGizmos = resetGizmos;
        }
        public void Reset() => ResetOwn();
        private void ResetOwn()
        {
            ActiveDefault = true;
            ResetGizmos = true;
        }

        public object Clone() => Copy();
        public EditorPlayerSettings Copy() => new(ActiveDefault, ResetGizmos);

        public void Pull(EditorPlayerSettings source)
        {
            ActiveDefault = source.ActiveDefault;
            ResetGizmos = source.ResetGizmos;
        }

        public void Update(EditorPlayerSettings src)
        {
            ActiveDefault = src.ActiveDefault;
            ResetGizmos = src.ResetGizmos;
        }

        public override int GetHashCode() => HashCode.Combine(ActiveDefault, ResetGizmos);
        public override bool Equals(object obj) => obj is EditorPlayerSettings value && Equals(value);

        public bool Equals(EditorPlayerSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ActiveDefault == other.ActiveDefault && ResetGizmos == other.ResetGizmos;
        }
    }
}
