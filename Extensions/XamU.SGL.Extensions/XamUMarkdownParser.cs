using MDPGen.Core.Services;
using System.Text;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// Override the parser to include features specific to the XamU site.
    /// </summary>
    public sealed class XamUMarkdownParser : IMarkdownParser
    {
        private MarkdownDeep.Markdown markdownParser;

        /// <summary>
        /// Convert a Markdown string to HTML
        /// </summary>
        /// <param name="source">Markdown text</param>
        /// <returns>HTML text</returns>
        public string Transform(string source)
        {
            if (markdownParser == null)
                markdownParser = CreateAndConfigureParser();

            return markdownParser.Transform(source);
        }

        /// <summary>
        /// Create the parser
        /// </summary>
        /// <returns></returns>
        MarkdownDeep.Markdown CreateAndConfigureParser()
        {
            return new MarkdownDeep.Markdown
            {
                // Support PanDoc extensions
                ExtraMode = true,
                // Support Markdown inside HTML
                MarkdownInHtml = true,
                // Do not allow \n in content
                GithubStyleLineBreaks = false,
                // Open up external links in a new window
                NewWindowForExternalLinks = true,
                // Class name for titled images
                HtmlClassTitledImages = "figure",
                // Class for figcaption elements
                HtmlClassFigureCaption = "figure-caption",
                // Support pretty-print in code blocks
                FormatCodeBlock = (m, marker, attrs, code) =>
                {
                    if (marker == null)
                        return code; // not really a code block
                    var sb = new StringBuilder();
                    if (attrs == null)
                        attrs = new MarkdownDeep.SpecialAttributes();
                    attrs.AddClass(marker.StartsWith("~") ? "prettyprint-collapse" : "prettyprint");
                    sb.AppendFormat("<pre{0}><code>", attrs.ToString());
                    sb.Append(code.Replace("&lt;mark&gt;", "<mark>").Replace("&lt;/mark&gt;", "</mark>"));
                    sb.Append("</code></pre>\n\n");
                    return sb.ToString();
                },
                // Support info and danger block quotes through ">>" and ">>>".
                EmitTag = (block, closeTag) =>
                {
                    if (block.BlockType == MarkdownDeep.BlockType.quote && !closeTag)
                    {
                        switch ((int)block.Data)
                        {
                            case 2:
                                return "<blockquote class=\"info-quote\">";
                            case 3:
                                return "<blockquote class=\"danger-quote\">";
                        }
                    }
                    return null;
                }
            };
        }
    }
}
