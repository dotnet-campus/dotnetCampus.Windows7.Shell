using Standard;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Microsoft.Windows.Shell
{
    public sealed class TaskbarItemInfo : Freezable
    {
        private const int c_MaximumThumbButtons = 7;
        private static readonly WM WM_TASKBARBUTTONCREATED = NativeMethods.RegisterWindowMessage("TaskbarButtonCreated");
        private static readonly Thickness _EmptyThickness = new Thickness();
        private SafeGdiplusStartupToken _gdipToken;
        private bool _haveAddedButtons;
        private Window _window;
        private HwndSource _hwndSource;
        private ITaskbarList3 _taskbarList;
        private readonly Size _overlaySize;
        private bool _isAttached;
        public static readonly DependencyProperty TaskbarItemInfoProperty = DependencyProperty.RegisterAttached(nameof(TaskbarItemInfo), typeof(TaskbarItemInfo), typeof(TaskbarItemInfo), new PropertyMetadata((object)null, new PropertyChangedCallback(TaskbarItemInfo._OnTaskbarItemInfoChanged), new CoerceValueCallback(TaskbarItemInfo._CoerceTaskbarItemInfoValue)));
        public static readonly DependencyProperty ProgressStateProperty = DependencyProperty.Register(nameof(ProgressState), typeof(TaskbarItemProgressState), typeof(TaskbarItemInfo), new PropertyMetadata((object)TaskbarItemProgressState.None, (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnProgressStateChanged()), (CoerceValueCallback)((d, e) => (object)TaskbarItemInfo._CoerceProgressState((TaskbarItemProgressState)e))));
        public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(nameof(ProgressValue), typeof(double), typeof(TaskbarItemInfo), new PropertyMetadata((object)0.0, (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnProgressValueChanged()), (CoerceValueCallback)((d, e) => (object)TaskbarItemInfo._CoerceProgressValue((double)e))));
        public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(nameof(Overlay), typeof(ImageSource), typeof(TaskbarItemInfo), new PropertyMetadata((object)null, (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnOverlayChanged())));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(TaskbarItemInfo), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnDescriptionChanged())));
        public static readonly DependencyProperty ThumbnailClipMarginProperty = DependencyProperty.Register(nameof(ThumbnailClipMargin), typeof(Thickness), typeof(TaskbarItemInfo), new PropertyMetadata((object)new Thickness(), (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnThumbnailClipMarginChanged()), (CoerceValueCallback)((d, e) => (object)TaskbarItemInfo._CoerceThumbnailClipMargin((Thickness)e))));
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos")]
        public static readonly DependencyProperty ThumbButtonInfosProperty = DependencyProperty.Register(nameof(ThumbButtonInfos), typeof(ThumbButtonInfoCollection), typeof(TaskbarItemInfo), new PropertyMetadata((object)null, (PropertyChangedCallback)((d, e) => ((TaskbarItemInfo)d)._OnThumbButtonsChanged())));

        protected override Freezable CreateInstanceCore() => (Freezable)new TaskbarItemInfo();

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static TaskbarItemInfo GetTaskbarItemInfo(Window window)
        {
            Verify.IsNotNull<Window>(window, nameof(window));
            return (TaskbarItemInfo)window.GetValue(TaskbarItemInfo.TaskbarItemInfoProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void SetTaskbarItemInfo(Window window, TaskbarItemInfo value)
        {
            Verify.IsNotNull<Window>(window, nameof(window));
            window.SetValue(TaskbarItemInfo.TaskbarItemInfoProperty, (object)value);
        }

        private static void _OnTaskbarItemInfoChanged(
          DependencyObject d,
          DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d))
                return;
            Window window = (Window)d;
            TaskbarItemInfo oldValue = (TaskbarItemInfo)e.OldValue;
            TaskbarItemInfo newValue = (TaskbarItemInfo)e.NewValue;
            if (oldValue == newValue || !Utility.IsOSWindows7OrNewer)
                return;
            if (oldValue != null && oldValue._window != null)
                oldValue._DetachWindow();
            newValue?._SetWindow(window);
        }

        private static object _CoerceTaskbarItemInfoValue(DependencyObject d, object value)
        {
            if (DesignerProperties.GetIsInDesignMode(d))
                return value;
            Verify.IsNotNull<DependencyObject>(d, nameof(d));
            Window window = (Window)d;
            TaskbarItemInfo taskbarItemInfo = (TaskbarItemInfo)value;
            if (taskbarItemInfo != null && (taskbarItemInfo._window != null && taskbarItemInfo._window != window))
                throw new NotSupportedException();
            window.VerifyAccess();
            return (object)taskbarItemInfo;
        }

        public TaskbarItemProgressState ProgressState
        {
            get => (TaskbarItemProgressState)this.GetValue(TaskbarItemInfo.ProgressStateProperty);
            set => this.SetValue(TaskbarItemInfo.ProgressStateProperty, (object)value);
        }

        private void _OnProgressStateChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateProgressState(true);
        }

        private static TaskbarItemProgressState _CoerceProgressState(
          TaskbarItemProgressState value)
        {
            switch (value)
            {
                case TaskbarItemProgressState.None:
                case TaskbarItemProgressState.Indeterminate:
                case TaskbarItemProgressState.Normal:
                case TaskbarItemProgressState.Error:
                case TaskbarItemProgressState.Paused:
                    return value;
                default:
                    value = TaskbarItemProgressState.None;
                    goto case TaskbarItemProgressState.None;
            }
        }

        public double ProgressValue
        {
            get => (double)this.GetValue(TaskbarItemInfo.ProgressValueProperty);
            set => this.SetValue(TaskbarItemInfo.ProgressValueProperty, (object)value);
        }

        private void _OnProgressValueChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateProgressValue(true);
        }

        private static double _CoerceProgressValue(double progressValue)
        {
            if (double.IsNaN(progressValue))
                progressValue = 0.0;
            progressValue = Math.Max(progressValue, 0.0);
            progressValue = Math.Min(1.0, progressValue);
            return progressValue;
        }

        public ImageSource Overlay
        {
            get => (ImageSource)this.GetValue(TaskbarItemInfo.OverlayProperty);
            set => this.SetValue(TaskbarItemInfo.OverlayProperty, (object)value);
        }

        private void _OnOverlayChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateOverlay(true);
        }

        public string Description
        {
            get => (string)this.GetValue(TaskbarItemInfo.DescriptionProperty);
            set => this.SetValue(TaskbarItemInfo.DescriptionProperty, (object)value);
        }

        private void _OnDescriptionChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateTooltip(true);
        }

        public Thickness ThumbnailClipMargin
        {
            get => (Thickness)this.GetValue(TaskbarItemInfo.ThumbnailClipMarginProperty);
            set => this.SetValue(TaskbarItemInfo.ThumbnailClipMarginProperty, (object)value);
        }

        private void _OnThumbnailClipMarginChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateThumbnailClipping(true);
        }

        private static Thickness _CoerceThumbnailClipMargin(Thickness margin) => margin.Left < 0.0 || margin.Right < 0.0 || margin.Top < 0.0 || margin.Bottom < 0.0 ? TaskbarItemInfo._EmptyThickness : margin;

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Infos")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ThumbButtonInfoCollection ThumbButtonInfos
        {
            get => (ThumbButtonInfoCollection)this.GetValue(TaskbarItemInfo.ThumbButtonInfosProperty);
            set => this.SetValue(TaskbarItemInfo.ThumbButtonInfosProperty, (object)value);
        }

        private void _OnThumbButtonsChanged()
        {
            if (!this._isAttached)
                return;
            this._UpdateThumbButtons(true);
        }

        private IntPtr _GetHICONFromImageSource(ImageSource image, Size dimensions)
        {
            if (this._gdipToken == null)
                this._gdipToken = SafeGdiplusStartupToken.Startup();
            return Utility.GenerateHICON(image, dimensions);
        }

        public TaskbarItemInfo()
        {
            if (!DesignerProperties.GetIsInDesignMode((DependencyObject)this))
            {
                ITaskbarList comObject = (ITaskbarList)null;
                try
                {
                    comObject = CLSID.CoCreateInstance<ITaskbarList>("56FDF344-FD6D-11d0-958A-006097C9A090");
                    comObject.HrInit();
                    this._taskbarList = comObject as ITaskbarList3;
                    comObject = (ITaskbarList)null;
                }
                finally
                {
                    Utility.SafeRelease<ITaskbarList>(ref comObject);
                }
                this._overlaySize = new Size((double)NativeMethods.GetSystemMetrics(SM.CXSMICON), (double)NativeMethods.GetSystemMetrics(SM.CYSMICON));
            }
            this.ThumbButtonInfos = new ThumbButtonInfoCollection();
        }

        private void _SetWindow(Window window)
        {
            Assert.IsNull<Window>(this._window);
            if (window == null)
                return;
            this._window = window;
            if (this._taskbarList == null)
                return;
            IntPtr handle = new WindowInteropHelper(this._window).Handle;
            if (!(handle != IntPtr.Zero))
            {
                this._window.SourceInitialized += new EventHandler(this._OnWindowSourceInitialized);
            }
            else
            {
                this._hwndSource = HwndSource.FromHwnd(handle);
                this._hwndSource.AddHook(new HwndSourceHook(this._WndProc));
                this._OnIsAttachedChanged(true);
            }
        }

        private void _OnWindowSourceInitialized(object sender, EventArgs e)
        {
            this._window.SourceInitialized -= new EventHandler(this._OnWindowSourceInitialized);
            IntPtr handle = new WindowInteropHelper(this._window).Handle;
            this._hwndSource = HwndSource.FromHwnd(handle);
            this._hwndSource.AddHook(new HwndSourceHook(this._WndProc));
            MSGFLTINFO filterInfo;
            NativeMethods.ChangeWindowMessageFilterEx(handle, TaskbarItemInfo.WM_TASKBARBUTTONCREATED, MSGFLT.ALLOW, out filterInfo);
            NativeMethods.ChangeWindowMessageFilterEx(handle, WM.COMMAND, MSGFLT.ALLOW, out filterInfo);
        }

        private IntPtr _WndProc(
          IntPtr hwnd,
          int uMsg,
          IntPtr wParam,
          IntPtr lParam,
          ref bool handled)
        {
            WM wm = (WM)uMsg;
            if (wm == TaskbarItemInfo.WM_TASKBARBUTTONCREATED)
            {
                this._OnIsAttachedChanged(true);
                this._isAttached = true;
                handled = false;
            }
            else
            {
                switch (wm)
                {
                    case WM.SIZE:
                        this._UpdateThumbnailClipping(this._isAttached);
                        handled = false;
                        break;
                    case WM.COMMAND:
                        if (Utility.HIWORD(wParam.ToInt32()) == 6144)
                        {
                            this.ThumbButtonInfos[Utility.LOWORD(wParam.ToInt32())].InvokeClick();
                            handled = true;
                            break;
                        }
                        break;
                }
            }
            return IntPtr.Zero;
        }

        private void _OnIsAttachedChanged(bool attached)
        {
            if (attached)
            {
                Assert.IsNotNull<Window>(this._window);
                Assert.IsNotNull<HwndSource>(this._hwndSource);
            }
            this._haveAddedButtons = false;
            if (!attached && this._hwndSource == null)
                return;
            this._UpdateOverlay(attached);
            this._UpdateProgressState(attached);
            this._UpdateProgressValue(attached);
            this._UpdateTooltip(attached);
            this._UpdateThumbnailClipping(attached);
            this._UpdateThumbButtons(attached);
            if (attached)
                return;
            this._hwndSource = (HwndSource)null;
        }

        private void _DetachWindow()
        {
            Assert.IsNotNull<Window>(this._window);
            this._window.SourceInitialized -= new EventHandler(this._OnWindowSourceInitialized);
            this._isAttached = false;
            this._OnIsAttachedChanged(false);
            this._window = (Window)null;
        }

        private HRESULT _UpdateOverlay(bool attached)
        {
            ImageSource overlay = this.Overlay;
            if (overlay == null || !attached)
                return this._taskbarList.SetOverlayIcon(this._hwndSource.Handle, IntPtr.Zero, (string)null);
            IntPtr hicon = IntPtr.Zero;
            try
            {
                hicon = this._GetHICONFromImageSource(overlay, this._overlaySize);
                return this._taskbarList.SetOverlayIcon(this._hwndSource.Handle, hicon, (string)null);
            }
            finally
            {
                Utility.SafeDestroyIcon(ref hicon);
            }
        }

        private HRESULT _UpdateTooltip(bool attached)
        {
            string pszTip = this.Description ?? "";
            if (!attached)
                pszTip = "";
            return this._taskbarList.SetThumbnailTooltip(this._hwndSource.Handle, pszTip);
        }

        private HRESULT _UpdateProgressValue(bool attached)
        {
            if (!attached || this.ProgressState == TaskbarItemProgressState.None || this.ProgressState == TaskbarItemProgressState.Indeterminate)
                return HRESULT.S_OK;
            Assert.BoundedDoubleInc(0.0, this.ProgressValue, 1.0);
            return this._taskbarList.SetProgressValue(this._hwndSource.Handle, (ulong)(this.ProgressValue * 1000.0), 1000UL);
        }

        private HRESULT _UpdateProgressState(bool attached)
        {
            TaskbarItemProgressState progressState = this.ProgressState;
            TBPF tbpFlags = TBPF.NOPROGRESS;
            if (attached)
            {
                switch (progressState)
                {
                    case TaskbarItemProgressState.None:
                        tbpFlags = TBPF.NOPROGRESS;
                        break;
                    case TaskbarItemProgressState.Indeterminate:
                        tbpFlags = TBPF.INDETERMINATE;
                        break;
                    case TaskbarItemProgressState.Normal:
                        tbpFlags = TBPF.NORMAL;
                        break;
                    case TaskbarItemProgressState.Error:
                        tbpFlags = TBPF.ERROR;
                        break;
                    case TaskbarItemProgressState.Paused:
                        tbpFlags = TBPF.PAUSED;
                        break;
                    default:
                        Assert.Fail();
                        tbpFlags = TBPF.NOPROGRESS;
                        break;
                }
            }
            HRESULT hresult = this._taskbarList.SetProgressState(this._hwndSource.Handle, tbpFlags);
            if (hresult.Succeeded)
                hresult = this._UpdateProgressValue(attached);
            return hresult;
        }

        private HRESULT _UpdateThumbnailClipping(bool attached)
        {
            Assert.IsNotNull<Window>(this._window);
            RefRECT prcClip = (RefRECT)null;
            if (attached && this.ThumbnailClipMargin != TaskbarItemInfo._EmptyThickness)
            {
                Thickness thumbnailClipMargin = this.ThumbnailClipMargin;
                RECT clientRect = NativeMethods.GetClientRect(this._hwndSource.Handle);
                Rect logical = DpiHelper.DeviceRectToLogical(new Rect((double)clientRect.Left, (double)clientRect.Top, (double)clientRect.Width, (double)clientRect.Height));
                if (thumbnailClipMargin.Left + thumbnailClipMargin.Right >= logical.Width || thumbnailClipMargin.Top + thumbnailClipMargin.Bottom >= logical.Height)
                {
                    prcClip = new RefRECT(0, 0, 0, 0);
                }
                else
                {
                    Rect device = DpiHelper.LogicalRectToDevice(new Rect(thumbnailClipMargin.Left, thumbnailClipMargin.Top, logical.Width - thumbnailClipMargin.Left - thumbnailClipMargin.Right, logical.Height - thumbnailClipMargin.Top - thumbnailClipMargin.Bottom));
                    prcClip = new RefRECT((int)device.Left, (int)device.Top, (int)device.Right, (int)device.Bottom);
                }
            }
            HRESULT hresult = this._taskbarList.SetThumbnailClip(this._hwndSource.Handle, prcClip);
            Assert.IsTrue(hresult.Succeeded);
            return hresult;
        }

        private HRESULT _RegisterThumbButtons()
        {
            HRESULT hresult = HRESULT.S_OK;
            if (!this._haveAddedButtons)
            {
                THUMBBUTTON[] pButtons = new THUMBBUTTON[7];
                for (int index = 0; index < 7; ++index)
                    pButtons[index] = new THUMBBUTTON()
                    {
                        iId = (uint)index,
                        dwFlags = THBF.DISABLED | THBF.NOBACKGROUND | THBF.HIDDEN,
                        dwMask = THB.ICON | THB.TOOLTIP | THB.FLAGS
                    };
                hresult = this._taskbarList.ThumbBarAddButtons(this._hwndSource.Handle, (uint)pButtons.Length, pButtons);
                if (hresult == HRESULT.E_INVALIDARG)
                    hresult = HRESULT.S_FALSE;
                this._haveAddedButtons = hresult.Succeeded;
            }
            return hresult;
        }

        private HRESULT _UpdateThumbButtons(bool attached)
        {
            THUMBBUTTON[] pButtons = new THUMBBUTTON[7];
            HRESULT hresult = this._RegisterThumbButtons();
            if (hresult.Failed)
                return hresult;
            ThumbButtonInfoCollection thumbButtonInfos = this.ThumbButtonInfos;
            try
            {
                uint num = 0;
                if (attached && thumbButtonInfos != null)
                {
                    foreach (ThumbButtonInfo thumbButtonInfo in (FreezableCollection<ThumbButtonInfo>)thumbButtonInfos)
                    {
                        THUMBBUTTON thumbbutton = new THUMBBUTTON()
                        {
                            iId = num,
                            dwMask = THB.ICON | THB.TOOLTIP | THB.FLAGS
                        };
                        switch (thumbButtonInfo.Visibility)
                        {
                            case Visibility.Hidden:
                                thumbbutton.dwFlags = THBF.DISABLED | THBF.NOBACKGROUND;
                                thumbbutton.hIcon = IntPtr.Zero;
                                break;
                            case Visibility.Collapsed:
                                thumbbutton.dwFlags = THBF.HIDDEN;
                                break;
                            default:
                                thumbbutton.szTip = thumbButtonInfo.Description ?? "";
                                thumbbutton.hIcon = this._GetHICONFromImageSource(thumbButtonInfo.ImageSource, this._overlaySize);
                                if (!thumbButtonInfo.IsBackgroundVisible)
                                    thumbbutton.dwFlags |= THBF.NOBACKGROUND;
                                if (!thumbButtonInfo.IsEnabled)
                                    thumbbutton.dwFlags |= THBF.DISABLED;
                                else
                                    thumbbutton.dwFlags |= THBF.ENABLED;
                                if (!thumbButtonInfo.IsInteractive)
                                    thumbbutton.dwFlags |= THBF.NONINTERACTIVE;
                                if (thumbButtonInfo.DismissWhenClicked)
                                {
                                    thumbbutton.dwFlags |= THBF.DISMISSONCLICK;
                                    break;
                                }
                                break;
                        }
                        pButtons[(int)num] = thumbbutton;
                        ++num;
                        if (num == 7U)
                            break;
                    }
                }
                for (; num < 7U; ++num)
                    pButtons[(int)num] = new THUMBBUTTON()
                    {
                        iId = num,
                        dwFlags = THBF.DISABLED | THBF.NOBACKGROUND | THBF.HIDDEN,
                        dwMask = THB.ICON | THB.TOOLTIP | THB.FLAGS
                    };
                return this._taskbarList.ThumbBarUpdateButtons(this._hwndSource.Handle, (uint)pButtons.Length, pButtons);
            }
            finally
            {
                foreach (THUMBBUTTON thumbbutton in pButtons)
                {
                    IntPtr hIcon = thumbbutton.hIcon;
                    if (IntPtr.Zero != hIcon)
                        Utility.SafeDestroyIcon(ref hIcon);
                }
            }
        }
    }
}
