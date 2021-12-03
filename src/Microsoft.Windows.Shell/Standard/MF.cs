using System;

namespace Standard
{
    [Flags]
    internal enum MF : uint
    {
        DOES_NOT_EXIST = 4294967295, // 0xFFFFFFFF
        ENABLED = 0,
        BYCOMMAND = 0,
        GRAYED = 1,
        DISABLED = 2,
    }
}
