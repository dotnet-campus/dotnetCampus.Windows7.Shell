using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6332debf-87b5-4670-90c0-5e57b408a49e")]
    [ComImport]
    internal interface ICustomDestinationList
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr), In] string pszAppID);

        [return: MarshalAs(UnmanagedType.Interface)]
        object BeginList(out uint pcMaxSlots, [In] ref Guid riid);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        HRESULT AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, IObjectArray poa);

        void AppendKnownCategory(KDC category);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        HRESULT AddUserTasks(IObjectArray poa);

        void CommitList();

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetRemovedDestinations([In] ref Guid riid);

        void DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        void AbortList();
    }
}
