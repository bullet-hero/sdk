using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Interop.AfterBeat.Import
{
    // One import writes into one scope - a level's own objects, or a prefab template's. Everything
    // that has to be shared across the whole run lives here rather than being threaded through
    // twenty parameters: the id table, the shape resources being synthesized on demand, the theme
    // literal colours are resolved against, and the report.
    //
    // The id table is the reason importing is TWO passes. Afterbeat identifies objects by string
    // and parents them by that string, in no particular order - a child can appear before its
    // parent. So every object is minted first and only then filled in, or half the parent links
    // resolve to nothing purely because of where they sat in the file.
    //
    // EffectiveLayers exists for the same kind of reason: Afterbeat's depth is ABSOLUTE and this
    // format's Layer is relative to the parent, so writing a child's layer needs its parent's
    // effective one, which is not stored anywhere on the model being built. It is filled by that
    // same first pass, and for the same reason - filling it as the second pass walks the list
    // answers zero for every parent that happens to be written later.

    /// <summary> Everything one Afterbeat import shares across its objects. </summary>
    public class ABImportContext
    {
        /// <summary> What the caller asked for. </summary>
        public ABOptions Options { get; }

        /// <summary> Where every finding of this import goes. </summary>
        public InteropReport Report { get; }

        /// <summary> Where imported objects land. </summary>
        public IObjectScope Scope { get; }

        /// <summary> Where their ids come from. At level scope this is a DIFFERENT object from
        /// <see cref="Scope"/> - Level.Game is the scope, Level.Settings is the counter. </summary>
        public IObjectIdCounter Counter { get; }

        /// <summary> Level shape resources; synthesized shapes are added here on first use. May be
        /// null when a prefab is imported on its own, in which case nothing is synthesized. </summary>
        public IDictionary<ShapeId, CompositeShape> Shapes { get; }

        /// <summary> Level effect resources; an imported particle emitter's definition is added
        /// here on first use. May be null on the same terms as <see cref="Shapes"/> - a prefab
        /// imported on its own has nowhere to put one, and the placement still gets its id. </summary>
        public IDictionary<EffectId, EffectData> Effects { get; }

        /// <summary> The theme a semi-transparent colour is resolved against. May be null. </summary>
        public ThemeData ReferenceTheme { get; set; }

        // Draw order is a property of the LEVEL, not of one object list: a prefab template's
        // objects are materialized into the level and ordered against its own objects there, by the
        // same depths, so both read one plan. Resolving each list on its own and stacking the
        // results with offsets is what spread one real level over 900 layers - see ABLayerMap's
        // header. Null while a list is imported outside any level (a bare .vgp), which is the one
        // case where a list IS the whole ordering.

        /// <summary> The whole level's resolved draw order, shared by every list in it. </summary>
        public ABLayerMap.Plan LayerPlan { get; set; }

        // Under ABLayerImport.Packed the OWN layer of every object is decided for the whole scope at
        // once by ABLayoutPacker - rows depend on what else is alive at the same time - so it is
        // computed before anything is imported and only looked up here. Null under every other
        // mode, which is what keeps them exactly as they were.

        /// <summary> Afterbeat's own object id to the own (parent-relative) layer the packer gave
        /// it, or null when this scope is not packed. </summary>
        public Dictionary<string, int> PackedLayers { get; set; }

        /// <summary> The same for this scope's prefab placements, keyed by the placement's own id -
        /// a separate table because the source format keeps the two id spaces apart. </summary>
        public Dictionary<string, int> PackedPlacementLayers { get; set; }

        /// <summary> Each template's packed table, keyed by the source template, for the templates
        /// this level imports. Read when the template's own context is built. </summary>
        public Dictionary<VgpPrefab, Dictionary<string, int>> PackedTemplateLayers { get; set; }

        /// <summary> The packed own layer of one source object, when this scope is packed and knows it. </summary>
        public bool TryGetPackedLayer(string sourceId, out int layer)
        {
            layer = 0;
            return PackedLayers != null && !string.IsNullOrEmpty(sourceId)
                                        && PackedLayers.TryGetValue(sourceId, out layer);
        }

        /// <summary> Afterbeat's own object id to the one minted for it here. </summary>
        public Dictionary<string, ObjectId> ObjectIds { get; } = new();

        /// <summary> Afterbeat's own object id to the effective (parent-chain-summed) layer it
        /// ended up with. </summary>
        public Dictionary<string, int> EffectiveLayers { get; } = new();

        // A template's lead time is not metadata about the template - it is part of where its
        // placements land, and only the placement can apply it (ObjectManager.AddPrefabToLevel:
        // every copied object starts at placement.t - prefab.Offset + its own st). The templates are
        // imported before the placements are, and by then the source document's own Offset is gone,
        // so it is kept here for the one step that needs it.

        /// <summary> Each imported template's lead time in SECONDS, as the source document wrote
        /// it. A template nothing recorded has none. </summary>
        public Dictionary<PrefabId, float> PrefabLeadTimes { get; } = new();

        /// <summary> What the times in this scope are relative TO, in seconds - the moment a
        /// template will be placed at, and zero at level scope, where a start already is absolute.
        /// Only Song Time autokill reads it; see <see cref="ABTimeMap.ResolveEndTime"/>. </summary>
        public float AbsoluteTimeBase { get; set; }

        // WHICH FIELD AN OBJECT'S SCALE IS WRITTEN INTO IS NOT A STYLE CHOICE - it is how this
        // format expresses Afterbeat's per-child scale-inheritance switch, and getting it wrong is
        // what breaks a parented hierarchy the moment anything in it is scaled.
        //
        // RectTransform2D.Apply propagates Scale to children and deliberately does NOT propagate
        // Size, while the renderer consumes only their product. So the two fields look identical on
        // screen and are exact opposites to a child: Sizes behaves as p_t[1] == '0', Scales as
        // p_t[1] == '1'. The bit therefore crosses EXACTLY, per object, with nothing baked and
        // nothing that an animated parent can invalidate - as long as the choice is made from the
        // CHILDREN's masks rather than from the object's own.
        //
        // Filled by the same first pass that mints ids, for the same reason: a child may be written
        // before its parent, and a decision made as the list is walked would read half of them.

        /// <summary> What one source object's scale track means to its children. </summary>
        public enum ScaleTarget
        {
            /// <summary> Nothing inherits it - it is this object's own extent. </summary>
            Size = 0,

            /// <summary> Children inherit it - it is a multiplier they sit inside. </summary>
            Scale = 1,
        }

        /// <summary> Afterbeat's own object id to the field its scale track is written into.
        /// Anything absent is <see cref="ScaleTarget.Size"/>, which is what an object with no
        /// children means. </summary>
        public Dictionary<string, ScaleTarget> ScaleTargets { get; } = new();

        /// <summary> Where one source object's scale track belongs. </summary>
        public ScaleTarget GetScaleTarget(string sourceId)
            => !string.IsNullOrEmpty(sourceId) && ScaleTargets.TryGetValue(sourceId, out var target)
                ? target
                : ScaleTarget.Size;

        /// <summary> The scale a child has to be COMPENSATED by because its parent's track went
        /// into the field the child's own mask disagrees with - see ABObjectImporter's
        /// ResolveScaleTargets. Absent for every child whose mask agrees, which is nearly all of
        /// them. </summary>
        public Dictionary<string, (float X, float Y)> ScaleCompensations { get; } = new();

        /// <summary> The factor a child's own scale is multiplied by so that its composition under
        /// a non-uniformly scaled parent lands as near as this format can reach to the matrix
        /// Afterbeat composes - see ABObjectImporter's ResolveShearFits and ABLinearFit. Applies to
        /// the SCALE only, never to the position. </summary>
        public Dictionary<string, (float X, float Y)> ShearScales { get; } = new();

        /// <summary> The angle added to a child's own rotation track by the same fit, in radians.
        /// Only ever present for an object whose rotation reaches nothing but itself - see
        /// ResolveShearFits - so it is absent for most of the objects ShearScales covers. </summary>
        public Dictionary<string, float> ShearRotations { get; } = new();

        // Afterbeat does not hang a camera-parented object off the camera directly: it hangs it off
        // a node whose scale is the camera's own zoom over 20 (EventManager.Update, 20 being the
        // format's neutral zoom), so such an object keeps a constant SCREEN size while the camera
        // zooms. This format's camera has no scale to inherit, so a converted level drew all of that
        // content at whatever size it was authored at - on an ordinary level sitting at zoom 30,
        // every camera-parented object at two thirds of its real size.
        //
        // The node is therefore rebuilt as an ordinary object (ABLevelImporter.ImportCameraScaleRoot)
        // and every "camera" parent reference resolves to it instead. Rebuilding the node rather
        // than multiplying each object's own tracks is what makes the DESCENDANTS right too: a
        // Scales track is inherited here exactly as localScale is there, so one track covers a whole
        // subtree and one object's worth of keyframes covers the level.

        /// <summary> Stands in for the source game's camera-scale node, or Null when this level has
        /// nothing parented to the camera. </summary>
        public ObjectId CameraScaleRootId { get; set; } = ObjectId.Null;

        /// <summary> The band of draw order this scope's own objects occupy, which is what the
        /// background is placed below and the prefab placements above. Both stay 0 until something
        /// is resolved, so a scope holding no objects puts its background on layer -1. </summary>
        public int LowestContentLayer { get; private set; }

        /// <summary> The largest layer the import has assigned so far, so what comes after it can stay above. </summary>
        public int HighestContentLayer { get; private set; }

        /// <summary> Everything one import run shares, built once and passed down. </summary>
        public ABImportContext(ABOptions options, InteropReport report,
            IObjectScope scope, IObjectIdCounter counter,
            IDictionary<ShapeId, CompositeShape> shapes = null,
            IDictionary<EffectId, EffectData> effects = null)
        {
            Options = options ?? new ABOptions();
            Report = report ?? new InteropReport();
            Scope = scope;
            Counter = counter;
            Shapes = shapes;
            Effects = effects;
        }

        /// <summary> Mints an id for one source object, or hands back the one already minted for it.
        /// A source object with no id of its own gets an id nothing can reference, which is correct:
        /// nothing in the file can name it either. </summary>
        public ObjectId Mint(string sourceId)
        {
            if (!string.IsNullOrEmpty(sourceId) && ObjectIds.TryGetValue(sourceId, out var existing))
                return existing;

            var id = Counter.GetNextObjectId();
            if (!string.IsNullOrEmpty(sourceId)) ObjectIds[sourceId] = id;
            return id;
        }

        /// <summary> True while this context is filling a prefab TEMPLATE rather than a level. </summary>
        public bool IsPrefabScope => Scope is Prefab;

        /// <summary> Resolves a parent reference. "camera" is a real parent target here, exactly as
        /// it is there; an id nothing minted is a dangling reference and becomes a root. </summary>
        public ObjectId ResolveParent(string sourceParentId, string path)
        {
            if (string.IsNullOrEmpty(sourceParentId)) return ObjectId.Null;

            if (sourceParentId == VgdObject.CameraParentId)
            {
                // A template has no camera to hang off - it is content, placed later, possibly many
                // times. The source format lets a template object name one anyway; keeping it makes
                // a template this format's own rules reject (RuleParentObjectIdValid), on real
                // content, so it becomes a root of the template instead.
                if (!IsPrefabScope)
                    return CameraScaleRootId.IsValid() ? CameraScaleRootId : ObjectId.Camera;

                Report.Approximated("parent_camera_in_prefab",
                    "Objects inside an Afterbeat prefab can be parented to the camera; a template here has no camera, so those objects became roots of the template.",
                    path);
                return ObjectId.Null;
            }

            if (ObjectIds.TryGetValue(sourceParentId, out var parent)) return parent;

            Report.Approximated("parent_missing",
                "Some objects name a parent that is not in the level; those objects were imported as roots.",
                path);
            return ObjectId.Null;
        }

        /// <summary> Records what one source object's own depth means as an effective layer.
        /// Filled for the whole list before any object is read, so a parent is known however late
        /// in the document it was written. </summary>
        public void SetEffectiveLayer(string sourceId, int effectiveLayer)
        {
            if (string.IsNullOrEmpty(sourceId)) return;
            EffectiveLayers[sourceId] = effectiveLayer;
        }

        /// <summary> The effective layer of a source object's parent, or 0 for a root. </summary>
        public int GetParentEffectiveLayer(string sourceParentId)
        {
            if (string.IsNullOrEmpty(sourceParentId)) return 0;
            return EffectiveLayers.GetValueOrDefault(sourceParentId, 0);
        }

        /// <summary> Widens the band this scope's content occupies. Called once per resolved object
        /// list rather than per object, so a scope built out of several lists still answers for all
        /// of them. </summary>
        public void RegisterContentLayers(int lowest, int highest)
        {
            if (lowest < LowestContentLayer) LowestContentLayer = lowest;
            if (highest > HighestContentLayer) HighestContentLayer = highest;
        }
    }
}