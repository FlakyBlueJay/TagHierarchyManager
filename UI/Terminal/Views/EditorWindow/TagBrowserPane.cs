using System.Collections.ObjectModel;
using TagHierarchyManager.Common;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A paned view for browsing the <see cref="TagDatabase" />'s tag hierarchy tree.
/// </summary>
public class TagBrowserPane : View
{
    internal readonly Button NewTagButton = new();
    private ComboBox modeSelector = null!;
    private TextField queryField = null!;
    private ListView resultsList = null!;
    private TreeView tagTree = null!;
    private CheckBox searchAliasesCheckbox = null!;
    private bool resultsListAutoSelect = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TagBrowserPane" /> class.
    /// </summary>
    public TagBrowserPane()
    {
        this.InitialiseUI();
    }
    
    private void TagDatabaseService_OnSearchFinished(object? sender, List<Tag> results)
    {
        this.resultsListAutoSelect = true;
        if (results.Count > 0)
            this.resultsList.SetSource(new ObservableCollection<Tag>(results));
        else
            this.resultsList.SetSource<string>(["No results found."]);
        this.resultsList.SelectedItem = 0;
        this.resultsListAutoSelect = false;
    }

    private void InitialiseUI()
    {
        TabView tabView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };

        Tab tagTreeTab = new()
        {
            DisplayText = "Hierarchy Tree",
            View = new View
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true,
            },
        };

        this.tagTree = new TreeView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() ! - 2,
            CanFocus = true,
            MultiSelect = false,
        };

        this.NewTagButton.Y = Pos.Bottom(this.tagTree);
        this.NewTagButton.Title = "New tag...";
        this.NewTagButton.Width = Dim.Fill();
        this.NewTagButton.CanFocus = true;

        tagTreeTab.View.Add(this.tagTree, this.NewTagButton);

        Tab searchTab = new()
        {
            DisplayText = "Search",
            View = new View
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true,
            },
        };

        this.queryField = new TextField
        {
            Width = Dim.Percent(75),
            CanFocus = true,
            Height = 1,
        };
        this.modeSelector = new ComboBox
        {
            Y = Pos.Bottom(this.queryField) + 2,
            Width = Dim.Fill(),
            Height = 5,
            CanFocus = true,
            ReadOnly = true,
        };
        this.searchAliasesCheckbox = new CheckBox 
        {
            Y = Pos.Top(this.modeSelector) - 1,
            Text = "Search in aliases",
            CanFocus = true,
        };
        Line separatorLine = new()
        {
            Y = Pos.Bottom(this.modeSelector),
        };
        Button searchButton = new()
        {
            X = Pos.Right(this.queryField),
            Y = 0,
            Text = "Search",
            Width = Dim.Fill(),
            CanFocus = true,
        };
        this.modeSelector.SetSource<string>(["Fuzzy search", "Starts with", "Ends with", "Exact match"]);
        this.modeSelector.SelectedItem = 0;
        this.resultsList = new ListView
        {
            X = 0,
            Y = Pos.Bottom(this.modeSelector) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };

        searchTab.View.Add(this.queryField, searchButton, this.searchAliasesCheckbox, this.modeSelector,
            separatorLine, this.resultsList);
        tabView.AddTab(tagTreeTab, true);
        tabView.AddTab(searchTab, false);
        this.Add(tabView);
        this.Enabled = false;

        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitialised;
        TagDatabaseService.TagAdded += this.TagDatabaseService_OnTagAdded;
        TagDatabaseService.TagDeleted += this.TagDatabaseService_OnTagDeleted;
        TagDatabaseService.TagSaved += this.TagDatabaseService_OnTagSaved;
        TagDatabaseService.SearchFinished += this.TagDatabaseService_OnSearchFinished;
        searchButton.Accepting += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(this.queryField.Text)) return;
            bool isChecked = this.searchAliasesCheckbox.CheckedState == CheckState.Checked;
            TagDatabaseService.SearchTags(this.queryField.Text,
                (TagDatabaseSearchMode)this.modeSelector.SelectedItem, isChecked);
            args.Handled = true;
        };
        this.queryField.KeyDown += (_, args) =>
        {
            if (args.KeyCode != Key.Enter || string.IsNullOrWhiteSpace(this.queryField.Text)) return;
            bool isChecked = this.searchAliasesCheckbox.CheckedState == CheckState.Checked;
            TagDatabaseService.SearchTags(this.queryField.Text,
                (TagDatabaseSearchMode)this.modeSelector.SelectedItem, false);
            args.Handled = true;
        };

        this.tagTree.ObjectActivated += (_, args) =>
        {
            if (args.ActivatedObject is TagHierarchyTreeNode node)
                Program.MainView.EditPane.LoadTag(node.AssociatedTag);
        };

        this.resultsList.OpenSelectedItem += (_, args) =>
        {
            if (args.Value is not Tag selectedTag || this.resultsListAutoSelect) return;
            Program.MainView.EditPane.LoadTag(selectedTag);
        };
    }

    private void RefreshTagTree(TagDatabase db)
    {
        this.tagTree.ClearObjects();
        IEnumerable<TagHierarchyTreeNode> topLevelNodes =
            db.Tags
                .Where(tag => tag.IsTopLevel)
                .OrderBy(tag => tag.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(tag => new TagHierarchyTreeNode(tag));
        this.tagTree.AddObjects(topLevelNodes);
    }

    private void TagDatabaseService_OnInitialised(object? sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        this.RefreshTagTree(db);
    }

    private void TagDatabaseService_OnTagAdded(object? sender, Tag tag)
    {
        foreach (int parent in tag.ParentIds)
            this.tagTree.Objects.Cast<TagHierarchyTreeNode>().Where(node => node.AssociatedTag.Id == parent)
                .ToList()
                .ForEach(node => this.tagTree.RefreshObject(node));
    }

    private void TagDatabaseService_OnTagDeleted(object? sender, (int Id, string Name) tag)
    {
        this.tagTree.Objects.Cast<TagHierarchyTreeNode>()
            .Where(node => node.AssociatedTag.Id == tag.Id).ToList()
            .ForEach(node => this.tagTree.Remove(node));
        this.resultsList.SetSource(new ObservableCollection<object>(Array.Empty<object>()));
    }

    private void TagDatabaseService_OnTagSaved(object? sender, Tag tag)
    {
        if (sender is not TagDatabase db) return; 
        List<TagHierarchyTreeNode> existingNodes = this.tagTree.Objects.Cast<TagHierarchyTreeNode>()
            .Where(node => node.AssociatedTag.Id == tag.Id).ToList();

        switch (tag.IsTopLevel)
        {
            case false when existingNodes.All(node => !node.IsChildNode):
            {
                foreach (TagHierarchyTreeNode node in existingNodes) this.tagTree.Remove(node);

                break;
            }
            case true:
                this.tagTree.AddObject(new TagHierarchyTreeNode(tag));
                // this feels jank but it works.
                // i wish there was smth better than refreshing the entire tree but terminal.gui doesn't do sorting
                // so this'll do.
                // the user may expect to see their changes immediately reflected anyway.
                this.RefreshTagTree(db);
                break;
        }

        foreach (TagHierarchyTreeNode node in
                 this.tagTree.Objects.Cast<TagHierarchyTreeNode>()
                     .Where(node => node.AssociatedTag.Id == tag.Id))
            this.tagTree.RefreshObject(node);
    }
}