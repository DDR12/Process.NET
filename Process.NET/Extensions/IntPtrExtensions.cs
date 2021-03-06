using System;

namespace Process.NET.Extensions
{
    public static class IntPtrExtensions
    {

        public static bool MayBeValid(this IntPtr ptr)
        {
            return ptr.IsInRange((IntPtr)0x10000, (IntPtr)int.MaxValue);
        }

        public static bool IsInRange(this IntPtr address, IntPtr start, IntPtr end)
        {
            var val = (uint)address.ToInt32();
            return (uint)start.ToInt32() <= val && val <= (uint)end.ToInt32();
        }
    }
}
