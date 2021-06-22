using System;
using System.Collections.Concurrent;
using System.Text;
using ProcessNET.Extensions;
using ProcessNET.Native;
using ProcessNET.Native.Types;

namespace ProcessNET.Memory
{
    /// <summary>
    ///     Class for memory editing a process.
    /// </summary>
    /// <seealso cref="IMemory" />
    public abstract class ProcessMemory : IMemory
    {
        /// <summary>
        ///     The open handle to the process which contains the memory of interest,
        /// </summary>
        protected readonly SafeMemoryHandle Handle;

        /// <summary>
        /// Runtime types names based on their addresses.
        /// </summary>
        protected ConcurrentDictionary<IntPtr, string> rttiCache = null;

        protected readonly bool is32Bit = false;
        /// <summary>
        /// Returns the type of the processor architecture of the process.
        /// </summary>
        public bool Is32Bit => is32Bit;
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProcessMemory" /> class.
        /// </summary>
        /// <param name="handle">The open handle to the process which contains the memory of interest.</param>
        protected ProcessMemory(SafeMemoryHandle handle)
        {
            Handle = handle;
            is32Bit = Kernel32.Is32BitProcess(handle.DangerousGetHandle());
            rttiCache = new ConcurrentDictionary<IntPtr, string>();
        }

        /// <summary>
        ///     Writes a set of bytes to memory.
        /// </summary>
        /// <param name="intPtr">The address where the bytes start in memory.</param>
        /// <param name="length">The length of the byte chunk to read from the memory address.</param>
        /// <returns>
        ///     The byte array section read from memory.
        /// </returns>
        public abstract byte[] Read(IntPtr intPtr, int length);
        /// <summary>
        /// Writes a set of bytes to memory.
        /// </summary>
        /// <param name="intPtr">The address where the bytes start in memory.</param>
        /// <param name="buffer">The buffer to read data onto, it's size determines the count of bytes to read.</param>
        public abstract void Read(IntPtr intPtr, byte[] buffer);
        /// <summary>
        ///     Reads a string with a specified encoding from memory.
        /// </summary>
        /// <param name="intPtr">The address where the string is read.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="maxLength">
        ///     The number of maximum bytes to read. The string is automatically cropped at this end ('\0'
        ///     char).
        /// </param>
        /// <returns>The string.</returns>
        public string ReadString(IntPtr intPtr, Encoding encoding, int maxLength = 512)
        {
            var buffer = Read(intPtr, maxLength);
            var ret = encoding.GetString(buffer);
            if (ret.IndexOf('\0') != -1)
                ret = ret.Remove(ret.IndexOf('\0'));
            return ret;
        }

        /// <summary>
        ///     Reads the value of a specified type from memory.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="intPtr">The address where the value is read.</param>
        /// <returns>A value.</returns>
        public abstract T Read<T>(IntPtr intPtr);

        /// <summary>
        ///     Reads an array of a specified type from memory.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="intPtr">The address where the values is read.</param>
        /// <param name="length">The number of cells in the array.</param>
        /// <returns>An array.</returns>
        public T[] Read<T>(IntPtr intPtr, int length)
        {
            var buffer = new T[length];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = Read<T>(intPtr);
            return buffer;
        }

        /// <summary>
        ///     Write an array of bytes in the remote process.
        /// </summary>
        /// <param name="intPtr">The address where the array is written.</param>
        /// <param name="bytesToWrite">The array of bytes to write.</param>
        public abstract int Write(IntPtr intPtr, byte[] bytesToWrite);

        /// <summary>
        ///     Writes a string with a specified encoding to memory.
        /// </summary>
        /// <param name="intPtr">The address where the string is written.</param>
        /// <param name="stringToWrite">The text to write.</param>
        /// <param name="encoding">The encoding used.</param>
        public virtual void WriteString(IntPtr intPtr, string stringToWrite, Encoding encoding)
        {
            if (stringToWrite[stringToWrite.Length - 1] != '\0')
                stringToWrite += '\0';
            var bytes = encoding.GetBytes(stringToWrite);
            Write(intPtr, bytes);
        }

        /// <summary>
        ///     Writes an array of a specified type to memory,
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="intPtr">The address where the values is written.</param>
        /// <param name="values">The array to write.</param>
        public void Write<T>(IntPtr intPtr, T[] values)
        {
            foreach (var value in values)
                Write(intPtr, value);
        }

        /// <summary>
        ///     Writes the values of a specified type to memory.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="intPtr">The address where the value is written.</param>
        /// <param name="value">The value to write.</param>
        public abstract void Write<T>(IntPtr intPtr, T value);

