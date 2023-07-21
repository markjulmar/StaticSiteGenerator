using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MDPGen.Core
{
    /// <summary>
    /// This initializes our site generator from a configuration file.
    /// </summary>
    public static class StaticSiteGeneratorExtensions
    {
        /// <summary>
        /// Initialize the page processor with a site configuration
        /// </summary>
        /// <param name="generator">Owning class</param>
        /// <param name="siteConfigFile">Site configuration file</param>
        public static void Initialize(this StaticSiteGenerator generator, string siteConfigFile)
        {
            if (siteConfigFile == null)
                throw new ArgumentNullException(nameof(siteConfigFile));

            if (!File.Exists(siteConfigFile))
                throw new FileNotFoundException($"Site configuration file {siteConfigFile} not found.", nameof(siteConfigFile));

            // We always treat the folder of the site config file as our "root" for things contained
            // within that folder.
            var folder = Path.GetDirectoryName(siteConfigFile) ?? "./";
            var baseInputFolder = Path.GetFullPath(folder);

            TraceLog.Write($"Using site configuration file {siteConfigFile}");

            var siteConfiguration = JsonConvert.DeserializeObject<SiteConfigInfo>(File.ReadAllText(siteConfigFile));

            // Root the template folder(s)
            if (siteConfiguration.SearchFolders?.Count > 0)
            {
                for (int i = 0; i < siteConfiguration.SearchFolders.Count; i++)
                {
                    siteConfiguration.SearchFolders[i] =
                        Utilities.FixupRelativePaths(siteConfiguration.SearchFolders[i], baseInputFolder);
                }
            }
            // No search folder supplied. Assume siteConfig folder.
            else
            {
                siteConfiguration.SearchFolders = new List<string> { baseInputFolder };
            }

            // Now do the asset folders.
            if (siteConfiguration.AssetFoldersToCopy?.Count > 0)
            {
                for (int i = 0; i < siteConfiguration.AssetFoldersToCopy.Count; i++)
                {
                    siteConfiguration.AssetFoldersToCopy[i] =
                        Utilities.FixupRelativePaths(siteConfiguration.AssetFoldersToCopy[i], baseInputFolder);
                }
            }

            // Check the site constants.
            if (siteConfiguration.SiteConstantsFilename != null)
            {
                string filename = Utilities.FixupRelativePaths(siteConfiguration.SiteConstantsFilename, baseInputFolder);
                if (File.Exists(filename))
                {
                    var constants = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(File.ReadAllText(filename));
                    if (siteConfiguration.Constants == null)
                        siteConfiguration.Constants = constants;
                    else
                    {
                        var dic = new Dictionary<string, string>();
                        foreach (var item in siteConfiguration.Constants)
                            dic.Add(item.Key, item.Value);
                        foreach (var item in constants)
                            dic[item.Key] = item.Value;
                        siteConfiguration.Constants = dic.ToList();
                    }
                }
            }

            if (siteConfiguration.ContentFolder != null)
            {
                siteConfiguration.ContentFolder = Utilities.FixupRelativePaths(siteConfiguration.ContentFolder, baseInputFolder);
            }

            if (siteConfiguration.ScriptConfig?.ScriptsFolder != null)
            {
                siteConfiguration.ScriptConfig.ScriptsFolder = Utilities.FixupRelativePaths(siteConfiguration.ScriptConfig.ScriptsFolder, baseInputFolder);
            }

            // Move over the asset folders. If it's not present, then assume we should
            // copy the content folder over (default behavior).
            if (siteConfiguration.CopyContentFolder)
            {
                generator.AssetFoldersToCopy.Add(siteConfiguration.ContentFolder);
            }
            if (siteConfiguration.AssetFoldersToCopy?.Count > 0)
            {
                generator.AssetFoldersToCopy.AddRange(siteConfiguration.AssetFoldersToCopy);
            }

            generator.Constants = siteConfiguration.Constants ?? new List<KeyValuePair<string, string>>();
            generator.DefaultPageTemplate = siteConfiguration.DefaultPageTemplate;
            generator.ContentFolder = siteConfiguration.ContentFolder;
            generator.Extensions = siteConfiguration.Extensions;
            generator.SearchFolders = siteConfiguration.SearchFolders;
            generator.ScriptConfiguration = siteConfiguration.ScriptConfig;

            // Replace the processing chain.
            generator.ProcessingChain = new List<IProcessingBlock>();
            if (siteConfiguration.ProcessingChain != null)
            {
                foreach (JToken item in siteConfiguration.ProcessingChain)
                {
                    IProcessingBlock block;
                    // Format style 1: list of types.
                    if (item.Type == JTokenType.String)
                    {
                        var typeName = FullyQualifyType("MDPGen.Core.Blocks.", item.Value<string>());
                        var type = ServiceFactory.LoadType(typeName);
                        try
                        {
                            block = (IProcessingBlock) Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            TraceLog.Write(TraceType.Error,
                                $"Failed to instantiate processing block {type.Name} - {ex.Message}");
                            throw;
                        }
                    }
                    // Format style 2: full type description
                    else
                    {
                        var td = item.ToObject<TypeDescription>();
                        td.Type = FullyQualifyType("MDPGen.Core.Blocks.", td.Type);
                        block = td.Create<IProcessingBlock>();
                    }

                    generator.ProcessingChain.Add(block);
                }
            }

            if (siteConfiguration.ProcessPagesSequentially)
                generator.MaxThreads = 1;

            // Copy over the build symbols.
            if (siteConfiguration.BuildSymbols?.Count > 0)
            {
                generator.BuildSymbols.AddRange(siteConfiguration.BuildSymbols);
            }

            // Work on the transform types.
            if (siteConfiguration.AssetHandlers?.HasValues == true)
            {
                // Go through each one. We support either a string type,
                // or a full TypeDescription.
                foreach (JToken item in siteConfiguration.AssetHandlers)
                {
                    IAssetHandler assetHandler;

                    if (item.Type == JTokenType.String)
                    {
                        string typeName = FullyQualifyType("MDPGen.Core.AssetHandlers.", item.Value<string>());
                        Type type = ServiceFactory.LoadType(typeName);
                        try
                        {
                            assetHandler = (IAssetHandler) Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            TraceLog.Write(TraceType.Error,
                                $"Failed to instantiate asset handler {type.Name} - {ex.Message}");
                            throw;
                        }
                    }
                    // Nope - it's a set of full type definitions.
                    else
                    {
                        var td = item.ToObject<TypeDescription>();
                        td.Type = FullyQualifyType("MDPGen.Core.Blocks.", td.Type);
                        assetHandler = td.Create<IAssetHandler>();
                    }

                    TraceLog.Write(TraceType.Diagnostic, $"Adding asset handler {assetHandler.GetType().Name}");
                    generator.TransformAsset += assetHandler.Process;
                }

            }

            // Work on the override types.
            if (siteConfiguration.OverrideTypes != null)
            {
                var types = siteConfiguration.OverrideTypes;
                if (types.IdGeneratorType != null)
                {
                    ServiceFactory.Instance
                        .RegisterServiceType<IIdGenerator>(
                            TypeDescription.FromToken(types.IdGeneratorType));
                }

                if (types.MarkdownParserType != null)
                {
                    ServiceFactory.Instance
                        .RegisterServiceType<IMarkdownParser>(
                            TypeDescription.FromToken(types.MarkdownParserType));
                }

                if (types.TokenCollectionType != null)
                {
                    ServiceFactory.Instance
                        .RegisterServiceType<ITokenCollection>(
                            TypeDescription.FromToken(types.TokenCollectionType));
                }

                if (types.MetadataLoaderType != null)
                {
                    TypeDescription td = TypeDescription.FromToken(types.MetadataLoaderType);
                    td.Type = FullyQualifyType("MDPGen.Core.Infrastructure.Metadata", td.Type);
                    ServiceFactory.Instance
                        .RegisterServiceType<IPageMetadataLoader>(td);
                }

                if (types.PageLoaderType != null)
                {
                    TypeDescription td = TypeDescription.FromToken(types.PageLoaderType);
                    td.Type = FullyQualifyType("MDPGen.Core.Infrastructure.", td.Type);
                    ServiceFactory.Instance
                        .RegisterServiceType<IContentPageLoader>(td);
                }
            }

            generator.Initialize();
        }

        /// <summary>
        /// THis method adds
        /// </summary>
        /// <param name="prefix">Namespace prefix to apply</param>
        /// <param name="typeName">Typename</param>
        /// <returns>Fully qualified typename</returns>
        private static string FullyQualifyType(string prefix, string typeName)
        {
            if (typeName.Contains("."))
                return typeName;

            return prefix + typeName;
        }
    }
}
