using System.Runtime.InteropServices;
using System.Text;
using ProcessNET.Native.Types;

namespace ProcessNET.Native
{
    public static class DbgHelp
    {
        private const string DLLName = "dbghelp.dll";
        [DllImport(DLLName, SetLastError = true, PreserveSig = true)]
        private static extern int UnDecorateSymbolName(
                [In] [MarshalAs(UnmanagedType.LPStr)] string DecoratedName,
                [Out] StringBuilder UnDecoratedName,
                [In] [MarshalAs(UnmanagedType.U4)] int UndecoratedLength,
                [In] [MarshalAs(UnmanagedType.U4)] UnDecorateFlags Flags);


        public static string UnDecorateSymbolName(string name, UnDecorateFlags flags)
        {
            StringBuilder undecorated = new StringBuilder(255);
            UnDecorateSymbolName(name, undecorated, undecorated.Capacity, flags);
            return undecorated.ToString();
        }
    }
}