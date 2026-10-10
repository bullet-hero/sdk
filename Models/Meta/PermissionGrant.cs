using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    // Option B of the UGC licensing policy, made storable: who said yes, how far it reaches, until when,
    // where the proof is. Both timestamps are UTC (nothing can enforce it: convert before filling in);
    // default(DateTime) is unset and reads as "no constraint". Docs/RESOURCE_META.md

    /// <summary> One rights holder's permission to use a resource, and the evidence for it. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class PermissionGrant : IModel<PermissionGrant>
    {
        /// <summary> Who granted it - the rights holder, not the level author. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Grantor)]
        public Author Grantor { get; set; }

        /// <summary> How far the permission reaches. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.PermissionScope)]
        public PermissionScope Scope { get; set; }

        /// <summary> When it was given, UTC. Unset means not stated. </summary>
        [JsonProperty(Names.GrantedAt)]
        public DateTime GrantedAt { get; set; }

        /// <summary> When it lapses, UTC. Unset means open-ended. </summary>
        [JsonProperty(Names.ExpiresAt)]
        public DateTime ExpiresAt { get; set; }

        /// <summary> Public evidence a moderator can open - the post, the tweet, the store page
        /// stating the permission. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxUrl)]
        [JsonProperty(Names.ProofUrl)]
        public string ProofUrl { get; set; }

        /// <summary> The wording itself, for evidence that has no public URL (an email, a DM) -
        /// quoted so it survives the mailbox it came from. </summary>
        [RuleNotNull, RuleStringMax(ResourceRules.MaxProofText)]
        [JsonProperty(Names.ProofText)]
        public string ProofText { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public PermissionGrant()
        {
            Grantor = new Author();
            Scope = PermissionScope.Undefined;
            GrantedAt = new DateTime();
            ExpiresAt = new DateTime();
            ProofUrl = string.Empty;
            ProofText = string.Empty;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public PermissionGrant(Author grantor, PermissionScope scope,
            DateTime grantedAt, DateTime expiresAt, string proofUrl, string proofText)
        {
            Grantor = grantor;
            Scope = scope;
            GrantedAt = grantedAt;
            ExpiresAt = expiresAt;
            ProofUrl = proofUrl;
            ProofText = proofText;
        }

        // An unset `now` is "this caller has no clock", not "the epoch". A client checking a level
        // offline still has to reach a verdict, and lapsing every dated grant because nobody passed
        // the time in would refuse levels for a reason that has nothing to do with them.

        /// <summary> True while the grant still stands. An open-ended grant never lapses, and
        /// neither does any grant when `now` is unset. </summary>
        public bool IsActiveAt(DateTime now)
        {
            if (ExpiresAt == default) return true;
            if (now == default) return true;
            return now <= ExpiresAt;
        }

        /// <summary> True when there is something a moderator can actually check. </summary>
        public bool HasProof() => !string.IsNullOrWhiteSpace(ProofUrl)
                                  || !string.IsNullOrWhiteSpace(ProofText);
    }
}
