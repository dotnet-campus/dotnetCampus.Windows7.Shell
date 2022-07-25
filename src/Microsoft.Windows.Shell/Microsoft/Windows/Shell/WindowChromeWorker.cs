using Standard;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Windows.Shell
{
    internal class WindowChromeWorker : DependencyObject
    {
        private const SWP _SwpFlags = SWP.DRAWFRAME | SWP.NOACTIVATE | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOSIZE | SWP.NOZORDER;
        private readonly List<KeyValuePair<WM, MessageHandler>> _messageTable;
        private Window _window;
        private IntPtr _hwnd;
        private HwndSource _hwndSource = (HwndSource)null;
        private bool _isHooked = false;
        private bool _isFixedUp = false;
        private bool _isUserResizing = false;
        private bool _hasUserMovedWindow = false;
        private Point _windowPosAtStartOfUserMove = new Point();
        private WindowChrome _chromeInfo;
        private WindowState _lastRoundingState;
        private WindowState _lastMenuState;
        private bool _isGlassEnabled;
        public static readonly DependencyProperty WindowChromeWorkerProperty = DependencyProperty.RegisterAttached(nameof(WindowChromeWorker), typeof(WindowChromeWorker), typeof(WindowChromeWorker), new PropertyMetadata((object)null, new PropertyChangedCallback(WindowChromeWorker._OnChromeWorkerChanged)));
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
        private static readonly HT[,] _HitTestBorders = new HT[3, 3]
        {
      {
        HT.TOPLEFT,
        HT.TOP,
        HT.TOPRIGHT
      },
      {
        HT.LEFT,
        HT.CLIENT,
        HT.RIGHT
      },
      {
        HT.BOTTOMLEFT,
        HT.BOTTOM,
        HT.BOTTOMRIGHT
      }
        };

        public WindowChromeWorker()
        {
            this._messageTable = new List<KeyValuePair<WM, MessageHandler>>()
      {
        new KeyValuePair<WM, MessageHandler>(WM.SETTEXT, new MessageHandler(this._HandleSetTextOrIcon)),
        new KeyValuePair<WM, MessageHandler>(WM.SETICON, new MessageHandler(this._HandleSetTextOrIcon)),
        new KeyValuePair<WM, MessageHandler>(WM.NCACTIVATE, new MessageHandler(this._HandleNCActivate)),
        new KeyValuePair<WM, MessageHandler>(WM.NCCALCSIZE, new MessageHandler(this._HandleNCCalcSize)),
        new KeyValuePair<WM, MessageHandler>(WM.NCHITTEST, new MessageHandler(this._HandleNCHitTest)),
        new KeyValuePair<WM, MessageHandler>(WM.NCRBUTTONUP, new MessageHandler(this._HandleNCRButtonUp)),
        new KeyValuePair<WM, MessageHandler>(WM.SIZE, new MessageHandler(this._HandleSize)),
        new KeyValuePair<WM, MessageHandler>(WM.WINDOWPOSCHANGED, new MessageHandler(this._HandleWindowPosChanged)),
        new KeyValuePair<WM, MessageHandler>(WM.DWMCOMPOSITIONCHANGED, new MessageHandler(this._HandleDwmCompositionChanged))
      };
            if (!Utility.IsPresentationFrameworkVersionLessThan4)
                return;
            this._messageTable.AddRange((IEnumerable<KeyValuePair<WM, MessageHandler>>)new KeyValuePair<WM, MessageHandler>[4]
            {
        new KeyValuePair<WM, MessageHandler>(WM.WININICHANGE, new MessageHandler(this._HandleSettingChange)),
        new KeyValuePair<WM, MessageHandler>(WM.ENTERSIZEMOVE, new MessageHandler(this._HandleEnterSizeMove)),
        new KeyValuePair<WM, MessageHandler>(WM.EXITSIZEMOVE, new MessageHandler(this._HandleExitSizeMove)),
        new KeyValuePair<WM, MessageHandler>(WM.MOVE, new MessageHandler(this._HandleMove))
            });
        }

        public void SetWindowChrome(WindowChrome newChrome)
        {
            this.VerifyAccess();
            Assert.IsNotNull<Window>(this._window);
            if (newChrome == this._chromeInfo)
                return;
            if (this._chromeInfo != null)
                this._chromeInfo.PropertyChangedThatRequiresRepaint -= new EventHandler(this._OnChromePropertyChangedThatRequiresRepaint);
            this._chromeInfo = newChrome;
            if (this._chromeInfo != null)
                this._chromeInfo.PropertyChangedThatRequiresRepaint += new EventHandler(this._OnChromePropertyChangedThatRequiresRepaint);
            this._ApplyNewCustomChrome();
        }

        private void _OnChromePropertyChangedThatRequiresRepaint(object sender, EventArgs e) => this._UpdateFrameState(true);

        private static void _OnChromeWorkerChanged(
          DependencyObject d,
          DependencyPropertyChangedEventArgs e)
        {
            Window window = (Window)d;
            WindowChromeWorker newValue = (WindowChromeWorker)e.NewValue;
            Assert.IsNotNull<Window>(window);
            Assert.IsNotNull<WindowChromeWorker>(newValue);
            Assert.IsNull<Window>(newValue._window);
            newValue._SetWindow(window);
        }

        private void _SetWindow(Window window)
        {
            Assert.IsNull<Window>(this._window);
            Assert.IsNotNull<Window>(window);
            this._window = window;
            this._hwnd = new WindowInteropHelper(this._window).Handle;
            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                Utility.AddDependencyPropertyChangeListener((object)this._window, Control.TemplateProperty, new EventHandler(this._OnWindowPropertyChangedThatRequiresTemplateFixup));
                Utility.AddDependencyPropertyChangeListener((object)this._window, FrameworkElement.FlowDirectionProperty, new EventHandler(this._OnWindowPropertyChangedThatRequiresTemplateFixup));
            }
            this._window.Closed += new EventHandler(this._UnsetWindow);
            if (IntPtr.Zero != this._hwnd)
            {
                this._hwndSource = HwndSource.FromHwnd(this._hwnd);
                Assert.IsNotNull<HwndSource>(this._hwndSource);
                this._window.ApplyTemplate();
                if (this._chromeInfo == null)
                    return;
                this._ApplyNewCustomChrome();
            }
            else
                this._window.SourceInitialized += (EventHandler)((sender, e) =>
               {
                   this._hwnd = new WindowInteropHelper(this._window).Handle;
                   Assert.IsNotDefault<IntPtr>(this._hwnd);
                   this._hwndSource = HwndSource.FromHwnd(this._hwnd);
                   Assert.IsNotNull<HwndSource>(this._hwndSource);
                   if (this._chromeInfo == null)
                       return;
                   this._ApplyNewCustomChrome();
               });
        }

        private void _UnsetWindow(object sender, EventArgs e)
        {
            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                Utility.RemoveDependencyPropertyChangeListener((object)this._window, Control.TemplateProperty, new EventHandler(this._OnWindowPropertyChangedThatRequiresTemplateFixup));
                Utility.RemoveDependencyPropertyChangeListener((object)this._window, FrameworkElement.FlowDirectionProperty, new EventHandler(this._OnWindowPropertyChangedThatRequiresTemplateFixup));
            }
            if (this._chromeInfo != null)
                this._chromeInfo.PropertyChangedThatRequiresRepaint -= new EventHandler(this._OnChromePropertyChangedThatRequiresRepaint);
            this._RestoreStandardChromeState(true);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static WindowChromeWorker GetWindowChromeWorker(Window window)
        {
            Verify.IsNotNull<Window>(window, nameof(window));
            return (WindowChromeWorker)window.GetValue(WindowChromeWorker.WindowChromeWorkerProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetWindowChromeWorker(Window window, WindowChromeWorker chrome)
        {
            Verify.IsNotNull<Window>(window, nameof(window));
            window.SetValue(WindowChromeWorker.WindowChromeWorkerProperty, (object)chrome);
        }

        private void _OnWindowPropertyChangedThatRequiresTemplateFixup(object sender, EventArgs e)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (this._chromeInfo == null || !(this._hwnd != IntPtr.Zero))
                return;
            this._window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Delegate)new WindowChromeWorker._Action(this._FixupTemplateIssues));
        }

        private void _ApplyNewCustomChrome()
        {
            if (this._hwnd == IntPtr.Zero)
                return;
            if (this._chromeInfo == null)
            {
                this._RestoreStandardChromeState(false);
            }
            else
            {
                if (!this._isHooked)
                {
                    this._hwndSource.AddHook(new HwndSourceHook(this._WndProc));
                    this._isHooked = true;
                }
                this._FixupTemplateIssues();
                this._UpdateSystemMenu(new WindowState?(this._window.WindowState));
                this._UpdateFrameState(true);
                NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP.DRAWFRAME | SWP.NOACTIVATE | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOSIZE | SWP.NOZORDER);
            }
        }

        private void _FixupRestoreBounds(object sender, EventArgs e)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (this._window.WindowState != WindowState.Maximized && this._window.WindowState != WindowState.Minimized || !this._hasUserMovedWindow)
                return;
            this._hasUserMovedWindow = false;
            WINDOWPLACEMENT windowPlacement = NativeMethods.GetWindowPlacement(this._hwnd);
            RECT adjustedWindowRect = this._GetAdjustedWindowRect(new RECT()
            {
                Bottom = 100,
                Right = 100
            });
            DpiScale dpi = VisualTreeHelper.GetDpi(_window);

            Point logical = DpiHelper.DevicePixelsToLogical(new Point((double)(windowPlacement.rcNormalPosition.Left - adjustedWindowRect.Left), (double)(windowPlacement.rcNormalPosition.Top - adjustedWindowRect.Top)),
                dpi.DpiScaleX, dpi.DpiScaleY);
            this._window.Top = logical.Y;
            this._window.Left = logical.X;
        }

        private void _FixupTemplateIssues()
        {
            Assert.IsNotNull<WindowChrome>(this._chromeInfo);
            Assert.IsNotNull<Window>(this._window);
            if (this._window.Template == null)
                return;
            if (VisualTreeHelper.GetChildrenCount((DependencyObject)this._window) == 0)
            {
                this._window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Delegate)new WindowChromeWorker._Action(this._FixupTemplateIssues));
            }
            else
            {
                FrameworkElement child = (FrameworkElement)VisualTreeHelper.GetChild((DependencyObject)this._window, 0);
                Thickness thickness1 = new Thickness();
                Thickness resizeBorderThickness;
                if ((uint)this._chromeInfo.NonClientFrameEdges > 0U)
                {
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 2))
                    {
                        ref Thickness local = ref thickness1;
                        double top1 = local.Top;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double top2 = resizeBorderThickness.Top;
                        local.Top = top1 - top2;
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 1))
                    {
                        ref Thickness local = ref thickness1;
                        double left1 = local.Left;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double left2 = resizeBorderThickness.Left;
                        local.Left = left1 - left2;
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 8))
                    {
                        ref Thickness local = ref thickness1;
                        double bottom1 = local.Bottom;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double bottom2 = resizeBorderThickness.Bottom;
                        local.Bottom = bottom1 - bottom2;
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 4))
                    {
                        ref Thickness local = ref thickness1;
                        double right1 = local.Right;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double right2 = resizeBorderThickness.Right;
                        local.Right = right1 - right2;
                    }
                }
                if (Utility.IsPresentationFrameworkVersionLessThan4)
                {
                    RECT windowRect = NativeMethods.GetWindowRect(this._hwnd);
                    RECT adjustedWindowRect = this._GetAdjustedWindowRect(windowRect);
                    Rect logical1 = DpiHelper.DeviceRectToLogical(new Rect((double)windowRect.Left, (double)windowRect.Top, (double)windowRect.Width, (double)windowRect.Height));
                    Rect logical2 = DpiHelper.DeviceRectToLogical(new Rect((double)adjustedWindowRect.Left, (double)adjustedWindowRect.Top, (double)adjustedWindowRect.Width, (double)adjustedWindowRect.Height));
                    if (!Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 1))
                    {
                        ref Thickness local = ref thickness1;
                        double right = local.Right;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double left = resizeBorderThickness.Left;
                        local.Right = right - left;
                    }
                    if (!Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 4))
                    {
                        ref Thickness local = ref thickness1;
                        double right1 = local.Right;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double right2 = resizeBorderThickness.Right;
                        local.Right = right1 - right2;
                    }
                    if (!Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 2))
                    {
                        ref Thickness local = ref thickness1;
                        double bottom = local.Bottom;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double top = resizeBorderThickness.Top;
                        local.Bottom = bottom - top;
                    }
                    if (!Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 8))
                    {
                        ref Thickness local = ref thickness1;
                        double bottom1 = local.Bottom;
                        resizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness;
                        double bottom2 = resizeBorderThickness.Bottom;
                        local.Bottom = bottom1 - bottom2;
                    }
                    thickness1.Bottom -= SystemParameters2.Current.WindowCaptionHeight;
                    if (this._window.FlowDirection == FlowDirection.RightToLeft)
                    {
                        Thickness thickness2 = new Thickness(logical1.Left - logical2.Left, logical1.Top - logical2.Top, logical2.Right - logical1.Right, logical2.Bottom - logical1.Bottom);
                        child.RenderTransform = (Transform)new MatrixTransform(1.0, 0.0, 0.0, 1.0, -(thickness2.Left + thickness2.Right), 0.0);
                    }
                    else
                        child.RenderTransform = (Transform)null;
                }
                child.Margin = thickness1;
                if (!Utility.IsPresentationFrameworkVersionLessThan4 || this._isFixedUp)
                    return;
                this._hasUserMovedWindow = false;
                this._window.StateChanged += new EventHandler(this._FixupRestoreBounds);
                this._isFixedUp = true;
            }
        }

        private RECT _GetAdjustedWindowRect(RECT rcWindow)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            WS windowLongPtr1 = (WS)(int)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE);
            WS_EX windowLongPtr2 = (WS_EX)(int)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.EXSTYLE);
            return NativeMethods.AdjustWindowRectEx(rcWindow, windowLongPtr1, false, windowLongPtr2);
        }

        private HT _GetHTFromResizeGripDirection(ResizeGripDirection direction)
        {
            bool flag = this._window.FlowDirection == FlowDirection.RightToLeft;
            switch (direction)
            {
                case ResizeGripDirection.TopLeft:
                    return flag ? HT.TOPRIGHT : HT.TOPLEFT;
                case ResizeGripDirection.Top:
                    return HT.TOP;
                case ResizeGripDirection.TopRight:
                    return flag ? HT.TOPLEFT : HT.TOPRIGHT;
                case ResizeGripDirection.Right:
                    return flag ? HT.LEFT : HT.RIGHT;
                case ResizeGripDirection.BottomRight:
                    return flag ? HT.BOTTOMLEFT : HT.BOTTOMRIGHT;
                case ResizeGripDirection.Bottom:
                    return HT.BOTTOM;
                case ResizeGripDirection.BottomLeft:
                    return flag ? HT.BOTTOMRIGHT : HT.BOTTOMLEFT;
                case ResizeGripDirection.Left:
                    return flag ? HT.RIGHT : HT.LEFT;
                default:
                    return HT.NOWHERE;
            }
        }

        private bool _IsWindowDocked
        {
            get
            {
                Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
                if ((uint)this._window.WindowState > 0U)
                    return false;
                RECT adjustedWindowRect = this._GetAdjustedWindowRect(new RECT()
                {
                    Bottom = 100,
                    Right = 100
                });
                Point point = new Point(this._window.Left, this._window.Top);
                DpiScale dpi = VisualTreeHelper.GetDpi(_window);

                point -= (Vector)DpiHelper.DevicePixelsToLogical(new Point((double)adjustedWindowRect.Left, (double)adjustedWindowRect.Top),
                    dpi.DpiScaleX, dpi.DpiScaleY);
                return this._window.RestoreBounds.Location != point;
            }
        }

        private IntPtr _WndProc(
          IntPtr hwnd,
          int msg,
          IntPtr wParam,
          IntPtr lParam,
          ref bool handled)
        {
            Assert.AreEqual<IntPtr>(hwnd, this._hwnd);
            WM uMsg = (WM)msg;
            foreach (KeyValuePair<WM, MessageHandler> keyValuePair in this._messageTable)
            {
                if (keyValuePair.Key == uMsg)
                    return keyValuePair.Value(uMsg, wParam, lParam, out handled);
            }
            return IntPtr.Zero;
        }

        private IntPtr _HandleSetTextOrIcon(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            bool flag = this._ModifyStyle(WS.VISIBLE, WS.OVERLAPPED);
            IntPtr num = NativeMethods.DefWindowProc(this._hwnd, uMsg, wParam, lParam);
            if (flag)
                this._ModifyStyle(WS.OVERLAPPED, WS.VISIBLE);
            handled = true;
            return num;
        }

        private IntPtr _HandleNCActivate(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            IntPtr num = NativeMethods.DefWindowProc(this._hwnd, WM.NCACTIVATE, wParam, new IntPtr(-1));
            handled = true;
            return num;
        }

        private IntPtr _HandleNCCalcSize(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            if (wParam != IntPtr.Zero)
            {
                handled = true;
                RECT structure = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                structure.Bottom -= -1;
                Marshal.StructureToPtr((object)structure, lParam, false);
            }
            if ((uint)this._chromeInfo.NonClientFrameEdges > 0U)
            {
                Thickness device = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness);
                RECT structure = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 2))
                    structure.Top += (int)device.Top;
                if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 1))
                    structure.Left += (int)device.Left;
                if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 8))
                    structure.Bottom -= (int)device.Bottom;
                if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 4))
                    structure.Right -= (int)device.Right;
                Marshal.StructureToPtr((object)structure, lParam, false);
            }
            handled = true;
            return new IntPtr(1792);
        }

        private IntPtr _HandleNCHitTest(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(_window);

            // Let the system know if we consider the mouse to be in our effective non-client area.
            var mousePosScreen = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            Rect windowPosition = _GetWindowRect();

            Point mousePosWindow = mousePosScreen;
            mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
            mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow, dpi.DpiScaleX, dpi.DpiScaleY);

            // If the app is asking for content to be treated as client then that takes precedence over _everything_, even DWM caption buttons.
            // This allows apps to set the glass frame to be non-empty, still cover it with WPF content to hide all the glass,
            // yet still get DWM to draw a drop shadow.
            IInputElement inputElement = _window.InputHitTest(mousePosWindow);
            if (inputElement != null)
            {
                if (WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                {
                    handled = true;
                    return new IntPtr((int) HT.CLIENT);
                }

                ResizeGripDirection direction = WindowChrome.GetResizeGripDirection(inputElement);
                if (direction != ResizeGripDirection.None)
                {
                    handled = true;
                    return new IntPtr((int) _GetHTFromResizeGripDirection(direction));
                }
            }

            // It's not opted out, so offer up the hittest to DWM, then to our custom non-client area logic.
            if (_chromeInfo.UseAeroCaptionButtons)
            {
                IntPtr lRet;
                if (Utility.IsOSVistaOrNewer && _chromeInfo.GlassFrameThickness != default(Thickness) && _isGlassEnabled)
                {
                    // If we're on Vista, give the DWM a chance to handle the message first.
                    handled = NativeMethods.DwmDefWindowProc(_hwnd, uMsg, wParam, lParam, out lRet);

                    if (IntPtr.Zero != lRet)
                    {
                        // If DWM claims to have handled this, then respect their call.
                        return lRet;
                    }
                }
            }

            HT ht = _HitTestNca(
                DpiHelper.DeviceRectToLogical(windowPosition, dpi.DpiScaleX, dpi.DpiScaleY),
                DpiHelper.DevicePixelsToLogical(mousePosScreen, dpi.DpiScaleX, dpi.DpiScaleY));

            handled = true;
            return new IntPtr((int) ht);
        }

        private IntPtr _HandleNCRButtonUp(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            if (2 == wParam.ToInt32())
                SystemCommands.ShowSystemMenuPhysicalCoordinates(this._window, new Point((double)Utility.GET_X_LPARAM(lParam), (double)Utility.GET_Y_LPARAM(lParam)));
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            WindowState? assumeState = new WindowState?();
            if (wParam.ToInt32() == 2)
                assumeState = new WindowState?(WindowState.Maximized);
            this._UpdateSystemMenu(assumeState);
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleWindowPosChanged(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            this._UpdateSystemMenu(new WindowState?());
            if (!this._isGlassEnabled)
            {
                Assert.IsNotDefault<IntPtr>(lParam);
                this._SetRoundingRegion(new WINDOWPOS?((WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))));
            }
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleDwmCompositionChanged(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            this._UpdateFrameState(false);
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleSettingChange(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            this._FixupTemplateIssues();
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleEnterSizeMove(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            this._isUserResizing = true;
            Assert.Implies(this._window.WindowState == WindowState.Maximized, Utility.IsOSWindows7OrNewer);
            if (this._window.WindowState != WindowState.Maximized && !this._IsWindowDocked)
                this._windowPosAtStartOfUserMove = new Point(this._window.Left, this._window.Top);
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleExitSizeMove(
          WM uMsg,
          IntPtr wParam,
          IntPtr lParam,
          out bool handled)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            this._isUserResizing = false;
            if (this._window.WindowState == WindowState.Maximized)
            {
                Assert.IsTrue(Utility.IsOSWindows7OrNewer);
                this._window.Top = this._windowPosAtStartOfUserMove.Y;
                this._window.Left = this._windowPosAtStartOfUserMove.X;
            }
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleMove(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (this._isUserResizing)
                this._hasUserMovedWindow = true;
            handled = false;
            return IntPtr.Zero;
        }

        private bool _ModifyStyle(WS removeStyle, WS addStyle)
        {
            Assert.IsNotDefault<IntPtr>(this._hwnd);
            WS int32 = (WS)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE).ToInt32();
            WS ws = int32 & ~removeStyle | addStyle;
            if (int32 == ws)
                return false;
            NativeMethods.SetWindowLongPtr(this._hwnd, GWL.STYLE, new IntPtr((int)ws));
            return true;
        }

        private WindowState _GetHwndState()
        {
            switch (NativeMethods.GetWindowPlacement(this._hwnd).showCmd)
            {
                case SW.SHOWMINIMIZED:
                    return WindowState.Minimized;
                case SW.SHOWMAXIMIZED:
                    return WindowState.Maximized;
                default:
                    return WindowState.Normal;
            }
        }

        private Rect _GetWindowRect()
        {
            RECT windowRect = NativeMethods.GetWindowRect(this._hwnd);
            return new Rect((double)windowRect.Left, (double)windowRect.Top, (double)windowRect.Width, (double)windowRect.Height);
        }

        private void _UpdateSystemMenu(WindowState? assumeState)
        {
            WindowState windowState = (WindowState)((int?)assumeState ?? (int)this._GetHwndState());
            if (!assumeState.HasValue && this._lastMenuState == windowState)
                return;
            this._lastMenuState = windowState;
            bool flag1 = this._ModifyStyle(WS.VISIBLE, WS.OVERLAPPED);
            IntPtr systemMenu = NativeMethods.GetSystemMenu(this._hwnd, false);
            if (IntPtr.Zero != systemMenu)
            {
                WS int32 = (WS)NativeMethods.GetWindowLongPtr(this._hwnd, GWL.STYLE).ToInt32();
                bool flag2 = Utility.IsFlagSet((int)int32, 131072);
                bool flag3 = Utility.IsFlagSet((int)int32, 65536);
                bool flag4 = Utility.IsFlagSet((int)int32, 262144);
                switch (windowState)
                {
                    case WindowState.Minimized:
                        int num1 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.RESTORE, MF.ENABLED);
                        int num2 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MOVE, MF.GRAYED | MF.DISABLED);
                        int num3 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.SIZE, MF.GRAYED | MF.DISABLED);
                        int num4 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MINIMIZE, MF.GRAYED | MF.DISABLED);
                        int num5 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MAXIMIZE, flag3 ? MF.ENABLED : MF.GRAYED | MF.DISABLED);
                        break;
                    case WindowState.Maximized:
                        int num6 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.RESTORE, MF.ENABLED);
                        int num7 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MOVE, MF.GRAYED | MF.DISABLED);
                        int num8 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.SIZE, MF.GRAYED | MF.DISABLED);
                        int num9 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MINIMIZE, flag2 ? MF.ENABLED : MF.GRAYED | MF.DISABLED);
                        int num10 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MAXIMIZE, MF.GRAYED | MF.DISABLED);
                        break;
                    default:
                        int num11 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.RESTORE, MF.GRAYED | MF.DISABLED);
                        int num12 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MOVE, MF.ENABLED);
                        int num13 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.SIZE, flag4 ? MF.ENABLED : MF.GRAYED | MF.DISABLED);
                        int num14 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MINIMIZE, flag2 ? MF.ENABLED : MF.GRAYED | MF.DISABLED);
                        int num15 = (int)NativeMethods.EnableMenuItem(systemMenu, SC.MAXIMIZE, flag3 ? MF.ENABLED : MF.GRAYED | MF.DISABLED);
                        break;
                }
            }
            if (flag1)
                this._ModifyStyle(WS.OVERLAPPED, WS.VISIBLE);
        }

        private void _UpdateFrameState(bool force)
        {
            if (IntPtr.Zero == this._hwnd)
                return;
            bool flag = NativeMethods.DwmIsCompositionEnabled();
            if (!force && flag == this._isGlassEnabled)
                return;
            this._isGlassEnabled = flag && this._chromeInfo.GlassFrameThickness != new Thickness();
            if (!this._isGlassEnabled)
            {
                this._SetRoundingRegion(new WINDOWPOS?());
            }
            else
            {
                this._ClearRoundingRegion();
                this._ExtendGlassFrame();
            }
            NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP.DRAWFRAME | SWP.NOACTIVATE | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOSIZE | SWP.NOZORDER);
        }

        private void _ClearRoundingRegion() => NativeMethods.SetWindowRgn(this._hwnd, IntPtr.Zero, NativeMethods.IsWindowVisible(this._hwnd));

        private void _SetRoundingRegion(WINDOWPOS? wp)
        {
            if (NativeMethods.GetWindowPlacement(this._hwnd).showCmd == SW.SHOWMAXIMIZED)
            {
                int num1;
                int num2;
                if (wp.HasValue)
                {
                    num1 = wp.Value.x;
                    num2 = wp.Value.y;
                }
                else
                {
                    Rect windowRect = this._GetWindowRect();
                    num1 = (int)windowRect.Left;
                    num2 = (int)windowRect.Top;
                }
                RECT rcWork = NativeMethods.GetMonitorInfo(NativeMethods.MonitorFromWindow(this._hwnd, 2U)).rcWork;
                rcWork.Offset(-num1, -num2);
                IntPtr gdiObject = IntPtr.Zero;
                try
                {
                    gdiObject = NativeMethods.CreateRectRgnIndirect(rcWork);
                    NativeMethods.SetWindowRgn(this._hwnd, gdiObject, NativeMethods.IsWindowVisible(this._hwnd));
                    gdiObject = IntPtr.Zero;
                }
                finally
                {
                    Utility.SafeDeleteObject(ref gdiObject);
                }
            }
            else
            {
                Size size;
                if (wp.HasValue && !Utility.IsFlagSet(wp.Value.flags, 1))
                {
                    size = new Size((double)wp.Value.cx, (double)wp.Value.cy);
                }
                else
                {
                    if (wp.HasValue && this._lastRoundingState == this._window.WindowState)
                        return;
                    size = this._GetWindowRect().Size;
                }
                this._lastRoundingState = this._window.WindowState;
                IntPtr gdiObject = IntPtr.Zero;
                try
                {
                    double num = Math.Min(size.Width, size.Height);
                    CornerRadius cornerRadius = this._chromeInfo.CornerRadius;
                    Point device = DpiHelper.LogicalPixelsToDevice(new Point(cornerRadius.TopLeft, 0.0));
                    double radius1 = Math.Min(device.X, num / 2.0);
                    if (WindowChromeWorker._IsUniform(this._chromeInfo.CornerRadius))
                    {
                        gdiObject = WindowChromeWorker._CreateRoundRectRgn(new Rect(size), radius1);
                    }
                    else
                    {
                        gdiObject = WindowChromeWorker._CreateRoundRectRgn(new Rect(0.0, 0.0, size.Width / 2.0 + radius1, size.Height / 2.0 + radius1), radius1);
                        cornerRadius = this._chromeInfo.CornerRadius;
                        device = DpiHelper.LogicalPixelsToDevice(new Point(cornerRadius.TopRight, 0.0));
                        double radius2 = Math.Min(device.X, num / 2.0);
                        Rect region1 = new Rect(0.0, 0.0, size.Width / 2.0 + radius2, size.Height / 2.0 + radius2);
                        region1.Offset(size.Width / 2.0 - radius2, 0.0);
                        Assert.AreEqual<double>(region1.Right, size.Width);
                        WindowChromeWorker._CreateAndCombineRoundRectRgn(gdiObject, region1, radius2);
                        cornerRadius = this._chromeInfo.CornerRadius;
                        device = DpiHelper.LogicalPixelsToDevice(new Point(cornerRadius.BottomLeft, 0.0));
                        double radius3 = Math.Min(device.X, num / 2.0);
                        Rect region2 = new Rect(0.0, 0.0, size.Width / 2.0 + radius3, size.Height / 2.0 + radius3);
                        region2.Offset(0.0, size.Height / 2.0 - radius3);
                        Assert.AreEqual<double>(region2.Bottom, size.Height);
                        WindowChromeWorker._CreateAndCombineRoundRectRgn(gdiObject, region2, radius3);
                        cornerRadius = this._chromeInfo.CornerRadius;
                        device = DpiHelper.LogicalPixelsToDevice(new Point(cornerRadius.BottomRight, 0.0));
                        double radius4 = Math.Min(device.X, num / 2.0);
                        Rect region3 = new Rect(0.0, 0.0, size.Width / 2.0 + radius4, size.Height / 2.0 + radius4);
                        region3.Offset(size.Width / 2.0 - radius4, size.Height / 2.0 - radius4);
                        Assert.AreEqual<double>(region3.Right, size.Width);
                        Assert.AreEqual<double>(region3.Bottom, size.Height);
                        WindowChromeWorker._CreateAndCombineRoundRectRgn(gdiObject, region3, radius4);
                    }
                    NativeMethods.SetWindowRgn(this._hwnd, gdiObject, NativeMethods.IsWindowVisible(this._hwnd));
                    gdiObject = IntPtr.Zero;
                }
                finally
                {
                    Utility.SafeDeleteObject(ref gdiObject);
                }
            }
        }

        private static IntPtr _CreateRoundRectRgn(Rect region, double radius) => DoubleUtilities.AreClose(0.0, radius) ? NativeMethods.CreateRectRgn((int)Math.Floor(region.Left), (int)Math.Floor(region.Top), (int)Math.Ceiling(region.Right), (int)Math.Ceiling(region.Bottom)) : NativeMethods.CreateRoundRectRgn((int)Math.Floor(region.Left), (int)Math.Floor(region.Top), (int)Math.Ceiling(region.Right) + 1, (int)Math.Ceiling(region.Bottom) + 1, (int)Math.Ceiling(radius), (int)Math.Ceiling(radius));

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "HRGNs")]
        private static void _CreateAndCombineRoundRectRgn(
          IntPtr hrgnSource,
          Rect region,
          double radius)
        {
            IntPtr gdiObject = IntPtr.Zero;
            try
            {
                gdiObject = WindowChromeWorker._CreateRoundRectRgn(region, radius);
                if (NativeMethods.CombineRgn(hrgnSource, hrgnSource, gdiObject, RGN.OR) == CombineRgnResult.ERROR)
                    throw new InvalidOperationException("Unable to combine two HRGNs.");
            }
            finally
            {
                Utility.SafeDeleteObject(ref gdiObject);
            }
        }

        private static bool _IsUniform(CornerRadius cornerRadius) => DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.BottomRight) && DoubleUtilities.AreClose(cornerRadius.TopLeft, cornerRadius.TopRight) && DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.TopRight);

        private void _ExtendGlassFrame()
        {
            Assert.IsNotNull<Window>(this._window);
            if (!Utility.IsOSVistaOrNewer || IntPtr.Zero == this._hwnd)
                return;
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                this._hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            }
            else
            {
                this._hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
                Thickness device1 = DpiHelper.LogicalThicknessToDevice(this._chromeInfo.GlassFrameThickness);
                if ((uint)this._chromeInfo.NonClientFrameEdges > 0U)
                {
                    Thickness device2 = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness);
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 2))
                    {
                        device1.Top -= device2.Top;
                        device1.Top = Math.Max(0.0, device1.Top);
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 1))
                    {
                        device1.Left -= device2.Left;
                        device1.Left = Math.Max(0.0, device1.Left);
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 8))
                    {
                        device1.Bottom -= device2.Bottom;
                        device1.Bottom = Math.Max(0.0, device1.Bottom);
                    }
                    if (Utility.IsFlagSet((int)this._chromeInfo.NonClientFrameEdges, 4))
                    {
                        device1.Right -= device2.Right;
                        device1.Right = Math.Max(0.0, device1.Right);
                    }
                }
                MARGINS pMarInset = new MARGINS()
                {
                    cxLeftWidth = (int)Math.Ceiling(device1.Left),
                    cxRightWidth = (int)Math.Ceiling(device1.Right),
                    cyTopHeight = (int)Math.Ceiling(device1.Top),
                    cyBottomHeight = (int)Math.Ceiling(device1.Bottom)
                };
                try
                {
                    NativeMethods.DwmExtendFrameIntoClientArea(this._hwnd, ref pMarInset);
                }
                catch
                {
                }
            }
        }

        private HT _HitTestNca(Rect windowPosition, Point mousePosition)
        {
            int index1 = 1;
            int index2 = 1;
            bool flag = false;
            if (mousePosition.Y >= windowPosition.Top && mousePosition.Y < windowPosition.Top + this._chromeInfo.ResizeBorderThickness.Top + this._chromeInfo.CaptionHeight)
            {
                flag = mousePosition.Y < windowPosition.Top + this._chromeInfo.ResizeBorderThickness.Top;
                index1 = 0;
            }
            else if (mousePosition.Y < windowPosition.Bottom && mousePosition.Y >= windowPosition.Bottom - (double)(int)this._chromeInfo.ResizeBorderThickness.Bottom)
                index1 = 2;
            if (mousePosition.X >= windowPosition.Left && mousePosition.X < windowPosition.Left + (double)(int)this._chromeInfo.ResizeBorderThickness.Left)
                index2 = 0;
            else if (mousePosition.X < windowPosition.Right && mousePosition.X >= windowPosition.Right - this._chromeInfo.ResizeBorderThickness.Right)
                index2 = 2;
            if (index1 == 0 && index2 != 1 && !flag)
                index1 = 1;
            HT ht = WindowChromeWorker._HitTestBorders[index1, index2];
            if (ht == HT.TOP && !flag)
                ht = HT.CAPTION;
            return ht;
        }

        private void _RestoreStandardChromeState(bool isClosing)
        {
            this.VerifyAccess();
            this._UnhookCustomChrome();
            if (isClosing)
                return;
            this._RestoreFrameworkIssueFixups();
            this._RestoreGlassFrame();
            this._RestoreHrgn();
            this._window.InvalidateMeasure();
        }

        private void _UnhookCustomChrome()
        {
            Assert.IsNotDefault<IntPtr>(this._hwnd);
            Assert.IsNotNull<Window>(this._window);
            if (!this._isHooked)
                return;
            this._hwndSource.RemoveHook(new HwndSourceHook(this._WndProc));
            this._isHooked = false;
        }

        private void _RestoreFrameworkIssueFixups()
        {
            ((FrameworkElement)VisualTreeHelper.GetChild((DependencyObject)this._window, 0)).Margin = new Thickness();
            if (!Utility.IsPresentationFrameworkVersionLessThan4)
                return;
            Assert.IsTrue(this._isFixedUp);
            this._window.StateChanged -= new EventHandler(this._FixupRestoreBounds);
            this._isFixedUp = false;
        }

        private void _RestoreGlassFrame()
        {
            Assert.IsNull<WindowChrome>(this._chromeInfo);
            Assert.IsNotNull<Window>(this._window);
            if (!Utility.IsOSVistaOrNewer || this._hwnd == IntPtr.Zero)
                return;
            this._hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            if (!NativeMethods.DwmIsCompositionEnabled())
                return;
            try
            {
                MARGINS pMarInset = new MARGINS();
                NativeMethods.DwmExtendFrameIntoClientArea(this._hwnd, ref pMarInset);
            }
            catch
            {
            }
        }

        private void _RestoreHrgn()
        {
            this._ClearRoundingRegion();
            NativeMethods.SetWindowPos(this._hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP.DRAWFRAME | SWP.NOACTIVATE | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOSIZE | SWP.NOZORDER);
        }

        private delegate void _Action();
    }
}
