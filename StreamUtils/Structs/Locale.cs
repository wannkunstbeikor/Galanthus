namespace StreamUtils.Structs;

public struct Locale
{
    public string Name = string.Empty;

    public Locale(DataStream inStream)
    {
        Name = inStream.ReadFixedSizedString(6);
    }
}