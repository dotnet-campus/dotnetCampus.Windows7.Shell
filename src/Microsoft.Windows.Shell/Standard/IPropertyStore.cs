﻿using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [ComImport]
    internal interface IPropertyStore
    {
        uint GetCount();

        PKEY GetAt(uint iProp);

        void GetValue([In] ref PKEY pkey, [In, Out] PROPVARIANT pv);

        void SetValue([In] ref PKEY pkey, PROPVARIANT pv);

        void Commit();
    }
}
