#region

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace JagexAccountSwitcher.Views;

public partial class LandingPage : UserControl
{
    public LandingPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}