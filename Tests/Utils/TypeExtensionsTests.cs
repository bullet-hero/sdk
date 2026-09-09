using System.Collections.Generic;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // THE TWO QUESTIONS THE RULE WALK AND THE MODIFICATION WALK BOTH ASK, and both of them answer
    // NO for everything that merely resembles the thing being asked about. That is the whole
    // content of this type and the whole risk in it: `IsList` matching an `IList`, an array or a
    // `List<T>`-derived class would send the walk down a collection branch for a member that is not
    // one, and the walk's failure mode is a rule silently never running rather than an exception.
    //
    // So what is pinned is as much what these refuse as what they accept - an open generic, an
    // interface, an array, and a Dictionary asked whether it is a List.

    /// <summary> TypeExtensions: closed List and Dictionary are recognised, everything that merely
    /// looks like one is not, and the element accessors answer null rather than throwing. </summary>
    public class TypeExtensionsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsList_AcceptsAClosedListAndNothingElse()
        {
            Assert.IsTrue(typeof(List<int>).IsList());
            Assert.IsTrue(typeof(List<RectObject>).IsList());

            // An OPEN generic answers TRUE, and that is worth writing down rather than asserting
            // away: the check is "is this the List<> shape", and List<> is trivially its own generic
            // definition. It is unreachable in practice - both walks read PROPERTY types, which are
            // always closed - so this is documenting the answer, not endorsing it as useful.
            Assert.IsTrue(typeof(List<>).IsList());

            Assert.IsFalse(typeof(IList<int>).IsList(), "the interface is not the type");
            Assert.IsFalse(typeof(int[]).IsList(), "an array is not a List<T>");
            Assert.IsFalse(typeof(Dictionary<int, int>).IsList());
            Assert.IsFalse(typeof(int).IsList());
            Assert.IsFalse(typeof(string).IsList(), "a string enumerates chars and is still not a list");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsDictionary_AcceptsAClosedDictionaryAndNothingElse()
        {
            Assert.IsTrue(typeof(Dictionary<ObjectId, RectObject>).IsDictionary());

            // Same as IsList above: the open generic IS its own definition, and no property type is
            // ever open, so the answer is a curiosity rather than a case.
            Assert.IsTrue(typeof(Dictionary<,>).IsDictionary());

            Assert.IsFalse(typeof(IDictionary<int, int>).IsDictionary());
            Assert.IsFalse(typeof(List<int>).IsDictionary());
            Assert.IsFalse(typeof(int).IsDictionary());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetListGenericParameter_IsTheElementTypeOrNull()
        {
            Assert.AreEqual(typeof(int), typeof(List<int>).GetListGenericParameterOrDefault());
            Assert.AreEqual(typeof(RectObject), typeof(List<RectObject>).GetListGenericParameterOrDefault());

            Assert.IsNull(typeof(int[]).GetListGenericParameterOrDefault());
            Assert.IsNull(typeof(Dictionary<int, int>).GetListGenericParameterOrDefault());
            Assert.IsNull(typeof(int).GetListGenericParameterOrDefault());
        }

        // The VALUE type, not the key - the walk descends into what a dictionary holds, and a
        // dictionary keyed by an id holding objects would otherwise be walked as a list of ids.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetDictionaryValueGenericParameter_IsTheValueTypeOrNull()
        {
            Assert.AreEqual(typeof(RectObject),
                typeof(Dictionary<ObjectId, RectObject>).GetDictionaryValueGenericParameterOrDefault());

            Assert.IsNull(typeof(List<int>).GetDictionaryValueGenericParameterOrDefault());
            Assert.IsNull(typeof(int).GetDictionaryValueGenericParameterOrDefault());
        }
    }
}