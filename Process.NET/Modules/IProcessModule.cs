using System.Diagnostics;
using ProcessNET.Memory;

namespace ProcessNET.Modules
{
    public interface IProcessModule : IPointer
    {
        IProcessFunction this[string functionName] { get; }

        bool IsMainModule { get; }
        string Name { get; }
        ProcessModule Native { get; }
        string Path { get; }
        int Size { get; }
        void Eject();

        IProcessFunction FindFunction(string functionName);
    }
}