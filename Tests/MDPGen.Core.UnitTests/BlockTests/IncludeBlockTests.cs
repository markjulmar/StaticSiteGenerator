using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;

namespace MDPGen.Core.UnitTests.BlockTests
{
    [TestClass]
    public class IncludeBlockTests
    {
        private string markdownSource_IncludeFile = "# Header\r\n\r\n{0}\r\n## Header 2\r\n";

        [TestMethod]
        public void TestIncludeFileSuccess()
        {
            const string inlineContents = "# Another H1 block!";
            string includeFile = Path.GetTempFileName();
            string markdownContent = string.Format(markdownSource_IncludeFile, $"[[include={includeFile}]]");

            using (var writer = File.CreateText(includeFile))
            {
                writer.Write(inlineContents);
            }

            try
            {
                string output = new IncludeDirective().Process(new PageVariables(), markdownContent);
                string expected = string.Format(markdownSource_IncludeFile, inlineContents);
                Assert.AreEqual(expected, output);
            }
            finally
            {
                File.Delete(includeFile);
            }
        }

        [TestMethod]
        public void TestIncludeFileWithReplacementTokenAndMissingTokenThrowsException()
        {
            string markdownContent = string.Format(markdownSource_IncludeFile, "[[include={{includeFile}}]]");
            Assert.ThrowsException<Exception>(() => new IncludeDirective().Process(new PageVariables(), markdownContent));
        }

        [TestMethod]
        public void TestIncludeFileMultipleInclusionsSuccess()
        {
            const string inlineContents = "# Another H1 block!";
            string includeFile = Path.GetTempFileName();

            string markdownContent = string.Format(
                markdownSource_IncludeFile, 
                $"Test [[include={includeFile}]] - next.\nHere's another one [[include={includeFile}]].\n[[include={includeFile}]] and one more.\n");

            using (var writer = File.CreateText(includeFile))
            {
                writer.Write(inlineContents);
            }

            try
            {
                PageVariables vars = new PageVariables();
                vars.InitializeFor(new ContentPage(), "");
                string output = new IncludeDirective().Process(vars, markdownContent);
                string expected = string.Format(markdownSource_IncludeFile,
                    $"Test {inlineContents} - next.\nHere's another one {inlineContents}.\n{inlineContents} and one more.\n");
                Assert.AreEqual(expected, output);
            }
            finally
            {
                File.Delete(includeFile);
            }
        }

        [TestMethod]
        public void TestIncludeFileMidLineSuccess()
        {
            const string inlineContents = "# Another H1 block!";
            string includeFile = Path.GetTempFileName();

            string markdownContent = string.Format(markdownSource_IncludeFile, $"This is a test of [[include={includeFile}]] with mid-line replacements.");

            using (var writer = File.CreateText(includeFile))
            {
                writer.Write(inlineContents);
            }

            try
            {
                PageVariables vars = new PageVariables();
                vars.InitializeFor(new ContentPage(), "");
                string output = new IncludeDirective().Process(vars, markdownContent);
                string expected = string.Format(markdownSource_IncludeFile,
                    $"This is a test of {inlineContents} with mid-line replacements.");
                Assert.AreEqual(expected, output);
            }
            finally
            {
                File.Delete(includeFile);
            }
        }

        [TestMethod]
        public void TestIncludeFileWithReplacementTokenSuccess()
        {
            const string inlineContents = "# Another H1 block!";
            string includeFile = Path.GetTempFileName();

            string markdownContent = string.Format(markdownSource_IncludeFile, "[[include={{includeFile}}]]");

            using (var writer = File.CreateText(includeFile))
            {
                writer.Write(inlineContents);
            }

            try
            {
                PageVariables vars = new PageVariables("", new[] { new KeyValuePair<string, string>("includeFile", includeFile) });
                vars.InitializeFor(new ContentPage(), "");
                string output = new IncludeDirective().Process(vars, markdownContent);
                string expected = string.Format(markdownSource_IncludeFile, inlineContents);
                Assert.AreEqual(expected, output);
            }
            finally
            {
                File.Delete(includeFile);
            }
        }
    }
}
