using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace JagexAccountSwitcher.Views;

public partial class Guide : UserControl
{
    public Guide()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}