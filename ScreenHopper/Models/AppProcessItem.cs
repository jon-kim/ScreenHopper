using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ScreenHopper.Models;

public sealed class AppProcessItem : INotifyPropertyChanged
{
    private AppPreferences _preferences = new();

    public required string ProcessName { get; init; }

    public required int ProcessId { get; init; }

    public required IntPtr MainWindowHandle { get; init; }

    public required string DisplayText { get; init; }

    public required Process Process { get; init; }

    public ImageSource? Icon { get; init; }

    public AppPreferences Preferences
    {
        get => _preferences;
        set
        {
            if (ReferenceEquals(_preferences, value))
            {
                return;
            }

            _preferences = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preferences)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => DisplayText;
}
