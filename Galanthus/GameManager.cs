using System;
using System.Collections.Generic;
using System.IO;
using StreamUtils;

namespace Galanthus;

public static class GameManager
{
    public static CompressionMethod CompressionMethod { get; set; }
    public static Platform Platform { get; set; }
    public static string? CodeName { get; private set; }
    public static string? GameDirectory { get; private set; }
    public static string? DataDirectory { get; private set; }
    public static char Separator { get; private set; } = '-';
    private static SdfToc? s_toc;

    /// <summary>
    /// Loads Snowdrop game.
    /// </summary>
    /// <param name="inDirectory">The directory of the game.</param>
    /// <param name="inPlatform">The <see cref="Platform"/> the game was built for.</param>
    /// <param name="inOverrideMethod">If this is set to something other than <see cref="CompressionMethod.None"/> then it will override the automatic detection.</param>
    /// <returns></returns>
    public static bool LoadGame(string inDirectory, Platform inPlatform, CompressionMethod inOverrideMethod = CompressionMethod.None)
    {
        GameDirectory = inDirectory;

        // check if the path is valid
        if (!Directory.Exists(inDirectory))
        {
            return false;
        }

        // get code name through containing folder of the .sdftoc files
        string? codeName = null;
        foreach (string path in Directory.EnumerateFiles(inDirectory, "*.sdftoc", SearchOption.AllDirectories))
        {
            codeName = Path.GetRelativePath(inDirectory, path);
            codeName = codeName[..codeName.IndexOf(Path.DirectorySeparatorChar)];
            break;
        }

        if (string.IsNullOrEmpty(codeName))
        {
            return false;
        }

        Platform = inPlatform;

        // setting code name
        CodeName = codeName;

        // get compression method from code name
        if (inOverrideMethod != CompressionMethod.None)
        {
            CompressionMethod = inOverrideMethod;
        }
        else
        {
            switch (CodeName)
            {
                case "moria":
                    CompressionMethod = CompressionMethod.ZStd;
                    break;
                case "onward":
                    CompressionMethod = CompressionMethod.Lz4;
                    break;
                case "hunter":
                case "ibex":
                case "rogue":
                case "camel":
                case "helix":
                    CompressionMethod = CompressionMethod.ZLib;
                    break;
                default:
                    Console.WriteLine("Couldn't detect compression method, defaulting to zlib!");
                    CompressionMethod = CompressionMethod.ZLib;
                    break;
            }
        }

        DataDirectory = Path.Combine(inDirectory, CodeName, "sdf", Platform.ToString().ToLower(), "data");

        foreach (string path in Directory.EnumerateFiles(DataDirectory, "*.sdfdata", SearchOption.AllDirectories))
        {
            if (path.Contains('_'))
            {
                Separator = '_';
            }
            break;
        }

        // load main table of content
        using BlockStream stream = BlockStream.FromFile(Path.Combine(DataDirectory, "sdf.sdftoc"));
        s_toc = SdfToc.Read(stream);
        if (s_toc is null)
        {
            return false;
        }

        HashSet<int> indices = new();

        // foreach (Structs.File file in s_toc.Files)
        // {
        //     foreach (DataSlice slice in file.DataSlices)
        //     {
        //
        //         if (!s_toc.TryGetDataFile(slice, out string? path))
        //         {
        //             Console.WriteLine($"Wrong index {slice.Index}");
        //             continue;
        //         }
        //
        //         FileInfo f = new(Path.Combine(inDirectory, CodeName, "sdf", Platform.ToString().ToLower(), "data",
        //             path));
        //
        //         if (!f.Exists)
        //         {
        //             Console.WriteLine($"Missing file {path}");
        //             indices.Add(slice.Index);
        //             continue;
        //         }
        //
        //         if (slice.Offset >= f.Length)
        //         {
        //             Console.WriteLine($"Invalid offset {slice.Offset} in {path} with length of {f.Length}");
        //             continue;
        //         }
        //
        //         if (slice.Offset + slice.CompressedSize > f.Length)
        //         {
        //             Console.WriteLine($"Slice exceeds file {path} by {slice.Offset + slice.CompressedSize - f.Length}");
        //             continue;
        //         }
        //     }
        // }

        return true;
    }

    public static string GetPath(string inName)
    {
        if (string.IsNullOrEmpty(DataDirectory) || string.IsNullOrEmpty(CodeName))
        {
            return string.Empty;
        }
        return Path.Combine(DataDirectory, inName);
    }

    public static Block<byte>? GetAssetData(string inName)
    {
        if (s_toc?.TryGetAsset(inName, out Structs.Asset file) != true)
        {
            return null;
        }
        return s_toc.GetData(file);
    }

    public static List<string>? GetAssetListing()
    {
        if (s_toc?.Assets == null)
        {
            return null;
        }
        return s_toc.Assets;
    }
}