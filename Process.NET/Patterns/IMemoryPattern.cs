using System.Collections.Generic;

namespace ProcessNET.Patterns
{
    public interface IMemoryPattern
    {
        int SearchStartOffset { get; }
        MemoryPatternType PatternType { get; }
        PatternScannerAlgorithm Algorithm { get; }
        IList<byte> GetBytes();
        string GetMask();
    }
}