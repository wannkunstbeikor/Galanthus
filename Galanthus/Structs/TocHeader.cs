namespace Galanthus.Structs;

public struct TocHeader
{
    public uint Magic;
    public uint Version;
    public int FileTableDecompressedSize;
    public int TocDataSize;
    public int FileTableCompressedSize;
    public int FirstDataSliceIndex;
    public int DataFileCount;
    public int DdsCount;
    public ushort CompressionType;
    public int SettingsCount;
    public uint DecompressedSize2;
    public int CompressedSize2;
    public uint Unk1;
    public uint Unk2;
    public uint Unk3;
    public uint Unk4;
};