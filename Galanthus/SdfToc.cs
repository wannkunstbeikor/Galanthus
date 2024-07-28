using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Galanthus.Structs;
using Galanthus.Utils;
using StreamUtils;

namespace Galanthus;

public class SdfToc : IDisposable
{
    public IEnumerable<File> Files => m_files;

    private TocHeader m_header;
    private List<Locale> m_locales;
    private List<Block<byte>> m_ddsHeaders;
    private List<File> m_files;
    private DataSliceIndexSettings m_settings;

    public SdfToc(TocHeader inHeader, List<Locale> inLocales, List<Block<byte>> inDdsHeaders, List<File> inFiles, DataSliceIndexSettings inSettings)
    {
        m_header = inHeader;
        m_locales = inLocales;
        m_ddsHeaders = inDdsHeaders;
        m_files = inFiles;
        m_settings = inSettings;
    }

    public static unsafe SdfToc? Read(DataStream inStream)
    {
        if (inStream.Length < 0x1C)
        {
            return null;
        }

        TocHeader header = new()
        {
            Magic = inStream.ReadUInt32(),
            Version = inStream.ReadUInt32(),
            FileTableDecompressedSize = inStream.ReadInt32(),
        };

        // be WEST
        if (header.Magic != 0x54534557)
        {
            return null;
        }

        // test which versions
        if (header.Version > 0x29)
        {
            return null;
        }

        if (header.Version == 0x17)
        {
            // compressed/encrypted section with datafile stuff and dds header
            header.TocDataSize = inStream.ReadInt32();
        }

        header.FileTableCompressedSize = inStream.ReadInt32();
        header.FirstDataSliceIndex = inStream.ReadInt32();
        header.DataFileCount = inStream.ReadInt32();
        if (header.Version != 0x17)
        {
            header.DdsCount = inStream.ReadInt32();
        }
        else
        {
            header.DdsCount = inStream.ReadUInt16();
            header.CompressionType = inStream.ReadUInt16();
        }


        if (header.Version >= 0x25)
        {
            header.SettingsCount = inStream.ReadInt32();

            if (GameManager.CodeName == "ibex")
            {
                // Rocksmith+ has an extra block of compressed data at the end
                header.DecompressedSize2 = inStream.ReadUInt32();
                header.CompressedSize2 = inStream.ReadInt32();
            }

            // these are not used directly when loading the toc format
            header.Unk1 = inStream.ReadUInt32(); // maybe some decompressedSize
            header.Unk2 = inStream.ReadUInt32(); // maybe some compressedSize
            header.Unk3 = inStream.ReadUInt32(); // looks like 2 int16, but read as int32
            header.Unk4 = inStream.ReadUInt32(); // looks like 2 int16, but read as int32
        }

        // validate start tag
        if (!Tag.TryReadTag(inStream, out _))
        {
            return null;
        }

        bool hasSign = inStream.ReadBoolean();

        bool isEncrypted = false;
        if (header.Version >= 0x25)
        {
            isEncrypted = inStream.ReadBoolean();
        }

        if (hasSign)
        {
            // just skip it
            inStream.Position += 0x140;
        }

        List<Locale> locales = new(header.Version >= 0x25 ? 10 : 0);
        List<Block<byte>> ddsHeaders = new(header.DdsCount);
        DataSliceIndexSettings settings = new();

        if (header.Version == 0x17 && header.CompressionType == 3)
        {
            // they compress it fuck my life
            using Block<byte> compressedTocData = new(header.TocDataSize);
            inStream.ReadExactly(compressedTocData);

            Block<byte> decomp = new(header.DataFileCount * 0x34 + header.DdsCount * 0xD4);

            Lz4.Decompress(compressedTocData, ref decomp);

            using (BlockStream stream = new(decomp))
            {
                // skip data file sizes
                stream.Position += sizeof(uint) * header.DataFileCount;

                // validate data file tags
                for (int i = 0; i < header.DataFileCount; i++)
                {
                    if (!Tag.TryReadTag(stream, out _))
                    {
                        return null;
                    }
                }

                for (int i = 0; i < header.DdsCount; i++)
                {
                    int size = stream.ReadInt32();
                    Block<byte> ddsHeader = new(size);
                    stream.ReadExactly(ddsHeader);
                    stream.Position += 0xD4 - sizeof(int) - size; // garbage im guessing (not always null)
                    ddsHeaders.Add(ddsHeader);
                }
            }
        }
        else
        {
            // index ranges
            for (int i = 0; i < header.SettingsCount; i++)
            {
                settings.SetValue(inStream.ReadUInt32(), inStream.ReadInt32());
            }

            if (header.Version >= 0x25)
            {
                for (int i = 0; i < 10; i++)
                {
                    locales.Add(new Locale(inStream));
                }
            }

            // skip data file sizes
            inStream.Position += sizeof(uint) * header.DataFileCount;

            // validate data file tags
            for (int i = 0; i < header.DataFileCount; i++)
            {
                if (!Tag.TryReadTag(inStream, out _))
                {
                    return null;
                }
            }

            if (header.Version >= 0x25)
            {
                // skip data file hashes
                inStream.Position += sizeof(ulong) * header.DataFileCount;
            }

            int ddsHeaderSize = 0x98;
            for (int i = 0; i < header.DdsCount; i++)
            {
                int size = inStream.ReadInt32();
                if (size > 0x94 && ddsHeaderSize != 0xCC)
                {
                    // HACK: some games use dx10 dds header, so we need bigger size
                    ddsHeaderSize = 0xCC;
                }
                Block<byte> ddsHeader = new(size);
                inStream.ReadExactly(ddsHeader);
                inStream.Position += ddsHeaderSize - sizeof(int) - size; // garbage im guessing (not always null)
                ddsHeaders.Add(ddsHeader);
            }
        }

        // compressed and sometimes encrypted file table
        using Block<byte> compressedFileTable = new(header.FileTableCompressedSize);
        inStream.ReadExactly(compressedFileTable);

        // validate end tag
        if (!Tag.TryReadTag(inStream, out _))
        {
            return null;
        }

        if (isEncrypted)
        {
            if (KeyManager.Key is null || KeyManager.Iv is null)
            {
                return null;
            }

            if (header.Version >= 0x29 && compressedFileTable.Size >= 8)
            {
                // first they use XTEA encryption, then they use des encryption
                XTEA((uint*)compressedFileTable.Ptr, 32);

                // DES-PCBC
                // Problem is c# doesnt have native support for it
                if (OpenSsl.DecryptDes((nuint)compressedFileTable.Ptr, (compressedFileTable.Size >> 3) << 3, (nuint)compressedFileTable.Ptr, (nuint)KeyManager.Key.Ptr,
                        (nuint)KeyManager.Iv.Ptr) != 0)
                {
                    return null;
                }
            }
            else if (compressedFileTable.Size >= 0x100)
            {
                // AES-192-OFB
                // Problem is c# doesnt have native support for it
                if (OpenSsl.DecryptAes((nuint)compressedFileTable.Ptr, 0x100, (nuint)compressedFileTable.Ptr, (nuint)KeyManager.Key.Ptr,
                        (nuint)KeyManager.Iv.Ptr) != 0)
                {
                    return null;
                }
            }
        }

        Block<byte> fileTable = new(header.FileTableDecompressedSize);
        switch (GameManager.CompressionMethod)
        {
            case CompressionMethod.ZLib:
                ZLib.Decompress(compressedFileTable, ref fileTable);
                break;
            case CompressionMethod.ZStd:
                ZStd.Decompress(compressedFileTable, ref fileTable);
                break;
            case CompressionMethod.Lz4:
                Lz4.Decompress(compressedFileTable, ref fileTable);
                break;
        }

        // Rocksmith has another compressed block here containing strings

        // parse file table
        using BlockStream subStream = new(fileTable);
        List<File> files = ParseFileTable(subStream, hasSign, header.Version);

        Console.WriteLine($"Got {files.Count} files.");

        return new SdfToc(header, locales, ddsHeaders, files, settings);
    }

