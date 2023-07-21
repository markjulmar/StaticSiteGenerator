using System.IO;
using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MDPGen.Core;

namespace XamU.Slide.Extensions.UnitTests
{
    [TestClass]
    public class PPtObjectivesTests
    {
        [TestInitialize]
        public void Initialize()
        {
            ExtensionProcessor.Init(typeof(PowerPointObjectivesExtension));
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void RetrieveObjectivesFromPowerPointSuccess()
        {
            string directory = Path.GetDirectoryName(GetType().Assembly.Location);
            var pageVars = new PageVariables();
            pageVars.InitializeFor(new ContentPage());

            pageVars.Tokens[PowerPointTitleExtension.PowerPointFilename] = Path.Combine(directory, "data", "test.pptx");
            ExtensionProcessor.InitializeExtensions(pageVars, null);

            string markdownSource = "@powerPointObjectives()";

            string result = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "<ol class=\"objectives\">\r\n" +
                              "<li>Create a Xamarin.Android project</li>\r\n" +
                              "<li>Decompose an app into Activities</li>\r\n" +
                              "<li>Build an Activity's UI</li>\r\n" +
                              "<li>Write an Activity's behavior</li>\r\n" +
                              "<li>Update your Android SDK</li>\r\n" +
                              "</ol>\r\n";
            Assert.AreEqual(expected, result);
        }
    }
}
