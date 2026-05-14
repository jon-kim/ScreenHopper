using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScreenHopper.Models;

public class AppPreferences : INotifyPropertyChanged
{
    private bool _requiresDoubleClick;
    private int _opacityLevel = 100;
    private bool _alwaysOnTop;
    private ZoneSnap _defaultZone = ZoneSnap.FullCenter;

    public bool RequiresDoubleClick
    {
        get => _requiresDoubleClick;
        set => SetField(ref _requiresDoubleClick, value);
    }

    public int OpacityLevel
    {
        get => _opacityLevel;
        set => SetField(ref _opacityLevel, Math.Clamp(value, 0, 100));
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetField(ref _alwaysOnTop, value);
    }

    public ZoneSnap DefaultZone
    {
        get => _defaultZone;
        set => SetField(ref _defaultZone, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
