using MDPGen.Core.Infrastructure;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This block removes spaces and comments from the input HTML
    /// to make it as small as possible.
    /// </summary>
    public class CompressHtml : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// True to use the original ZetaProducerHtmlCompressor.
        /// False to use the WebMarkupMin compressor.
        /// </summary>
        public bool UseLegacyCompressor { get; set; } = false;

        /// <summary>
        /// Process the minify/versioning logic
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">HTML file</param>
        /// <returns>HTML file</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null) return null;

            if (UseLegacyCompressor)
            {
                return new ZetaProducerHtmlCompressor.HtmlContentCompressor().Compress(input);
            }
            else
            {
                return new WebMarkupMin.Core.HtmlMinifier(new WebMarkupMin.Core.HtmlMinificationSettings {
                            WhitespaceMinificationMode = WebMarkupMin.Core.WhitespaceMinificationMode.Aggressive,
                            RemoveHtmlComments = true,
                            RemoveOptionalEndTags = false,
                        }).Minify(input).MinifiedContent;
            }
        }
    }
}
