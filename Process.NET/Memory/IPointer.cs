﻿using System;
using System.Text;

namespace ProcessNET.Memory
{
    public interface IPointer
    {
        IntPtr BaseAddress { get; }
        bool IsValid { get; }
        byte[] Read(int offset, int length);
        string ReadString(int offset, Encoding encoding, int maxLength);
        T Read<T>(int offset);
        T[] Read<T>(int offset, int length);
        int Write(int offset, byte[] toWrite);
        void WriteString(int offset, string stringToWrite, Encoding encoding);
        void Write<T>(int offset, T[] values);
        void Write<T>(int offset, T value);
    }
}