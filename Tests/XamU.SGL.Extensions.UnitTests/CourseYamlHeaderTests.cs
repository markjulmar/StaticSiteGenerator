using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XamU.SGL.Extensions.UnitTests
{
    [TestClass]
    public class CourseYamlHeaderTests
    {
        [TestMethod]
        public void RootNodeCanBeCourse()
        {
            var pageLoader = new OrderedContentPageLoader {
                MetadataLoader = new XamUPageMetadataLoader()
            };

            string metaJson = "['default']";

            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(metaJson);
            }

            string markdown = "---\n"
                + "topicId: 1\n"
                + "creditSlug: aaa\n"
                + "---\n"
                + "# Header";

            string fn2 = Path.Combine(rootFolder, "default.md");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(markdown);
            }

            try
            {
                var root = pageLoader.LoadAsync(rootFolder).Result;

                Assert.IsTrue(root.IsCourse());
                Assert.AreEqual(1, root.GetMetadata<XamUMetadata>().TopicId);
                Assert.AreEqual("aaa", root.GetMetadata<XamUMetadata>().CreditSlug);
                Assert.AreEqual(1, root.Enumerate().Count());
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void CheckCourseSlugAvailableOnSubFolder()
        {
            var pageLoader = new OrderedContentPageLoader
            {
                MetadataLoader = new XamUPageMetadataLoader()
            };

            string basicMetaJson = "[ 'default', 'xam100' ]";

            string rootFolder = Path.GetTempPath();
            string folder = Path.Combine(rootFolder, "xam100");
            Directory.CreateDirectory(folder);

            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string innerJson = "[ 'default' ]";
            string fn2 = Path.Combine(folder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(innerJson);
            }

            string markdownFile = "---\n"
                + "creditSlug: xam101\n"
                + "topicId: 1\n"
                + "---\n"
                + "# Header";

            string fn3 = Path.Combine(folder, "default.md");
            using (var sw = new StreamWriter(fn3))
            {
                sw.WriteLine(markdownFile);
            }

            try
            {
                var root = pageLoader.LoadAsync(rootFolder).Result;

                Assert.AreEqual(2, root.Enumerate().Count());
                var courseNode = root.Children[0].GetCourseOwner();
                Assert.AreEqual(1, courseNode.GetMetadata<XamUMetadata>().TopicId);
                Assert.AreEqual("xam101", courseNode.GetMetadata<XamUMetadata>().CreditSlug);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
                File.Delete(fn3);
                Directory.Delete(folder);
            }
        }

        [TestMethod]
        public void GetCoursesWalksMetaFilesProperly()
        {
            var pageLoader = new OrderedContentPageLoader
            {
                MetadataLoader = new XamUPageMetadataLoader()
            };

            string[] folders = { "test101", "test102", "test103" };
            var files = new List<string>();

            string rootFolder = Path.GetTempPath();
            string rootFile = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);

            var rootMetaJson = new StringBuilder("[");
            for (int i = 0; i < folders.Length; i++)
            {
                if (i > 0)
                    rootMetaJson.Append(",");
                rootMetaJson.Append("'" + folders[i] + "'");
                var dirName = Path.Combine(rootFolder, folders[i]);
                Directory.CreateDirectory(dirName);
                var filename = Path.Combine(dirName, pageLoader.DirectoryInfoFilename);
                files.Add(filename);

                string courseJson = "[ 'default', 'page1', 'page2', 'page3' ]";
                File.WriteAllText(filename, courseJson);

                var dataFn = Path.Combine(dirName, "default.md");
                files.Add(dataFn);
                File.WriteAllText(dataFn, "---\nid: default.md\ntopicId: -1\ncreditSlug: aaa\n---\n\n");
            }

            rootMetaJson.Append("]");

            File.WriteAllText(rootFile, rootMetaJson.ToString());

            try
            {
                var root = pageLoader.LoadAsync(rootFolder).Result;
                Assert.AreEqual(1 + 4 * folders.Length, root.Enumerate().Count());
                Assert.AreEqual(3, root.GetCourses().Count);
            }
            finally
            {
                File.Delete(rootFile);
                foreach (var f in files)
                    File.Delete(f);
                foreach (var f in folders)
                    Directory.Delete(Path.Combine(rootFolder, f), true);
            }
        }
    }
}
