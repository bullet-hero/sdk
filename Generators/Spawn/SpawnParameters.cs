using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;

namespace BH.SDK.Generators.Spawn
{
    // Shared object-template fields, inherited by reflection. Hints must still name inherited fields, so
    // the two arrays are spliced into each generator's Section calls: Main (what is drawn, paint,
    // collider) and Additional (size, tint). Docs/GENERATORS.md.

    /// <summary>
    /// The object template shared by every generator that spawns objects: what shape is drawn, what
    /// is painted on it, how big, what colour, and what (if anything) it collides with.
    /// </summary>
    public class SpawnParameters
    {
        // Defaulting Shape to Square rather than to Collider's value: a generator whose objects do
        // not collide must still draw something, and a Null shape draws nothing at all - a run that
        // silently produces invisible objects reads as the generator being broken.

        /// <summary> Shape every spawned object draws. </summary>
        public ShapeId Shape = ShapeId.Square.Fill;

        /// <summary> Image painted onto that shape, Null for none - the same default a hand-created
        /// ShapeObject gets, since the shape itself is the silhouette now. </summary>
        public TextureResourceId Texture = TextureResourceId.Null;

        /// <summary> Collision shape, or Null for pure decoration. </summary>
        public ShapeId Collider = ShapeId.Null;

        /// <summary> Size of one object. Polymorphic on purpose - RandomRect here is what makes a
        /// field of bullets look hand-placed instead of stamped. </summary>
        public IVector2 Size = new Vector2Value(1f, 1f);

        /// <summary> Tint of one object; themeable, so a generated pattern follows the level's
        /// palette instead of freezing a literal colour. </summary>
        public IColor4 Color = new Color4Value(1f, 1f, 1f, 1f);

        /// <summary> The inherited resources - splice into each generator's own Main section. </summary>
        public static readonly string[] MainFields =
        {
            nameof(Shape), nameof(Texture), nameof(Collider),
        };

        /// <summary> The inherited look-of-it fields - splice into each generator's own Additional
        /// section. </summary>
        public static readonly string[] AdditionalFields =
        {
            nameof(Size), nameof(Color),
        };
    }

    /// <summary> The schema descriptions of SpawnParameters' shared fields, written once for every
    /// spawning generator. A generator using a field differently describes it again afterwards. </summary>
    public static class SpawnParameterHints
    {
        /// <summary> Describes Shape, Texture, Collider, Size and Color. </summary>
        public static GeneratorHints.Builder DescribeSpawnFields(this GeneratorHints.Builder builder)
            => builder
                .Describe(nameof(SpawnParameters.Shape),
                    "Shape every object draws: a catalog name like \"Square\" or \"Circle\", or a shape " +
                    "GUID. Default is a filled square. Drawing only - it never hurts the player, see " +
                    "Collider.")
                .Describe(nameof(SpawnParameters.Texture),
                    "Texture resource id painted onto the shape. 0 (the default) paints none, so the shape " +
                    "shows its plain Color.")
                .Describe(nameof(SpawnParameters.Collider),
                    "What hurts the player. Empty (the default) makes a decoration that can never hurt; " +
                    "pass a shape for a hazard: a catalog name like \"Circle\" or a shape GUID.")
                .Describe(nameof(SpawnParameters.Size),
                    "Width and height of each object in world units; the default camera view is 10 units " +
                    "tall. Default 1 x 1.")
                .Describe(nameof(SpawnParameters.Color),
                    "Tint of each object: \"#RRGGBBAA\" (alpha 00 is invisible), or {\"theme\": index} to " +
                    "follow the level's theme palette. Default opaque white.");
    }
}