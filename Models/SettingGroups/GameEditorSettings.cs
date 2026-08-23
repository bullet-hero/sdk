using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// Preferences for the in-game level editor, per device: autosave policy, camera limits, how the
    /// preview player starts and which wire format the editor writes with. Belongs to the person
    /// editing, never to the level being edited.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(GameEditorSettings.CameraMinSize), nameof(GameEditorSettings.CameraMaxSize))]
    public class GameEditorSettings : IModel<GameEditorSettings>, IMoveable<GameEditorSettings>
    {
        // Savings

        /// <summary> Whether the editor saves on its own. </summary>
        [JsonProperty(Names.Autosave)]
        public bool Autosave { get; set; }

        /// <summary> Seconds between autosaves. </summary>
        [RuleMinValue(1f)]
        [JsonProperty(Names.AutosaveRate)]
        public float AutosaveRate { get; set; }

        /// <summary> How many autosaves are kept before the oldest is dropped - the depth of the
        /// safety net, traded against disk space. </summary>
        [RuleInRange(1, 1000)]
        [JsonProperty(Names.MaxAutosaveFiles)]
        public int MaxAutosaveFiles { get; set; }

        // Editor Camera

        /// <summary> Closest the editor camera may zoom in. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.CameraMinSize)]
        public float CameraMinSize { get; set; }

        /// <summary> Furthest the editor camera may zoom out. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.CameraMaxSize)]
        public float CameraMaxSize { get; set; }

        // Preview player

        // The preview player's toggle is also what decides who owns the viewport's touches, so its
        // starting state is a real preference rather than a constant: a desktop author wants the player
        // there from the first frame (a mouse loses nothing to it), a phone author does not, since the
        // whole viewport goes to the avatar the moment it exists. The platform only picks the value a
        // FRESH settings file is born with - after that it is the author's own.

        /// <summary> Whether the editor's preview player starts switched on. </summary>
        [JsonProperty(Names.PlayerActiveDefault)]
        public bool PlayerActiveDefault { get; set; }

        /// <summary> Whether switching the preview player on drops the gizmo mode back to None. </summary>
        [JsonProperty(Names.GizmosResetOnPlayer)]
        public bool GizmosResetOnPlayer { get; set; }

        // Viewport grid

        // The grid's own visibility is NOT here, and that is the split: whether the lines are drawn
        // right now is the current view, like the active gizmo, and lives in the session
        // (GridModeService). How BIG a cell is describes how the author works - a level authored on
        // a half-unit grid stays authored on one across sessions - so only that is remembered.

        /// <summary> Side of one cell of the editor's viewport grid, in world units. </summary>
        [RuleMinValue(ValueRules.MinGridSize)]
        [JsonProperty(Names.GridSize)]
        public float GridSize { get; set; }

        // Alpha only, and deliberately the ONLY thing authored about the grid's colour: the hue is
        // the inverse of whatever the camera is showing on the current frame, so the one decision
        // left is how far the lines fade into it. The camera's OWN alpha never takes part - a level
        // fading its background out would otherwise take the grid with it, and a guide that
        // disappears while the content it guides is still on screen is worse than no guide.

        /// <summary> Opacity of the editor viewport grid's lines. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.GridOpacity)]
        public float GridOpacity { get; set; }

        // Selection

        // Whether multi-selection is a MODIFIER or a MODE is a real preference, not a constant: on a
        // desktop the modifier is the familiar answer and an ordinary click should still replace the
        // selection, while on touch there is no key to hold, so the mode has to apply on its own.
        // Turning this off makes every click additive for as long as the mode is on.
        //
        // A mode entered by LONG PRESS ignores this either way, and has to: a finger has no modifier
        // to hold, so gating it there would make the touch entry point reach a mode that never applies.

        /// <summary> Whether multi-selection applies only while the modifier is held down. </summary>
        [JsonProperty(Names.MultiSelectRequiresHold)]
        public bool MultiSelectRequiresHold { get; set; }

        // Off by default, unlike almost everything else here, and the asymmetry is the point: the fill
        // covers the object an author has just selected and is about to work on, so it is in the way
        // far more often than it answers a question. The global collider view (a session toggle, not a
        // preference) is what an author reaches for when the question is actually about hitboxes; this
        // one exists for the case where they want the answer without leaving that view on.

        /// <summary> Whether selecting an object draws its collider. </summary>
        [JsonProperty(Names.PreviewColliderOnSelect)]
        public bool PreviewColliderOnSelect { get; set; }

        // Off by default, which is the picker's own answer rather than a taste: a shape is clicked by
        // what it DRAWS (or by its collider), and the empty padding around a slice or a ring belongs
        // to whatever sits behind it. Turning this on gives every object its whole rect back, for an
        // author who would rather have a generous target than an exact one. Either way an object
        // carrying no geometry at all is picked by its rect - it has nothing else to be clicked by.

        /// <summary> Whether a click picks an object by its whole rect instead of its own shape. </summary>
        [JsonProperty(Names.PickInvisibleAABB)]
        public bool PickInvisibleAABB { get; set; }

        // Frame hierarchy

        // Off by default, and for the same shape of reason as the two above: inframes are what an
        // effect SPAWNS while it plays - engine-owned rows that cannot be selected, edited or
        // addressed, appearing and vanishing on their own as the playhead moves. That is a diagnostic
        // view of the simulation rather than the content the tree exists to navigate, so it is the
        // author who asks for it. Added after the domain reached 1.0 and deliberately does NOT bump
        // it: a settings.json written before this property existed simply has no key for it, and
        // Newtonsoft leaves the constructor's default in place - see UserSettings.Interface.

        /// <summary> Whether the editor's frame hierarchy lists the objects effects spawn at runtime. </summary>
        [JsonProperty(Names.RenderInframes)]
        public bool RenderInframes { get; set; }

        // Serialization

        // Which wire format the editor WRITES with, split by what is being written rather than kept as
        // one switch: a level is the thing an author hands to somebody else, a library resource is
        // reused across levels, and a clipboard payload leaves the process entirely - three different
        // trade-offs between size and being readable by hand. None of the three describes how anything
        // is READ, which is always resolved from the file itself (PathUtils.FindDataFile), so changing
        // one of these can never make existing content unreadable.

        /// <summary> Format new levels are created with - level.* and metadata.* alike. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.LevelSerializeMode)]
        public SerializationType LevelSerializeMode { get; set; }

        /// <summary> Format every resource exported to the device library is written with. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.ResourcesSerializeMode)]
        public SerializationType ResourcesSerializeMode { get; set; }

        /// <summary> Format a copied selection is serialized with for the clipboard. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.CopySerializeMode)]
        public SerializationType CopySerializeMode { get; set; }

        public GameEditorSettings()
        {
            ResetOwn();
        }
        public GameEditorSettings(bool autosave, float autosaveRate, int maxAutosaveFiles,
            float cameraMinSize, float cameraMaxSize, bool playerActiveDefault, bool gizmosResetOnPlayer,
            bool multiSelectRequiresHold, bool previewColliderOnSelect, bool pickInvisibleAABB,
            bool renderInframes, float gridSize, float gridOpacity,
            SerializationType levelSerializeMode,
            SerializationType resourcesSerializeMode, SerializationType copySerializeMode)
        {
            Autosave = autosave;
            AutosaveRate = autosaveRate;
            MaxAutosaveFiles = maxAutosaveFiles;
            CameraMinSize = cameraMinSize;
            CameraMaxSize = cameraMaxSize;
            PlayerActiveDefault = playerActiveDefault;
            GizmosResetOnPlayer = gizmosResetOnPlayer;
            MultiSelectRequiresHold = multiSelectRequiresHold;
            PreviewColliderOnSelect = previewColliderOnSelect;
            PickInvisibleAABB = pickInvisibleAABB;
            RenderInframes = renderInframes;
            GridSize = gridSize;
            GridOpacity = gridOpacity;
            LevelSerializeMode = levelSerializeMode;
            ResourcesSerializeMode = resourcesSerializeMode;
            CopySerializeMode = copySerializeMode;
        }
        public void Reset()
        {
            ResetOwn();
        }
        private void ResetOwn()
        {
            Autosave = true;
            AutosaveRate = 60f;
            MaxAutosaveFiles = 25;
            CameraMinSize = 0.1f;
            CameraMaxSize = 100f;
            PlayerActiveDefault = true;
            GizmosResetOnPlayer = true;
            MultiSelectRequiresHold = true;
            PreviewColliderOnSelect = false;
            PickInvisibleAABB = false;
            RenderInframes = false;
            GridSize = 1f;
            GridOpacity = 0.25f;
            LevelSerializeMode = SerializationType.Json;
            ResourcesSerializeMode = SerializationType.Json;
            CopySerializeMode = SerializationType.Json;
        }

        public object Clone() => Copy();
        public GameEditorSettings Copy() => new(Autosave, AutosaveRate, MaxAutosaveFiles, CameraMinSize,
            CameraMaxSize, PlayerActiveDefault, GizmosResetOnPlayer, MultiSelectRequiresHold,
            PreviewColliderOnSelect, PickInvisibleAABB, RenderInframes, GridSize, GridOpacity,
            LevelSerializeMode, ResourcesSerializeMode, CopySerializeMode);

        public void Pull(GameEditorSettings source)
        {
            Autosave = source.Autosave;
            AutosaveRate = source.AutosaveRate;
            MaxAutosaveFiles = source.MaxAutosaveFiles;
            CameraMinSize = source.CameraMinSize;
            CameraMaxSize = source.CameraMaxSize;
            PlayerActiveDefault = source.PlayerActiveDefault;
            GizmosResetOnPlayer = source.GizmosResetOnPlayer;
            MultiSelectRequiresHold = source.MultiSelectRequiresHold;
            PreviewColliderOnSelect = source.PreviewColliderOnSelect;
            PickInvisibleAABB = source.PickInvisibleAABB;
            RenderInframes = source.RenderInframes;
            GridSize = source.GridSize;
            GridOpacity = source.GridOpacity;
            LevelSerializeMode = source.LevelSerializeMode;
            ResourcesSerializeMode = source.ResourcesSerializeMode;
            CopySerializeMode = source.CopySerializeMode;
        }

        public override bool Equals(object obj) => obj is GameEditorSettings value && Equals(value);

        // HashCode.Combine takes at most 8 values, and this class holds 16 - the tail folds into the
        // eighth slot rather than being dropped, twice over.
        public override int GetHashCode() => HashCode.Combine(Autosave, AutosaveRate, MaxAutosaveFiles,
            CameraMinSize, CameraMaxSize, PlayerActiveDefault, GizmosResetOnPlayer,
            HashCode.Combine(MultiSelectRequiresHold, PreviewColliderOnSelect, PickInvisibleAABB,
                GridSize, GridOpacity, LevelSerializeMode, ResourcesSerializeMode,
                HashCode.Combine(CopySerializeMode, RenderInframes)));

        public bool Equals(GameEditorSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Autosave == other.Autosave
                   && AutosaveRate.Equals(other.AutosaveRate)
                   && MaxAutosaveFiles == other.MaxAutosaveFiles
                   && CameraMinSize.Equals(other.CameraMinSize)
                   && CameraMaxSize.Equals(other.CameraMaxSize)
                   && PlayerActiveDefault == other.PlayerActiveDefault
                   && GizmosResetOnPlayer == other.GizmosResetOnPlayer
                   && MultiSelectRequiresHold == other.MultiSelectRequiresHold
                   && PreviewColliderOnSelect == other.PreviewColliderOnSelect
                   && PickInvisibleAABB == other.PickInvisibleAABB
                   && RenderInframes == other.RenderInframes
                   && GridSize.Equals(other.GridSize)
                   && GridOpacity.Equals(other.GridOpacity)
                   && LevelSerializeMode == other.LevelSerializeMode
                   && ResourcesSerializeMode == other.ResourcesSerializeMode
                   && CopySerializeMode == other.CopySerializeMode;
        }
    }
}