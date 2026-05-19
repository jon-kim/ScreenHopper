using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ScreenHopper;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var assembly = Assembly.GetExecutingAssembly();

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        var fileVersion = FileVersionInfo
            .GetVersionInfo(assembly.Location)
            .ProductVersion;

        var assemblyVersion = assembly.GetName().Version?.ToString();

        var displayVersion = informationalVersion;

        if (string.IsNullOrWhiteSpace(displayVersion))
        {
            displayVersion = fileVersion;
        }

        if (string.IsNullOrWhiteSpace(displayVersion))
        {
            displayVersion = assemblyVersion;
        }

        VersionText.Text = string.IsNullOrWhiteSpace(displayVersion)
            ? "Version unknown"
            : $"Version {displayVersion}";
    }

    private void GitHubLink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = "https://github.com/jon-kim/ScreenHopper",
            UseShellExecute = true
        });
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
