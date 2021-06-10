using System;
using System.Collections.Generic;
using ProcessNET.Native.Types;

namespace ProcessNET.Memory
{
    public interface IMemoryFactory : IDisposable
    {
        IEnumerable<MemoryRegion> Regions { get; }
        IEnumerable<IAllocatedMemory> Allocations { get; }
        IAllocatedMemory this[string name] { get; }

        IAllocatedMemory Allocate(string name, int size,
            MemoryProtectionFlags protection = MemoryProtectionFlags.ExecuteReadWrite,
            bool mustBeDisposed = true);

        void Deallocate(IAllocatedMemory allocation);
    }
}