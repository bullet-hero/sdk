using BH.SDK.Services;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> Variable substitution into a template, including a variable resolved only when reached. </summary>
    public class TextFormatTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestTextFormat()
        {
            var service = new TextFormatService();
            service.AddVariable("username", () => Metadata.Author.Vertoker);
            
            Assert.AreEqual(service.Process("{username}"), Metadata.Author.Vertoker);
            Cat.Meow(service.Process("Hello <color=green>{username}</color>!"));
        }
    }
}