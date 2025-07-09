#region

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GithubReleaseDownloader;
using GithubReleaseDownloader.Entities;
using JagexAccountSwitcher.Model;
using Jeek.Avalonia.Localization;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

#endregion

namespace JagexAccountSwitcher.ViewModels;

public class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IStorageProvider _storageProvider;
    private readonly UserSettings _userSettings;
    private string _currentVersion;

    private double _downloadProgress;
    private bool _isCheckingForUpdates;
    private bool _isDownloading;
    private bool _isUpdating;
    private string _latestVersion;
    private string _selectedLanguage;
    private double _updateProgress;

    public SettingsViewModel(IStorageProvider storageProvider, UserSettings userSettings)
    {
        _storageProvider = storageProvider;
        _userSettings = userSettings;
        _selectedLanguage = userSettings.SelectedLanguage;

        // Subscribe to property changes from UserSettings
        _userSettings.PropertyChanged += UserSettings_PropertyChanged;
        var operationSystem = Environment.OSVersion.Platform;
        if (operationSystem != PlatformID.MacOSX)
        {
            CheckForUpdatesOnStartup();
        }
    }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        set
        {
            _isCheckingForUpdates = value;
            this.RaisePropertyChanged();
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        set
        {
            _isUpdating = value;
            this.RaisePropertyChanged();
        }
    }

    public double UpdateProgress
    {
        get => _updateProgress;
        set
        {
            _updateProgress = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UpdateProgressText));
        }
    }

    public string UpdateProgressText => $"{(int)(UpdateProgress * 100)}%";

    public string CurrentVersion
    {
        get => _currentVersion;
        set
        {
            _currentVersion = value;
            this.RaisePropertyChanged();
        }
    }

    public string LatestVersion
    {
        get => _latestVersion;
        set
        {
            _latestVersion = value;
            this.RaisePropertyChanged();
        }
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            _downloadProgress = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(DownloadProgressText));
        }
    }

    public string DownloadProgressText => $"{(int)(DownloadProgress * 100)}%";

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            this.RaisePropertyChanged();
        }
    }

    public string RunelitePath
    {
        get => _userSettings.RunelitePath;
        set
        {
            if (_userSettings.RunelitePath != value)
            {
                _userSettings.RunelitePath = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public string MicroBotJarPath
    {
        get => _userSettings.MicroBotJarPath;
        set
        {
            if (_userSettings.MicroBotJarPath != value)
            {
                _userSettings.MicroBotJarPath = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public string ConfigurationsPath
    {
        get => _userSettings.ConfigurationsPath;
        set
        {
            if (_userSettings.ConfigurationsPath != value)
            {
                _userSettings.ConfigurationsPath = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public static ObservableCollection<string> DisplayLanguages =>
    [
        "English",
        "Spanish",
        "Portuguese"
    ];

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            var culture = value switch
            {
                "Spanish" => "es",
                "Portuguese" => "pt",
                _ => "en"
            };
            _selectedLanguage = value;
            _userSettings.SelectedLanguage = value;
            _userSettings.SaveToFile();
            Localizer.Language = culture;
        }
    }

    public void Dispose()
    {
        _userSettings.PropertyChanged -= UserSettings_PropertyChanged!;
    }

    private void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // When UserSettings properties change, notify the UI
        if (e.PropertyName == nameof(UserSettings.RunelitePath))
        {
            this.RaisePropertyChanged(nameof(RunelitePath));
        }
        else if (e.PropertyName == nameof(UserSettings.ConfigurationsPath))
        {
            this.RaisePropertyChanged(nameof(ConfigurationsPath));
        }
        else if (e.PropertyName == nameof(UserSettings.MicroBotJarPath))
        {
            this.RaisePropertyChanged(nameof(MicroBotJarPath));
        }
    }

    private string GetApplicationVersion()
    {
        // Try multiple approaches to get the version
        try
        {
            // Approach 1: Try using Environment.ProcessPath (.NET 5+)
            if (!string.IsNullOrEmpty(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
                if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                {
                    return versionInfo.FileVersion;
                }
            }
        
            // Approach 2: Try to get version from assembly attributes
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (fileVersionAttr != null && !string.IsNullOrEmpty(fileVersionAttr.Version))
            {
                return fileVersionAttr.Version;
            }
        
            // Approach 3: Try assembly version
            var version = assembly.GetName().Version;
            if (version != null)
            {
                return version.ToString();
            }
        
            // Fallback: Use the version from csproj
            return "1.6.0.2"; // Update this with each release
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting version: {ex.Message}");
            return "1.6.0.2"; // Fallback version
        }
    }
    public async Task CheckForUpdates(bool showNoUpdateMessage = false)
    {
        try
        {
            IsCheckingForUpdates = true;

            // Get current version from assembly
            CurrentVersion = GetApplicationVersion();

            // Get latest release from GitHub
            var latestRelease = await ReleaseManager.Instance.GetLatestAsync("SteffenCarlsen", "Jagex-Account-Switcher");
            if (latestRelease == null)
            {
                if (showNoUpdateMessage)
                {
                    await MessageBoxManager.GetMessageBoxStandard("Update Check", "Unable to check for updates at this time.",
                        ButtonEnum.Ok, Icon.Info).ShowAsync();
                }

                return;
            }

            // Parse version from tag (assuming tag format is v1.2.3.4)
            LatestVersion = latestRelease.TagName.TrimStart('v');

            // Compare versions
            var currentVersionParts = CurrentVersion.Split('.').Select(int.Parse).ToArray();
            var latestVersionParts = LatestVersion.Split('.').Select(int.Parse).ToArray();

            var isUpdateAvailable = false;
            for (var i = 0; i < Math.Min(currentVersionParts.Length, latestVersionParts.Length); i++)
            {
                if (latestVersionParts[i] > currentVersionParts[i])
                {
                    isUpdateAvailable = true;
                    break;
                }

                if (latestVersionParts[i] < currentVersionParts[i])
                {
                    break;
                }
            }

            if (isUpdateAvailable)
            {
                var result = await MessageBoxManager.GetMessageBoxStandard(
                    "Update Available",
                    $"A new version ({LatestVersion}) is available. Your current version is {CurrentVersion}.\n\nWould you like to update now?",
                    ButtonEnum.YesNo, Icon.Question).ShowAsync();

                if (result == ButtonResult.Yes)
                {
                    await DownloadAndInstallUpdate(latestRelease);
                }
            }
            else if (showNoUpdateMessage)
            {
                await MessageBoxManager.GetMessageBoxStandard("Update Check",
                    "You are running the latest version.", ButtonEnum.Ok, Icon.Info).ShowAsync();
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Update Error",
                $"Failed to check for updates: {ex.Message}", ButtonEnum.Ok, Icon.Error).ShowAsync();
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private async Task DownloadAndInstallUpdate(Release latestRelease)
    {
        try
        {
            IsUpdating = true;
            UpdateProgress = 0;
            var operationSystem = Environment.OSVersion.Platform;
            var stringToFind = operationSystem == PlatformID.MacOSX ? "macos" : "windows";

            // Find the asset to download (usually the exe or zip file)
            var asset = latestRelease.Assets.FirstOrDefault(a => a.Name.Contains(stringToFind) && a.Name.EndsWith(".zip"));
            if (asset == null)
            {
                await MessageBoxManager.GetMessageBoxStandard("Update Error",
                    "Could not find the update package in the release assets.", ButtonEnum.Ok, Icon.Error).ShowAsync();
                return;
            }

            // Create updates directory if it doesn't exist
            var updatesDir = Path.Combine(Directory.GetCurrentDirectory(), "Updates");
            Directory.CreateDirectory(updatesDir);

            var downloadPath = Path.Combine(updatesDir, asset.Name);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "JagexAccountSwitcher");

                // Download with progress tracking
                using (var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue)
                            {
                                UpdateProgress = (double)totalBytesRead / totalBytes.Value;
                            }
                        }
                    }
                }
            }

            // Create updater batch file
            var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            var batchFilePath = Path.Combine(updatesDir, "update.bat");

            var batchContent = @"
@echo off
echo Updating Jagex Account Switcher...
timeout /t 2 /nobreak > nul
copy /Y ""{0}"" ""{1}""
echo Update complete!
start """" ""{1}""
del ""%~f0""
".Trim().Replace("{0}", downloadPath.Replace("\\", "\\\\")).Replace("{1}", currentExePath.Replace("\\", "\\\\"));

            await File.WriteAllTextAsync(batchFilePath, batchContent);

            // Show final confirmation
            var result = await MessageBoxManager.GetMessageBoxStandard("Update Ready",
                "The update has been downloaded and is ready to install. The application will close to complete the update. Continue?",
                ButtonEnum.YesNo, Icon.Question).ShowAsync();

            if (result == ButtonResult.Yes)
            {
                // Launch updater and exit
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{batchFilePath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                // Exit application
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Update Error",
                $"Failed to download update: {ex.Message}", ButtonEnum.Ok, Icon.Error).ShowAsync();
        }
        finally
        {
            IsUpdating = false;
            UpdateProgress = 0;
        }
    }

    public async Task CheckForUpdatesOnStartup()
    {
        await Task.Delay(2000); // Wait a bit after startup
        await CheckForUpdates();
    }

    public async Task BrowseRunelitePath()
    {
        var options = new FolderPickerOpenOptions { Title = "Select RuneLite Userdata folder" };
        var folders = await _storageProvider.OpenFolderPickerAsync(options);

        if (folders.Count > 0)
        {
            RunelitePath = folders[0].Path.LocalPath;
            _userSettings.SaveToFile();
        }
    }

    public async Task BrowseConfigurationsPath()
    {
        var options = new FolderPickerOpenOptions { Title = "Select Configurations Folder" };
        var folders = await _storageProvider.OpenFolderPickerAsync(options);

        if (folders.Count > 0)
        {
            ConfigurationsPath = folders[0].Path.LocalPath;
            _userSettings.SaveToFile();
        }
    }

    public async Task BrowseMicrobotJarPath()
    {
        var jarFileType = new FilePickerFileType("Java Archive")
        {
            Patterns = new[] { "*.jar" },
            MimeTypes = new[] { "application/java-archive" }
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Select Microbot Jar",
            FileTypeFilter = new[] { jarFileType },
            AllowMultiple = false
        };

        var files = await _storageProvider.OpenFilePickerAsync(options);

        if (files.Count > 0)
        {
            MicroBotJarPath = files[0].Path.LocalPath;
            _userSettings.SaveToFile();
        }
    }


    public async Task DownloadLatestMicrobotJar()
    {
        try
        {
            var latestRelease = await ReleaseManager.Instance.GetWithTagAsync("chsami", "Microbot", "nightly");
            if (latestRelease == null)
            {
                Console.WriteLine("Failed to get the latest release.");
                return;
            }

            var asset = latestRelease.Assets.FirstOrDefault(x => x.Name.Contains("microbot-"));
            if (asset == null)
            {
                Console.WriteLine("Failed to find the Microbot asset.");
                return;
            }

            IsDownloading = true;
            DownloadProgress = 0;
            var downloadPath = Path.Combine(Directory.GetCurrentDirectory(), asset.Name);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "JagexAccountSwitcher");

                // Use streaming approach to track download progress
                using (var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue)
                            {
                                DownloadProgress = (double)totalBytesRead / totalBytes.Value;
                            }
                        }
                    }
                }

                // Update the path setting
                MicroBotJarPath = downloadPath;
                _userSettings.MicroBotJarPath = downloadPath;
                _userSettings.SaveToFile();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading Microbot JAR: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 1; // Complete the progress bar
        }
    }

    public async Task DeleteOldMicrobotJars()
    {
        try
        {
            if (string.IsNullOrEmpty(MicroBotJarPath))
            {
                await MessageBoxManager.GetMessageBoxStandard("No Microbot Jar Path", "Please set a Microbot Jar path first.").ShowAsync();
                return;
            }

            // Get the directory of the current jar file
            var directory = Path.GetDirectoryName(MicroBotJarPath) ?? string.Empty;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                await MessageBoxManager.GetMessageBoxStandard("Invalid Directory", "The Microbot Jar directory is invalid.").ShowAsync();
                return;
            }

            // Get all jar files in the directory
            var jarFiles = Directory.GetFiles(directory, "*.jar")
                .Where(f => Path.GetFileName(f).ToLower().Contains("microbot"))
                .ToList();

            if (jarFiles.Count <= 1)
            {
                await MessageBoxManager.GetMessageBoxStandard("No Old Jars", "There are no old Microbot jar files to delete.").ShowAsync();
                return;
            }

            // Get the current jar file (the one we want to keep)
            var currentJarFile = MicroBotJarPath;

            // Delete all jar files except the current one
            var deletedCount = 0;
            foreach (var file in jarFiles)
            {
                if (!string.Equals(file, currentJarFile, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with other files
                        Debug.WriteLine($"Error deleting {file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to delete old Microbot jar files: {ex.Message}", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner).ShowAsync();
        }
    }
}