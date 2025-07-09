#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

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