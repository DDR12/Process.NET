using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ProcessNET.Native;

namespace ProcessNET.Utilities
{
    /// <summary>
    ///     Static core class providing tools for manipulating modules and libraries.
    /// </summary>
    public static class ModuleHelper
    {
        /// <summary>
        ///     Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="moduleName">The module name (not case-sensitive).</param>
        /// <param name="functionName">The function or variable name, or the function's ordinal value.</param>
        /// <returns>The address of the exported function.</returns>
        public static IntPtr GetProcAddress(string moduleName, string functionName)
        {
            // Get the module
            var module =
                System.Diagnostics.Process.GetCurrentProcess()
                    .Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.CurrentCultureIgnoreCase));

            // Check whether there is a module loaded with this name
            if (module == null)
                throw new ArgumentException(
                    $"Couldn't get the module {moduleName} because it doesn't exist in the current process.");

            // Get the function address
            var ret = Kernel32.GetProcAddress(module.BaseAddress, functionName);

            // Check whether the function was found
            if (ret != IntPtr.Zero)
                return ret;

            // Else the function was not found, throws an exception
            throw new Win32Exception($"Couldn't get the function address of {functionName}.");
        }

        /// <summary>
        ///     Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        /// </summary>
        /// <param name="libraryName">The name of the library to free (not case-sensitive).</param>
        public static void FreeLibrary(string libraryName)
        {
            // Get the module
            var module =
                System.Diagnostics.Process.GetCurrentProcess()
                    .Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => m.ModuleName.ToLower() == libraryName.ToLower());

            // Check whether there is a library loaded with this name
            if (module == null)
                throw new ArgumentException(
                    $"Couldn't free the library {libraryName} because it doesn't exist in the current process.");

            // Free the library
            if (!Kernel32.FreeLibrary(module.BaseAddress))
                throw new Win32Exception($"Couldn't free the library {libraryName}.");
        }

        /// <summary>
        ///     Loads the specified module into the address space of the calling process.
        /// </summary>
        /// <param name="libraryPath">
        ///     The name of the module. This can be either a library module (a .dll file) or an executable
        ///     module (an .exe file).
        /// </param>
        /// <returns>A <see cref="ProcessModule" /> corresponding to the loaded library.</returns>
        public static ProcessModule LoadLibrary(string libraryPath)
        {
            // Check whether the file exists
            if (!File.Exists(libraryPath))
                throw new FileNotFoundException(
                    $"Couldn't load the library {libraryPath} because the file doesn't exist.");

            // Load the library
            if (Kernel32.LoadLibrary(libraryPath) == IntPtr.Zero)
                throw new Win32Exception($"Couldn't load the library {libraryPath}.");

            // Enumerate the loaded modules and return the one newly added
            return
                System.Diagnostics.Process.GetCurrentProcess()
                    .Modules.Cast<ProcessModule>()
                    .First(m => m.FileName == libraryPath);
        }


        //public class ModuleEx
        //{
        //    public string FileName { get; set; }
        //}
        //public static ModuleEx[] GetProcessModulesEx(int pid)
        //{
        //    var procPtr = Kernel32.OpenProcess(Native.Types.ProcessAccessFlags.AllAccess, false, pid).DangerousGetHandle();
        //    if (procPtr == IntPtr.Zero)
        //        return new ModuleEx[0];
        //    uint[] modsInt = new uint[1024];
        //    IntPtr[] hMods = new IntPtr[1024];
        //    GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned);
        //    IntPtr pModules = gch.AddrOfPinnedObject();
        //    uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * (hMods.Length));
        //    List<ModuleEx> result = new List<ModuleEx>();
        //    uint cbNeeded = 0;
        //    if(PSAPI.EnumProcessModulesEx(procPtr, pModules, uiSize, out cbNeeded, Native.Types.DwModuleFilterFlag.LIST_MODULES_ALL) == true)
        //    {
        //        int uiTotalNumberOfModules = (int)(cbNeeded / (Marshal.SizeOf(typeof(IntPtr))));
        //        for(int i= 0; i < uiTotalNumberOfModules; i++)
        //        {
        //            StringBuilder sb = new StringBuilder(1024);
        //            PSAPI.GetModuleFileNameEx(procPtr, hMods[i], sb, sb.Capacity);
        //            string module = sb.ToString();
        //            result.Add(new ModuleEx() { FileName = module });
        //        }
        //    }
        //    return result.ToArray();
        //}
    }
}