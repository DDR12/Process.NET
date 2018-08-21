using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Process.NET.Extensions;
using Process.NET.Memory;
using Process.NET.Utilities;

namespace Process.NET.Applied.Detours
{
    /// <summary>
    ///     A manager class to handle function detours, and hooks.
    ///     <remarks>All credits to the GreyMagic library written by Apoc @ www.ownedcore.com</remarks>
    /// </summary>
    public class Detour : IComplexApplied
    {
        /// <summary>
        ///     This var is not used within the detour itself. It is only here
        ///     to keep a reference, to avoid the GC from collecting the delegate instance!
        /// </summary>
        // ReSharper disable once NotAccessedField.Local
        private readonly Delegate _hookDelegate;

        public Detour(Delegate target, Delegate hook, string identifier, IMemory memory,
            bool ignoreRules = false, bool fastCall = true, bool x64 = true)
        {
            ProcessMemory = memory;
            Identifier = identifier;
            IgnoreRules = ignoreRules;

            TargetDelegate = target;
            Target = target.ToFunctionPtr();

            _hookDelegate = hook;
            HookPointer = hook.ToFunctionPtr(); //target

            //Store the original bytes
            Original = new List<byte>();

            //Setup the detour bytes
            New = new List<byte> { 0x48, 0xb8 }; // movabs rax
            var bytes = BitConverter.GetBytes(HookPointer.ToInt64()); // hookptr
            New.AddRange(bytes);
            New.AddRange(new byte[] { 0xff, 0xe0 }); // jmp rax

            Original.AddRange(memory.Read(Target, New.Count));
        }

        public Detour(Delegate target, Delegate hook, string identifier, IMemory memory,
            bool ignoreRules = false, bool fastCall = true)
        {
            ProcessMemory = memory;
            Identifier = identifier;
            IgnoreRules = ignoreRules;

            TargetDelegate = target;
            Target = target.ToFunctionPtr();

            _hookDelegate = hook;
            HookPointer = hook.ToFunctionPtr(); //target

            //Store the orginal bytes
            Original = new List<byte>();
            Original.AddRange(memory.Read(Target, 6));

            //here the mess starts ...
            //-----------------------
            paramCount = target.Method.GetParameters().Length;

            //preparing the stack from fastcall to stdcall
            first = new List<byte>();

            first.Add(0x58);                    // pop eax - store the ret addr

            if (paramCount > 1)
                first.Add(0x52);                // push edx

            if (paramCount > 0)
                first.Add(0x51);                // push ecx

            first.Add(0x50);                    // push eax - retrieve ret addr

            //jump to the hook
            first.Add(0x68);                    // push HookPointer

            var bytes = BitConverter.GetBytes(HookPointer.ToInt32());

            first.AddRange(bytes);
            first.Add(0xC3);                    // ret - jump to the detour handler

            firstPtr = Marshal.AllocHGlobal(first.Count);
            //ProcessMemory.Write(firstPtr, first.ToArray());

            //Setup the detour bytes
            New = new List<byte> { 0x68 };     //push firstPtr
            var bytes2 = IntPtr.Size == 4 ? BitConverter.GetBytes(firstPtr.ToInt32()) :
                BitConverter.GetBytes(firstPtr.ToInt64());
            New.AddRange(bytes2);
            New.Add(0xC3);                     //ret - jump to the first

            //preparing ecx, edx and the stack from stdcall to fastcall
            last = new List<byte>();
            last.Add(0x58);                     // pop eax - store the ret addr

            if (paramCount > 0)
                last.Add(0x59);                 // pop ecx

            if (paramCount > 1)
                last.Add(0x5A);                 // pop edx

            last.Add(0x50);                     // push eax - retrieve ret addr

            //jump to the original function
            last.Add(0x68);                     // push Target

            var bytes3 = BitConverter.GetBytes(HookPointer.ToInt32());

            last.AddRange(bytes3);
            last.Add(0xC3);                     // ret

            lastPtr = Marshal.AllocHGlobal(last.Count);

            //ProcessMemory.Write(lastPtr, last.ToArray());

            //create the func called after the hook
            lastDelegate = Marshal.GetDelegateForFunctionPointer(lastPtr, TargetDelegate.GetType());
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Detour" /> class.
        /// </summary>
        /// <param name="target">The target delegate.</param>
        /// <param name="hook">The hook delegate.</param>
        /// <param name="identifier"></param>
        /// <param name="memory">The <see cref="MemoryPlus" /> instance.</param>
        /// <param name="ignoreRules"></param>
        public Detour(Delegate target, Delegate hook, string identifier, IMemory memory,
            bool ignoreRules = false)
        {
            ProcessMemory = memory;
            Identifier = identifier;
            IgnoreRules = ignoreRules;

            TargetDelegate = target;
            Target = target.ToFunctionPtr();

            _hookDelegate = hook;
            HookPointer = hook.ToFunctionPtr(); //target

            //Store the original bytes
            Original = new List<byte>();
            Original.AddRange(memory.Read(Target, 6));

            //Setup the detour bytes
            New = new List<byte> { 0x68 };

            var bytes = BitConverter.GetBytes(HookPointer.ToInt32());

            New.AddRange(bytes);
            New.Add(0xC3);
        }

        /// <summary>
        ///     The reference of the <see cref="ProcessMemory" /> object.
        /// </summary>
        private IMemory ProcessMemory { get; }

        /// <summary>
        ///     Gets the pointer to be hooked/being hooked.
        /// </summary>
        /// <value>The pointer to be hooked/being hooked.</value>
        public IntPtr HookPointer { get; }

        /// <summary>
        ///     Gets the new bytes.
        /// </summary>
        /// <value>The new bytes.</value>
        public List<byte> New { get; }

        /// <summary>
        ///     Gets the original bytes.
        /// </summary>
        /// <value>The original bytes.</value>
        public List<byte> Original { get; }

        /// <summary>
        ///     Gets the pointer of the target function.
        /// </summary>
        /// <value>The pointer of the target function.</value>
        public IntPtr Target { get; }

        /// <summary>
        ///     Gets the targeted delegate instance.
        /// </summary>
        /// <value>The targeted delegate instance.</value>
        public Delegate TargetDelegate { get; }

        public int paramCount { get; }
        public List<byte> first { get; }
        public IntPtr firstPtr { get; }
        public List<byte> last { get; }
        public IntPtr lastPtr { get; }
        public Delegate lastDelegate { get; }

        /// <summary>
        ///     Get a value indicating if the detour has been disabled due to a running AntiCheat scan
        /// </summary>
        public bool DisabledDueToRules { get; set; }

        /// <summary>
        ///     Geta s value indicating if the detour should never be disabled by the AntiCheat scan logic
        /// </summary>
        public bool IgnoreRules { get; }

        /// <summary>
        ///     States if the detour is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        ///     The name of the detour.
        /// </summary>
        /// <value>The name of the detour.</value>
        public string Identifier { get; }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="Detour" /> is disposed.
        /// </summary>
        public bool IsDisposed { get; internal set; }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="Detour" /> must be disposed when the Garbage Collector collects the
        ///     object.
        /// </summary>
        public bool MustBeDisposed { get; set; } = true;

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. In this
        ///     case, it will disable the <see cref="Detour" /> instance and suppress the finalizer.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            if (IsEnabled)
                Disable();
            if (firstPtr != IntPtr.Zero || lastPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(firstPtr);
                Marshal.FreeHGlobal(lastPtr);
            }
            GC.SuppressFinalize(this);
        }

