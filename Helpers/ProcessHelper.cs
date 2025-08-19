#region

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Avalonia.Threading;
using JagexAccountSwitcher.Model;

#endregion

namespace JagexAccountSwitcher.Helpers;

public static class ProcessHelper
{
    public static void KillClient(MassAccountLinkerModel model)
    {
        if (model?.Process == null || model.Process.HasExited) return;

        try
        {
            // Store info before attempting to kill
            var processId = model.Process.Id;

            // Try standard approach first
            model.Process.CloseMainWindow();

            if (!model.Process.WaitForExit(2000))
            {
                try
                {
                    // Try direct kill
                    model.Process.Kill(true);
                    model.Process.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kill failed: {ex.Message}");
                }

                // Try more aggressive taskkill approach
                var killProcess = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c taskkill /F /T /PID {processId} & taskkill /F /IM javaw.exe /FI \"PID eq {processId}\" & taskkill /F /IM java.exe /FI \"PID eq {processId}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (var process = Process.Start(killProcess))
                {
                    process?.WaitForExit();
                }

                // Double-check if the process is truly gone
                try
                {
                    var checkProcess = Process.GetProcessById(processId);
                    if (!checkProcess.HasExited)
                    {
                        Console.WriteLine($"Process {processId} still alive, using final termination attempt");
                        // Use even more aggressive approach with WMIC
                        var wmicKill = new ProcessStartInfo
                        {
                            FileName = "wmic",
                            Arguments = $"process where processid=\"{processId}\" call terminate",
                            CreateNoWindow = true,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(wmicKill)?.WaitForExit();
                    }
                }
                catch (ArgumentException)
                {
                    // Process not found, which means it was successfully terminated
                    Console.WriteLine("Process was successfully terminated");
                }
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                model.Process = null;
                model.ProcessLifetime = string.Empty;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing process: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if the current process is running with administrator privileges
    /// </summary>
    /// <returns>True if the process has admin rights, false otherwise</returns>
    public static bool IsRunningAsAdmin()
    {
        // This only applies to Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;
                
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
}