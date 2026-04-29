using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class HierarchyTreeViewModel : ViewModelBase, IDisposable
{
    private readonly Dictionary<int, HashSet<int>> _childNodeMap = new();
    private readonly Func<List<int>, List<string>> _getParentNamesById;
    private readonly MainWindowViewModel _mainWindow;
    private readonly DialogService _dialogService;

    private readonly Dictionary<int, HashSet<TagItemViewModel>> _viewModelMap = new();
    [ObservableProperty] private TagItemViewModel? _contextMenuTag;

    [ObservableProperty] private TagItemViewModel? _selectedTag;

    public HierarchyTreeViewModel(MainWindowViewModel mainWindow, DialogService dialogService)
    {
        this._mainWindow = mainWindow;
        this._dialogService = dialogService;
        this._getParentNamesById = mainWindow.TagDatabaseService.GetParentNamesByIds;
        this.SubscribeToEvents();
    }

    public ICommand NewTagCommand => this._mainWindow.StartNewTagCommand;

    public ICommand ShowBulkAddDialogCommand => this._mainWindow.ShowBulkAddDialogCommand;

    public ObservableCollection<TagItemViewModel> TopLevelTagNodes { get; } = [];
    private TagDatabaseService TagDatabaseService => this._mainWindow.TagDatabaseService;

    public void Dispose()
    {
        this.TagDatabaseService.TagsWritten -= this.TagDatabaseService_OnTagsWritten;
    }

    public async Task InitializeAsync()
    {
        if (!this.TagDatabaseService.IsDatabaseOpen) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            this._viewModelMap.Clear();
            this._childNodeMap.Clear();
            this.TopLevelTagNodes.Clear();
        });


        var topLevelTags = await Task.Run(() => this.TagDatabaseService.GetAllTags(true));
        var childLookup = await Task.Run(() => this.TagDatabaseService.GetChildLookup());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var buildingTopLevelNodes = new List<TagItemViewModel>();
            foreach (var tagNode in
                     topLevelTags.Select(tag => new TagItemViewModel(tag, this._getParentNamesById)))
            {
                buildingTopLevelNodes.Add(tagNode);
                this.AddTagNodeToViewModelMap(tagNode);
                this.AddAllChildren(tagNode, childLookup, []);
            }

            foreach (var tagNode in buildingTopLevelNodes)
                this.TopLevelTagNodes.Add(tagNode);
        });


        await Task.CompletedTask;
    }

    private void AddAllChildren(TagItemViewModel tag,
        Dictionary<int, List<Tag>> childLookup,
        HashSet<int> ancestors,
        bool beingUpdated = false)
    {
        if (beingUpdated)
        {
            tag.CurrentChildren.Clear();
            if (this._childNodeMap.TryGetValue(tag.Id, out var existingParents))
                existingParents.Clear();
        }

        if (!childLookup.TryGetValue(tag.Id, out var childTags)) return;
        if (!ancestors.Add(tag.Id)) return;

        try
        {
            foreach (var childNode in from child in childTags
                     where !ancestors.Contains(child.Id)
                     select new TagItemViewModel(child, this._getParentNamesById))
            {
                if (!this._childNodeMap.ContainsKey(childNode.Id))
                    this._childNodeMap.Add(childNode.Tag.Id, []);
                this._childNodeMap[childNode.Tag.Id].Add(tag.Id);

                this.AddTagNodeToViewModelMap(childNode);

                tag.CurrentChildren.Add(childNode);
                this.AddAllChildren(childNode, childLookup, ancestors);
            }
        }
        finally
        {
            ancestors.Remove(tag.Id);
        }
    }

    private async Task AddChildNode(Tag tag, int parentId)
    {
        if (!this._viewModelMap.TryGetValue(parentId, out var parentViewModels)) return;
        var childLookup = this.TagDatabaseService.GetChildLookup();

        foreach (var parent in parentViewModels.Where(parent => parent.CurrentChildren.All(c => c.Id != tag.Id)))
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var tagNode = new TagItemViewModel(tag, this._getParentNamesById);
                this.AddAllChildren(tagNode, childLookup, []);

                var index = 0;
                while (index < parent.CurrentChildren.Count
                       && this.CompareTagNodes(parent.CurrentChildren[index], tagNode) < 0) index++;

                parent.CurrentChildren.Insert(index, tagNode);
                this.AddTagNodeToViewModelMap(tagNode);
            });

        if (!this._childNodeMap.TryGetValue(tag.Id, out var set))
            this._childNodeMap.Add(tag.Id, [parentId]);
        else
            set.Add(parentId);
    }

    private void AddTagNodeToViewModelMap(TagItemViewModel tagNode)
    {
        if (!this._viewModelMap.TryGetValue(tagNode.Id, out var tagNodeSet))
            this._viewModelMap.Add(tagNode.Id, [tagNode]);
        else
            // if (beingUpdated) tagNodeSet.Clear();
            tagNodeSet.Add(tagNode);
    }

    private void AddTopLevelNode(Tag tag)
    {
        if (this.TopLevelTagNodes.Any(t => t.Id == tag.Id)) return;

        var newTopLevelTag = new TagItemViewModel(tag, this._getParentNamesById);
        var childLookup = this.TagDatabaseService.GetChildLookup();
        this.AddAllChildren(newTopLevelTag, childLookup, []);

        var index = 0;
        while (index < this.TopLevelTagNodes.Count
               && this.CompareTagNodes(this.TopLevelTagNodes[index], newTopLevelTag) < 0)
            index++;

        this.TopLevelTagNodes.Insert(index, newTopLevelTag);
        this.AddTagNodeToViewModelMap(newTopLevelTag);
    }

    private int CompareTagNodes(TagItemViewModel a, TagItemViewModel b)
    {
        var nameCompare = string.Compare(a.CurrentName, b.CurrentName, StringComparison.CurrentCultureIgnoreCase);
        if (nameCompare != 0) return nameCompare;

        var bindingA = a.Tag.TagBindings.FirstOrDefault() ?? string.Empty;
        var bindingB = b.Tag.TagBindings.FirstOrDefault() ?? string.Empty;

        return string.Compare(bindingA, bindingB, StringComparison.CurrentCultureIgnoreCase);
    }

    private void DeleteChildNode(int parentId, int idToDelete)
    {
        if (!this._viewModelMap.TryGetValue(parentId, out var parentViewModels)) return;

        foreach (var parentTag in parentViewModels)
        {
            var foundChild =
                parentTag.CurrentChildren.FirstOrDefault(t => t.Id == idToDelete)!;
            parentTag.CurrentChildren.Remove(foundChild);
        }

        if (!this._childNodeMap.TryGetValue(idToDelete, out var parentSet)) return;
        parentSet.Remove(parentId);
    }

    private void DeleteTopLevelNode(int idToDelete)
    {
        var topLevelNode = this.TopLevelTagNodes.FirstOrDefault(t => t.Id == idToDelete);
        if (topLevelNode is null) return;
        this.TopLevelTagNodes.Remove(topLevelNode);
    }

    private async Task HandleTagsWrittenEventAsync(object? sender, TagDatabaseService.TagWriteResult result)
    {
        try
        {
            if (sender is not TagDatabaseService { IsDatabaseOpen: true }) return;

            foreach (var updatedTag in result.Updated)
                await Dispatcher.UIThread.InvokeAsync(async () => await this.HandleTagUpdateAsync(updatedTag));

            if (result.Added.Count > 0)
                await Dispatcher.UIThread.InvokeAsync(async () => await this.ProcessTagAdditionsAsync(result.Added));

            foreach (var deletedTag in result.Deleted)
            {
                await Dispatcher.UIThread.InvokeAsync(() => this.WipeTagNodes(deletedTag.id));
                if (this.SelectedTag?.Id == deletedTag.id)
                    this.SelectedTag = null;
            }
        }
        catch (Exception e)
        {
            await this._dialogService.ShowErrorDialog(e.Message);
        }
    }

    private async Task HandleTagUpdateAsync(Tag updatedTag)
    {
        if (!this._viewModelMap.TryGetValue(updatedTag.Id, out var tagViewModels)) return;


        foreach (var tag in tagViewModels)
        {
            tag.UpdateTag(updatedTag);
            tag.RefreshSelf();
            tag.RefreshParentsString();
        }

        HashSet<int> oldParents = [];
        HashSet<int> newParents = new(updatedTag.ParentIds);

        // grab old parents if they exist
        if (this._childNodeMap.TryGetValue(updatedTag.Id, out var parentList))
            oldParents = [..parentList];

        // add/remove parents as necessary
        var removedParents = oldParents.Except(newParents);
        var addedParents = newParents.Except(oldParents);

        foreach (var parentId in removedParents)
            this.DeleteChildNode(parentId, updatedTag.Id);

        foreach (var parentId in addedParents)
            await this.AddChildNode(updatedTag, parentId);

        // add/remove top level nodes as necessary
        if (updatedTag.IsTopLevel)
            this.AddTopLevelNode(updatedTag);
        else
            this.DeleteTopLevelNode(updatedTag.Id);

        // clear child node map if tag has no parents
        if (newParents.Count == 0)
            this._childNodeMap.Remove(updatedTag.Id);
    }

    // public ICommand NewTagCommand => this._mainWindow.NewTagCommand;
    // ReSharper disable once PartialMethodParameterNameMismatch
    partial void OnSelectedTagChanged(TagItemViewModel? _, TagItemViewModel? newValue)
    {
        if (newValue is null || this._mainWindow.SelectedTagId == newValue.Id) return;
        this._mainWindow.SelectedTag = newValue;
        this._mainWindow.SelectedTagId = newValue.Id;
    }

    private async Task ProcessTagAdditionsAsync(IReadOnlyList<Tag> newTags)
    {
        foreach (var newTag in newTags)
        {
            if (newTag.IsTopLevel)
                this.AddTopLevelNode(newTag);

            if (newTag.ParentIds.Count == 0) continue;
            foreach (var parentId in newTag.ParentIds)
                await this.AddChildNode(newTag, parentId);
        }
    }

    [RelayCommand]
    private async Task StartContextMenuTagDeletion()
    {
        if (this.ContextMenuTag is null) return;
        await this._mainWindow.StartTagDeletionAsync(this.ContextMenuTag.Id);
    }

    private void SubscribeToEvents()
    {
        if (!this.TagDatabaseService.IsDatabaseOpen) return;
        this.TagDatabaseService.TagsWritten += this.TagDatabaseService_OnTagsWritten;
    }

    private void TagDatabaseService_OnTagsWritten(object? sender, TagDatabaseService.TagWriteResult result)
    {
        _ = this.HandleTagsWrittenEventAsync(sender, result);
    }

    private void WipeTagNodes(int idToDelete)
    {
        if (this._childNodeMap.TryGetValue(idToDelete, out var parentList))
        {
            foreach (var parentId in parentList) this.DeleteChildNode(parentId, idToDelete);
            this._childNodeMap.Remove(idToDelete);
            this._viewModelMap.Remove(idToDelete);
        }

        this.DeleteTopLevelNode(idToDelete);
    }
}