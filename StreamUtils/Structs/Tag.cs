namespace StreamUtils.Structs;

public struct Tag
{
    public readonly ulong Massive = 0x006576697373616D;
    public byte[] Hash = new byte[0x20];
    public readonly ulong Ubisoft = 0x0074666F73696275;

    public Tag(DataStream inStream)
    {
        Massive = inStream.ReadUInt64();
        Hash = inStream.ReadBytes(0x20);
        Ubisoft = inStream.ReadUInt64();
    }

    public static bool TryReadTag(DataStream inStream, out Tag tag)
    {
        tag = new Tag(inStream);

        if (tag.Massive != 0x006576697373616D || tag.Ubisoft != 0x0074666F73696275)
        {
            return false;
        }

        return true;
    }
}