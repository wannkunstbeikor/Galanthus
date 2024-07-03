using System;
using System.Collections.Generic;
using System.Diagnostics;
using Galanthus.Utils;
using StreamUtils;
using StreamUtils.Structs;

namespace Galanthus;

public class SdfToc : IDisposable
{
    private TocHeader m_header;
    private List<Unk> m_unks;
    private List<Locale> m_locales;
    private List<Block<byte>> m_ddsHeaders;
    private List<File> m_files;

    public SdfToc(TocHeader inHeader, List<Unk> inUnks, List<Locale> inLocales, List<Block<byte>> inDdsHeaders, List<File> inFiles)
    {
        m_header = inHeader;
        m_unks = inUnks;
        m_locales = inLocales;
        m_ddsHeaders = inDdsHeaders;
        m_files = inFiles;
    }

    public static unsafe SdfToc? Read(DataStream inStream, CompressionMethod inMethod)
    {
        if (inStream.Length < 0x30)
        {
            return null;
        }
        bool isRocksmith = false;
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
            header.DataFileTableSize = inStream.ReadInt32();
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
            header.UnkCount = inStream.ReadInt32();

            if (isRocksmith)
            {
                header.DecompressedSize2 = inStream.ReadUInt32();
                header.CompressedSize2 = inStream.ReadInt32();
            }

            header.Unk1 = inStream.ReadUInt32();
            header.Unk2 = inStream.ReadUInt32();
            header.Unk3 = inStream.ReadUInt32();
            header.Unk4 = inStream.ReadUInt32();
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

        List<Unk> unks = new(header.UnkCount);
        List<Locale> locales = new(header.Version >= 0x25 ? 10 : 0);
        List<Block<byte>> ddsHeaders = new(header.DdsCount);

        if (header.Version == 0x17 && header.CompressionType == 3)
        {
            // they compress it fuck my life
            using Block<byte> weird = new(header.DataFileTableSize);
            inStream.ReadExactly(weird);

            Block<byte> decomp = new(header.DataFileCount * 0x34 + header.DdsCount * 0xD4);

            Lz4.Decompress(weird, ref decomp);

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
            for (int i = 0; i < header.UnkCount; i++)
            {
                unks.Add(new Unk(inStream));
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

        if (isEncrypted && compressedFileTable.Size >= 0x100)
        {
            if (KeyManager.Key is null || KeyManager.Iv is null)
            {
                return null;
            }

            // AES-192-OFB
            // Problem is c# doesnt have native support for it
            if (OpenSsl.Decrypt((nuint)compressedFileTable.Ptr, 0x100, (nuint)compressedFileTable.Ptr, (nuint)KeyManager.Key.Ptr,
                    (nuint)KeyManager.Iv.Ptr) != 0)
            {
                return null;
            }
        }

        Block<byte> fileTable = new(header.FileTableDecompressedSize);
        switch (inMethod)
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
        List<File> files = ParseFileTable(subStream, hasSign);

        return new SdfToc(header, unks, locales, ddsHeaders, files);
    }

    private static List<File> ParseFileTable(DataStream inStream, bool isSigned)
    {
        List<File> retVal = new();
        ParseEntry(inStream, isSigned, string.Empty, retVal);
        return retVal;
    }

    private static void ParseEntry(DataStream inStream, bool isSigned, string name, List<File> files)
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
            ParseEntry(inStream, isSigned, name, files);

            return;
        }

        if (id >= 'A' && id <= 'Z')
        {
            int var = id - 'A';
            int count = var & 7;
            int flags = (id >> 3) & 1;
            if (count <= 0)
            {
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
                Slices = new List<DataSlice>(count),
                DdsIndex = ddsIndex,
                Unk = unk
            };

            for (int i = 0; i < count; i++)
            {
                byte sizesAndFlags = inStream.ReadByte();
                bool isCompressed = (sizesAndFlags >> 5 & 1) != 0;

                long decompressedSize = inStream.ReadSizedInt((sizesAndFlags & 3) + 1);
                long compressedSize;
                if (isCompressed)
                {
                    compressedSize = inStream.ReadSizedInt((sizesAndFlags & 3) + 1);
                }
                else
                {
                    compressedSize = decompressedSize;
                }

                long dataSliceOffset = inStream.ReadSizedInt((sizesAndFlags >> 2) & 7);

                ushort index = inStream.ReadUInt16();

                List<int>? pageSizes = null;
                if (isCompressed)
                {
                    long pageCount = (decompressedSize + 0xffff) >> 16;
                    pageSizes = new List<int>((int)pageCount);
                    if (pageCount > 1)
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

                file.Slices.Add(new DataSlice()
                {
                    DecompressedSize = decompressedSize,
                    CompressedSize = compressedSize,
                    IsCompressed = isCompressed,
                    IsOodle = ((sizesAndFlags >> 6) & 1) != 0,
                    IsEncrypted = ((sizesAndFlags >> 6) & 1) != 0,
                    Offset = dataSliceOffset,
                    Index = index,
                    PageSizes = pageSizes,
                    Sign = sign,
                });
            }

            files.Add(file);

            if (flags != 0)
            {
                Debug.Assert(false);
                byte count2 = inStream.ReadByte();
                inStream.Position += 2 * count2;
            }
            return;
        }

        // pointer to next entry
        uint offset = inStream.ReadUInt32();
        Debug.Assert(offset < inStream.Length);
        ParseEntry(inStream, isSigned, name, files);
        inStream.Position = offset;
        ParseEntry(inStream, isSigned, name, files);
    }

    public void Dispose()
    {
        foreach (Block<byte> header in m_ddsHeaders)
        {
            header.Dispose();
        }
    }
}