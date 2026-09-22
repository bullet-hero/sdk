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

        // THE SECOND CEILING, AND IT IS NOT THE SAME QUESTION. HistoryLength counts nodes, and a node
        // costs whatever its operation cached - deleting a selection of fifty thousand objects is ONE
        // node holding fifty thousand of them. So a history obeying its node budget exactly can still
        // be the largest thing in the process, and this is what stops that. It is a guard against
        // outright overload, not a tuning knob: at its resolved value an ordinary session of 512
        // operations never reaches it, and what it catches is a run of mass edits on a huge level.
        //
        // THREE VALUES, AND THE NEGATIVE ONE IS WHY THIS IS AN INT RATHER THAN A PAIR. Zero is no
        // limit at all; a positive number is that many objects; NEGATIVE means "let the device
        // decide", which is the default and resolves to no limit on a desktop and to a real ceiling
        // on a phone - see Core's HistoryBudgetPlanner. That is the shape TexturesGraphicsSettings
        // and AudioLoadPlanner already use ("Auto, resolved per platform") rather than a per-platform
        // value SEEDED into a fresh file, which this project tried once for the editor's preview
        // avatar and removed: a seeded default is indistinguishable from a choice the author made,
        // so it can never be revised.

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