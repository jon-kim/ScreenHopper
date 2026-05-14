using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenHopper.Helpers;
using ScreenHopper.Models;

namespace ScreenHopper.Services;

public sealed class ProcessDiscoveryService
{
    public IReadOnlyList<AppProcessItem> GetVisibleProcesses(HashSet<string> blacklist, Dictionary<string, AppPreferences> storedPreferences)
    {
        var results = new List<AppProcessItem>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                if (!NativeMethods.IsWindowVisible(process.MainWindowHandle))
                {
                    continue;
                }

                if (IsToolOrOwnedWindow(process.MainWindowHandle))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(process.ProcessName) || blacklist.Contains(process.ProcessName))
                {
                    continue;
                }

                var display = $"{process.ProcessName} - {process.MainWindowTitle}";

                var preferences = storedPreferences.TryGetValue(process.ProcessName, out var existingPreferences)
                    ? existingPreferences
                    : new AppPreferences();

                results.Add(new AppProcessItem
                {
                    ProcessName = process.ProcessName,
                    ProcessId = process.Id,
                    MainWindowHandle = process.MainWindowHandle,
                    DisplayText = display,
                    Process = process,
                    Icon = TryExtractIcon(process),
                    Preferences = preferences
                });
            }
            catch
            {
                // Ignore inaccessible or exited process entries.
            }
        }

        return results
            .OrderBy(item => item.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ImageSource? TryExtractIcon(Process process)
    {
        try
        {
            var filePath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            using var icon = Icon.ExtractAssociatedIcon(filePath);
            if (icon is null)
            {
                return null;
            }

            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(16, 16));

            imageSource.Freeze();
            return imageSource;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsToolOrOwnedWindow(IntPtr handle)
    {
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlStyle).ToInt64();
        if ((style & NativeMethods.WsChild) != 0)
        {
            return true;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExstyle).ToInt64();
        if ((exStyle & NativeMethods.WsExToolwindow) != 0)
        {
            return true;
        }

        if (NativeMethods.GetWindow(handle, NativeMethods.GwOwner) != IntPtr.Zero)
        {
            return true;
        }

        var rootOwner = NativeMethods.GetAncestor(handle, NativeMethods.GaRootOwner);
        return rootOwner != IntPtr.Zero && rootOwner != handle;
    }
}
