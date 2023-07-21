using MDPGen.Core.Blocks;
using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class ConvertMarkdownToHtmlTests
    {
        [TestInitialize]
        public void Init()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void ConvertMarkdownSkipsNoContent()
        {
            string result = new ConvertMarkdownToHtml().Process(new Infrastructure.PageVariables(), null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ConvertMarkdownSkipsNonMarkdownContent()
        {
            string input = "<html />";
            var pageVars = new PageVariables();
            pageVars.InitializeFor(new ContentPage {
                ContentType = ContentType.Html
            });

            string result = new ConvertMarkdownToHtml().Process(pageVars, input);
            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void ConvertMarkdownProcessesMarkdown()
        {
            string input = "**Hello, _World_**";
            var pageVars = new PageVariables();
            pageVars.InitializeFor(new ContentPage {
                ContentType = ContentType.Markdown
            });

            string expected = "<p><strong>Hello, <em>World</em></strong></p>";
            string result = new ConvertMarkdownToHtml().Process(pageVars, input);
            Assert.AreEqual(expected, result);
        }
    }
}
