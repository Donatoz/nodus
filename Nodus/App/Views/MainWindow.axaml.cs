using Avalonia;
using Avalonia.Controls;

namespace Nodus.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        this.AttachDevTools();
    }
}