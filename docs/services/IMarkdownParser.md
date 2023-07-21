```csharp
namespace MDPGen.Core.Services
{
    /// <summary>
    /// Markdown parser interface
    /// </summary>
    public interface IMarkdownParser
    {
        /// <summary>
        /// Convert a Markdown string to HTML
        /// </summary>
        /// <param name="source">Markdown text</param>
        /// <returns>HTML text</returns>
        string Transform(string source);
    }
}
```

