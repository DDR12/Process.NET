using System;
using System.Diagnostics;
using ProcessNET.Utilities;

namespace ProcessNET.Extensions
{
    public static class ProcessModuleExtensions
    {
        // Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        public static IntPtr GetProcAddress(this ProcessModule module, string functionName)
        {
            return ModuleHelper.GetProcAddress(module.ModuleName, functionName);
        }

        // Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        public static void FreeLibrary(this ProcessModule module)
        {
            ModuleHelper.FreeLibrary(module.ModuleName);
        }
    }
}
