#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using JagexAccountSwitcher.Helpers;
using JagexAccountSwitcher.Model;
using JagexAccountSwitcher.Views;

#endregion

namespace JagexAccountSwitcher.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly UserSettings _settings;
    private readonly Dictionary<string, object> _viewInstances;
    private object _currentView;

    public MainWindowViewModel(Window window)
    {
        _settings = new UserSettings();
        _settings.LoadFromFile();
        // Set default view
        AccountOverviewViewModel = new AccountOverviewViewModel(_settings);
        LandingPageViewModel = new LandingPageViewModel();
        SettingsViewModel = new SettingsViewModel(window.StorageProvider, _settings);
        MassAccountHandlerViewModel = new MassAccountHandlerViewModel(AccountOverviewViewModel, _settings);
        AccountGroupingViewModel = new AccountGroupingViewModel(AccountOverviewViewModel, _settings);
        ChangeViewCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<string>(ChangeView);
        _viewInstances = new Dictionary<string, object>();
        ChangeView("LandingPage");
#if WINDOWS
        if (_settings.EnableSecurityMode)
        {
            ProcessProtection.RestrictProcessAccess();
        }
#endif
    }

    public AccountOverviewViewModel AccountOverviewViewModel { get; set; }
    public LandingPageViewModel LandingPageViewModel { get; set; }
    public SettingsViewModel SettingsViewModel { get; set; }
    public MassAccountHandlerViewModel MassAccountHandlerViewModel { get; set; }
    public AccountGroupingViewModel AccountGroupingViewModel { get; set; }

    public object CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public ICommand ChangeViewCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ChangeView(string viewName)
    {
        if (!_viewInstances.TryGetValue(viewName, out var view))
        {
            view = viewName switch
            {
                "Guide" => new Guide(),
                "LandingPage" => new LandingPage(),
                "AccountOverview" => new AccountOverview(AccountOverviewViewModel),
                "Settings" => new Settings(SettingsViewModel),
                "MassAccountHandler" => new MassAccountHandler(MassAccountHandlerViewModel),
                "Grouping" => new AccountGrouping(AccountGroupingViewModel),
                _ => new LandingPage()
            };
            _viewInstances[viewName] = view;
        }

        CurrentView = view;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}