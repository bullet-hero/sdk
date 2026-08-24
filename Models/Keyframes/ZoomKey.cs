using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Camera zoom key. The camera's replacement for a two-axis size track: zoom is one number
    /// because stretching the view per axis would break the level's aspect contract.
    /// </summary>
    [RuleContainer]
    public class ZoomKey : Keyframe, IModel<ZoomKey>
    {
        /// <summary> Visible-area multiplier at this frame - smaller means closer in. </summary>
        [RuleNotNull(typeof(FloatValue)), RuleIFloatInRange(ValueRules.MinZoom, ValueRules.MaxZoom)]
        [JsonProperty(Names.Float)]
        public IFloat Zoom { get; set; }

        public ZoomKey()
        {
            Zoom = new FloatValue(ValueRules.DefaultZoom);
        }
        public ZoomKey(IFloat zoom, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Zoom = zoom;
        }
        public override void Reset()
        {
            base.Reset();
            Zoom = new FloatValue(ValueRules.DefaultZoom);
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ZoomKey ICopyable<ZoomKey>.Copy() => CopyImpl();
        
        private ZoomKey CopyImpl() => new(Zoom.Copy(), Frame, Ease);

        public void Update(ZoomKey src)
        {
            base.Update(src);

            Zoom = src.Zoom.Copy();
        }

        public void Pull(ZoomKey src)
        {
            base.Pull(src);

            Zoom = Zoom.PullFrom(src.Zoom);
        }

        public override bool Equals(object obj) => obj is ZoomKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Zoom);

        public bool Equals(ZoomKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Zoom.Equals(other.Zoom);
            return result;
        }
    }
}