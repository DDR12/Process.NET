using System;
using ProcessNET.Windows.Mouse;

namespace ProcessNET.Windows
{
    public abstract class HookEventArgs : EventArgs
    {
        protected HookEventType EventType { get; set; }
    }
}