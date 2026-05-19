using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ScreenHopper;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
                              .GetName()
                              .Version;

        VersionText.Text = version is not null
            ? $"Version {version.Major}.{version.Minor}.{version.Build}"
            : "Version unknown";
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
