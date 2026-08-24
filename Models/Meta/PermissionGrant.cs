using System;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    // Option B of the UGC licensing policy, made storable. A resource whose own license forbids
    // redistribution can still be published if its rights holder said yes specifically - but a
    // private "sure, go ahead" is worthless to a moderator who cannot see it, so the claim has to
    // carry who said it, how far it reaches, until when, and where the proof is. Without this type
    // the only way to express Option B was a free-text line in ResourceSources, which no code and no
    // moderator can act on.
    //
    // Both timestamps are UTC, and the only reason nothing enforces that is that nothing can: a
    // DateTime carries its Kind through JSON (ISO-8601 with the offset) and through BSON (a UTC
    // instant by definition), but a caller handing over a Local one still writes a moment that reads
    // differently on another machine. Whoever fills these in converts first - a permission that
    // lapses "on the 3rd" must not lapse a day early for a moderator in another timezone.
    //
    // default(DateTime) is the unset value, and it means two different things by position, both of
    // which read correctly as "no constraint": on ExpiresAt it is an open-ended grant, which is the
    // common case, and on GrantedAt it is a record whose author did not bother with the date.

    /// <summary> One rights holder's permission to use a resource, and the evidence for it. </summary>
    [RuleContainer]
    public class PermissionGrant : IModel<PermissionGrant>
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

        public PermissionGrant()
        {
            Grantor = new Author();
            Scope = PermissionScope.Undefined;
            GrantedAt = new DateTime();
            ExpiresAt = new DateTime();
            ProofUrl = string.Empty;
            ProofText = string.Empty;
        }
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
        public void Reset()
        {
            Grantor = new Author();
            Scope = PermissionScope.Undefined;
            GrantedAt = new DateTime();
            ExpiresAt = new DateTime();
            ProofUrl = string.Empty;
            ProofText = string.Empty;
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

        public object Clone() => Copy();
        public PermissionGrant Copy() => new(Grantor.Copy(), Scope,
            GrantedAt, ExpiresAt, ProofUrl, ProofText);

        public void Update(PermissionGrant src)
        {
            Grantor = src.Grantor.Copy();
            Scope = src.Scope;
            GrantedAt = src.GrantedAt;
            ExpiresAt = src.ExpiresAt;
            ProofUrl = src.ProofUrl;
            ProofText = src.ProofText;
        }

        public void Pull(PermissionGrant src)
        {
            Grantor.Pull(src.Grantor);
            Scope = src.Scope;
            GrantedAt = src.GrantedAt;
            ExpiresAt = src.ExpiresAt;
            ProofUrl = src.ProofUrl;
            ProofText = src.ProofText;
        }

        public override bool Equals(object obj) => obj is PermissionGrant value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Grantor, (int)Scope,
            GrantedAt, ExpiresAt, ProofUrl, ProofText);

        public bool Equals(PermissionGrant other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Grantor.Equals(other.Grantor)
                   && Scope == other.Scope
                   && GrantedAt == other.GrantedAt
                   && ExpiresAt == other.ExpiresAt
                   && ProofUrl.Equals(other.ProofUrl)
                   && ProofText.Equals(other.ProofText);
        }
    }
}
