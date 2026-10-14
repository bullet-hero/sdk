using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Services.Shapes;

namespace BH.SDK.Services.Skins
{
    /// <summary> The shapes a skin part can draw: a catalogue shape by id, or one of the skin's own meshes.
    /// Also the name lookup the preset generator builds from. </summary>
    public static class AvatarSkinShapes
    {
        private static readonly object Gate = new();
        private static Dictionary<string, ShapeId> _byName;
        private static readonly Dictionary<ShapeId, CompositeShape> Geometries = new();

        /// <summary> Whether <paramref name="shapeId"/> is a catalogue shape. </summary>
        public static bool IsCatalogue(ShapeId shapeId) =>
            shapeId.IsEnabled() && ShapeCatalogService.TryDecode(shapeId, out var parameters) &&
            !ShapeCatalogService.IsDegenerate(parameters);

        /// <summary> The id of the catalogue shape called <paramref name="name"/>; case-sensitive. </summary>
        public static bool TryGet(string name, out ShapeId shapeId)
        {
            shapeId = ShapeId.Null;
            if (string.IsNullOrEmpty(name)) return false;

            BuildNames();
            return _byName.TryGetValue(name, out shapeId);
        }

        /// <summary> The mesh <paramref name="shapeId"/> names in <paramref name="skin"/>: one of its
        /// own, else the catalogue's, built once. Null when it names neither. Shared - never modify it.
        /// </summary>
        public static IShapeGeometry Geometry(AvatarSkin skin, ShapeId shapeId)
        {
            if (skin?.Meshes != null)
                foreach (var mesh in skin.Meshes)
                    if (mesh != null && mesh.Id == shapeId)
                        return mesh;

            if (!IsCatalogue(shapeId)) return null;
            lock (Gate)
            {
                if (!Geometries.TryGetValue(shapeId, out var shape))
                {
                    ShapeCatalogService.TryDecode(shapeId, out var parameters);
                    shape = ShapeCatalogService.Build(parameters);
                    Geometries[shapeId] = shape;
                }

                return shape;
            }
        }

        private static void BuildNames()
        {
            if (_byName != null) return;

            lock (Gate)
            {
                if (_byName != null) return;

                var byName = new Dictionary<string, ShapeId>(StringComparer.Ordinal);
                foreach (var parameters in ShapeCatalogService.EnumerateCatalog())
                    byName[ShapeCatalogService.GetName(parameters)] = ShapeCatalogService.ToShapeId(parameters);
                _byName = byName;
            }
        }
    }
}