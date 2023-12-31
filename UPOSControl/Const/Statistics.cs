﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Const
{
    internal class Statistics
    {
        public static string STAT_HoursPoweredCount{ get; } = "HoursPoweredCount";
        public static string STAT_CommunicationErrorCount{ get; } = "CommunicationErrorCount";
        public static string STAT_SuccessfulMatchCount{ get; } = "SuccessfulMatchCount";
        public static string STAT_UnsuccessfulMatchCount{ get; } = "UnsuccessfulMatchCount";
        public static string STAT_AverageFAR{ get; } = "AverageFAR";
        public static string STAT_AverageFRR{ get; } = "AverageFRR";
        public static string STAT_BumpCount{ get; } = "BumpCount";
        public static string STAT_DrawerGoodOpenCount{ get; } = "DrawerGoodOpenCount";
        public static string STAT_DrawerFailedOpenCount{ get; } = "DrawerFailedOpenCount";
        public static string STAT_ChecksScannedCount{ get; } = "ChecksScannedCount";
        public static string STAT_WriteCount{ get; } = "WriteCount";
        public static string STAT_FailedWriteCount{ get; } = "FailedWriteCount";
        public static string STAT_EraseCount{ get; } = "EraseCount";
        public static string STAT_MediumRemovedCount{ get; } = "MediumRemovedCount";
        public static string STAT_MediumSize{ get; } = "MediumSize";
        public static string STAT_MediumFreeSpace{ get; } = "MediumFreeSpace";
        public static string STAT_BarcodePrintedCount{ get; } = "BarcodePrintedCount";
        public static string STAT_FormInsertionCount{ get; } = "FormInsertionCount";
        public static string STAT_HomeErrorCount{ get; } = "HomeErrorCount";
        public static string STAT_JournalCharacterPrintedCount{ get; } = "JournalCharacterPrintedCount";
        public static string STAT_JournalLinePrintedCount{ get; } = "JournalLinePrintedCount";
        public static string STAT_MaximumTempReachedCount{ get; } = "MaximumTempReachedCount";
        public static string STAT_NVRAMWriteCount{ get; } = "NVRAMWriteCount";
        public static string STAT_PaperCutCount{ get; } = "PaperCutCount";
        public static string STAT_FailedPaperCutCount{ get; } = "FailedPaperCutCount";
        public static string STAT_PrinterFaultCount{ get; } = "PrinterFaultCount";
        public static string STAT_PrintSideChangeCount{ get; } = "PrintSideChangeCount";
        public static string STAT_FailedPrintSideChangeCount{ get; } = "FailedPrintSideChangeCount";
        public static string STAT_ReceiptCharacterPrintedCount{ get; } = "ReceiptCharacterPrintedCount";
        public static string STAT_ReceiptCoverOpenCount{ get; } = "ReceiptCoverOpenCount";
        public static string STAT_ReceiptLineFeedCount{ get; } = "ReceiptLineFeedCount";
        public static string STAT_ReceiptLinePrintedCount{ get; } = "ReceiptLinePrintedCount";
        public static string STAT_SlipCharacterPrintedCount{ get; } = "SlipCharacterPrintedCount";
        public static string STAT_SlipCoverOpenCount{ get; } = "SlipCoverOpenCount";
        public static string STAT_SlipLineFeedCount{ get; } = "SlipLineFeedCount";
        public static string STAT_SlipLinePrintedCount{ get; } = "SlipLinePrintedCount";
        public static string STAT_StampFiredCount{ get; } = "StampFiredCount";
        public static string STAT_GoodReadCount{ get; } = "GoodReadCount";
        public static string STAT_NoReadCount{ get; } = "NoReadCount";
        public static string STAT_SessionCount{ get; } = "SessionCount";
        public static string STAT_LockPositionChangeCount{ get; } = "LockPositionChangeCount";
        public static string STAT_OnlineTransitionCount{ get; } = "OnlineTransitionCount";
        public static string STAT_FailedDataParseCount{ get; } = "FailedDataParseCount";
        public static string STAT_UnreadableCardCount{ get; } = "UnreadableCardCount";
        public static string STAT_GoodWriteCount{ get; } = "GoodWriteCount";
        public static string STAT_MissingStartSentinelTrack1Count{ get; } = "MissingStartSentinelTrack1Count";
        public static string STAT_ParityLRCErrorTrack1Count{ get; } = "ParityLRCErrorTrack1Count";
        public static string STAT_MissingStartSentinelTrack2Count{ get; } = "MissingStartSentinelTrack2Count";
        public static string STAT_ParityLRCErrorTrack2Count{ get; } = "ParityLRCErrorTrack2Count";
        public static string STAT_MissingStartSentinelTrack3Count{ get; } = "MissingStartSentinelTrack3Count";
        public static string STAT_ParityLRCErrorTrack3Count{ get; } = "ParityLRCErrorTrack3Count";
        public static string STAT_MissingStartSentinelTrack4Count{ get; } = "MissingStartSentinelTrack4Count";
        public static string STAT_ParityLRCErrorTrack4Count{ get; } = "ParityLRCErrorTrack4Count";
        public static string STAT_GoodCardAuthenticationDataCount{ get; } = "GoodCardAuthenticationDataCount";
        public static string STAT_FailedCardAuthenticationDataCount{ get; } = "FailedCardAuthenticationDataCount";
        public static string STAT_ChallengeRequestCount{ get; } = "ChallengeRequestCount";
        public static string STAT_GoodDeviceAuthenticationCount{ get; } = "GoodDeviceAuthenticationCount";
        public static string STAT_FailedDeviceAuthenticationCount{ get; } = "FailedDeviceAuthenticationCount";
        public static string STAT_FailedReadCount{ get; } = "FailedReadCount";
        public static string STAT_MotionEventCount{ get; } = "MotionEventCount";
        public static string STAT_ValidPINEntryCount{ get; } = "ValidPINEntryCount";
        public static string STAT_InvalidPINEntryCount{ get; } = "InvalidPINEntryCount";
        public static string STAT_KeyPressedCount{ get; } = "KeyPressedCount";
        public static string STAT_TagReadCount{ get; } = "TagReadCount";
        public static string STAT_GoodTagWriteCount{ get; } = "GoodTagWriteCount";
        public static string STAT_FailedTagWriteCount{ get; } = "FailedTagWriteCount";
        public static string STAT_GoodTagLockCount{ get; } = "GoodTagLockCount";
        public static string STAT_FailedTagLockCount{ get; } = "FailedTagLockCount";
        public static string STAT_GoodTagDisableCount{ get; } = "GoodTagDisableCount";
        public static string STAT_FailedTagDisableCount{ get; } = "FailedTagDisableCount";
        public static string STAT_GoodWeightReadCount{ get; } = "GoodWeightReadCount";
        public static string STAT_GoodScanCount{ get; } = "GoodScanCount";
        public static string STAT_GoodSignatureReadCount{ get; } = "GoodSignatureReadCount";
        public static string STAT_FailedSignatureReadCount{ get; } = "FailedSignatureReadCount";
        public static string STAT_ToneSoundedCount{ get; } = "ToneSoundedCount";
    }
}
