using System;

namespace Standard
{
    [Flags]
    internal enum SHGDN
    {
        SHGDN_NORMAL = 0,
        SHGDN_INFOLDER = 1,
        SHGDN_FOREDITING = 4096, // 0x00001000
        SHGDN_FORADDRESSBAR = 16384, // 0x00004000
        SHGDN_FORPARSING = 32768, // 0x00008000
    }
}
