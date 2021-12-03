using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Standard
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Win32Error
    {
        [FieldOffset(0)]
        private readonly int _value;
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_SUCCESS = new Win32Error(0);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_INVALID_FUNCTION = new Win32Error(1);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_FILE_NOT_FOUND = new Win32Error(2);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_PATH_NOT_FOUND = new Win32Error(3);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_TOO_MANY_OPEN_FILES = new Win32Error(4);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_ACCESS_DENIED = new Win32Error(5);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_INVALID_HANDLE = new Win32Error(6);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_OUTOFMEMORY = new Win32Error(14);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_NO_MORE_FILES = new Win32Error(18);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_SHARING_VIOLATION = new Win32Error(32);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_INVALID_PARAMETER = new Win32Error(87);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_INSUFFICIENT_BUFFER = new Win32Error(122);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_NESTING_NOT_ALLOWED = new Win32Error(215);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_KEY_DELETED = new Win32Error(1018);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_NOT_FOUND = new Win32Error(1168);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_NO_MATCH = new Win32Error(1169);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_BAD_DEVICE = new Win32Error(1200);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_CANCELLED = new Win32Error(1223);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_CLASS_ALREADY_EXISTS = new Win32Error(1410);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Win32Error ERROR_INVALID_DATATYPE = new Win32Error(1804);

        public Win32Error(int i) => this._value = i;

        public static explicit operator HRESULT(Win32Error error) => error._value <= 0 ? new HRESULT((uint)error._value) : HRESULT.Make(true, Facility.Win32, error._value & (int)ushort.MaxValue);

        public HRESULT ToHRESULT() => (HRESULT)this;

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static Win32Error GetLastError() => new Win32Error(Marshal.GetLastWin32Error());

        public override bool Equals(object obj)
        {
            try
            {
                return ((Win32Error)obj)._value == this._value;
            }
            catch (InvalidCastException ex)
            {
                return false;
            }
        }

        public override int GetHashCode() => this._value.GetHashCode();

        public static bool operator ==(Win32Error errLeft, Win32Error errRight) => errLeft._value == errRight._value;

        public static bool operator !=(Win32Error errLeft, Win32Error errRight) => !(errLeft == errRight);
    }
}
