using System.Collections.Generic;

namespace Galanthus.Structs;

public struct DataSlice
{
    public bool IsCompressed;
    public bool IsEncrypted;
    public bool IsOodle;
    public long DecompressedSize;
    public long CompressedSize;
    public long Offset;
    public ushort Index; // SDF File Index
    public List<int>? PageSizes;
    public int Sign;
}