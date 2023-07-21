using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// This represents a page of content in the navigation structure.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Url) + "}")]
    public class ContentPage
    {
        private const string NoTemplate = "none";
        private string pageTemplate;
        private DocumentMetadata metadata;

        /// <summary>
        /// Unique identifier for page
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Title for the page
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The template to render this page with
        /// </summary>
        public string PageTemplate
        {
            get
            {
                var template = pageTemplate ?? metadata?.PageTemplate;
                if (template != null && String.CompareOrdinal(template, NoTemplate) == 0)
                    template = null;

                return template;
            }

            set => pageTemplate = value;
        }

        /// <summary>
        /// Set the metadata for this page.
        /// </summary>
        /// <param name="md">Loaded metadata</param>
        public void SetMetadata(DocumentMetadata md)
        {
            metadata = md;
            if (!string.IsNullOrEmpty(md?.Id))
                this.Id = md.Id;
            if (!string.IsNullOrEmpty(md?.Title))
                this.Title = md.Title;
        }

        /// <summary>
        /// Return the base metadata object
        /// </summary>
        public DocumentMetadata GetMetadata()
        {
            return metadata;
        }

        /// <summary>
        /// Document metadata from the YAML header.
        /// </summary>
        /// <typeparam name="T">Metadata type</typeparam>
        public T GetMetadata<T>() where T : DocumentMetadata
        {
            return metadata as T;
        }

        /// <summary>
        /// Root folder node.
        /// </summary>
        public ContentPage Root
        {
            get
            {
                // Back up to the root parent.
                var root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }

        /// <summary>
        /// True if this is the default page for the folder.
        /// </summary>
        public bool IsDefaultPage { get; set; }

        /// <summary>
        /// Parent for this node, null if this is a root.
        /// </summary>
        public ContentPage Parent { get; set; }

        /// <summary>
        /// Represents the PREVIOUS page in this site (if any)
        /// </summary>
        public ContentPage PreviousPage { get; set; }

        /// <summary>
        /// Represents the NEXT page in this site (if any)
        /// </summary>
        public ContentPage NextPage { get; set; }

        /// <summary>
        /// Relative filename (excluding base path)
        /// </summary>
        public string RelativeFilename { get; set; }

        /// <summary>
        /// FQN to the file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The filename to use for output (no path)
        /// </summary>
        public string RelativeOutputFilename { get; set; }

        /// <summary>
        /// URL for this node. This should be a fully-qualified URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The content loaded for this page.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The type of content loaded (extension)
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// Tag which can be used by processing blocks to store information
        /// for this page - since this persists _after_ the processing is complete
        /// external processes can read this information post build.
        /// Note: this is not used by the tool.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// List of children if any.
        /// </summary>
        public List<ContentPage> Children { get; } = new List<ContentPage>();
    }
}