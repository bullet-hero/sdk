using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Services.Skins;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // The messages are diagnostics for whoever wrote the JSON and are shown as they are, not localized.
    // Columns and rows count from 0 as the mask does, row 0 at the bottom.

    /// <summary> What makes an avatar skin drawable, beyond what its attributes clamp: the id, the mask
    /// inside its grid, one connected body, every cell's part, every part's shape and colour, the meshes.
    /// </summary>
    public static class AvatarSkinValidation
    {
        /// <summary> Every problem with <paramref name="skin"/>; empty when it is drawable. </summary>
        public static IReadOnlyList<string> Validate(AvatarSkin skin)
        {
            var errors = new List<string>();
            if (skin == null)
            {
                errors.Add("The skin is empty.");
                return errors;
            }

            if (!skin.Id.IsEnabled()) errors.Add("'id' is missing.");

            ValidateGrid(skin, errors);
            var meshIds = ValidateMeshes(skin, errors);
            ValidateParts(skin, meshIds, errors);

            if (skin.Colors == null || skin.Colors.Count == 0 || skin.Colors.Count > AvatarSkinRules.MaxColors)
                errors.Add($"'colors' must hold 1-{AvatarSkinRules.MaxColors} colours.");
            else
                for (var i = 0; i < skin.Colors.Count; i++)
                    if (skin.Colors[i] == null)
                        errors.Add($"Colour {i} is missing.");

            return errors;
        }

        private static void ValidateGrid(AvatarSkin skin, List<string> errors)
        {
            int columns = skin.Columns, rows = skin.Rows;
            if (columns < AvatarSkinRules.MinGridSide || columns > AvatarSkinRules.MaxGridSide ||
                rows < AvatarSkinRules.MinGridSide || rows > AvatarSkinRules.MaxGridSide)
            {
                errors.Add(
                    $"'columns' and 'rows' must be {AvatarSkinRules.MinGridSide}-{AvatarSkinRules.MaxGridSide}.");
                return;
            }

            if ((skin.Mask & ~AvatarSkinMask.Full(columns, rows)) != 0)
            {
                errors.Add($"'mask' fills cells outside the {columns}x{rows} grid.");
                return;
            }

            var filled = AvatarSkinMask.Count(skin.Mask);
            if (filled < AvatarSkinRules.MinFilledCells)
            {
                errors.Add($"At least {AvatarSkinRules.MinFilledCells} cells must be filled, {filled} are.");
                return;
            }

            if (AvatarSkinMask.CountConnected(skin.Mask, columns, rows) != filled)
                errors.Add("The filled cells must form one piece, joined edge to edge.");

            var cells = skin.Cells;
            if (cells == null)
            {
                errors.Add("'cells' is missing.");
                return;
            }

            if (cells.Count != 0 && cells.Count != filled)
            {
                errors.Add($"'cells' must be empty or hold one part per filled cell: {filled}, not {cells.Count}.");
                return;
            }

            var partCount = skin.Parts?.Count ?? 0;
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i] >= 0 && cells[i] < partCount) continue;
                errors.Add($"Cell {i} names part {cells[i]}, 'parts' has {partCount}.");
                return;
            }
        }

        private static HashSet<ShapeId> ValidateMeshes(AvatarSkin skin, List<string> errors)
        {
            var ids = new HashSet<ShapeId>();
            if (skin.Meshes == null)
            {
                errors.Add("'meshes' is missing.");
                return ids;
            }

            if (skin.Meshes.Count > AvatarSkinRules.MaxMeshes)
                errors.Add($"'meshes' may hold at most {AvatarSkinRules.MaxMeshes}.");

            for (var i = 0; i < skin.Meshes.Count; i++)
            {
                var mesh = skin.Meshes[i];
                if (mesh == null)
                {
                    errors.Add($"Mesh {i} is missing.");
                    continue;
                }

                if (!mesh.Id.IsEnabled() || AvatarSkinShapes.IsCatalogue(mesh.Id))
                    errors.Add($"Mesh {i} needs an 'id' of its own, not empty and not a catalogue shape's.");
                else if (!ids.Add(mesh.Id))
                    errors.Add($"Mesh {i} has the same 'id' as an earlier one.");

                if (!ShapeGeometryUtils.Analyze(mesh.Vertices, mesh.Indices).IsClean)
                    errors.Add($"Mesh {i} is not a clean set of triangles inside its box.");
            }

            return ids;
        }

        private static void ValidateParts(AvatarSkin skin, HashSet<ShapeId> meshIds, List<string> errors)
        {
            if (skin.Parts == null || skin.Parts.Count == 0)
            {
                errors.Add("'parts' must hold at least one part.");
                return;
            }

            if (skin.Parts.Count > AvatarSkinRules.MaxParts)
                errors.Add($"'parts' may hold at most {AvatarSkinRules.MaxParts}.");

            var colorCount = skin.Colors?.Count ?? 0;
            for (var i = 0; i < skin.Parts.Count; i++)
            {
                var part = skin.Parts[i];
                if (part == null)
                {
                    errors.Add($"Part {i} is missing.");
                    continue;
                }

                if (!meshIds.Contains(part.Shape) && !AvatarSkinShapes.IsCatalogue(part.Shape))
                    errors.Add(
                        $"Part {i}: shape {part.Shape.value} is neither a catalogue shape nor a mesh of this skin.");

                if (part.Color < 0 || part.Color >= colorCount)
                    errors.Add($"Part {i}: colour {part.Color} does not exist ('colors' has {colorCount}).");
            }
        }
    }
}