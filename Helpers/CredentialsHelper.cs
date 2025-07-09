#region

using System.IO;

#endregion

namespace JagexAccountSwitcher.Helpers;

public static class CredentialsHelper
{
    public static string? GetDisplayName(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file does not exist.", filePath);

        foreach (var line in File.ReadLines(filePath))
        {
            if (line.StartsWith("JX_DISPLAY_NAME="))
            {
                return line.Substring("JX_DISPLAY_NAME=".Length).Trim();
            }
        }

        return null; // Return null if the key is not found
    }
}