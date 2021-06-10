using ProcessNET.Marshaling;

namespace ProcessNET.Memory
{
    public interface IAllocatedMemory : IPointer, IDisposableState
    {
        bool IsAllocated { get; }
        int Size { get; }
        string Identifier { get; }
    }
}