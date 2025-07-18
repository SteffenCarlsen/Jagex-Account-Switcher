﻿#region

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#endregion

namespace JagexAccountSwitcher.Model;

public class MassAccountLinkerModel : INotifyPropertyChanged
{
    // In MassAccountLinkerModel.cs
    private Process? _process;
    private string? _processLifetime;

    public required RunescapeAccount Account { get; set; }

    public Process? Process
    {
        get => _process;
        set
        {
            _process = value;
            OnPropertyChanged();
        }
    }

    public string? ProcessLifetime
    {
        get => _processLifetime;
        set
        {
            _processLifetime = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}