using BH.SDK.Models.Enums;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // A real Prefab rather than a stand-in, for RuleAnyDeviceActiveTests' reason: the invariant is
    // about Prefab.Root specifically, and a throwaway class carrying a RectObject would prove
    // nothing about the type that ships. AssertHasIssue rather than AssertInvalid, same as there -
    // a real aggregate may raise other issues and this is about this rule firing.

    /// <summary>
    /// RulePrefabRootFixed: a template's root carries the root id, stays active, stays on layer
    /// zero and starts on the first frame unanchored - and Fix writes every one of them back while
    /// carrying the authored length through.
    /// </summary>
    public class RulePrefabRootFixedTests : BaseRuleTests
    {
        // A real PrefabId, because the AssertValid cases judge the WHOLE aggregate and a Null one is
        // its own issue (RuleIPrimitiveGuidNotNull). Everything else here is the constructor's.
        private static Prefab ValidTemplate() => new() { PrefabId = PrefabId.NewGuid() };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_FreshTemplate_Passes() => AssertValid(ValidTemplate());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_InactiveRoot_Reported()
        {
            var prefab = ValidTemplate();
            prefab.Root.Active = false;

            AssertHasIssue<RulePrefabRootFixedAttribute>(prefab);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_NonZeroLayer_Reported()
        {
            var prefab = ValidTemplate();
            prefab.Root.Layer = 7;

            AssertHasIssue<RulePrefabRootFixedAttribute>(prefab);
        }

        // The id is the half RuleObjectIdValid cannot cover: that rule sees one value and no path,
        // so it can say PrefabRoot is legal as an identity inside a template but not that THIS slot
        // must hold exactly it. A user-space id here is a root nothing can address.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_UserSpaceId_Reported()
        {
            var prefab = ValidTemplate();
            prefab.Root.ObjectId = new ObjectId(1);

            AssertHasIssue<RulePrefabRootFixedAttribute>(prefab);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_WritesEveryPinnedFieldBack()
        {
            var prefab = ValidTemplate();
            prefab.Root.ObjectId = ObjectId.Null;
            prefab.Root.Active = false;
            prefab.Root.Layer = -42;
            prefab.Root.Span = new FrameSpan(50, 20, FrameAnchor.Both);

            new RulePrefabRootFixedAttribute().Fix(prefab, RuleContext.ForRoot(prefab));

            Assert.AreEqual(ObjectId.PrefabRoot, prefab.Root.ObjectId);
            Assert.IsTrue(prefab.Root.Active);
            Assert.AreEqual(ValueRules.DefaultLayer, prefab.Root.Layer);
            Assert.AreEqual(FrameRules.MinFrame, prefab.Root.Span.StartFrame);
            Assert.AreEqual(FrameAnchor.None, prefab.Root.Span.Anchors);
        }

        // The one thing the fix must NOT normalize: the length is the template's whole timeline, so
        // resetting the span outright would shorten every placement of it to a single frame.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_CarriesTheAuthoredLengthThrough()
        {
            var prefab = ValidTemplate();
            prefab.Root.Span = new FrameSpan(50, 240);

            new RulePrefabRootFixedAttribute().Fix(prefab, RuleContext.ForRoot(prefab));

            Assert.AreEqual(FrameRules.MinFrame, prefab.Root.Span.StartFrame);
            Assert.AreEqual(240, prefab.Root.Span.FrameDuration);
        }

        // The START is pinned and the LENGTH is authored - the two halves of one span, which is
        // why the rule has to look at it at all. A start of 50 would read as "the template begins
        // on frame 50", which nothing implements: ApplyRoot copies the duration and nothing else.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_RootStartsLate_Reported()
        {
            var prefab = ValidTemplate();
            prefab.Root.Span = new FrameSpan(50, 20);

            AssertHasIssue<RulePrefabRootFixedAttribute>(prefab);
        }

        // Same reasoning as the start: an anchor means "this edge follows the parent's" and the
        // root has no parent inside the template for either edge to follow.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_AnchoredRoot_Reported()
        {
            var prefab = ValidTemplate();
            prefab.Root.Span = prefab.Root.Span.WithAnchors(FrameAnchor.End);

            AssertHasIssue<RulePrefabRootFixedAttribute>(prefab);
        }

        // What the rule deliberately says nothing about: the authored half. The name and the tracks
        // are the template's own content, and so is the span's LENGTH - a template 20 frames long
        // is ordinary data, and the rule only ever moves that span back to frame one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_AuthoredNameLengthAndTracks_Pass()
        {
            var prefab = ValidTemplate();
            prefab.Root.Name = "wave";
            prefab.Root.Span = new FrameSpan(FrameRules.MinFrame, 20);
            prefab.Root.Positions.Add(new PosKey());

            AssertValid(prefab);
        }
    }
}