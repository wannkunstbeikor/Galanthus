using System;

namespace Galanthus.Structs;

public struct DataSliceIndexSettings
{
    private const uint c_mfSdfIndexRangeSizeForPartCLocalizedAudio = 0x353CFA07; // MF_SDF_INDEX_RANGE_SIZE_FOR_PARTC_LOCALIZED_AUDIO
    private const uint c_mfSdfIndexRangeSizeForDlc = 0x7E54C40B; // MF_SDF_INDEX_RANGE_SIZE_FOR_DLC

    // A
    private const uint c_mfSdfDataSliceStartIndexResidentMCacheFiles = 0x2F315819; // MF_SDF_DATASLICE_START_INDEX_RESIDENT_MCACHE_FILES
    private const uint c_mfSdfDataSliceEndIndexResidentMCacheFiles = 0xAD484616; // MF_SDF_DATASLICE_END_INDEX_RESIDENT_MCACHE_FILES
    private const uint c_mfSdfDataSliceStartIndexResidentMCacheFilesSecondary = 0x17C849D; // MF_SDF_DATASLICE_START_INDEX_RESIDENT_MCACHE_SECONDARY
    private const uint c_mfSdfDataSliceEndIndexResidentMCacheFilesSecondary = 0x65299533; // MF_SDF_DATASLICE_END_INDEX_RESIDENT_MCACHE_SECONDARY
    private const uint c_mfSdfDataSliceStartIndexAlwaysResident = 0x66D1031A; // MF_SDF_DATASLICE_START_INDEX_ALWAYS_RESIDENT
    private const uint c_mfSdfDataSliceEndIndexAlwaysResident = 0xDA9609CF; // MF_SDF_DATASLICE_END_INDEX_ALWAYS_RESIDENT
    private const uint c_mfSdfDataSliceStartIndexFrontendResident = 0x7BB0C6B; // MF_SDF_DATASLICE_START_INDEX_FRONTEND_RESIDENT
    private const uint c_mfSdfDataSliceEndIndexFrontendResident = 0xFA2057BD; // MF_SDF_DATASLICE_END_INDEX_FRONTEND_RESIDENT

    private const uint c_mfSdfDataSliceStartIndexUnkResident = 0x24303CB0; // MF_SDF_DATASLICE_START_INDEX_...
    private const uint c_mfSdfDataSliceEndIndexUnkResident = 0x38CF66B4; // MF_SDF_DATASLICE_END_INDEX_...

    private const uint c_mfSdfDataSliceStartIndexResidentDuringLoadscreen = 0x1B1AA960; // MF_SDF_DATASLICE_START_INDEX_RESIDENT_DURING_LOADSCREEN
    private const uint c_mfSdfDataSliceEndIndexResidentDuringLoadscreen = 0x675DE6B7; // MF_SDF_DATASLICE_END_INDEX_RESIDENT_DURING_LOADSCREEN
    private const uint c_mfSdfDataSliceStartIndexPartA = 0x7CDE36F; // MF_SDF_DATASLICE_START_INDEX_PARTA
    private const uint c_mfSdfDataSliceEndIndexPartA = 0xDC42F4E5; // MF_SDF_DATASLICE_End_INDEX_PARTA

    // B
    private const uint c_mfSdfDataSliceStartIndexPartB = 0x9243CB90; // MF_SDF_DATASLICE_START_INDEX_PARTB
    private const uint c_mfSdfDataSliceEndIndexPartB = 0xE2CF6B76; // MF_SDF_DATASLICE_END_INDEX_PARTB

    // C
    private const uint c_mfSdfDataSliceStartIndexPartCLocalizedAudio = 0xF8C55AE0; // MF_SDF_DATASLICE_START_INDEX_PARTC_LOCALIZED_AUDIO
    private const uint c_mfSdfDataSliceEndIndexPartCLocalizedAudio = 0x14C9D726; // MF_SDF_DATASLICE_END_INDEX_PARTC_LOCALIZED_AUDIO

