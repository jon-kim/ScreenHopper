using System.Drawing;
using ScreenHopper.Helpers;
using ScreenHopper.Models;
using Screen = System.Windows.Forms.Screen;

namespace ScreenHopper.Services;

public sealed class WindowMoveService
{
    private const int SwRestore = 9;

    public bool MoveWindow(MoveRequest request)
    {
        if (request.WindowHandle == IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.ShowWindow(request.WindowHandle, SwRestore);
        NativeMethods.SetForegroundWindow(request.WindowHandle);

        if (request.RequiresDoubleClick)
        {
            DoubleClickWindowCenter(request.WindowHandle);
        }

        var targetBounds = CalculateTargetBounds(request.WindowHandle, request.DestinationMonitor, request.Zone);
        var insertAfter = request.AlwaysOnTop ? NativeMethods.HwndTopmost : NativeMethods.HwndNotTopmost;

        var moved = NativeMethods.SetWindowPos(
            request.WindowHandle,
            insertAfter,
            targetBounds.X,
            targetBounds.Y,
            targetBounds.Width,
            targetBounds.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        ApplyOpacity(request.WindowHandle, request.OpacityLevel);
        return moved;
    }

    private static Rectangle CalculateTargetBounds(IntPtr windowHandle, MonitorInfo destinationMonitor, ZoneSnap zone)
    {
        var working = destinationMonitor.WorkingArea;

        var sourceMonitorHandle = NativeMethods.MonitorFromWindow(windowHandle, NativeMethods.MonitorDefaulttonearest);
        var sourceDpi = TryGetDpi(sourceMonitorHandle);

        var destinationScreen = Screen.AllScreens.FirstOrDefault(screen =>
            string.Equals(screen.DeviceName, destinationMonitor.DeviceName, StringComparison.OrdinalIgnoreCase));

        var centerPoint = destinationScreen is null
            ? new NativeMethods.Point { X = working.X + (working.Width / 2), Y = working.Y + (working.Height / 2) }
            : new NativeMethods.Point
            {
                X = destinationScreen.Bounds.X + (destinationScreen.Bounds.Width / 2),
                Y = destinationScreen.Bounds.Y + (destinationScreen.Bounds.Height / 2)
            };

        var destinationMonitorHandle = NativeMethods.MonitorFromPoint(centerPoint, NativeMethods.MonitorDefaulttonearest);
        var destinationDpi = TryGetDpi(destinationMonitorHandle);

        var scale = destinationDpi / sourceDpi;

        var zoneRect = zone switch
        {
            ZoneSnap.LeftHalf => new Rectangle(working.X, working.Y, working.Width / 2, working.Height),
            ZoneSnap.RightHalf => new Rectangle(working.X + (working.Width / 2), working.Y, working.Width / 2, working.Height),
            ZoneSnap.TopLeft => new Rectangle(working.X, working.Y, working.Width / 2, working.Height / 2),
            ZoneSnap.TopRight => new Rectangle(working.X + (working.Width / 2), working.Y, working.Width / 2, working.Height / 2),
            ZoneSnap.BottomLeft => new Rectangle(working.X, working.Y + (working.Height / 2), working.Width / 2, working.Height / 2),
            ZoneSnap.BottomRight => new Rectangle(working.X + (working.Width / 2), working.Y + (working.Height / 2), working.Width / 2, working.Height / 2),
            _ => new Rectangle(working.X, working.Y, working.Width, working.Height)
        };

        var scaledWidth = Math.Max(100, (int)Math.Round(zoneRect.Width * scale));
        var scaledHeight = Math.Max(100, (int)Math.Round(zoneRect.Height * scale));

        var centeredX = zoneRect.X + ((zoneRect.Width - scaledWidth) / 2);
        var centeredY = zoneRect.Y + ((zoneRect.Height - scaledHeight) / 2);

        var boundedX = Math.Max(working.X, centeredX);
        var boundedY = Math.Max(working.Y, centeredY);
        var boundedWidth = Math.Min(scaledWidth, working.Right - boundedX);
        var boundedHeight = Math.Min(scaledHeight, working.Bottom - boundedY);

        return new Rectangle(boundedX, boundedY, boundedWidth, boundedHeight);
    }

    private static double TryGetDpi(IntPtr monitorHandle)
    {
        if (monitorHandle == IntPtr.Zero)
        {
            return 96d;
        }

        try
        {
            var hr = NativeMethods.GetDpiForMonitor(monitorHandle, NativeMethods.MonitorDpiType.EffectiveDpi, out var dpiX, out _);
            if (hr == 0 && dpiX > 0)
            {
                return dpiX;
            }
        }
        catch
        {
            // Fall back to default.
        }

        return 96d;
    }

    private static void DoubleClickWindowCenter(IntPtr windowHandle)
    {
        if (!NativeMethods.GetWindowRect(windowHandle, out var rect))
        {
            return;
        }

        var centerX = rect.Left + ((rect.Right - rect.Left) / 2);
        var centerY = rect.Top + ((rect.Bottom - rect.Top) / 2);

        NativeMethods.GetCursorPos(out var originalPos);
        NativeMethods.SetCursorPos(centerX, centerY);

        ClickOnce();
        Thread.Sleep(50);
        ClickOnce();

        NativeMethods.SetCursorPos(originalPos.X, originalPos.Y);
    }

    private static void ClickOnce()
    {
        NativeMethods.mouse_event(NativeMethods.MouseeventfLeftdown, 0, 0, 0, UIntPtr.Zero);
        NativeMethods.mouse_event(NativeMethods.MouseeventfLeftup, 0, 0, 0, UIntPtr.Zero);
    }

    private static void ApplyOpacity(IntPtr windowHandle, int opacityLevel)
    {
        var exStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlExstyle).ToInt64();
        var updatedStyle = new IntPtr(exStyle | NativeMethods.WsExLayered);
        NativeMethods.SetWindowLongPtr(windowHandle, NativeMethods.GwlExstyle, updatedStyle);

        var alpha = (byte)Math.Clamp((int)Math.Round((opacityLevel / 100d) * 255d), 0, 255);
        NativeMethods.SetLayeredWindowAttributes(windowHandle, 0, alpha, NativeMethods.LwaAlpha);
    }
}
