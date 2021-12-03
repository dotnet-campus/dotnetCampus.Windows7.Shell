using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Standard
{
    internal struct NONCLIENTMETRICS
    {
        public int cbSize;
        public int iBorderWidth;
        public int iScrollWidth;
        public int iScrollHeight;
        public int iCaptionWidth;
        public int iCaptionHeight;
        public LOGFONT lfCaptionFont;
        public int iSmCaptionWidth;
        public int iSmCaptionHeight;
        public LOGFONT lfSmCaptionFont;
        public int iMenuWidth;
        public int iMenuHeight;
        public LOGFONT lfMenuFont;
        public LOGFONT lfStatusFont;
        public LOGFONT lfMessageFont;
        public int iPaddedBorderWidth;

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static NONCLIENTMETRICS VistaMetricsStruct => new NONCLIENTMETRICS()
        {
            cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS))
        };

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static NONCLIENTMETRICS XPMetricsStruct => new NONCLIENTMETRICS()
        {
            cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS)) - 4
        };
    }
}
