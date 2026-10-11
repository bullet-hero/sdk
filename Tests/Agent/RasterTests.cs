using System.Collections.Generic;
using BH.SDK.Agent.Imaging;
using BH.SDK.Models.Resources;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The agent's rasteriser, bitmap font and contact sheet, counted pixel by pixel. </summary>
    [TestFixture]
    public class RasterTests
    {
        private static readonly Pixel White = new(255, 255, 255, 255);
        private static readonly Pixel HalfRed = new(255, 0, 0, 128);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FillTriangle_CoversThePixelCentresInside()
        {
            var raster = new Raster(8, 8);
            raster.FillTriangle(0f, 0f, 8f, 0f, 0f, 8f, White);

            Assert.AreEqual(28, raster.CountPixels(White), "centres on the hypotenuse belong to the far side");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FillTriangle_WindingDoesNotMatter()
        {
            var raster = new Raster(8, 8);
            raster.FillTriangle(0f, 0f, 0f, 8f, 8f, 0f, White);

            Assert.AreEqual(28, raster.CountPixels(White));
        }

        // Shapes arrive triangulated: a diagonal blended twice would draw a darker seam through every quad.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FillTriangle_SharedEdge_IsPaintedOnce()
        {
            var raster = new Raster(8, 8);
            raster.FillTriangle(0f, 0f, 8f, 0f, 0f, 8f, HalfRed);
            raster.FillTriangle(8f, 0f, 8f, 8f, 0f, 8f, HalfRed);

            Assert.AreEqual(64, raster.CountPixels(HalfRed));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FillTriangle_Degenerate_PaintsNothing()
        {
            var raster = new Raster(8, 8);
            raster.FillTriangle(0f, 0f, 4f, 4f, 8f, 8f, White);

            Assert.AreEqual(0, raster.CountPixels(White));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void BlendPixel_IsSourceOver()
        {
            var raster = new Raster(1, 1);
            raster.Clear(new Pixel(0, 0, 255, 255));
            raster.BlendPixel(0, 0, HalfRed);

            var pixel = raster.GetPixel(0, 0);
            Assert.AreEqual(128, pixel.r);
            Assert.AreEqual(0, pixel.g);
            Assert.AreEqual(127, pixel.b);
            Assert.AreEqual(255, pixel.a);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Rects_FillAndOutline_CountTheirPixels()
        {
            var filled = new Raster(8, 8);
            filled.FillRect(1f, 1f, 4f, 3f, White);
            Assert.AreEqual(12, filled.CountPixels(White));

            var outlined = new Raster(8, 8);
            outlined.DrawRect(1f, 1f, 4f, 3f, HalfRed);
            Assert.AreEqual(10, outlined.CountPixels(HalfRed), "a corner blended twice would show here");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DrawLine_IsClippedAndPaintsEachPixelOnce()
        {
            var raster = new Raster(8, 8);
            raster.DrawLine(-100f, -100f, 100f, 100f, HalfRed);

            Assert.AreEqual(8, raster.CountPixels(HalfRed));
            for (var i = 0; i < 8; i++)
                Assert.AreEqual(HalfRed, raster.GetPixel(i, i));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PixelFont_Label_LightsItsGlyphPixels()
        {
            var raster = new Raster(64, 32);
            PixelFont.Draw(raster, "#12", 1, 1, White);
            Assert.AreEqual(44, raster.CountPixels(White));

            var scaled = new Raster(64, 32);
            PixelFont.Draw(scaled, "#12", 1, 1, White, 2);
            Assert.AreEqual(44 * 4, scaled.CountPixels(White));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PixelFont_Measures_AndReadsLowerCaseAsUpper()
        {
            Assert.AreEqual(17, PixelFont.MeasureWidth("#12"));
            Assert.AreEqual(34, PixelFont.MeasureWidth("#12", 2));
            Assert.AreEqual(14, PixelFont.MeasureHeight(2));
            Assert.AreEqual(3, PixelFont.FitCharacters(17));
            Assert.AreEqual(2, PixelFont.FitCharacters(16));

            var upper = new Raster(32, 8);
            PixelFont.Draw(upper, "AZ", 0, 0, White);
            var lower = new Raster(32, 8);
            PixelFont.Draw(lower, "az", 0, 0, White);
            CollectionAssert.AreEqual(upper.Pixels, lower.Pixels);
            Assert.IsTrue(PixelFont.Supports('q'));
            Assert.IsFalse(PixelFont.Supports('?'));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ContactSheet_LaysOutAGridWithLabels()
        {
            var images = new List<Raster>();
            for (var i = 0; i < 5; i++)
            {
                var image = i == 4 ? new Raster(8, 4) : new Raster(10, 6);
                image.Clear(White);
                images.Add(image);
            }
            var background = new Pixel(0, 0, 0, 255);
            var ink = new Pixel(0, 255, 0, 255);

            var sheet = ContactSheet.Compose(images, new[] { "1", "2", "3", "4", "5" }, 3, background, ink);

            // Cells 10x6, label band 4 + 7, padding 4 around and between.
            Assert.AreEqual(3 * 10 + 4 * 4, sheet.Width);
            Assert.AreEqual(2 * (6 + 11) + 3 * 4, sheet.Height);
            Assert.AreEqual(4 * 10 * 6 + 8 * 4, sheet.CountPixels(White));
            Assert.AreEqual(White, sheet.GetPixel(4, 4), "the first image starts at the padding");
            Assert.Greater(sheet.CountPixels(ink), 0);

            var square = ContactSheet.Compose(images, null, 0, background, ink);
            Assert.AreEqual(3 * 10 + 4 * 4, square.Width, "five images pick three columns");
        }
    }
}
