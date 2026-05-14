using System.Drawing;

namespace ScreenHopper.Models;

public sealed class MonitorInfo
{
    public required string DeviceName { get; init; }

    public required string DisplayName { get; init; }

    public required Rectangle WorkingArea { get; init; }

    public required Rectangle Bounds { get; init; }

    public required bool IsPrimary { get; init; }

    public override string ToString() => DisplayName;
}
