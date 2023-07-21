using MDPGen.Core;
using MDPGen.Core.Infrastructure;
using System.IO;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// This block detects the end of the lab block and combines all the prior
    /// content into a single string which it passes on the chain. If this is
    /// not the end of the lab, this will place the current page's content into
    /// the tag.
    /// </summary>
    public class WriteLabPage : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// The name of the folder where additional resources are placed.
        /// </summary>
        public string PartsFolder { get; set; } = "parts";
        /// <summary>
        /// The root folder name for lab materials
        /// </summary>
        public string LabMaterialsFolder { get; set; } = "Lab Materials";
        /// <summary>
        /// The name of the file written for the course
        /// </summary>
        public string CourseIntroFilename { get; set; } = "StartHere.html";

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

            // Process the root course page.
            if (thisPage.IsCourse())
            {
                string labFolder = GetLabFolder(pageVars.OutputFolder, thisPage);
                Directory.CreateDirectory(labFolder);
                // Write the startHere file.
                string outputFile = Path.Combine(labFolder, CourseIntroFilename);
                File.WriteAllText(outputFile, input);
            }

            // Or process the final lab page.
            else // if (thisPage.Parent.Tag != null
                 //&& thisPage.Parent.Children.Last() == thisPage)
            {
                // Create our folder.
                string partsFolder = GetPartsFolder(pageVars.OutputFolder, thisPage.Parent);
                Directory.CreateDirectory(partsFolder);

                // We need to copy any sub-folders here.
                string sourceFolder = Path.GetDirectoryName(thisPage.Parent.Filename);
                pageVars.CopyFolder(sourceFolder, partsFolder);

                // We need to copy the real "parts" content here too.
                // It should be one of the search path folders.
                sourceFolder = Utilities.FindFolderAlongPath(pageVars.SearchFolders, PartsFolder);
                if (sourceFolder != null)
                {
                    pageVars.CopyFolder(sourceFolder, partsFolder);
                }

                // Now generate the file.
                string outputFile = Path.Combine(GetLabFolder(pageVars.OutputFolder, thisPage.Parent), thisPage.Parent.RelativeOutputFilename);
                File.WriteAllText(outputFile, input);
            }

            return input;
        }

        private string GetLabFolder(string outputFolder, ContentPage page)
        {
            string title = page.GetCourseOwner().GetCourseTitle();
            title = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
            title = Path.Combine(outputFolder, title, LabMaterialsFolder);
            return title;
        }

        private string GetPartsFolder(string outputFolder, ContentPage page)
        {
            return Path.Combine(GetLabFolder(outputFolder, page), PartsFolder);
        }
    }
}
