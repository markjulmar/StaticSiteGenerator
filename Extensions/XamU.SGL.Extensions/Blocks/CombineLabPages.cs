using MDPGen.Core;
using MDPGen.Core.Infrastructure;
using System.Linq;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// This block detects the end of the lab block and combines all the prior
    /// content into a single string which it passes on the chain. If this is
    /// not the end of the lab, this will place the current page's content into
    /// the tag.
    /// </summary>
    public class CombineLabPages : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// The page template to use for the StartPage
        /// </summary>
        public string StartPageTemplate { get; set; } = "startPageTemplate.cshtml";

        /// <summary>
        /// The page template to use for Lab pages
        /// </summary>
        public string LabPageTemplate { get; set; } = "LabTemplate.cshtml";

        /// <summary>
        /// Takes the HTML rendered output and puts it into
        /// the Page.Tag or combines the entire lab into one.
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">HTML</param>
        /// <returns>input</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            var thisPage = pageVars.Page;
            thisPage.Tag = input; // HTML single-page output

            // Root page?
            if (!string.IsNullOrEmpty(thisPage.GetMetadata<XamUMetadata>()?.CreditSlug))
            {
                thisPage.PageTemplate = StartPageTemplate;
                return input;
            }

            // Final lab page?
            else if (thisPage.Parent.Tag != null
                && thisPage.Parent.Children.Last() == thisPage)
            {
                pageVars.Tokens["title"] = thisPage.Parent.GetMetadata<XamUMetadata>().Title;

                input = string.Join("\r\n<hr>\r\n",
                    thisPage.Parent.Enumerate().Select(p => p.Tag?.ToString() ?? ""));
                thisPage.PageTemplate = LabPageTemplate;

                // Fixup "@" for Razor; we've already processed the
                // pages once which drops the processing "@".
                input = input.Replace("@", "@@");
                return input;
            }

            // Skip intermediate lab pages.
            throw new SkipProcessingException();
        }
    }
}
