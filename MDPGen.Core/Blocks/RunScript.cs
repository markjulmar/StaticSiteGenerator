using System;
using System.Collections.Generic;
using System.Linq;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This block removes spaces and comments from the input HTML
    /// to make it as small as possible.
    /// </summary>
    public class RunScript : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Name of the script to run.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Arguments to pass in.
        /// </summary>
        public List<string> Args { get; set; }

        /// <summary>
        /// Process the minify/versioning logic
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">HTML file</param>
        /// <returns>HTML file</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception($"Missing {nameof(Name)} property on {nameof(RunScript)}.");

            // Add the input as the 1st parameter passed in.
            var args = Args.ToList();
            args.Insert(0, input);

            // Run the script.
            var result = ExtensionProcessor.RunScript(Name, pageVars, args.ToArray());
            return result?.ToString();
        }
    }
}
