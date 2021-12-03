using System.Windows;

namespace Microsoft.Windows.Shell
{
    public class ThumbButtonInfoCollection : FreezableCollection<ThumbButtonInfo>
    {
        private static ThumbButtonInfoCollection s_empty;

        protected override Freezable CreateInstanceCore() => (Freezable)new ThumbButtonInfoCollection();

        internal static ThumbButtonInfoCollection Empty
        {
            get
            {
                if (ThumbButtonInfoCollection.s_empty == null)
                {
                    ThumbButtonInfoCollection buttonInfoCollection = new ThumbButtonInfoCollection();
                    buttonInfoCollection.Freeze();
                    ThumbButtonInfoCollection.s_empty = buttonInfoCollection;
                }
                return ThumbButtonInfoCollection.s_empty;
            }
        }
    }
}
