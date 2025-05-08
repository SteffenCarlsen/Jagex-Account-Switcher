using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JagexAccountSwitcher.ViewModels;

namespace JagexAccountSwitcher.Views;

public partial class ImportJagexAccount : UserControl
{
    private readonly ImportJagexAccountViewModel _viewModel;
    
    public ImportJagexAccount(ImportJagexAccountViewModel importJagexAccountViewModel)
    {
        InitializeComponent();
        _viewModel = importJagexAccountViewModel;
        DataContext = _viewModel;
    }

    public ImportJagexAccount()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}