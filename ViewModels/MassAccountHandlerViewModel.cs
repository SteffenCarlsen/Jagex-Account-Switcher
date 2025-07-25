﻿#region

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using JagexAccountSwitcher.Helpers;
using JagexAccountSwitcher.Model;
using RelayCommand = JagexAccountSwitcher.Helpers.RelayCommand;

#endregion

namespace JagexAccountSwitcher.ViewModels;

public class MassAccountHandlerViewModel : INotifyPropertyChanged
{
    private readonly UserSettings _settings;
    private readonly AccountOverviewViewModel _viewModel;
    private ObservableCollection<MassAccountLinkerModel> _accountProcesses;
    private bool _showClientOutput;
    private int _updateDelay = 1000;

    public MassAccountHandlerViewModel()
    {
    }

    public MassAccountHandlerViewModel(AccountOverviewViewModel accountOverviewViewModel, UserSettings settings)
    {
        _settings = settings;
        _viewModel = accountOverviewViewModel;
        _accountProcesses = new ObservableCollection<MassAccountLinkerModel>();
        StartAllCommand = new RelayCommand(StartAllAccounts);
        KillClientCommand = new RelayCommand<MassAccountLinkerModel>(KillClient);
        StartClientCommand = new RelayCommand<MassAccountLinkerModel>(StartClient);

        // Initialize with accounts directly in the constructor (synchronously)
        if (_viewModel.Accounts != null)
        {
            foreach (var account in _viewModel.Accounts)
            {
                _accountProcesses.Add(new MassAccountLinkerModel { Account = account });
            }
        }

        // Subscribe to future account changes
        _viewModel.Accounts.CollectionChanged += (sender, args) => { UpdateAccountsList(); };

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UpdateDelay))
            {
                _updateDelay = UpdateDelay;
            }
        };

        _showClientOutput = ConsoleHelper.IsConsoleVisible();
        Task.Run(UpdateProcessLifetimes);
    }

    public ObservableCollection<MassAccountLinkerModel> AccountProcesses
    {
        get => _accountProcesses;
        set
        {
            _accountProcesses = value;
            OnPropertyChanged();
        }
    }

    public int UpdateDelay
    {
        get => _updateDelay;
        set
        {
            _updateDelay = value;
            OnPropertyChanged();
        }
    }

    public bool ShowClientOutput
    {
        get => _showClientOutput;
        set
        {
            _showClientOutput = value;

            // Toggle console visibility
#if WINDOWS
            if (value)
            {
                ConsoleHelper.ShowConsole();
            }
            else
            {
                ConsoleHelper.HideConsole();
            }
#endif


            OnPropertyChanged();
        }
    }

    public ICommand StartAllCommand { get; }
    public ICommand KillClientCommand { get; }
    public ICommand StartClientCommand { get; }


    public event PropertyChangedEventHandler PropertyChanged;

    private void UpdateAccountsList()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Clear existing accounts
            _accountProcesses.Clear();

            // Add all current accounts
            if (_viewModel.Accounts != null)
            {
                foreach (var account in _viewModel.Accounts)
                {
                    _accountProcesses.Add(new MassAccountLinkerModel { Account = account });
                }
            }

            // Ensure property change notification is triggered
            OnPropertyChanged(nameof(AccountProcesses));
        });
    }

    public void KillClient(MassAccountLinkerModel model)
    {
        ProcessHelper.KillClient(model);
    }

    public void StartClient(MassAccountLinkerModel model)
    {
        if (string.IsNullOrWhiteSpace(_settings.MicroBotJarPath)) return;

        // Check if already running
        if (model.Process != null && !model.Process.HasExited)
            return;

        if (RuneliteHelper.SetActiveAccount(model.Account, _viewModel.Accounts, _settings.ConfigurationsPath, _settings.RunelitePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "javaw.exe",
                Arguments = $"-jar{(model.Account.ClientArguments != null && model.Account.ClientArguments.Contains("--developer-mode") ? " -ea " : string.Empty)} \"{_settings.MicroBotJarPath}\"" + $" {model.Account.ClientArguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };

            // Set up output redirection
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                    Console.WriteLine($"[{model.Account.AccountName}] {args.Data}");
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                    Console.WriteLine($"[{model.Account.AccountName}] ERROR: {args.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                model.Process = process;
                model.ProcessLifetime = $"Runtime: {DateTime.Now - process.StartTime:hh\\:mm\\:ss}";
            });

            RuneliteHelper.SaveAccounts(_viewModel.Accounts, _settings.ConfigurationsPath);
        }
    }

    private async void StartAllAccounts()
    {
        if (string.IsNullOrWhiteSpace(_settings.MicroBotJarPath)) return;
        foreach (var account in _viewModel.Accounts)
        {
            // Check if the account is already running
            if (AccountProcesses.Any(x => x.Account == account && x.Process is { HasExited: false }))
            {
                continue;
            }

            if (RuneliteHelper.SetActiveAccount(account, _viewModel.Accounts, _settings.ConfigurationsPath, _settings.RunelitePath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "javaw.exe",
                    Arguments = $"-jar \"{_settings.MicroBotJarPath}\"" + $" {account.ClientArguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = startInfo };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var model = AccountProcesses.FirstOrDefault(x => x.Account == account);
                if (model != null)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        model.Process = process;
                        model.ProcessLifetime = $"Runtime: {DateTime.Now - process.StartTime:hh\\:mm\\:ss}";
                    });
                }

                var hasFullyLoaded = false;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null && args.Data.Contains("Client initialization took"))
                    {
                        hasFullyLoaded = true;
                    }
                };
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
                // Set up output redirection
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                        Console.WriteLine($"[{account.AccountName}:] {args.Data}");
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data) && ConsoleHelper.IsConsoleVisible())
                        Console.WriteLine($"[{account.AccountName}:] ERROR: {args.Data}");
                };
                RuneliteHelper.SaveAccounts(_viewModel.Accounts, _settings.ConfigurationsPath);
            }
        }
    }

    private async Task UpdateProcessLifetimes()
    {
        while (true)
        {
            foreach (var model in AccountProcesses)
            {
                if (model.Process != null && !model.Process.HasExited)
                {
                    try
                    {
                        var runTime = DateTime.Now - model.Process.StartTime;
                        // Use dispatcher to update UI from background thread
                        await Dispatcher.UIThread.InvokeAsync(() => { model.ProcessLifetime = $"Runtime: {runTime:hh\\:mm\\:ss}"; });
                    }
                    catch (Exception)
                    {
                        // Handle process access exceptions
                    }
                }
                else if (model.Process != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        model.Process = null;
                        model.ProcessLifetime = string.Empty;
                    });
                }
            }

            await Task.Delay(UpdateDelay);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}