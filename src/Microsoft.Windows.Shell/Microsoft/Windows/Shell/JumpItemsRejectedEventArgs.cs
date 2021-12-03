using System;
using System.Collections.Generic;

namespace Microsoft.Windows.Shell
{
    public sealed class JumpItemsRejectedEventArgs : EventArgs
    {
        public JumpItemsRejectedEventArgs()
          : this((IList<JumpItem>)null, (IList<JumpItemRejectionReason>)null)
        {
        }

        public JumpItemsRejectedEventArgs(
          IList<JumpItem> rejectedItems,
          IList<JumpItemRejectionReason> reasons)
        {
            if (rejectedItems == null && reasons != null || reasons == null && rejectedItems != null || rejectedItems != null && reasons != null && rejectedItems.Count != reasons.Count)
                throw new ArgumentException("The counts of rejected items doesn't match the count of reasons.");
            if (rejectedItems != null)
            {
                this.RejectedItems = (IList<JumpItem>)new List<JumpItem>((IEnumerable<JumpItem>)rejectedItems).AsReadOnly();
                this.RejectionReasons = (IList<JumpItemRejectionReason>)new List<JumpItemRejectionReason>((IEnumerable<JumpItemRejectionReason>)reasons).AsReadOnly();
            }
            else
            {
                this.RejectedItems = (IList<JumpItem>)new List<JumpItem>().AsReadOnly();
                this.RejectionReasons = (IList<JumpItemRejectionReason>)new List<JumpItemRejectionReason>().AsReadOnly();
            }
        }

        public IList<JumpItem> RejectedItems { get; private set; }

        public IList<JumpItemRejectionReason> RejectionReasons { get; private set; }
    }
}
