using System;
using System.IO;
using System.Linq;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Text.Json;

namespace UE_Extractor
{
    public class Extractor
    {
        private readonly Options _options;
        private readonly DefaultFileProvider _provider;

        public Extractor(Options options)
        {
            _options = options;
            _options.OutputPath = Path.GetFullPath(_options.OutputPath);

            _provider = new DefaultFileProvider(
                _options.GameDir,
                SearchOption.AllDirectories,
                isCaseInsensitive: false,
                new VersionContainer(_options.EngineVersion)
            );

            _provider.Initialize();

            var aesKeyBytes = Convert.FromHexString(_options.AesKey.Replace("0x", ""));
            _provider.SubmitKey(new FGuid(), new FAesKey(aesKeyBytes));

            _provider.Mount();

            if (_options.Verbose)
            {
                Console.WriteLine($"Successfully mounted {_provider.Files.Count} files");
            }

            if (_options.DumpPaths)
            {
                DumpAllPaths();
            }
        }

        public void Run()
        {
            var targetPathLower = NormalizePath(_options.ContentPath).ToLowerInvariant();
            if (_options.Verbose)
            {
                Console.WriteLine($"Searching for: {_options.ContentPath} (case-insensitive)");
            }
            if (IsDirectory(targetPathLower))
            {
                ExtractFolder(targetPathLower);
            }
            else
            {
                ExtractFile(targetPathLower);
            }
        }

        private void ExtractFile(string targetPathLower)
        {
            var fileEntry = _provider.Files.FirstOrDefault(f => NormalizePath(f.Key).Equals(targetPathLower, StringComparison.OrdinalIgnoreCase));

            if (fileEntry.Value != null && _provider.TrySavePackage(fileEntry.Key, out var packageData))
            {
                WriteToFile(packageData, _options.OutputPath, fileEntry.Key);

                if (_options.Verbose)
                {
                    Console.WriteLine($"Extracted: {Path.GetFileName(fileEntry.Key)} to {_options.OutputPath}");
                }
            }
            else
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"Could not find or load file: {_options.ContentPath} (case-insensitive)");
                }
            }
        }

        private void ExtractFolder(string targetPathLower)
        {
            var files = _provider.Files
                .Where(x => NormalizePath(x.Key).StartsWith(targetPathLower, StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                if (_provider.TrySavePackage(file.Key, out var packageData))
                {
                    var relativePath = NormalizePath(file.Key[targetPathLower.Length..]);
                    Console.WriteLine(relativePath);

                    var lastSlashIndex = relativePath.LastIndexOf('\\');

                    string subfolderPath = string.Empty;
                    if (lastSlashIndex != -1)
                    {
                        subfolderPath = relativePath[..lastSlashIndex];
                    }

                    string outputFilePath;
                    if (string.IsNullOrEmpty(subfolderPath))
                    {
                        outputFilePath = _options.OutputPath;
                    }
                    else
                    {
                        outputFilePath = Path.Combine(_options.OutputPath, subfolderPath);
                    }

                    WriteToFile(packageData, outputFilePath, file.Key);

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Extracted: {file.Key} to {outputFilePath}");
                    }
                }
                else if (_options.Verbose)
                {
                    Console.WriteLine($"Could not find or load file: {file.Key}");
                }
            }
        }

        private static void WriteToFile(IReadOnlyDictionary<string, byte[]> packageData, string outputDirectoryPath, string originalFilename)
        {
            string finalOutputPath = Path.Combine(Directory.CreateDirectory(outputDirectoryPath).FullName, Path.GetFileName(originalFilename));

            File.WriteAllBytes(finalOutputPath, packageData.First().Value);
        }

        private bool IsDirectory(string targetPathLower)
        {
            return _provider.Files.Any(x => NormalizePath(x.Key).StartsWith(targetPathLower, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizePath(string path) => path
            .Replace('/', '\\')
            .TrimStart('\\').TrimEnd('\\');

        private void DumpAllPaths()
        {
            var paths = _provider.Files.Keys.ToList();
            var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(Directory.CreateDirectory(_options.OutputPath).FullName, "paths.json"), json);

            if (_options.Verbose)
            {
                Console.WriteLine($"Dumped all virtual paths to: {Path.Combine(_options.OutputPath, "paths.json")}");
            }
        }
    }
}