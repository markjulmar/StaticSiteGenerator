using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace ReadSlides
{
    public class SlideManager : IDisposable
    {
        private static readonly string[] Empty = new string[0];
        private PresentationDocument presentationDocument;

        public SlideManager(string presentationFile)
        {
            // Open the presentation as read-only.
            presentationDocument = PresentationDocument.Open(presentationFile, false);
        }

        public int SlideCount => (presentationDocument.PresentationPart?.Presentation.SlideIdList?.ChildElements?.Count ?? (int?)0).Value;

        public string GetSlideTitle(int slideIndex)
        {
            var slidePart = GetSlidePart(slideIndex);
            return slidePart == null ? string.Empty : GetSlideTitle(slidePart);
        }

        public string[] GetAllTextInSlide(int slideIndex)
        {
            var slidePart = GetSlidePart(slideIndex);
            return slidePart == null ? Empty : GetAllTextInSlide(slidePart);
        }

        private SlidePart GetSlidePart(int slideIndex)
        {
            if (presentationDocument == null)
                throw new Exception("No open document.");
            if (slideIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(slideIndex));

            // Get the presentation part of the presentation document.
            var presentationPart = presentationDocument.PresentationPart;

            // Get the Presentation object from the presentation part.
            var presentation = presentationPart?.Presentation;

            // Get the collection of slide IDs from the slide ID list.
            DocumentFormat.OpenXml.OpenXmlElementList slideIds =
                presentation?.SlideIdList?.ChildElements;

            // If the slide ID is in range...
            if (slideIndex < slideIds?.Count)
            {
                // Get the relationship ID of the slide.
                var id = slideIds[slideIndex] as SlideId;
                if (id != null)
                {
                    string slidePartRelationshipId = id.RelationshipId;

                    // Get the specified slide part from the relationship ID.
                    SlidePart slidePart = presentationPart.GetPartById(slidePartRelationshipId) as SlidePart;
                    return slidePart;
                }
            }

            return null;
        }

        static string[] GetAllTextInSlide(SlidePart slidePart)
        {
            // Verify that the slide part exists.
            if (slidePart == null)
                throw new ArgumentNullException(nameof(slidePart));

            // Create a new linked list of strings.
            LinkedList<string> texts = new LinkedList<string>();

            // If the slide exists...
            if (slidePart.Slide != null)
            {
                // Iterate through all the paragraphs in the slide.
                foreach (var paragraph in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    // Create a new string builder.                    
                    StringBuilder paragraphText = new StringBuilder();

                    // Iterate through the lines of the paragraph.
                    foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        // Append each line to the previous lines.
                        paragraphText.Append(text.Text);
                    }

                    if (paragraphText.Length > 0)
                    {
                        // Add each paragraph to the linked list.
                        texts.AddLast(paragraphText.ToString());
                    }
                }
            }

            return texts.Count > 0 ? texts.ToArray() : Empty;
        }

        static string GetSlideTitle(SlidePart slidePart)
        {
            if (slidePart == null)
            {
                throw new ArgumentNullException(nameof(slidePart));
            }

            // Declare a paragraph separator.
            string paragraphSeparator = null;

            if (slidePart.Slide != null)
            {
                // Find all the title shapes.
                var shapes = from shape in slidePart.Slide.Descendants<Shape>()
                    where IsTitleShape(shape)
                    select shape;

                StringBuilder paragraphText = new StringBuilder();

                foreach (var shape in shapes)
                {
                    // Get the text in each paragraph in this shape.
                    foreach (var paragraph in shape.TextBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                    {
                        // Add a line break.
                        paragraphText.Append(paragraphSeparator);

                        foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        {
                            paragraphText.Append(text.Text);
                        }

                        paragraphSeparator = Environment.NewLine;
                    }
                }

                return paragraphText.ToString();
            }

            return string.Empty;
        }

        private static bool IsTitleShape(Shape shape)
        {
            var placeholderShape = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
            if (placeholderShape?.Type != null && placeholderShape.Type.HasValue)
            {
                switch ((PlaceholderValues)placeholderShape.Type)
                {
                    // Any title shape.
                    case PlaceholderValues.Title:

                    // A centered title.
                    case PlaceholderValues.CenteredTitle:
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }
        public void Dispose()
        {
            presentationDocument?.Dispose();
            presentationDocument = null;
        }
    }
}