#region

using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

#endregion

#pragma warning disable CS8601 // Possible null reference assignment.

namespace JagexAccountSwitcher.Views;

public partial class ClientArgumentsDialog : Window
{
    private Button _cancelButton;
    private CheckBox _cleanJagexLauncherCheckBox;
    private CheckBox _debugCheckBox;
    private CheckBox _developerModeCheckBox;
    private CheckBox _disableTelemetryCheckBox;
    private CheckBox _disableWalkerUpdateCheckBox;
    private CheckBox _insecureSkipTlsCheckBox;
    private TextBox _javConfigTextBox;
    private CheckBox _microbotDebugCheckBox;
    private CheckBox _noUpdateCheckBox;
    private Button _okButton;
    private TextBox _profileTextBox;
    private Grid _proxyCredentialsGrid;
    private Grid _proxyHostPortGrid;
    private TextBox _proxyHostTextBox;
    private TextBox _proxyPasswordTextBox;
    private TextBox _proxyPortTextBox;
    private ComboBox _proxyTypeComboBox;
    private TextBox _proxyUserTextBox;
    private TextBox _rawArgumentsTextBox;
    private CheckBox _safeModeCheckBox;

    public ClientArgumentsDialog()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        InitializeControls();
        RegisterEvents();
    }

    public ClientArgumentsDialog(string existingArguments) : this()
    {
        ParseExistingArguments(existingArguments);
    }


    public string ClientArguments { get; private set; }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        _cleanJagexLauncherCheckBox = this.FindControl<CheckBox>("CleanJagexLauncherCheckBox");
        _developerModeCheckBox = this.FindControl<CheckBox>("DeveloperModeCheckBox");
        _debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");
        _microbotDebugCheckBox = this.FindControl<CheckBox>("MicrobotDebugCheckBox");
        _safeModeCheckBox = this.FindControl<CheckBox>("SafeModeCheckBox");
        _insecureSkipTlsCheckBox = this.FindControl<CheckBox>("InsecureSkipTlsCheckBox");
        _disableTelemetryCheckBox = this.FindControl<CheckBox>("DisableTelemetryCheckBox");
        _disableWalkerUpdateCheckBox = this.FindControl<CheckBox>("DisableWalkerUpdateCheckBox");
        _noUpdateCheckBox = this.FindControl<CheckBox>("NoUpdateCheckBox");
        _javConfigTextBox = this.FindControl<TextBox>("JavConfigTextBox");
        _profileTextBox = this.FindControl<TextBox>("ProfileTextBox");
        _proxyTypeComboBox = this.FindControl<ComboBox>("ProxyTypeComboBox");
        _rawArgumentsTextBox = this.FindControl<TextBox>("RawArgumentsTextBox");
        _cancelButton = this.FindControl<Button>("CancelButton");
        _okButton = this.FindControl<Button>("OkButton");
        _proxyHostPortGrid = this.FindControl<Grid>("ProxyHostPortGrid");
        _proxyCredentialsGrid = this.FindControl<Grid>("ProxyCredentialsGrid");
        _proxyHostTextBox = this.FindControl<TextBox>("ProxyHostTextBox");
        _proxyPortTextBox = this.FindControl<TextBox>("ProxyPortTextBox");
        _proxyUserTextBox = this.FindControl<TextBox>("ProxyUserTextBox");
        _proxyPasswordTextBox = this.FindControl<TextBox>("ProxyPasswordTextBox");

        if (_proxyTypeComboBox != null) _proxyTypeComboBox.SelectedIndex = 0;
    }

    private void RegisterEvents()
    {
        // Register checkbox and textbox changes to update the raw arguments display
        _cleanJagexLauncherCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _developerModeCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _debugCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _microbotDebugCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _safeModeCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _insecureSkipTlsCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _disableTelemetryCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _disableWalkerUpdateCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _noUpdateCheckBox.PropertyChanged += (s, e) => UpdateRawArguments();
        _proxyTypeComboBox.SelectionChanged += (s, e) =>
        {
            var showProxyFields = _proxyTypeComboBox.SelectedIndex > 0;
            _proxyHostPortGrid.IsVisible = showProxyFields;
            _proxyCredentialsGrid.IsVisible = showProxyFields;
            UpdateRawArguments();
        };

        _javConfigTextBox.TextChanged += (s, e) => UpdateRawArguments();
        _profileTextBox.TextChanged += (s, e) => UpdateRawArguments();
        _proxyTypeComboBox.SelectionChanged += (s, e) => UpdateRawArguments();
        _proxyHostTextBox.TextChanged += (s, e) => UpdateRawArguments();
        _proxyPortTextBox.TextChanged += (s, e) => UpdateRawArguments();
        _proxyUserTextBox.TextChanged += (s, e) => UpdateRawArguments();
        _proxyPasswordTextBox.TextChanged += (s, e) => UpdateRawArguments();

        _cancelButton.Click += (s, e) => Close(null);
        _okButton.Click += OnOkButtonClick;

        UpdateRawArguments();
    }

    private void UpdateRawArguments()
    {
        var sb = new StringBuilder();

        // Add flag options
        if (_cleanJagexLauncherCheckBox.IsChecked == true) sb.Append("--clean-jagex-launcher ");
        if (_developerModeCheckBox.IsChecked == true) sb.Append("--developer-mode ");
        if (_debugCheckBox.IsChecked == true) sb.Append("--debug ");
        if (_microbotDebugCheckBox.IsChecked == true) sb.Append("--microbot-debug ");
        if (_safeModeCheckBox.IsChecked == true) sb.Append("--safe-mode ");
        if (_insecureSkipTlsCheckBox.IsChecked == true) sb.Append("--insecure-skip-tls-verification ");
        if (_disableTelemetryCheckBox.IsChecked == true) sb.Append("--disable-telemetry ");
        if (_disableWalkerUpdateCheckBox.IsChecked == true) sb.Append("--disable-walker-update ");
        if (_noUpdateCheckBox.IsChecked == true) sb.Append("--noupdate ");

        // Add options with values
        if (!string.IsNullOrWhiteSpace(_javConfigTextBox.Text))
            sb.Append($"--jav_config \"{_javConfigTextBox.Text}\" ");

        if (!string.IsNullOrWhiteSpace(_profileTextBox.Text))
            sb.Append($"--profile \"{_profileTextBox.Text}\" ");

        if (_proxyTypeComboBox.SelectedIndex > 0) // 0 is "None"
        {
            var proxyType = ((ComboBoxItem)_proxyTypeComboBox.SelectedItem!)?.Content?.ToString();

            if (!string.IsNullOrWhiteSpace(_proxyHostTextBox.Text))
            {
                // Build the proxy string in format ip:port:user:pass
                var proxyBuilder = new StringBuilder(_proxyHostTextBox.Text);

                if (!string.IsNullOrWhiteSpace(_proxyPortTextBox.Text))
                    proxyBuilder.Append(":" + _proxyPortTextBox.Text);
                else
                    proxyBuilder.Append(":");

                if (!string.IsNullOrWhiteSpace(_proxyUserTextBox.Text))
                {
                    proxyBuilder.Append(":" + _proxyUserTextBox.Text);

                    if (!string.IsNullOrWhiteSpace(_proxyPasswordTextBox.Text))
                        proxyBuilder.Append(":" + _proxyPasswordTextBox.Text);
                    else
                        proxyBuilder.Append(":");
                }

                sb.Append($"-proxy={proxyBuilder} ");
            }

            sb.Append($"-proxy-type={proxyType} ");
        }


        _rawArgumentsTextBox.Text = sb.ToString().Trim();
    }

    private void ParseExistingArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return;

        // Parse flags
        _cleanJagexLauncherCheckBox.IsChecked = arguments.Contains("--clean-jagex-launcher");
        _developerModeCheckBox.IsChecked = arguments.Contains("--developer-mode");
        _debugCheckBox.IsChecked = arguments.Contains("--debug");
        _microbotDebugCheckBox.IsChecked = arguments.Contains("--microbot-debug");
        _safeModeCheckBox.IsChecked = arguments.Contains("--safe-mode");
        _insecureSkipTlsCheckBox.IsChecked = arguments.Contains("--insecure-skip-tls-verification");
        _disableTelemetryCheckBox.IsChecked = arguments.Contains("--disable-telemetry");
        _disableWalkerUpdateCheckBox.IsChecked = arguments.Contains("--disable-walker-update");
        _noUpdateCheckBox.IsChecked = arguments.Contains("--noupdate");

        // Parse complex arguments - this is a simplified approach
        // A more robust approach would use regex or a proper argument parser

        // Parse jav_config
        var javConfigIndex = arguments.IndexOf("--jav_config");
        if (javConfigIndex >= 0)
        {
            var startQuote = arguments.IndexOf('"', javConfigIndex);
            if (startQuote >= 0)
            {
                var endQuote = arguments.IndexOf('"', startQuote + 1);
                if (endQuote > startQuote)
                {
                    _javConfigTextBox.Text = arguments.Substring(startQuote + 1, endQuote - startQuote - 1);
                }
            }
        }

        // Parse profile
        var profileIndex = arguments.IndexOf("--profile");
        if (profileIndex >= 0)
        {
            var startQuote = arguments.IndexOf('"', profileIndex);
            if (startQuote >= 0)
            {
                var endQuote = arguments.IndexOf('"', startQuote + 1);
                if (endQuote > startQuote)
                {
                    _profileTextBox.Text = arguments.Substring(startQuote + 1, endQuote - startQuote - 1);
                }
            }
        }

        var proxyArgIndex = arguments.IndexOf("-proxy=");
        if (proxyArgIndex >= 0)
        {
            var endIndex = arguments.IndexOf(' ', proxyArgIndex);
            if (endIndex < 0) endIndex = arguments.Length;

            var proxyValue = arguments.Substring(proxyArgIndex + 7, endIndex - proxyArgIndex - 7);
            var parts = proxyValue.Split(':');

            if (parts.Length >= 1) _proxyHostTextBox.Text = parts[0];
            if (parts.Length >= 2) _proxyPortTextBox.Text = parts[1];
            if (parts.Length >= 3) _proxyUserTextBox.Text = parts[2];
            if (parts.Length >= 4) _proxyPasswordTextBox.Text = parts[3];

            // Set proxy type
            if (arguments.Contains("-proxy-type=HTTP"))
                _proxyTypeComboBox.SelectedIndex = 1;
            else if (arguments.Contains("-proxy-type=SOCKS"))
                _proxyTypeComboBox.SelectedIndex = 2;
        }
    }

    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
        ClientArguments = _rawArgumentsTextBox.Text;
        Close(ClientArguments);
    }
}