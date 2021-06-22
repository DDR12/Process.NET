using ProcessNET.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessNET.Memory
{
    /// <summary>
    /// Extension methods for safe handling of objects allocated in the remote/local memory.
    /// </summary>
    public class MemoryResourcesExtensions
    {
        /// <summary>
        /// Safely frees up the allocated memory region.
        /// </summary>
        /// <param name="allocatedMemory">The region to free up.</param>
        /// <returns>True if cleaned successfully, false otherwise.</returns>
        public static bool SafeRelease(IAllocatedMemory allocatedMemory)
        {
            try
            {
                allocatedMemory.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
