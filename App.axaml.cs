using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using JagexAccountSwitcher.Converters;
using JagexAccountSwitcher.Model;
using JagexAccountSwitcher.ViewModels;
using Jeek.Avalonia.Localization;

namespace JagexAccountSwitcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        StartUp();
    }

    private void StartUp()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configurations");
        if (!Path.Exists(configPath))
        {
            Directory.CreateDirectory(configPath);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var config = new UserSettings();
        config.LoadFromFile();
        Localizer.SetLocalizer(new ResXLocalizer());
        var culture = config.SelectedLanguage switch
        {
            "Spanish" => "es",
            "Portuguese" => "pt",
            _ => "en"
        };
        Localizer.Language = culture;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.MainWindow.DataContext = new MainWindowViewModel(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
}