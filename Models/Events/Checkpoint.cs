using System;
using BH.SDK.Models.Enums;
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
    /// A point playback rewinds to after a death. Real gameplay state, unlike the purely decorative
    /// Marker it sits next to - place them and a hard section stops costing the whole run.
    /// </summary>
    [RuleContainer]
    public class Checkpoint : IFrame, IModel<Checkpoint>
    {
        /// <summary> Level frame a retry resumes from. </summary>
        [RuleLevelFrame]
        [JsonProperty(Names.FrameShort)]
        public int Frame { get; set; }

        /// <summary> Editor-facing label. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> Whether the checkpoint counts - lets a mapper disable one without deleting it
        /// and losing its position. </summary>
        [JsonProperty(Names.ActiveShort)]
        public bool Active { get; set; }

        /// <summary> Color of the in-game checkpoint marker; themeable, unlike Marker's. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.Color)]
        public IColor4 Color4 { get; set; }

        /// <summary> Where a retry puts the player, read in <see cref="Space"/>. Polymorphic like
        /// every other authored point, so a checkpoint can scatter its respawn. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinPos, ValueRules.MaxPos)]
        [JsonProperty(Names.Position)]
        public IVector2 Position { get; set; }

        /// <summary> How <see cref="Position"/> is interpreted. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Space)]
        public CheckpointSpace Space { get; set; }

        public Checkpoint()
        {
            Frame = FrameRules.MinFrame;
            Name = string.Empty;
            Active = true;
            Color4 = Color4Value.white;
            Position = Vector2Value.Zero;
            Space = CheckpointSpace.World;
        }
        public Checkpoint(string name, bool active, IColor4 color4, int frame)
            : this(name, active, color4, frame, Vector2Value.Zero, CheckpointSpace.World) { }
        public Checkpoint(string name, bool active, IColor4 color4, int frame,
            IVector2 position, CheckpointSpace space)
        {
            Frame = frame;
            Name = name;
            Active = active;
            Color4 = color4;
            Position = position;
            Space = space;
        }
        public void Reset()
        {
            Frame = FrameRules.MinFrame;
            Name = string.Empty;
            Active = true;
            Color4 = Color4Value.white;
            Position = Vector2Value.Zero;
            Space = CheckpointSpace.World;
        }

        public object Clone() => Copy();
        public Checkpoint Copy() => new(Name, Active, Color4.Copy(), Frame, Position.Copy(), Space);

        public override bool Equals(object obj) => obj is Checkpoint value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Frame, Name, Active, Color4, Position, Space);

        public bool Equals(Checkpoint other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Frame.Equals(other.Frame)
                         && Name.Equals(other.Name)
                         && Active == other.Active
                         && Color4.Equals(other.Color4)
                         && Position.Equals(other.Position)
                         && Space == other.Space;
            return result;
        }
    }
}