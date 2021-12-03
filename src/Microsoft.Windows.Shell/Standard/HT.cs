namespace Standard
{
    internal enum HT
    {
        ERROR = -2, // 0xFFFFFFFE
        TRANSPARENT = -1, // 0xFFFFFFFF
        NOWHERE = 0,
        CLIENT = 1,
        CAPTION = 2,
        SYSMENU = 3,
        GROWBOX = 4,
        SIZE = 4,
        MENU = 5,
        HSCROLL = 6,
        VSCROLL = 7,
        MINBUTTON = 8,
        REDUCE = 8,
        MAXBUTTON = 9,
        ZOOM = 9,
        LEFT = 10, // 0x0000000A
        SIZEFIRST = 10, // 0x0000000A
        RIGHT = 11, // 0x0000000B
        TOP = 12, // 0x0000000C
        TOPLEFT = 13, // 0x0000000D
        TOPRIGHT = 14, // 0x0000000E
        BOTTOM = 15, // 0x0000000F
        BOTTOMLEFT = 16, // 0x00000010
        BOTTOMRIGHT = 17, // 0x00000011
        SIZELAST = 17, // 0x00000011
        BORDER = 18, // 0x00000012
        OBJECT = 19, // 0x00000013
        CLOSE = 20, // 0x00000014
        HELP = 21, // 0x00000015
    }
}
