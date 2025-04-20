using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using JagexAccountSwitcher.Model;
using ReactiveUI;

namespace JagexAccountSwitcher.ViewModels
{
    public class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly UserSettings _userSettings;
        private readonly IStorageProvider _storageProvider;

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

        public SettingsViewModel(IStorageProvider storageProvider, UserSettings userSettings)
        {
            _storageProvider = storageProvider;
            _userSettings = userSettings;
            
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
    }
}