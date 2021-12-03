using Standard;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace Microsoft.Windows.Shell
{
    [ContentProperty("JumpItems")]
    public sealed class JumpList : ISupportInitialize
    {
        private static readonly object s_lock = new object();
        private static readonly Dictionary<Application, JumpList> s_applicationMap = new Dictionary<Application, JumpList>();
        private Application _application;
        private bool? _initializing;
        private List<JumpItem> _jumpItems;
        private static readonly string _FullName = NativeMethods.GetModuleFileName(IntPtr.Zero);

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static JumpList()
        {
        }

        public static void AddToRecentCategory(string itemPath)
        {
            Verify.FileExists(itemPath, nameof(itemPath));
            itemPath = Path.GetFullPath(itemPath);
            NativeMethods.SHAddToRecentDocs(itemPath);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static void AddToRecentCategory(JumpPath jumpPath)
        {
            Verify.IsNotNull<JumpPath>(jumpPath, nameof(jumpPath));
            JumpList.AddToRecentCategory(jumpPath.Path);
        }

        public static void AddToRecentCategory(JumpTask jumpTask)
        {
            Verify.IsNotNull<JumpTask>(jumpTask, nameof(jumpTask));
            if (!Utility.IsOSWindows7OrNewer)
                return;
            IShellLinkW linkFromJumpTask = JumpList.CreateLinkFromJumpTask(jumpTask, false);
            try
            {
                if (linkFromJumpTask != null)
                    NativeMethods.SHAddToRecentDocs(linkFromJumpTask);
            }
            finally
            {
                Utility.SafeRelease<IShellLinkW>(ref linkFromJumpTask);
            }
        }

        public static void SetJumpList(Application application, JumpList value)
        {
            Verify.IsNotNull<Application>(application, nameof(application));
            lock (JumpList.s_lock)
            {
                JumpList jumpList;
                if (JumpList.s_applicationMap.TryGetValue(application, out jumpList) && jumpList != null)
                    jumpList._application = (Application)null;
                JumpList.s_applicationMap[application] = value;
                if (value != null)
                    value._application = application;
            }
            value?.ApplyFromApplication();
        }

        public static JumpList GetJumpList(Application application)
        {
            Verify.IsNotNull<Application>(application, nameof(application));
            JumpList jumpList;
            JumpList.s_applicationMap.TryGetValue(application, out jumpList);
            return jumpList;
        }

        public JumpList()
          : this((IEnumerable<JumpItem>)null, false, false)
        {
            this._initializing = new bool?();
        }

        public JumpList(IEnumerable<JumpItem> items, bool showFrequent, bool showRecent)
        {
            this._jumpItems = items == null ? new List<JumpItem>() : new List<JumpItem>(items);
            this.ShowFrequentCategory = showFrequent;
            this.ShowRecentCategory = showRecent;
            this._initializing = new bool?(false);
        }

        public bool ShowFrequentCategory { get; set; }

        public bool ShowRecentCategory { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<JumpItem> JumpItems => this._jumpItems;

        private bool _IsUnmodified => !this._initializing.HasValue && this.JumpItems.Count == 0 && !this.ShowRecentCategory && !this.ShowFrequentCategory;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginInit")]
        public void BeginInit()
        {
            if (!this._IsUnmodified)
                throw new InvalidOperationException("Calls to BeginInit cannot be nested.");
            this._initializing = new bool?(true);
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EndInit")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginInit")]
        public void EndInit()
        {
            bool? initializing = this._initializing;
            bool flag = true;
            if (!(initializing.GetValueOrDefault() == flag & initializing.HasValue))
                throw new NotSupportedException("Can't call EndInit without first calling BeginInit.");
            this._initializing = new bool?(false);
            this.ApplyFromApplication();
        }

        private static string _RuntimeId
        {
            get
            {
                string AppID;
                HRESULT hresult = NativeMethods.GetCurrentProcessExplicitAppUserModelID(out AppID);
                if (hresult == HRESULT.E_FAIL)
                {
                    hresult = HRESULT.S_OK;
                    AppID = (string)null;
                }
                hresult.ThrowIfFailed();
                return AppID;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "JumpList")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EndInit")]
        public void Apply()
        {
            bool? initializing = this._initializing;
            bool flag = true;
            if (initializing.GetValueOrDefault() == flag & initializing.HasValue)
                throw new InvalidOperationException("The JumpList can't be applied until EndInit has been called.");
            this._initializing = new bool?(false);
            this._ApplyList();
        }

        private void ApplyFromApplication()
        {
            bool? initializing1 = this._initializing;
            bool flag1 = true;
            if (!(initializing1.GetValueOrDefault() == flag1 & initializing1.HasValue) && !this._IsUnmodified)
                this._initializing = new bool?(false);
            int num;
            if (this._application == Application.Current)
            {
                bool? initializing2 = this._initializing;
                bool flag2 = false;
                num = initializing2.GetValueOrDefault() == flag2 & initializing2.HasValue ? 1 : 0;
            }
            else
                num = 0;
            if (num == 0)
                return;
            this._ApplyList();
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "JumpLists")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Standard.Verify.IsApartmentState(System.Threading.ApartmentState,System.String)")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void _ApplyList()
        {
            bool? initializing = this._initializing;
            bool flag = false;
            Debug.Assert(initializing.GetValueOrDefault() == flag & initializing.HasValue);
            Verify.IsApartmentState(ApartmentState.STA, "JumpLists can only be effected on STA threads.");
            if (!Utility.IsOSWindows7OrNewer)
            {
                this.RejectEverything();
            }
            else
            {
                List<JumpItem> successList;
                List<JumpList._RejectedJumpItemPair> rejectedList;
                List<JumpList._ShellObjectPair> removedList;
                try
                {
                    this._BuildShellLists(out successList, out rejectedList, out removedList);
                }
                catch (Exception ex)
                {
                    Standard.Assert.Fail();
                    this.RejectEverything();
                    return;
                }
                this._jumpItems = successList;
                EventHandler<JumpItemsRejectedEventArgs> jumpItemsRejected = this.JumpItemsRejected;
                EventHandler<JumpItemsRemovedEventArgs> itemsRemovedByUser = this.JumpItemsRemovedByUser;
                if (rejectedList.Count > 0 && jumpItemsRejected != null)
                {
                    List<JumpItem> jumpItemList = new List<JumpItem>(rejectedList.Count);
                    List<JumpItemRejectionReason> itemRejectionReasonList = new List<JumpItemRejectionReason>(rejectedList.Count);
                    foreach (JumpList._RejectedJumpItemPair rejectedJumpItemPair in rejectedList)
                    {
                        jumpItemList.Add(rejectedJumpItemPair.JumpItem);
                        itemRejectionReasonList.Add(rejectedJumpItemPair.Reason);
                    }
                    jumpItemsRejected((object)this, new JumpItemsRejectedEventArgs((IList<JumpItem>)jumpItemList, (IList<JumpItemRejectionReason>)itemRejectionReasonList));
                }
                if (removedList.Count <= 0 || itemsRemovedByUser == null)
                    return;
                List<JumpItem> jumpItemList1 = new List<JumpItem>(removedList.Count);
                foreach (JumpList._ShellObjectPair shellObjectPair in removedList)
                {
                    if (shellObjectPair.JumpItem != null)
                        jumpItemList1.Add(shellObjectPair.JumpItem);
                }
                if (jumpItemList1.Count > 0)
                    itemsRemovedByUser((object)this, new JumpItemsRemovedEventArgs((IList<JumpItem>)jumpItemList1));
            }
        }

        private void _BuildShellLists(
          out List<JumpItem> successList,
          out List<JumpList._RejectedJumpItemPair> rejectedList,
          out List<JumpList._ShellObjectPair> removedList)
        {
            List<List<JumpList._ShellObjectPair>> shellObjectPairListList = (List<List<JumpList._ShellObjectPair>>)null;
            removedList = (List<JumpList._ShellObjectPair>)null;
            ICustomDestinationList instance = CLSID.CoCreateInstance<ICustomDestinationList>("77f10cf0-3db5-4966-b520-b7c54fd35ed6");
            try
            {
                string runtimeId = JumpList._RuntimeId;
                if (!string.IsNullOrEmpty(runtimeId))
                    instance.SetAppID(runtimeId);
                Guid riid = new Guid("92CA9DCD-5622-4bba-A805-5E9F541BD8C9");
                IObjectArray shellObjects = (IObjectArray)instance.BeginList(out uint _, ref riid);
                removedList = JumpList.GenerateJumpItems(shellObjects);
                successList = new List<JumpItem>(this.JumpItems.Count);
                rejectedList = new List<JumpList._RejectedJumpItemPair>(this.JumpItems.Count);
                shellObjectPairListList = new List<List<JumpList._ShellObjectPair>>()
        {
          new List<JumpList._ShellObjectPair>()
        };
                foreach (JumpItem jumpItem in this.JumpItems)
                {
                    if (jumpItem == null)
                    {
                        rejectedList.Add(new JumpList._RejectedJumpItemPair()
                        {
                            JumpItem = jumpItem,
                            Reason = JumpItemRejectionReason.InvalidItem
                        });
                    }
                    else
                    {
                        object comObject = (object)null;
                        try
                        {
                            comObject = JumpList.GetShellObjectForJumpItem(jumpItem);
                            if (comObject == null)
                                rejectedList.Add(new JumpList._RejectedJumpItemPair()
                                {
                                    Reason = JumpItemRejectionReason.InvalidItem,
                                    JumpItem = jumpItem
                                });
                            else if (JumpList.ListContainsShellObject(removedList, comObject))
                            {
                                rejectedList.Add(new JumpList._RejectedJumpItemPair()
                                {
                                    Reason = JumpItemRejectionReason.RemovedByUser,
                                    JumpItem = jumpItem
                                });
                            }
                            else
                            {
                                JumpList._ShellObjectPair shellObjectPair = new JumpList._ShellObjectPair()
                                {
                                    JumpItem = jumpItem,
                                    ShellObject = comObject
                                };
                                if (string.IsNullOrEmpty(jumpItem.CustomCategory))
                                {
                                    shellObjectPairListList[0].Add(shellObjectPair);
                                }
                                else
                                {
                                    bool flag = false;
                                    foreach (List<JumpList._ShellObjectPair> shellObjectPairList in shellObjectPairListList)
                                    {
                                        if (shellObjectPairList.Count > 0 && shellObjectPairList[0].JumpItem.CustomCategory == jumpItem.CustomCategory)
                                        {
                                            shellObjectPairList.Add(shellObjectPair);
                                            flag = true;
                                            break;
                                        }
                                    }
                                    if (!flag)
                                        shellObjectPairListList.Add(new List<JumpList._ShellObjectPair>()
                    {
                      shellObjectPair
                    });
                                }
                                comObject = (object)null;
                            }
                        }
                        finally
                        {
                            Utility.SafeRelease<object>(ref comObject);
                        }
                    }
                }
                shellObjectPairListList.Reverse();
                if (this.ShowFrequentCategory)
                    instance.AppendKnownCategory(KDC.FREQUENT);
                if (this.ShowRecentCategory)
                    instance.AppendKnownCategory(KDC.RECENT);
                foreach (List<JumpList._ShellObjectPair> jumpItems in shellObjectPairListList)
                {
                    if (jumpItems.Count > 0)
                    {
                        string customCategory = jumpItems[0].JumpItem.CustomCategory;
                        JumpList.AddCategory(instance, customCategory, jumpItems, successList, rejectedList);
                    }
                }
                instance.CommitList();
                successList.Reverse();
            }
            finally
            {
                Utility.SafeRelease<ICustomDestinationList>(ref instance);
                if (shellObjectPairListList != null)
                {
                    foreach (List<JumpList._ShellObjectPair> list in shellObjectPairListList)
                        JumpList._ShellObjectPair.ReleaseShellObjects(list);
                }
                JumpList._ShellObjectPair.ReleaseShellObjects(removedList);
            }
        }

        private static bool ListContainsShellObject(
          List<JumpList._ShellObjectPair> removedList,
          object shellObject)
        {
            Debug.Assert(removedList != null);
            Debug.Assert(shellObject != null);
            if (removedList.Count == 0)
                return false;
            switch (shellObject)
            {
                case IShellItem shellItem:
                    foreach (JumpList._ShellObjectPair removed in removedList)
                    {
                        if (removed.ShellObject is IShellItem shellObject4 && shellItem.Compare(shellObject4, SICHINT.CANONICAL | SICHINT.TEST_FILESYSPATH_IF_NOT_EQUAL) == 0)
                            return true;
                    }
                    return false;
                case IShellLinkW shellLink:
                    foreach (JumpList._ShellObjectPair removed in removedList)
                    {
                        if (removed.ShellObject is IShellLinkW shellObject5 && JumpList.ShellLinkToString(shellObject5) == JumpList.ShellLinkToString(shellLink))
                            return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static object GetShellObjectForJumpItem(JumpItem jumpItem)
        {
            JumpPath jumpPath = jumpItem as JumpPath;
            JumpTask jumpTask = jumpItem as JumpTask;
            if (jumpPath != null)
                return (object)JumpList.CreateItemFromJumpPath(jumpPath);
            if (jumpTask != null)
                return (object)JumpList.CreateLinkFromJumpTask(jumpTask, true);
            Debug.Assert(false);
            return (object)null;
        }

        private static List<JumpList._ShellObjectPair> GenerateJumpItems(
          IObjectArray shellObjects)
        {
            Debug.Assert(shellObjects != null);
            List<JumpList._ShellObjectPair> shellObjectPairList = new List<JumpList._ShellObjectPair>();
            Guid riid = new Guid("00000000-0000-0000-C000-000000000046");
            uint count = shellObjects.GetCount();
            for (uint uiIndex = 0; uiIndex < count; ++uiIndex)
            {
                object at = shellObjects.GetAt(uiIndex, ref riid);
                JumpItem jumpItem = (JumpItem)null;
                try
                {
                    jumpItem = JumpList.GetJumpItemForShellObject(at);
                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException || ex is SEHException)
                        throw;
                }
                shellObjectPairList.Add(new JumpList._ShellObjectPair()
                {
                    ShellObject = at,
                    JumpItem = jumpItem
                });
            }
            return shellObjectPairList;
        }

        private static void AddCategory(
          ICustomDestinationList cdl,
          string category,
          List<JumpList._ShellObjectPair> jumpItems,
          List<JumpItem> successList,
          List<JumpList._RejectedJumpItemPair> rejectionList)
        {
            JumpList.AddCategory(cdl, category, jumpItems, successList, rejectionList, true);
        }

        private static void AddCategory(
          ICustomDestinationList cdl,
          string category,
          List<JumpList._ShellObjectPair> jumpItems,
          List<JumpItem> successList,
          List<JumpList._RejectedJumpItemPair> rejectionList,
          bool isHeterogenous)
        {
            Debug.Assert((uint)jumpItems.Count > 0U);
            Debug.Assert(cdl != null);
            IObjectCollection instance = (IObjectCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("2d3468c1-36a7-43b6-ac24-d3f02fd9607a")));
            foreach (JumpList._ShellObjectPair jumpItem in jumpItems)
                instance.AddObject(jumpItem.ShellObject);
            HRESULT hresult = !string.IsNullOrEmpty(category) ? cdl.AppendCategory(category, (IObjectArray)instance) : cdl.AddUserTasks((IObjectArray)instance);
            if (hresult.Succeeded)
            {
                int count = jumpItems.Count;
                while (--count >= 0)
                    successList.Add(jumpItems[count].JumpItem);
            }
            else if (isHeterogenous && hresult == HRESULT.DESTS_E_NO_MATCHING_ASSOC_HANDLER)
            {
                Utility.SafeRelease<IObjectCollection>(ref instance);
                List<JumpList._ShellObjectPair> jumpItems1 = new List<JumpList._ShellObjectPair>();
                foreach (JumpList._ShellObjectPair jumpItem in jumpItems)
                {
                    if (jumpItem.JumpItem is JumpPath)
                        rejectionList.Add(new JumpList._RejectedJumpItemPair()
                        {
                            JumpItem = jumpItem.JumpItem,
                            Reason = JumpItemRejectionReason.NoRegisteredHandler
                        });
                    else
                        jumpItems1.Add(jumpItem);
                }
                if (jumpItems1.Count > 0)
                {
                    Debug.Assert(jumpItems.Count != jumpItems1.Count);
                    JumpList.AddCategory(cdl, category, jumpItems1, successList, rejectionList, false);
                }
            }
            else
            {
                Debug.Assert(HRESULT.DESTS_E_NO_MATCHING_ASSOC_HANDLER != hresult);
                foreach (JumpList._ShellObjectPair jumpItem in jumpItems)
                    rejectionList.Add(new JumpList._RejectedJumpItemPair()
                    {
                        JumpItem = jumpItem.JumpItem,
                        Reason = JumpItemRejectionReason.InvalidItem
                    });
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static IShellLinkW CreateLinkFromJumpTask(
          JumpTask jumpTask,
          bool allowSeparators)
        {
            Debug.Assert(jumpTask != null);
            if (string.IsNullOrEmpty(jumpTask.Title) && (!allowSeparators || !string.IsNullOrEmpty(jumpTask.CustomCategory)))
                return (IShellLinkW)null;
            IShellLinkW comObject = (IShellLinkW)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
            try
            {
                string pszFile = JumpList._FullName;
                if (!string.IsNullOrEmpty(jumpTask.ApplicationPath))
                    pszFile = jumpTask.ApplicationPath;
                comObject.SetPath(pszFile);
                if (!string.IsNullOrEmpty(jumpTask.WorkingDirectory))
                    comObject.SetWorkingDirectory(jumpTask.WorkingDirectory);
                if (!string.IsNullOrEmpty(jumpTask.Arguments))
                    comObject.SetArguments(jumpTask.Arguments);
                if (jumpTask.IconResourceIndex != -1)
                {
                    string pszIconPath = JumpList._FullName;
                    if (!string.IsNullOrEmpty(jumpTask.IconResourcePath))
                    {
                        if (jumpTask.IconResourcePath.Length >= 260)
                            return (IShellLinkW)null;
                        pszIconPath = jumpTask.IconResourcePath;
                    }
                    comObject.SetIconLocation(pszIconPath, jumpTask.IconResourceIndex);
                }
                if (!string.IsNullOrEmpty(jumpTask.Description))
                    comObject.SetDescription(jumpTask.Description);
                IPropertyStore propertyStore = (IPropertyStore)comObject;
                using (PROPVARIANT pv = new PROPVARIANT())
                {
                    PKEY pkey1 = new PKEY();
                    PKEY pkey2;
                    if (!string.IsNullOrEmpty(jumpTask.Title))
                    {
                        pv.SetValue(jumpTask.Title);
                        pkey2 = PKEY.Title;
                    }
                    else
                    {
                        pv.SetValue(true);
                        pkey2 = PKEY.AppUserModel_IsDestListSeparator;
                    }
                    propertyStore.SetValue(ref pkey2, pv);
                }
                propertyStore.Commit();
                IShellLinkW shellLinkW = comObject;
                comObject = (IShellLinkW)null;
                return shellLinkW;
            }
            catch (Exception ex)
            {
                return (IShellLinkW)null;
            }
            finally
            {
                Utility.SafeRelease<IShellLinkW>(ref comObject);
            }
        }

        private static IShellItem2 GetShellItemForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return (IShellItem2)null;
            Guid riid = new Guid("7e9fb0d3-919f-4307-ab2e-9b1860310c93");
            object ppv;
            HRESULT hresult = NativeMethods.SHCreateItemFromParsingName(path, (IBindCtx)null, ref riid, out ppv);
            if (hresult == (HRESULT)Win32Error.ERROR_FILE_NOT_FOUND || hresult == (HRESULT)Win32Error.ERROR_PATH_NOT_FOUND)
            {
                hresult = HRESULT.S_OK;
                ppv = (object)null;
            }
            hresult.ThrowIfFailed();
            return (IShellItem2)ppv;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static IShellItem2 CreateItemFromJumpPath(JumpPath jumpPath)
        {
            Debug.Assert(jumpPath != null);
            try
            {
                return JumpList.GetShellItemForPath(Path.GetFullPath(jumpPath.Path));
            }
            catch (Exception ex)
            {
            }
            return (IShellItem2)null;
        }

        private static JumpItem GetJumpItemForShellObject(object shellObject)
        {
            IShellItem2 shellItem2 = shellObject as IShellItem2;
            IShellLinkW shellLinkW = shellObject as IShellLinkW;
            if (shellItem2 != null)
                return (JumpItem)new JumpPath()
                {
                    Path = shellItem2.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING)
                };
            if (shellLinkW != null)
            {
                StringBuilder pszFile1 = new StringBuilder(260);
                shellLinkW.GetPath(pszFile1, pszFile1.Capacity, (WIN32_FIND_DATAW)null, SLGP.RAWPATH);
                StringBuilder pszArgs = new StringBuilder(1024);
                shellLinkW.GetArguments(pszArgs, pszArgs.Capacity);
                StringBuilder pszFile2 = new StringBuilder(1024);
                shellLinkW.GetDescription(pszFile2, pszFile2.Capacity);
                StringBuilder pszIconPath = new StringBuilder(260);
                int piIcon;
                shellLinkW.GetIconLocation(pszIconPath, pszIconPath.Capacity, out piIcon);
                StringBuilder pszDir = new StringBuilder(260);
                shellLinkW.GetWorkingDirectory(pszDir, pszDir.Capacity);
                JumpTask jumpTask = new JumpTask()
                {
                    ApplicationPath = pszFile1.ToString(),
                    Arguments = pszArgs.ToString(),
                    Description = pszFile2.ToString(),
                    IconResourceIndex = piIcon,
                    IconResourcePath = pszIconPath.ToString(),
                    WorkingDirectory = pszDir.ToString()
                };
                PROPVARIANT pv = new PROPVARIANT();
                try
                {
                    IPropertyStore propertyStore = (IPropertyStore)shellLinkW;
                    PKEY title = PKEY.Title;
                    propertyStore.GetValue(ref title, pv);
                    jumpTask.Title = pv.GetValue() ?? "";
                }
                finally
                {
                    pv?.Dispose();
                }
                return (JumpItem)jumpTask;
            }
            Debug.Assert(false);
            return (JumpItem)null;
        }

        private static string ShellLinkToString(IShellLinkW shellLink)
        {
            StringBuilder pszFile = new StringBuilder(260);
            shellLink.GetPath(pszFile, pszFile.Capacity, (WIN32_FIND_DATAW)null, SLGP.RAWPATH);
            string str = (string)null;
            PROPVARIANT pv = new PROPVARIANT();
            try
            {
                IPropertyStore propertyStore = (IPropertyStore)shellLink;
                PKEY title = PKEY.Title;
                propertyStore.GetValue(ref title, pv);
                str = pv.GetValue() ?? "";
            }
            finally
            {
                pv?.Dispose();
            }
            StringBuilder pszArgs = new StringBuilder(1024);
            shellLink.GetArguments(pszArgs, pszArgs.Capacity);
            return pszFile.ToString().ToUpperInvariant() + str.ToUpperInvariant() + pszArgs.ToString();
        }

        private void RejectEverything()
        {
            EventHandler<JumpItemsRejectedEventArgs> jumpItemsRejected = this.JumpItemsRejected;
            if (jumpItemsRejected == null)
            {
                this._jumpItems.Clear();
            }
            else
            {
                if (this._jumpItems.Count <= 0)
                    return;
                List<JumpItemRejectionReason> itemRejectionReasonList = new List<JumpItemRejectionReason>(this.JumpItems.Count);
                for (int index = 0; index < this.JumpItems.Count; ++index)
                    itemRejectionReasonList.Add(JumpItemRejectionReason.InvalidItem);
                JumpItemsRejectedEventArgs e = new JumpItemsRejectedEventArgs((IList<JumpItem>)this.JumpItems, (IList<JumpItemRejectionReason>)itemRejectionReasonList);
                this._jumpItems.Clear();
                jumpItemsRejected((object)this, e);
            }
        }

        public event EventHandler<JumpItemsRejectedEventArgs> JumpItemsRejected;

        public event EventHandler<JumpItemsRemovedEventArgs> JumpItemsRemovedByUser;

        private class _RejectedJumpItemPair
        {
            public JumpItem JumpItem { get; set; }

            public JumpItemRejectionReason Reason { get; set; }
        }

        private class _ShellObjectPair
        {
            public JumpItem JumpItem { get; set; }

            public object ShellObject { get; set; }

            public static void ReleaseShellObjects(List<JumpList._ShellObjectPair> list)
            {
                if (list == null)
                    return;
                foreach (JumpList._ShellObjectPair shellObjectPair in list)
                {
                    object shellObject = shellObjectPair.ShellObject;
                    shellObjectPair.ShellObject = (object)null;
                    Utility.SafeRelease<object>(ref shellObject);
                }
            }
        }
    }
}
