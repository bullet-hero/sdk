using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// Base of every graphics sub-group: the one switch they all share.
    /// </summary>
    [RuleContainer]
    public abstract class BaseGraphicsSettings : IModel<BaseGraphicsSettings>
    {
        /// <summary> Whether this subsystem renders at all - the cheapest way for a weak device to
        /// drop a whole feature instead of tuning it. </summary>
        [JsonProperty(Names.Render)]
        public bool Render { get; set; }

        protected BaseGraphicsSettings()
        {
            Render = true;
        }
        protected BaseGraphicsSettings(bool render)
        {
            Render = render;
        }
        public virtual void Reset()
        {
            Render = true;
        }

        public abstract object Clone();
        public abstract BaseGraphicsSettings Copy();

        public void Update(BaseGraphicsSettings src)
        {
            Render = src.Render;
        }

        public void Pull(BaseGraphicsSettings src)
        {
            Render = src.Render;
        }

        public override bool Equals(object obj) => obj is BaseGraphicsSettings value && Equals(value);
        public override int GetHashCode() => Render.GetHashCode();

        public bool Equals(BaseGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Render == other.Render;
        }
    }
}