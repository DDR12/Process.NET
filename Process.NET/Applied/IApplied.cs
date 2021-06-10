using ProcessNET.Marshaling;

namespace ProcessNET.Applied
{
    public interface IApplied : IDisposableState
    {
        string Identifier { get; }
        bool IsEnabled { get; }
        void Disable();
        void Enable();
    }
}