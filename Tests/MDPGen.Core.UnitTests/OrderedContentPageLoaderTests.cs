using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace MDPGen.Core.UnitTests
{
    [TestClass]
    public class OrderedContentPageLoaderTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void FindsFileWithMarkdownExtensionSuccess()
        {
            var pageLoader = new OrderedContentPageLoader();
            using (var folder = TempDir.Create())
            {
                var fn1 = new TempFile(
                    Path.Combine(folder.Name, pageLoader.DirectoryInfoFilename),
                    "[ 'default' ]");

                var fn2 = new TempFile(
                    Path.Combine(folder.Name, "default.md"),
                    "<html />");

                var page = pageLoader.LoadAsync(folder.Name).Result;

                Assert.AreEqual(fn2.Name, page.Filename);
                Assert.AreEqual("default.md", page.RelativeFilename);
                Assert.AreEqual("default.html", page.RelativeOutputFilename);
                Assert.AreEqual("default.md", page.Id);
                Assert.AreEqual("default", page.Title);
            }
        }

        [TestMethod]
        public void FindsFileWithHtmlExtensionSuccess()
        {
            var pageLoader = new OrderedContentPageLoader();

            string basicMetaJson = "[ 'default' ]";
            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string defaultContents = "<html />";

            string fn2 = Path.Combine(rootFolder, "default.html");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(defaultContents);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                Assert.AreEqual(fn2, page.Filename);
                Assert.AreEqual("default.html", page.RelativeFilename);
                Assert.AreEqual("default.html", page.RelativeOutputFilename);
                Assert.AreEqual("default.html", page.Id);
                Assert.AreEqual("default", page.Title);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void FindsFileWithHtmExtensionSuccess()
        {
            var pageLoader = new OrderedContentPageLoader();

            string basicMetaJson = "[ 'default' ]";
            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string defaultContents = "<html />";

            string fn2 = Path.Combine(rootFolder, "default.htm");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(defaultContents);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                Assert.AreEqual(fn2, page.Filename);
                Assert.AreEqual("default.htm", page.RelativeFilename);
                Assert.AreEqual("default.htm", page.RelativeOutputFilename);
                Assert.AreEqual("default.htm", page.Id);
                Assert.AreEqual("default", page.Title);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void FindsFileWithAspxExtensionSuccess()
        {
            var pageLoader = new OrderedContentPageLoader();

            string basicMetaJson = "[ 'default' ]";
            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string defaultContents = "<html />";

            string fn2 = Path.Combine(rootFolder, "default.aspx");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(defaultContents);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                Assert.AreEqual(fn2, page.Filename);
                Assert.AreEqual("default.aspx", page.RelativeFilename);
                Assert.AreEqual("default.aspx", page.RelativeOutputFilename);
                Assert.AreEqual("default.aspx", page.Id);
                Assert.AreEqual("default", page.Title);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void NoTitleUsesFilename()
        {
            var pageLoader = new OrderedContentPageLoader();

            string basicMetaJson = "[ 'default' ]";

            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string defaultContents = "---\r\n" +
                "id: 1234\r\n" +
                "---\r\n" +
                "# Content goes here\r\n";

            string fn2 = Path.Combine(rootFolder, "default.md");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(defaultContents);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                Assert.AreEqual("1234", page.Id);
                Assert.AreEqual("default", page.Title);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void GetTitleFromPageYaml()
        {
            var pageLoader = new OrderedContentPageLoader();

            string basicMetaJson = "[ 'default' ]";

            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string title = "This is the webpage title";
            string mdContents = "---\r\n" +
                "title: " + title + "\r\n" +
                "---\r\n" +
                "# Content goes here\r\n";

            string fn2 = Path.Combine(rootFolder, "default.md");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(mdContents);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                var pageState = new PageVariables();
                pageState.InitializeFor(page, "");

                Assert.AreEqual(title, page.GetMetadata().Title);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }
    }
}
