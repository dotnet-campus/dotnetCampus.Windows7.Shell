using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F2-0000-0000-C000-000000000046")]
    [ComImport]
    internal interface IEnumIDList
    {
        [MethodImpl(MethodImplOptions.PreserveSig)]
        HRESULT Next(uint celt, out IntPtr rgelt, out int pceltFetched);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        HRESULT Skip(uint celt);

        void Reset();

        void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenum);
    }
}
