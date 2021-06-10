using System;
using System.Collections.Generic;

namespace ProcessNET.Windows
{
    public interface IWindowFactory : IDisposable
    {
        IEnumerable<IWindow> this[string windowTitle] { get; }

        IWindow MainWindow { get; set; }
        IEnumerable<IWindow> Windows { get; }

        IEnumerable<IWindow> GetWindowsByClassName(string className);
        IEnumerable<IWindow> GetWindowsByTitle(string windowTitle);
        IEnumerable<IWindow> GetWindowsByTitleContains(string windowTitle);
    }
}