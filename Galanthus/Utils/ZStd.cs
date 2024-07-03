using System;
using System.Runtime.InteropServices;
using StreamUtils;

namespace Galanthus.Utils;

public partial class ZStd
{
    public string Identifier => "ZStandard";
    private const string NativeLibName = "ThirdParty/libzstd";

    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_getErrorName(nuint code);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_isError(nuint code);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_createDDict(nuint dict, nuint dictSize);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_createDCtx();
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_freeDCtx(nuint dctx);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_decompress(nuint dst, nuint dstCapacity, nuint src, nuint compressedSize);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_decompress_usingDDict(nuint dctx, nuint dst, nuint dstCapacity, nuint src, nuint srcSize, nuint dict);
    [LibraryImport(NativeLibName)] internal static partial nuint ZSTD_compress(nuint dst, nuint dstCapacity, nuint src, nuint srcSize);

    /// <summary>
    /// Checks if the specified code is a valid ZStd error.
    /// </summary>
    private static unsafe void GetError(nuint code)
    {
        if (ZSTD_isError(code) != nuint.Zero)
        {
            string error = new((sbyte*)ZSTD_getErrorName(code));
            throw new Exception($"A ZStandard operation failed with error: \"{error}\"");
        }
    }

    public static unsafe void Decompress<T>(Block<T> inData, ref Block<T> outData) where T : unmanaged
    {
        nuint code = ZSTD_decompress((nuint)outData.Ptr, (nuint)outData.Size, (nuint)inData.Ptr, (nuint)inData.Size);
        GetError(code);
    }

    public static unsafe void Compress<T>(Block<T> inData, ref Block<T> outData) where T : unmanaged
    {
        nuint code = ZSTD_compress((nuint)outData.Ptr, (nuint)outData.Size, (nuint)inData.Ptr, (nuint)inData.Size);
        GetError(code);
    }
}