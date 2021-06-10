using System;
using System.ComponentModel;
using ProcessNET.Native.Types;
using ProcessNET.Utilities;

namespace ProcessNET.Memory
{
    /// <summary>
    ///     Class representing an allocated memory in a remote process.
    /// </summary>
    public class AllocatedMemory : MemoryRegion, IAllocatedMemory
    {
        private readonly System.Diagnostics.Process process = null;
        /// <summary>
        ///     Initializes a new instance of the <see cref="AllocatedMemory" /> class.
        /// </summary>
        /// <param name="processPlus">The reference of the <see cref="IProcess" /> object.</param>
        /// <param name="name"></param>
        /// <param name="size">The size of the allocated memory.</param>
        /// <param name="protection">The protection of the allocated memory.</param>
        /// <param name="mustBeDisposed">The allocated memory will be released when the finalizer collects the object.</param>
        public AllocatedMemory(IProcess processPlus, string name, int size,
            MemoryProtectionFlags protection = MemoryProtectionFlags.ExecuteReadWrite,
            bool mustBeDisposed = true)
            : base(processPlus, MemoryHelper.Allocate(processPlus.Handle, size, protection))
        {
            process = processPlus.Native;
            // Set local vars
            Identifier = name;
            MustBeDisposed = mustBeDisposed;
            IsDisposed = false;
            Size = size;
        }

        /// <summary>
        ///     Gets a value indicating whether the element is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the element must be disposed when the Garbage Collector collects the object.
        /// </summary>
        public bool MustBeDisposed { get; set; }

        public bool IsAllocated => !IsDisposed;
        public int Size { get; }
        public string Identifier { get; }

        /// <summary>
        ///     Releases all resources used by the <see cref="AllocatedMemory" /> object.
        /// </summary>
        /// <remarks>Don't use the IDisposable pattern because the class is sealed.</remarks>
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            // Set the flag to true
            IsDisposed = true;
            if (!process.HasExited)
            {
                try
                {
                    // Release the allocated memory
                    Release();
                }
                catch (Win32Exception ex)
                {
                    // Rethrow the exception but with the region's name.
                    throw new Win32Exception($"Allocated memory: {Identifier} in process id {process.Id}, {ex.Message}");
                }
            }
            // Remove this object from the collection of allocated memory
            Process.MemoryFactory.Deallocate(this);
            // Remove the pointer
            BaseAddress = IntPtr.Zero;
            // Avoid the finalizer 
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///     Frees resources and perform other cleanup operations before it is reclaimed by garbage collection.
        /// </summary>
        ~AllocatedMemory()
        {
            if (MustBeDisposed)
                Dispose();
        }
    }
}