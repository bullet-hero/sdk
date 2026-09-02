using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The three autosave numbers and the history depth are one group because they answer one
    // question - how much work is an author willing to lose - and they trade against the same
    // resource from two directions: autosaves cost disk, the history costs memory. An author who
    // turns one of them up has usually just lost something and wants the other up too.

    /// <summary>
    /// How far back the editor can take an author: its autosave policy and the depth of its
    /// operation history.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorSavingsSettings : IModel<EditorSavingsSettings>, IMoveable<EditorSavingsSettings>
    {
        /// <summary> Whether the editor saves on its own. </summary>
        [JsonProperty(Names.Autosave)]
        public bool Autosave { get; set; }

        /// <summary> Seconds between autosaves. </summary>
        [RuleMinValue(1f)]
        [JsonProperty(Names.Rate)]
        public float AutosaveRate { get; set; }

        /// <summary> How many autosaves are kept before the oldest is dropped - the depth of the
        /// safety net, traded against disk space. </summary>
        [RuleInRange(1, 1000)]
        [JsonProperty(Names.MaxFiles)]
        public int MaxAutosaveFiles { get; set; }

        // Nodes rather than steps, and the difference matters once a branch exists: the history is a
        // tree, so an abandoned line still occupies its nodes until the whole branch is evicted. The
        // ceiling is what a session may hold, never what one line may reach.

        /// <summary> Operations the history tree holds before its oldest branch is dropped. </summary>
        [RuleInRange(16, 8192)]
        [JsonProperty(Names.HistoryLength)]
        public int HistoryLength { get; set; }

        public EditorSavingsSettings()
        {
            ResetOwn();
        }
        public EditorSavingsSettings(bool autosave, float autosaveRate, int maxAutosaveFiles, int historyLength)
        {
            Autosave = autosave;
            AutosaveRate = autosaveRate;
            MaxAutosaveFiles = maxAutosaveFiles;
            HistoryLength = historyLength;
        }
        private void ResetOwn()
        {
            Autosave = true;
            AutosaveRate = 60f;
            MaxAutosaveFiles = 25;
            HistoryLength = 512;
        }
    }
}
