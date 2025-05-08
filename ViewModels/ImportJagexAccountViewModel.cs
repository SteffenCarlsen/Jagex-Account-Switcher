using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using JagexAccountSwitcher.Helpers;
using JagexAccountSwitcher.Model;
using Microsoft.Win32;

namespace JagexAccountSwitcher.ViewModels;

public class ImportJagexAccountViewModel : INotifyPropertyChanged
{
    private string _newAccountName = string.Empty;
    private string _jagexLauncherPath = string.Empty;
    private string _selectedWindowsAccount = string.Empty;
    private UserSettings _settings;
    private const string Password = "null";
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> WindowsAccounts { get; } = new();

    public string NewAccountName
    {
        get => _newAccountName;
        set
        {
            if (_newAccountName != value)
            {
                _newAccountName = value;
                OnPropertyChanged(nameof(NewAccountName));
                ((RelayCommand)CreateWindowsAccountCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string JagexLauncherPath
    {
        get => _jagexLauncherPath;
        set
        {
            if (_jagexLauncherPath != value)
            {
                _jagexLauncherPath = value;
                OnPropertyChanged(nameof(JagexLauncherPath));
                ((RelayCommand)LaunchAsUserCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedWindowsAccount
    {
        get => _selectedWindowsAccount;
        set
        {
            if (_selectedWindowsAccount != value)
            {
                _selectedWindowsAccount = value;
                OnPropertyChanged(nameof(SelectedWindowsAccount));
                ((RelayCommand)LaunchAsUserCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteWindowsAccountCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand BrowseJagexLauncherCommand { get; }
    public ICommand CreateWindowsAccountCommand { get; }
    public ICommand LaunchAsUserCommand { get; }
    public ICommand DeleteWindowsAccountCommand { get; }

    public ImportJagexAccountViewModel(UserSettings userSettings)
    {
        _settings = userSettings;
        BrowseJagexLauncherCommand = new RelayCommand( BrowseJagexLauncher);
        CreateWindowsAccountCommand = new RelayCommand(CreateWindowsAccount, () => !string.IsNullOrWhiteSpace(NewAccountName), AccountDoesNotAlreadyExist);
        LaunchAsUserCommand = new RelayCommand(LaunchAsUser, () => !string.IsNullOrWhiteSpace(SelectedWindowsAccount) && File.Exists(JagexLauncherPath));
        DeleteWindowsAccountCommand = new RelayCommand(DeleteWindowsAccount, () => !string.IsNullOrWhiteSpace(SelectedWindowsAccount));
        LoadWindowsAccounts();
        TryFindJagexLauncher();
    }

    private bool AccountDoesNotAlreadyExist()
    {
        return !WindowsAccounts.Any(account => 
            string.Equals(account, _newAccountName, StringComparison.OrdinalIgnoreCase));
    }

    private void DeleteWindowsAccount()
    {
        if (string.IsNullOrWhiteSpace(SelectedWindowsAccount)) return;
    
        try
        {
            // This requires admin rights
            var startInfo = new ProcessStartInfo
            {
                FileName = "net",
                Arguments = $"user {SelectedWindowsAccount} /delete",
                Verb = "runas",
                CreateNoWindow = false,
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                LoadWindowsAccounts();
                SelectedWindowsAccount = string.Empty;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting Windows account: {ex.Message}");
        }
    }

    private void LoadWindowsAccounts()
    {
        WindowsAccounts.Clear();
        string currentUsername = Environment.UserName;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount = True AND Name <> 'wdagutilityaccount'");
            foreach (var account in searcher.Get())
            {
                string sid = account["SID"].ToString();
                string accountName = account["Name"].ToString()!;
                // Skip system accounts by SID pattern
                if (sid.StartsWith("S-1-5-") && 
                    (sid.EndsWith("-500") || // Administrator
                     sid.EndsWith("-501") || // Guest
                     sid.EndsWith("-503") || // DefaultAccount
                     sid.Contains("-16")))   // System/special accounts
                {
                    continue;
                }
                
                // Skip current user account
                if (string.Equals(accountName, currentUsername, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                // Process regular user accounts
                WindowsAccounts.Add(account["Name"].ToString()!);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading Windows accounts: {ex.Message}");
        }
    }

    private void TryFindJagexLauncher()
    {
        // Common installation paths
        string[] possiblePaths = {
            @"C:\Program Files\Jagex Launcher\JagexLauncher.exe",
            @"C:\Program Files (x86)\Jagex Launcher\JagexLauncher.exe",
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                JagexLauncherPath = path;
                return;
            }
        }
    }

    private async void BrowseJagexLauncher()
    {
        var topLevel = TopLevel.GetTopLevel(new Window());
        if (topLevel == null) return;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Select Jagex Launcher",
            FileTypeFilter = new[] { 
                new FilePickerFileType("Executable") { Patterns = new[] { "*.exe" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            },
            AllowMultiple = false
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (result.Count > 0)
        {
            JagexLauncherPath = result[0].TryGetLocalPath() ?? string.Empty;
        }
    }

    private void CreateWindowsAccount()
    {
        try
        {
            WindowsAccountHelper.CreateLocalWindowsAccountWithBatchLogon(_newAccountName, Password, "Jagex Account for " + _newAccountName);
            LoadWindowsAccounts();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating Windows account: {ex.Message}");
        }
    }

    private void LaunchAsUser()
    {
        var processes = Process.GetProcessesByName("JagexLauncher");
        foreach (var process in processes)
        {
            process.Kill(true);
        }
        using (var process = new Process())
        {
            process.StartInfo.FileName = _jagexLauncherPath;
            process.StartInfo.Domain = Environment.MachineName;
            process.StartInfo.UserName = _selectedWindowsAccount;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_jagexLauncherPath);
            process.StartInfo.LoadUserProfile = true; // Load the user profile
            process.StartInfo.CreateNoWindow = false; // Allow window to be visible
        
            // Convert string password to SecureString
            var securePassword = new System.Security.SecureString();
            foreach (char c in Password)
            {
                securePassword.AppendChar(c);
            }
            process.StartInfo.Password = securePassword;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to run Jagex Launcher as user {_selectedWindowsAccount}: {ex.Message}", ex);
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}