using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Services.Skins;
using BH.SDK.Utils;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That the shipped preset table builds drawable, distinct presets, silhouettes first with eyes.
    /// </summary>
    [TestFixture]
    public class AvatarSkinPresetsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryPreset_IsDrawable_Distinct_AndEyedOnlyWhenASilhouette()
        {
            var presets = AvatarSkinPresets.Build();
            var ids = new HashSet<AvatarSkinId>();
            var geometry = new HashSet<string>();

            Assert.AreEqual(AvatarSkinPresets.Silhouettes.Length + AvatarSkinPresets.Seeds.Length, presets.Count);
            for (var i = 0; i < presets.Count; i++)
            {
                var skin = presets[i];
                var silhouette = i < AvatarSkinPresets.Silhouettes.Length;

                CollectionAssert.IsEmpty(AvatarSkinValidation.Validate(skin), skin.Name);
                Assert.IsTrue(skin.Id.IsBuiltin(), skin.Name);
                Assert.AreNotEqual(AvatarSkinId.Classic, skin.Id, skin.Name);
                Assert.IsTrue(ids.Add(skin.Id), $"{skin.Name}: the same id as an earlier preset");
                Assert.IsTrue(geometry.Add(AvatarSkinGenerator.GeometryKey(skin)), $"{skin.Name}: a duplicate");
                Assert.AreEqual(silhouette, (skin.Elements & AvatarSkinElements.Eyes) != 0, skin.Name);
                Assert.AreEqual(silhouette, skin.Meshes.Count > 0, skin.Name);
            }
        }

        // The player's colours are stretched over every preset, so two presets differing only in colour
        // look the same on screen. The form is the grid, and each cell's shape, turn, mirror and scale.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void NoTwoPresets_ShareAForm_AndNoneIsTheClassicOne()
        {
            var forms = new Dictionary<string, string> { [Form(new AvatarSkin())] = "Classic" };
            var outlines = new HashSet<string>();

            foreach (var (outline, _) in AvatarSkinPresets.Silhouettes)
                Assert.IsTrue(outlines.Add(outline), $"{outline}: one silhouette per outline");

            foreach (var skin in AvatarSkinPresets.Build())
            {
                var form = Form(skin);
                Assert.IsFalse(forms.ContainsKey(form), $"{skin.Name} has the form of {(forms.TryGetValue(form, out var o) ? o : "")}");
                forms[form] = skin.Name;
            }
        }

        private static string Form(AvatarSkin skin)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append(skin.Columns).Append('x').Append(skin.Rows).Append(':').Append(skin.Mask).Append('/');
            var full = AvatarSkinMask.Full(skin.Columns, skin.Rows) == skin.Mask;
            var plain = true;
            for (var i = 0; i < AvatarSkinMask.Count(skin.Mask); i++)
            {
                var part = skin.Parts[skin.Cells.Count == 0 ? 0 : skin.Cells[i]];
                var mesh = skin.Meshes.FindIndex(candidate => candidate.Id == part.Shape);
                plain &= mesh < 0 && part.Shape == ShapeId.Square.Fill && part.Rotation == 0f && part.Scale >= 1f;
                builder.Append(mesh >= 0 ? "mesh" + mesh : part.Shape.value.ToString()).Append(',')
                    .Append(part.Rotation).Append(',').Append(part.Mirror).Append(',').Append(part.Scale).Append(';');
            }

            // A full grid of plain squares is a plain square whatever its size: the classic body.
            return full && plain ? "square" : builder.ToString();
        }
    }
}
