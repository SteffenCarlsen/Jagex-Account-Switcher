#region

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace JagexAccountSwitcher.Helpers;

public static class ConsoleHelper
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;

    private static TextWriterTraceListener _consoleTraceListener;

    [DllImport("kernel32.dll")]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    public static void ShowConsole()
    {
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            AllocConsole();

            // Set console title
            Console.Title = "Debug Console";

            // Reset standard output streams
            var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOutput);

            var standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(standardError);

            // Add trace listeners
            if (_consoleTraceListener == null)
            {
                _consoleTraceListener = new TextWriterTraceListener(Console.Out);
                Trace.Listeners.Add(_consoleTraceListener);

                // Also add a console listener for more complete output capture
                Trace.Listeners.Add(new ConsoleTraceListener(true));
            }

            Trace.AutoFlush = true;
            Debug.AutoFlush = true;
        }
    }

    public static void HideConsole()
    {
        if (GetConsoleWindow() != IntPtr.Zero)
        {
            // Remove trace listeners to prevent writing to a closed console
            if (_consoleTraceListener != null)
            {
                Trace.Listeners.Remove(_consoleTraceListener);
                _consoleTraceListener = null;
            }

            FreeConsole();
        }
    }

    public static bool IsConsoleVisible()
    {
#if WINDOWS
        return GetConsoleWindow() != IntPtr.Zero;
#endif
        return false;
    }
}