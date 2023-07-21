using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class ReplaceTokensTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void SimpleReplaceShouldSucceed()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("replace_me", "cool");

            string expected = "This is a cool test.";
            string result = new ReplaceTokens().Process(pageVars, "This is a {{replace_me}} test.");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void XRefReplacementUsesPage()
        {
            var root = new ContentPage { Id = "root" };
            root.Children.AddRange(new[]
            {
                new ContentPage { Id = "test1", Parent = root, Url = "t1" },
                new ContentPage { Id = "test2", Parent = root, Url = "t2" },
                new ContentPage { Id = "test3", Parent = root, Url = "t3" },
                new ContentPage { Id = "test4", Parent = root, Url = "t4" },
            });

            var pageVars = new PageVariables();
            pageVars.InitializeFor(root.Children[2], "");

            string expected = "[](t2)";
            string result = new ReplaceTokens().Process(
                pageVars, "[]({{xref:test2}})");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MissingXrefReplacementLeavesBlank()
        {
            var root = new ContentPage { Id = "root" };
            root.Children.AddRange(new[]
            {
                new ContentPage { Id = "test1", Parent = root, Url = "t1" },
                new ContentPage { Id = "test2", Parent = root, Url = "t2" },
                new ContentPage { Id = "test3", Parent = root, Url = "t3" },
                new ContentPage { Id = "test4", Parent = root, Url = "t4" },
            });

            var pageVars = new PageVariables();
            pageVars.InitializeFor(root.Children[2], "");

            string expected = "[](<!--xref:test5-->)";
            string result = new ReplaceTokens().Process(
                pageVars, "[]({{xref:test5}})");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MultipleXRefReplacementsSuccess()
        {
            var root = new ContentPage { Id = "root" };
            root.Children.AddRange(new[]
            {
                new ContentPage { Id = "test1", Parent = root, Url = "t1" },
                new ContentPage { Id = "test2", Parent = root, Url = "t2" },
                new ContentPage { Id = "test3", Parent = root, Url = "t3" },
                new ContentPage { Id = "test4", Parent = root, Url = "t4" },
            });

            var pageVars = new PageVariables();
            pageVars.InitializeFor(root.Children[2], "");

            string expected = "[](t2)\n\n# [](t4)";
            string result = new ReplaceTokens().Process(
                pageVars, "[]({{xref:test2}})\n\n# []({{xref:test4}})");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MissingReplaceShouldCommentOutToken()
        {
            var pageVars = new PageVariables();

            string expected = "This is a <!--replace_me--> test.";
            string result = new ReplaceTokens().Process(pageVars, "This is a {{replace_me}} test.");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MultipleReplaceShouldSucceed()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("replace_me", "consider me replaced");

            string expected = "consider me replaced - consider me replaced again.";
            string result = new ReplaceTokens().Process(pageVars, "{{replace_me}} - {{replace_me}} again.");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void OneLevelEmbeddedReplaceShouldSucceed()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("replace_me", "{{no_really}}");
            pageVars.Tokens.Add("no_really", "completed!");

            string expected = "Test: completed!.";
            string result = new ReplaceTokens().Process(pageVars, "Test: {{replace_me}}.");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TwoLevelEmbeddedReplaceShouldSucceed()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("replace_me", "{{no_really}}");
            pageVars.Tokens.Add("no_really", "check this out");

            string expected = "Test: check this out";
            string result = new ReplaceTokens().Process(pageVars, "Test: {{replace_me}}");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ThreeLevelEmbeddedReplaceShouldFail()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("0", "{{1}}");
            pageVars.Tokens.Add("1", "{{2}}");
            pageVars.Tokens.Add("2", "3");

            string expected = "Test: <!--2--> ...";
            string result = new ReplaceTokens().Process(pageVars, "Test: {{0}} ...");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void NumericStringFormatReplaceShouldSucceed()
        {
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("price", "120.75");

            string expected = "$120.75";
            string result = new ReplaceTokens().Process(pageVars, "{{price:C}}");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IsolatedEndMarkerIsNotCommentedOut()
        {
            var pageVars = new PageVariables();

            string expected = "This is a test}} with some {{ markers in it.";
            string result = new ReplaceTokens().Process(pageVars, expected);

            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        public void DateStringFormatReplaceShouldSucceed()
        {
            var dt = new DateTime(2000, 12, 25);
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("xmas", dt.ToString());

            string expected = "December, 2000";
            string result = new ReplaceTokens().Process(pageVars, "{{xmas:Y}}");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CustomDateStringFormatReplaceShouldSucceed()
        {
            var dt = new DateTime(2000, 12, 25, 2, 55, 12).ToUniversalTime();
            var pageVars = new PageVariables();
            pageVars.Tokens.Add("xmas", dt.ToString());

            string expected = "12/25/00 14:55:12";
            string result = new ReplaceTokens().Process(pageVars, "{{xmas:MM/dd/yy H:mm:ss}}");

            Assert.AreEqual(expected, result);
        }

    }
}
