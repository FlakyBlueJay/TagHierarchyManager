using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Controls;

public partial class SearchPane : UserControl
{
    public SearchPane()
    {
        this.InitializeComponent();
        this.SearchResultsListBox.AddHandler(PointerPressedEvent, ListBox_OnPointerPressed, RoutingStrategies.Tunnel);
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

        this.ViewModel?.SearchCommand.Execute(args);
        e.Handled = true;
    }
    
    private static void ListBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsRightButtonPressed) return;
        if (e.Source is not Control source) return;

        var listItem = source.FindAncestorOfType<ListBoxItem>();
        if (listItem is null) return;
        e.Handled = true;
        listItem.ContextMenu?.Open(listItem);
    }
    
    private void SearchPane_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.Source is Control { DataContext: TagItemViewModel tag }
            && this.DataContext is SearchViewModel vm)
            vm.ContextMenuTag = tag;
    }
}