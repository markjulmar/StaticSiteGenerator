using System;
using MDPGen.Core;
using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MDPGen.UnitTests.ExtensionTests
{
    [TestClass]
    public class RunExtensionTests
    {
        private PageVariables pageVars;

        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
            pageVars = new PageVariables();
        }

        [TestMethod]
        public void MissingExtensionThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => new RunMarkdownExtensions().Process(pageVars, "@NoExtensionFound()"));
        }

        [TestMethod]
        public void EscapedMarkerIsIgnored()
        {
            string input = "# Test\r\n@@IgnoreMe!\r\n@@Ignore('Me too')\r\n@ and me## End";
            string result = new RunMarkdownExtensions().Process(pageVars, input);
            string expected = input + "\r\n";

            Assert.AreEqual(expected, result);
        }

        class NoParamsExtension : IMarkdownExtension
        {
            public string Process(IServiceProvider provider)
            {
                return "Hello from the Extension";
            }
        }

        [TestMethod]
        public void NoParametersSuccess()
        {
            string markdownSource = "# No parameters test\r\n" +
                        "@NoParamsExtension()\r\n" +
                        "## End";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(NoParamsExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "# No parameters test\r\nHello from the Extension## End\r\n";

            Assert.AreEqual(expected, actual);
        }

        class TwoStringParamsExtension : IMarkdownExtension
        {
            private readonly string v1;
            private readonly string v2;

            public TwoStringParamsExtension(string v1, string v2)
            {
                this.v1 = v1;
                this.v2 = v2;
            }
            public string Process(IServiceProvider provider)
            {
                return $"{this.v1} - {this.v2}";
            }
        }

        class EchoBoolExtension : IMarkdownExtension
        {
            private readonly bool v1;

            public EchoBoolExtension(bool v1)
            {
                this.v1 = v1;
            }
            public string Process(IServiceProvider provider)
            {
                return v1.ToString();
            }
        }

        [TestMethod]
        public void BooleanParamSuccess()
        {
            string markdownSource = "@EchoBool(true)\r\n@EchoBool(false)\r\n";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(EchoBoolExtension));
            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "TrueFalse";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IgnoreRestOfLineSuccess()
        {
            string markdownSource = "@EchoBool(true)  // This is a comment - all will be ignored.";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(EchoBoolExtension));
            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "True";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LiteralAtSignInlineSuccess()
        {
            string markdownSource = "# Header @Inline";

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "# Header @Inline\r\n";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EscapedAtSignStartSuccess()
        {
            string markdownSource = "@ Header";

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "@ Header\r\n";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TwoStringsParametersSuccess()
        {
            string markdownSource = "# 2 string test\r\n" +
                        "@TwoStringParamsExtension(\"value1\", 'value2')\r\n" +
                        "## End";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(TwoStringParamsExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "# 2 string test\r\nvalue1 - value2## End\r\n";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultilineParametersSuccess()
        {
            string markdownSource = "@TwoStringParamsExtension(\r\n\"value1\",\r\n'value2')\r\n";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(TwoStringParamsExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "value1 - value2";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultilineParametersWithEmbeddedCRSuccess()
        {
            string markdownSource = "@TwoStringParamsExtension(\r\n" +
                                    "                         \"value\n1\",\r\n" +
                                    "                         'value\r2')\r\n";

            ExtensionProcessor.Init(typeof(TwoStringParamsExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "value\n1 - value\r2";

            Assert.AreEqual(expected, actual);
        }
        class NumberAndStringParamsExtension : IMarkdownExtension
        {
            private readonly double num1;
            private readonly string v2;

            public NumberAndStringParamsExtension(double num1, string v2)
            {
                this.num1 = num1;
                this.v2 = v2;
            }
            public string Process(IServiceProvider provider)
            {
                return $"{num1} - {v2}";
            }
        }

        [TestMethod]
        public void NumberAndStringParametersSuccess()
        {
            string markdownSource = "@NumberAndStringParamsExtension(100.50, 'value2')";

            ExtensionProcessor.Init(typeof(NumberAndStringParamsExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "100.5 - value2";

            Assert.AreEqual(expected, actual);

        }

        class TwoNumbersExtension : IMarkdownExtension
        {
            private readonly int n1;
            private readonly double n2;

            public TwoNumbersExtension(int n1, double n2)
            {
                this.n1 = n1;
                this.n2 = n2;
            }
            public string Process(IServiceProvider provider)
            {
                return $"{n1} - {n2}";
            }
        }

        [TestMethod]
        public void IntegerAndDoubleParametersSuccess()
        {
            string markdownSource = "@TwoNumbers(100, 500.75)";

            ExtensionProcessor.Init(typeof(TwoNumbersExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "100 - 500.75";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoubleAndIntegerParametersShouldThrowException()
        {
            string markdownSource = "@TwoNumbers(100.5, 500)";

            ExtensionProcessor.Init(typeof(TwoNumbersExtension));
            Assert.ThrowsException<ArgumentException>(() => new RunMarkdownExtensions().Process(pageVars, markdownSource));

        }
    }
}
