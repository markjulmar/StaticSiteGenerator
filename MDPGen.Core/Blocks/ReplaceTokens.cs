using System.Linq;
using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using System.Text.RegularExpressions;
using System.Text;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This class replaces "{{token}}" values in a given string from a 
    /// known dictionary of text replacements. It supports multi-level replacements.
    /// </summary>
    public class ReplaceTokens : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Process the token replacement
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">Input to process</param>
        /// <returns></returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            var tokenCollection = pageVars.GetService<ITokenCollection>();

            // Start by first replacing {{xref:id}} tokens, these
            // are evaluated to the URL for the given identifier.
            var xref = new Regex(@"\{\{xref:([\w-.]+)\}\}");
            var matches = xref.Matches(input);
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    string original = m.Groups[0].Value;
                    string id = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(id))
                    {
                        var referencedPage = pageVars.Page.Root.FindById(id);
                        if (referencedPage != null)
                        {
                            input = input.Replace(original, referencedPage.Url);
                        }
                    }
                }
            }

            // Next, replace any dynamic site-defined tokens {{token}}.
            input = tokenCollection.Replace(input);

            // See if we still have blocks, if so - run again to catch tokens added from above.
            // This happens mostly in the navigation area
            if (tokenCollection.Enumerate(input).Any())
            {
                input = tokenCollection.Replace(input);

                var missing = tokenCollection.Enumerate(input).ToList();
                if (missing.Count > 0)
                {
                    TraceLog.Write(TraceType.Warning, $"{pageVars.Page?.RelativeFilename} - missing tokens: " +
                        string.Join(",", missing.Select(k => k.Item2 + ":" + k.Item1.ToString())));

                    var sb = new StringBuilder(input);
                    foreach (var item in missing)
                        sb.Replace(item.Item3, Constants.HtmlStartComment + item.Item2 + Constants.HtmlEndComment);

                    input = sb.ToString();
                }
            }

            return input;
        }
    }
}
