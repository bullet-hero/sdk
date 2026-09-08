using System;
using System.Runtime.CompilerServices;

namespace BH.SDK.Versions
{
    /// <summary> One envelope's two halves: what version it was written at, and what was inside it. </summary>
    public readonly struct EnvelopeData
    {
        /// <summary> The version the file claimed - the one it was WRITTEN at, not the one it now holds. </summary>
        public readonly Version Version;

        /// <summary> The payload, already upgraded to the domain's current shape. Untyped, not un-migrated. </summary>
        public readonly object RawPayload;

        /// <summary> Built from its version and payload. </summary>
        public EnvelopeData(Version version, object rawPayload)
        {
            Version = version;
            RawPayload = rawPayload;
        }

        /// <summary> The payload as the caller's own type. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetPayload<TValue>() => (TValue)RawPayload;
    }
}