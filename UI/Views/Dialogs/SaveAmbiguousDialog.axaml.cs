using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class SaveAmbiguousDialog : Window
{
    private SaveAmbiguousViewModel? ViewModel => this.DataContext as SaveAmbiguousViewModel;
    
    public SaveAmbiguousDialog()
    {
        InitializeComponent();
    }

    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        this.Close(this.TagListBox.SelectedItem);
    }
    
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}