using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Galanthus.Structs;
using Galanthus.Utils;
using StreamUtils;

namespace Galanthus;

public class SdfToc : IDisposable
{
    public List<string> Assets => m_assets.Keys.ToList();

    private TocHeader m_header;
    private List<Locale> m_locales;
    private List<Block<byte>> m_ddsHeaders;
    private Dictionary<string, Asset> m_assets;
    private DataSliceIndexSettings m_settings;

    public SdfToc(TocHeader inHeader, List<Locale> inLocales, List<Block<byte>> inDdsHeaders, List<Asset> inAssets, DataSliceIndexSettings inSettings)
    {
        m_header = inHeader;
        m_locales = inLocales;
        m_ddsHeaders = inDdsHeaders;
        m_assets = inAssets.ToDictionary(f => f.Name);
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
        if (header.Version > 0x2A)
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

            int ddsHeaderSize = GameManager.CodeName == "moria" ? 0xCC : 0x98;
            for (int i = 0; i < header.DdsCount; i++)
            {
                int size = inStream.ReadInt32();
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
            Block<byte> tempBuffer = new(compressedFileTable.Ptr, compressedFileTable.Size);
            tempBuffer.MarkMemoryAsFragile();

            if (!DecryptBlock(header.Version, compressedFileTable, ref tempBuffer))
            {
                Console.WriteLine($"Toc failed to decrypt!");
                return null;
            }

            tempBuffer.Dispose();
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
        List<Asset> assets = ParseAssetTable(subStream, hasSign, header.Version);

        Console.WriteLine($"Got {assets.Count} assets.");

        return new SdfToc(header, locales, ddsHeaders, assets, settings);
    }

    public bool TryGetAsset(string inName, out Asset asset)
    {
        return m_assets.TryGetValue(inName, out asset);
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
        else if (inSlice.Index >= m_settings.StartIndexPartC &&
                 inSlice.Index <= m_settings.EndIndexPartC)
        {
            part = 'C';
        }
        else if (inSlice.Index >= m_settings.StartIndexDlc &&
                 inSlice.Index <= m_settings.EndIndexDlc)
        {
            part = 'D';
        }
        else if (inSlice.Index >= m_settings.StartIndexPartEDownloadOnDemand &&
                 inSlice.Index <= m_settings.EndIndexPartEDownloadOnDemand)
        {
            // TODO: figure out how these work
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
            path = GameManager.GetPath($"sdf{s}{part}{s}{inSlice.Index:D4}.sdfdata");
        }
        else
        {
            path = GameManager.GetPath($"sdf{s}{part}{s}{inSlice.Index:D4}{s}{locale}.sdfdata");
        }

        if (File.Exists(path))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public unsafe Block<byte>? GetData(Asset inAsset)
    {
        // get final file size
        int outBufferSize = 0;
        int ddsHeaderSize = 0;
        if (inAsset.DdsIndex != -1)
        {
            outBufferSize += m_ddsHeaders[inAsset.DdsIndex].Size;
            ddsHeaderSize = m_ddsHeaders[inAsset.DdsIndex].Size;
        }

        foreach (DataSlice dataSlice in inAsset.DataSlices)
        {
            if (!TryGetDataFile(dataSlice, out string? _))
            {
                continue;
            }

            outBufferSize += (int)dataSlice.DecompressedSize;
        }

        // probably a localised file that isn't installed so just return null before we do anything
        if (outBufferSize - ddsHeaderSize <= 0 && inAsset.DataSlices[0].DecompressedSize != 0)
        {
            return null;
        }

        Block<byte> outBuffer = new(outBufferSize);

        // add dds header data
        if (inAsset.DdsIndex != -1)
        {
            m_ddsHeaders[inAsset.DdsIndex].CopyTo(outBuffer);
            outBuffer.Shift(m_ddsHeaders[inAsset.DdsIndex].Size);
        }

        // read slices
        foreach (DataSlice dataSlice in inAsset.DataSlices)
        {
            if (!TryGetDataFile(dataSlice, out string? path))
            {
                continue;
            }

            using (DataStream stream = BlockStream.FromFile(path, dataSlice.Offset, (int)dataSlice.CompressedSize))
            {
                // read whole slice into buffer
                Block<byte> compressedBuffer = new((int)dataSlice.CompressedSize);
                stream.ReadExactly(compressedBuffer);

                // decompress slice if needed
                if (dataSlice.IsCompressed)
                {
                    int pageSize = 0x10000;
                    int decompressedOffset = 0;

                    // iterate through pages
                    for (int i = 0; i < dataSlice.PageSizes!.Count; i++)
                    {
                        int decompressedSize = (int)Math.Min(dataSlice.DecompressedSize - decompressedOffset, pageSize);
                        if (dataSlice.PageSizes!.Count == 1)
                        {
                            decompressedSize = (int)dataSlice.DecompressedSize;
                        }

                        if (dataSlice.PageSizes[i] == 0 || decompressedSize == dataSlice.PageSizes[i])
                        {
                            // uncompressed page
                            // set up temp buffer with only the page data
                            Block<byte> tempBuffer = new(compressedBuffer.Ptr, decompressedSize);
                            tempBuffer.MarkMemoryAsFragile();

                            // decrypt page
                            if (dataSlice.IsEncrypted)
                            {
                                if (!DecryptBlock(m_header.Version, tempBuffer, ref outBuffer))
                                {
                                    Console.WriteLine("Page failed to decrypt!");
                                    break;
                                }
                            }
                            else
                            {
                                tempBuffer.CopyTo(outBuffer, decompressedSize);
                            }

                            tempBuffer.Dispose();
                            compressedBuffer.Shift(decompressedSize);
                        }
                        else
                        {
                            // compressed page
                            // set up temp buffer with only the page data
                            Block<byte> tempBuffer = new(compressedBuffer.Ptr, dataSlice.PageSizes[i]);
                            tempBuffer.MarkMemoryAsFragile();

                            // decrypt page
                            if (dataSlice.IsEncrypted)
                            {
                                if (!DecryptBlock(m_header.Version, tempBuffer, ref tempBuffer))
                                {
                                    Console.WriteLine("Page failed to decrypt!");
                                    break;
                                }
                            }

                            if (!dataSlice.IsOodle)
                            {
                                switch (GameManager.CompressionMethod)
                                {
                                    case CompressionMethod.ZLib:
                                        ZLib.Decompress(tempBuffer, ref outBuffer);
                                        break;
                                    case CompressionMethod.ZStd:
                                        ZStd.Decompress(tempBuffer, ref outBuffer);
                                        break;
                                    case CompressionMethod.Lz4:
                                        Lz4.Decompress(tempBuffer, ref outBuffer);
                                        break;
                                }
                            }
                            else
                            {
                                // oodle is annoying and won't work unless the output buffer is exactly as big as the decompressed data
                                Block<byte> oodleOutBuffer = new(outBuffer.Ptr, decompressedSize);
                                oodleOutBuffer.MarkMemoryAsFragile();
                                Oodle.Decompress(tempBuffer, ref oodleOutBuffer);
                                oodleOutBuffer.Dispose();
                            }
                            tempBuffer.Dispose();
                            compressedBuffer.Shift(dataSlice.PageSizes[i]);
                        }

                        decompressedOffset += decompressedSize;
                        outBuffer.Shift(decompressedSize);
                    }
                }
                else
                {
                    // decrypt whole slice
                    if (dataSlice.IsEncrypted)
                    {
                        if (!DecryptBlock(m_header.Version, compressedBuffer, ref outBuffer))
                        {
                            Console.WriteLine("Slice failed to decrypt!");
                            break;
                        }
                    }
                    else
                    {
                        compressedBuffer.CopyTo(outBuffer, (int)dataSlice.DecompressedSize);
                    }

                    outBuffer.Shift((int)dataSlice.DecompressedSize);
                }

                compressedBuffer.Dispose();
            }
        }

        outBuffer.ResetShift();
        return outBuffer;
    }

    private static List<Asset> ParseAssetTable(DataStream inStream, bool isSigned, uint inVersion)
    {
        List<Asset> retVal = new();
        ParseEntry(inStream, isSigned, string.Empty, retVal, inVersion);
        return retVal;
    }

    private static void ParseEntry(DataStream inStream, bool isSigned, string name, List<Asset> files, uint inVersion)
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

        Asset asset = new()
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
            bool isEncrypted = (sizesAndFlags >> (inVersion >= 0x29 ? 7 : 6) & 1) != 0;
            bool noPageSize = inVersion >= 0x29 && (sizesAndFlags >> 6 & 1) != 0;

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
                if (pageCount > 1 && !noPageSize)
                {
                    int sum = 0;
                    for (int page = 0; page < pageCount; page++)
                    {
                        int size = inStream.ReadUInt16();
                        pageSizes.Add(size);
                        if (size == 0)
                        {
                            size = 0x10000;
                        }
                        sum += size;
                    }
                    Debug.Assert(sum == compressedSize, "Invalid page sizes");
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

            asset.DataSlices.Add(new DataSlice
            {
                DecompressedSize = decompressedSize,
                CompressedSize = compressedSize,
                IsCompressed = isCompressed,
                IsOodle = inVersion >= 0x29 || (sizesAndFlags >> 7 & 1) != 0,
                IsEncrypted = isEncrypted,
                Offset = offset,
                Index = index,
                PageSizes = pageSizes,
                Sign = sign,
                Unk1 = unk1,
            });
        }

        files.Add(asset);

        if (flags != 0)
        {
            Debug.Assert(false, "Havent encountered");
            Console.WriteLine("Unknown thing");
            byte count2 = inStream.ReadByte();
            inStream.Position += 2 * count2;
        }
    }

    public static unsafe bool DecryptBlock(uint tocVersion, Block<Byte> inBuf, ref Block<Byte> outBuf)
    {
        if (KeyManager.Key is null || KeyManager.Iv is null)
        {
            return false;
        }

        if (tocVersion >= 0x29 && inBuf.Size >= 8)
        {
            // first they use XTEA encryption, then they use des encryption
            XTEA((uint*)inBuf.Ptr, 32);

            // DES-PCBC
            // Problem is c# doesnt have native support for it
            if (Crypto.DecryptDes((nuint)inBuf.Ptr, (inBuf.Size >> 3) << 3, (nuint)outBuf.Ptr, (nuint)KeyManager.Key.Ptr,
                    (nuint)KeyManager.Iv.Ptr) != 0)
            {
                return false;
            }
        }
        else if (inBuf.Size >= 0x100)
        {
            // AES-192-OFB
            // Problem is c# doesnt have native support for it
            if (Crypto.DecryptAes((nuint)inBuf.Ptr, 0x100, (nuint)outBuf.Ptr, (nuint)KeyManager.Key.Ptr,
                    (nuint)KeyManager.Iv.Ptr) != 0)
            {
                return false;
            }
        }

        return true;
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