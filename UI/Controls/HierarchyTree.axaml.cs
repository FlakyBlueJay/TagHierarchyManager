using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views.Controls;

public partial class HierarchyTree : UserControl
{
    public HierarchyTree()
    {
        this.InitializeComponent();
        this.TagTree.AddHandler(PointerPressedEvent, OnTagTreeItemRightClick, RoutingStrategies.Tunnel);
    }

    private static void OnTagTreeItemRightClick(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsRightButtonPressed) return;
        if (e.Source is not Control source) return;

        var treeItem = source.FindAncestorOfType<TreeViewItem>();
        if (treeItem is null) return;
        e.Handled = true; // prevents right-click selection change
        treeItem.ContextMenu?.Open(treeItem);
    }

    private void TagTree_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.Source is Control { DataContext: TagItemViewModel tag }
            && this.DataContext is HierarchyTreeViewModel vm)
            vm.ContextMenuTag = tag;
    }
}