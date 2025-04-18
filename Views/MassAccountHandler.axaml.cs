using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JagexAccountSwitcher.ViewModels;

namespace JagexAccountSwitcher.Views;

public partial class MassAccountHandler : UserControl
{
    public MassAccountHandler(MassAccountHandlerViewModel massAccountHandlerViewModel)
    {
        InitializeComponent();
        DataContext = massAccountHandlerViewModel;
    }
    public MassAccountHandler()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}