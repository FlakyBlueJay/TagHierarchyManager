using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesViewModel? ViewModel => DataContext as UnsavedChangesViewModel;
    
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }
    
    public void ButtonYes_Click(object? sender, RoutedEventArgs e) => this.Close(true);
    public void ButtonNo_Click(object? sender, RoutedEventArgs e) => this.Close(false);
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => this.Close(null);
}