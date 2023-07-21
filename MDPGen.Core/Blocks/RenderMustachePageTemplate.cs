using System.Text;
using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using System.IO;
using System;
using MDPGen.Core.Services;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This processing block merges the created Markdown/HTML into a page template.
    /// </summary>
    public class RenderMustachePageTemplate : BaseProcessingBlock<string, string>
    {
        const string BodyTag = Constants.BeginMarker + "!body" + Constants.EndMarker;

        /// <summary>
        /// Process the merge template function
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">File to run processor on</param>
        /// <returns>Merged files</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            StringBuilder builder;
            string pageTemplate = pageVars.Page.PageTemplate;
            if (string.IsNullOrWhiteSpace(pageTemplate))
            {
                builder = new StringBuilder(input);
            }
            else
            {
                string filename = Utilities.FindFileAlongPath(pageVars.SearchFolders, pageTemplate);
                if (filename == null)
                    throw new Exception($"Unable to locate page template: {pageTemplate}");
                builder = new StringBuilder(File.ReadAllText(filename));

                if (builder.ToString().IndexOf(BodyTag, StringComparison.Ordinal) == -1)
                {
                    TraceLog.Write(TraceType.Warning, 
                        $"No {BodyTag} found in template {pageTemplate}.");
                }

                builder.Replace(BodyTag, input);
            }

            return builder.ToString();
        }
    }
}

