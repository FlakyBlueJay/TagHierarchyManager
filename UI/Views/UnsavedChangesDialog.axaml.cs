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
        this.DataContextChanged += (s, e) =>
        {
            if (ViewModel is not null)
            {
                ViewModel.CloseAction = (result) => this.Close(result);
            }
        };
    }
    
    public void ButtonYes_Click(object? sender, RoutedEventArgs e) => ViewModel?.Save();
    public void ButtonNo_Click(object? sender, RoutedEventArgs e) => ViewModel?.Discard();
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => ViewModel?.Cancel();
}