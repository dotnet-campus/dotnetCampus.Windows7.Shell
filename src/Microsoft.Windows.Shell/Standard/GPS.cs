namespace Standard
{
    internal enum GPS
    {
        DEFAULT = 0,
        HANDLERPROPERTIESONLY = 1,
        READWRITE = 2,
        TEMPORARY = 4,
        FASTPROPERTIESONLY = 8,
        OPENSLOWITEM = 16, // 0x00000010
        DELAYCREATION = 32, // 0x00000020
        BESTEFFORT = 64, // 0x00000040
        NO_OPLOCK = 128, // 0x00000080
        MASK_VALID = 255, // 0x000000FF
    }
}
