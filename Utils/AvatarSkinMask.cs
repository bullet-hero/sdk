using System.Collections.Generic;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // The row stride is AvatarSkinRules.MaxGridSide whatever the grid's width, so a cell keeps its bit when
    // the grid grows or shrinks.

    /// <summary> The bit mask of an avatar skin's filled cells: bit <c>row * 8 + column</c>, row 0 at the
    /// bottom. </summary>
    public static class AvatarSkinMask
    {
        private const int Stride = AvatarSkinRules.MaxGridSide;

        /// <summary> The bit of one cell. </summary>
        public static ulong Bit(int column, int row) => 1UL << (row * Stride + column);

        /// <summary> Whether the cell is filled. </summary>
        public static bool Has(ulong mask, int column, int row) => (mask & Bit(column, row)) != 0;

        /// <summary> Every cell of a <paramref name="columns"/> by <paramref name="rows"/> grid filled.
        /// </summary>
        public static ulong Full(int columns, int rows)
        {
            var mask = 0UL;
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                mask |= Bit(column, row);
            return mask;
        }

        /// <summary> How many cells are filled. </summary>
        public static int Count(ulong mask)
        {
            var count = 0;
            for (; mask != 0; mask &= mask - 1) count++;
            return count;
        }

        /// <summary> Where in ascending bit order the filled cell sits - its index into
        /// <c>AvatarSkin.Cells</c>; -1 when the cell is empty. </summary>
        public static int RankOf(ulong mask, int column, int row)
        {
            var bit = Bit(column, row);
            return (mask & bit) == 0 ? -1 : Count(mask & (bit - 1));
        }
        /// <summary> How many filled cells the first filled one reaches edge to edge, inside a
        /// <paramref name="columns"/> by <paramref name="rows"/> grid: equal to <see cref="Count"/> exactly when
        /// the body is one piece. </summary>
        public static int CountConnected(ulong mask, int columns, int rows)
        {
            var seen = 0UL;
            var stack = new Stack<(int Column, int Row)>();

            for (var row = 0; row < rows && stack.Count == 0; row++)
            for (var column = 0; column < columns; column++)
            {
                if (!Has(mask, column, row)) continue;
                stack.Push((column, row));
                seen |= Bit(column, row);
                break;
            }

            var count = 0;
            while (stack.Count > 0)
            {
                var (column, row) = stack.Pop();
                count++;
                Visit(column - 1, row);
                Visit(column + 1, row);
                Visit(column, row - 1);
                Visit(column, row + 1);
            }

            return count;

            void Visit(int column, int row)
            {
                if (row < 0 || row >= rows || column < 0 || column >= columns) return;
                var bit = Bit(column, row);
                if ((mask & bit) == 0 || (seen & bit) != 0) return;
                seen |= bit;
                stack.Push((column, row));
            }
        }
    }
}
