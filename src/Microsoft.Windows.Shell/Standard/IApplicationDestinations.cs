using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("12337d35-94c6-48a0-bce7-6a9c69d4d600")]
    [ComImport]
    internal interface IApplicationDestinations
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr), In] string pszAppID);

        void RemoveDestination([MarshalAs(UnmanagedType.IUnknown)] object punk);

        void RemoveAllDestinations();
    }
}
