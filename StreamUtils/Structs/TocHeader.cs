namespace StreamUtils.Structs;

public struct TocHeader
{
    public uint Magic;
    public uint Version;
    public int FileTableDecompressedSize;
    public int DataFileTableSize;
    public int FileTableCompressedSize;
    public int FirstDataSliceIndex;
    public int DataFileCount;
    public int DdsCount;
    public ushort CompressionType;
    public int UnkCount;
    public uint DecompressedSize2;
    public int CompressedSize2;
    public uint Unk1;
    public uint Unk2;
    public uint Unk3;
    public uint Unk4;
};