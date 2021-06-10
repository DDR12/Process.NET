using System;
using System.Collections.Concurrent;
using System.Linq;
using ProcessNET.Memory;
using ProcessNET.Modules;
using ProcessNET.Utilities;

namespace ProcessNET.Patterns
{
    public class PatternScanner : IPatternScanner
    {
        private class CachedPatternScanResult
        {
            public int Offset { get; set; }
            public string ModuleName { get; set; }
        }

        static ConcurrentDictionary<string, CachedPatternScanResult> cache = null;

        private readonly IProcessModule _module;
        public byte[] Data { get; }

        public PatternScanner(IProcessModule module)
        {
            _module = module;
            Data = module.Read(0, _module.Size);
        }
        static PatternScanner()
        {
            cache = new ConcurrentDictionary<string, CachedPatternScanResult>();
        }


        public PatternScanResult Find(IMemoryPattern pattern)
        {
            if(cache.TryGetValue(GetPatternCacheKey(pattern), out CachedPatternScanResult cachedResult))
            {
                return new PatternScanResult()
                {
                    BaseAddress = _module.BaseAddress + cachedResult.Offset,
                    ReadAddress = _module.BaseAddress + cachedResult.Offset,
                    Offset = cachedResult.Offset,
                    Found = true,
                };
            }

            return pattern.PatternType == MemoryPatternType.Function
                ? FindFunctionPattern(pattern)
                : FindDataPattern(pattern);
        }
      
      
        private int GetOffset(IMemoryPattern pattern)
        {
            switch (pattern.Algorithm)
            {
                case PatternScannerAlgorithm.Naive:
                    return StringSearching.Naive.GetIndexOf(pattern, Data, _module, pattern.SearchStartOffset);
                case PatternScannerAlgorithm.BoyerMooreHorspool:
                    return StringSearching.BoyerMooreHorspool.IndexOf(Data, pattern.GetBytes().ToArray(), pattern.SearchStartOffset);
                default:
                    throw new NotImplementedException($"Unknown search algorithm, please implement it, {pattern.Algorithm}.");
            }
        }
        private PatternScanResult FindFunctionPattern(IMemoryPattern pattern)
        {
            int offset = GetOffset(pattern);
            if (offset < 0)
                return PatternScanResult.NotFound;
            PatternScanResult result = new PatternScanResult
            {
                BaseAddress = _module.BaseAddress + offset,
                ReadAddress = _module.BaseAddress + offset,
                Offset = offset,
                Found = true
            };
            CachePatternScanResult(pattern, result);
            return result;
        }
        private PatternScanResult FindDataPattern(IMemoryPattern pattern)
        {
            int offset = GetOffset(pattern);
            if (offset < 0)
                return PatternScanResult.NotFound;

            var result = new PatternScanResult();
            // If this area is reached, the pattern has been found.
            result.Found = true;
            result.ReadAddress = _module.Read<IntPtr>(offset);
            result.BaseAddress = new IntPtr(result.ReadAddress.ToInt64() - _module.BaseAddress.ToInt64());
            result.Offset = offset;
            CachePatternScanResult(pattern, result);
            return result;
        }

        private string GetPatternCacheKey(IMemoryPattern pattern)
        {
            return $"{pattern.ToString()}_{_module.Name}_{_module.Path}_{pattern.SearchStartOffset}";
        }
        private void CachePatternScanResult(IMemoryPattern pattern, PatternScanResult result)
        {
            if (!result.Found)
                return;
            string key = GetPatternCacheKey(pattern);
            CachedPatternScanResult cachedPatternScanResult = new CachedPatternScanResult()
            {
                ModuleName = _module.Name,
                Offset = result.Offset,
            };

            cache.AddOrUpdate(key, oldKey =>
            {
                return cachedPatternScanResult;
            }, (oldKey, oldValue) =>
            {
                return cachedPatternScanResult;
            });
        }
    }
}