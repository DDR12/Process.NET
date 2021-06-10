using System;

namespace ProcessNET.Modules
{
    public interface IProcessFunction
    {
        IntPtr BaseAddress { get; }
        string Name { get; }
        T GetDelegate<T>();
    }
}