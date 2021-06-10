using System;
using System.Collections.Generic;
using System.Linq;
using ProcessNET.Utilities;

namespace ProcessNET.Windows
{
    /// <summary>
    ///     Class providing tools for manipulating windows.
    /// </summary>
    public class WindowFactory : IWindowFactory
    {
        private IProcess _process;

        private IWindow _mainWindow;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WindowFactory" /> class.
        /// </summary>
        public WindowFactory(IProcess process)
        {
            // Save the parameter
            _process = process;
        }

        /// <summary>
        ///     Gets all the window handles that belong to the application.
        /// </summary>
        internal IEnumerable<IntPtr> WindowHandles => new List<IntPtr>(WindowHelper.GetProcessWindowsHandles(_process.Native.Id));

        /// <summary>
        ///     Gets the main window handle of the application.
        /// </summary>
        public IntPtr MainWindowHandle => _process.Native.MainWindowHandle;


        /// <summary>
        ///     Gets the main window of the application.
        /// </summary>
        /*
        /// If your applications main window has child windows as well, this will not always work.In this case,
        /// you can set the main window property and it will use that value instead from that point
        /// on. You can set the value to null if you would like the return return value to go back to default. If your applications main window has child windows as well, this will not
        /// always work. In this case, you can set the main window property and it will use that value instead from that point
        ///  on. You can set the value to null if you would like the return return value to go back to default.
        */
        public IWindow MainWindow
        {
            get
            {
                return _mainWindow ?? new RemoteWindow(_process, MainWindowHandle);
            }
            set { _mainWindow = value; }
        }

        /// <summary>
        ///     Gets all the windows that have the same specified title.
        /// </summary>
        /// <param name="windowTitle">The window title string.</param>
        /// <returns>A collection of <see cref="RemoteWindow" />.</returns>
        public IEnumerable<IWindow> this[string windowTitle] => GetWindowsByTitle(windowTitle);

        /// <summary>
        ///     Gets all the windows that belong to the application.
        /// </summary>
        public IEnumerable<IWindow> Windows
        {
            get { return WindowHandles.Select(handle => new RemoteWindow(_process, handle)); }
        }

        /// <summary>
        ///     Gets all the windows that have the specified class name.
        /// </summary>
        /// <param name="className">The class name string.</param>
        /// <returns>A collection of <see cref="RemoteWindow" />.</returns>
        public IEnumerable<IWindow> GetWindowsByClassName(string className)
        {
            return WindowHandles
                .Where(handle => WindowHelper.GetClassName(handle) == className)
                .Select(handle => new RemoteWindow(_process, handle));
        }

        /// <summary>
        ///     Gets all the windows that have the same specified title.
        /// </summary>
        /// <param name="windowTitle">The window title string.</param>
        /// <returns>A collection of <see cref="RemoteWindow" />.</returns>
        public IEnumerable<IWindow> GetWindowsByTitle(string windowTitle)
        {
            return WindowHandles
                .Where(handle => WindowHelper.GetWindowText(handle) == windowTitle)
                .Select(handle => new RemoteWindow(_process, handle));
        }

        /// <summary>
        ///     Gets all the windows that contain the specified title.
        /// </summary>
        /// <param name="windowTitle">A part a window title string.</param>
        /// <returns>A collection of <see cref="RemoteWindow" />.</returns>
        public IEnumerable<IWindow> GetWindowsByTitleContains(string windowTitle)
        {
            return WindowHandles
                .Where(handle => WindowHelper.GetWindowText(handle).Contains(windowTitle))
                .Select(handle => new RemoteWindow(_process, handle));
        }

        /// <summary>
        ///     Releases all resources used by the <see cref="WindowFactory" /> object.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose... yet
        }
    }
}