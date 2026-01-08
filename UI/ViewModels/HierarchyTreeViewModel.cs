using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class HierarchyTreeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly Dictionary<string, TagItemViewModel> _viewModelMap = new();
    
    [ObservableProperty]
    private TagItemViewModel? _selectedTag;

    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        _mainWindow.SelectedTag = value;
    }
    
    [ObservableProperty]
    private ObservableCollection<TagItemViewModel> _topLevelTags = [];
    
    public HierarchyTreeViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this._mainWindow.Database.TagUpdated += this.OnTagUpdated;
    }
    
    public async Task InitializeAsync()
    {
        await this.SyncHierarchyAsync();
    }

    
    private async void OnTagUpdated(object sender, Tag updatedTag)
    {
        try
        {
            await Task.Run(() =>
            {
                foreach (var viewModel in this._viewModelMap.Values)
                {
                    viewModel.RefreshParentsString();
                }
            });
            await this.SyncHierarchyAsync();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    public async Task SyncHierarchyAsync()
    {
        if (this._mainWindow.Database is null) return;

        var activeKeys = new HashSet<string>();

        try
        {
            var result = await Task.Run(() =>
            {
                var activeKeys = new HashSet<string>();
                var children = this._mainWindow.Database.Tags
                    .SelectMany(t => t.ParentIds.Select(pId => new { ParentId = pId, Child = t }))
                    .ToLookup(x => x.ParentId, x => x.Child);
                var topLevelTags = this._mainWindow.Database.Tags.Where(t => t.IsTopLevel).OrderBy(t => t.Name)
                    .ToList();

                return (activeKeys, topLevelTags, children);
            });

            var topLevelViewModels = result.topLevelTags.Select(t =>
            {
                var vm = GetOrCreateViewModel(t, 0);
                activeKeys.Add($"0_{t.Id}");
                SyncTagRecursive(vm, result.children, activeKeys);
                return vm;
            }).ToList();
            SyncCollection(TopLevelTags, topLevelViewModels);

            var keysToRemove = _viewModelMap.Keys.Where(k => !activeKeys.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                _viewModelMap.Remove(key);
            }
        }
        catch
        {
            throw; // TODO handle exception
        }
    }
    
    private TagItemViewModel GetOrCreateViewModel(Tag tag, int parentId)
    {
        string key = $"{parentId}_{tag.Id}";
        if (!this._viewModelMap.TryGetValue(key, out var viewModel))
        {
            viewModel = new TagItemViewModel(tag, id => 
                this._viewModelMap.Values.FirstOrDefault(v => v.Id == id)?.Name);
            this._viewModelMap[key] = viewModel;
            viewModel.UserEditedTag += (s, e) => this._mainWindow.UnsavedChanges = true;
        }
        return viewModel;
    }
    
    private void SyncTagRecursive(TagItemViewModel parentVm, ILookup<int, Tag> childrenLookup, HashSet<string> activeKeys)
    {
        var childTags = childrenLookup[parentVm.Id].OrderBy(t => t.Name).ToList();
        var childVms = new List<TagItemViewModel>();

        foreach (var ct in childTags)
        {
            var key = $"{parentVm.Id}_{ct.Id}";
            activeKeys.Add(key);
            
            var childVm = GetOrCreateViewModel(ct, parentVm.Id);
            childVms.Add(childVm);
            
            // Recurse: build this child's children
            SyncTagRecursive(childVm, childrenLookup, activeKeys);
        }

        parentVm.SyncChildren(childVms);
    }
    
    private void SyncCollection(ObservableCollection<TagItemViewModel> collection, List<TagItemViewModel> newItems)
    {
        var updatedKeys = newItems.Select(v => v.Id).ToHashSet();
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (!updatedKeys.Contains(collection[i].Id))
                collection.RemoveAt(i);
        }
        
        var currentKeys = collection.Select(v => v.Id).ToHashSet();
        foreach (var newItem in newItems)
        {
            if (!currentKeys.Contains(newItem.Id))
            {
                // Find the correct index to maintain alphabetical order
                int index = 0;
                while (index < collection.Count && string.Compare(collection[index].Name, newItem.Name, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    index++;
                }
                collection.Insert(index, newItem);
            }
        }
    }
}