using System;

namespace ProcessNET.Threads
{
    public interface IFrozenThread : IDisposable
    {
        IRemoteThread Thread { get; }
    }
}