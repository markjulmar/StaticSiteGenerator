using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using System.IO;
using System.Text;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This block writes a string into a file.
    /// </summary>
    public class WriteFile : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Write the given text to a file.
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">Text</param>
        /// <returns>Input</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            string outputFile = Path.Combine(pageVars.OutputFolder, pageVars.Page.RelativeOutputFilename);
            string folder = Path.GetDirectoryName(outputFile);

            // Create the output folder if needed.
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            TraceLog.Write(TraceType.Diagnostic, $"Rendering {pageVars.Page.RelativeFilename} => {pageVars.Page.RelativeOutputFilename}");
            File.WriteAllText(outputFile, input, Encoding.UTF8);

            return input;
        }
    }

}
