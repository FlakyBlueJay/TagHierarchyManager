using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI.Views;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        this.InitializeComponent();
    }

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(null);
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