        public void Enable()
        {
            Enable(false);
        }

        public void Disable()
        {
            Disable(false);
        }

        /// <summary>
        ///     Removes this Detour from memory. (Reverts the bytes back to their originals.)
        /// </summary>
        /// <returns></returns>
        public void Disable(bool disableDueToRules)
        {
            if (IgnoreRules && disableDueToRules)
                return;

            DisabledDueToRules = disableDueToRules;

            ProcessMemory.Write(Target, Original.ToArray());
            IsEnabled = false;
        }

        /// <summary>
        ///     Applies this Detour to memory. (Writes new bytes to memory)
        /// </summary>
        /// <returns></returns>
        public void Enable(bool disableDueToRules)
        {
            if (disableDueToRules && DisabledDueToRules)
            {
                DisabledDueToRules = false;
                ProcessMemory.Write(Target, New.ToArray());
                IsEnabled = true;
            }
            else
            {
                if (DisabledDueToRules)
                    return;

                if (IsEnabled)
                    return;

                ProcessMemory.Write(Target, New.ToArray());
                if (lastPtr != IntPtr.Zero && lastPtr != IntPtr.Zero)
                {
                    ProcessMemory.Write(firstPtr, first.ToArray());
                    ProcessMemory.Write(lastPtr, last.ToArray());
                }
                IsEnabled = true;
            }
        }

        ~Detour()
        {
            if (MustBeDisposed)
                Dispose();
        }

        /// <summary>
        ///     Calls the original function, and returns a return value.
        /// </summary>
        /// <param name="args">
        ///     The arguments to pass. If it is a 'void' argument list,
        ///     you MUST pass 'null'.
        /// </param>
        /// <returns>An object containing the original functions return value.</returns>
        public object CallOriginal(params object[] args)
        {
            Disable();
            object ret;
            if (firstPtr != IntPtr.Zero || lastPtr != IntPtr.Zero)
            {
                ret = lastDelegate.DynamicInvoke(args);
                Enable();
                return ret;
            }
            ret = TargetDelegate.DynamicInvoke(args);
            Enable();
            return ret;
        }
    }
}