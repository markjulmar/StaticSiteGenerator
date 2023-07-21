using System;
using MDPGen.Core.Data;
using MDPGen.Core.Services;
using SharpScss;
using System.IO;

namespace MDPGen.Core.AssetHandlers
{
    /// <summary>
    /// This asset handler detects .SCSS files and converts
    /// them to minified CSS files.
    /// </summary>
    public class ConvertScssToCss : IAssetHandler
    {
        /// <summary>
        /// True to generate a source map
        /// </summary>
        public bool GenerateSourceMap { get; set; } = true;

        /// <summary>
        /// Output style
        /// </summary>
        public ScssOutputStyle OutputStyle { get; set; } = ScssOutputStyle.Compressed;

        /// <summary>
        /// Process any filename with the .scss file extension
        /// </summary>
        /// <param name="input">Source Filename</param>
        /// <param name="output">Possible destination filename</param>
        /// <returns></returns>
        public bool Process(string input, string output)
        {
            if (string.IsNullOrEmpty(output))
                throw new ArgumentNullException(nameof(output));

            // Check for a transformation
            if (File.Exists(input))
            {
                string extension = Path.GetExtension(input)?.ToLower();
                if (extension == FileExtensions.Scss)
                {
                    output = Path.ChangeExtension(output, OutputStyle == ScssOutputStyle.Compressed
                        ? FileExtensions.CssMin
                        : FileExtensions.Css);

                    var options = new ScssOptions
                    {
                        InputFile = input,
                        OutputFile = output,
                        OutputStyle = this.OutputStyle,
                        GenerateSourceMap = this.GenerateSourceMap
                    };

                    options.IncludePaths.Add(Path.GetDirectoryName(input));

                    TraceLog.Write(TraceType.Diagnostic, $"{GetType().Name} transforming {input} to {output}");
                    var result = Scss.ConvertToCss(File.ReadAllText(input), options);

                    // Write the files out in the destination
                    File.WriteAllText(output, result.Css);
                    if (GenerateSourceMap)
                    {
                        File.WriteAllText(Path.ChangeExtension(output, FileExtensions.CssMap), result.SourceMap);
                    }

                    // File processed; stop here.
                    return false;
                }
            }

            // Continue processing.
            return true;
        }
    }
}
