using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProcessNET.Patterns
{
    public class DwordPattern : IMemoryPattern
    {
        private readonly byte[] _bytes;
        private readonly string _mask;

        public readonly string PatternText;
        public int SearchStartOffset { get; set; }
        public MemoryPatternType PatternType { get; }
        public PatternScannerAlgorithm Algorithm { get; }

        public DwordPattern(string dwordPattern, int startOffset = 0, PatternScannerAlgorithm algorithm = PatternScannerAlgorithm.Naive)
        {
            this.PatternType = MemoryPatternType.Function;
            this.Algorithm = algorithm;
            this.SearchStartOffset = startOffset;
            PatternText = dwordPattern;
            _bytes = GetBytesFromDwordPattern(dwordPattern);
            _mask = GetMaskFromDwordPattern(dwordPattern);
        }
        public DwordPattern(byte[] pattern, int startOffset = 0, PatternScannerAlgorithm algorithm = PatternScannerAlgorithm.Naive)
        {
            this.PatternType = MemoryPatternType.Function;
            this.Algorithm = algorithm;
            this.SearchStartOffset = startOffset;
            PatternText = string.Join(" ", pattern.Select(o => o.ToString("X2")));
            _bytes = new byte[pattern.Length];
            for (int i = 0; i < _bytes.Length; i++)
                _bytes[i] = pattern[i];
            _mask = new string(Enumerable.Repeat<char>('x', _bytes.Length).ToArray());
        }
        public IList<byte> GetBytes()
        {
            return _bytes;
        }

        public string GetMask()
        {
            return _mask;
        }


        private static string GetMaskFromDwordPattern(string pattern)
        {
            var mask = pattern.Split(' ').Select(s => s.Contains('?') ? "?" : "x");

            return string.Concat(mask);
        }

        private static byte[] GetBytesFromDwordPattern(string pattern)
        {
            return
                pattern.Split(' ')
                    .Select(s => s.Contains('?') ? (byte) 0 : byte.Parse(s, NumberStyles.HexNumber))
                    .ToArray();
        }

        public override string ToString()
        {
            return PatternText;
        }
    }
}