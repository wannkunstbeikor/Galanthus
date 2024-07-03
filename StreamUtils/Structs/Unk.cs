namespace StreamUtils.Structs;

public struct Unk
{
    public uint Hash;
    public int Count;

    public Unk(DataStream inStream)
    {
        Hash = inStream.ReadUInt32();
        Count = inStream.ReadInt32();
    }
}