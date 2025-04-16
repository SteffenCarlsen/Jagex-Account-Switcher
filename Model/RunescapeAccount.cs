using System.ComponentModel;

namespace JagexAccountSwitcher.Model;

public class RunescapeAccount : INotifyPropertyChanged
{
    private bool _isActiveAccount;
    
    public string AccountName { get; set; }
    public string FilePath { get; set; }
    
    public bool IsActiveAccount
    {
        get => _isActiveAccount;
        set
        {
            if (_isActiveAccount != value)
            {
                _isActiveAccount = value;
                OnPropertyChanged(nameof(IsActiveAccount));
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}