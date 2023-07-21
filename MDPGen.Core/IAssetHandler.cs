namespace MDPGen.Core
{
    /// <summary>
    /// This can be used to process an asset. The default
    /// behavior is to _copy_ them from source to destination.
    /// </summary>
    public interface IAssetHandler
    {
        /// <summary>
        /// Process the asset.
        /// </summary>
        /// <param name="input">Input filename</param>
        /// <param name="output">Possible output filename</param>
        /// <returns>True to keep processing, false to stop after this.</returns>
        bool Process(string input, string output);
    }
}
