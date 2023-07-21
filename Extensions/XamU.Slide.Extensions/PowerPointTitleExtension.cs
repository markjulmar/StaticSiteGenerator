using System;
using MDPGen.Core.MarkdownExtensions;
using MDPGen.Core.Services;
using ReadSlides;

namespace XamU.Slide.Extensions
{
    /// <summary>
    /// Extension to retrieve the ID and Title from a XamU PowerPoint deck.
    /// </summary>
    public class PowerPointTitleExtension : BaseMarkdownExtension
    {
        public const string PowerPointFilename = "Powerpoint.Filename";
        public const string PowerPointId = "Powerpoint.Id";
        public const string PowerPointTitle = "Powerpoint.Title";

        /// <summary>
        /// Set the ID and Title from a Powerpoint file into
        /// a set of tokens.
        /// </summary>
        /// <param name="provider">Service Provider</param>
        [ExtensionInit]
        public static void SetIdAndTitle(IServiceProvider provider)
        {
            var tokens = provider.GetService<ITokenCollection>();

            // Get the filename
            var fn = tokens[PowerPointTitleExtension.PowerPointFilename];
            if (string.IsNullOrWhiteSpace(fn))
                throw new ArgumentException($"Missing {PowerPointFilename} (filename) for {typeof(PowerPointTitleExtension).Name}.");

            SlideManager mgr = new SlideManager(fn);
            if (mgr.SlideCount > 1)
            {
                var text = mgr.GetAllTextInSlide(0);
                tokens[PowerPointId] = text[1];
                tokens[PowerPointTitle] = text[0];
            }
        }

        /// <summary>
        /// Method used to generate the HTML output.
        /// </summary>
        /// <returns>HTML output to add to the resulting page, or null if no output.</returns>
        protected override string Process()
        {
            return null;
        }
    }
}
