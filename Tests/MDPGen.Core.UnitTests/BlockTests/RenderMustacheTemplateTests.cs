using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class RenderMustacheTemplateTests
    {
        const string bodyMarker = "{{!body}}";

        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void NoPageTemplateReturnsUnchangedInput()
        {
            string input = "# Test\nThis is a test.\n";
            PageVariables pageVars = new PageVariables();
            pageVars.InitializeFor(new ContentPage());

            string result = new RenderMustachePageTemplate().Process(pageVars, input);
            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void PageTemplateInsertionSucceeds()
        {
            string fn = Path.GetTempFileName();

            try
            {
                using (var sw = new StreamWriter(fn))
                {
                    sw.WriteLine("--Start--");
                    sw.WriteLine(bodyMarker);
                    sw.WriteLine("--End--");
                }

                string input = "# Test\nThis is a test.\n";
                PageVariables pageVars = new PageVariables();
                pageVars.InitializeFor(new ContentPage {
                    PageTemplate = fn
                });

                string result = new RenderMustachePageTemplate().Process(pageVars, input);
                string expected = "--Start--\r\n" + input + "\r\n--End--\r\n";
                Assert.AreEqual(expected, result);
            }
            finally
            {
                File.Delete(fn);
            }
        }

        [TestMethod]
        public void PageTemplateFoundAlongPathSucceds()
        {
            string fn = Path.GetTempFileName();

            try
            {
                using (var sw = new StreamWriter(fn))
                {
                    sw.WriteLine("--Start--");
                    sw.WriteLine(bodyMarker);
                    sw.WriteLine("--End--");
                }

                string input = "# Test\nThis is a test.\n";
                PageVariables pageVars = new PageVariables(null, null, searchFolders: new List<string>() { ".", "\\", "temp", Path.GetDirectoryName(fn) });
                pageVars.InitializeFor(new ContentPage {
                    PageTemplate = Path.GetFileName(fn)
                });

                string result = new RenderMustachePageTemplate().Process(pageVars, input);
                string expected = "--Start--\r\n" + input + "\r\n--End--\r\n";
                Assert.AreEqual(expected, result);
            }
            finally
            {
                File.Delete(fn);
            }
        }

        [TestMethod]
        public void InvalidCharsInPathWritesLog()
        {
            var logList = new List<string>();
            TraceLog.OutputHandler += (t, s) => logList.Add(s);

            string fn = Path.GetTempFileName();

            try
            {
                using (var sw = new StreamWriter(fn))
                {
                    sw.WriteLine("--Start--");
                    sw.WriteLine(bodyMarker);
                    sw.WriteLine("--End--");
                }

                string input = "# Test\nThis is a test.\n";
                PageVariables pageVars = new PageVariables(null, null, searchFolders: new List<string>() { "f|>\n", Path.GetDirectoryName(fn) });
                pageVars.InitializeFor(new ContentPage {
                    PageTemplate = Path.GetFileName(fn)
                });

                string result = new RenderMustachePageTemplate().Process(pageVars, input);
                string expected = "--Start--\r\n" + input + "\r\n--End--\r\n";
                Assert.AreEqual(expected, result);

                Assert.AreEqual(1, logList.Count);
                Assert.IsTrue(logList[0].StartsWith("Invalid path specified to FindFileAlongPath:"));
            }
            finally
            {
                File.Delete(fn);
            }
        }

        [TestMethod]
        public void InvalidPageTemplateFileNameThrowsException()
        {
            string fn = @"/:>test.txt";

            string input = "# Test\nThis is a test.\n";
            PageVariables pageVars = new PageVariables();
            pageVars.InitializeFor(new ContentPage {
                PageTemplate = fn
            });

            Assert.ThrowsException<ArgumentException>(() => new RenderMustachePageTemplate().Process(pageVars, input));
        }

        [TestMethod]
        public void PageTemplateNotFoundAlongPathThrowsException()
        {
            string fn = "test.txt";

            string input = "# Test\nThis is a test.\n";
            PageVariables pageVars = new PageVariables(null, null, searchFolders: new List<string>() { ".", "\\", "temp" });
            pageVars.InitializeFor(new ContentPage {
                PageTemplate = fn
            });

            Assert.ThrowsException<Exception>(() => new RenderMustachePageTemplate().Process(pageVars, input));
        }

        [TestMethod]
        public void NonePageTemplateReturnsRawContent()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc =
                "---\n" +
                "id: one\n" +
                "title: One\n" +
                "template: none\n" +
                "---\n" +
                "# Title goes here";

            string basicMetaJson = "[ 'default' ]";

            string rootFolder = Path.GetTempPath();
            string fn1 = Path.Combine(rootFolder, pageLoader.DirectoryInfoFilename);
            using (var sw = new StreamWriter(fn1))
            {
                sw.WriteLine(basicMetaJson);
            }

            string fn2 = Path.Combine(rootFolder, "default.md");
            using (var sw = new StreamWriter(fn2))
            {
                sw.WriteLine(markdownSrc);
            }

            try
            {
                var root = pageLoader.LoadAsync(rootFolder).Result;
                var pageVars = new PageVariables("defaultTemplate");
                pageVars.InitializeFor(root);

                string result = new RenderMustachePageTemplate().Process(pageVars, root.Content);
                string expected = "# Title goes here\r\n";
                Assert.AreEqual(expected, result);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }
    }
}
