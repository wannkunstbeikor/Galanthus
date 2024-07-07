using System.Collections.Generic;

namespace Galanthus.Structs;

public struct File()
{
    public string Name = string.Empty;
    public uint Hash;
    public int DdsIndex;
    public int Unk;
    public List<DataSlice> DataSlices = new();
}