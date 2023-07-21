using CommandLine;
using System.Collections.Generic;

namespace MDPGen
{
    /// <summary>
    /// Type representing our command line options.
    /// </summary>
    public class CommandLineOptions
    {
        [Option('i', "Initialize", HelpText = "Initialize a new site.")]
        public bool Initialize { get; set; }

        [Option('d', "DefineSymbol", HelpText = "Define build symbols (comma-separated)", Separator = ',')]
        public IEnumerable<string> BuildSymbols { get; set; }

        [Option('s', "SiteConfiguration", HelpText = "Site configuration file.")]
        public string SiteConfigFile { get; set; }

        [Option('m', "MaxThreads", HelpText = "# of Threads to use during processing (defaults to # of logical CPUs)")]
        public int MaxThreads { get; set; }

        [Option('o', "OutputFolder", HelpText = "Folder to generate site into (defaults to \".\\output\"")]
        public string OutputFolder { get; set; }

        [Option('v', "Verbose", HelpText = "Verbose output")]
        public bool Verbose { get; set; }
    }
}