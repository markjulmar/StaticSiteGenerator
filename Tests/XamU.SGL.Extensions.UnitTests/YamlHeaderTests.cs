using MDPGen.Core;
using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace XamU.SGL.Extensions.UnitTests
{
    [TestClass]
    public class YamlHeaderTests
    {
        const string data = 
@"---
id:    123-234-345-456-77799
title: This is a test page title
nav-title: This is the navigation title

tokens:
    - key:   ABC
      value: One Two Three
    - key:   123
      value: A, B, C

links:
    - title: Frequently Asked Questions
      group: First Block
      description: >
        This is a test of wrapping lines
        which can span multiple lines in the YAML document.
      url: https://www.xamarin.com
    - title: What about iOS?
      url: https://developer.xamarin.com/ios 
    - group: Other Resources
      title: Another title
      url: https://www.anotherlink.com
---
## This is the page contents
";

        [TestInitialize]
        public void Init()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void CheckYamlHeaderWithLinks()
        {
            var pageLoader = new OrderedContentPageLoader
            {
                MetadataLoader = new XamUPageMetadataLoader()
            };

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

                Assert.AreEqual(3, page.GetMetadata<XamUMetadata>().Links.Count);

                var link = page.GetMetadata<XamUMetadata>().Links[0];
                Assert.AreEqual("Frequently Asked Questions", link.Title);
                Assert.AreEqual("This is a test of wrapping lines which can span multiple lines in the YAML document.\n", link.Description);
                Assert.AreEqual("First Block", link.Group);
                Assert.AreEqual("https://www.xamarin.com", link.Url);
                link = page.GetMetadata<XamUMetadata>().Links[1];
                Assert.AreEqual("What about iOS?", link.Title);
                Assert.AreEqual(null, link.Description);
                Assert.AreEqual("First Block", link.Group);
                Assert.AreEqual("https://developer.xamarin.com/ios", link.Url);
                link = page.GetMetadata<XamUMetadata>().Links[2];
                Assert.AreEqual("Another title", link.Title);
                Assert.AreEqual(null, link.Description);
                Assert.AreEqual("https://www.anotherlink.com", link.Url);
                Assert.AreEqual("Other Resources", link.Group);
            }
            finally
            {
                File.Delete(fn1);
                File.Delete(fn2);
            }
        }
    }
}
