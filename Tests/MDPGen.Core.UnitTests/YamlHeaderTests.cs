using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class YamlHeaderTests
    {
        const string data = 
@"---
id:    123-234-345-456-77799
title: This is a test page title

tokens:
    - key:   ABC
      value: One Two Three
    - key:   123
      value: A, B, C
---
## This is the page contents
";

        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void CheckYamlHeaderWithContentsShouldAddTokens()
        {
            var pageLoader = new OrderedContentPageLoader();

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
                sw.WriteLine(data);
            }

            try
            {
                var page = pageLoader.LoadAsync(rootFolder).Result;

                var pageState = new PageVariables();
                pageState.InitializeFor(page, "");

                Assert.AreEqual("## This is the page contents\r\n\r\n", page.Content);
                Assert.AreEqual("One Two Three", pageState.Tokens["ABC"]);
                Assert.AreEqual("A, B, C", pageState.Tokens["123"]);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void InitialBlankLinesAreSkipped()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc = "\r\n  \r\n\t\t  \r\n" + data;
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
                var page = pageLoader.LoadAsync(rootFolder).Result;

                var pageState = new PageVariables();
                pageState.InitializeFor(page, "");

                Assert.AreEqual("## This is the page contents\r\n\r\n", page.Content);
                Assert.AreEqual("One Two Three", pageState.Tokens["ABC"]);
                Assert.AreEqual("A, B, C", pageState.Tokens["123"]);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void NoPageTemplateInHeaderUsesDefault()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc =
                "---\n" +
                "id: default.md\n" +
                "title: test\n" +
                "---\n" +
                "# Title goes here\n" +
                " \n";
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

                var pageVars = new PageVariables("defaultTemplate.html");
                pageVars.InitializeFor(root);

                Assert.AreEqual("defaultTemplate.html", root.PageTemplate);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void NoneTemplateReturnsNull()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc =
                "---\n" +
                "id: default.md\n" +
                "title: test\n" +
                "template: none\n" +
                "---\n" +
                "# Title goes here\n" +
                " \n";
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

                var pageVars = new PageVariables("defaultTemplate.html");
                pageVars.InitializeFor(root);

                Assert.AreEqual(null, root.PageTemplate);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void PageTemplateInHeaderIsAddedToContentPage()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc = 
                "---\n" +
                "id: default.md\n" +
                "title: test\n" +
                "template: newTemplate.html\n" +
                "---\n" +
                "# Title goes here\n" +
                " \n";
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
                Assert.AreEqual("newTemplate.html", root.PageTemplate);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }

        [TestMethod]
        public void MissingEndTagInYamlThrowsException()
        {
            var pageLoader = new OrderedContentPageLoader();

            string markdownSrc = "---\n" +
                                 "pageTitle: This is a test\n" +
                                 "\n" +
                                 "secondValue: Another test\n" +
                                 "# Title goes here\n" +
                                 " \n";
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
                Assert.ThrowsException<FormatException>(() => 
                    pageLoader.LoadAsync(rootFolder).GetAwaiter().GetResult());
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }
    }
}
