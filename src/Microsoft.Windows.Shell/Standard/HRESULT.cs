using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Standard
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct HRESULT
    {
        [FieldOffset(0)]
        private readonly uint _value;
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT S_OK = new HRESULT(0U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT S_FALSE = new HRESULT(1U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_PENDING = new HRESULT(2147483658U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_NOTIMPL = new HRESULT(2147500033U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_NOINTERFACE = new HRESULT(2147500034U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_POINTER = new HRESULT(2147500035U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_ABORT = new HRESULT(2147500036U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_FAIL = new HRESULT(2147500037U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_UNEXPECTED = new HRESULT(2147549183U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT STG_E_INVALIDFUNCTION = new HRESULT(2147680257U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT REGDB_E_CLASSNOTREG = new HRESULT(2147746132U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT DESTS_E_NO_MATCHING_ASSOC_HANDLER = new HRESULT(2147749635U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT DESTS_E_NORECDOCS = new HRESULT(2147749636U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT DESTS_E_NOTALLCLEARED = new HRESULT(2147749637U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_ACCESSDENIED = new HRESULT(2147942405U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_OUTOFMEMORY = new HRESULT(2147942414U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT E_INVALIDARG = new HRESULT(2147942487U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT INTSAFE_E_ARITHMETIC_OVERFLOW = new HRESULT(2147942934U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT COR_E_OBJECTDISPOSED = new HRESULT(2148734498U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT WC_E_GREATERTHAN = new HRESULT(3222072867U);
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly HRESULT WC_E_SYNTAX = new HRESULT(3222072877U);

        public HRESULT(uint i) => this._value = i;

        public static HRESULT Make(bool severe, Facility facility, int code)
        {
            Assert.Implies(facility != (facility & (Facility)511), facility == Facility.Ese || facility == Facility.WinCodec);
            Assert.AreEqual<int>(code, code & (int)ushort.MaxValue);
            return new HRESULT((uint)((severe ? int.MinValue : 0) | (int)facility << 16 | code));
        }

        public Facility Facility => HRESULT.GetFacility((int)this._value);

        public static Facility GetFacility(int errorCode) => (Facility)(errorCode >> 16 & 8191);

        public int Code => HRESULT.GetCode((int)this._value);

        public static int GetCode(int error) => error & (int)ushort.MaxValue;

        public override string ToString()
        {
            foreach (FieldInfo field in typeof(HRESULT).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (field.FieldType == typeof(HRESULT) && (HRESULT)field.GetValue((object)null) == this)
                    return field.Name;
            }
            if (this.Facility == Facility.Win32)
            {
                foreach (FieldInfo field in typeof(Win32Error).GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    if (field.FieldType == typeof(Win32Error) && (HRESULT)(Win32Error)field.GetValue((object)null) == this)
                        return "HRESULT_FROM_WIN32(" + field.Name + ")";
                }
            }
            return string.Format((IFormatProvider)CultureInfo.InvariantCulture, "0x{0:X8}", (object)this._value);
        }

        public override bool Equals(object obj)
        {
            try
            {
                return (int)((HRESULT)obj)._value == (int)this._value;
            }
            catch (InvalidCastException ex)
            {
                return false;
            }
        }

        public override int GetHashCode() => this._value.GetHashCode();

        public static bool operator ==(HRESULT hrLeft, HRESULT hrRight) => (int)hrLeft._value == (int)hrRight._value;

        public static bool operator !=(HRESULT hrLeft, HRESULT hrRight) => !(hrLeft == hrRight);

        public bool Succeeded => (int)this._value >= 0;

        public bool Failed => (int)this._value < 0;

        public void ThrowIfFailed() => this.ThrowIfFailed((string)null);

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Only recreating Exceptions that were already raised.")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public void ThrowIfFailed(string message)
        {
            if (this.Failed)
            {
                message = !string.IsNullOrEmpty(message) ? message + " (" + this.ToString() + ")" : this.ToString();
                Exception exception = Marshal.GetExceptionForHR((int)this._value, new IntPtr(-1));
                Assert.IsNotNull<Exception>(exception);
                Assert.IsFalse(exception is ArgumentNullException);
                if (exception.GetType() == typeof(COMException))
                {
                    exception = this.Facility == Facility.Win32 ? (Exception)new Win32Exception(this.Code, message) : (Exception)new COMException(message, (int)this._value);
                }
                else
                {
                    ConstructorInfo constructor = exception.GetType().GetConstructor(new Type[1]
                    {
            typeof (string)
                    });
                    if (constructor != null)
                    {
                        exception = constructor.Invoke(new object[1]
                        {
              (object) message
                        }) as Exception;
                        Assert.IsNotNull<Exception>(exception);
                    }
                }
                throw exception;
            }
        }

        public static void ThrowLastError()
        {
            ((HRESULT)Win32Error.GetLastError()).ThrowIfFailed();
            Assert.Fail();
        }
    }
}
