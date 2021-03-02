using System;
using System.Runtime.InteropServices;
using Process.NET.Native.Types;

namespace Process.NET.Native
{
    public static class Nt
    {
        [DllImport("ntdll.dll")]
        public static extern int NtAllocateVirtualMemory(IntPtr processHandle, ref IntPtr baseAddress, uint zeroBits, ref uint regionSize, uint allocationType, uint protect);

        [DllImport("ntdll.dll")]
        public static extern int NtWriteVirtualMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, uint bufferSize, out uint written);

        [DllImport("ntdll.dll")]
        public static extern int RtlCreateUserThread(SafeMemoryHandle processHandle, IntPtr securityDescriptor, bool createSuspended, uint zeroBits, IntPtr zeroReserve, IntPtr zeroCommit, IntPtr startAddress, IntPtr startParameter, ref IntPtr threadHandle, ref NtClientId clientid);

        [DllImport("ntdll.dll")]
        public static extern int NtWaitForSingleObject(IntPtr threadHandle, bool alertable, LARGE_INTEGER largeInt);

        [DllImport("ntdll.dll")]
        public static extern int NtClose(IntPtr handle);

        [DllImport("ntdll.dll")]
        public static extern int NtProtectVirtualMemory(IntPtr processHandle, ref IntPtr baseAddress, ref uint numberOfBytes, uint newProtect, ref uint oldProtect);


        [DllImport("ntdll.dll")]
        public static extern int NtFreeVirtualMemory(IntPtr processHandle, ref IntPtr baseAddress, uint regionSize, uint freeType);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern SafeMemoryHandle RtlCreateUserThread(SafeMemoryHandle processHandle, IntPtr threadSecurity, bool createSuspended, Int32 stackZeroBits, IntPtr stackReserved,
            IntPtr stackCommit, IntPtr startAddress, IntPtr parameter, ref IntPtr threadHandle, ref IntPtr clientId);

        public const uint INFINITE = 0xFFFFFFFF;

        /// <summary>
        ///     Retrieves information about the specified process.
        /// </summary>
        /// <param name="processHandle">A handle to the process for which information is to be retrieved.</param>
        /// <param name="infoclass">The type of process information to be retrieved.</param>
        /// <param name="processinfo">
        ///     A pointer to a buffer supplied by the calling application into which the function writes the
        ///     requested information.
        /// </param>
        /// <param name="length">The size of the buffer pointed to by the ProcessInformation parameter, in bytes.</param>
        /// <param name="bytesread">
        ///     [Optional] A pointer to a variable in which the function returns the size of the requested information.
        ///     If the function was successful, this is the size of the information written to the buffer pointed to by the
        ///     ProcessInformation parameter,
        ///     but if the buffer was too small, this is the minimum size of buffer needed to receive the information successfully.
        /// </param>
        /// <returns>Returns an NTSTATUS success or error code. (STATUS_SUCCESS = 0x0).</returns>
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(SafeMemoryHandle processHandle,
            ProcessInformationClass infoclass,
            ref ProcessBasicInformation processinfo, int length, IntPtr bytesread);

        /// <summary>
        ///     Retrieves information about the specified thread.
        /// </summary>
        /// <param name="hwnd">A handle to the thread about which information is being requested.</param>
        /// <param name="infoclass">
        ///     Usually equals to 0 to dump all the structure correctly.
        ///     If this parameter is the ThreadIsIoPending value of the THREADINFOCLASS enumeration, the function determines
        ///     whether the thread has any I/O operations pending.
        ///     If this parameter is the ThreadQuerySetWin32StartAddress value of the THREADINFOCLASS enumeration, the function
        ///     returns the start address of the thread.
        ///     Note that on versions of Windows prior to Windows Vista, the returned start address is only reliable before the
        ///     thread starts running.
        /// </param>
        /// <param name="threadinfo">
        ///     A pointer to a buffer in which the function writes the requested information.
        ///     If ThreadIsIoPending is specified for the ThreadInformationClass parameter, this buffer must be large enough to
        ///     hold a ULONG value,
        ///     which indicates whether the specified thread has I/O requests pending.
        ///     If this value is equal to zero, then there are no I/O operations pending; otherwise, if the value is nonzero, then
        ///     the thread does have I/O operations pending.
        ///     If ThreadQuerySetWin32StartAddress is specified for the ThreadInformationClass parameter,
        ///     this buffer must be large enough to hold a PVOID value, which is the start address of the thread.
        /// </param>
        /// <param name="length">The size of the buffer pointed to by the ThreadInformation parameter, in bytes.</param>
        /// <param name="bytesread">
        ///     [Optional] A pointer to a variable in which the function returns the size of the requested information.
        ///     If the function was successful, this is the size of the information written to the buffer pointed to by the
        ///     ThreadInformation parameter,
        ///     but if the buffer was too small, this is the minimum size of buffer required to receive the information
        ///     successfully.
        /// </param>
        /// <returns>Returns an NTSTATUS success or error code. (STATUS_SUCCESS = 0x0).</returns>
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(SafeMemoryHandle hwnd, int infoclass,
            ref ThreadBasicInformation threadinfo, int length, IntPtr bytesread);


        /// <summary>
        /// Check if the returned flag from an Nt function call represents an operation success.
        /// </summary>
        /// <param name="value">The flag value to check.</param>
        /// <returns>True if the flag represents success.</returns>
        public static bool IsNT_StatusSuccess(int value)
        {
            return (value >= 0 && value <= 0x3FFFFFFF) || (value >= 0x40000000 && value <= 0x7FFFFFFF);
        }
    }
}