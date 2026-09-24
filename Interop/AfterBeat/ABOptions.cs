using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat
{
    /// <summary>
    /// The handful of choices a conversion cannot make on its own. Every default here is the one
    /// that changes the level least.
    /// </summary>
    public class ABOptions
    {
        /// <summary> What a fresh <see cref="Models.SettingGroups.LevelSettings"/> runs at.
        /// FrameRules has no constant for it - the number lives in that class's own constructor. </summary>
        public const int DefaultFramerate = 60;

        /// <summary> Frames per second the imported level runs at. Afterbeat stores seconds, so
        /// this decides how finely every time in the file is resolved. </summary>
        public int Framerate = DefaultFramerate;

        /// <summary> Turn the five parallax layers into ordinary collider-less objects. Off means
        /// the level's whole background is dropped. </summary>
        public bool ImportParallax = true;

        // The background is the one part of an imported level that is always on screen and never
        // authored: it spans the whole timeline, it loops by itself, and its keyframes are BAKED,
        // so it is also the part an author is most likely to want out of the way while working on
        // the content. Importing it inactive keeps the objects - the bake is expensive to redo and
        // impossible to recover once dropped - while leaving the level looking like its content
        // alone. Off by default for that reason, which is the one place in this class where the
        // default is not the one that changes the level least.

        /// <summary> Whether imported parallax objects arrive ACTIVE. Inactive ones are stored, kept
        /// and editable, and draw nothing until the author ticks them on. </summary>
        public bool ParallaxActive;

        /// <summary> Read prefabs and their placements. Off flattens nothing - the placements are
        /// simply not created, and the templates not stored. </summary>
        public bool ImportPrefabs = true;

        // This number is the CONVERTER's, not the format's. It was LevelRules.MaxObjectKeys, which
        // read as "as many as a track may hold" - but that ceiling is about storage and this is
        // about how much of a background's loop is worth baking, so raising the one silently
        // multiplied the other. A parallax loop is a repeating cycle: past a few dozen keyframes
        // the extra cycles are indistinguishable and only cost load time.

        /// <summary> The most keyframes a baked parallax loop may spend, and the default. More
        /// cycles than this and the loop is truncated rather than thinned, since a thinned loop
        /// drifts out of phase with the rest of the background. </summary>
        public const int MaxParallaxLoopKeysCap = 32;

        /// <summary> How many keyframes a baked parallax loop may spend, up to
        /// <see cref="MaxParallaxLoopKeysCap"/>. </summary>
        public int MaxParallaxLoopKeys = MaxParallaxLoopKeysCap;

        /// <summary> Name imported objects after their Afterbeat name, falling back to their source
        /// id. Off leaves every name empty, which is what an author gets from this editor's own
        /// create. </summary>
        public bool KeepObjectNames = true;

        // A gradient corner landing between the ramp's two ends is a BLEND of two theme colours,
        // and no theme reference expresses a blend - so a rotated or short ramp can be carried
        // either by its angle or by its theme, never by both. Neither answer is right for every
        // level, which is why this is a switch rather than a decision made in ABGradientMap.
        //
        // On is the default because reproducing how the source level LOOKS is what an importer is
        // for. Off is worth reaching for on a level built around theme switching, where a frozen
        // colour is the more visible loss of the two.

        /// <summary> Let a gradient corner become a literal colour when the ramp does not land it
        /// exactly on one of its two ends. Off snaps such a corner to its nearer end instead: a
        /// hard edge in place of a blend, with every theme reference kept alive. </summary>
        public bool BakeGradientCorners = true;

        // Afterbeat's DEPTH is what orders a level over there and it is mostly left at its default,
        // so a converted level's objects share few layers - which reads as several thousand clips
        // stacked into a handful of timeline rows. Spending draw order on the source EDITOR's own
        // grouping is what this fixed once and paid for with the range (a real level over 900
        // layers, reaching -520); the row count is the editor's problem to solve by grouping, not
        // draw order's. Auto is depth alone, packed - see ABLayerMap. Packed, the default, keeps
        // depth as the ORDER and spends the rows on time instead: objects that never overlap share
        // a row, the way a level authored here is laid out - see ABLayoutPacker. The other three
        // modes are there for an author who wants the source editor's organisation expressed as
        // layers anyway, and they are the ones that can run out of range.

        /// <summary> What the converted level's draw order is derived from. See
        /// <see cref="ABLayerImport"/> - this changes what draws in front of what, so it is
        /// the one import choice worth showing an author first. </summary>
        public ABLayerImport LayerImport = ABLayerImport.Packed;

        // The four switches below reshape the level's STRUCTURE before anything is layered: they
        // decide which objects end up behind a prefab placement, and the packer needs the final set
        // of entries to lay out. The default on each is the one that recovers what the source author
        // actually built - an expanded instance was a placement once, and a nested placement is
        // content the source game draws - while the two that INVENT structure (finding repeats,
        // merging look-alikes) stay opt-in. See Import/ABPrefabExtractor.

        /// <summary> Turn objects Afterbeat's editor expanded out of a placement (pre_iid) back
        /// into a placement of their template, or of a forked copy of it when they were edited
        /// after expanding. </summary>
        public bool RestoreExpandedInstances = true;

        /// <summary> Find root subtrees that repeat identically apart from when and where they
        /// start, and turn each group into a template plus placements. </summary>
        public bool ExtractRepeatedSubtrees;

        /// <summary> Merge templates whose contents are identical, repointing every placement of
        /// the duplicates at the survivor. </summary>
        public bool MergeDuplicateTemplates;

        /// <summary> Inline a template's own placements into it instead of dropping them. This
        /// converter never produces a nested prefab, so off means the nested content is lost. </summary>
        public bool FlattenNestedPrefabs = true;

        /// <summary> Under <see cref="ABLayerImport.Packed"/>: put everything that can hurt the
        /// player at layer 1 and up and everything that cannot at 0 and down - this project's own
        /// convention. Off, the default, keeps Afterbeat's bands, where every ordinary object draws
        /// behind the player whether it hits or not. </summary>
        public bool CollidersAbovePlayer;

        /// <summary> How many layers one source editor group is given, under
        /// <see cref="ABLayerImport.DepthAndEditor"/> alone. The source format's whole depth
        /// range, so a group's own depths cannot reach into the next group's band; narrower
        /// interleaves the groups, which is a thing an author may want and this format can express. </summary>
        public int EditorGroupStride = ABLayerMap.DepthSpan;

        /// <summary> How far below the level's lowest content layer the first parallax object sits;
        /// every object behind it steps one further down. Relative rather than absolute because how
        /// deep the content itself reaches depends on the level and on
        /// <see cref="LayerImport"/> - a fixed base used to land in the middle of it. </summary>
        public int ParallaxLayerOffset = 1;

        /// <summary> How far above the level's highest content layer the first prefab placement
        /// sits; each further placement steps one up, carrying its whole materialized subtree with
        /// it. Zero, the default, gives a placement no draw order of its own, which is what the
        /// source game does with one - see ABPrefabImporter's ResolveLayer. Raising it pulls every
        /// placement in front of the level and spreads them over a layer each, which is a timeline
        /// row per placement and a level drawn in an order Afterbeat never drew it in. Read by the
        /// legacy layer modes only - under <see cref="ABLayerImport.Packed"/> a placement is packed
        /// into a row like any other unit. </summary>
        public int PlacementLayerOffset;

        // THE SONG IS THE LEVEL over there. Afterbeat stores no length of its own: its timeline is
        // its audio clip, an object timed past the end of the song simply never plays, and its
        // editor cannot scroll past it either. So a converted level whose length was MEASURED off
        // its content is a level of the wrong length by definition - it ends wherever the last
        // object happened to, which is neither where the song ends nor where the author was working.
        //
        // The SDK opens no files, so it cannot measure a clip; the host that opened the folder can,
        // and fills this in. Zero means it could not, and the length falls back to being measured.

        /// <summary> Length of the level's song in SECONDS, as the host measured it. Zero when it
        /// is unknown, which is the only case where the level's length is derived from its content
        /// instead. </summary>
        public float AudioLengthSeconds;

        /// <summary> Afterbeat's own threshold: its damage check refuses anything drawn below alpha
        /// 1, so this is what reproduces the source level. </summary>
        public const float DefaultOpacityHitThreshold = 1f;

        // The one option here that deliberately BREAKS fidelity, and it exists because the rule it
        // relaxes is invisible in this editor. A shockwave that grows for three seconds while fading
        // is lethal for the first tenth of a second over there and inert afterwards - correct, and
        // indistinguishable on screen from a hitbox that was lost in conversion. An author who wants
        // the ring to hurt for as long as it is visible cannot express that by editing the import;
        // they would have to find every generated child and restretch it.
        //
        // Zero switches the pass off outright rather than meaning "alpha 0 still hits": an object
        // keeps its own collider for its whole life and no children are made at all. That is the
        // same shape ImportParallax = false takes - the option removes the pass, it does not ask it
        // for a degenerate answer - and it keeps an overshooting curve that dips below zero from
        // punching a hole in a window the author asked to be whole.

        /// <summary> The alpha an object must be DRAWN at to hurt the player, in [0, 1]. One - the
        /// default - is Afterbeat's own rule. Lower arms the collider for more of a fade; zero
        /// disables the gate entirely, leaving every object's collider alone. See
        /// <see cref="Import.ABOpacityHitGate"/>. </summary>
        public float OpacityHitThreshold = DefaultOpacityHitThreshold;

        /// <summary> Every option at its default. </summary>
        public ABOptions()
        {
        }

        /// <summary> The framerate the source's seconds are resolved into; everything else defaults. </summary>
        public ABOptions(int framerate)
        {
            Framerate = framerate;
        }

        /// <summary> A copy with the framerate clamped into what the format allows - a generator's
        /// form can hand over anything. </summary>
        public ABOptions Sanitized()
        {
            var copy = (ABOptions)MemberwiseClone();
            copy.Framerate = System.Math.Clamp(Framerate, FrameRules.MinFramerate, FrameRules.MaxFramerate);
            copy.MaxParallaxLoopKeys = System.Math.Clamp(MaxParallaxLoopKeys, 2, MaxParallaxLoopKeysCap);
            copy.AudioLengthSeconds = AudioLengthSeconds > 0f ? AudioLengthSeconds : 0f;
            copy.EditorGroupStride = System.Math.Clamp(EditorGroupStride, 1, ValueRules.MaxLayer);
            copy.ParallaxLayerOffset = System.Math.Clamp(ParallaxLayerOffset, 0, ValueRules.MaxLayer);
            copy.PlacementLayerOffset = System.Math.Clamp(PlacementLayerOffset, 0, ValueRules.MaxLayer);
            copy.OpacityHitThreshold = System.Math.Clamp(OpacityHitThreshold, 0f, DefaultOpacityHitThreshold);
            return copy;
        }
    }
}