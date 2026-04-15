using Avalonia.Controls;
using Avalonia.Input;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views.Controls;

public partial class SearchPane : UserControl
{
    public SearchPane()
    {
        this.InitializeComponent();
    }

    private SearchViewModel? ViewModel => this.DataContext as SearchViewModel;

    public void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var args = new object[]
        {
            this.SearchTextBox.Text ?? "",
            this.SearchModeComboBox.SelectedIndex,
            this.SearchAliasesCheckBox.IsChecked ?? false
        };

        this.ViewModel.StartSearchCommand.Execute(args);
        e.Handled = true;
    }
}