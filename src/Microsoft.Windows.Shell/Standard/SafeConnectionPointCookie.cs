using Microsoft.Win32.SafeHandles;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.ComTypes;

namespace Standard
{
    internal sealed class SafeConnectionPointCookie : SafeHandleZeroOrMinusOneIsInvalid
    {
        private IConnectionPoint _cp;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "IConnectionPoint")]
        public SafeConnectionPointCookie(IConnectionPointContainer target, object sink, Guid eventId)
          : base(true)
        {
            Verify.IsNotNull<IConnectionPointContainer>(target, nameof(target));
            Verify.IsNotNull<object>(sink, nameof(sink));
            Verify.IsNotDefault<Guid>(eventId, nameof(eventId));
            this.handle = IntPtr.Zero;
            IConnectionPoint ppCP = (IConnectionPoint)null;
            try
            {
                target.FindConnectionPoint(ref eventId, out ppCP);
                int pdwCookie;
                ppCP.Advise(sink, out pdwCookie);
                this.handle = pdwCookie != 0 ? new IntPtr(pdwCookie) : throw new InvalidOperationException("IConnectionPoint::Advise returned an invalid cookie.");
                this._cp = ppCP;
                ppCP = (IConnectionPoint)null;
            }
            finally
            {
                Utility.SafeRelease<IConnectionPoint>(ref ppCP);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Disconnect() => this.ReleaseHandle();

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            try
            {
                if (!this.IsInvalid)
                {
                    int int32 = this.handle.ToInt32();
                    this.handle = IntPtr.Zero;
                    Assert.IsNotNull<IConnectionPoint>(this._cp);
                    try
                    {
                        this._cp.Unadvise(int32);
                    }
                    finally
                    {
                        Utility.SafeRelease<IConnectionPoint>(ref this._cp);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
