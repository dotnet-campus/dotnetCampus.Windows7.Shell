using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;

namespace Standard
{
    internal static class DpiHelper
    {
        private static Matrix _transformToDevice;
        private static Matrix _transformToDip;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static DpiHelper()
        {
            using (SafeDC desktop = SafeDC.GetDesktop())
            {
                int deviceCaps1 = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
                int deviceCaps2 = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);
                DpiHelper._transformToDip = Matrix.Identity;
                DpiHelper._transformToDip.Scale(96.0 / (double)deviceCaps1, 96.0 / (double)deviceCaps2);
                DpiHelper._transformToDevice = Matrix.Identity;
                DpiHelper._transformToDevice.Scale((double)deviceCaps1 / 96.0, (double)deviceCaps2 / 96.0);
            }
        }

        public static Point LogicalPixelsToDevice(Point logicalPoint) => DpiHelper._transformToDevice.Transform(logicalPoint);

        public static Point DevicePixelsToLogical(Point devicePoint) => DpiHelper._transformToDip.Transform(devicePoint);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static Rect LogicalRectToDevice(Rect logicalRectangle) => new Rect(DpiHelper.LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top)), DpiHelper.LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom)));

        public static Rect DeviceRectToLogical(Rect deviceRectangle) => new Rect(DpiHelper.DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top)), DpiHelper.DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom)));

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static Size LogicalSizeToDevice(Size logicalSize)
        {
            Point device = DpiHelper.LogicalPixelsToDevice(new Point(logicalSize.Width, logicalSize.Height));
            return new Size()
            {
                Width = device.X,
                Height = device.Y
            };
        }

        public static Size DeviceSizeToLogical(Size deviceSize)
        {
            Point logical = DpiHelper.DevicePixelsToLogical(new Point(deviceSize.Width, deviceSize.Height));
            return new Size(logical.X, logical.Y);
        }

        public static Thickness LogicalThicknessToDevice(Thickness logicalThickness)
        {
            Point device1 = DpiHelper.LogicalPixelsToDevice(new Point(logicalThickness.Left, logicalThickness.Top));
            Point device2 = DpiHelper.LogicalPixelsToDevice(new Point(logicalThickness.Right, logicalThickness.Bottom));
            return new Thickness(device1.X, device1.Y, device2.X, device2.Y);
        }
    }
}
