using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Events
{
    /// <summary>
    /// A note the mapper leaves on the timeline ("chorus starts here"). Editor-only: it is saved with
    /// the level and read back, but has no effect on playback whatsoever.
    /// </summary>
    [RuleContainer]
    public class Marker : IFrame, IModel<Marker>
    {
        /// <summary> Level frame the note is pinned to. </summary>
        [RuleLevelFrame]
        [JsonProperty(Names.FrameShort)]
        public int Frame { get; set; }

        /// <summary> Short label shown on the timeline. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> Longer note behind the label. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorDescription)]
        [JsonProperty(Names.Description)]
        public string Description { get; set; }

        /// <summary> Marker color in the editor. Concrete Color4Value, not IColor4 - an editor
        /// annotation has nothing to do with the level's theme. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Color)]
        public Color4Value Color4 { get; set; }

        public Marker()
        {
            Frame = FrameRules.MinFrame;
            Name = string.Empty;
            Description = string.Empty;
            Color4 = new Color4Value();
        }
        public Marker(string name, string description, Color4Value color4, int frame)
        {
            Frame = frame;
            Name = name;
            Description = description;
            Color4 = color4;
        }
        public void Reset()
        {
            Frame = FrameRules.MinFrame;
            Name = string.Empty;
            Description = string.Empty;
            Color4 = new Color4Value();
        }

        public object Clone() => Copy();
        public Marker Copy() => new(Name, Description, Color4.Copy(), Frame);

        public void Update(Marker src)
        {
            Name = src.Name;
            Description = src.Description;
            Color4 = src.Color4.Copy();
            Frame = src.Frame;
        }

        public void Pull(Marker src)
        {
            Name = src.Name;
            Description = src.Description;
            Color4.Pull(src.Color4);
            Frame = src.Frame;
        }

        public override bool Equals(object obj) => obj is Marker value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Frame, Name, Description, Color4);

        public bool Equals(Marker other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Frame.Equals(other.Frame)
                         && Name.Equals(other.Name)
                         && Description.Equals(other.Description)
                         && Color4.Equals(other.Color4);
            return result;
        }
    }
}