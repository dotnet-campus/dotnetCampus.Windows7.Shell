using Microsoft.Win32.SafeHandles;

using System.Security.Permissions;

namespace Standard
{
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        private SafeFindHandle()
          : base(true)
        {
        }

        protected override bool ReleaseHandle() => NativeMethods.FindClose(this.handle);
    }
}
