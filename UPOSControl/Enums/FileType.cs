using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UPOSControl.Enums
{
    /// <summary>
    /// Типы файлов
    /// </summary>
    public enum FileType
    {
        audio,
        video,
        image,
        document,
        directory,
        UNKNOWN = -1
    }

    public enum VidType
    {
        MP4, M4A, M4V, F4V, F4A, M4B, M4R, F4B, MOV,
        _3GP, _3GP2, _3G2, _3GPP, _3GPP2,
        OGG, OGA, OGV, OGX,
        WMV, WMA, ASF,
        WEBM,
        FLV,
        AVI,
        QUICKTIME,
        HDV,
        OP1A, OP_ATOM,
        TS,
        PS,
        WAV,
        LXF, GXF,
        VOB,
        UNKNOWN = -1
    }

    public enum AudioType
    {
        WAV, FLAC, AIFF, MP3, AAC, ALAC, WavPack, DSD, WMA,
        UNKNOWN = -1
    }


    public enum DocType
    {
        DOC,
        DOCX,
        XLS,
        XLSX,
        PPT,
        PPTX,
        PDF,
        UNKNOWN = -1
    }

    public enum ImgType
    {
        JPEG,
        JPG,
        BMP,
        PNG,
        TIF,
        TIFF,
        GIF,
        UNKNOWN = -1
    }
}
