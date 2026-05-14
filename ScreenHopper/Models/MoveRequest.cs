namespace ScreenHopper.Models;

public sealed class MoveRequest
{
    public required IntPtr WindowHandle { get; init; }

    public required MonitorInfo DestinationMonitor { get; init; }

    public required ZoneSnap Zone { get; init; }

    public required bool RequiresDoubleClick { get; init; }

    public required int OpacityLevel { get; init; }

    public required bool AlwaysOnTop { get; init; }
}
