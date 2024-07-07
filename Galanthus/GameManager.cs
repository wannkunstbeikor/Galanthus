using System;
using System.IO;
using StreamUtils;

namespace Galanthus;

public static class GameManager
{
    public static CompressionMethod CompressionMethod { get; private set; }
    public static string? CodeName { get; private set; }
    public static char Seperator { get; private set; } = '-';
    private static SdfToc? m_toc;

    /// <summary>
    /// Loads Snowdrop game.
    /// </summary>
    /// <param name="inDirectory">The directory of the game.</param>
    /// <param name="inOverrideMethod">If this is set to something other than <see cref="CompressionMethod.None"/> then it will override the automatic detection.</param>
    /// <returns></returns>
    public static bool LoadGame(string inDirectory, CompressionMethod inOverrideMethod = CompressionMethod.None)
    {
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
                case "moira":
                    CompressionMethod = CompressionMethod.ZStd;
                    break;
                case "onward":
                    CompressionMethod = CompressionMethod.Lz4;
                    break;
                case "hunter":
                case "ibex":
                case "rogue":
                case "camel":
                    CompressionMethod = CompressionMethod.ZLib;
                    break;
                default:
                    Console.WriteLine("Couldn't detect compression method, defaulting to zlib!");
                    CompressionMethod = CompressionMethod.ZLib;
                    break;
            }
        }

        foreach (string path in Directory.EnumerateFiles(Path.Combine(inDirectory, CodeName, "sdf/pc/data"), "*.sdfdata", SearchOption.AllDirectories))
        {
            if (path.Contains('_'))
            {
                Seperator = '_';
            }
            break;
        }

        // load main table of content
        using BlockStream stream = BlockStream.FromFile(Path.Combine(inDirectory, CodeName, "sdf/pc/data/sdf.sdftoc"));
        m_toc = SdfToc.Read(stream);
        if (m_toc is null)
        {
            return false;
        }

        return true;
    }
}