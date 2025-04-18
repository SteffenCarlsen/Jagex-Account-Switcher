using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JagexAccountSwitcher.Converters;
using JagexAccountSwitcher.Model;

namespace JagexAccountSwitcher.Helpers
{
    public static class RuneliteHelper
    {
        public static string GetRunelitePath()
        {
            // Get the user's profile directory
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Construct the default Runelite path
            string runelitePath = Path.Combine(userProfile, ".runelite");
            
            // Check if the path exists
            if (Directory.Exists(runelitePath))
            {
                return runelitePath;
            }
            
            // Handle the case where the path does not exist
            throw new DirectoryNotFoundException($"Runelite path not found: {runelitePath}");
        }
        
        /// <summary>
        /// Sets the specified account as the active RuneLite account
        /// </summary>
        /// <param name="account">The account to set as active</param>
        /// <param name="allAccounts">List of all accounts to update IsActiveAccount flag</param>
        /// <param name="configPath">The path to the configurations directory</param>
        /// <param name="runelitePath">The path to the RuneLite directory</param>
        /// <returns>True if the account was successfully set as active</returns>
        public static bool SetActiveAccount(RunescapeAccount account, IEnumerable<RunescapeAccount> allAccounts, string configPath, string runelitePath)
        {
            var credentialsFile = new FileInfo(Path.Combine(configPath, $"credentials.properties.{account.AccountName}"));
            if (!credentialsFile.Exists)
            {
                return false;
            }
            
            // Copy the credentials file to the RuneLite directory
            credentialsFile.CopyTo(Path.Combine(runelitePath, "credentials.properties"), true);
            
            // Update active status flags
            foreach (var acc in allAccounts)
            {
                acc.IsActiveAccount = false;
            }
            account.IsActiveAccount = true;
            
            return true;
        }
        
        /// <summary>
        /// Saves the list of Runescape accounts to the accounts.json file
        /// </summary>
        /// <param name="accounts">Collection of accounts to save</param>
        /// <param name="configPath">Path to configuration directory</param>
        /// <returns>True if accounts were saved successfully</returns>
        public static bool SaveAccounts(IEnumerable<RunescapeAccount> accounts, string configPath)
        {
            try
            {
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }

                var accountsFile = Path.Combine(configPath, "accounts.json");
                var json = JsonSerializer.Serialize(accounts.ToList(), AppJsonContext.Default.ListRunescapeAccount);
                File.WriteAllText(accountsFile, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}