    // D
    private const uint c_mfSdfDataSliceStartIndexDlc = 0xF319E8CE; // MF_SDF_DATASLICE_START_INDEX_DLC
    private const uint c_mfSdfDataSliceEndIndexDlc = 0xE01C7E59; // MF_SDF_DATASLICE_END_INDEX_DLC

    private const uint c_mfSdfDataSliceStartIndexPartAPatch = 0x1909240F; // MF_SDF_DATASLICE_START_INDEX_PARTA_PATCH
    private const uint c_mfSdfDataSliceEndIndexPartAPatch = 0x84ABBA21; // MF_SDF_DATASLICE_END_INDEX_PARTA_PATCH

    private const uint c_mfSdfDataSliceStartIndexPartBPatch = 0xD20B3ACA; // MF_SDF_DATASLICE_START_INDEX_PARTB_PATCH
    private const uint c_mfSdfDataSliceEndIndexPartBPatch = 0xD46E30CC; // MF_SDF_DATASLICE_END_INDEX_PARTB_PATCH

    private const uint c_mfSdfDataSliceStartIndexPartCPatch = 0xCEFF6597; // MF_SDF_DATASLICE_START_INDEX_PARTC_PATCH
    private const uint c_mfSdfDataSliceEndIndexPartCPatch = 0xE1379AE8; // MF_SDF_DATASLICE_END_INDEX_PARTC_PATCH

    private const uint c_mfSdfDataSliceStartIndexDlcPatch = 0x33C7C230; // MF_SDF_DATASLICE_START_INDEX_DLC_PATCH
    private const uint c_mfSdfDataSliceEndIndexDlcPatch = 0xB0BA2363; // MF_SDF_DATASLICE_END_INDEX_DLC_PATCH

    private const uint c_mfSdfDataSliceStartIndexPartEDownloadOnDemand = 2186534504; // MF_SDF_DATASLICE_START_INDEX_PARTE_DOWNLOADONDEMAND
    private const uint c_mfSdfDataSliceEndIndexPartEDownloadOnDemand = 2456653752; // MF_SDF_DATASLICE_END_INDEX_PARTE_DOWNLOADONDEMAND

    private const uint c_mfSdfDataSliceMaxIndex = 0x986CE4C8; // MF_SDF_DATASLICE_MAX_INDEX

    public int IndexRangeSizeForPartCLocalizedAudio = 0;
    public int IndexRangeSizeForDlc = 0;

    public int StartIndexResidentMCacheFiles = -1;
    public int EndIndexResidentMCacheFiles = -1;
    public int StartIndexResidentMCacheFilesSecondary = -1;
    public int EndIndexResidentMCacheFilesSecondary = -1;
    public int StartIndexAlwaysResident = -1;
    public int EndIndexAlwaysResident = -1;
    public int StartIndexFrontendResident = -1;
    public int EndIndexFrontendResident = -1;

    public int StartIndexUnkResident = -1;
    public int EndIndexUnkResident = -1;

    public int StartIndexResidentDuringLoadscreen = -1;
    public int EndIndexResidentDuringLoadscreen = -1;
    public int StartIndexPartA = 0;
    public int EndIndexPartA = 999;

    // B
    public int StartIndexPartB = 1000;
    public int EndIndexPartB = 1999;

    // C
    public int StartIndexPartCLocalizedAudio = -1;
    public int EndIndexPartCLocalizedAudio = -1;
    public int StartIndexPartC = 2000;
    public int EndIndexPartC = 2900;

    // D
    public int StartIndexDlc = 3000;
    public int EndIndexDlc = 3999;

    public int StartIndexPartAPatch = -1;
    public int EndIndexPartAPatch = -1;

    public int StartIndexPartBPatch = -1;
    public int EndIndexPartBPatch = -1;

    public int StartIndexPartCPatch = -1;
    public int EndIndexPartCPatch = -1;

