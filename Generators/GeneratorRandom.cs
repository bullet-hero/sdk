namespace BH.SDK.Generators
{
    // Neither of the obvious options works here. System.Random's sequence is not contractually
    // stable - .NET Core reimplemented it once already - so a level generated today could come out
    // different on a future runtime, and "same seed, same level" is a promise this system makes to
    // authors. UnityEngine.Random is not even reachable: the core SDK asmdef is
    // noEngineReferences: true, on purpose (see the SDK's own CLAUDE.md).
    //
    // xorshift32 is the smallest thing that is fully specified by its own source: same seed, same
    // numbers, forever, on every runtime. Quality is adequate for scattering bullets; nothing here
    // is cryptographic (CryptographyService exists for that) or statistical.

    /// <summary>
    /// Deterministic RNG for generators. Same seed always yields the same sequence.
    /// </summary>
    public struct GeneratorRandom
    {
        private uint _state;

        /// <summary> Zero is remapped, since xorshift32 is stuck at zero forever. </summary>
        public GeneratorRandom(uint seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : seed;
        }

        /// <summary> The next number of the xorshift sequence - hand-written, so the same seed gives the same level on every runtime. </summary>
        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary> Uniform in [0, 1). </summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary> Uniform in [min, max). </summary>
        public float NextFloat(float min, float max) => min + NextFloat() * (max - min);

        /// <summary> Uniform in [min, max) - returns min when the range is empty. </summary>
        public int NextInt(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        /// <summary> One bit of it. </summary>
        public bool NextBool() => (NextUInt() & 1u) == 1u;
    }
}
