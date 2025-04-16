using System.ComponentModel;
using System.IO;
using System.Text.Json;
using JagexAccountSwitcher.Converters;
using JagexAccountSwitcher.Helpers;

namespace JagexAccountSwitcher.Model;

public class UserSettings : INotifyPropertyChanged
{
    public string RunelitePath { get; set; } = RuneliteHelper.GetRunelitePath();
    public string ConfigurationsPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Configurations");
    
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
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}