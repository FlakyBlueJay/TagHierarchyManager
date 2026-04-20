using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class ImportDialog : Window
{
    public ImportDialog()
    {
        this.InitializeComponent();

        this.DataContextChanged += (_, _) =>
        {
            if (this.ViewModel != null) this.ViewModel.RequestClose += this.Close;
        };

        this.Unloaded += (_, _) =>
        {
            if (this.ViewModel != null)
                this.ViewModel.RequestClose -= this.Close;
        };
    }

    private ImportDialogViewModel? ViewModel => this.DataContext as ImportDialogViewModel;

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}