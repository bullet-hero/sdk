using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// A music or sfx file the level brings with it - usually the song itself, which is why almost
    /// every level has at least one. Referenced by LevelTrack.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioResource : Resource, IModel<AudioResource>
    {
        /// <summary> Identity of this clip within the level. </summary>
        [RuleIPrimitiveIntMax(AudioResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.AudioResourceId)]
        public AudioResourceId AudioResourceId { get; set; }
        
        /// <summary> Which kind of resource this is. </summary>
        [JsonProperty(Names.Type)]
        public override ResourceType Type => ResourceType.Audio;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AudioResource()
        {
            AudioResourceId = AudioResourceId.Null;
        }
        /// <summary> Built from its resource id and sources. </summary>
        public AudioResource(AudioResourceId audioResourceId, List<ResourceKey> sources) : base(sources)
        {
            AudioResourceId = audioResourceId;
        }
    }
}