    public int StartIndexDlcPatch = -1;
    public int EndIndexDlcPatch = -1;

    public int StartIndexPartEDownloadOnDemand = -1;
    public int EndIndexPartEDownloadOnDemand = -1;

    public int MaxIndex = 3999;

    public DataSliceIndexSettings()
    {
    }

    public void SetValue(uint inHash, int inValue)
    {
        switch (inHash)
        {
            case c_mfSdfIndexRangeSizeForPartCLocalizedAudio:
                IndexRangeSizeForPartCLocalizedAudio = inValue;
                break;
            case c_mfSdfIndexRangeSizeForDlc:
                IndexRangeSizeForDlc = inValue;
                break;
            case c_mfSdfDataSliceStartIndexResidentMCacheFiles:
                StartIndexResidentMCacheFiles = inValue;
                break;
            case c_mfSdfDataSliceEndIndexResidentMCacheFiles:
                EndIndexResidentMCacheFiles = inValue;
                break;
            case c_mfSdfDataSliceStartIndexResidentMCacheFilesSecondary:
                StartIndexResidentMCacheFilesSecondary = inValue;
                break;
            case c_mfSdfDataSliceEndIndexResidentMCacheFilesSecondary:
                EndIndexResidentMCacheFilesSecondary = inValue;
                break;
            case c_mfSdfDataSliceStartIndexAlwaysResident:
                StartIndexAlwaysResident = inValue;
                break;
            case c_mfSdfDataSliceEndIndexAlwaysResident:
                EndIndexAlwaysResident = inValue;
                break;
            case c_mfSdfDataSliceStartIndexFrontendResident:
                StartIndexFrontendResident = inValue;
                break;
            case c_mfSdfDataSliceEndIndexFrontendResident:
                EndIndexFrontendResident = inValue;
                break;
            case c_mfSdfDataSliceStartIndexUnkResident:
                StartIndexUnkResident = inValue;
                break;
            case c_mfSdfDataSliceEndIndexUnkResident:
                EndIndexUnkResident = inValue;
                break;
            case c_mfSdfDataSliceStartIndexResidentDuringLoadscreen:
                StartIndexResidentDuringLoadscreen = inValue;
                break;
            case c_mfSdfDataSliceEndIndexResidentDuringLoadscreen:
                EndIndexResidentDuringLoadscreen = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartA:
                StartIndexPartA = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartA:
                EndIndexPartA = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartB:
                StartIndexPartB = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartB:
                EndIndexPartB = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartCLocalizedAudio:
                StartIndexPartCLocalizedAudio = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartCLocalizedAudio:
                EndIndexPartCLocalizedAudio = inValue;
                break;
            case c_mfSdfDataSliceStartIndexDlc:
                StartIndexDlc = inValue;
                break;
            case c_mfSdfDataSliceEndIndexDlc:
                EndIndexDlc = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartAPatch:
                StartIndexPartAPatch = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartAPatch:
                EndIndexPartAPatch = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartBPatch:
                StartIndexPartBPatch = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartBPatch:
                EndIndexPartBPatch = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartCPatch:
                StartIndexPartCPatch = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartCPatch:
                EndIndexPartCPatch = inValue;
                break;
            case c_mfSdfDataSliceStartIndexDlcPatch:
                StartIndexDlcPatch = inValue;
                break;
            case c_mfSdfDataSliceEndIndexDlcPatch:
                EndIndexDlcPatch = inValue;
                break;
            case c_mfSdfDataSliceStartIndexPartEDownloadOnDemand:
                StartIndexPartEDownloadOnDemand = inValue;
                break;
            case c_mfSdfDataSliceEndIndexPartEDownloadOnDemand:
                EndIndexPartEDownloadOnDemand = inValue;
                break;
            case c_mfSdfDataSliceMaxIndex:
                MaxIndex = inValue;
                break;
            default:
                Console.WriteLine($"Unsupported hash: {inHash} with value: {inValue}");
                break;
        }
    }
}