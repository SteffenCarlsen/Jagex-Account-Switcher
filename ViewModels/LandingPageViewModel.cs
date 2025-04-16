using System.ComponentModel;

namespace JagexAccountSwitcher.ViewModels
{
    public class LandingPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add properties and logic specific to the Landing Page here
    }
}