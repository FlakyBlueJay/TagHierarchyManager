using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class BulkAddWindow : Window
{
    // TODO keydown del = delete selected row/s
    public BulkAddWindow()
    {
        this.InitializeComponent();
    }

    private BulkAddViewModel? ViewModel => this.DataContext as BulkAddViewModel;

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void ButtonAdd_OnClick(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.AddTag();
    }

    private void ButtonDelete_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedRows =
            this.TagTable.SelectedItems.Cast<BulkAddViewModel.TagRow>().ToHashSet();
        this.ViewModel?.RemoveTags(selectedRows);
    }

    private async void ButtonSave_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel is null) return;
            await this.ViewModel.SaveTags();
            this.Close();
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    private void DataGrid_OnDoubleClick(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.AddTag();
    }
}