using System.Linq;
using BH.SDK.Roslyn.Model;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // THE STRONGEST ASSERTION HERE IS "IT COMPILES". A generator that emits plausible-looking text
    // which does not build is the ordinary failure, and every fixture below therefore checks the
    // whole compilation - original sources plus generated ones - before it checks anything else.
    // The text assertions after that are about SHAPE: virtual where a subclass has to override,
    // explicit interface implementations where a polymorphic variant needs a second contract, and
    // the base-typed Equals that downgrades rather than answering false.

    [TestFixture]
    public class ModelGeneratorTests
    {
        private const string Usings = @"
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;
";

        private static GeneratorRun Run(params string[] sources)
            => GeneratorHarness.Run(new[] { new ModelGenerator() }, sources);

        private static void AssertCompiles(GeneratorRun run)
        {
            Assert.That(run.CompilationErrors, Is.Empty,
                () => string.Join("\n", run.CompilationErrors.Select(d => d.ToString()))
                      + "\n\n--- generated ---\n"
                      + string.Join("\n", run.Sources.Select(s => s.Key + ":\n" + s.Value)));
        }

        #region A leaf

        private const string Leaf = Usings + @"
namespace Fixture
{
    [GenerateModel]
    public sealed partial class Leaf : IModel<Leaf>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        [JsonProperty(Names.Name)] public string Name { get; set; }
        [JsonProperty(Names.Value)] public List<int> Values { get; set; }

        public Leaf() { Name = string.Empty; Values = new List<int>(); }
    }
}";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ALeaf_GetsTheWholeContract()
        {
            var run = Run(Leaf);
            AssertCompiles(run);

            var source = run.Source("Fixture.Leaf.Model.g.cs");
            Assert.That(source, Does.Contain("public void Reset()"));
            Assert.That(source, Does.Contain("public object Clone() => CopyImpl();"));
            Assert.That(source, Does.Contain("public Leaf Copy() => CopyImpl();"));
            Assert.That(source, Does.Contain("public void Update(Leaf src)"));
            Assert.That(source, Does.Contain("public void Pull(Leaf src)"));
            Assert.That(source, Does.Contain("public override bool Equals(object obj) => obj is Leaf value && Equals(value);"));
            Assert.That(source, Does.Contain("public override int GetHashCode()"));
            Assert.That(source, Does.Contain("public bool Equals(Leaf other)"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASealedLeaf_IsNotVirtualAnywhere()
        {
            // Sealed is the only signal the generator has for "can anything override this", so a
            // sealed model must come out with no virtual member at all - otherwise every one of the
            // 198 sealed models carries a vtable slot nothing can ever use.
            var source = Run(Leaf).Source("Fixture.Leaf.Model.g.cs");

            Assert.That(source, Does.Not.Contain("virtual"));
            Assert.That(source, Does.Contain("private bool EqualsLeaf("), "a sealed type's helpers stay private");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AValueList_IsCopiedByItsConstructor_NotPerItem()
        {
            // List<int> has no ICopyable elements, so CopyList would not even compile. Getting this
            // wrong is the commonest way a member-shape classifier fails.
            var source = Run(Leaf).Source("Fixture.Leaf.Model.g.cs");

            Assert.That(source, Does.Contain("new global::System.Collections.Generic.List<int>(src.Values)"));
            Assert.That(source, Does.Not.Contain("CopyList(src.Values)"));
        }

        #endregion

        #region A hierarchy

        private const string Hierarchy = Usings + @"
namespace Fixture
{
    [GenerateModel]
    public partial class Node : IModel<Node>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Node() { }
    }

    [GenerateModel]
    public sealed partial class Child : Node, IModel<Child>
    {
        [JsonProperty(Names.Name)] public string Name { get; set; }
        public Child() { Name = string.Empty; }
    }
}";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AHierarchy_ChainsThroughTheBase()
        {
            var run = Run(Hierarchy);
            AssertCompiles(run);

            var node = run.Source("Fixture.Node.Model.g.cs");
            var child = run.Source("Fixture.Child.Model.g.cs");

            Assert.That(node, Does.Contain("public virtual void Reset()"));
            Assert.That(node, Does.Contain("public virtual bool Equals(Node other)"));
            Assert.That(node, Does.Contain("protected bool EqualsNode("), "a subclass has to reach it");

            Assert.That(child, Does.Contain("public override void Reset()"));
            Assert.That(child, Does.Contain("base.Update(src);"));
            Assert.That(child, Does.Contain("base.Pull(src);"));
            Assert.That(child, Does.Contain("hash.Add(base.GetHashCode());"));
            Assert.That(child, Does.Contain("ResetFromNode(defaults);"));
            Assert.That(child, Does.Contain("CopyToNode(copy);"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ASubclass_DowngradesThroughABaseReference()
        {
            // Documented behaviour rather than a nicety: Equals through a base reference compares
            // the shared half when the other side is a sibling subtype, while Equals(object) says
            // false. ModelHierarchyTests pins the same thing on the real RectObject.
            var child = Run(Hierarchy).Source("Fixture.Child.Model.g.cs");

            Assert.That(child, Does.Contain("public override bool Equals(global::Fixture.Node other)"));
            Assert.That(child, Does.Contain("other is Child value ? EqualsChild(value) : EqualsNode(other)"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASubclass_KeepsCopyVirtualAndAddsTheTypedOne()
        {
            // Copy() cannot be overloaded on its return type, so the strongly typed answer is the
            // interface's own explicit implementation - which is exactly what CopyList relies on
            // when its element type is a subclass.
            var child = Run(Hierarchy).Source("Fixture.Child.Model.g.cs");

            Assert.That(child, Does.Contain("public override global::Fixture.Node Copy() => CopyImpl();"));
            Assert.That(child, Does.Contain("ICopyable<Child>.Copy() => CopyImpl();"));
        }

        #endregion

        #region A polymorphic family

        private const string Family = Usings + @"
namespace Fixture
{
    public enum ShapeKind : byte { Round = 0, Square = 1 }

    public interface IShape : IModel<IShape> { ShapeKind GetModelType(); }

    [GenerateModel]
    public sealed partial class Round : IShape, IModel<Round>
    {
        public ShapeKind GetModelType() => ShapeKind.Round;

        [JsonProperty(Names.Value)] public float Radius { get; set; }
        public Round() { }
    }

    [GenerateModel]
    public sealed partial class Holder : IModel<Holder>
    {
        [JsonProperty(Names.Value)] public IShape Shape { get; set; }
        public Holder() { Shape = new Round(); }
    }
}";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AFamilyMember_GetsTheInterfaceContractToo()
        {
            var run = Run(Family);
            AssertCompiles(run);

            var round = run.Source("Fixture.Round.Model.g.cs");

            Assert.That(round, Does.Contain("ICopyable<global::Fixture.IShape>.Copy() => CopyImpl();"));
            Assert.That(round, Does.Contain("IUpdatable<global::Fixture.IShape>.Update"));
            Assert.That(round, Does.Contain("IMoveable<global::Fixture.IShape>.Pull"));
            Assert.That(round, Does.Contain("public bool Equals(global::Fixture.IShape other) => other is Round value && Equals(value);"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFamilyMember_IgnoresASiblingVariant()
        {
            // A Round cannot become a Square, so the interface-typed Update must leave it alone
            // rather than write one variant's fields out of another's. ModelUtils.PullFrom is the
            // path that gets it right, and this no-op is what makes the wrong call harmless.
            var round = Run(Family).Source("Fixture.Round.Model.g.cs");

            Assert.That(round, Does.Contain("if (src is Round value) Update(value);"));
            Assert.That(round, Does.Contain("if (src is Round value) Pull(value);"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void APolymorphicMember_IsPulledThroughPullFrom()
        {
            var holder = Run(Family).Source("Fixture.Holder.Model.g.cs");

            Assert.That(holder, Does.Contain("PullFrom(Shape, src.Shape)"));
        }

        #endregion

        #region Scope dictionaries

        private const string Scope = Usings + @"
namespace Fixture
{
    public enum ItemKind : byte { Item = 0, Slim = 1 }

    [GenerateModel]
    public partial class Item : IModel<Item>
    {
        public virtual ItemKind GetModelType() => ItemKind.Item;

        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Item() { }
    }

    [GenerateModel]
    public sealed partial class Slim : Item, IModel<Slim>
    {
        public override ItemKind GetModelType() => ItemKind.Slim;

        [JsonProperty(Names.Name)] public string Name { get; set; }
        public Slim() { Name = string.Empty; }
    }

    [GenerateModel]
    public sealed partial class Scope : IModel<Scope>
    {
        [GenerateModelMerge]
        [JsonProperty(Names.Value)] public Dictionary<int, Item> Items { get; set; }
        public Scope() { Items = new Dictionary<int, Item>(); }
    }
}";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AMergedDictionary_IsPulledKeyByKeyThroughADispatcher()
        {
            var run = Run(Scope);
            AssertCompiles(run);

            var scope = run.Source("Fixture.Scope.Model.g.cs");
            var dispatchers = run.Source("ModelDispatchers.g.cs");

            Assert.That(scope, Does.Contain("PullDictionary(Items, src.Items, global::BH.SDK.Models.Generated.ItemModelPull.PullValue)"));
            Assert.That(scope, Does.Contain("CopyDictionary(src.Items)"), "Copy and Update still replace it");

            // The generated replacement for the hand-kept LevelUtils.PullObject switch: pulling
            // through the base reference would write Item's half and drop Slim's Name.
            Assert.That(dispatchers, Does.Contain("case global::Fixture.Slim typed: typed.Pull((global::Fixture.Slim)source); break;"));
            Assert.That(dispatchers, Does.Contain("if (target is null || target.GetType() != source.GetType()) return source.Copy();"));

            // The blob half of the same answer: a value read back through the base reference has to
            // know what to construct, and the tag it uses is the model's own GetModelType().
            Assert.That(dispatchers, Does.Contain("internal static class ItemBlob"));
            Assert.That(dispatchers, Does.Contain("case 1: value = new global::Fixture.Slim(); break;"));
        }

        #endregion

        #region Declining

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NoAttribute_EmitsNothingAtAll()
        {
            // The generator is scoped by Unity to BH.SDK and every assembly referencing it, which
            // is most of the project. Declining cleanly is what makes that free.
            var run = Run(Usings + @"
namespace Fixture
{
    public partial class Plain : IModel<Plain>
    {
        public object Clone() => Copy();
        public Plain Copy() => new Plain();
        public void Reset() { }
        public void Update(Plain src) { }
        public void Pull(Plain src) { }
        public bool Equals(Plain other) => true;
    }
}");

            Assert.That(run.Sources, Is.Empty);
            Assert.That(run.GeneratorDiagnostics, Is.Empty);
        }

        #endregion
    }
}
