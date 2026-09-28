using System;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // A prefab needs what it draws, what its effects emit and what it nests - transitively - and
    // nothing the source does not own. A missing member here is a dependency an export leaves behind.
    public class ResourceClosureTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void APrefab_BringsItsShapesTexturesEffectsAndNestedTemplates()
        {
            var resources = new LevelResources();

            var texture = new TextureResource { TextureResourceId = new TextureResourceId(-1) };
            resources.Textures.Add(texture.TextureResourceId, texture);
            var particleTexture = new TextureResource { TextureResourceId = new TextureResourceId(-2) };
            resources.Textures.Add(particleTexture.TextureResourceId, particleTexture);
            var shape = new CompositeShape { ShapeId = new ShapeId(Guid.NewGuid()) };
            resources.CompositeShapes.Add(shape.ShapeId, shape);

            var effect = new EffectData { EffectId = new EffectId(Guid.NewGuid()) };
            effect.Core.TextureResourceId = particleTexture.TextureResourceId;
            resources.Effects.Add(effect.EffectId, effect);

            var nested = new Prefab { PrefabId = new PrefabId(Guid.NewGuid()) };
            var emitter = new EffectObject { ObjectId = new ObjectId(1), EffectId = effect.EffectId };
            nested.Objects.Add(emitter.ObjectId, emitter);
            resources.Prefabs.Add(nested.PrefabId, nested);

            var outer = new Prefab { PrefabId = new PrefabId(Guid.NewGuid()) };
            var drawn = new ShapeObject
            {
                ObjectId = new ObjectId(1), ShapeId = shape.ShapeId, TextureResourceId = texture.TextureResourceId,
                // A game-defined shape: every build has it, so the closure must not try to carry it.
                ColliderId = new ShapeId(Guid.NewGuid()),
            };
            var placement = new PrefabObject { ObjectId = new ObjectId(2), PrefabId = nested.PrefabId };
            outer.Objects.Add(drawn.ObjectId, drawn);
            outer.Objects.Add(placement.ObjectId, placement);
            resources.Prefabs.Add(outer.PrefabId, outer);

            var unrelated = new ThemeData { ThemeId = new ThemeId(Guid.NewGuid()) };
            resources.Themes.Add(unrelated.ThemeId, unrelated);

            var closure = ResourceClosure.Collect(resources, new[] { ResourceRef.Of(outer.PrefabId) });

            CollectionAssert.AreEquivalent(new[]
            {
                ResourceRef.Of(outer.PrefabId), ResourceRef.Of(nested.PrefabId), ResourceRef.Of(effect.EffectId),
                ResourceRef.Of(shape.ShapeId), ResourceRef.Of(texture.TextureResourceId),
                ResourceRef.Of(particleTexture.TextureResourceId),
            }, closure);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ARootTheSourceDoesNotOwn_IsDropped()
        {
            var closure = ResourceClosure.Collect(new LevelResources(),
                new[] { ResourceRef.Of(new ThemeId(Guid.NewGuid())) });

            Assert.AreEqual(0, closure.Count);
        }
    }
}
