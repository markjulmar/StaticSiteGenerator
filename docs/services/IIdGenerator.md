```csharp
namespace MDPGen.Core.Services
{
    /// <summary>
    /// Interface for the ID generator
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// Generate a unique number to use on the page
        /// </summary>
        /// <returns>Integer</returns>
        int GenerateNumber();

        /// <summary>
        /// Generate a unique valid HTML ID/name
        /// </summary>
        /// <returns></returns>
        string GenerateText();
    }
}
```