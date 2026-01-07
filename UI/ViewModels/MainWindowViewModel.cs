using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private TagDatabase _database;
    
    
    
    private readonly Dictionary<string, TagItemViewModel> _viewModelMap = new();
    
    [ObservableProperty]
    private TagItemViewModel? _selectedTag;
    
    [ObservableProperty]
    private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    [ObservableProperty]
    private bool _isDbLoaded;
    
    public int TotalTags => this._database?.Tags.Count ?? 0;
    
    public string WindowTitle => IsDbLoaded
        ? string.Format(Resources.TitleWithDatabase, this._database.Name)
        : Resources.Title;
    // TODO searchViewModel
    // TODO hierarchyViewModel
    
    public MainWindowViewModel()
    {
    }
    
    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        value?.BeginEdit();
    }

    public async Task LoadDatabase(string filePath)
    {
        TagDatabase db = new();
        db.InitialisationComplete += OnDatabaseLoaded;
        this.IsDbLoaded = false;
        this.TopLevelTags.Clear();
        this._viewModelMap.Clear();
        await db.LoadAsync(filePath);
    }
    
    public async Task SaveTag()
    {
        if (SelectedTag is null || _database is null) return;
        var oldName = SelectedTag.Name;
        
        SelectedTag.CommitEdit();
        await _database.WriteTagToDatabase(SelectedTag.Tag);
    }
    
    private void OnDatabaseLoaded(object sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this._database = db;
            this.IsDbLoaded = true;
            this.SyncHierarchyAsync();
            this.OnPropertyChanged(nameof(TotalTags));
            this.OnPropertyChanged(nameof(WindowTitle));
            this._database.InitialisationComplete -= OnDatabaseLoaded;
            this._database.TagUpdated += OnTagUpdated;
            
        });
        Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
    }
    
    private void OnTagUpdated(object sender, Tag updatedTag)
    {
       _ = Task.Run(async () => {
            foreach (var viewModel in _viewModelMap.Values)
            {
                viewModel.RefreshParentsString();
            }
            this.SyncHierarchyAsync();
        });
    }
    
    public async Task SyncHierarchyAsync()
    {
        if (this._database is null) return;
        
        var activeKeys = new HashSet<string>();

        var result = await Task.Run(() =>
        {
            var activeKeys = new HashSet<string>();
            var children = _database.Tags
                .SelectMany(t => t.ParentIds.Select(pId => new { ParentId = pId, Child = t }))
                .ToLookup(x => x.ParentId, x => x.Child);
            var topLevelTags = _database.Tags.Where(t => t.IsTopLevel).OrderBy(t => t.Name).ToList();

            return (activeKeys, topLevelTags, children);
        });
        
        var topLevelViewModels = result.topLevelTags.Select(t => {
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

    private TagItemViewModel GetOrCreateViewModel(Tag tag, int parentId)
    {
        string key = $"{parentId}_{tag.Id}";
        if (!this._viewModelMap.TryGetValue(key, out var viewModel))
        {
            viewModel = new TagItemViewModel(tag, id => 
                this._viewModelMap.Values.FirstOrDefault(v => v.Id == id)?.Name);
            this._viewModelMap[key] = viewModel;
        }
        return viewModel;
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