#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using Avalonia.Threading;
using JagexAccountSwitcher.Model;

#endregion

namespace JagexAccountSwitcher.Helpers;

public static class ProcessHelper
{
    public static string GetProcessOwner(Process process)
    {
        if (process == null) return string.Empty;
    
        try
        {
            string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}";
            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            {
                foreach (var obj in results.Cast<ManagementObject>())
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        // argList[0] contains the username, argList[1] contains the domain
                        return argList[0]; // Just the username
                        // Or return $"{argList[1]}\\{argList[0]}"; // Domain\Username format
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting process owner: {ex.Message}");
        }
    
        return string.Empty;
    }
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

    public static Process? LaunchProgramAsUser(string program, string username, string password, string arguments = "", string accountName = "")
    {
        var currUser = Environment.UserName;
        var process = new Process();
        process.StartInfo.FileName = program;
        process.StartInfo.Arguments = arguments;
        if (currUser != username)
        {
            process.StartInfo.UserName = username;
        }
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(program);
        process.StartInfo.CreateNoWindow = false; // Allow window to be visible
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
#if WINDOWS
        process.StartInfo.Domain = Environment.MachineName;
        process.StartInfo.LoadUserProfile = true; // Load the user profile
        if (currUser != username)
        {
            // Convert string password to SecureString
            var securePassword = new System.Security.SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }
            process.StartInfo.Password = securePassword;
        }
#endif
        try
        {
            if (process.Start())
            {
                // Set up output redirection
                process.OutputDataReceived += (sender, args) => {
                    if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                        Console.WriteLine($"[{accountName}] {args.Data}");
                };
        
                process.ErrorDataReceived += (sender, args) => {
                    if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                        Console.WriteLine($"[{accountName}] ERROR: {args.Data}");
                }; 
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return process;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to run {Path.GetFileName(program)} as user {username}: {ex.Message}", ex);
        }

        return null;
    }
}