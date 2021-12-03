using System;

namespace Standard
{
    [Flags]
    internal enum SPIF
    {
        None = 0,
        UPDATEINIFILE = 1,
        SENDCHANGE = 2,
        SENDWININICHANGE = SENDCHANGE, // 0x00000002
    }
}
