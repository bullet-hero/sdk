using BH.SDK.Models;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.GameEditor;

namespace BH.SDK.Versions.V1_0.Migrations
{
    // ReSharper disable once InconsistentNaming

    // Every field of the old flat GameEditorSettings has a home in one of the nine groups, so nothing
    // is dropped and nothing is invented. The properties the restructure ADDED are the ones this
    // migrator says nothing about - each new group's own constructor has already run by the time its
    // setters are assigned, so an upgraded file carries the shipped defaults for them, which is
    // exactly what a settings file that predates a setting should carry.
    //
    // A null group is a file that never had that key (every group before GameEditor was additive at
    // some point), and the current constructor's instance is what stays in place for it - hence the
    // `?? new()`-free shape: the target UserSettings is constructed first, then only the groups the
    // snapshot actually holds are pulled over it.

    public class UserSettingsV1_0ToV2_0 : DataMigration<UserSettingsV1_0, UserSettings>
    {
        public override UserSettings Migrate(UserSettingsV1_0 from)
        {
            var settings = new UserSettings();

            if (from.General != null) settings.General.Pull(from.General);
            if (from.Controls != null) settings.Controls.Pull(from.Controls);
            if (from.Audio != null) settings.Audio.Pull(from.Audio);
            if (from.Graphics != null) settings.Graphics.Pull(from.Graphics);
            if (from.Interface != null) settings.Interface.Pull(from.Interface);
            if (from.Keybindings != null) settings.Keybindings.Pull(from.Keybindings);
            if (from.GameEditor != null) MigrateGameEditor(from.GameEditor, settings.GameEditor);

            return settings;
        }

        private static void MigrateGameEditor(GameEditorSettingsV1_0 from, GameEditorSettings to)
        {
            to.Savings.Autosave = from.Autosave;
            to.Savings.AutosaveRate = from.AutosaveRate;
            to.Savings.MaxAutosaveFiles = from.MaxAutosaveFiles;

            to.Camera.MinSize = from.CameraMinSize;
            to.Camera.MaxSize = from.CameraMaxSize;

            to.Player.ActiveDefault = from.PlayerActiveDefault;
            to.Player.ResetGizmos = from.GizmosResetOnPlayer;

            to.Grid.Size = from.GridSize;
            to.Grid.Opacity = from.GridOpacity;

            to.Selection.MultiRequiresHold = from.MultiSelectRequiresHold;
            to.Selection.PreviewColliderOnSelect = from.PreviewColliderOnSelect;
            to.Selection.PickInvisibleAABB = from.PickInvisibleAABB;

            to.Interface.RenderInframes = from.RenderInframes;

            to.Serialization.LevelMode = from.LevelSerializeMode;
            to.Serialization.ResourcesMode = from.ResourcesSerializeMode;

            // CopySerializeMode is DROPPED rather than carried over: the clipboard is text, so the
            // setting it fed could never mean anything. The snapshot keeps the property because a
            // v1.0 file on disk still has the key - a snapshot records what WAS written.
        }
    }
}
