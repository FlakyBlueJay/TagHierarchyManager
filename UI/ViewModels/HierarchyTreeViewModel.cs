using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class HierarchyTreeViewModel : ViewModelBase, IDisposable
{
    private readonly Func<List<int>, List<string>> _getParentNamesById;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private Dictionary<int, HashSet<int>> _childNodeMap = new();

    [ObservableProperty] private TagItemViewModel? _selectedTag;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _topLevelTagNodes = [];

    [ObservableProperty] private Dictionary<int, HashSet<TagItemViewModel>> _viewModelMap = new();

    public HierarchyTreeViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this._getParentNamesById = mainWindow.TagDatabaseService.GetParentNamesByIds;
        this.SubscribeToEvents();
    }

    public void Dispose()
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen) return;

        this._mainWindow.TagDatabaseService.TagUpdated -= this.TagDatabase_OnTagUpdated;
        this._mainWindow.TagDatabaseService.TagAdded -= this.TagDatabase_OnTagAdded;
        this._mainWindow.TagDatabaseService.TagDeleted -= this.TagDatabase_OnTagDeleted;
        this._mainWindow.TagDatabaseService.TagsAdded -= this.TagDatabase_OnTagsAdded;
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync()
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen) return;
        this.ViewModelMap.Clear();
        this.TopLevelTagNodes.Clear();
        await Task.Run(() =>
        {
            var topLevelTags = this._mainWindow.TagDatabaseService.GetAllTags(true);
            var buildingTopLevelNodes = new List<TagItemViewModel>();
            foreach (var tagNode in
                     topLevelTags!.Select(tag => new TagItemViewModel(tag, this._getParentNamesById)))
            {
                buildingTopLevelNodes.Add(tagNode);
                this.AddTagNodeToViewModelMap(tagNode);
                this.AddAllChildrenAsync(tagNode);
            }

            foreach (var tagNode in buildingTopLevelNodes)
                this.TopLevelTagNodes.Add(tagNode);
        });
    }

    private void AddAllChildrenAsync(TagItemViewModel tag, bool beingUpdated = false)
    {
        if (beingUpdated)
        {
            tag.Children.Clear();
            if (this.ChildNodeMap.TryGetValue(tag.Id, out var existingParents))
                existingParents.Clear();
        }

        var childTags =
            this._mainWindow.TagDatabaseService.GetAllTagChildren(tag.Id);

        if (childTags is null) return;
        foreach (var childNode in childTags.Select(child =>
                     new TagItemViewModel(child, this._getParentNamesById)))
        {
            if (!this.ChildNodeMap.ContainsKey(childNode.Id))
                this.ChildNodeMap.Add(childNode.Tag.Id, []);
            this.ChildNodeMap[childNode.Tag.Id].Add(tag.Id);

            this.AddTagNodeToViewModelMap(childNode, beingUpdated);

            tag.Children.Add(childNode);
            this.AddAllChildrenAsync(childNode);
        }
    }

    private async Task AddChildNode(Tag tag, int parentId)
    {
        if (!this.ViewModelMap.TryGetValue(parentId, out var parentViewModels)) return;

        foreach (var parent in parentViewModels)
        {
            if (parent.Children.Any(c => c.Id == tag.Id)) continue;

            await Task.Run(() =>
            {
                var tagNode = new TagItemViewModel(tag, this._getParentNamesById);
                this.AddAllChildrenAsync(tagNode);

                var index = 0;
                while (index < parent.Children.Count && string.Compare(parent.Children[index].Name, tagNode.Name,
                           StringComparison.CurrentCultureIgnoreCase) < 0) index++;

                parent.Children.Insert(index, tagNode);
                this.AddTagNodeToViewModelMap(tagNode);
            });
        }

        if (!this.ChildNodeMap.TryGetValue(tag.Id, out var set))
            this.ChildNodeMap.Add(tag.Id, [parentId]);
        else
            set.Add(parentId);
    }

    private void AddTagNodeToViewModelMap(TagItemViewModel tagNode, bool beingUpdated = false)
    {
        if (!this.ViewModelMap.TryGetValue(tagNode.Id, out var tagNodeSet))
        {
            this.ViewModelMap.Add(tagNode.Id, [tagNode]);
        }
        else
        {
            if (beingUpdated) tagNodeSet.Clear();
            tagNodeSet.Add(tagNode);
        }
    }

    private async Task AddTopLevelNode(Tag tag)
    {
        if (this.TopLevelTagNodes.Any(t => t.Id == tag.Id)) return;
        await Task.Run(() =>
        {
            var newTopLevelTag = new TagItemViewModel(tag, this._getParentNamesById);
            // this does create considerable delay, would be nice to speed it up somehow.
            this.AddAllChildrenAsync(newTopLevelTag);

            var index = 0;
            while (index < this.TopLevelTagNodes.Count
                   && string.Compare(this.TopLevelTagNodes[index].Name, newTopLevelTag.Name,
                       StringComparison.CurrentCultureIgnoreCase) < 0)
                index++;

            this.TopLevelTagNodes.Insert(index, newTopLevelTag);
            this.AddTagNodeToViewModelMap(newTopLevelTag);
        });
    }

    private async Task DeleteChildNode(int parentId, int idToDelete)
    {
        if (!this.ViewModelMap.TryGetValue(parentId, out var parentViewModels)) return;

        foreach (var parentTag in parentViewModels)
            await Task.Run(() =>
            {
                var foundChild =
                    parentTag.Children.FirstOrDefault(t => t.Id == idToDelete)!;
                parentTag.Children.Remove(foundChild);
            });

        if (!this.ChildNodeMap.TryGetValue(idToDelete, out var parentSet)) return;
        parentSet.Remove(parentId);
    }

    private async Task DeleteTopLevelNode(int idToDelete)
    {
        var topLevelNode = this.TopLevelTagNodes.FirstOrDefault(t => t.Id == idToDelete);
        if (topLevelNode is null) return;
        await Task.Run(() => { this.TopLevelTagNodes.Remove(topLevelNode); });
    }

    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        this._mainWindow.SelectedTag = value;
    }

    private async Task ProcessTagAdditions(List<Tag> newTags)
    {
        foreach (var newTag in newTags)
        {
            if (newTag.IsTopLevel)
                await this.AddTopLevelNode(newTag);

            if (newTag.ParentIds.Count == 0) return;
            foreach (var parentId in newTag.ParentIds)
                await this.AddChildNode(newTag, parentId);
        }
    }

    private void SubscribeToEvents()
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen) return;
        this._mainWindow.TagDatabaseService.TagUpdated += this.TagDatabase_OnTagUpdated;
        this._mainWindow.TagDatabaseService.TagAdded += this.TagDatabase_OnTagAdded;
        this._mainWindow.TagDatabaseService.TagDeleted += this.TagDatabase_OnTagDeleted;
        this._mainWindow.TagDatabaseService.TagsAdded += this.TagDatabase_OnTagsAdded;
    }

    private async void TagDatabase_OnTagAdded(object? sender, Tag newTag)
    {
        try
        {
            await this.ProcessTagAdditions([newTag]);
        }
        catch (Exception e)
        {
            var error = new ErrorDialogViewModel(e.Message);
            error.ShowDialog();
        }
    }

    private async void TagDatabase_OnTagDeleted(object? sender, (int id, string name) deletedTag)
    {
        try
        {
            await this.WipeTagNodes(deletedTag.id);
        }
        catch (Exception e)
        {
            var error = new ErrorDialogViewModel(e.Message);
            error.ShowDialog();
        }
    }

    private async void TagDatabase_OnTagsAdded(object? sender, List<Tag> newTags)
    {
        try
        {
            await this.ProcessTagAdditions(newTags);
        }
        catch (Exception e)
        {
            var error = new ErrorDialogViewModel(e.Message);
            error.ShowDialog();
        }
    }

    private async void TagDatabase_OnTagUpdated(object? sender, Tag updatedTag)
    {
        try
        {
            if (!this.ViewModelMap.TryGetValue(updatedTag.Id, out var tagViewModels)) return;

            HashSet<int> oldParents = [];
            HashSet<int> newParents = new(updatedTag.ParentIds);

            // grab old parents if they exist
            if (this.ChildNodeMap.TryGetValue(updatedTag.Id, out var parentList))
                oldParents = [..parentList];

            // add/remove parents as necessary
            var removedParents = oldParents.Except(newParents);
            var addedParents = newParents.Except(oldParents);

            foreach (var parentId in removedParents)
                await this.DeleteChildNode(parentId, updatedTag.Id);

            foreach (var parentId in addedParents)
                await this.AddChildNode(updatedTag, parentId);

            // refresh all tagViewModels
            foreach (var tag in tagViewModels)
                tag.RefreshSelf();

            // add/remove top level nodes as necessary
            if (updatedTag.IsTopLevel)
                await this.AddTopLevelNode(updatedTag);
            else
                await this.DeleteTopLevelNode(updatedTag.Id);

            // clear child node map if tag has no parents
            if (newParents.Count == 0)
                this.ChildNodeMap.Remove(updatedTag.Id);
        }
        catch (Exception e)
        {
            var error = new ErrorDialogViewModel(e.Message);
            error.ShowDialog();
        }
    }

    private async Task WipeTagNodes(int idToDelete)
    {
        if (this.ChildNodeMap.TryGetValue(idToDelete, out var parentList))
        {
            foreach (var parentId in parentList) await this.DeleteChildNode(parentId, idToDelete);
            this.ChildNodeMap.Remove(idToDelete);
            this.ViewModelMap.Remove(idToDelete);
        }

        await this.DeleteTopLevelNode(idToDelete);
    }
}