using System.IO;
using MDPGen.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MDPGen.UnitTests.PageGeneratorTests
{
    [TestClass]
    public class LoadSiteConfigInfoTests
    {
        private string siteConfigContents = 
            "{" +  
            "    'defaultPageTemplate': 'pageTemplate.html'," +
            "    'constants': [ {'key': 'pageTitle','value': 'Test Page' }," +
            "                   {'key': 'envTest', 'value': 'FIRST-%REPLACE_ME%-SECOND' } ]," +
            "    'searchFolders': [ 'temp', 'data/temp', '/temp' ]" +
            "}";

        [TestMethod]
        public void NullSiteConfigThrowsException()
        {
            var pageGenerator = new StaticSiteGenerator();
            Assert.ThrowsException<ArgumentNullException>(() => pageGenerator.Initialize(null));
        }

        [TestMethod]
        public void MissingSiteConfigThrowsException()
        {
            var pageGenerator = new StaticSiteGenerator();
            Assert.ThrowsException<FileNotFoundException>(() => pageGenerator.Initialize(@"test.siteconfig"));
        }

        [TestMethod]
        public void DefaultPageTemplateIsNotRooted()
        {
            string filename = Path.GetTempFileName();
            string folder = Path.GetDirectoryName(filename);
            File.WriteAllText(filename, siteConfigContents);

            try
            {
                var pageGenerator = new StaticSiteGenerator();
                pageGenerator.Initialize(filename);

                string expected = "pageTemplate.html";
                Assert.AreEqual(expected, pageGenerator.DefaultPageTemplate);
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [TestMethod]
        public void IncludeFoldersAreRooted()
        {
            string filename = Path.GetTempFileName();
            string folder = Path.GetDirectoryName(filename);
            File.WriteAllText(filename, siteConfigContents);

            try
            {
                var pageGenerator = new StaticSiteGenerator();
                pageGenerator.Initialize(filename);

                var expected = new string[] {
                    Path.Combine(folder, "temp"),
                    Path.Combine(folder, "data", "temp"),
                    @"C:\temp"
                };

                CollectionAssert.AreEqual(expected, pageGenerator.SearchFolders);
            }
            finally
            {
                File.Delete(filename);
            }
        }


        [TestMethod]
        public void TokenWithEnvironmentVariableReplacementSuccess()
        {
            Environment.SetEnvironmentVariable("REPLACE_ME", "testValue");

            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, siteConfigContents);

            try
            {
                var pageGenerator = new StaticSiteGenerator();
                pageGenerator.Initialize(filename);
                Assert.AreEqual("FIRST-testValue-SECOND", pageGenerator.Constants[1].Value);
            }
            finally
            {
                Environment.SetEnvironmentVariable("REPLACE_ME", null);
                File.Delete(filename);
            }

        }

        [TestMethod]
        public void MissingSiteConfigFileShouldThrowException()
        {
            Assert.ThrowsException<FileNotFoundException>(() =>
                new StaticSiteGenerator().Initialize(@"data/siteinfo.ext.json"));
        }

        [TestMethod]
        public void TokenWithEnvironmentVariableReplacementNoValueExits()
        {
            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, siteConfigContents);
            try
            {
                var pageGenerator = new StaticSiteGenerator();
                pageGenerator.Initialize(filename);
                Assert.AreEqual("FIRST--SECOND", pageGenerator.Constants[1].Value);
            }
            finally
            {
                File.Delete(filename);
            }
        }
    }
}
