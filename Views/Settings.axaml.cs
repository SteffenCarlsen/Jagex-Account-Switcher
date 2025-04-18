using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using JagexAccountSwitcher.ViewModels;

namespace JagexAccountSwitcher.Views;

public partial class Settings : UserControl
{
    private readonly SettingsViewModel _viewModel;
    
    public Settings()
    {
        InitializeComponent();
        
        // This will be initialized in OnAttachedToVisualTree
        _viewModel = null!;
    }
    
    public Settings(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        _viewModel = settingsViewModel;
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Only initialize if not already done in constructor
        if (DataContext == null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                DataContext = _viewModel;
            }
        }
    }
    
    private async void BrowseRunelitePath_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseRunelitePath();
    }
    
    private async void BrowseConfigurationsPath_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseConfigurationsPath();
    }

    private async void BrowseMicrobotJarPath_Click(object? sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseMicrobotJarPath();
    }
}