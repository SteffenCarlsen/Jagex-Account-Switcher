#region

using System.ComponentModel;
using System.IO;
using System.Text.Json;
using JagexAccountSwitcher.Converters;
using JagexAccountSwitcher.Helpers;

#endregion

namespace JagexAccountSwitcher.Model;

public class UserSettings : INotifyPropertyChanged
{
    private string _selectedLanguage;
    public string RunelitePath { get; set; } = RuneliteHelper.GetRunelitePath();
    public string ConfigurationsPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Configurations");
    public string MicroBotJarPath { get; set; } = string.Empty;

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;
            OnPropertyChanged(nameof(_selectedLanguage));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void SaveToFile()
    {
        var json = JsonSerializer.Serialize(this, AppJsonContext.Default.UserSettings);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Settings.json"), json);
    }

    public void LoadFromFile()
    {
        var file = Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
        if (File.Exists(file))
        {
            var json = File.ReadAllText(file);
            var config = JsonSerializer.Deserialize<UserSettings>(json, AppJsonContext.Default.UserSettings);
            if (config != null)
            {
                RunelitePath = config.RunelitePath;
                ConfigurationsPath = config.ConfigurationsPath;
                MicroBotJarPath = config.MicroBotJarPath;
                SelectedLanguage = config.SelectedLanguage;
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}