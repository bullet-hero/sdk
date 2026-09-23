using System;

namespace BH.SDK.Versions
{
    // A NEWER FILE IS REFUSED, NOT DEGRADED. Every model change since 1.0.0 is a generation bump, so
    // a generation above this build's latest is a shape this build has provably never seen - reading
    // it by property name would open a file that is silently missing whatever moved, and the next
    // save would write that loss over the original. The reader throws instead, and the caller tells
    // the player to update. Docs/VERSIONING.md lists every site that throws it.

    /// <summary> A file claims a generation of <see cref="Domain"/> newer than this build knows. Thrown before
    /// anything is read on the payload's behalf; nothing the reader returned is usable. </summary>
    public sealed class NewerGenerationException : Exception
    {
        /// <summary> The domain whose envelope carried the newer generation. </summary>
        public string Domain { get; }

        /// <summary> The generation the file claims. </summary>
        public int FileGeneration { get; }

        /// <summary> The newest generation of <see cref="Domain"/> this build knows. </summary>
        public int BuildGeneration { get; }

        /// <summary> Names all three in the message. </summary>
        public NewerGenerationException(string domain, int fileGeneration, int buildGeneration)
            : base($"'{domain}' is at generation {fileGeneration}, newer than this build's {buildGeneration}")
        {
            Domain = domain;
            FileGeneration = fileGeneration;
            BuildGeneration = buildGeneration;
        }
    }
}
