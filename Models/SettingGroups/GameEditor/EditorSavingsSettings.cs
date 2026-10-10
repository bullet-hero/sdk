using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // One group: the autosave numbers and the history depth answer how much work an author will lose.

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

        // A second ceiling, in OBJECTS (a node costs what it cached). 0 = no limit, positive = that many,
        // negative = let the device decide (HistoryBudgetPlanner). Docs/SETTING_GROUPS.md

        /// <summary> Cached level objects the whole history may hold. 0 = no limit, negative =
        /// resolved per device. </summary>
        [RuleMinValue(-1)]
        [JsonProperty(Names.HistoryObjects)]
        public int HistoryObjects { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorSavingsSettings()
        {
            ResetOwn();
        }

        /// <summary> Built from its autosave, rate, autosave files, length and object ceiling. </summary>
        public EditorSavingsSettings(bool autosave, float autosaveRate, int maxAutosaveFiles, int historyLength,
            int historyObjects)
        {
            Autosave = autosave;
            AutosaveRate = autosaveRate;
            MaxAutosaveFiles = maxAutosaveFiles;
            HistoryLength = historyLength;
            HistoryObjects = historyObjects;
        }

        private void ResetOwn()
        {
            Autosave = true;
            AutosaveRate = 60f;
            MaxAutosaveFiles = 25;
            HistoryLength = 512;
            HistoryObjects = -1;
        }
    }
}