using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
#if X64
using ADDR = System.UInt64;
#else
using ADDR = System.UInt32;
#endif

namespace ProcessNET.Applied
{
    public class AppliedUtilities
    {
        #region Delegate/Function Registration

        /// <summary>
        /// Registers a function into a delegate. Note: The delegate must provide a proper function signature!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public static T RegisterDelegate<T>(ADDR address) where T : class
        {
            return RegisterDelegate<T>((IntPtr)address);
        }

        /// <summary>
        /// Registers a function into a delegate. Note: The delegate must provide a proper function signature!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public static T RegisterDelegate<T>(IntPtr address) where T : class
        {
#if !NOEXCEPTIONS
            // Make sure delegates are attributed properly!
            if (!HasUFPAttribute(typeof(T)))
            {
                throw new ArgumentException("The delegate does not have proper attributes! It must be adorned with" +
                                            " the System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute, with proper calling conventions" +
                                            " to work properly!");
            }
#endif
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        #endregion

        internal static bool HasAttrib<T>(Type item)
        {
            return item.GetCustomAttributes(typeof(T), true).Length != 0;
        }

        internal static bool HasUFPAttribute(Delegate d)
        {
            return HasUFPAttribute(d.GetType());
        }

        internal static bool HasUFPAttribute(Type t)
        {
            return HasAttrib<UnmanagedFunctionPointerAttribute>(t);
        }
    }
}
