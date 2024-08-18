using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StreamUtils;

namespace Galanthus.Utils;

public static partial class Oodle
{
    private const string NativeLibName = "ThirdParty/oo2core";

    internal enum OodleLZ_FuzzSafe
    {
        No = 0,
        Yes = 1
    }

    internal enum OodleLZ_CheckCRC
    {
        No = 0,
        Yes = 1,
        Force32 = 0x40000000
    }

    internal enum OodleLZ_Verbosity
    {
        None = 0,
        Minimal = 1,
        Some = 2,
        Lots = 3,
        Force32 = 0x40000000
    }

    internal enum OodleLZ_Decode_ThreadPhase
    {
        ThreadPhase1 = 1,
        ThreadPhase2 = 2,
        ThreadPhaseAll = 3,
        Unthreaded = ThreadPhaseAll
    }

    internal enum OodleLZ_Compressor
    {
        OodleLZ_Compressor_Invalid = -1,
        OodleLZ_Compressor_None = 3,

        OodleLZ_Compressor_Kraken = 8,
        OodleLZ_Compressor_Leviathan = 13,
        OodleLZ_Compressor_Mermaid = 9,
        OodleLZ_Compressor_Selkie = 11,
        OodleLZ_Compressor_Hydra = 12,

        OodleLZ_Compressor_Count = 14,
        OodleLZ_Compressor_Force32 = 0x40000000
    }

    internal  enum OodleLZ_CompressionLevel
    {
        OodleLZ_CompressionLevel_None=0,
        OodleLZ_CompressionLevel_SuperFast=1,
        OodleLZ_CompressionLevel_VeryFast=2,
        OodleLZ_CompressionLevel_Fast=3,
        OodleLZ_CompressionLevel_Normal=4,

        OodleLZ_CompressionLevel_Optimal1=5,
        OodleLZ_CompressionLevel_Optimal2=6,
        OodleLZ_CompressionLevel_Optimal3=7,
        OodleLZ_CompressionLevel_Optimal4=8,
        OodleLZ_CompressionLevel_Optimal5=9,

        OodleLZ_CompressionLevel_HyperFast1=-1,
        OodleLZ_CompressionLevel_HyperFast2=-2,
        OodleLZ_CompressionLevel_HyperFast3=-3,
        OodleLZ_CompressionLevel_HyperFast4=-4,

        OodleLZ_CompressionLevel_HyperFast=OodleLZ_CompressionLevel_HyperFast1,
        OodleLZ_CompressionLevel_Optimal = OodleLZ_CompressionLevel_Optimal2,
        OodleLZ_CompressionLevel_Max     = OodleLZ_CompressionLevel_Optimal5,
        OodleLZ_CompressionLevel_Min     = OodleLZ_CompressionLevel_HyperFast4,

        OodleLZ_CompressionLevel_Force32 = 0x40000000,
        OodleLZ_CompressionLevel_Invalid = OodleLZ_CompressionLevel_Force32
    }

    [LibraryImport(NativeLibName)]
    internal static partial nuint OodleLZ_Decompress(nuint compBuf, nuint compBufSize, nuint rawBuf, nuint rawLen,
        OodleLZ_FuzzSafe fuzzSafe = OodleLZ_FuzzSafe.No, OodleLZ_CheckCRC checkCRC = OodleLZ_CheckCRC.No,
        OodleLZ_Verbosity verbosity = OodleLZ_Verbosity.None, nuint decBufBase = 0, nuint decBufSize = 0,
        nuint fpCallback = 0, nuint callbackUserData = 0, nuint decoderMemory = 0, nuint decoderMemorySize = 0,
        OodleLZ_Decode_ThreadPhase threadPhase = OodleLZ_Decode_ThreadPhase.Unthreaded);

    public static unsafe void Decompress<T>(Block<T> inData, ref Block<T> outData) where T : unmanaged
    {
        nuint retCode = OodleLZ_Decompress((nuint)inData.Ptr, (nuint)inData.Size, (nuint)outData.Ptr, (nuint)outData.Size);
        if (retCode == 0)
        {
            throw new Exception("An Oodle operation failed.");
        }
    }
}