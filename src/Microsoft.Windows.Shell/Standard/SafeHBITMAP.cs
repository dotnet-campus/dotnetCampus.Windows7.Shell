using Microsoft.Win32.SafeHandles;

using System.Runtime.ConstrainedExecution;

namespace Standard
{
    internal sealed class SafeHBITMAP : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeHBITMAP()
          : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle() => NativeMethods.DeleteObject(this.handle);
    }
}
