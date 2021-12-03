namespace Standard
{
    internal enum FOF : ushort
    {
        MULTIDESTFILES = 1,
        CONFIRMMOUSE = 2,
        SILENT = 4,
        RENAMEONCOLLISION = 8,
        NOCONFIRMATION = 16, // 0x0010
        WANTMAPPINGHANDLE = 32, // 0x0020
        ALLOWUNDO = 64, // 0x0040
        FILESONLY = 128, // 0x0080
        SIMPLEPROGRESS = 256, // 0x0100
        NOCONFIRMMKDIR = 512, // 0x0200
        NOERRORUI = 1024, // 0x0400
        NOCOPYSECURITYATTRIBS = 2048, // 0x0800
        NORECURSION = 4096, // 0x1000
        NO_CONNECTED_ELEMENTS = 8192, // 0x2000
        WANTNUKEWARNING = 16384, // 0x4000
        NORECURSEREPARSE = 32768, // 0x8000
    }
}
