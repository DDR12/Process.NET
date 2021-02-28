using System;
using System.Collections.Concurrent;
using System.Linq;
using Process.NET.Memory;
using Process.NET.Modules;

namespace Process.NET.Patterns
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

        public PatternScanner(IProcessModule module)
        {
            _module = module;
            Data = module.Read(0, _module.Size);
        }
        static PatternScanner()
        {
            cache = new ConcurrentDictionary<string, CachedPatternScanResult>();
        }

        public byte[] Data { get; }

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
        private PatternScanResult FindFunctionPattern(IMemoryPattern pattern)
        {
            var patternData = Data;
            var patternDataLength = patternData.Length;

            for (var offset = 0; offset < patternDataLength; offset++)
            {
                if (
                    pattern.GetMask()
                        .Where((m, b) => m == 'x' && pattern.GetBytes()[b] != patternData[b + offset])
                        .Any())
                    continue;

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
            return PatternScanResult.NotFound;
        }

        private PatternScanResult FindDataPattern(IMemoryPattern pattern)
        {
            var patternData = Data;
            var patternBytes = pattern.GetBytes();
            var patternMask = pattern.GetMask();

            var result = new PatternScanResult();

            for (var offset = 0; offset < patternData.Length; offset++)
            {
                if (patternMask.Where((m, b) => m == 'x' && patternBytes[b] != patternData[b + offset]).Any())
                    continue;
                // If this area is reached, the pattern has been found.
                result.Found = true;
                result.ReadAddress = _module.Read<IntPtr>(offset + pattern.Offset);
                result.BaseAddress = new IntPtr(result.ReadAddress.ToInt64() - _module.BaseAddress.ToInt64());
                result.Offset = offset;
                CachePatternScanResult(pattern, result);
                return result;
            }
            // If this is reached, the pattern was not found.
            return PatternScanResult.NotFound;
        }

        private string GetPatternCacheKey(IMemoryPattern pattern)
        {
            return $"{pattern.ToString()}_{_module.Name}_{_module.Path}";
        }
    }
}