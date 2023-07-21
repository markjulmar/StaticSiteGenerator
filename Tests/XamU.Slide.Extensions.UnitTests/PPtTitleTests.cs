using System.IO;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MDPGen.Core;

namespace XamU.Slide.Extensions.UnitTests
{
    [TestClass]
    public class PPtTitleTests
    {
        [TestInitialize]
        public void Initialize()
        {
            ExtensionProcessor.Init(typeof(PowerPointTitleExtension));
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void RetrieveTitleFromPowerPointSuccess()
        {
            string directory = Path.GetDirectoryName(GetType().Assembly.Location);
            var pageVars = new PageVariables();
            pageVars.Tokens[PowerPointTitleExtension.PowerPointFilename] = Path.Combine(directory, "data", "test.pptx");

            ExtensionProcessor.InitializeExtensions(pageVars, null);

            Assert.AreEqual("AND101", pageVars.Tokens["Powerpoint.Id"]);
            Assert.AreEqual("Introduction to Xamarin.Android", pageVars.Tokens["Powerpoint.Title"]);
        }
    }
}
