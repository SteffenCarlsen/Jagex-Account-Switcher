using System;
using System.Diagnostics;
using Avalonia.Threading;
using JagexAccountSwitcher.Model;

namespace JagexAccountSwitcher.Helpers;

public static class ProcessHelper
{
    public static void KillClient(MassAccountLinkerModel model)
    {
        if (model?.Process != null && !model.Process.HasExited)
        {
            try
            {
                // Try graceful shutdown first
                model.Process.CloseMainWindow();
                model.Process.Kill();
            
                // If process doesn't respond within a reasonable time, force kill the entire tree
                if (!model.Process.WaitForExit(1000))
                {
                    // Kill process tree using taskkill command
                    int processId = model.Process.Id;
                    var killProcess = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {processId}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(killProcess)?.WaitForExit();
                }
            
                Dispatcher.UIThread.InvokeAsync(() => {
                    model.Process = null;
                    model.ProcessLifetime = string.Empty;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}