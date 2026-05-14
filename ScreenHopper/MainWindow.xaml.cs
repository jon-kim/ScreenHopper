using System.Windows;
using ScreenHopper.ViewModels;

namespace ScreenHopper;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
