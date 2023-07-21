using MDPGen.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace MDPGen.Core.UnitTests
{
    [TestClass]
    public class ReplacementTokenCollectionTests
    {
        [TestMethod]
        public void NoTokensReturnsOriginaStringSuccess()
        {
            var rt = new ReplacementTokenCollection {
                { "replace_me", "value" }
            };

            string input = "This is a test\r\nOf the emergency broadcast system. Don't replace_me.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void SingleReplaceSuccess()
        {
            var rt = new ReplacementTokenCollection {
                { "replace_me", "value" }
            };

            string input = "This is a test\r\nOf the emergency broadcast {{replace_me}} system.\r\n";
            string expected = "This is a test\r\nOf the emergency broadcast value system.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MultipleReplaceSuccess()
        {
            var rt = new ReplacementTokenCollection {
                { "replace_me", "value" }
            };

            string input = "This is a {{replace_me}}test\r\nOf the emergency broadcast {{replace_me}} system.\r\n";
            string expected = "This is a valuetest\r\nOf the emergency broadcast value system.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TokenKeysAreCaseInsensitive()
        {
            var rt = new ReplacementTokenCollection {
                { "replace_me", "value" }
            };

            string input = "This is a {{Replace_Me}}test\r\nOf the emergency broadcast {{REPLACE_ME}} system.\r\n";
            string expected = "This is a valuetest\r\nOf the emergency broadcast value system.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void SpacesAreTrimmedFromReplacementTokenKeys()
        {
            var rt = new ReplacementTokenCollection {
                { "   replace_me ", "value" }
            };

            string input = "This is a {{replace_me}}test\r\nOf the emergency broadcast {{replace_me}} system.\r\n";
            string expected = "This is a valuetest\r\nOf the emergency broadcast value system.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TokensAreEnumerated()
        {
            var rt = new ReplacementTokenCollection();

            string input = "This is a {{replace_me}}test\r\nOf the emergency broadcast {{Replace_ME }} system.\r\n";

            var tokens = rt.Enumerate(input).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    Tuple.Create(10, "replace_me", "{{replace_me}}"),
                    Tuple.Create(57, "Replace_ME", "{{Replace_ME }}")
                }, tokens);
        }

        [TestMethod]
        public void IsValidTokenStopsReplacement()
        {
            var rt = new ReplacementTokenCollection {
                { "replace_me", "value" },
                { "name", "Mark" },
                { "author", "Creator" }
            };

            rt.IsValidToken += t => t != "replace_me";

            string input = "{{author}} is {{name}}. Don't {{replace_me}}.\r\n";
            string expected = "Creator is Mark. Don't {{replace_me}}.\r\n";

            string result = rt.Replace(input);
            Assert.AreEqual(expected, result);
        }

    }
}
