using MDPGen.Core.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MDPGen.Core.UnitTests
{

    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod]
        public void SmartIndexOfHandlesIndexOfCase()
        {
            string value = "This is a cool test";

            int pos = value.SmartIndexOf(0, 'c', null);
            Assert.AreEqual(10, pos);
        }

        [TestMethod]
        public void SmartIndexOfHandlesEmbedChars()
        {
            var embeds = new Tuple<char, char>[] { Tuple.Create('(',')') };

            string value = "This is a (cool) test of calamity!";

            int pos = value.SmartIndexOf(0, 'c', embeds);
            Assert.AreEqual(25, pos);
        }

        [TestMethod]
        public void SmartIndexIgnoresEscapedChars()
        {
            var embeds = new Tuple<char, char>[] { Tuple.Create('(', ')') };

            string value = @"This is a \(cool) test of calamity!";

            int pos = value.SmartIndexOf(0, 'c', embeds);
            Assert.AreEqual(12, pos);
        }

        [TestMethod]
        public void GrabToCapturesInnerText()
        {
            string value = "One, Two; Three";

            string result = value.GrabTo(0, ';');
            Assert.AreEqual("One, Two", result);
        }

        [TestMethod]
        public void GrabToCapturesTerminator()
        {
            string value = "One, Two; Three";

            string result = value.GrabTo(0, ';', includeChar: true);
            Assert.AreEqual("One, Two;", result);
        }
    }
}
