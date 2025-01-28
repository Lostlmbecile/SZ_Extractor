// Options.cs
using CommandLine;
using CUE4Parse.UE4.Versions;

namespace SZ_Extractor
{
    public class Options
    {
        [Option('p', "content-path", HelpText = "Virtual content path to extract")]
        public required string ContentPath { get; set; }

        [Option('e', "engine-version", Required = true, HelpText = "Unreal Engine version")]
        public EGame EngineVersion { get; set; }

        [Option('k', "aes-key", Required = true, HelpText = "AES key in hex format")]
        public required string AesKey { get; set; }

        [Option('g', "game-dir", Required = true, HelpText = "Path to Paks directory")]
        public required string GameDir { get; set; }

        [Option('o', "output", Default = "Output", HelpText = "Output directory")]
        public required string OutputPath { get; set; }

        [Option('d', "dump-paths", Default = false, HelpText = "Dump all virtual file paths to paths.txt")]
        public bool DumpPaths { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Enable verbose logging")]
        public bool Verbose { get; set; }

        public void Validate()
        {
            if (!Directory.Exists(GameDir))
            {
                throw new ArgumentException($"Game directory does not exist: {GameDir}");
            }

            if (!DumpPaths && string.IsNullOrEmpty(ContentPath))
            {
                throw new ArgumentException("Content path is required when not dumping paths.");
            }
        }
    }
}