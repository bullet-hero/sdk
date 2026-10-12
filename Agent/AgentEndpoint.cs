using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BH.SDK.Models;
using Newtonsoft.Json;

namespace BH.SDK.Agent
{
    /// <summary> The operating systems the game's data path is known for. </summary>
    public enum AgentOs
    {
        /// <summary> Windows. </summary>
        Windows,

        /// <summary> Linux. </summary>
        Linux,

        /// <summary> macOS. </summary>
        MacOs,
    }

    /// <summary>
    /// The endpoint file the running game writes and the MCP server reads: where the bridge listens and the
    /// token that opens it. Machine-local, never part of a level, and not a versioned model.
    /// </summary>
    public sealed class AgentEndpoint
    {
        /// <summary> The company name Unity builds the data path from. </summary>
        public const string Company = "vertoker";

        /// <summary> The product name Unity builds the data path from. </summary>
        public const string Product = "Bullet Hero";

        /// <summary> The <see cref="AgentProtocol.Version"/> of the game that wrote it. </summary>
        [JsonProperty("protocol")]
        public int Protocol { get; set; }

        /// <summary> The loopback port the bridge listens on. </summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary> The session token, <see cref="AgentProtocol.TokenLength"/> hex digits. </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary> The game process; tells the server which of two running games it reached. </summary>
        [JsonProperty("pid")]
        public int Pid { get; set; }

        /// <summary> The client version of the game that wrote it. </summary>
        [JsonProperty("gv")]
        public string Gv { get; set; }

        /// <summary> A new endpoint for this process with a fresh random token. </summary>
        public static AgentEndpoint Create(int port, int pid, string gv) => new AgentEndpoint
        {
            Protocol = AgentProtocol.Version,
            Port = port,
            Token = NewToken(),
            Pid = pid,
            Gv = gv,
        };

        /// <summary> A random token of <see cref="AgentProtocol.TokenLength"/> hex digits. </summary>
        public static string NewToken()
        {
            var bytes = new byte[AgentProtocol.TokenLength / 2];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        /// <summary> The OS this code runs on; anything that is neither Windows nor macOS counts as Linux.
        /// </summary>
        public static AgentOs CurrentOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return AgentOs.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return AgentOs.MacOs;
            return AgentOs.Linux;
        }

        /// <summary> Where Unity's <c>persistentDataPath</c> of this game is for <paramref name="os"/>, under
        /// the user's <paramref name="home"/> directory. </summary>
        public static string PersistentDataPath(AgentOs os, string home)
        {
            switch (os)
            {
                case AgentOs.Windows: return Path.Combine(home, "AppData", "LocalLow", Company, Product);
                case AgentOs.MacOs: return Path.Combine(home, "Library", "Application Support", Company, Product);
                default: return Path.Combine(home, ".config", "unity3d", Company, Product);
            }
        }

        /// <summary> The endpoint file inside a data path. </summary>
        public static string PathIn(string persistentDataPath)
            => Path.Combine(persistentDataPath, FileNames.AgentDirectory, FileNames.AgentEndpointFile);

        /// <summary> The endpoint file of an installed game for <paramref name="os"/>. </summary>
        public static string DefaultPath(AgentOs os, string home) => PathIn(PersistentDataPath(os, home));

        /// <summary> The endpoint file of the game on this machine. </summary>
        public static string DefaultPath()
            => DefaultPath(CurrentOs(), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        /// <summary> The folder of per-game endpoint files beside <paramref name="endpointPath"/>, so a server
        /// started with <c>--endpoint</c> finds the games of exactly that data folder. </summary>
        public static string InstancesDirectoryFor(string endpointPath)
            => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(endpointPath)) ?? string.Empty,
                FileNames.AgentInstancesDirectory);

        /// <summary> The endpoint file of the game <paramref name="pid"/> beside
        /// <paramref name="endpointPath"/>. </summary>
        public static string InstancePath(string endpointPath, int pid)
            => Path.Combine(InstancesDirectoryFor(endpointPath), pid + ".json");

        // No liveness check: whether a pid is alive is the server's question, and a crashed game's file
        // stays until something reads past it.

        /// <summary> Every usable per-game endpoint file beside <paramref name="endpointPath"/>, by pid; a
        /// file whose name is not its own pid is skipped. </summary>
        public static IReadOnlyList<AgentEndpoint> ListInstances(string endpointPath)
        {
            var directory = InstancesDirectoryFor(endpointPath);
            if (!Directory.Exists(directory)) return Array.Empty<AgentEndpoint>();

            return Directory.GetFiles(directory, "*.json")
                .Select(file => TryRead(file, out var endpoint, out _) &&
                                Path.GetFileNameWithoutExtension(file) == endpoint.Pid.ToString()
                    ? endpoint
                    : null)
                .Where(endpoint => endpoint != null)
                .OrderBy(endpoint => endpoint.Pid)
                .ToList();
        }

        /// <summary> Writes the file whole or not at all, creating its folder. </summary>
        public void Write(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var temporary = path + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(this, Formatting.Indented),
                new UTF8Encoding(false));

            try
            {
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch
            {
                File.Delete(temporary);
                throw;
            }
        }

        /// <summary> Reads and checks the file; <paramref name="error"/> says what is wrong when it is not
        /// usable. </summary>
        public static bool TryRead(string path, out AgentEndpoint endpoint, out string error)
        {
            endpoint = null;

            if (!File.Exists(path))
            {
                error = $"No endpoint file at {path}.";
                return false;
            }

            try
            {
                endpoint = JsonConvert.DeserializeObject<AgentEndpoint>(File.ReadAllText(path));
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                error = $"Cannot read the endpoint file {path}: {e.Message}";
                return false;
            }

            if (endpoint == null || endpoint.Port < 1 || endpoint.Port > 65535 || !IsToken(endpoint.Token))
            {
                endpoint = null;
                error = $"The endpoint file {path} has no valid port and token.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary> Removes the file when the bridge stops; a missing file is not an error. </summary>
        public static void Delete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private static bool IsToken(string text)
            => text != null && text.Length == AgentProtocol.TokenLength && text.All(Uri.IsHexDigit);
    }
}