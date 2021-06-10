using ProcessNET.Modules;
using ProcessNET.Patterns;
using System.Linq;

namespace ProcessNET.Utilities
{
    public class StringSearching
    {
        public class Naive
        {
            public static int GetIndexOf(IMemoryPattern pattern, byte[] Data, IProcessModule module, int startOffset = 0)
            {
                var patternData = Data;
                var patternDataLength = patternData.Length;

                for (var offset = startOffset; offset < patternDataLength; offset++)
                {
                    if (
                        pattern.GetMask()
                            .Where((m, b) => m == 'x' && pattern.GetBytes()[b] != patternData[b + offset])
                            .Any())
                        continue;

                    return offset;
                }
                return -1;
            }
        }
        public class BoyerMooreHorspool
        {
            private const byte WildCard = 0x00;

            private static int[] BuildBadCharTable(byte[] pPattern)
            {
                var idx = 0;
                var last = pPattern.Length - 1;
                var badShift = new int[256];

                // Get last wildcard position
                for (idx = last; idx > 0 && pPattern[idx] != WildCard; --idx) ;
                var diff = last - idx;
                if (diff == 0)
                    diff = 1;

                // Prepare shift table
                for (idx = 0; idx <= 255; ++idx)
                    badShift[idx] = diff;
                for (idx = last - diff; idx < last; ++idx)
                    badShift[pPattern[idx]] = last - idx;
                return badShift;
            }

            public static int IndexOf(byte[] buffer, byte[] pattern, int startOffset = 0)
            {
                if (pattern.Length > buffer.Length)
                {
                    return -1;
                }
                var badShift = BuildBadCharTable(pattern);
                var offset = startOffset;
                var position = 0;
                var last = pattern.Length - 1;
                var maxoffset = buffer.Length - pattern.Length;
                while (offset <= maxoffset)
                {
                    for (position = last; (pattern[position] == buffer[position + offset] || pattern[position] == WildCard); position--)
                    {
                        if (position == 0)
                        {
                            return offset;
                        }
                    }
                    offset += badShift[(int)buffer[offset + last]];
                }
                return -1;
            }
        }
    }
}