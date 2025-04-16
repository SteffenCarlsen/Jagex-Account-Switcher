using System;
using System.IO;

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
    }
}