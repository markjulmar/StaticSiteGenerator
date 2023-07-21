using HeyRed.MarkdownSharp;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Markdown parser interface
    /// </summary>
    public interface IMarkdownParser
    {
        /// <summary>
        /// Convert a Markdown string to HTML
        /// </summary>
        /// <param name="source">Markdown text</param>
        /// <returns>HTML text</returns>
        string Transform(string source);
    }

    /// <summary>
    /// MarkdownDeep implementation of our parser
    /// </summary>
    class MarkdownParser : IMarkdownParser
    {
        readonly Markdown markdownParser = new Markdown();

        /// <summary>
        /// Convert a Markdown string to HTML
        /// </summary>
        /// <param name="source">Markdown text</param>
        /// <returns>HTML text</returns>
        public string Transform(string source)
        {
            return markdownParser.Transform(source);
        }
    }
}