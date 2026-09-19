using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Data;
using BH.SDK.Models.Events;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // WHAT STILL POINTS AT A RESOURCE, which nothing could answer before - the validator answers the
    // opposite direction and only after a delete has already broken something. Every case here is
    // one the editor's confirmation depends on being right: a count of zero means the delete goes
    // through with no question, so a MISSED reference is the expensive failure, not a spurious one.
    public class ResourceReferenceUtilsTests
    {
        private static Level LevelWith(params RectObject[] objects)
        {
            var level = new Level();
            foreach (var obj in objects) level.Game.Objects.Add(obj.ObjectId, obj);
            return level;
        }

        private static ShapeObject Shape(int id) => new() { ObjectId = new ObjectId(id) };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ATextureCountsEveryObjectDrawingWithIt()
        {
            var texture = new TextureResourceId(-1);
            var other = new TextureResourceId(-2);

            var a = Shape(1);
            a.TextureResourceId = texture;
            var b = Shape(2);
            b.TextureResourceId = texture;
            var c = Shape(3);
            c.TextureResourceId = other;

            var level = LevelWith(a, b, c);

            Assert.AreEqual(2, ResourceReferenceUtils.CountTextureReferences(level, texture));
            Assert.AreEqual(1, ResourceReferenceUtils.CountTextureReferences(level, other));
        }

        // An effect preset emits an image the same way an object draws one, and deleting that image
        // out from under it is exactly as destructive.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ATextureCountsAnEffectPresetToo()
        {
            var texture = new TextureResourceId(-1);

            var level = new Level();
            var effect = new EffectData { EffectId = new EffectId(System.Guid.NewGuid()) };
            effect.Core.TextureResourceId = texture;
            level.Resources.Effects.Add(effect.EffectId, effect);

            Assert.AreEqual(1, ResourceReferenceUtils.CountTextureReferences(level, texture));
        }

        // THE TEMPLATE'S CONTENT COUNTS, ROOT INCLUDED. A texture used by nothing but a prefab is
        // still used, and the root is the one object no dictionary holds.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryScopeIsWalked_TemplateRootIncluded()
        {
            var texture = new TextureResourceId(-1);

            var level = new Level();
            var prefab = new Prefab { PrefabId = new PrefabId(System.Guid.NewGuid()) };

            var root = new ShapeObject { ObjectId = ObjectId.PrefabRoot, TextureResourceId = texture };
            prefab.Root = root;

            var inner = Shape(1);
            inner.TextureResourceId = texture;
            prefab.Objects.Add(inner.ObjectId, inner);

            level.Resources.Prefabs.Add(prefab.PrefabId, prefab);

            Assert.AreEqual(2, ResourceReferenceUtils.CountTextureReferences(level, texture));
        }

        // Null is how an object says it draws nothing, so counting it would report every plain
        // object in the level as a reference to whatever is about to be deleted.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ANullIdIsNeverCounted()
        {
            var level = LevelWith(Shape(1), Shape(2));

            Assert.AreEqual(0, ResourceReferenceUtils.CountTextureReferences(level, TextureResourceId.Null));
            Assert.AreEqual(0, ResourceReferenceUtils.CountShapeReferences(level, ShapeId.Null));
            Assert.AreEqual(0, ResourceReferenceUtils.CountPrefabReferences(level, PrefabId.Null));
        }

        // A shape answers two questions - what an object is DRAWN as and what it is HIT as - so an
        // object using one custom shape for both holds two references to it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AShapeCountsBothSlots()
        {
            var shapeId = new ShapeId(System.Guid.NewGuid());

            var both = Shape(1);
            both.ShapeId = shapeId;
            both.ColliderId = shapeId;

            var drawnOnly = Shape(2);
            drawnOnly.ShapeId = shapeId;

            var level = LevelWith(both, drawnOnly);

            Assert.AreEqual(3, ResourceReferenceUtils.CountShapeReferences(level, shapeId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFontCountsTheTextObjectsSetInIt()
        {
            var font = new FontResourceId(-1);

            var text = new TextObject { ObjectId = new ObjectId(1), FontResourceId = font };
            var level = LevelWith(text, Shape(2));

            Assert.AreEqual(1, ResourceReferenceUtils.CountFontReferences(level, font));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnAudioClipCountsTheTracksPlayingIt()
        {
            var clip = new AudioResourceId(-1);

            var level = new Level();
            var track = new LevelTrack { AudioId = new AudioId(1), AudioResourceId = clip };
            level.Audio.Tracks.Add(track.AudioId, track);

            Assert.AreEqual(1, ResourceReferenceUtils.CountAudioReferences(level, clip));
        }

        // A ThemeRef colour stores a slot INDEX rather than a ThemeId, so only the level's own theme
        // track names a palette - which is why this counts keyframes and not colours.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AThemeCountsTheKeyframesSwitchingToIt()
        {
            var theme = new ThemeId(System.Guid.NewGuid());

            var level = new Level();
            level.Game.Events.Themes.Add(new ThemeKeyframe(theme, 1));
            level.Game.Events.Themes.Add(new ThemeKeyframe(theme, 30));
            level.Game.Events.Themes.Add(new ThemeKeyframe(new ThemeId(System.Guid.NewGuid()), 60));

            Assert.AreEqual(2, ResourceReferenceUtils.CountThemeReferences(level, theme));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnEffectCountsTheObjectsEmittingIt()
        {
            var effectId = new EffectId(System.Guid.NewGuid());

            var effect = new EffectObject { ObjectId = new ObjectId(1), EffectId = effectId };
            var level = LevelWith(effect, Shape(2));

            Assert.AreEqual(1, ResourceReferenceUtils.CountEffectReferences(level, effectId));
        }

        // A template placed only INSIDE another template is still placed, which is the case a walk
        // of Level.Game alone would report as unused - and deleting it would break the outer one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void APrefabCountsAPlacementNestedInAnotherTemplate()
        {
            var inner = new PrefabId(System.Guid.NewGuid());

            var level = new Level();
            var outer = new Prefab { PrefabId = new PrefabId(System.Guid.NewGuid()) };
            var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = inner };
            outer.Objects.Add(placement.ObjectId, placement);
            level.Resources.Prefabs.Add(outer.PrefabId, outer);

            Assert.AreEqual(1, ResourceReferenceUtils.CountPrefabReferences(level, inner));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ANullLevelAnswersZeroRatherThanThrowing()
        {
            Assert.AreEqual(0, ResourceReferenceUtils.CountTextureReferences(null, new TextureResourceId(-1)));
            Assert.AreEqual(0, ResourceReferenceUtils.CountAudioReferences(null, new AudioResourceId(-1)));
            Assert.AreEqual(0, ResourceReferenceUtils.CountThemeReferences(null, new ThemeId(System.Guid.NewGuid())));
        }
    }
}
