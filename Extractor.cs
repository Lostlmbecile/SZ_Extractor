using System;
using System.IO;
using System.Linq;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Text.Json;
using CUE4Parse.UE4.VirtualFileSystem;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UE_Extractor
{
    public class Extractor
    {
        private readonly Options _options;
        private readonly DefaultFileProvider _provider;
        private readonly Dictionary<string, List<string>> _duplicates;

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

            // Initialize _duplicates here
            _duplicates = FindDuplicateFiles();

            if (_options.DumpPaths)
            {
                DumpAllPaths();
            }
        }

        private Dictionary<string, List<string>> FindDuplicateFiles()
        {
            var fileLocations = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var vfs in _provider.MountedVfs)
            {
                foreach (var file in vfs.Files)
                {
                    var normalizedPath = NormalizePath(file.Key);
                    if (!fileLocations.ContainsKey(normalizedPath))
                    {
                        fileLocations[normalizedPath] = new List<string>();
                    }
                    fileLocations[normalizedPath].Add(Path.GetFileNameWithoutExtension(vfs.Name));
                }
            }
            return fileLocations.Where(kv => kv.Value.Count > 1).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Run()
        {
            var targetPathLower = NormalizePath(_options.ContentPath).ToLowerInvariant();
            if (_options.Verbose)
            {
                Console.WriteLine($"Searching for: {_options.ContentPath} (case-insensitive)");
            }

            bool anyExtractionSuccessful = false;
            foreach (var vfs in _provider.MountedVfs)
            {
                if (IsDirectory(targetPathLower, vfs))
                {
                    if (ExtractFolder(targetPathLower, vfs))
                    {
                        anyExtractionSuccessful = true;
                    }
                }
                else
                {
                    if (ExtractFile(targetPathLower, vfs))
                    {
                        anyExtractionSuccessful = true;
                    }
                }
            }

            if (anyExtractionSuccessful)
            {
                Console.WriteLine("Extraction completed successfully for at least one file/folder.");
            }
            else
            {
                Console.WriteLine($"Could not find or load file/folder: {_options.ContentPath} (case-insensitive) in any mounted archive.");
            }
        }

        private bool ExtractFile(string targetPathLower, IAesVfsReader vfs, string? archiveNameOverride = null)
        {
            bool isFilenameOnly = !targetPathLower.Contains('\\');

            // Find all matching files (either full path or filename match)
            var fileEntries = vfs.Files.Where(f =>
                isFilenameOnly
                    ? NormalizePath(f.Key).EndsWith(targetPathLower, StringComparison.OrdinalIgnoreCase)
                    : NormalizePath(f.Key).Equals(targetPathLower, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (fileEntries.Count == 0)
            {
                return false;
            }

            bool extractionSuccess = false;
            foreach (var fileEntry in fileEntries)
            {
                if (_provider.TrySavePackage(fileEntry.Key, out var packageData))
                {
                    string archiveName = archiveNameOverride ?? Path.GetFileNameWithoutExtension(vfs.Name);
                    string outputDirectory = _options.OutputPath;

                    // Handle duplicates (different handling for filename-only mode)
                    if (isFilenameOnly)
                    {
                        // If filename-only, use the full path from the entry to determine duplicates
                        if (_duplicates.ContainsKey(NormalizePath(fileEntry.Key)))
                        {
                            outputDirectory = Path.Combine(outputDirectory, archiveName);
                        }
                    }
                    else
                    {
                        // If full path, use the provided target path for duplicate handling
                        if (_duplicates.ContainsKey(targetPathLower))
                        {
                            outputDirectory = Path.Combine(outputDirectory, archiveName);
                        }
                    }

                    WriteToFile(packageData, outputDirectory, fileEntry.Value.Name);

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Extracted: {Path.GetFileName(fileEntry.Key)} from {archiveName} to {outputDirectory}");
                    }
                    extractionSuccess = true;
                }
            }
            return extractionSuccess;
        }

        private bool ExtractFolder(string targetPathLower, IAesVfsReader vfs)
        {
            var files = vfs.Files
                .Where(x => NormalizePath(x.Key).StartsWith(targetPathLower, StringComparison.OrdinalIgnoreCase));

            bool extractionSuccess = false;
            foreach (var file in files)
            {
                if (_provider.TrySavePackage(file.Key, out var packageData))
                {
                    var relativePath = NormalizePath(file.Key[targetPathLower.Length..]);
                    var lastSlashIndex = relativePath.LastIndexOf('\\');

                    string subfolderPath = string.Empty;
                    if (lastSlashIndex != -1)
                    {
                        subfolderPath = relativePath[..lastSlashIndex];
                    }

                    string outputDirectory = _options.OutputPath;

                    // Handle duplicates by adding archive name to the path
                    string archiveName = Path.GetFileNameWithoutExtension(vfs.Name);
                    if (_duplicates.ContainsKey(NormalizePath(file.Key)))
                    {
                        outputDirectory = Path.Combine(outputDirectory, archiveName);
                    }

                    if (!string.IsNullOrEmpty(subfolderPath))
                    {
                        outputDirectory = Path.Combine(outputDirectory, subfolderPath);
                    }

                    WriteToFile(packageData, outputDirectory, file.Value.Name);

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Extracted: {file.Key} from {archiveName} to {outputDirectory}");
                    }
                    extractionSuccess = true;
                }
            }
            return extractionSuccess;
        }

        private static void WriteToFile(IReadOnlyDictionary<string, byte[]> packageData, string outputDirectoryPath, string originalFilename)
        {
            string finalOutputPath = Path.Combine(Directory.CreateDirectory(outputDirectoryPath).FullName, Path.GetFileName(originalFilename));

            File.WriteAllBytesAsync(finalOutputPath, packageData.First().Value);
        }

        private static bool IsDirectory(string targetPathLower, IAesVfsReader vfs)
        {
            return vfs.Files.Any(x => NormalizePath(x.Key).StartsWith(targetPathLower, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizePath(string path) => path
            .Replace('/', '\\')
            .TrimStart('\\').TrimEnd('\\');

        private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };
        private class DuplicateEntry
        {
            [JsonPropertyName("path")]
            public required string Path { get; set; }

            [JsonPropertyName("archives")]
            public required List<string> Archives { get; set; }
        }

        private void DumpAllPaths()
        {
            var output = new Dictionary<string, object>
            {
                ["duplicates"] = new List<DuplicateEntry>() // Use List<DuplicateEntry>
            };

            foreach (var vfs in _provider.MountedVfs)
            {
                var archiveName = Path.GetFileNameWithoutExtension(vfs.Name);
                output[archiveName] = new List<string>(); // Store file paths for this archive

                foreach (var file in vfs.Files)
                {
                    var normalizedPath = NormalizePath(file.Key);
                    ((List<string>)output[archiveName]).Add(normalizedPath); // Add path to archive's list

                    // Add to duplicates list if necessary
                    if (_duplicates.TryGetValue(normalizedPath, out var archiveNames))
                    {
                        var existingDuplicateEntry = ((List<DuplicateEntry>)output["duplicates"])
                            .FirstOrDefault(d => d.Path == normalizedPath);

                        if (existingDuplicateEntry != null)
                        {
                            // Add archive names to the existing entry
                            existingDuplicateEntry.Archives.AddRange(archiveNames);
                            existingDuplicateEntry.Archives = existingDuplicateEntry.Archives.Distinct().ToList();
                        }
                        else
                        {
                            // Create a new DuplicateEntry
                            var duplicateEntry = new DuplicateEntry
                            {
                                Path = normalizedPath,
                                Archives = archiveNames
                            };
                            ((List<DuplicateEntry>)output["duplicates"]).Add(duplicateEntry);
                        }
                    }
                }
            }

            var json = JsonSerializer.Serialize(output, _serializerOptions);
            File.WriteAllText(Path.Combine(Directory.CreateDirectory(_options.OutputPath).FullName, "paths.json"), json);

            if (_options.Verbose)
            {
                Console.WriteLine($"Dumped all virtual paths to: {Path.Combine(_options.OutputPath, "paths.json")}");
            }
        }
    }
}