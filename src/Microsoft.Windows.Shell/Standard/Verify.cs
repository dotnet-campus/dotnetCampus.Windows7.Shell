using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Standard
{
    internal static class Verify
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsApartmentState(ApartmentState requiredState, string message)
        {
            if (Thread.CurrentThread.GetApartmentState() != requiredState)
                throw new InvalidOperationException(message);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        [DebuggerStepThrough]
        public static void IsNeitherNullNorEmpty(string value, string name)
        {
            Assert.IsNeitherNullNorEmpty(name);
            if (value == null)
                throw new ArgumentNullException(name, "The parameter can not be either null or empty.");
            if ("" == value)
                throw new ArgumentException("The parameter can not be either null or empty.", name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        [DebuggerStepThrough]
        public static void IsNeitherNullNorWhitespace(string value, string name)
        {
            Assert.IsNeitherNullNorEmpty(name);
            if (value == null)
                throw new ArgumentNullException(name, "The parameter can not be either null or empty or consist only of white space characters.");
            if ("" == value.Trim())
                throw new ArgumentException("The parameter can not be either null or empty or consist only of white space characters.", name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsNotDefault<T>(T obj, string name) where T : struct
        {
            if (default(T).Equals((object)obj))
                throw new ArgumentException("The parameter must not be the default value.", name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsNotNull<T>(T obj, string name) where T : class
        {
            if ((object)obj == null)
                throw new ArgumentNullException(name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsNull<T>(T obj, string name) where T : class
        {
            if ((object)obj != null)
                throw new ArgumentException("The parameter must be null.", name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void PropertyIsNotNull<T>(T obj, string name) where T : class
        {
            if ((object)obj == null)
                throw new InvalidOperationException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "The property {0} cannot be null at this time.", (object)name));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void PropertyIsNull<T>(T obj, string name) where T : class
        {
            if ((object)obj != null)
                throw new InvalidOperationException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "The property {0} must be null at this time.", (object)name));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsTrue(bool statement, string name)
        {
            if (!statement)
                throw new ArgumentException("", name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsTrue(bool statement, string name, string message)
        {
            if (!statement)
                throw new ArgumentException(message, name);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void AreEqual<T>(T expected, T actual, string parameterName, string message)
        {
            if ((object)expected == null)
            {
                if ((object)actual != null && !actual.Equals((object)expected))
                    throw new ArgumentException(message, parameterName);
            }
            else if (!expected.Equals((object)actual))
                throw new ArgumentException(message, parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void AreNotEqual<T>(
          T notExpected,
          T actual,
          string parameterName,
          string message)
        {
            if ((object)notExpected == null)
            {
                if ((object)actual == null || actual.Equals((object)notExpected))
                    throw new ArgumentException(message, parameterName);
            }
            else if (notExpected.Equals((object)actual))
                throw new ArgumentException(message, parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void UriIsAbsolute(Uri uri, string parameterName)
        {
            Verify.IsNotNull<Uri>(uri, parameterName);
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("The URI must be absolute.", parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void BoundedInteger(
          int lowerBoundInclusive,
          int value,
          int upperBoundExclusive,
          string parameterName)
        {
            if (value < lowerBoundInclusive || value >= upperBoundExclusive)
                throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "The integer value must be bounded with [{0}, {1})", (object)lowerBoundInclusive, (object)upperBoundExclusive), parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void BoundedDoubleInc(
          double lowerBoundInclusive,
          double value,
          double upperBoundInclusive,
          string message,
          string parameter)
        {
            if (value < lowerBoundInclusive || value > upperBoundInclusive)
                throw new ArgumentException(message, parameter);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void TypeSupportsInterface(Type type, Type interfaceType, string parameterName)
        {
            Assert.IsNeitherNullNorEmpty(parameterName);
            Verify.IsNotNull<Type>(type, nameof(type));
            Verify.IsNotNull<Type>(interfaceType, nameof(interfaceType));
            if (type.GetInterface(interfaceType.Name) == null)
                throw new ArgumentException("The type of this parameter does not support a required interface", parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void FileExists(string filePath, string parameterName)
        {
            Verify.IsNeitherNullNorEmpty(filePath, parameterName);
            if (!File.Exists(filePath))
                throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "No file exists at \"{0}\"", (object)filePath), parameterName);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        internal static void ImplementsInterface(
          object parameter,
          Type interfaceType,
          string parameterName)
        {
            Assert.IsNotNull<object>(parameter);
            Assert.IsNotNull<Type>(interfaceType);
            Assert.IsTrue(interfaceType.IsInterface);
            bool flag = false;
            foreach (Type type in parameter.GetType().GetInterfaces())
            {
                if (type == interfaceType)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
                throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "The parameter must implement interface {0}.", (object)interfaceType.ToString()), parameterName);
        }
    }
}
