using MDPGen.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XamU.SGL.Extensions;

namespace MDPGen.UnitTests
{
    [TestClass]
    public class MarkdownTests
    {
        private IMarkdownParser markdownParser;

        [TestInitialize]
        public void Initialize()
        {
            markdownParser = new XamUMarkdownParser();
        }

        [TestMethod]
        public void ParagraphOnInnerHtmlContentWithPLiteral()
        {
            string markdownSource = "<h3><p>Test</p></h3>";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<h3>\n<p>\nTest\n</p>\n</h3>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void JustTextGeneratesParagraph()
        {
            string markdownSource = "This is a test.";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<p>This is a test.</p>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TagsWithTextGeneratesParagraph()
        {
            string markdownSource = "\r\nIn this exercise.\r\n\r\n[Download](./assets/and101-ex1-completed.zip) {.btn .btn-info }\r\n";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<p>In this exercise.</p>\n<a href=\"./assets/and101-ex1-completed.zip\" class=\"btn btn-info\">Download</a>\n";
            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        public void InnerHtmlWithMultipleChildrenHasParagraph()
        {
            string markdownSource = "<ide>Test.\r\n<h1>Test #1</h1>\r\nTest #2</ide>";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<ide>\n<p>Test.</p>\n<h1>\nTest #1\n</h1>\n<p>Test #2</p>\n</ide>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void NoParagraphOnInnerHtmlContent()
        {
            string markdownSource = "<h3>Test</h3>";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<h3>\nTest\n</h3>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void EmitTableWithClassTag()
        {
            string markdownSource = "| Platform                       | File                    | Project | {.table .table-striped}\r\n| -------------                  | -------------           | ------  |\r\n| iOS                            | PhoneDialer.iOS.cs      | iOS |\r\n| Android                        | PhoneDialer.Droid.cs    | Android                 |\r\n| Windows Phone 8                | PhoneDialer.WinPhone.cs | WinPhone |\r\n| Windows Store                  | PhoneDialer.Windows.cs  | Windows |\r\n| Universal Windows Platform     | PhoneDialer.UWP.cs | UWP |\r\n";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<table class=\"table table-striped\">";
            Assert.IsTrue(result.StartsWith(expected));
        }

        [TestMethod]
        public void EmitMarkTagWithEquals()
        {
            string markdownSource =
                "Test\r\n==Test==\r\n## Test\r\n";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<p>Test\r\n<mark>Test</mark></p>\n<h2>Test</h2>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void EmitRawHtmlBlockWithNoMarkdown()
        {
            string markdownSource =
                "<div class=\"btn-toolbar spacing-top\">\r\n\t<a class=\"btn btn-purple btn-nav\" role=\"button\" href=\"{{baseUrl}}/profile\"><span class=\"glyphicon glyphicon-user\"></span> Profile</a>\r\n\t<a class=\"btn btn-primary btn-nav\" role=\"button\" href=\"{{nextCourseUrl}}\">Next Course &#8680;</a>\r\n</div>\r\n";
            string result = markdownParser.Transform(markdownSource);
            string expected = "<div class=\"btn-toolbar spacing-top\">\n<a class=\"btn btn-purple btn-nav\" role=\"button\" href=\"{{baseUrl}}/profile\"><span class=\"glyphicon glyphicon-user\"></span> Profile</a>\n<a class=\"btn btn-primary btn-nav\" role=\"button\" href=\"{{nextCourseUrl}}\">Next Course &#8680;</a>\n</div>\n";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IdeTagCheck()
        {
            string markdownSource =
                "This is a test\r\n<ide name=\"vs\">\r\nSome **More** Text\r\n</ide>\r\nAnd some ending text.\r\n";

            string result = markdownParser.Transform(markdownSource);

            string expected = "<p>This is a test</p>\n<ide name=\"vs\">\nSome <strong>More</strong> Text\n</ide>\n<p>And some ending text.</p>\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IdeTagWithNumberedListCheck()
        {
            string markdownSource =
                "This is a test\r\n<ide name=\"vs\">\r\n1. Item 1\r\n2. Item 2\r\n</ide>\r\nAnd some ending text.\r\n";

            string result = markdownParser.Transform(markdownSource);

            string expected = "<p>This is a test</p>\n<ide name=\"vs\">\n<ol>\n<li>Item 1</li>\n<li>Item 2</li>\n</ol>\n</ide>\n<p>And some ending text.</p>\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MarkdownInHtmlProducesHtml()
        {
            string markdownSource = "<div class=\"center\">" +
                                    "**Note:** this is a test of _markdown_ rendering.\r\n" +
                                    "</div>\r\n";
            string result = markdownParser.Transform(markdownSource);

            string expected = "<div class=\"center\">\n" +
                              "<strong>Note:</strong> this is a test of <em>markdown</em> rendering.\n" + "</div>\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ParseAndRenderMarkdownSuccess()
        {
            string markdownSource_Normal = "# Header\r\n" +
                                           "\r\n" +
                                           "[Test Link](#someFile)\r\n" +
                                           "~~~\r\n" +
                                           "class Test {}\r\n" +
                                           "~~~\r\n";

            string result = markdownParser.Transform(markdownSource_Normal);

            string expected = "<h1>Header</h1>\n" +
                "<a href=\"#someFile\">Test Link</a>\n" +
                "<pre class=\"prettyprint-collapse\"><code>class Test {}\n" +
                "</code></pre>\n\n";

            Assert.AreEqual(expected, result);
        }


    }
}
