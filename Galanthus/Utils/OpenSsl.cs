using System.Runtime.InteropServices;

namespace Galanthus.Utils;

public partial class OpenSsl
{
    private const string NativeLibName = "ThirdParty/libopenssl";

    [LibraryImport(NativeLibName)]
    public static partial int Decrypt(nuint inData, long inCount, nuint outData, nuint key, nuint iv);
}