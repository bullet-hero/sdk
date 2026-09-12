using Unity.Collections;

// ReSharper disable InconsistentNaming

namespace BH.SDK
{
    public struct constants
    {
        public const int jobInnerBatchCount = CollectionHelper.CacheLineSize;
    }
}