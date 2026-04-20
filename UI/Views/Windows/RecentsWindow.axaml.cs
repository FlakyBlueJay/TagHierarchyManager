using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class RecentsWindow : Window
{
    public RecentsWindow()
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

    private RecentsViewModel? ViewModel => this.DataContext as RecentsViewModel;

    public void DataGrid_DoubleTapped(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel is null) return;
        if (e.Source is Control { DataContext: RecentsViewModel.TagRow row })
            this.ViewModel.ActivateRowToTagItem(row);
    }
}