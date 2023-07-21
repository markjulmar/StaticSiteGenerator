using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class IfDirectiveTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StaticSiteGenerator.RegisterDefaultServices();
        }

        [TestMethod]
        public void NoIfBlockReturnsSameData()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();
            string result = new IfDirective().Process(pageVars, input);

            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void MissingTokenIfBlockSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if {{test}}]]\r\n**Ignore Me**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();
            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IfBlockWithValidTokenSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if {{test}}]]\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();
            pageVars.Tokens.Add("test", "1");

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IfBlockWithValidEnvVarSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if %test%]]\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();

            Environment.SetEnvironmentVariable("test", "1");
            try
            {
                string result = new IfDirective().Process(pageVars, input);
                string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

                Assert.AreEqual(expected, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("test", null);
            }
        }

        [TestMethod]
        public void IfBlockWithNotBuildSymbolDefinedSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if !TEST]]\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables(null, null, new List<string> { "TEST" });

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IfBlockWithNotBuildSymbolSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if !TEST]]\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IfBlockWithValidBuildSymbolSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if TEST]]\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables(null, null, new List<string>() { "TEST" });

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void InnerIfIsIgnoredWhenOuterIfFails()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if TEST]]\r\n**I'm Missing**\r\n[[if {{test}}]]\r\n**I should be missing too**\r\n[[endif]]\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();
            pageVars.Tokens.Add("test", "1");

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void InnerIfIsProcessedWhenOuterIfSucceeds()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if {{test}}]]\r\n**I'm Here**\r\n[[if TEST]]\r\n**I should be missing**\r\n[[endif]]\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();
            pageVars.Tokens.Add("test", "1");

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DoubleIfIsChecked()
        {
            string input = "# This is a header\r\nWelcome to my house.\r\n[[if {{test}}]]\r\n[[if TEST]]\r\n**I should be there**\r\n[[endif]]\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables(null, null, new List<string>() { "TEST" });
            pageVars.Tokens.Add("test", "1");

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I should be there**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MissingIfWritesLogEvent()
        {
            List<string> logLines = new List<string>();
            TraceLog.OutputHandler += (t, s) => logLines.Add(s);

            string input = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n[[endif]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n**I'm Here**\r\n## Footer\r\n";

            Assert.AreEqual(expected, result);
            Assert.AreEqual(1, logLines.Count);
            Assert.IsTrue(logLines[0].StartsWith("ENDIF directive found in"));
        }

        [TestMethod]
        public void MissingEndifWritesLogEvent()
        {
            List<string> logLines = new List<string>();
            TraceLog.OutputHandler += (t,s) => { logLines.Add(s); };

            string input = "# This is a header\r\nWelcome to my house.\r\n[[if TEST]]\r\n**I'm Here**\r\n[[end]]\r\n## Footer\r\n";
            PageVariables pageVars = new PageVariables();

            string result = new IfDirective().Process(pageVars, input);
            string expected = "# This is a header\r\nWelcome to my house.\r\n";

            Assert.AreEqual(expected, result);
            Assert.AreEqual(1, logLines.Count);
            Assert.IsTrue(logLines[0].StartsWith("IF directive not closed in"));
        }
    }
}
