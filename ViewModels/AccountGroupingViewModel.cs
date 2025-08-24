#region

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using JagexAccountSwitcher.Helpers;
using JagexAccountSwitcher.Model;
using JagexAccountSwitcher.Services;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

#endregion

namespace JagexAccountSwitcher.ViewModels;

public class AccountGroupingViewModel : INotifyPropertyChanged
{
    private readonly GroupService _groupService;
    private IBrush _editingGroupColor;
    private string _editingGroupName;
    private RunescapeAccount _selectedAvailableAccount;
    private IBrush _selectedColor;
    private AccountGroup _selectedGroup;
    private RunescapeAccount _selectedGroupAccount;

    public AccountGroupingViewModel(AccountOverviewViewModel accountOverviewViewModel, UserSettings settings)
    {
        AccountOverviewViewModel = accountOverviewViewModel;
        UserSettings = settings;

        // Initialize GroupService with the configuration directory
        _groupService = new GroupService(UserSettings.ConfigurationsPath);

        // Initialize commands
        CreateGroupCommand = new RelayCommand(CreateGroup);
        EditGroupCommand = new RelayCommand(EditGroup, () => SelectedGroup != null);
        DeleteGroupCommand = new RelayCommand(DeleteGroup, () => SelectedGroup != null);
        SaveGroupCommand = new RelayCommand(SaveGroup, () => SelectedGroup != null);
        AddAccountToGroupCommand = new RelayCommand(AddAccountToGroup, () => CanAddToGroup);
        RemoveAccountFromGroupCommand = new RelayCommand(RemoveAccountFromGroup, () => CanRemoveFromGroup);
        StartGroupCommand = new RelayCommand(StartGroupAccounts, () => SelectedGroup != null);
        StopGroupCommand = new RelayCommand(StopGroupAccounts, () => SelectedGroup != null);

        // Initialize collections
        Groups = new ObservableCollection<AccountGroup>(_groupService.Groups);
        AvailableAccounts = new ObservableCollection<RunescapeAccount>();
        GroupAccounts = new ObservableCollection<RunescapeAccount>();

        // Initialize colors
        InitializeColors();

        // Load accounts
        RefreshAccountLists();
    }

    public UserSettings UserSettings { get; }
    public AccountOverviewViewModel AccountOverviewViewModel { get; }

    public ObservableCollection<AccountGroup> Groups { get; }
    public ObservableCollection<RunescapeAccount> AvailableAccounts { get; }
    public ObservableCollection<RunescapeAccount> GroupAccounts { get; }
    public ObservableCollection<IBrush> AvailableColors { get; private set; }

