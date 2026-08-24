using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// A music or sfx file the level brings with it - usually the song itself, which is why almost
    /// every level has at least one. Referenced by LevelTrack.
    /// </summary>
    [RuleContainer]
    public class AudioResource : Resource, IModel<AudioResource>
    {
        /// <summary> Identity of this clip within the level. </summary>
        [RuleIPrimitiveIntMax(AudioResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.AudioResourceId)]
        public AudioResourceId AudioResourceId { get; set; }
        
        public override ResourceType Type => ResourceType.Audio;

        public AudioResource()
        {
            AudioResourceId = AudioResourceId.Null;
        }
        public AudioResource(AudioResourceId audioResourceId, List<ResourceKey> sources) : base(sources)
        {
            AudioResourceId = audioResourceId;
        }
        public override void Reset()
        {
            base.Reset();
            AudioResourceId = AudioResourceId.Null;
        }
        
        public override object Clone() => CopyImpl();
        public override Resource Copy() => CopyImpl();
        AudioResource ICopyable<AudioResource>.Copy() => CopyImpl();
        
        private AudioResource CopyImpl() => new(AudioResourceId, Sources.CopyList());

        public void Update(AudioResource src)
        {
            base.Update(src);

            AudioResourceId = src.AudioResourceId;
        }

        public void Pull(AudioResource src)
        {
            base.Pull(src);

            AudioResourceId = src.AudioResourceId;
        }

        public override bool Equals(object obj) => obj is AudioResource value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AudioResourceId);

        public bool Equals(AudioResource other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && AudioResourceId.Equals(other.AudioResourceId);
            return result;
        }
    }
}