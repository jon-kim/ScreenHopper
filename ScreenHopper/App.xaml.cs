using System.Windows;
using ScreenHopper.Models;
using ScreenHopper.ViewModels;

namespace ScreenHopper;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (TryParseCliArguments(e.Args, out var processName, out var monitorIndex, out var zone))
        {
            var viewModel = new MainViewModel();
            viewModel.TryMoveByArguments(processName, monitorIndex, zone);
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static bool TryParseCliArguments(string[] args, out string processName, out int monitorIndex, out ZoneSnap zone)
    {
        processName = string.Empty;
        monitorIndex = 1;
        zone = ZoneSnap.FullCenter;

        if (args.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--move", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                processName = args[++i];
            }
            else if (string.Equals(arg, "--monitor", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                _ = int.TryParse(args[++i], out monitorIndex);
            }
            else if (string.Equals(arg, "--zone", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var zoneText = args[++i].Replace("-", string.Empty).Replace("_", string.Empty);
                if (Enum.TryParse<ZoneSnap>(zoneText, true, out var parsedZone))
                {
                    zone = parsedZone;
                }
            }
        }

        return !string.IsNullOrWhiteSpace(processName);
    }
}