    public AccountGroup SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            _selectedGroup = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedGroup));

            if (_selectedGroup != null)
            {
                EditingGroupName = _selectedGroup.Name;
                EditingGroupColor = SolidColorBrush.Parse(_selectedGroup.Color);
                SelectedColor = EditingGroupColor;
                RefreshAccountLists();
            }

            ((RelayCommand)EditGroupCommand).NotifyCanExecuteChanged();
            ((RelayCommand)DeleteGroupCommand).NotifyCanExecuteChanged();
            ((RelayCommand)SaveGroupCommand).NotifyCanExecuteChanged();
            ((RelayCommand)StartGroupCommand).NotifyCanExecuteChanged();
            ((RelayCommand)StopGroupCommand).NotifyCanExecuteChanged();
        }
    }

    public RunescapeAccount SelectedAvailableAccount
    {
        get => _selectedAvailableAccount;
        set
        {
            _selectedAvailableAccount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAddToGroup));
            ((RelayCommand)AddAccountToGroupCommand).NotifyCanExecuteChanged();
        }
    }

    public RunescapeAccount SelectedGroupAccount
    {
        get => _selectedGroupAccount;
        set
        {
            _selectedGroupAccount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRemoveFromGroup));
            ((RelayCommand)RemoveAccountFromGroupCommand).NotifyCanExecuteChanged();
        }
    }

    public string EditingGroupName
    {
        get => _editingGroupName;
        set
        {
            _editingGroupName = value;
            OnPropertyChanged();
        }
    }

    public IBrush EditingGroupColor
    {
        get => _editingGroupColor;
        set
        {
            _editingGroupColor = value;
            OnPropertyChanged();
        }
    }

    public IBrush SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            EditingGroupColor = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedGroup => SelectedGroup != null;
    public bool CanAddToGroup => SelectedGroup != null && SelectedAvailableAccount != null;
    public bool CanRemoveFromGroup => SelectedGroup != null && SelectedGroupAccount != null;

    public ICommand CreateGroupCommand { get; }
    public ICommand EditGroupCommand { get; }
    public ICommand DeleteGroupCommand { get; }
    public ICommand SaveGroupCommand { get; }
    public ICommand AddAccountToGroupCommand { get; }
    public ICommand RemoveAccountFromGroupCommand { get; }
    public ICommand StartGroupCommand { get; }
    public ICommand StopGroupCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void InitializeColors()
    {
        AvailableColors = new ObservableCollection<IBrush>
        {
            new SolidColorBrush(Color.Parse("#3498db")), // Blue
            new SolidColorBrush(Color.Parse("#2ecc71")), // Green
            new SolidColorBrush(Color.Parse("#e74c3c")), // Red
            new SolidColorBrush(Color.Parse("#f39c12")), // Orange
            new SolidColorBrush(Color.Parse("#9b59b6")), // Purple
            new SolidColorBrush(Color.Parse("#1abc9c")), // Teal
            new SolidColorBrush(Color.Parse("#34495e")), // Dark Blue
            new SolidColorBrush(Color.Parse("#7f8c8d")) // Gray
        };
    }

    private void RefreshAccountLists()
    {
        // Clear existing collections
        AvailableAccounts.Clear();
        GroupAccounts.Clear();

        if (SelectedGroup == null)
            return;

        // Get all accounts from AccountOverviewViewModel
        var allAccounts = AccountOverviewViewModel.Accounts;

        // Get accounts in the selected group
        foreach (var accountId in SelectedGroup.AccountIds)
        {
            var account = allAccounts.FirstOrDefault(a => a.AccountName == accountId);
            if (account != null)
            {
                GroupAccounts.Add(account);
            }
        }

        // Get accounts not in the selected group
        foreach (var account in allAccounts)
        {
            if (!SelectedGroup.AccountIds.Contains(account.AccountName))
            {
                AvailableAccounts.Add(account);
            }
        }
    }

    private async void StartGroupAccounts()
    {
        if (SelectedGroup == null || SelectedGroup.AccountIds.Count == 0 ||
            string.IsNullOrWhiteSpace(UserSettings.MicroBotJarPath))
            return;

        // Get the MassAccountHandlerViewModel from MainWindowViewModel
        var mainWindowViewModel = Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow.DataContext as MainWindowViewModel
            : null;

        var massAccountHandler = mainWindowViewModel?.MassAccountHandlerViewModel;
        if (massAccountHandler == null)
            return;

        // Start each account in the group
        foreach (var accountId in SelectedGroup.AccountIds)
        {
            var accountLinker = massAccountHandler.AccountProcesses
                .FirstOrDefault(a => a.Account.AccountName == accountId);

            // Skip accounts that are already running
            if (accountLinker != null && accountLinker.Process == null)
            {
                // Start the process using the same logic as in StartAllAccounts
                if (RuneliteHelper.SetActiveAccount(accountLinker.Account,
                        AccountOverviewViewModel.Accounts,
                        UserSettings.ConfigurationsPath,
                        UserSettings.RunelitePath))
                {
                    bool hasDeveloperMode = accountLinker.Account.ClientArguments != null && accountLinker.Account.ClientArguments.Contains("--developer-mode");
                    var needMacOsArgs = OperatingSystem.IsMacOS() ? "--add-opens=java.desktop/com.apple.eawt=ALL-UNNAMED --add-opens=java.desktop/sun.awt=ALL-UNNAMED" : String.Empty;
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = (OperatingSystem.IsWindows() ? "javaw" : "java") + " " + needMacOsArgs,
                        Arguments = $"-jar{(hasDeveloperMode ? " -ea" : string.Empty)} \"{UserSettings.MicroBotJarPath}\"" + $" {accountLinker.Account.ClientArguments}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = new Process { StartInfo = startInfo };

                    var hasFullyLoaded = false;
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null && args.Data.Contains("Client initialization took"))
                        {
                            hasFullyLoaded = true;
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Update the model in the UI thread
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        accountLinker.Process = process;
                        accountLinker.ProcessLifetime = $"Runtime: {DateTime.Now - process.StartTime:hh\\:mm\\:ss}";
                    });

                    var loadingTask = Task.Run(async () =>
                    {
                        while (!hasFullyLoaded)
                        {
                            if (process.HasExited)
                            {
                                hasFullyLoaded = true;
                            }

                            Console.WriteLine("Waiting for account to fully load...");
                            await Task.Delay(1000);
                        }
                    });

                    await loadingTask;
                }
            }
        }
    }

    private void StopGroupAccounts()
    {
        if (SelectedGroup == null || SelectedGroup.AccountIds.Count == 0)
            return;

        // Get the MassAccountHandlerViewModel from MainWindowViewModel
        var mainWindowViewModel = Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow.DataContext as MainWindowViewModel
            : null;

        var massAccountHandler = mainWindowViewModel?.MassAccountHandlerViewModel;
        if (massAccountHandler == null)
            return;

        // Stop each account in the group
        foreach (var accountId in SelectedGroup.AccountIds)
        {
            var accountLinker = massAccountHandler.AccountProcesses
                .FirstOrDefault(a => a.Account.AccountName == accountId);

            if (accountLinker != null && accountLinker.Process != null)
            {
                massAccountHandler.KillClient(accountLinker);
            }
        }
    }

    private void CreateGroup()
    {
        var newGroup = new AccountGroup
        {
            Name = "New Group",
            Color = "#3498db" // Default blue color
        };

        _groupService.AddGroup(newGroup);
        Groups.Add(newGroup);
        SelectedGroup = newGroup;
    }

    private void EditGroup()
    {
        // Just set the selected group which will populate the editing fields
    }

    private void DeleteGroup()
    {
        if (SelectedGroup == null)
            return;

        _groupService.DeleteGroup(SelectedGroup.Id);
        Groups.Remove(SelectedGroup);
        SelectedGroup = null;
    }

    private void SaveGroup()
    {
        if (SelectedGroup == null)
            return;

        // Store the current index of the selected group
        var selectedIndex = Groups.IndexOf(SelectedGroup);

        SelectedGroup.Name = EditingGroupName;
        SelectedGroup.Color = ((SolidColorBrush)EditingGroupColor).Color.ToString();

        _groupService.UpdateGroup(SelectedGroup);

        // Force UI refresh by replacing the item in the ObservableCollection
        if (selectedIndex >= 0)
        {
            Groups[selectedIndex] = SelectedGroup;
        }

        // This will refresh the entire list if needed
        OnPropertyChanged(nameof(Groups));
    }

    private void AddAccountToGroup()
    {
        if (SelectedGroup == null || SelectedAvailableAccount == null)
            return;

        SelectedGroup.AccountIds.Add(SelectedAvailableAccount.AccountName);
        _groupService.UpdateGroup(SelectedGroup);

        GroupAccounts.Add(SelectedAvailableAccount);
        AvailableAccounts.Remove(SelectedAvailableAccount);

        SelectedAvailableAccount = null;
    }

    private void RemoveAccountFromGroup()
    {
        if (SelectedGroup == null || SelectedGroupAccount == null)
            return;

        SelectedGroup.AccountIds.Remove(SelectedGroupAccount.AccountName);
        _groupService.UpdateGroup(SelectedGroup);

        AvailableAccounts.Add(SelectedGroupAccount);
        GroupAccounts.Remove(SelectedGroupAccount);

        SelectedGroupAccount = null;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}