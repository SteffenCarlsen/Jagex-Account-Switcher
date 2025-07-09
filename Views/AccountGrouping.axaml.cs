#region

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using JagexAccountSwitcher.ViewModels;

#endregion

namespace JagexAccountSwitcher.Views;

public partial class AccountGrouping : UserControl
{
    public AccountGrouping(AccountGroupingViewModel accountGroupingViewModel)
    {
        InitializeComponent();
        DataContext = accountGroupingViewModel;
    }

    public AccountGrouping()
    {
        InitializeComponent();
    }

    private AccountGroupingViewModel ViewModel => DataContext as AccountGroupingViewModel;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AvailableAccounts_DoubleTapped(object sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedAvailableAccount != null && ViewModel.CanAddToGroup)
        {
            ViewModel.AddAccountToGroupCommand.Execute(null);
        }
    }

    private void GroupAccounts_DoubleTapped(object sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedGroupAccount != null && ViewModel.CanRemoveFromGroup)
        {
            ViewModel.RemoveAccountFromGroupCommand.Execute(null);
        }
    }
}