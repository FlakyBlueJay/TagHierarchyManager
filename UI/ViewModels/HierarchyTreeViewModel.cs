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
    private readonly MainWindowViewModel _mainWindow;
    private readonly Dictionary<string, TagItemViewModel> _viewModelMap = new();

    [ObservableProperty] private TagItemViewModel? _selectedTag;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    public HierarchyTreeViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;

        this.SubscribeToEvents();
    }

    public void Dispose()
    {
        if (this._mainWindow.Database is null) return;

        this._mainWindow.Database.TagUpdated -= this.TagDatabase_OnTagUpdated;
        this._mainWindow.Database.TagAdded -= this.TagDatabase_OnTagAdded;
        this._mainWindow.Database.TagDeleted -= this.TagDatabase_OnTagDeleted;

        this._viewModelMap.Clear();
    }

    public async Task InitializeAsync()
    {
        await this.SyncHierarchyAsync();
    }

    private TagItemViewModel GetOrCreateViewModel(Tag tag, int parentId)
    {
        var key = $"{parentId}_{tag.Id}";
        if (!this._viewModelMap.TryGetValue(key, out var viewModel))
        {
            viewModel = new TagItemViewModel(tag, id =>
                this._viewModelMap.Values.FirstOrDefault(v => v.Id == id)?.Name);
            this._viewModelMap[key] = viewModel;
            viewModel.UserEditedTag += this.OnUserEditedTag;
        }

        return viewModel;
    }
    
    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        this._mainWindow.SelectedTag = value;
    }

    private async Task OnTreeUpdate()
    {
        await Task.Run(() =>
        {
            foreach (var viewModel in this._viewModelMap.Values) viewModel.RefreshParentsString();
        });
        await this.SyncHierarchyAsync();
    }
    
    private void OnUserEditedTag(object? sender, EventArgs e) => this._mainWindow.UnsavedChanges = true;

    private void SubscribeToEvents()
    {
        if (this._mainWindow.Database is null) return;
        this._mainWindow.Database.TagUpdated += this.TagDatabase_OnTagUpdated;
        this._mainWindow.Database.TagAdded += this.TagDatabase_OnTagAdded;
        this._mainWindow.Database.TagDeleted += this.TagDatabase_OnTagDeleted;
    }

    private void SyncCollection(ObservableCollection<TagItemViewModel> collection, List<TagItemViewModel> newItems)
    {
        var updatedKeys = newItems.Select(v => v.Id).ToHashSet();
        for (var i = collection.Count - 1; i >= 0; i--)
            if (!updatedKeys.Contains(collection[i].Id))
                collection.RemoveAt(i);

        var currentKeys = collection.Select(v => v.Id).ToHashSet();
        foreach (var newItem in newItems)
            if (!currentKeys.Contains(newItem.Id))
            {
                // Find the correct index to maintain alphabetical order
                var index = 0;
                while (index < collection.Count && string.Compare(collection[index].Name, newItem.Name,
                           StringComparison.CurrentCultureIgnoreCase) < 0) index++;
                collection.Insert(index, newItem);
            }
    }

    private async Task SyncHierarchyAsync()
    {
        if (this._mainWindow.Database is null) return;

        var activeKeys = new HashSet<string>();

        var result = await Task.Run(() =>
        {
            var children = this._mainWindow.Database.Tags
                .SelectMany(t => t.ParentIds.Select(pId => new { ParentId = pId, Child = t }))
                .ToLookup(x => x.ParentId, x => x.Child);
            var topLevelTags = this._mainWindow.Database.Tags.Where(t => t.IsTopLevel).OrderBy(t => t.Name)
                .ToList();

            return (topLevelTags, children);
        });

        var topLevelViewModels = result.topLevelTags.Select(t =>
        {
            var vm = this.GetOrCreateViewModel(t, 0);
            activeKeys.Add($"0_{t.Id}");
            this.SyncTagRecursive(vm, result.children, activeKeys);
            return vm;
        }).ToList();
        this.SyncCollection(this.TopLevelTags, topLevelViewModels);

        var keysToRemove = this._viewModelMap.Keys.Where(k => !activeKeys.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            this._viewModelMap[key].UserEditedTag -= this.OnUserEditedTag;
            this._viewModelMap.Remove(key);
        }
    }

    private void SyncTagRecursive(TagItemViewModel parentVm, ILookup<int, Tag> childrenLookup,
        HashSet<string> activeKeys)
    {
        var childTags = childrenLookup[parentVm.Id].OrderBy(t => t.Name).ToList();
        var childVms = new List<TagItemViewModel>();

        foreach (var ct in childTags)
        {
            var key = $"{parentVm.Id}_{ct.Id}";
            activeKeys.Add(key);

            var childVm = this.GetOrCreateViewModel(ct, parentVm.Id);
            childVms.Add(childVm);

            this.SyncTagRecursive(childVm, childrenLookup, activeKeys);
        }

        parentVm.SyncChildren(childVms);
    }

    private void TagDatabase_OnTagAdded(object? sender, Tag tag) =>
        _ = Task.Run(async () => await this.OnTreeUpdate());

    private void TagDatabase_OnTagDeleted(object? sender, (int id, string name) tag) =>
        _ = Task.Run(async () => await this.OnTreeUpdate());

    private void TagDatabase_OnTagUpdated(object? sender, Tag tag) =>
        _ = Task.Run(async () => await this.OnTreeUpdate());
}