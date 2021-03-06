using System;

namespace ProcessNET.Patterns
{
    public struct PatternScanResult
    {
        public IntPtr ReadAddress { get; set; }
        public IntPtr BaseAddress { get; set; }
        public int Offset { get; set; }
        public bool Found { get; set; }


        public static PatternScanResult NotFound => new PatternScanResult();
    }
}