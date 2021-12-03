using System;
using System.Collections.Generic;

namespace Microsoft.Windows.Shell
{
    public sealed class JumpItemsRemovedEventArgs : EventArgs
    {
        public JumpItemsRemovedEventArgs()
          : this((IList<JumpItem>)null)
        {
        }

        public JumpItemsRemovedEventArgs(IList<JumpItem> removedItems)
        {
            if (removedItems != null)
                this.RemovedItems = (IList<JumpItem>)new List<JumpItem>((IEnumerable<JumpItem>)removedItems).AsReadOnly();
            else
                this.RemovedItems = (IList<JumpItem>)new List<JumpItem>().AsReadOnly();
        }

        public IList<JumpItem> RemovedItems { get; private set; }
    }
}
