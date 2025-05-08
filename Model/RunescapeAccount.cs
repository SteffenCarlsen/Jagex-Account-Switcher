using System.ComponentModel;

namespace JagexAccountSwitcher.Model;

public class RunescapeAccount : INotifyPropertyChanged
{
    private bool _isActiveAccount;
    public string AccountName { get; set; }
    public string UserAccount { get; set; }
    public string FilePath { get; set; }
    
    private string? _clientArguments { get; set; }
    
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

    public string? ClientArguments
    {
        get => _clientArguments;
        set
        {
            if (_clientArguments != value)
            {
                _clientArguments = value;
                OnPropertyChanged(nameof(ClientArguments));
                OnPropertyChanged(nameof(HasClientArguments));
            }
        }
    }

    public bool HasClientArguments => (ClientArguments != string.Empty) && (!string.IsNullOrWhiteSpace(ClientArguments));

    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}