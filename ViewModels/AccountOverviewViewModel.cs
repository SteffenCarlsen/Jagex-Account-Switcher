#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using JagexAccountSwitcher.Converters;
using JagexAccountSwitcher.Helpers;
using JagexAccountSwitcher.Model;
using JagexAccountSwitcher.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

#endregion

namespace JagexAccountSwitcher.ViewModels;

public class AccountOverviewViewModel : INotifyPropertyChanged
{
    private readonly UserSettings _userSettings;

    private ObservableCollection<RunescapeAccount> _accounts;
    private RunescapeAccount _selectedAccount;

    public AccountOverviewViewModel(UserSettings userSettings)
    {
        _userSettings = userSettings;
        Accounts = new ObservableCollection<RunescapeAccount>();

        AddAccountCommand = new RelayCommand(AddAccount);
        RefreshAccountsCommand = new RelayCommand(RefreshAccounts, () => Accounts.Count > 0);
        DeleteAccountCommand = new RelayCommand(DeleteAccount, HasAccountSelected);
        SwitchToAccountCommand = new RelayCommand(SetActiveAccount, HasAccountSelected, IsNotAlreadyActiveAccount);
        SetClientArgumentsCommand = new RelayCommand(UpdateClientArgs, HasAccountSelected);
        LoadAccounts();
    }

    public ObservableCollection<RunescapeAccount> Accounts
    {
        get => _accounts;
        set
        {
            _accounts = value;
            OnPropertyChanged(nameof(Accounts));
        }
    }

    public RunescapeAccount SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            _selectedAccount = value;
            OnPropertyChanged(nameof(SelectedAccount));
            ((RelayCommand)DeleteAccountCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SwitchToAccountCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SetClientArgumentsCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand AddAccountCommand { get; }
    public ICommand RefreshAccountsCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand SwitchToAccountCommand { get; }
    public ICommand SetClientArgumentsCommand { get; }


    public event PropertyChangedEventHandler PropertyChanged;

    private void AddAccount()
    {
        var creditalsFile = new FileInfo(Path.Combine(_userSettings.RunelitePath, "credentials.properties"));
        if (creditalsFile.Exists)
        {
            var accountName = CredentialsHelper.GetDisplayName(creditalsFile.FullName);
            if (Accounts.Any(a => a.AccountName.Equals(accountName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBoxManager.GetMessageBoxStandard("Error", $"{accountName} already exists", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner).ShowAsync();
                return;
            }

            var credentialsFilePath = Path.Combine(_userSettings.RunelitePath, $"credentials.properties.{accountName}");
            var newAccount = new RunescapeAccount
            {
                AccountName = accountName,
                FilePath = credentialsFilePath,
                IsActiveAccount = true
            };
            creditalsFile.CopyTo(Path.Combine(_userSettings.ConfigurationsPath, $"credentials.properties.{accountName}"), true);
            foreach (var account in Accounts)
            {
                account.IsActiveAccount = false;
            }

            Accounts.Add(newAccount);
            SaveAccounts();
        }
    }

    private void DeleteAccount()
    {
        var file = new FileInfo(SelectedAccount.FilePath);
        if (file.Exists)
        {
            file.Delete();
        }

        Accounts.Remove(SelectedAccount);
        SaveAccounts();
    }

    private void RefreshAccounts()
    {
        var activeAccount = CredentialsHelper.GetDisplayName(Path.Combine(_userSettings.RunelitePath, "credentials.properties"));
        foreach (var account in Accounts)
        {
            account.IsActiveAccount = account.AccountName.Equals(activeAccount, StringComparison.OrdinalIgnoreCase);
        }

        SaveAccounts();
    }

    private void SetActiveAccount()
    {
        if (RuneliteHelper.SetActiveAccount(SelectedAccount, Accounts, _userSettings.ConfigurationsPath, _userSettings.RunelitePath))
        {
            SaveAccounts();
        }
    }

    private bool HasAccountSelected()
    {
        return SelectedAccount != null;
    }

    private bool IsNotAlreadyActiveAccount()
    {
        return SelectedAccount is { IsActiveAccount: false };
    }

    private void LoadAccounts()
    {
        var configDir = _userSettings.ConfigurationsPath;
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            return;
        }

        var accountsFile = Path.Combine(configDir, "accounts.json");
        if (File.Exists(accountsFile))
        {
            var json = File.ReadAllText(accountsFile);
            var accounts = JsonSerializer.Deserialize<List<RunescapeAccount>>(json, AppJsonContext.Default.ListRunescapeAccount);
            if (accounts != null)
            {
                Accounts = new ObservableCollection<RunescapeAccount>(accounts);
            }
        }
    }

    private void SaveAccounts()
    {
        RuneliteHelper.SaveAccounts(Accounts, _userSettings.ConfigurationsPath);
    }

    private void UpdateClientArgs()
    {
        var dialog = new ClientArgumentsDialog(SelectedAccount.ClientArguments ?? string.Empty, SelectedAccount.RamLimitation);

        // Get the main window to use as owner
        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        // Show dialog as modal (blocks until dialog is closed)
        dialog.ShowDialog(mainWindow!).ContinueWith(t =>
        {
            if (dialog.ClientArguments != null)
            {
                // This needs to run on the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    SelectedAccount.ClientArguments = dialog.ClientArguments;
                    SaveAccounts();
                    OnPropertyChanged(nameof(Accounts));
                });
            }
            if (dialog.SelectedRamLimitation != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SelectedAccount.RamLimitation = dialog.SelectedRamLimitation;
                    SaveAccounts();
                    OnPropertyChanged(nameof(Accounts));
                });
            }
        });
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}