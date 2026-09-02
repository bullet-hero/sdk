using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// Base of every graphics sub-group: the one switch they all share.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public abstract partial class BaseGraphicsSettings : IModel<BaseGraphicsSettings>
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
    }
}