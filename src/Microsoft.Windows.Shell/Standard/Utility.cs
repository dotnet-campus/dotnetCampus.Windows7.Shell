using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Standard
{
    internal static class Utility
    {
        private static readonly Version _osVersion = Environment.OSVersion.Version;
        private static readonly Version _presentationFrameworkVersion = Assembly.GetAssembly(typeof(Window)).GetName().Version;
        private static int s_bitDepth;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool _MemCmp(IntPtr left, IntPtr right, long cb)
        {
            int ofs;
            for (ofs = 0; (long)ofs < cb - 8L; ofs += 8)
            {
                if (Marshal.ReadInt64(left, ofs) != Marshal.ReadInt64(right, ofs))
                    return false;
            }
            for (; (long)ofs < cb; ++ofs)
            {
                if ((int)Marshal.ReadByte(left, ofs) != (int)Marshal.ReadByte(right, ofs))
                    return false;
            }
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int RGB(Color c) => (int)c.R | (int)c.G << 8 | (int)c.B << 16;

        public static Color ColorFromArgbDword(uint color) => Color.FromArgb((byte)((color & 4278190080U) >> 24), (byte)((color & 16711680U) >> 16), (byte)((color & 65280U) >> 8), (byte)(color & (uint)byte.MaxValue));

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int GET_X_LPARAM(IntPtr lParam) => Utility.LOWORD(lParam.ToInt32());

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int GET_Y_LPARAM(IntPtr lParam) => Utility.HIWORD(lParam.ToInt32());

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int HIWORD(int i) => (int)(short)(i >> 16);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int LOWORD(int i) => (int)(short)(i & (int)ushort.MaxValue);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static bool AreStreamsEqual(Stream left, Stream right)
        {
            if (left == null)
                return right == null;
            if (right == null)
                return false;
            if (!left.CanRead || !right.CanRead)
                throw new NotSupportedException("The streams can't be read for comparison");
            if (left.Length != right.Length)
                return false;
            int length = (int)left.Length;
            left.Position = 0L;
            right.Position = 0L;
            int num = 0;
            int actual1 = 0;
            int expected = 0;
            int actual2 = 0;
            byte[] buffer1 = new byte[512];
            byte[] buffer2 = new byte[512];
            GCHandle gcHandle1 = GCHandle.Alloc((object)buffer1, GCHandleType.Pinned);
            IntPtr left1 = gcHandle1.AddrOfPinnedObject();
            GCHandle gcHandle2 = GCHandle.Alloc((object)buffer2, GCHandleType.Pinned);
            IntPtr right1 = gcHandle2.AddrOfPinnedObject();
            try
            {
                while (num < length)
                {
                    Assert.AreEqual<int>(num, actual1);
                    expected = left.Read(buffer1, 0, buffer1.Length);
                    actual2 = right.Read(buffer2, 0, buffer2.Length);
                    if (expected != actual2 || !Utility._MemCmp(left1, right1, (long)expected))
                        return false;
                    num += expected;
                    actual1 += actual2;
                }
                Assert.AreEqual<int>(expected, actual2);
                Assert.AreEqual<int>(num, actual1);
                Assert.AreEqual<int>(length, num);
                return true;
            }
            finally
            {
                gcHandle1.Free();
                gcHandle2.Free();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool GuidTryParse(string guidString, out Guid guid)
        {
            Verify.IsNeitherNullNorEmpty(guidString, nameof(guidString));
            try
            {
                guid = new Guid(guidString);
                return true;
            }
            catch (FormatException ex)
            {
            }
            catch (OverflowException ex)
            {
            }
            guid = new Guid();
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsFlagSet(int value, int mask) => (uint)(value & mask) > 0U;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsFlagSet(uint value, uint mask) => (value & mask) > 0U;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsFlagSet(long value, long mask) => (ulong)(value & mask) > 0UL;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsFlagSet(ulong value, ulong mask) => (value & mask) > 0UL;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsOSVistaOrNewer => Utility._osVersion >= new Version(6, 0);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsOSWindows7OrNewer => Utility._osVersion >= new Version(6, 1);

        public static bool IsPresentationFrameworkVersionLessThan4 => Utility._presentationFrameworkVersion < new Version(4, 0);

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static IntPtr GenerateHICON(ImageSource image, Size dimensions)
        {
            if (image == null)
                return IntPtr.Zero;
            BitmapFrame bestMatch;
            if (image is BitmapFrame bitmapFrame)
            {
                bestMatch = Utility.GetBestMatch((IList<BitmapFrame>)bitmapFrame.Decoder.Frames, (int)dimensions.Width, (int)dimensions.Height);
            }
            else
            {
                Rect rectangle = new Rect(0.0, 0.0, dimensions.Width, dimensions.Height);
                double num1 = dimensions.Width / dimensions.Height;
                double num2 = image.Width / image.Height;
                if (image.Width <= dimensions.Width && image.Height <= dimensions.Height)
                    rectangle = new Rect((dimensions.Width - image.Width) / 2.0, (dimensions.Height - image.Height) / 2.0, image.Width, image.Height);
                else if (num1 > num2)
                {
                    double width = image.Width / image.Height * dimensions.Width;
                    rectangle = new Rect((dimensions.Width - width) / 2.0, 0.0, width, dimensions.Height);
                }
                else if (num1 < num2)
                {
                    double height = image.Height / image.Width * dimensions.Height;
                    rectangle = new Rect(0.0, (dimensions.Height - height) / 2.0, dimensions.Width, height);
                }
                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                drawingContext.DrawImage(image, rectangle);
                drawingContext.Close();
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)dimensions.Width, (int)dimensions.Height, 96.0, 96.0, PixelFormats.Pbgra32);
                renderTargetBitmap.Render((Visual)drawingVisual);
                bestMatch = BitmapFrame.Create((BitmapSource)renderTargetBitmap);
            }
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder bitmapEncoder = (BitmapEncoder)new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(bestMatch);
                bitmapEncoder.Save((Stream)memoryStream);
                using (ManagedIStream managedIstream = new ManagedIStream((Stream)memoryStream))
                {
                    IntPtr bitmap = IntPtr.Zero;
                    try
                    {
                        IntPtr hbmReturn;
                        return (uint)NativeMethods.GdipCreateBitmapFromStream((IStream)managedIstream, out bitmap) > 0U || (uint)NativeMethods.GdipCreateHICONFromBitmap(bitmap, out hbmReturn) > 0U ? IntPtr.Zero : hbmReturn;
                    }
                    finally
                    {
                        Utility.SafeDisposeImage(ref bitmap);
                    }
                }
            }
        }

        public static BitmapFrame GetBestMatch(
          IList<BitmapFrame> frames,
          int width,
          int height)
        {
            return Utility._GetBestMatch(frames, Utility._GetBitDepth(), width, height);
        }

        private static int _MatchImage(
          BitmapFrame frame,
          int bitDepth,
          int width,
          int height,
          int bpp)
        {
            return 2 * Utility._WeightedAbs(bpp, bitDepth, false) + Utility._WeightedAbs(frame.PixelWidth, width, true) + Utility._WeightedAbs(frame.PixelHeight, height, true);
        }

        private static int _WeightedAbs(int valueHave, int valueWant, bool fPunish)
        {
            int num = valueHave - valueWant;
            if (num < 0)
                num = (fPunish ? -2 : -1) * num;
            return num;
        }

        private static BitmapFrame _GetBestMatch(
          IList<BitmapFrame> frames,
          int bitDepth,
          int width,
          int height)
        {
            int num1 = int.MaxValue;
            int num2 = 0;
            int index1 = 0;
            bool flag = frames[0].Decoder is IconBitmapDecoder;
            for (int index2 = 0; index2 < frames.Count && (uint)num1 > 0U; ++index2)
            {
                PixelFormat format;
                int bitsPerPixel;
                if (!flag)
                {
                    format = frames[index2].Format;
                    bitsPerPixel = format.BitsPerPixel;
                }
                else
                {
                    format = frames[index2].Thumbnail.Format;
                    bitsPerPixel = format.BitsPerPixel;
                }
                int bpp = bitsPerPixel;
                if (bpp == 0)
                    bpp = 8;
                int num3 = Utility._MatchImage(frames[index2], bitDepth, width, height, bpp);
                if (num3 < num1)
                {
                    index1 = index2;
                    num2 = bpp;
                    num1 = num3;
                }
                else if (num3 == num1 && num2 < bpp)
                {
                    index1 = index2;
                    num2 = bpp;
                }
            }
            return frames[index1];
        }

        private static int _GetBitDepth()
        {
            if (Utility.s_bitDepth == 0)
            {
                using (SafeDC desktop = SafeDC.GetDesktop())
                    Utility.s_bitDepth = NativeMethods.GetDeviceCaps(desktop, DeviceCap.BITSPIXEL) * NativeMethods.GetDeviceCaps(desktop, DeviceCap.PLANES);
            }
            return Utility.s_bitDepth;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            File.Delete(path);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDeleteObject(ref IntPtr gdiObject)
        {
            IntPtr hObject = gdiObject;
            gdiObject = IntPtr.Zero;
            if (!(IntPtr.Zero != hObject))
                return;
            NativeMethods.DeleteObject(hObject);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDestroyIcon(ref IntPtr hicon)
        {
            IntPtr handle = hicon;
            hicon = IntPtr.Zero;
            if (!(IntPtr.Zero != handle))
                return;
            NativeMethods.DestroyIcon(handle);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDestroyWindow(ref IntPtr hwnd)
        {
            IntPtr hwnd1 = hwnd;
            hwnd = IntPtr.Zero;
            if (!NativeMethods.IsWindow(hwnd1))
                return;
            NativeMethods.DestroyWindow(hwnd1);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDispose<T>(ref T disposable) where T : IDisposable
        {
            IDisposable disposable1 = (IDisposable)disposable;
            disposable = default(T);
            disposable1?.Dispose();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDisposeImage(ref IntPtr gdipImage)
        {
            IntPtr image = gdipImage;
            gdipImage = IntPtr.Zero;
            if (!(IntPtr.Zero != image))
                return;
            int num = (int)NativeMethods.GdipDisposeImage(image);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static void SafeCoTaskMemFree(ref IntPtr ptr)
        {
            IntPtr ptr1 = ptr;
            ptr = IntPtr.Zero;
            if (!(IntPtr.Zero != ptr1))
                return;
            Marshal.FreeCoTaskMem(ptr1);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static void SafeFreeHGlobal(ref IntPtr hglobal)
        {
            IntPtr hglobal1 = hglobal;
            hglobal = IntPtr.Zero;
            if (!(IntPtr.Zero != hglobal1))
                return;
            Marshal.FreeHGlobal(hglobal1);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static void SafeRelease<T>(ref T comObject) where T : class
        {
            T obj = comObject;
            comObject = default(T);
            if ((object)obj == null)
                return;
            Assert.IsTrue(Marshal.IsComObject((object)obj));
            Marshal.ReleaseComObject((object)obj);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void GeneratePropertyString(
          StringBuilder source,
          string propertyName,
          string value)
        {
            Assert.IsNotNull<StringBuilder>(source);
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));
            if ((uint)source.Length > 0U)
                source.Append(' ');
            source.Append(propertyName);
            source.Append(": ");
            if (string.IsNullOrEmpty(value))
            {
                source.Append("<null>");
            }
            else
            {
                source.Append('"');
                source.Append(value);
                source.Append('"');
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Obsolete]
        public static string GenerateToString<T>(T @object) where T : struct
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if ((uint)stringBuilder.Length > 0U)
                    stringBuilder.Append(", ");
                Assert.AreEqual<int>(0, property.GetIndexParameters().Length);
                object obj = property.GetValue((object)@object, (object[])null);
                string format = obj == null ? "{0}: <null>" : "{0}: \"{1}\"";
                stringBuilder.AppendFormat(format, (object)property.Name, obj);
            }
            return stringBuilder.ToString();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void CopyStream(Stream destination, Stream source)
        {
            Assert.IsNotNull<Stream>(source);
            Assert.IsNotNull<Stream>(destination);
            destination.Position = 0L;
            if (source.CanSeek)
            {
                source.Position = 0L;
                destination.SetLength(source.Length);
            }
            byte[] buffer = new byte[4096];
            int count;
            do
            {
                count = source.Read(buffer, 0, buffer.Length);
                if ((uint)count > 0U)
                    destination.Write(buffer, 0, count);
            }
            while (buffer.Length == count);
            destination.Position = 0L;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string HashStreamMD5(Stream stm)
        {
            stm.Position = 0L;
            StringBuilder stringBuilder = new StringBuilder();
            using (MD5 md5 = MD5.Create())
            {
                foreach (byte num in md5.ComputeHash(stm))
                    stringBuilder.Append(num.ToString("x2", (IFormatProvider)CultureInfo.InvariantCulture));
            }
            return stringBuilder.ToString();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void EnsureDirectory(string path)
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool MemCmp(byte[] left, byte[] right, int cb)
        {
            Assert.IsNotNull<byte[]>(left);
            Assert.IsNotNull<byte[]>(right);
            Assert.IsTrue(cb <= Math.Min(left.Length, right.Length));
            GCHandle gcHandle1 = GCHandle.Alloc((object)left, GCHandleType.Pinned);
            IntPtr left1 = gcHandle1.AddrOfPinnedObject();
            GCHandle gcHandle2 = GCHandle.Alloc((object)right, GCHandleType.Pinned);
            IntPtr right1 = gcHandle2.AddrOfPinnedObject();
            bool flag = Utility._MemCmp(left1, right1, (long)cb);
            gcHandle1.Free();
            gcHandle2.Free();
            return flag;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string UrlDecode(string url)
        {
            if (url == null)
                return (string)null;
            Utility._UrlDecoder urlDecoder = new Utility._UrlDecoder(url.Length, Encoding.UTF8);
            int length = url.Length;
            for (int index = 0; index < length; ++index)
            {
                char ch = url[index];
                int num1;
                switch (ch)
                {
                    case '%':
                        num1 = index < length - 2 ? 1 : 0;
                        break;
                    case '+':
                        urlDecoder.AddByte((byte)32);
                        continue;
                    default:
                        num1 = 0;
                        break;
                }
                if (num1 != 0)
                {
                    if (url[index + 1] == 'u' && index < length - 5)
                    {
                        int num2 = Utility._HexToInt(url[index + 2]);
                        int num3 = Utility._HexToInt(url[index + 3]);
                        int num4 = Utility._HexToInt(url[index + 4]);
                        int num5 = Utility._HexToInt(url[index + 5]);
                        if (num2 >= 0 && num3 >= 0 && num4 >= 0 && num5 >= 0)
                        {
                            urlDecoder.AddChar((char)(num2 << 12 | num3 << 8 | num4 << 4 | num5));
                            index += 5;
                            continue;
                        }
                    }
                    else
                    {
                        int num2 = Utility._HexToInt(url[index + 1]);
                        int num3 = Utility._HexToInt(url[index + 2]);
                        if (num2 >= 0 && num3 >= 0)
                        {
                            urlDecoder.AddByte((byte)(num2 << 4 | num3));
                            index += 2;
                            continue;
                        }
                    }
                }
                if (((int)ch & 65408) == 0)
                    urlDecoder.AddByte((byte)ch);
                else
                    urlDecoder.AddChar(ch);
            }
            return urlDecoder.GetString();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string UrlEncode(string url)
        {
            if (url == null)
                return (string)null;
            byte[] bytes = Encoding.UTF8.GetBytes(url);
            bool flag = false;
            int num1 = 0;
            foreach (byte b in bytes)
            {
                if (b == (byte)32)
                    flag = true;
                else if (!Utility._UrlEncodeIsSafe(b))
                {
                    ++num1;
                    flag = true;
                }
            }
            if (flag)
            {
                byte[] numArray1 = new byte[bytes.Length + num1 * 2];
                int actual = 0;
                foreach (byte b in bytes)
                {
                    if (Utility._UrlEncodeIsSafe(b))
                        numArray1[actual++] = b;
                    else if (b == (byte)32)
                    {
                        numArray1[actual++] = (byte)43;
                    }
                    else
                    {
                        byte[] numArray2 = numArray1;
                        int index1 = actual;
                        int num2 = index1 + 1;
                        numArray2[index1] = (byte)37;
                        byte[] numArray3 = numArray1;
                        int index2 = num2;
                        int num3 = index2 + 1;
                        int hex1 = (int)Utility._IntToHex((int)b >> 4 & 15);
                        numArray3[index2] = (byte)hex1;
                        byte[] numArray4 = numArray1;
                        int index3 = num3;
                        actual = index3 + 1;
                        int hex2 = (int)Utility._IntToHex((int)b & 15);
                        numArray4[index3] = (byte)hex2;
                    }
                }
                bytes = numArray1;
                Assert.AreEqual<int>(numArray1.Length, actual);
            }
            return Encoding.ASCII.GetString(bytes);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool _UrlEncodeIsSafe(byte b)
        {
            if (Utility._IsAsciiAlphaNumeric(b))
                return true;
            switch ((char)b)
            {
                case '!':
                case '\'':
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                    return true;
                default:
                    return false;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool _IsAsciiAlphaNumeric(byte b) => b >= (byte)97 && b <= (byte)122 || b >= (byte)65 && b <= (byte)90 || b >= (byte)48 && b <= (byte)57;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static byte _IntToHex(int n)
        {
            Assert.BoundedInteger(0, n, 16);
            return n <= 9 ? (byte)(n + 48) : (byte)(n - 10 + 65);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static int _HexToInt(char h)
        {
            if (h >= '0' && h <= '9')
                return (int)h - 48;
            if (h >= 'a' && h <= 'f')
                return (int)h - 97 + 10;
            if (h >= 'A' && h <= 'F')
                return (int)h - 65 + 10;
            Assert.Fail("Invalid hex character " + h.ToString());
            return -1;
        }

        public static void AddDependencyPropertyChangeListener(
          object component,
          DependencyProperty property,
          EventHandler listener)
        {
            if (component == null)
                return;
            Assert.IsNotNull<DependencyProperty>(property);
            Assert.IsNotNull<EventHandler>(listener);
            DependencyPropertyDescriptor.FromProperty(property, component.GetType()).AddValueChanged(component, listener);
        }

        public static void RemoveDependencyPropertyChangeListener(
          object component,
          DependencyProperty property,
          EventHandler listener)
        {
            if (component == null)
                return;
            Assert.IsNotNull<DependencyProperty>(property);
            Assert.IsNotNull<EventHandler>(listener);
            DependencyPropertyDescriptor.FromProperty(property, component.GetType()).RemoveValueChanged(component, listener);
        }

        public static bool IsThicknessNonNegative(Thickness thickness) => Utility.IsDoubleFiniteAndNonNegative(thickness.Top) && Utility.IsDoubleFiniteAndNonNegative(thickness.Left) && (Utility.IsDoubleFiniteAndNonNegative(thickness.Bottom) && Utility.IsDoubleFiniteAndNonNegative(thickness.Right));

        public static bool IsCornerRadiusValid(CornerRadius cornerRadius) => Utility.IsDoubleFiniteAndNonNegative(cornerRadius.TopLeft) && Utility.IsDoubleFiniteAndNonNegative(cornerRadius.TopRight) && (Utility.IsDoubleFiniteAndNonNegative(cornerRadius.BottomLeft) && Utility.IsDoubleFiniteAndNonNegative(cornerRadius.BottomRight));

        public static bool IsDoubleFiniteAndNonNegative(double d) => !double.IsNaN(d) && !double.IsInfinity(d) && d >= 0.0;

        private class _UrlDecoder
        {
            private readonly Encoding _encoding;
            private readonly char[] _charBuffer;
            private readonly byte[] _byteBuffer;
            private int _byteCount;
            private int _charCount;

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public _UrlDecoder(int size, Encoding encoding)
            {
                this._encoding = encoding;
                this._charBuffer = new char[size];
                this._byteBuffer = new byte[size];
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void AddByte(byte b) => this._byteBuffer[this._byteCount++] = b;

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void AddChar(char ch)
            {
                this._FlushBytes();
                this._charBuffer[this._charCount++] = ch;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            private void _FlushBytes()
            {
                if (this._byteCount <= 0)
                    return;
                this._charCount += this._encoding.GetChars(this._byteBuffer, 0, this._byteCount, this._charBuffer, this._charCount);
                this._byteCount = 0;
            }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string GetString()
            {
                this._FlushBytes();
                return this._charCount > 0 ? new string(this._charBuffer, 0, this._charCount) : "";
            }
        }
    }
}
