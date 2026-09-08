using System;
using BH.SDK.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> Reading and writing a field addressed by a path string, including into a list. </summary>
    public class ModificationTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestGet()
        {
            var instanceMetadata = new ModificationService();
            instanceMetadata.Add(typeof(Model1));
            instanceMetadata.Add(typeof(Model2));
            
            var model2 = new Model2("321", 345);
            var model22 = new Model2("131", 313);
            var model1 = new Model1("123", 123, new[] { 0f, 1f }, new[] { model2 }, model22);

            // Get
            
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop1"), model1.Prop1);
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop5.prop1"), model1.Prop5.Prop1);
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[0]"), model1.Prop4[0]);
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[0].prop2"), model1.Prop4[0].Prop2);
            
            // No exceptions, but null
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[0].prop3"), null); // no parameter in object
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[4].prop2"), null); // index out of range
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop6"), null); // not existed parameter
            
            // Exceptions
            try { Cat.Meow($"{instanceMetadata.GetValue(model1, "prop1.")}"); } // inappropriate ending
            catch (Exception e) { Assert.AreEqual(e.GetType(), typeof(ArgumentException)); }
            try { Cat.Meow($"{instanceMetadata.GetValue(model1, ".prop1")}"); } // inappropriate beginning
            catch (Exception e) { Assert.AreEqual(e.GetType(), typeof(ArgumentException)); }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestSet()
        {
            var instanceMetadata = new ModificationService();
            instanceMetadata.Add(typeof(Model1));
            instanceMetadata.Add(typeof(Model2));
            
            var model2 = new Model2("321", 345);
            var model22 = new Model2("131", 313);
            var model1 = new Model1("123", 123, new[] { 0f, 1f }, new[] { model2 }, model22);
            
            instanceMetadata.SetValue(model1, "666", "prop1");
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop1"), "666");
            
            instanceMetadata.SetValue(model1, "777", "prop5.prop1");
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop5.prop1"), "777");
            
            instanceMetadata.SetValue(model1, new Model2("888", 888), "prop4[0]");
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[0].prop1"), "888");
            
            instanceMetadata.SetValue(model1, 999, "prop4[0].prop2");
            Assert.AreEqual(instanceMetadata.GetValue(model1, "prop4[0].prop2"), 999);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestJToken()
        {
            var model2 = new Model2("321", 345);
            var model22 = new Model2("131", 313);
            var model1 = new Model1("123", 123, new[] { 0f, 1f }, new[] { model2 }, model22);

            var rootToken = JToken.FromObject(model1);
            Assert.AreNotEqual(null, rootToken);

            var token2 = rootToken.SelectToken("prop1");
            Assert.AreNotEqual(null, token2);

            var token3 = rootToken.SelectToken("prop3[1]");
            Assert.AreNotEqual(null, token3);

            var token4 = rootToken.SelectToken("prop4");
            Assert.AreNotEqual(null, token4);

            var token5 = rootToken.SelectToken("prop4[0]");
            Assert.AreNotEqual(null, token5);

            var token6 = rootToken.SelectToken("prop4[0].prop2");
            Assert.AreNotEqual(null, token6);

            var path1 = rootToken.Children();
            Assert.AreNotEqual(null, path1);

            // ReSharper disable once PossibleNullReferenceException
            var path2 = token5.Children();
            Assert.AreNotEqual(null, path2);
        }
        
        /// <summary> A fixture model with plain and nested properties for a path to walk into. </summary>
        public class Model1
        {
            [JsonProperty("prop1")]
            public string Prop1 { get; set; }
            
            [JsonProperty("prop2")]
            public int Prop2 { get; set; }
            
            [JsonProperty("prop3")]
            public float[] Prop3 { get; set; }
            
            [JsonProperty("prop4")]
            public Model2[] Prop4 { get; set; }
            
            [JsonProperty("prop5")]
            public Model2 Prop5 { get; set; }

            public Model1(string prop1, int prop2, float[] prop3, Model2[] prop4, Model2 prop5)
            {
                Prop1 = prop1;
                Prop2 = prop2;
                Prop3 = prop3;
                Prop4 = prop4;
                Prop5 = prop5;
            }
        }

        /// <summary> The nested half of <see cref="Model1"/>, including a list a path can index. </summary>
        public class Model2
        {
            [JsonProperty("prop1")]
            public string Prop1 { get; set; }
            
            [JsonProperty("prop2")]
            public int Prop2 { get; set; }

            public Model2(string prop1, int prop2)
            {
                Prop1 = prop1;
                Prop2 = prop2;
            }
        }
    }
}