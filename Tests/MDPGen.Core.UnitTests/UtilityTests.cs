using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace MDPGen.Core.UnitTests
{
    [TestClass]
    public class UtilityTests
    {
        [TestMethod]
        public void CheckUrlCombineTrimsAllSlashes()
        {
            string root = "http://www.test.com/";
            string[] parts = { "/one/", "/two/", "/three/" };

            string result = root.UrlCombine(parts);
            string expected = "http://www.test.com/one/two/three";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RecursiveCopySkipsDesignatedFilesFromFilter()
        {
            string folder = Path.GetTempPath();
            string source = Path.Combine(folder, "_xt1");
            string dest = Path.Combine(folder, "_xt2");

            if (Directory.Exists(source))
                Directory.Delete(source, true);
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);

            Directory.CreateDirectory(source);

            string[] files = { "one.md", "two.md", "three.md" };
            foreach (var f in files)
            {
                File.WriteAllText(Path.Combine(source, f), "<html/>");
            }

            try
            {
                Utilities.RecursiveCopyFolder(
                    new DirectoryInfo(source),
                    new DirectoryInfo(dest),
                    (fn,tfn) => Path.GetFileName(fn) != files[1]);

                var result = Directory.GetFiles(dest);
                Assert.AreEqual(2, result.Length);
                Assert.AreEqual("<html/>", File.ReadAllText(result[0]));
                Assert.IsFalse(result.Any(f => Path.GetFileName(f) == files[1]));
            }
            finally
            {
                Directory.Delete(source, true);
                Directory.Delete(dest, true);
            }
        }

        [TestMethod]
        public void VerifyNormalizedFilename()
        {
            string folder = ".\\test/output";
            string fn = "test.txt";

            string result = Utilities.CreateNormalizedFilename(folder, fn);
            Assert.AreEqual("test\\output\\test.txt", result);
        }
    }
}
