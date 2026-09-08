using System.Runtime.CompilerServices;

namespace BH.SDK.Versions
{
    /// <summary> One envelope's two halves: what generation it was written at, and what was inside it. </summary>
    public readonly struct EnvelopeData
    {
        /// <summary> The generation the file claimed - the one it was WRITTEN at, not the one it now
        /// holds. <see cref="ModelGenerations.Invalid"/> when nothing was read. </summary>
        public readonly int Generation;

        /// <summary> The payload, already upgraded to the domain's current shape. Untyped, not un-migrated. </summary>
        public readonly object RawPayload;

        /// <summary> Built from its generation and payload. </summary>
        public EnvelopeData(int generation, object rawPayload)
        {
            Generation = generation;
            RawPayload = rawPayload;
        }

        /// <summary> The payload as the caller's own type. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetPayload<TValue>() => (TValue)RawPayload;
    }
}