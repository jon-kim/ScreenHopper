using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ScreenHopper;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        VersionText.Text = string.IsNullOrWhiteSpace(assemblyVersion)
            ? "Version unknown"
            : $"Version {assemblyVersion}";
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
