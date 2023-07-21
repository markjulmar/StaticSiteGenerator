using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MDPGen.Core.UnitTests.CoreTests
{
    [TestClass]
    public class PageLoaderTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void NullContentFolderThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => 
                new OrderedContentPageLoader().LoadAsync(null)
                    .GetAwaiter().GetResult());
        }

        [TestMethod]
        public void MissingContentFolderThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => 
                new OrderedContentPageLoader().LoadAsync(@"test_none")
                    .GetAwaiter().GetResult());
        }

        [TestMethod]
        public void TitleUsedAsEntryName()
        {
            var pageLoader = new OrderedContentPageLoader();

            const string basicMetaJson = "[ 'default' ]";

            using (var folder = TempDir.Create())
            {
                var fn = new TempFile(
                    Path.Combine(folder.Name, pageLoader.DirectoryInfoFilename), 
                    basicMetaJson);

                var defaultFile = new TempFile(
                    Path.Combine(folder.Name, "default.md"),
                    "# header");

                var root = pageLoader.LoadAsync(folder.Name).Result;

                Assert.AreEqual(Path.Combine(folder.Name, "default.md"), root.Filename);
                Assert.AreEqual("default.md", root.Id);
                Assert.IsTrue(root.IsDefaultPage);
                Assert.AreEqual("default.md", root.RelativeFilename);
                Assert.AreEqual("/", root.Url);
            }
        }

        [TestMethod]
        public void CheckFullAndRelativePathToLoadedFileEntries()
        {
            var pageLoader = new OrderedContentPageLoader();
            const string basicMetaJson = "[ 'default', 'two', 'three', 'four', 'five' ]";

            string folder = Path.GetTempPath();
            string fn = Path.Combine(folder, pageLoader.DirectoryInfoFilename);

            using (var sw = new StreamWriter(fn))
            {
                sw.WriteLine(basicMetaJson);
            }

            try
            {
                var root = pageLoader.LoadAsync(folder).Result;

                Assert.AreEqual(5, root.Enumerate().Count());
                var pageTwo = root.Children[0];

                Assert.AreEqual("two.md", pageTwo.RelativeFilename);
                Assert.AreEqual(Path.Combine(folder, "two.md"), pageTwo.Filename);
                Assert.AreEqual("/two", pageTwo.Url);

            }
            finally
            {
                File.Delete(fn);
            }
        }

        [TestMethod]
        public void CheckFullAndRelativePathToLoadedDirectoryEntries()
        {
            var pageLoader = new OrderedContentPageLoader();

            const string folderName = "innerTest";
            const string basicMetaJson = "[ 'default', '" + folderName + "' ]";
            const string innerMetaJson = "[ 'default', 'two', 'three', 'four', 'five' ]";

            using (var folder = TempDir.Create())
            {
                var fn = new TempFile(
                    Path.Combine(folder.Name, 
                    pageLoader.DirectoryInfoFilename), basicMetaJson);

                var defaultFile = new TempFile(
                    Path.Combine(folder.Name, "default.md"), "# Hello");

                using (var innerFolder = new TempDir(Path.Combine(folder.Name, folderName)))
                {
                    var fn2 = new TempFile(
                        Path.Combine(innerFolder.Name,
                            pageLoader.DirectoryInfoFilename), innerMetaJson);

                    var defaultFile2 = new TempFile(
                        Path.Combine(innerFolder.Name, "default.md"), "# Hello");

                    var files = new[] {"two.md", "three.md", "four.md", "five.md"}
                        .Select(f => new TempFile(Path.Combine(innerFolder.Name, f), "# Hello"))
                        .ToList();

                    var root = pageLoader.LoadAsync(folder.Name).Result;

                    Assert.AreEqual(6, root.Enumerate().Count());
                    Assert.AreEqual(4, root.Children[0].Children.Count);

                    var pageTwo = root.Children[0].Children[0];
                    Assert.AreEqual(Path.Combine(folderName, "two.md"), pageTwo.RelativeFilename);
                    Assert.AreEqual(Path.Combine(innerFolder.Name, "two.md"), pageTwo.Filename);
                    Assert.AreEqual("/" + folderName + "/two", pageTwo.Url);
                }
            }
        }

        [TestMethod]
        public void UseDifferentMetaJsonNameSuccess()
        {
            var pageLoader = new OrderedContentPageLoader
            {
                DirectoryInfoFilename = "test.txt"
            };

            const string basicMetaJson = "[ 'default' ]";

            using (var folder = TempDir.Create())
            {
                var fn1 = new TempFile(
                    Path.Combine(folder.Name, 
                        pageLoader.DirectoryInfoFilename), basicMetaJson);
                var fn2 = new TempFile(
                    Path.Combine(folder.Name, "default.md"), "# Hello");

                var root = pageLoader.LoadAsync(folder.Name).Result;

                Assert.AreEqual(1, root.Enumerate().Count());
                Assert.AreEqual(Path.Combine(folder.Name, "default.md"), root.Filename);
                Assert.AreEqual("default.md", root.RelativeFilename);
                Assert.AreEqual("/", root.Url);

            }
        }
    }
}
