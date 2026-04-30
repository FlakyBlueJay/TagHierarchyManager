using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class SaveAmbiguousDialog : Window
{
    public SaveAmbiguousDialog()
    {
        this.InitializeComponent();
    }

    private SaveAmbiguousViewModel? ViewModel => this.DataContext as SaveAmbiguousViewModel;

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        this.Close(this.TagListBox.SelectedItem);
    }
}