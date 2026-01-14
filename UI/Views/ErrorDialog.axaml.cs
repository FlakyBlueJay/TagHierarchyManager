using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI.Views;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        this.InitializeComponent();
    }
    
    public void OnOkClick(object? sender, RoutedEventArgs e) => this.Close();
}