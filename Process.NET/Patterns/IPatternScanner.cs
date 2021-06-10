namespace ProcessNET.Patterns
{
    public interface IPatternScanner
    {
        PatternScanResult Find(IMemoryPattern pattern);
    }
}