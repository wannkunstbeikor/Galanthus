using System.Runtime.InteropServices;

namespace Galanthus.Utils;

public partial class Crypto
{
    private const string NativeLibName = "ThirdParty/libcrypto";

    [LibraryImport(NativeLibName)]
    public static partial int DecryptAes(nuint inData, long inCount, nuint outData, nuint key, nuint iv);

    [LibraryImport(NativeLibName)]
    public static partial int DecryptDes(nuint inData, long inCount, nuint outData, nuint key, nuint iv);
}