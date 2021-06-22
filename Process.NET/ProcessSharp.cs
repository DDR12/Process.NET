using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using ProcessNET.Extensions;
using ProcessNET.Memory;
using ProcessNET.Modules;
using ProcessNET.Native.Types;
using ProcessNET.Threads;
using ProcessNET.Utilities;
using ProcessNET.Windows;

namespace ProcessNET
{
    /// <summary>
    ///     A class that offsers several tools to interact with a process.
    /// </summary>
    /// <seealso cref="IProcess" />
    public class ProcessSharp : IProcess
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProcessSharp" /> class.
        /// </summary>
        /// <param name="native">The native process.</param>
        /// <param name="type">The type of memory being manipulated.</param>
        public ProcessSharp(System.Diagnostics.Process native, MemoryType type)
        {
            native.EnableRaisingEvents = true;

            native.Exited += (s, e) =>
            {
                ProcessExited?.Invoke(s, e);
                HandleProcessExiting();
            };

            Native = native;

            Handle = MemoryHelper.OpenProcess(ProcessAccessFlags.AllAccess, Native.Id);

            switch (type)
            {
                case MemoryType.Local:
                    Memory = new LocalProcessMemory(Handle);
                    break;
                case MemoryType.Remote:
                    Memory = new ExternalProcessMemory(Handle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            native.ErrorDataReceived += OutputDataReceived;
            native.OutputDataReceived += OutputDataReceived;

            ThreadFactory = new ThreadFactory(this);
            ModuleFactory = new ModuleFactory(this);
            MemoryFactory = new MemoryFactory(this);
            WindowFactory = new WindowFactory(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessSharp"/> class.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <param name="type">The type of memory being manipulated.</param>
        public ProcessSharp(string processName, MemoryType type) : this(ProcessHelper.FromName(processName), type)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessSharp"/> class.
        /// </summary>
        /// <param name="processId">The process id of the process to open with all rights.</param>
        /// <param name="type">The type of memory being manipulated.</param>
        public ProcessSharp(int processId, MemoryType type) : this(ProcessHelper.FromProcessId(processId), type)
        {
        }


        /// <summary>
        /// Raises when the <see cref="ProcessSharp"/> object is disposed.
        /// </summary>
        public event EventHandler OnDispose;

        /// <summary>
        ///     Class for reading and writing memory.
        /// </summary>
        public IMemory Memory { get; set; }

        /// <summary>
        ///     Provide access to the opened process.
        /// </summary>
        public System.Diagnostics.Process Native { get; set; }

        /// <summary>
        ///     The process handle opened with all rights.
        /// </summary>
        public SafeMemoryHandle Handle { get; set; }

        /// <summary>
        ///     Factory for manipulating threads.
        /// </summary>
        public IThreadFactory ThreadFactory { get; set; }

        /// <summary>
        ///     Factory for manipulating modules and libraries.
        /// </summary>
        public IModuleFactory ModuleFactory { get; set; }

        /// <summary>
        ///     Factory for manipulating memory space.
        /// </summary>
        public IMemoryFactory MemoryFactory { get; set; }

        /// <summary>
        ///     Factory for manipulating windows.
        /// </summary>
        public IWindowFactory WindowFactory { get; set; }

        protected string ownerUser = null;
        /// <summary>
        /// Gets the name of the user this process belongs to.
        /// </summary>
        public string OwnerUser
        {
            get
            {
                if(string.IsNullOrWhiteSpace(ownerUser))
                {
                    ownerUser = ProcessHelper.GetProcessUser(Native.Id);
                }
                return ownerUser;
            }
        }


        /// <summary>
        /// Gets the <see cref="IProcessModule"/> with the specified module name.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>IProcessModule.</returns>
        public IProcessModule this[string moduleName] => ModuleFactory[moduleName];

        /// <summary>
        /// Gets the <see cref="IPointer"/> with the specified address.
        /// </summary>
        /// <param name="intPtr">The address the pointer is located at in memory.</param>
        /// <returns>IPointer.</returns>
        public IPointer this[IntPtr intPtr] => new MemoryPointer(this, intPtr);

        protected bool IsDisposed { get; set; }
        protected bool MustBeDisposed { get; set; } = true;



        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public virtual void Dispose()
        {
            //TODO Consider adding a null check here, or a nasty crash deadlock can make it in here occasionally.
            //TODO followup: did the invoke threadsafety characterstics fix the deadlock?
            if (!IsDisposed)
            {
                IsDisposed = true;
                OnDispose?.Invoke(this, EventArgs.Empty);
                ThreadFactory?.Dispose();
                ModuleFactory?.Dispose();
                MemoryFactory?.Dispose();
                WindowFactory?.Dispose();
                Handle?.Close();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///     Handles the process exiting.
        /// </summary>
        /// <remarks>Created 2012-02-15</remarks>
        protected virtual void HandleProcessExiting()
        {
        }
        #region Pointers
        /// <summary>
        /// Calculates the final address a pointer is pointing at in a module, knowing it's base address and offsets.
        /// </summary>
        /// <param name="moduleName">The module name to calculate the pointer at.</param>
        /// <param name="baseAddress">The base address of the pointer.</param>
        /// <param name="offsets">The offsets of the pointer.</param>
        /// <returns>The final address the pointer is pointing at.</returns>
        public IntPtr GetPointerAddress(string moduleName, IntPtr baseAddress, params int[] offsets)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw new ArgumentException($"'{nameof(moduleName)}' cannot be null or whitespace.", nameof(moduleName));
            }
            return GetPointerAddress(this[moduleName], baseAddress, offsets);
        }
        /// <summary>
        /// Calculates the final address a pointer is pointing at in a module, knowing it's base address and offsets.
        /// </summary>
        /// <param name="module">The module to calculate the pointer at.</param>
        /// <param name="baseAddress">The base address of the pointer.</param>
        /// <param name="offsets">The offsets of the pointer.</param>
        /// <returns>The final address the pointer is pointing at.</returns>
        public IntPtr GetPointerAddress(IProcessModule module, IntPtr baseAddress, params int[] offsets)
        {
            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            IntPtr finalBaseAddress;
            if (Memory.Is32Bit)
                finalBaseAddress = module.BaseAddress + baseAddress.ToInt32();
            else
                finalBaseAddress = new IntPtr(module.BaseAddress.ToInt64() + baseAddress.ToInt64());
            return GetPointerAddress(finalBaseAddress, offsets);
        }
        /// <summary>
        /// Calculates the final address a pointer is pointing at, knowing it's base address and offsets.
        /// </summary>
        /// <param name="baseAddress">The base address of the pointer.</param>
        /// <param name="offsets">The offsets of the pointer.</param>
        /// <returns>The final address the pointer is pointing at.</returns>
        public IntPtr GetPointerAddress(IntPtr baseAddress, params int[] offsets)
        {
            if (baseAddress.MayBeValid() == false)
                return IntPtr.Zero;

            IntPtr address = baseAddress;

            if (offsets != null && offsets.Length > 0)
            {
                for(int i = 0; i < offsets.Length; i++)
                {
                    try
                    {
                        address = Memory.Read<IntPtr>(address + offsets[i]);
                    }
                    catch(Exception ex)
                    {
                        return IntPtr.Zero;
                    }
                }
            }
            return address;
        }
        #endregion
        /// <summary>
        ///     Event queue for all listeners interested in ProcessExited events.
        /// </summary>
        public event EventHandler ProcessExited;

        private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Trace.WriteLine(e.Data);
        }

        ~ProcessSharp()
        {
            if (MustBeDisposed)
            {
                Dispose();
            }
        }
    }
}