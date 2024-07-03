using System;
using System.Runtime.InteropServices;
using StreamUtils;

namespace Galanthus.Utils;

public partial class ZLib
{
    public string Identifier => "ZLib";
    private const string NativeLibName = "ThirdParty/libzlib";

    [LibraryImport(NativeLibName)] internal static partial int compress(nuint dest, nuint destLen, nuint source, nuint sourceLen);
    [LibraryImport(NativeLibName)] internal static partial int uncompress(nuint dst, nuint dstCapacity, nuint source, nuint compressedSize);
    [LibraryImport(NativeLibName)] internal static partial IntPtr zError(int code);

    public static unsafe int Decompress<T>(Block<T> inData, ref Block<T> outData) where T : unmanaged
    {
        int destCapacity = outData.Size;
        int err = uncompress((nuint)outData.Ptr, (nuint)(&destCapacity), (nuint)inData.Ptr, (nuint)inData.Size);
        Error(err);
        return destCapacity;
    }

    public static unsafe void Compress<T>(Block<T> inData, ref Block<T> outData) where T : unmanaged
    {
        int err = compress((nuint)outData.Ptr, (nuint)outData.Size, (nuint)inData.Ptr, (nuint)inData.Size);
        Error(err);
    }

    private static unsafe void Error(int code)
    {
        if (code != 0)
        {
            string error = new((sbyte*)zError(code));
            throw new Exception(error);
        }
    }
}