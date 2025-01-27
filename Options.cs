// Options.cs
using CommandLine;
using CUE4Parse.UE4.Versions;
using System.IO;
using System;

namespace UE_Extractor
{
    public class Options
    {
        [Option('p', "content-path", Required = true, HelpText = "Virtual content path to extract")] 
        public string ContentPath { get; set; }

        [Option('e', "engine-version", Required = true, HelpText = "Unreal Engine version")]
        public EGame EngineVersion { get; set; }

        [Option('k', "aes-key", Required = true, HelpText = "AES key in hex format")]
        public string AesKey { get; set; }

        [Option('g', "game-dir", Required = true, HelpText = "Path to Paks directory")]
        public string GameDir { get; set; }

        [Option('o', "output", Default = "Output", HelpText = "Output directory")]
        public string OutputPath { get; set; }

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
        }
    }
}