using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MDPGen.Core.UnitTests
{
    [TestClass]
    public class FolderContentPageLoaderTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void FindsFileWithMarkdownExtensionSuccess()
        {
            string[] files = { "one.md", "two.md", "three.md", "default.md" };
            using (var dir = TempDir.Create())
            {
                TempFile.Create(dir.Name, files, "# Hello");

                var pageLoader = new FolderContentPageLoader();
                var page = pageLoader.LoadAsync(dir.Name).Result;
                Assert.AreEqual(3, page.Children.Count);
                Assert.AreEqual("/one", page.Children[0].Url);
                Assert.AreEqual("one.html", page.Children[0].RelativeOutputFilename);
                string[] result = files.OrderBy(fn => fn).Skip(1).ToArray();
                for (var index = 0; index < result.Length; index++)
                {
                    var item = result[index];
                    Assert.AreEqual(item, page.Children[index].RelativeFilename);
                }
            }
        }

        [TestMethod]
        public void FindsFileWithWildcardExtensionSuccess()
        {
            string[] files = { "one.md", "two.html", "three.aspx" };
            using (var dir = TempDir.Create())
            {
                TempFile.Create(dir.Name, files, "<h1>Hello</h1>\r\n");

                var pageLoader = new FolderContentPageLoader {Filespec = "*.*"};
                var page = pageLoader.LoadAsync(dir.Name).Result;
                Assert.AreEqual(3, page.Children.Count);
                Assert.AreEqual("/one", page.Children[0].Url);
                Assert.AreEqual("one.html", page.Children[0].RelativeOutputFilename);
                string[] result = files.OrderBy(fn => fn).ToArray();
                for (var index = 0; index < result.Length; index++)
                {
                    var item = result[index];
                    Assert.AreEqual(item, page.Children[index].RelativeFilename);
                }
            }
        }
    }
}
