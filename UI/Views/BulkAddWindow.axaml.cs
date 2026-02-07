using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class BulkAddWindow : Window
{
    // TODO keydown del = delete selected row/s
    public BulkAddWindow()
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

    private BulkAddViewModel? ViewModel => this.DataContext as BulkAddViewModel;

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => this.Close();

    private void BulkAddWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;
        var currentElement = this.FocusManager?.GetFocusedElement();
        if (currentElement is TextBox) return;
        if (this.ViewModel?.RemoveTagsCommand.CanExecute(null) != true) return;
        this.ViewModel.RemoveTagsCommand.Execute(null);
        e.Handled = true;
    }

    private void DataGrid_OnDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel?.AddTagCommand.CanExecute(null) != true) return;
        this.ViewModel.AddTagCommand.Execute(null);
    }

    private void TagTable_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.ViewModel is null) return;
        this.ViewModel.SelectedRows.Clear();
        foreach (var row in this.TagTable.SelectedItems.Cast<BulkAddViewModel.TagRow>())
            this.ViewModel.SelectedRows.Add(row);
    }
}