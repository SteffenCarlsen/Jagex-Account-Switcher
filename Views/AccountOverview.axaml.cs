using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JagexAccountSwitcher.ViewModels;

namespace JagexAccountSwitcher.Views;

public partial class AccountOverview : UserControl
{
    private readonly AccountOverviewViewModel _viewModel;
    public AccountOverview(AccountOverviewViewModel accountOverviewViewModel)
    {
        InitializeComponent();
        _viewModel = accountOverviewViewModel;
        DataContext = _viewModel;
    }
    
    public AccountOverview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}