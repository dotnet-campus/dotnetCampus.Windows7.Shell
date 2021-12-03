using System;

namespace Standard
{
    [Flags]
    internal enum NIF : uint
    {
        MESSAGE = 1,
        ICON = 2,
        TIP = 4,
        STATE = 8,
        INFO = 16, // 0x00000010
        GUID = 32, // 0x00000020
        REALTIME = 64, // 0x00000040
        SHOWTIP = 128, // 0x00000080
        XP_MASK = GUID | INFO | STATE | ICON | MESSAGE, // 0x0000003B
        VISTA_MASK = XP_MASK | SHOWTIP | REALTIME, // 0x000000FB
    }
}
