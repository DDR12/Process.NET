using System;
using System.Runtime.InteropServices;
using System.Text;
using ProcessNET.Native.Types;

namespace ProcessNET.Native
{
    public static class PSAPI
    {
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(
            IntPtr hProcess,
            [Out] IntPtr lphModule,
            UInt32 cb,
            [MarshalAs(UnmanagedType.U4)] out UInt32 lpcbNeeded,
            DwModuleFilterFlag dwff);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleFileNameEx(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            [In][MarshalAs(UnmanagedType.U4)] int nSize);
    }
}