        /// <summary>
        /// Reads the Runtime Type Information (RTTI) at certain address.
        /// </summary>
        /// <param name="address">Address to read the type from.</param>
        /// <returns>Runtime Type Information of the object at the passed address or null if it's not an object.</returns>
        public string ReadRemoteRuntimeTypeInformation(IntPtr address)
        {
            if (address.MayBeValid())
            {
                if(!rttiCache.TryGetValue(address, out string rtti))
                {
                    var objectLocatorPtr = Read<IntPtr>(address - IntPtr.Size);
                    if (objectLocatorPtr.MayBeValid())
                    {
                        if (is32Bit)
                            rtti = ReadRemoteRuntimeTypeInformation32(objectLocatorPtr);
                        else
                            rtti = ReadRemoteRuntimeTypeInformation64(objectLocatorPtr);

                        rttiCache.AddOrUpdate(address, toAdd =>
                        {
                            return rtti;
                        }, (toUpdate, old) =>
                        {
                            if (old != rtti)
                                old = rtti;
                            return old;
                        });
                    }
                }
                return rtti;
            }

            return null;
        }
        private string ReadRemoteRuntimeTypeInformation32(IntPtr address)
        {
            var classHierarchyDescriptorPtr = Read<IntPtr>(address + 0x10);
            if (classHierarchyDescriptorPtr.MayBeValid())
            {
                var baseClassCount = Read<int>(classHierarchyDescriptorPtr + 8);
                if (baseClassCount > 0 && baseClassCount < 25)
                {
                    var baseClassArrayPtr = Read<IntPtr>(classHierarchyDescriptorPtr + 0xC);
                    if (baseClassArrayPtr.MayBeValid())
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < baseClassCount; ++i)
                        {
                            var baseClassDescriptorPtr = Read<IntPtr>(baseClassArrayPtr + (4 * i));
                            if (baseClassDescriptorPtr.MayBeValid())
                            {
                                var typeDescriptorPtr = Read<IntPtr>(baseClassDescriptorPtr);
                                if (typeDescriptorPtr.MayBeValid())
                                {
                                    var name = ReadString(typeDescriptorPtr + 0x0C, Encoding.UTF8, 60);
                                    if (name.EndsWith("@@"))
                                    {
                                        name = DbgHelp.UnDecorateSymbolName($"?{name}", UnDecorateFlags.UNDNAME_NAME_ONLY);
                                    }
                                    sb.Append(name);
                                    sb.Append(" : ");

                                    continue;
                                }
                            }

                            break;
                        }

                        if (sb.Length != 0)
                        {
                            sb.Length -= 3;

                            return sb.ToString();
                        }
                    }
                }
            }

            return null;
        }
        private string ReadRemoteRuntimeTypeInformation64(IntPtr address)
        {
            int baseOffset = Read<int>(address + 0x14);
            if (baseOffset != 0)
            {
                var baseAddress = address - baseOffset;

                var classHierarchyDescriptorOffset = Read<int>(address + 0x10);
                if (classHierarchyDescriptorOffset != 0)
                {
                    var classHierarchyDescriptorPtr = baseAddress + classHierarchyDescriptorOffset;

                    var baseClassCount = Read<int>(classHierarchyDescriptorPtr + 0x08);
                    if (baseClassCount > 0 && baseClassCount < 25)
                    {
                        var baseClassArrayOffset = Read<int>(classHierarchyDescriptorPtr + 0x0C);
                        if (baseClassArrayOffset != 0)
                        {
                            var baseClassArrayPtr = baseAddress + baseClassArrayOffset;

                            var sb = new StringBuilder();
                            for (var i = 0; i < baseClassCount; ++i)
                            {
                                var baseClassDescriptorOffset = Read<int>(baseClassArrayPtr + (4 * i));
                                if (baseClassDescriptorOffset != 0)
                                {
                                    var baseClassDescriptorPtr = baseAddress + baseClassDescriptorOffset;

                                    var typeDescriptorOffset = Read<int>(baseClassDescriptorPtr);
                                    if (typeDescriptorOffset != 0)
                                    {
                                        var typeDescriptorPtr = baseAddress + typeDescriptorOffset;

                                        var name = ReadString(typeDescriptorPtr + 0x14, Encoding.UTF8, 60);
                                        if (string.IsNullOrEmpty(name))
                                        {
                                            break;
                                        }

                                        if (name.EndsWith("@@"))
                                        {
                                            name = DbgHelp.UnDecorateSymbolName($"?{name}", UnDecorateFlags.UNDNAME_NAME_ONLY);
                                        }

                                        sb.Append(name);
                                        sb.Append(" : ");

                                        continue;
                                    }
                                }

                                break;
                            }

                            if (sb.Length != 0)
                            {
                                sb.Length -= 3;

                                return sb.ToString();
                            }
                        }
                    }
                }
            }

            return null;
        }

    }
}