using Microsoft.Win32.SafeHandles;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;

namespace Standard
{
    internal sealed class SafeGdiplusStartupToken : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeGdiplusStartupToken()
          : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle() => NativeMethods.GdiplusShutdown(this.handle) == Status.Ok;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public static SafeGdiplusStartupToken Startup()
        {
            SafeGdiplusStartupToken gdiplusStartupToken = new SafeGdiplusStartupToken();
            IntPtr token;
            if (NativeMethods.GdiplusStartup(out token, new StartupInput(), out StartupOutput _) == Status.Ok)
            {
                gdiplusStartupToken.handle = token;
                return gdiplusStartupToken;
            }
            gdiplusStartupToken.Dispose();
            throw new Exception("Unable to initialize GDI+");
        }
    }
}
