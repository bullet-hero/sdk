using System.Collections.Generic;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Clipboard;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Game;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Models.SettingGroups.Graphics;
using BH.SDK.Models.Values;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Update/Pull are OVERLOADS down a hierarchy, not overrides, and Copy() being virtual is the
    // contrast that makes it worth pinning: Copy() called through a base reference still returns the
    // full subclass, while Update() called through one writes the base half and leaves the rest.
    // That is deliberate (IModel's own header says why an override would cost three methods per
    // subclass), so it is documented behaviour rather than a bug, and documented behaviour is what a
    // test is for - nobody reading ShapeObject can see it from the class alone.
    //
    // The polymorphic value families are the other half. IVector2 is IModel<IVector2>, so every
    // implementation carries the interface's own Update/Pull explicitly, and those do NOTHING when
    // handed a sibling implementation - a Vector2Value cannot become a Vector2Rect. ModelUtils
    // .PullFrom is the path that gets it right, and the no-op is what makes a wrong call harmless
    // instead of half-applied.

    public class ModelHierarchyTests
    {
        #region Fixtures

        private static ShapeObject CreateShape(int layer, ShapeId shapeId) => new()
        {
            Name = layer.ToString(),
            Layer = layer,
            Span = new FrameSpan(layer, layer + 1),
            ShapeId = shapeId,
            ShaderType = ShaderType.Opaque,
        };

        private static AngleKey CreateAngle(int frame, float angle)
            => new(new FloatValue(angle), frame, EaseType.InSine);

        private static TextureResource CreateTexture(int id, TextureKind kind) => new()
        {
            TextureResourceId = new TextureResourceId(-id),
            Kind = kind,
            Sources = new List<ResourceKey> { new(ResourceUriType.LevelPath, id.ToString()) },
        };

        #endregion

        #region Update through a base reference

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_ThroughBaseType_WritesBaseHalfOnly()
        {
            var target = CreateShape(1, ShapeId.Square.Fill);
            var source = CreateShape(2, ShapeId.Circle.Fill);

            // binds RectObject.Update(RectObject), the only overload this reference can see
            RectObject baseView = target;
            baseView.Update(source);

            Assert.AreEqual(2, target.Layer, "the RectObject half must be written");
            Assert.AreEqual("2", target.Name, "the RectObject half must be written");
            Assert.AreEqual(ShapeId.Square.Fill, target.ShapeId, "the ShapeObject half must be untouched");
            Assert.AreNotEqual(source, target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_ThroughOwnType_WritesEveryHalf()
        {
            var target = CreateShape(1, ShapeId.Square.Fill);
            var source = CreateShape(2, ShapeId.Circle.Fill);

            target.Update(source);

            Assert.AreEqual(source, target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_ThroughBaseType_WritesBaseHalfOnly()
        {
            var target = CreateShape(1, ShapeId.Square.Fill);
            var source = CreateShape(2, ShapeId.Circle.Fill);

            RectObject baseView = target;
            baseView.Pull(source);

            Assert.AreEqual(2, target.Layer);
            Assert.AreEqual(ShapeId.Square.Fill, target.ShapeId);
            Assert.AreNotEqual(source, target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_ThroughOwnType_WritesEveryHalf()
        {
            var target = CreateShape(1, ShapeId.Square.Fill);
            var source = CreateShape(2, ShapeId.Circle.Fill);

            target.Pull(source);

            Assert.AreEqual(source, target);
        }

        // The contrast the two tests above exist for: Copy IS virtual, so the same base reference
        // that truncates an Update returns a whole ShapeObject from a Copy.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Copy_ThroughBaseType_StaysVirtual()
        {
            var shape = CreateShape(1, ShapeId.Circle.Fill);

            RectObject baseView = shape;
            var copy = baseView.Copy();

            Assert.IsInstanceOf<ShapeObject>(copy);
            Assert.AreEqual(ShapeId.Circle.Fill, ((ShapeObject)copy).ShapeId);
        }

        #endregion

        #region Keyframe hierarchy

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Update_KeyframeBase_WritesFrameAndEaseOnly()
        {
            var target = CreateAngle(1, 0.5f);
            var source = CreateAngle(7, 1.5f);

            Keyframe baseView = target;
            baseView.Update(source);

            Assert.AreEqual(7, target.Frame);
            Assert.AreEqual(EaseType.InSine, target.Ease);
            Assert.AreEqual(new FloatValue(0.5f), target.Angle, "the payload belongs to AngleKey, not Keyframe");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void UpdateAndPull_AngleKey_CarryBaseAndPayload()
        {
            var updated = CreateAngle(1, 0.5f);
            var pulled = CreateAngle(1, 0.5f);
            var source = CreateAngle(7, 1.5f);

            updated.Update(source);
            pulled.Pull(source);

            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
        }

        #endregion

        #region Abstract bases

        // Resource, BaseGraphicsSettings and BaseDeviceControlsSettings all declare an ABSTRACT
        // Copy() while their Update/Pull are concrete, so these are the three places where the base
        // half is only reachable through base.Update/base.Pull - a subclass that forgets the call
        // silently stops carrying Sources / Render / the six device fields.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UpdateAndPull_TextureResource_CarryTheResourceBase()
        {
            var updated = CreateTexture(1, TextureKind.Auto);
            var pulled = CreateTexture(1, TextureKind.Auto);
            var source = CreateTexture(2, TextureKind.PixelArt);

            updated.Update(source);
            pulled.Pull(source);

            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
            Assert.AreEqual(1, updated.Sources.Count);
            Assert.AreEqual("2", updated.Sources[0].Uri, "Resource.Sources lives on the abstract base");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void UpdateAndPull_AudioGraphicsSettings_CarryTheRenderSwitch()
        {
            var source = new AudioGraphicsSettings { Render = false, UseScrub = false, ScrubTime = 0.5f };

            var updated = new AudioGraphicsSettings();
            var pulled = new AudioGraphicsSettings();
            updated.Update(source);
            pulled.Pull(source);

            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
            Assert.IsFalse(updated.Render, "Render lives on BaseGraphicsSettings");
            Assert.IsFalse(pulled.Render, "Render lives on BaseGraphicsSettings");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void UpdateAndPull_GamepadControlsSettings_CarryTheDeviceBase()
        {
            var source = new GamepadControlsSettings
            {
                Active = false, Sensitivity = 2.5f, InvertY = true, Mode = GamepadControlMode.Direction,
            };

            var updated = new GamepadControlsSettings();
            var pulled = new GamepadControlsSettings();
            updated.Update(source);
            pulled.Pull(source);

            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
            Assert.IsTrue(updated.InvertY, "InvertY lives on BaseDeviceControlsSettings");
            Assert.IsTrue(pulled.InvertY, "InvertY lives on BaseDeviceControlsSettings");
        }

        #endregion

        #region Polymorphic value families

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Update_ThroughValueInterface_SameConcreteType_Writes()
        {
            IVector2 target = new Vector2Value(1f, 2f);

            target.Update(new Vector2Value(3f, 4f));

            Assert.AreEqual(new Vector2Value(3f, 4f), target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Update_ThroughValueInterface_ForeignConcreteType_DoesNothing()
        {
            IVector2 target = new Vector2Value(1f, 2f);

            target.Update(new Vector2Rect(3f, 4f, 5f, 6f));

            Assert.AreEqual(new Vector2Value(1f, 2f), target,
                "a Vector2Value cannot become a Vector2Rect, so the explicit impl must leave it alone");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Pull_ThroughValueInterface_ForeignConcreteType_DoesNothing()
        {
            IFloat target = new FloatValue(1f);

            target.Pull(new FloatMinMax(2f, 3f));

            Assert.AreEqual(new FloatValue(1f), target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PullFrom_SameConcreteType_KeepsTheInstance()
        {
            IVector2 target = new Vector2Value(1f, 2f);
            var before = target;

            var result = target.PullFrom(new Vector2Value(3f, 4f));

            Assert.AreSame(before, result, "identity is the whole point of Pull");
            Assert.AreEqual(new Vector2Value(3f, 4f), result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PullFrom_ForeignConcreteType_ReplacesWithACopy()
        {
            IVector2 target = new Vector2Value(1f, 2f);
            IVector2 source = new Vector2Rect(3f, 4f, 5f, 6f);

            var result = target.PullFrom(source);

            Assert.IsInstanceOf<Vector2Rect>(result, "identity cannot survive a type change");
            Assert.AreEqual(source, result);
            Assert.AreNotSame(source, result, "and it must not alias the source either");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PullFrom_NullTarget_ReturnsACopyOfSource()
        {
            IVector2 source = new Vector2Value(3f, 4f);

            var result = ((IVector2)null).PullFrom(source);

            Assert.AreEqual(source, result);
            Assert.AreNotSame(source, result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PullFrom_NullSource_ReturnsNull()
        {
            IVector2 target = new Vector2Value(1f, 2f);

            Assert.IsNull(target.PullFrom(null));
        }

        #endregion

        #region Identity - the one thing Update and Pull disagree about

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_ReplacesNestedInstances()
        {
            var target = MockData.CreateTestEffectData();
            var source = MockData.CreateTestEffectData();
            source.Core.ParticleCount = target.Core.ParticleCount + 1;
            var nested = target.Core;

            target.Update(source);

            Assert.AreNotSame(nested, target.Core, "Update replaces, only the receiver survives");
            Assert.AreEqual(source.Core.ParticleCount, target.Core.ParticleCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_KeepsNestedInstances()
        {
            var target = MockData.CreateTestEffectData();
            var source = MockData.CreateTestEffectData();
            source.Core.ParticleCount = target.Core.ParticleCount + 1;
            var nested = target.Core;

            target.Pull(source);

            Assert.AreSame(nested, target.Core, "Pull writes into what is already there");
            Assert.AreEqual(source.Core.ParticleCount, nested.ParticleCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_KeepsNestedInstances_AllTheWayDown()
        {
            var target = MockData.CreateValidTestSettings();
            var source = MockData.CreateValidTestSettings();
            source.Graphics.Textures.SizeLimit = TextureSizeLimit.Side1024;
            var textures = target.Graphics.Textures;

            target.Pull(source);

            Assert.AreSame(textures, target.Graphics.Textures,
                "a device hands its settings out one sub-group at a time, so every level of the tree keeps its instance");
            Assert.AreEqual(TextureSizeLimit.Side1024, textures.SizeLimit);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_ReplacesNestedInstances_AllTheWayDown()
        {
            var target = MockData.CreateValidTestSettings();
            var source = MockData.CreateValidTestSettings();
            var textures = target.Graphics.Textures;

            target.Update(source);

            Assert.AreNotSame(textures, target.Graphics.Textures);
        }

        #endregion

        #region GameLevel.Objects - the one collection Pull merges instead of replacing

        // A scope is where a reference IS the address, so this is the one dictionary Pull walks key
        // by key. Three things have to hold at once and only the first is obvious: the surviving
        // objects keep their instances, the SUBCLASS half travels (a merge through a RectObject
        // reference would write the transform and leave the shape behind), and an id whose concrete
        // type changed is replaced rather than half-written.

        private static GameLevel CreateScope(params RectObject[] objects)
        {
            var level = new GameLevel();
            foreach (var obj in objects) level.Objects.Add(obj.ObjectId, obj);
            return level;
        }

        private static ShapeObject CreateShape(int id, int layer, ShapeId shapeId)
        {
            var shape = CreateShape(layer, shapeId);
            shape.ObjectId = new ObjectId(id);
            return shape;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_Objects_KeepsTheDictionaryAndItsInstances()
        {
            var target = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill));
            var source = CreateScope(CreateShape(1, 9, ShapeId.Square.Fill));
            var objects = target.Objects;
            var kept = objects[new ObjectId(1)];

            target.Pull(source);

            Assert.AreSame(objects, target.Objects, "the scope's own dictionary is held elsewhere too");
            Assert.AreSame(kept, target.Objects[new ObjectId(1)]);
            Assert.AreEqual(9, kept.Layer, "and it must actually have been written");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_Objects_CarriesTheSubclassHalf()
        {
            var target = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill));
            var source = CreateScope(CreateShape(1, 9, ShapeId.Circle.Fill));
            var kept = (ShapeObject)target.Objects[new ObjectId(1)];

            target.Pull(source);

            Assert.AreEqual(ShapeId.Circle.Fill, kept.ShapeId,
                "a merge through a RectObject reference would have dropped this");
            Assert.AreEqual(source.Objects[new ObjectId(1)], kept);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_Objects_ReplacesWhenTheConcreteTypeChanged()
        {
            var target = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill));
            var text = new TextObject { ObjectId = new ObjectId(1), Layer = 9 };
            var source = CreateScope(text);
            var was = target.Objects[new ObjectId(1)];

            target.Pull(source);

            var now = target.Objects[new ObjectId(1)];
            Assert.IsInstanceOf<TextObject>(now, "identity cannot survive a type change");
            Assert.AreNotSame(was, now);
            Assert.AreNotSame(text, now, "and the replacement must not alias the source");
            Assert.AreEqual(text, now);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_Objects_AddsAndDropsKeys()
        {
            var target = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill),
                CreateShape(2, 2, ShapeId.Square.Fill));
            var added = CreateShape(3, 3, ShapeId.Circle.Fill);
            var source = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill), added);

            target.Pull(source);

            Assert.AreEqual(2, target.Objects.Count);
            Assert.IsFalse(target.Objects.ContainsKey(new ObjectId(2)), "the source no longer has it");
            Assert.IsTrue(target.Objects.ContainsKey(new ObjectId(3)));
            Assert.AreNotSame(added, target.Objects[new ObjectId(3)], "a new key takes a copy, never the source's own");
            Assert.AreEqual(added, target.Objects[new ObjectId(3)]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_Objects_ReplacesTheDictionary()
        {
            var target = CreateScope(CreateShape(1, 1, ShapeId.Square.Fill));
            var source = CreateScope(CreateShape(1, 9, ShapeId.Circle.Fill));
            var objects = target.Objects;
            var was = objects[new ObjectId(1)];

            target.Update(source);

            Assert.AreNotSame(objects, target.Objects, "Update replaces, that is the whole difference");
            Assert.AreNotSame(was, target.Objects[new ObjectId(1)]);
            Assert.AreEqual(source, target);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PullObject_DispatchesEveryObjectType()
        {
            RectObject[] objects =
            {
                new RectObject { Layer = 5 },
                new ShapeObject { Layer = 5, ShapeId = ShapeId.Circle.Fill },
                new TextObject { Layer = 5 },
                new EffectObject { Layer = 5 },
                new PrefabObject { Layer = 5 },
            };

            foreach (var source in objects)
            {
                var target = source.Copy();
                target.Layer = 0;

                var result = LevelUtils.PullObject(target, source);

                Assert.AreSame(target, result, $"{source.GetType().Name} must keep its instance");
                Assert.AreEqual(source, result, $"{source.GetType().Name} must be written whole");
            }
        }

        #endregion

        #region Prefab and ClipboardData - the other scopes held from outside

        // Prefab is an IObjectScope exactly like GameLevel (Prefab Mode repoints the editor at one),
        // and a ClipboardData's five sections are held one per timeline. Same merge, same reasons -
        // and the track dictionaries take the constrained overload instead, LevelTrack being a
        // concrete type with no hierarchy to dispatch over.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_PrefabObjects_MergesInPlace()
        {
            var target = new Prefab { Name = "before" };
            target.Objects.Add(new ObjectId(1), CreateShape(1, 1, ShapeId.Square.Fill));
            target.Objects.Add(new ObjectId(2), CreateShape(2, 2, ShapeId.Square.Fill));

            var source = new Prefab { Name = "after" };
            source.Objects.Add(new ObjectId(1), CreateShape(1, 9, ShapeId.Circle.Fill));

            var objects = target.Objects;
            var kept = (ShapeObject)objects[new ObjectId(1)];

            target.Pull(source);

            Assert.AreSame(objects, target.Objects, "Prefab Mode points the editor at this dictionary");
            Assert.AreSame(kept, target.Objects[new ObjectId(1)]);
            Assert.AreEqual(ShapeId.Circle.Fill, kept.ShapeId, "the subclass half must travel");
            Assert.AreEqual(1, target.Objects.Count, "and the id the source dropped must go");
            Assert.AreEqual("after", target.Name);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_ClipboardObjectSections_MergeInPlace()
        {
            var target = new ClipboardData();
            target.Objects.Add(new ObjectId(1), CreateShape(1, 1, ShapeId.Square.Fill));
            target.PrefabObjects.Add(new ObjectId(2), new PrefabObject { ObjectId = new ObjectId(2), Layer = 1 });
            target.KeyObjects.Add(new ObjectId(3), new TextObject { ObjectId = new ObjectId(3), Layer = 1 });

            var source = new ClipboardData { Content = ClipboardContent.Objects };
            source.Objects.Add(new ObjectId(1), CreateShape(1, 9, ShapeId.Circle.Fill));
            source.PrefabObjects.Add(new ObjectId(2), new PrefabObject { ObjectId = new ObjectId(2), Layer = 9 });
            source.KeyObjects.Add(new ObjectId(3), new TextObject { ObjectId = new ObjectId(3), Layer = 9 });

            var objects = target.Objects;
            var shape = (ShapeObject)objects[new ObjectId(1)];
            var prefab = target.PrefabObjects[new ObjectId(2)];
            var text = target.KeyObjects[new ObjectId(3)];

            target.Pull(source);

            Assert.AreSame(objects, target.Objects, "each section is a buffer held by its own timeline");
            Assert.AreSame(shape, target.Objects[new ObjectId(1)]);
            Assert.AreSame(prefab, target.PrefabObjects[new ObjectId(2)]);
            Assert.AreSame(text, target.KeyObjects[new ObjectId(3)]);
            Assert.AreEqual(ShapeId.Circle.Fill, shape.ShapeId);
            Assert.AreEqual(9, prefab.Layer);
            Assert.AreEqual(9, text.Layer);
            Assert.AreEqual(ClipboardContent.Objects, target.Content);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_ClipboardTrackSections_MergeInPlace()
        {
            var target = new ClipboardData();
            target.AudioTracks.Add(new AudioId(1), new LevelTrack { AudioId = new AudioId(1), Volume = 0.25f });
            target.KeyTracks.Add(new AudioId(2), new LevelTrack { AudioId = new AudioId(2), Volume = 0.25f });

            var source = new ClipboardData();
            source.AudioTracks.Add(new AudioId(1), new LevelTrack { AudioId = new AudioId(1), Volume = 0.75f });
            source.KeyTracks.Add(new AudioId(3), new LevelTrack { AudioId = new AudioId(3), Volume = 0.75f });

            var tracks = target.AudioTracks;
            var kept = tracks[new AudioId(1)];

            target.Pull(source);

            Assert.AreSame(tracks, target.AudioTracks);
            Assert.AreSame(kept, target.AudioTracks[new AudioId(1)]);
            Assert.AreEqual(0.75f, kept.Volume);
            Assert.IsFalse(target.KeyTracks.ContainsKey(new AudioId(2)), "the source no longer has it");
            Assert.IsTrue(target.KeyTracks.ContainsKey(new AudioId(3)));
            Assert.AreNotSame(source.KeyTracks[new AudioId(3)], target.KeyTracks[new AudioId(3)],
                "a new key takes a copy, never the source's own");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Update_PrefabAndClipboard_StillReplaceTheirDictionaries()
        {
            var prefab = new Prefab();
            prefab.Objects.Add(new ObjectId(1), CreateShape(1, 1, ShapeId.Square.Fill));
            var prefabObjects = prefab.Objects;

            var clipboard = new ClipboardData();
            clipboard.Objects.Add(new ObjectId(1), CreateShape(1, 1, ShapeId.Square.Fill));
            var clipboardObjects = clipboard.Objects;

            prefab.Update(new Prefab());
            clipboard.Update(new ClipboardData());

            Assert.AreNotSame(prefabObjects, prefab.Objects);
            Assert.AreNotSame(clipboardObjects, clipboard.Objects);
        }

        #endregion
    }
}
