using System;
using System.Collections.Generic;
using BH.SDK.Models.Resources;

namespace BH.SDK.Agent.Imaging
{
    /// <summary> Lays images out in a grid, a <see cref="PixelFont"/> label under each - what turns a run
    /// of captures into one picture. </summary>
    public static class ContactSheet
    {
        /// <summary> Gap around and between cells, and between an image and its label, in pixels. </summary>
        public const int DefaultPadding = 4;

        /// <summary> The sheet. Every cell is as large as the largest image, which sits centred in it; a
        /// label too long for its cell is cut. <paramref name="columns"/> of zero or less picks a near-square
        /// grid. </summary>
        /// <param name="images">The images, in reading order. At least one.</param>
        /// <param name="labels">One label per image; null, short, or holding nulls for no label.</param>
        /// <param name="columns">Cells per row.</param>
        /// <param name="background">The sheet's own colour.</param>
        /// <param name="labelColor">The labels' colour.</param>
        /// <param name="padding">See <see cref="DefaultPadding"/>.</param>
        /// <param name="labelScale">The labels' <see cref="PixelFont"/> scale.</param>
        public static Raster Compose(IReadOnlyList<Raster> images, IReadOnlyList<string> labels, int columns,
            Pixel background, Pixel labelColor, int padding = DefaultPadding, int labelScale = 1)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (images.Count == 0) throw new ArgumentException("A contact sheet needs at least one image.", nameof(images));
            if (padding < 0) throw new ArgumentOutOfRangeException(nameof(padding), padding, "Must not be negative.");

            var count = images.Count;
            if (columns <= 0) columns = (int)Math.Ceiling(Math.Sqrt(count));
            columns = Math.Min(columns, count);
            var rows = (count + columns - 1) / columns;

            int cellWidth = 0, cellHeight = 0;
            for (var i = 0; i < count; i++)
            {
                var image = images[i] ?? throw new ArgumentException($"Image {i} is null.", nameof(images));
                cellWidth = Math.Max(cellWidth, image.Width);
                cellHeight = Math.Max(cellHeight, image.Height);
            }

            var labelHeight = padding + PixelFont.MeasureHeight(labelScale);
            var sheet = new Raster(
                checked(columns * cellWidth + (columns + 1) * padding),
                checked(rows * (cellHeight + labelHeight) + (rows + 1) * padding));
            sheet.Clear(background);

            var fit = PixelFont.FitCharacters(cellWidth, labelScale);
            for (var i = 0; i < count; i++)
            {
                var image = images[i];
                var cellX = padding + i % columns * (cellWidth + padding);
                var cellY = padding + i / columns * (cellHeight + labelHeight + padding);
                sheet.Blit(image, cellX + (cellWidth - image.Width) / 2, cellY + (cellHeight - image.Height) / 2);

                var label = labels != null && i < labels.Count ? labels[i] : null;
                if (string.IsNullOrEmpty(label)) continue;
                if (label.Length > fit) label = label.Substring(0, fit);

                var labelX = cellX + (cellWidth - PixelFont.MeasureWidth(label, labelScale)) / 2;
                PixelFont.Draw(sheet, label, labelX, cellY + cellHeight + padding, labelColor, labelScale);
            }

            return sheet;
        }
    }
}
