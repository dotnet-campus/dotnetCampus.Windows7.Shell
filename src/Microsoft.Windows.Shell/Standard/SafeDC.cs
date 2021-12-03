using Microsoft.Win32.SafeHandles;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Standard
{
    internal sealed class SafeDC : SafeHandleZeroOrMinusOneIsInvalid
    {
        private IntPtr? _hwnd;
        private bool _created;

        public IntPtr Hwnd
        {
            set
            {
                Assert.NullableIsNull<IntPtr>(this._hwnd);
                this._hwnd = new IntPtr?(value);
            }
        }

        private SafeDC()
          : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (this._created)
                return SafeDC.NativeMethods.DeleteDC(this.handle);
            return !this._hwnd.HasValue || this._hwnd.Value == IntPtr.Zero || SafeDC.NativeMethods.ReleaseDC(this._hwnd.Value, this.handle) == 1;
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC CreateDC(string deviceName)
        {
            SafeDC safeDc = (SafeDC)null;
            try
            {
                safeDc = SafeDC.NativeMethods.CreateDC(deviceName, (string)null, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                if (safeDc != null)
                    safeDc._created = true;
            }
            if (safeDc.IsInvalid)
            {
                safeDc.Dispose();
                throw new SystemException("Unable to create a device context from the specified device information.");
            }
            return safeDc;
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC CreateCompatibleDC(SafeDC hdc)
        {
            SafeDC safeDc = (SafeDC)null;
            try
            {
                IntPtr hdc1 = IntPtr.Zero;
                if (hdc != null)
                    hdc1 = hdc.handle;
                safeDc = SafeDC.NativeMethods.CreateCompatibleDC(hdc1);
                if (safeDc == null)
                    HRESULT.ThrowLastError();
            }
            finally
            {
                if (safeDc != null)
                    safeDc._created = true;
            }
            if (safeDc.IsInvalid)
            {
                safeDc.Dispose();
                throw new SystemException("Unable to create a device context from the specified device information.");
            }
            return safeDc;
        }

        public static SafeDC GetDC(IntPtr hwnd)
        {
            SafeDC safeDc = (SafeDC)null;
            try
            {
                safeDc = SafeDC.NativeMethods.GetDC(hwnd);
            }
            finally
            {
                if (safeDc != null)
                    safeDc.Hwnd = hwnd;
            }
            if (safeDc.IsInvalid)
                HRESULT.E_FAIL.ThrowIfFailed();
            return safeDc;
        }

        public static SafeDC GetDesktop() => SafeDC.GetDC(IntPtr.Zero);

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC WrapDC(IntPtr hdc)
        {
            SafeDC safeDc = new SafeDC();
            safeDc.handle = hdc;
            safeDc._created = false;
            safeDc._hwnd = new IntPtr?(IntPtr.Zero);
            return safeDc;
        }

        private static class NativeMethods
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("user32.dll")]
            public static extern SafeDC GetDC(IntPtr hwnd);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
            public static extern SafeDC CreateDC(
              [MarshalAs(UnmanagedType.LPWStr)] string lpszDriver,
              [MarshalAs(UnmanagedType.LPWStr)] string lpszDevice,
              IntPtr lpszOutput,
              IntPtr lpInitData);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeDC CreateCompatibleDC(IntPtr hdc);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteDC(IntPtr hdc);
        }
    }
}
