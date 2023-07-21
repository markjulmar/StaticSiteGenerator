using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class YamlHeaderTests
    {
        [TestMethod]
        public void CheckYamlHeaderWithContentsShouldAddTokens()
        {
            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "secondValue: Another test\n" +
                                 "---\n" +
                                 "# Title goes here\n";

            var pageState = new PageVariables();

            string result = new YamlHeader().Process(pageState, markdownSrc);

            Assert.AreEqual("# Title goes here\n", result);
            Assert.AreEqual("This is a test", pageState.Tokens["pageTitle"]);
            Assert.AreEqual("Another test", pageState.Tokens["secondValue"]);
        }

        [TestMethod]
        public void InitialBlankLinesAreSkipped()
        {
            string markdownSrc = "\r\n  \r\n\t\t  \r\n---\n" +
                                 "pageTitle: This is a test\n" +
                                 "secondValue: Another test\n" +
                                 "---\n" +
                                 "# Title goes here\n";

            var pageState = new PageVariables();

            string result = new YamlHeader().Process(pageState, markdownSrc);

            Assert.AreEqual("# Title goes here\n", result);
            Assert.AreEqual("This is a test", pageState.Tokens["pageTitle"]);
            Assert.AreEqual("Another test", pageState.Tokens["secondValue"]);
        }

        [TestMethod]
        public void BlankLinesAreSkippedInHeader()
        {
            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "\n" +
                                 "secondValue: Another test\n" +
                                 "---\n" +
                                 "# Title goes here\n";

            var pageState = new PageVariables();

            string result = new YamlHeader().Process(pageState, markdownSrc);

            Assert.AreEqual("# Title goes here\n", result);
            Assert.AreEqual("This is a test", pageState.Tokens["pageTitle"]);
            Assert.AreEqual("Another test", pageState.Tokens["secondValue"]);
        }

        [TestMethod]
        public void MissingEndTagInYamlThrowsException()
        {
            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "\n" +
                                 "secondValue: Another test\n" +
                                 "# Title goes here\n" +
                                 " \n";

            var pageState = new PageVariables();

            Assert.ThrowsException<Exception>(() => new YamlHeader().Process(pageState, markdownSrc));
        }

        [TestMethod]
        public void InvalidKVPThrowsException()
        {
            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "\n" +
                                 "secondValue: Another test\n" +
                                 "# Title goes here\n" +
                                 "This is a test.\n" +
                                 "---\n" +
                                 "**Bold**\n";

            var pageState = new PageVariables();

            Assert.ThrowsException<Exception>(() => new YamlHeader().Process(pageState, markdownSrc));
        }

        [TestMethod]
        public void CommentLinesAreSkippedInHeader()
        {
            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "# This is a comment\n" +
                                 "secondValue: Another test\n" +
                                 "---\n" +
                                 "# Title goes here\n";

            var pageState = new PageVariables();

            string result = new YamlHeader().Process(pageState, markdownSrc);

            Assert.AreEqual("# Title goes here\n", result);
            Assert.AreEqual("This is a test", pageState.Tokens["pageTitle"]);
            Assert.AreEqual("Another test", pageState.Tokens["secondValue"]);
        }

        [TestMethod]
        public void IncorrectYamlHeaderIsIgnored()
        {
            string markdownSrc = "----\n" +
                                 "pageTitle: This is a test\n" +
                                 "secondValue: Another test\n" +
                                 "----\n" +
                                 "# Title goes here\n";

            var pageState = new PageVariables();

            string result = new YamlHeader().Process(pageState, markdownSrc);

            Assert.AreEqual(markdownSrc, result);
            Assert.AreEqual(0, pageState.Tokens.Count);
        }
    }
}
