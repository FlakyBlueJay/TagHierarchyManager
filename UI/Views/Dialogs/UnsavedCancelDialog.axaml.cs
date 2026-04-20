using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI.Views;

public partial class UnsavedCancelDialog : Window
{
    public UnsavedCancelDialog()
    {
        this.InitializeComponent();
    }

    public void ButtonNo_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    public void ButtonYes_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(true);
    }
}