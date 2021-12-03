using System;
using System.Runtime.InteropServices;

namespace Standard
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    internal struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.U4)]
        public FO wFunc;
        public string pFrom;
        public string pTo;
        [MarshalAs(UnmanagedType.U2)]
        public FOF fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }
}
