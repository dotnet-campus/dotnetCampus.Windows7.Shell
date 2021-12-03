﻿using System;

namespace Standard
{
    [Flags]
    internal enum STATE_SYSTEM
    {
        UNAVAILABLE = 1,
        SELECTED = 2,
        FOCUSED = 4,
        PRESSED = 8,
        CHECKED = 16, // 0x00000010
        MIXED = 32, // 0x00000020
        INDETERMINATE = MIXED, // 0x00000020
        READONLY = 64, // 0x00000040
        HOTTRACKED = 128, // 0x00000080
        DEFAULT = 256, // 0x00000100
        EXPANDED = 512, // 0x00000200
        COLLAPSED = 1024, // 0x00000400
        BUSY = 2048, // 0x00000800
        FLOATING = 4096, // 0x00001000
        MARQUEED = 8192, // 0x00002000
        ANIMATED = 16384, // 0x00004000
        INVISIBLE = 32768, // 0x00008000
        OFFSCREEN = 65536, // 0x00010000
        SIZEABLE = 131072, // 0x00020000
        MOVEABLE = 262144, // 0x00040000
        SELFVOICING = 524288, // 0x00080000
        FOCUSABLE = 1048576, // 0x00100000
        SELECTABLE = 2097152, // 0x00200000
        LINKED = 4194304, // 0x00400000
        TRAVERSED = 8388608, // 0x00800000
        MULTISELECTABLE = 16777216, // 0x01000000
        EXTSELECTABLE = 33554432, // 0x02000000
        ALERT_LOW = 67108864, // 0x04000000
        ALERT_MEDIUM = 134217728, // 0x08000000
        ALERT_HIGH = 268435456, // 0x10000000
        PROTECTED = 536870912, // 0x20000000
        VALID = PROTECTED | ALERT_HIGH | ALERT_MEDIUM | ALERT_LOW | EXTSELECTABLE | MULTISELECTABLE | TRAVERSED | LINKED | SELECTABLE | FOCUSABLE | SELFVOICING | MOVEABLE | SIZEABLE | OFFSCREEN | INVISIBLE | ANIMATED | MARQUEED | FLOATING | BUSY | COLLAPSED | EXPANDED | DEFAULT | HOTTRACKED | READONLY | INDETERMINATE | CHECKED | PRESSED | FOCUSED | SELECTED | UNAVAILABLE, // 0x3FFFFFFF
    }
}