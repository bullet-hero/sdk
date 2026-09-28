using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Events;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // ONE LEVEL TOUCHING EVERY REFERENCE FIELD THE FORMAT HAS, each with its own id, so a field the
    // walk forgets is a missing entry and a field it visits twice is a duplicate. The count, the
    // closure and the remap all ride on this walk, which is why it is pinned from both directions:
    // what it reads, and what it writes when told to move everything.
    public class ResourceGraphTests
    {
        private sealed class Fixture
        {
            public readonly Level Level = new();
            public readonly List<ResourceRef> Expected = new();

            public readonly ShapeObject Shape = new() { ObjectId = new ObjectId(1) };
            public readonly TextObject Text = new() { ObjectId = new ObjectId(2) };
            public readonly EffectObject Emitter = new() { ObjectId = new ObjectId(3) };
            public readonly PrefabObject Placement = new() { ObjectId = new ObjectId(4) };
            public readonly EffectData Effect = new() { EffectId = new EffectId(Guid.NewGuid()) };
            public readonly Prefab Template = new() { PrefabId = new PrefabId(Guid.NewGuid()) };
            public readonly ThemeKeyframe ThemeKey = new(new ThemeId(Guid.NewGuid()), 1);
            public readonly LevelTrack Track = new();

            public Fixture()
            {
                Shape.ShapeId = new ShapeId(Guid.NewGuid());
                Shape.ColliderId = new ShapeId(Guid.NewGuid());
                Shape.TextureResourceId = new TextureResourceId(-1);
                Text.FontResourceId = new FontResourceId(-2);
                Emitter.EffectId = Effect.EffectId;
                Effect.Core.ParticleShapeId = new ShapeId(Guid.NewGuid());
                Effect.Core.TextureResourceId = new TextureResourceId(-3);
                Track.AudioResourceId = new AudioResourceId(-4);

                // The template holds a nested placement of itself only as an id - the walk does not
                // resolve references, so a self-reference is as good a fixture as any other template.
                Placement.PrefabId = Template.PrefabId;
                Placement.Modifications.Add(
                    new ModificationKey(new ObjectId(9), ModificationFields.ShapeId),
                    new Modification(new ObjectId(9), ModificationFields.ShapeId, new ShapeId(Guid.NewGuid())));
                Placement.Modifications.Add(
                    new ModificationKey(new ObjectId(9), ModificationFields.TextureResourceId),
                    new Modification(new ObjectId(9), ModificationFields.TextureResourceId, new TextureResourceId(-5)));
                Placement.Modifications.Add(
                    new ModificationKey(new ObjectId(9), ModificationFields.FontResourceId),
                    new Modification(new ObjectId(9), ModificationFields.FontResourceId, new FontResourceId(-6)));
                Placement.Modifications.Add(
                    new ModificationKey(new ObjectId(9), ModificationFields.EffectId),
                    new Modification(new ObjectId(9), ModificationFields.EffectId, new EffectId(Guid.NewGuid())));

                Level.Game.Objects.Add(Shape.ObjectId, Shape);
                Level.Game.Objects.Add(Text.ObjectId, Text);
                Level.Game.Objects.Add(Emitter.ObjectId, Emitter);
                Level.Game.Events.Themes.Add(ThemeKey);
                Level.Audio.Tracks.Add(new AudioId(1), Track);
                Level.Resources.Effects.Add(Effect.EffectId, Effect);
                Template.Objects.Add(Placement.ObjectId, Placement);
                Level.Resources.Prefabs.Add(Template.PrefabId, Template);

                Expected.Add(ResourceRef.Of(Shape.ShapeId));
                Expected.Add(ResourceRef.Of(Shape.ColliderId));
                Expected.Add(ResourceRef.Of(Shape.TextureResourceId));
                Expected.Add(ResourceRef.Of(Text.FontResourceId));
                Expected.Add(ResourceRef.Of(Emitter.EffectId));
                Expected.Add(ResourceRef.Of(ThemeKey.ThemeId));
                Expected.Add(ResourceRef.Of(Track.AudioResourceId));
                Expected.Add(ResourceRef.Of(Effect.Core.ParticleShapeId));
                Expected.Add(ResourceRef.Of(Effect.Core.TextureResourceId));
                Expected.Add(ResourceRef.Of(Placement.PrefabId));
                foreach (var modification in Placement.Modifications.Values)
                    Expected.Add(AsRef(modification.Value));
            }

            private static ResourceRef AsRef(object value) => value switch
            {
                ShapeId id => ResourceRef.Of(id),
                TextureResourceId id => ResourceRef.Of(id),
                FontResourceId id => ResourceRef.Of(id),
                EffectId id => ResourceRef.Of(id),
                _ => throw new ArgumentException(value?.GetType().Name),
            };
        }

        private static List<ResourceRef> Visit(Level level, bool modifications)
        {
            var visited = new List<ResourceRef>();
            ResourceGraph.Walk(level, reference =>
            {
                visited.Add(reference);
                return reference;
            }, modifications);
            return visited;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryReferenceField_IsVisitedExactlyOnce()
        {
            var fixture = new Fixture();

            CollectionAssert.AreEquivalent(fixture.Expected, Visit(fixture.Level, modifications: true));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void WithoutModifications_OnlyTheOverridesAreLeftOut()
        {
            var fixture = new Fixture();

            Assert.AreEqual(fixture.Expected.Count - fixture.Placement.Modifications.Count,
                Visit(fixture.Level, modifications: false).Count);
        }

        // The write half: move every reference and check each field landed on its new id. A field the
        // walk reads but forgets to write back fails here and nowhere else.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void MovingEveryReference_RewritesEveryField()
        {
            var fixture = new Fixture();
            var remap = new ResourceRemap();
            var next = -100;
            foreach (var reference in fixture.Expected.Distinct())
            {
                remap.Add(reference, reference.Type.IsGuidAddressed()
                    ? ResourceRef.Of(reference.Type, 0, Guid.NewGuid())
                    : ResourceRef.Of(reference.Type, next--, Guid.Empty));
            }

            ResourceGraph.Walk(fixture.Level, remap.Map, modifications: true);

            var expected = fixture.Expected.Select(remap.Map).ToList();
            CollectionAssert.AreEquivalent(expected, Visit(fixture.Level, modifications: true));
        }
    }
}
