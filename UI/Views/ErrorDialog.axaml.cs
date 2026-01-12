using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TagHierarchyManager.UI.Views;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }
    
    public void OnOkClick(object? sender, RoutedEventArgs e) => this.Close();
}