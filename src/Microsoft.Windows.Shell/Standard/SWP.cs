using System;

namespace Standard
{
    [Flags]
    internal enum SWP
    {
        ASYNCWINDOWPOS = 16384, // 0x00004000
        DEFERERASE = 8192, // 0x00002000
        DRAWFRAME = 32, // 0x00000020
        FRAMECHANGED = DRAWFRAME, // 0x00000020
        HIDEWINDOW = 128, // 0x00000080
        NOACTIVATE = 16, // 0x00000010
        NOCOPYBITS = 256, // 0x00000100
        NOMOVE = 2,
        NOOWNERZORDER = 512, // 0x00000200
        NOREDRAW = 8,
        NOREPOSITION = NOOWNERZORDER, // 0x00000200
        NOSENDCHANGING = 1024, // 0x00000400
        NOSIZE = 1,
        NOZORDER = 4,
        SHOWWINDOW = 64, // 0x00000040
    }
}
