using System.Drawing;
using System.Windows.Forms;
using ScreenHopper.Models;

namespace ScreenHopper.Services;

public sealed class MonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Screen.AllScreens
            .Select((screen, index) => new MonitorInfo
            {
                DeviceName = screen.DeviceName,
                DisplayName = $"Monitor {index + 1} ({screen.Bounds.Width}x{screen.Bounds.Height}){(screen.Primary ? " [Primary]" : string.Empty)}",
                WorkingArea = new Rectangle(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height),
                Bounds = new Rectangle(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height),
                IsPrimary = screen.Primary
            })
            .ToList();
    }
}
