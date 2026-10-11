using System;
using System.Collections.Generic;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Agent
{
    // One decider for what may live in a level's ai/ folder and what of it travels: the archive builder,
    // the level copy and the context tools all ask here. A check answers null or the reason it refuses.

    /// <summary> What a level's <c>ai/</c> folder may hold: text files with plain names, within the size
    /// limits. </summary>
    public static class AiFolderRules
    {
        /// <summary> The extensions a file in <c>ai/</c> may carry. </summary>
        public static IReadOnlyList<string> Extensions => AllowedExtensions;

        private static readonly string[] AllowedExtensions = { ".md", ".json" };

        // A name valid here must also be a file on Windows, where these stems are devices.
        private static readonly string[] ReservedStems =
        {
            "con", "prn", "aux", "nul",
            "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
            "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
        };

        /// <summary> The store path of a file in <c>ai/</c>, relative to the level folder. </summary>
        public static string StorePath(string name) => FileNames.AiDirectory + ContentPath.Separator + name;

        /// <summary> The name of a store path directly inside <c>ai/</c>, or null for any other. </summary>
        public static string NameOf(string storePath)
        {
            if (!ContentPath.HasPrefix(storePath, FileNames.AiDirectory)) return null;

            var name = storePath.Substring(FileNames.AiDirectory.Length + 1);
            return name.IndexOf(ContentPath.Separator) < 0 ? name : null;
        }

        /// <summary> Null when the name may live in <c>ai/</c>; otherwise why it may not. </summary>
        public static string CheckName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "the name is empty";

            // CONTEXT.md is the one capital name; context.md beside it would be the same file on Windows.
            if (string.Equals(name, FileNames.AiContextFile, StringComparison.OrdinalIgnoreCase))
                return name == FileNames.AiContextFile
                    ? null
                    : $"the context file is '{FileNames.AiContextFile}'";

            foreach (var c in name)
                if (!IsNameChar(c))
                    return $"'{name}' may use only a-z, 0-9, '-', '_' and '.'";

            var extension = Path.GetExtension(name);
            if (Array.IndexOf(AllowedExtensions, extension) < 0)
                return $"'{name}' must end in {string.Join(" or ", Extensions)}";

            var stem = Path.GetFileNameWithoutExtension(name);
            if (stem.Length == 0) return $"'{name}' has no name before its extension";
            if (Array.IndexOf(ReservedStems, stem) >= 0)
                return $"'{name}' is a reserved device name on Windows";

            if (!ArchivePolicy.FitsName(StorePath(name)))
                return $"'{name}' is too long to travel in an archive";

            return null;
        }

        /// <summary> Null when one file of this size may live in <c>ai/</c>; otherwise why not. </summary>
        public static string CheckSize(long bytes) => bytes <= AgentRules.MaxContextFileBytes
            ? null
            : $"{bytes} bytes is over the {AgentRules.MaxContextFileBytes} bytes one context file may hold";

        /// <summary> Null when the whole folder may hold this many bytes; otherwise why not. </summary>
        public static string CheckFolder(long totalBytes) => totalBytes <= AgentRules.MaxContextFolderBytes
            ? null
            : $"{totalBytes} bytes is over the {AgentRules.MaxContextFolderBytes} bytes the folder may hold";

        /// <summary> Null when this file may live in <c>ai/</c>; otherwise why not. </summary>
        public static string Check(string name, long bytes) => CheckName(name) ?? CheckSize(bytes);

        /// <summary> The names that travel, in ordinal order: every file passing <see cref="Check"/> while
        /// the running total stays within the folder limit. </summary>
        public static List<string> SelectShareable(IEnumerable<KeyValuePair<string, long>> files)
        {
            var candidates = new List<KeyValuePair<string, long>>(files);
            candidates.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));

            var selected = new List<string>();
            var total = 0L;
            foreach (var file in candidates)
            {
                if (Check(file.Key, file.Value) != null) continue;
                if (CheckFolder(total + file.Value) != null) continue;

                total += file.Value;
                selected.Add(file.Key);
            }

            return selected;
        }

        private static bool IsNameChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.';
    }
}
