using System;

namespace Standard
{
    [Flags]
    internal enum HCF
    {
        HIGHCONTRASTON = 1,
        AVAILABLE = 2,
        HOTKEYACTIVE = 4,
        CONFIRMHOTKEY = 8,
        HOTKEYSOUND = 16, // 0x00000010
        INDICATOR = 32, // 0x00000020
        HOTKEYAVAILABLE = 64, // 0x00000040
    }
}
