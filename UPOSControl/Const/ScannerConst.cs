using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Const
{
    internal class ScannerConst
    {

        #region ScanDataType

        // One dimensional symbologies
        public static int SCAN_SDT_UPCA = 101;  // Digits
        public static int SCAN_SDT_UPCE = 102;  // Digits
        public static int SCAN_SDT_JAN8 = 103;  // = EAN 8
        public static int SCAN_SDT_EAN8 = 103;  // = JAN 8
        public static int SCAN_SDT_JAN13 = 104;  // = EAN 13
        public static int SCAN_SDT_EAN13 = 104;  // = JAN 13
        public static int SCAN_SDT_TF = 105;  // (Discrete 2 of 5)
                                                    //   Digits
        public static int SCAN_SDT_ITF = 106;  // (Interleaved 2 of 5)
                                                     //   Digits
        public static int SCAN_SDT_Codabar = 107;  // Digits, -, $, :, /, .,
                                                         //   +; 4 start/stop
                                                         //   characters (a, b, c,
                                                         //   d)
        public static int SCAN_SDT_Code39 = 108;  // Alpha, Digits, Space,
                                                        //   -, ., $, /, +, %;
                                                        //   start/stop (*)
                                                        // Also has Full Ascii
                                                        //   feature
        public static int SCAN_SDT_Code93 = 109;  // Same characters as
                                                        //   Code 39
        public static int SCAN_SDT_Code128 = 110;  // 128 data characters
        public static int SCAN_SDT_UPCA_S = 111;  // UPC-A with
                                                        //   supplemental barcode
        public static int SCAN_SDT_UPCE_S = 112;  // UPC-E with
                                                        //   supplemental barcode
        public static int SCAN_SDT_UPCD1 = 113;  // UPC-D1
        public static int SCAN_SDT_UPCD2 = 114;  // UPC-D2
        public static int SCAN_SDT_UPCD3 = 115;  // UPC-D3
        public static int SCAN_SDT_UPCD4 = 116;  // UPC-D4
        public static int SCAN_SDT_UPCD5 = 117;  // UPC-D5
        public static int SCAN_SDT_EAN8_S = 118;  // EAN 8 with
                                                        //   supplemental barcode
        public static int SCAN_SDT_EAN13_S = 119;  // EAN 13 with
                                                         //   supplemental barcode
        public static int SCAN_SDT_EAN128 = 120;  // EAN 128
        public static int SCAN_SDT_OCRA = 121;  // OCR "A"
        public static int SCAN_SDT_OCRB = 122;  // OCR "B"

        // One dimensional symbologies (Added in Release 1.8)
        //        The following RSS constants deprecated in 1.12.
        //        Instead use the GS1DATABAR constants below.
        public static int SCAN_SDT_RSS14 = 131;  // Reduced Space Symbology - 14 digit GTIN
        public static int SCAN_SDT_RSS_EXPANDED = 132;  // RSS - 14 digit GTIN plus additional fields

        // One dimensional symbologies (added in Release 1.12)
        public static int SCAN_SDT_GS1DATABAR = 131;  // GS1 DataBar Omnidirectional (normal or stacked)
        public static int SCAN_SDT_GS1DATABAR_E = 132;  // GS1 DataBar Expanded (normal or stacked)

        // One dimensional symbologies (added in Release 1.14)
        public static int SCAN_SDT_ITF_CK = 133;  // Interleaved 2 of 5 check digit verified and transmitted
        public static int SCAN_SDT_GS1DATABAR_TYPE2 = 134; // GS1 DataBar Limited
        public static int SCAN_SDT_AMES = 135;  // Ames Code
        public static int SCAN_SDT_TFMAT = 136;  // Matrix 2 of 5
        public static int SCAN_SDT_Code39_CK = 137;  // Code 39 with check character verified and transmitted
        public static int SCAN_SDT_Code32 = 138;  // Code 39 with Mod 32 check character
        public static int SCAN_SDT_CodeCIP = 139;  // Code 39 CIP
        public static int SCAN_SDT_TRIOPTIC39 = 140;  // Tri-Optic Code 39
        public static int SCAN_SDT_ISBT128 = 141;  // ISBT-128
        public static int SCAN_SDT_Code11 = 142;  // Code 11
        public static int SCAN_SDT_MSI = 143;  // MSI Code
        public static int SCAN_SDT_PLESSEY = 144;  // Plessey Code
        public static int SCAN_SDT_TELEPEN = 145;  // Telepen

        // Composite Symbologies (Added in Release 1.8)
        public static int SCAN_SDT_CCA = 151;  // Composite Component A.
        public static int SCAN_SDT_CCB = 152;  // Composite Component B.
        public static int SCAN_SDT_CCC = 153;  // Composite Component C.

        // Composite Symbologies (Added in Release 1.14)
        public static int SCAN_SDT_TLC39 = 154;  // TLC-39

        // Two dimensional symbologies
        public static int SCAN_SDT_PDF417 = 201;
        public static int SCAN_SDT_MAXICODE = 202;

        // Two dimensional symbologies (Added in Release 1.11)
        public static int SCAN_SDT_DATAMATRIX = 203;  // Data Matrix
        public static int SCAN_SDT_QRCODE = 204;  // QR Code
        public static int SCAN_SDT_UQRCODE = 205;  // Micro QR Code
        public static int SCAN_SDT_AZTEC = 206;  // Aztec
        public static int SCAN_SDT_UPDF417 = 207;  // Micro PDF 417

        // Two dimensional symbologies (Added in Release 1.14)
        public static int SCAN_SDT_GS1DATAMATRIX = 208;  // GS1 DataMatrix
        public static int SCAN_SDT_GS1QRCODE = 209;  // GS1 QR Code
        public static int SCAN_SDT_Code49 = 210;  // Code 49
        public static int SCAN_SDT_Code16k = 211;  // Code 16K
        public static int SCAN_SDT_CodablockA = 212;  // Codablock A
        public static int SCAN_SDT_CodablockF = 213;  // Codablock F
        public static int SCAN_SDT_Codablock256 = 214;  // Codablock 256
        public static int SCAN_SDT_HANXIN = 215;  // Han Xin Code

        // Postal Code Symbologies (Added in Release 1.14)
        public static int SCAN_SDT_AusPost = 301;  // Australian Post
        public static int SCAN_SDT_CanPost = 302;  // Canada Post
        public static int SCAN_SDT_ChinaPost = 303;  // China Post
        public static int SCAN_SDT_DutchKix = 304;  // Dutch Post
        public static int SCAN_SDT_InfoMail = 305;  // InfoMail
        public static int SCAN_SDT_JapanPost = 306;  // Japan Post
        public static int SCAN_SDT_KoreanPost = 307;  // Korean Post
        public static int SCAN_SDT_SwedenPost = 308;  // Sweden Post
        public static int SCAN_SDT_UkPost = 309;  // UK Post BPO
        public static int SCAN_SDT_UsIntelligent = 310;  // US Intelligent Mail
        public static int SCAN_SDT_UsPlanet = 311;  // US Planet Code
        public static int SCAN_SDT_PostNet = 312;  // US Postnet

        // Special cases
        public static int SCAN_SDT_OTHER = 501;  // Start of Scanner-
                                                       //   Specific bar code
                                                       //   symbologies
        public static int SCAN_SDT_UNKNOWN = 0;  // Cannot determine the

        #endregion

    }
}
