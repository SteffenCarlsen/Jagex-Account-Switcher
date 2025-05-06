using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using JagexAccountSwitcher.Model;
using Jeek.Avalonia.Localization;
using ReactiveUI;

namespace JagexAccountSwitcher.ViewModels
{
    public class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly UserSettings _userSettings;
        private readonly IStorageProvider _storageProvider;
        
        private double _downloadProgress;
        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                this.RaisePropertyChanged(nameof(DownloadProgress));
                this.RaisePropertyChanged(nameof(DownloadProgressText));
            }
        }

        public string DownloadProgressText => $"{(int)(DownloadProgress * 100)}%";
        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                this.RaisePropertyChanged(nameof(IsDownloading));
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
                    this.RaisePropertyChanged(nameof(RunelitePath));
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
                    this.RaisePropertyChanged(nameof(MicroBotJarPath));
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
                    this.RaisePropertyChanged(nameof(ConfigurationsPath));
                }
            }
        }

        public static ObservableCollection<string> DisplayLanguages =>
        [
            "English",
            "Spanish",
            "Portuguese"
        ];

        private String _selectedLanguage;
        public String SelectedLanguage
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
        public SettingsViewModel(IStorageProvider storageProvider, UserSettings userSettings)
        {
            _storageProvider = storageProvider;
            _userSettings = userSettings;
            _selectedLanguage = userSettings.SelectedLanguage;
            
            // Subscribe to property changes from UserSettings
            _userSettings.PropertyChanged += UserSettings_PropertyChanged;
        }
        
        private void UserSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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

        public void Dispose()
        {
            _userSettings.PropertyChanged -= UserSettings_PropertyChanged!;
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
        var latestRelease = await GithubReleaseDownloader.ReleaseManager.Instance.GetWithTagAsync("chsami", "Microbot", "nightly");
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
        string downloadPath = Path.Combine(Directory.GetCurrentDirectory(), asset.Name);

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "JagexAccountSwitcher");

            // Use streaming approach to track download progress
            using (var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                long? totalBytes = response.Content.Headers.ContentLength;
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
    }
}