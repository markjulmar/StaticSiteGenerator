using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This block takes Markdown input and converts it to HTML
    /// </summary>
    public class ConvertMarkdownToHtml : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Process the given text input as Markdown and return HTML
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">Markdown text</param>
        /// <returns>HTML</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null
                || pageVars.Page.ContentType != ContentType.Markdown)
                return input;

            return pageVars.GetService<IMarkdownParser>().Transform(input);
        }
    }
}
