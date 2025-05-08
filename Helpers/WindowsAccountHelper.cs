#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;

#endregion

namespace JagexAccountSwitcher.Helpers;

public static class WindowsAccountHelper
{
    public static void CreateLocalWindowsAccountWithBatchLogon(string username, string password, string description = "")
    {
        // First create the account
        using (var context = new PrincipalContext(ContextType.Machine))
        {
            // Create new user
            var user = new UserPrincipal(context);
            user.Name = username;
            user.DisplayName = username;
            user.SetPassword(password);
            user.Description = description;
            user.Enabled = true;

            // Save the new user
            user.Save();

            // Add to Users group
            var group = GroupPrincipal.FindByIdentity(context, "Users");
            if (group != null)
            {
                group.Members.Add(user);
                group.Save();
            }
        }

        // Grant "Log on as a batch job" right using PowerShell
        var psCommand = $"$sid = (New-Object System.Security.Principal.NTAccount(\"{username}\")).Translate([System.Security.Principal.SecurityIdentifier]).Value; " +
                        "$tempPath = [System.IO.Path]::GetTempFileName(); " +
                        "secedit /export /cfg $tempPath; " +
                        "$content = Get-Content -Path $tempPath; " +
                        "$batchLogonRight = ($content | Select-String \"SeBatchLogonRight\").ToString(); " +
                        "if($batchLogonRight) " +
                        "{$batchLogonRight = $batchLogonRight -replace \"SeBatchLogonRight = \", \"SeBatchLogonRight = *$sid,\"; " +
                        "$content = $content -replace \"SeBatchLogonRight = .*\", $batchLogonRight} " +
                        "else {$content += \"SeBatchLogonRight = *$sid\"}; " +
                        "$content | Out-File $tempPath; " +
                        "secedit /configure /db secedit.sdb /cfg $tempPath /areas USER_RIGHTS; " +
                        "Remove-Item -Path $tempPath";

        using (var process = new Process())
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-Command \"{psCommand}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Verb = "runas"; // Run as administrator

            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to grant batch logon rights: " + ex.Message, ex);
            }
        }
    }
}