using System;
using System.Collections.Generic;
using System.Text;

namespace BH.SDK.Agent
{
    /// <summary> The game's launch-argument line, split the way the game reads it; shared with the
    /// server, which starts the game with such a line. </summary>
    public static class AgentLaunchLine
    {
        /// <summary> Turns one typed line into tokens, keeping a quoted value whole. </summary>
        public static string[] Split(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return Array.Empty<string>();

            var tokens = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            var started = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    // An empty pair of quotes is still a token - it is how a value is deliberately
                    // blanked - so the flag survives the character that produced nothing.
                    quoted = !quoted;
                    started = true;
                    continue;
                }

                if (!quoted && char.IsWhiteSpace(c))
                {
                    if (started) tokens.Add(current.ToString());
                    current.Clear();
                    started = false;
                    continue;
                }

                current.Append(c);
                started = true;
            }

            if (started) tokens.Add(current.ToString());

            return tokens.ToArray();
        }
    }
}
