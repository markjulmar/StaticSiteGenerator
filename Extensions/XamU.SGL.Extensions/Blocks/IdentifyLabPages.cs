using MDPGen.Core;
using MDPGen.Core.Infrastructure;
using System.Linq;

namespace XamU.SGL.Extensions
{
    public class IdentifyLabPages : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Prefix we look for in titles to identify exercises.
        /// </summary>
        public string LabPrefix { get; set; } = "exercise";

        /// <summary>
        /// The folder name to store lab files into.
        /// </summary>
        public string LabFolder { get; set; } = "parts";

        /// <summary>
        /// The root name of the lab files we write out - 
        /// the lab # is appended to this value.
        /// </summary>
        public string RootLabFilename { get; set; } = "part";

        /// <summary>
        /// Check a given page prior to render to see if it's
        /// part of a lab exercise and stop processing if not.
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">Source</param>
        /// <returns>Input unchanged</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            var thisPage = pageVars.Page;

            // Process the root page for each course
            if (!string.IsNullOrEmpty(thisPage.GetMetadata<XamUMetadata>()?.CreditSlug))
            {
                // Switch to a file-based URL.
                int index = 1;
                foreach (var ex in thisPage.Enumerate()
                    .Where(p => p.Title.ToLower().StartsWith(LabPrefix) && p.Children.Count > 0))
                {
                    ex.RelativeOutputFilename = $"{LabFolder}/{RootLabFilename}{index++}.html";
                    ex.Url = ex.RelativeOutputFilename;
                }

                return input;
            }

            // ASSUMPTIONS:
            // 1. All labs have the title "Exercise .."
            // 2. Labs which have sub-folders are option labs (e.g. NodeJS vs. ASP.NET)
            // 3. Labs are always in a folder - never the root of the class.
            // 4. We found a root page (course)
            if (thisPage.Parent != null
                && (thisPage.RelativeOutputFilename.Contains($"{LabFolder}/{RootLabFilename}") 
                    || thisPage.Parent.RelativeOutputFilename.Contains($"{LabFolder}/{RootLabFilename}"))
                && (thisPage.Title.ToLower().StartsWith(LabPrefix)
                    || thisPage.Parent.Title.ToLower().StartsWith(LabPrefix)))
            {
                // This page should have children, but _not_ grandchildren.
                if (thisPage.Children.All(c => c.Children.Count == 0))
                {
                    // Yep - it's a lab
                    return input;
                }
            }

            throw new SkipProcessingException("Not a lab.");
        }
    }
}
