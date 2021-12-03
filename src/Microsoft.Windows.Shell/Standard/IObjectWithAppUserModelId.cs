using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("36db0196-9665-46d1-9ba7-d3709eecf9ed")]
    [ComImport]
    internal interface IObjectWithAppUserModelId
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetAppID();
    }
}
