using System;
using System.Text;
using MDPGen.Core;
using MDPGen.Core.Blocks;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MDPGen.UnitTests.ExtensionTests
{
    [TestClass]
    public class JsonTests
    {
        private PageVariables pageVars;

        [TestInitialize]
        public void Initialize()
        {
            pageVars = new PageVariables();
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime Dob { get; set; }

            public override string ToString()
            {
                return $"{Name} is {Age} years old and was born on {Dob:d}";
            }
        }

        class PersonExtension : IMarkdownExtension
        {
            private readonly Person p;

            public PersonExtension(Person p)
            {
                this.p = p;
            }

            public string Process(IServiceProvider provider)
            {
                return p.ToString();
            }
        }

        [TestMethod]
        public void PersonObjectShouldSucceed()
        {
            string markdownSource = "@Person({ name: 'Mark', age: 8, Dob: '12/25/1945' })";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(PersonExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "Mark is 8 years old and was born on 12/25/45";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultilinePersonObjectShouldSucceed()
        {
            string markdownSource = "@Person(\r\n" + 
                "{\r\n" + 
                "\tname: 'Mark',\r\n" + 
                "\tage: 8,\r\n" + 
                "\tDob: '12/25/1945'\r\n" + 
                "})";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(PersonExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "Mark is 8 years old and was born on 12/25/45";

            Assert.AreEqual(expected, actual);
        }

        class PeopleExtension : IMarkdownExtension
        {
            private readonly Person[] people;

            public PeopleExtension(Person[] people)
            {
                this.people = people;
            }
            public string Process(IServiceProvider provider)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var p in people)
                    sb.AppendLine(p.ToString());
                return sb.ToString();
            }
        }

        [TestMethod]
        public void ArrayOfPersonShouldSucceed()
        {
            string markdownSource = "@People([ { name: 'Adrian', age: 10 }, { name: 'Mark', age: 8 } ])";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(PeopleExtension));

            string actual = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "Adrian is 10 years old and was born on 1/1/01\r\nMark is 8 years old and was born on 1/1/01\r\n";

            Assert.AreEqual(expected, actual);

        }

        class StringExtension : IMarkdownExtension
        {
            private readonly string[] data;

            public StringExtension(string[] data)
            {
                this.data = data;
            }

            public string Process(IServiceProvider provider)
            {
                return String.Join("|", data);
            }
        }

        [TestMethod]
        public void ArrayOfStringsShouldSucceed()
        {
            string markdownSource =
                "@String([ \"1\", \"2\", \"3\", \"4\", \"5\" ])\r\n";

            ExtensionProcessor.Reset();
            ExtensionProcessor.Init(typeof(StringExtension));
            string result = new RunMarkdownExtensions().Process(pageVars, markdownSource);
            string expected = "1|2|3|4|5";
            Assert.AreEqual(expected, result);
        }
    }
}