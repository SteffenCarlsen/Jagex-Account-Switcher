using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using JagexAccountSwitcher.Model;
using JagexAccountSwitcher.Views;

namespace JagexAccountSwitcher.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private UserSettings _settings;
        private object _currentView;
        private readonly Dictionary<string, object> _viewInstances;
        public AccountOverviewViewModel AccountOverviewViewModel { get; set; }
        public LandingPageViewModel LandingPageViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
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

        public MainWindowViewModel(Window window)
        {
            _settings = new UserSettings();
            _settings.LoadFromFile();
            // Set default view
            AccountOverviewViewModel = new AccountOverviewViewModel(_settings);
            LandingPageViewModel = new LandingPageViewModel();
            SettingsViewModel = new SettingsViewModel(window.StorageProvider, _settings);
            ChangeViewCommand = new RelayCommand<string>(ChangeView);
            _viewInstances = new Dictionary<string, object>();
            ChangeView("LandingPage");
        }

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
                    _ => new LandingPage()
                };
                _viewInstances[viewName] = view;
            }

            CurrentView = view;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}