    public bool TryGetDataFile(DataSlice inSlice, [NotNullWhen(true)] out string? path)
    {
        path = null;

        if (inSlice.Index > m_settings.MaxIndex)
        {
            return false;
        }

        char part;
        string? locale = null;

        if (inSlice.Index >= m_settings.StartIndexResidentMCacheFiles &&
            inSlice.Index <= m_settings.EndIndexResidentMCacheFiles)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexResidentMCacheFilesSecondary &&
                 inSlice.Index <= m_settings.EndIndexResidentMCacheFilesSecondary)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexAlwaysResident &&
            inSlice.Index <= m_settings.EndIndexAlwaysResident)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexFrontendResident &&
                 inSlice.Index <= m_settings.EndIndexFrontendResident)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexUnkResident &&
                 inSlice.Index <= m_settings.EndIndexUnkResident)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexResidentDuringLoadscreen &&
                 inSlice.Index <= m_settings.EndIndexResidentDuringLoadscreen)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexPartA &&
                 inSlice.Index <= m_settings.EndIndexPartA)
        {
            part = 'A';
        }
        else if (inSlice.Index >= m_settings.StartIndexPartB &&
                 inSlice.Index <= m_settings.EndIndexPartB)
        {
            part = 'B';
        }
        else if (inSlice.Index >= m_settings.StartIndexPartCLocalizedAudio &&
                 inSlice.Index <= m_settings.EndIndexPartCLocalizedAudio)
        {
            part = 'C';
            int localIndex = (inSlice.Index - m_settings.StartIndexPartCLocalizedAudio) / m_settings.IndexRangeSizeForPartCLocalizedAudio;
            locale = m_locales[localIndex].Name;
        }
        else if (inSlice.Index >= m_settings.StartIndexDlc &&
                 inSlice.Index <= m_settings.EndIndexDlc)
        {
            part = 'D';
        }
        else if (inSlice.Index >= m_settings.StartIndexPartEDownloadOnDemand &&
                 inSlice.Index <= m_settings.EndIndexPartEDownloadOnDemand)
        {
            part = 'E';
        }
        else
        {
            Console.WriteLine("Not Implemented Index range");
            return false;
        }

        // fuck mario rabbids sparks of hope
        char s = GameManager.Separator;
        if (locale is null)
        {
            path = $"sdf{s}{part}{s}{inSlice.Index:D4}.sdfdata";
        }
        else
        {
            path = $"sdf{s}{part}{s}{inSlice.Index:D4}{s}{locale}.sdfdata";
        }

        return true;
    }

    private static List<File> ParseFileTable(DataStream inStream, bool isSigned, uint inVersion)
    {
        List<File> retVal = new();
        ParseEntry(inStream, isSigned, string.Empty, retVal, inVersion);
        return retVal;
    }

    private static void ParseEntry(DataStream inStream, bool isSigned, string name, List<File> files, uint inVersion)
    {
        char id = inStream.ReadChar();

        if (id == 0)
        {
            throw new Exception();
        }

        if (id > 0 && id <= 0x1F)
        {
            name += inStream.ReadFixedSizedString(id);

            // next
            ParseEntry(inStream, isSigned, name, files, inVersion);

            return;
        }

        if (id <'A' || id > 'Z')
        {
            // pointer to next entry
            uint pNext = inStream.ReadUInt32();
            Debug.Assert(pNext < inStream.Length);
            ParseEntry(inStream, isSigned, name, files, inVersion);
            inStream.Position = pNext;
            ParseEntry(inStream, isSigned, name, files, inVersion);

            return;
        }

        int var = id - 'A';
        int count = var & (inVersion >= 0x29 ? 15 : 7);
        int flags = var >> (inVersion >= 0x29 ? 4 : 3);
        if (count <= 0)
        {
            Console.WriteLine($"Empty file {name} id: {id}");
            return;
        }

        uint hash = inStream.ReadUInt32(); // not unique
        byte packedStuff = inStream.ReadByte();
        int unk = packedStuff >> 2;
        int ddsIndex = (int)inStream.ReadSizedInt(packedStuff & 3, -1);

        File file = new()
        {
            Name = name,
            Hash = hash,
            DataSlices = new List<DataSlice>(count),
            DdsIndex = ddsIndex,
            Unk = unk
        };

        for (int i = 0; i < count; i++)
        {
            byte sizesAndFlags = inStream.ReadByte();
            bool isCompressed = (sizesAndFlags >> 5 & 1) != 0;
            bool isEncrypted = ((sizesAndFlags >> 6) & 1) != 0;

            long decompressedSize = inStream.ReadSizedInt((sizesAndFlags & 3) + 1);
            byte unk1 = 0;
            long compressedSize;
            if (isCompressed)
            {
                int size = (sizesAndFlags & 3) + 1;
                if (inVersion >= 0x29)
                {
                    unk1 = inStream.ReadByte(); // always 2
                    size = inStream.ReadByte();
                }
                compressedSize = inStream.ReadSizedInt(size);
            }
            else
            {
                compressedSize = decompressedSize;
            }

            long offset = inStream.ReadSizedInt((sizesAndFlags >> 2) & 7);

            ushort index = inStream.ReadUInt16();

            List<int>? pageSizes = null;
            if (isCompressed)
            {
                long pageCount = (decompressedSize + 0xffff) >> 16;
                pageSizes = new List<int>((int)pageCount);
                if (pageCount > 1 && (inVersion < 0x29 || !isEncrypted))
                {
                    for (int page = 0; page < pageCount; page++)
                    {
                        pageSizes.Add(inStream.ReadUInt16());
                    }
                }
                else
                {
                    pageSizes.Add((int)compressedSize);
                }
            }

            int sign = -1;
            if (isSigned)
            {
                sign = inStream.ReadInt32();
            }

            file.DataSlices.Add(new DataSlice()
            {
                DecompressedSize = decompressedSize,
                CompressedSize = compressedSize,
                IsCompressed = isCompressed,
                IsOodle = (sizesAndFlags >> 7 & 1) != 0,
                IsEncrypted = isEncrypted,
                Offset = offset,
                Index = index,
                PageSizes = pageSizes,
                Sign = sign,
                Unk1 = unk1,
            });
        }

        files.Add(file);

        if (flags != 0)
        {
            Debug.Assert(false, "Havent encountered");
            byte count2 = inStream.ReadByte();
            inStream.Position += 2 * count2;
        }
    }

    public static unsafe void XTEA(uint* v, uint numRounds)
    {
        uint[] key = [0xb, 0x11, 0x17, 0x1f];
        uint v0 = v[0], v1 = v[1], delta = 0x9E3779B9, sum = delta * numRounds;
        for (int i=0; i < numRounds; i++)
        {
            v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
            sum -= delta;
            v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
        }

        v[0] = v0;
        v[1] = v1;
    }

    public void Dispose()
    {
        foreach (Block<byte> header in m_ddsHeaders)
        {
            header.Dispose();
        }
    }
}