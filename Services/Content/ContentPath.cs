using System;

namespace BH.SDK.Services.Content
{
    // The one place that decides what a path inside a store may look like, and it REFUSES rather
    // than repairs. Every alternative is worse in the same way: converting a backslash to a slash
    // renames a file that legitimately carries one on Linux, collapsing "a/../b" resolves a
    // traversal instead of reporting it, and trimming a trailing slash turns a caller's directory
    // into a blob it never meant to write. A store is addressed by names that came from an archive,
    // a database or a hostile upload - the answer to a bad one is "no", not a guess at what was
    // meant.
    //
    // Comparison is ORDINAL everywhere, case included: a package written on Linux has to read back
    // as the same package on Windows, and a case-folding store would merge two entries a tar file
    // holds separately. What the file system underneath a DirectoryContentStore does about case is
    // its own answer and this cannot change it - a stated gap, not a claim.

    /// <summary> Validation of the relative paths an <see cref="IContentStore"/> is addressed by. </summary>
    public static class ContentPath
    {
        /// <summary> The separator a store path uses, on every platform. </summary>
        public const char Separator = '/';

        /// <summary> Whether this is a legal store path. </summary>
        public static bool IsValid(string path) => TryValidate(path, out _);

        /// <summary> Validates a store path, naming what is wrong with it when it is not one. </summary>
        public static bool TryValidate(string path, out string error)
        {
            if (string.IsNullOrEmpty(path))
            {
                error = "path is empty";
                return false;
            }

            if (path[0] == Separator)
            {
                error = "path is rooted";
                return false;
            }

            if (path[path.Length - 1] == Separator)
            {
                error = "path names a directory, not a file";
                return false;
            }

            // A drive letter is what "C:file.txt" is - a path relative to another drive's current
            // directory, which resolves outside the root while looking relative.
            if (path.Length >= 2 && path[1] == ':')
            {
                error = "path carries a drive letter";
                return false;
            }

            var segmentStart = 0;
            for (var i = 0; i <= path.Length; i++)
            {
                if (i < path.Length)
                {
                    var c = path[i];
                    if (c == '\\')
                    {
                        error = "path contains a backslash";
                        return false;
                    }

                    if (c < ' ' || c == '\u007f')
                    {
                        error = "path contains a control character";
                        return false;
                    }

                    if (c != Separator) continue;
                }

                var length = i - segmentStart;
                if (length == 0)
                {
                    error = "path contains an empty segment";
                    return false;
                }

                if (length == 1 && path[segmentStart] == '.')
                {
                    error = "path contains a '.' segment";
                    return false;
                }

                if (length == 2 && path[segmentStart] == '.' && path[segmentStart + 1] == '.')
                {
                    error = "path contains a '..' segment";
                    return false;
                }

                segmentStart = i + 1;
            }

            error = null;
            return true;
        }

        /// <summary> Returns the path, or throws naming what is wrong with it. </summary>
        public static string Require(string path, string parameterName)
        {
            if (TryValidate(path, out var error)) return path;
            throw new ArgumentException($"'{path}' is not a valid store path: {error}.", parameterName);
        }

        // Segment boundaries are what makes this a PREFIX of the tree rather than of the string:
        // "audio" must not match "audio-backup.ogg", and every listing in this project is a listing
        // of a folder.

        /// <summary> Whether a store path lies under a prefix. An empty prefix matches everything. </summary>
        public static bool HasPrefix(string path, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return true;
            if (path == null || path.Length <= prefix.Length) return false;
            if (string.CompareOrdinal(path, 0, prefix, 0, prefix.Length) != 0) return false;

            var boundary = prefix[prefix.Length - 1] == Separator ? prefix.Length - 1 : prefix.Length;
            return path[boundary] == Separator;
        }
    }
}