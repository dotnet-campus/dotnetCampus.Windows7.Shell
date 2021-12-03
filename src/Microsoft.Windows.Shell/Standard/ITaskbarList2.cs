using System;
using System.Runtime.InteropServices;

namespace Standard
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("602D4995-B13A-429b-A66E-1935E44F4317")]
    [ComImport]
    internal interface ITaskbarList2 : ITaskbarList
    {
        new void HrInit();

        new void AddTab(IntPtr hwnd);

        new void DeleteTab(IntPtr hwnd);

        new void ActivateTab(IntPtr hwnd);

        new void SetActiveAlt(IntPtr hwnd);

        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
    }
}
